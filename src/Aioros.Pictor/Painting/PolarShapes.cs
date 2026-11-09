using Aioros.Graphs.Paths;
using Aioros.Pictor.Layout;

namespace Aioros.Pictor.Painting;

/// <summary>Recharts' <c>Sector</c>: a ring or a pie slice between two angles, with rounded corners when asked.</summary>
internal readonly record struct SectorShape(double Cx, double Cy, double InnerRadius, double OuterRadius, double StartAngle, double EndAngle, double CornerRadius = 0) : IShape
{
    /// <summary>
    /// The sector's numbers for the web's update, a new sector sweeping from <paramref name="from"/> (Recharts' sector
    /// path; null for a rounded sector, whose corners Aioros's morph does not draw).
    /// </summary>
    public Motion.MorphMark? Morph(double from) => CornerRadius > 0
        ? null
        : Motion.MorphMark.Numbers(Motion.MorphKind.Sector, new Motion.Six(Cx, Cy, InnerRadius, OuterRadius, StartAngle, EndAngle), 6, new Motion.Six(double.NaN, double.NaN, double.NaN, double.NaN, from, from), 6);

    /// <summary>Every digit, as Recharts writes a sector: a ring's 359.999° arc ends 0.0003 px from where it starts.</summary>
    public static int? Digits => null;

    /// <summary>Whether Recharts draws the sector at all: not when its outer radius is below its inner one or its angles meet.</summary>
    public bool Drawn => OuterRadius >= InnerRadius && StartAngle != EndAngle;

    /// <summary>Whether a sector too narrow for its corners is drawn as a rounded cap anyway (Recharts' <c>forceCornerRadius</c>).</summary>
    public bool ForceCorner { get; init; }

    /// <summary>Whether the corners round outwards, beyond the sector's angles (Recharts' <c>cornerIsExternal</c>).</summary>
    public bool CornerExternal { get; init; }

    public void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink
    {
        var corner = Math.Min(CornerRadius, (OuterRadius - InnerRadius) / 2);
        if (CornerRadius > 0 && Math.Abs(StartAngle - EndAngle) < 360 && Cornered(ref sink, corner))
        {
            return;
        }

        Plain(ref sink);
    }

    private void Plain<TSink>(ref TSink sink)
        where TSink : struct, IPathSink
    {
        var delta = Math.Sign(EndAngle - StartAngle) * Math.Min(Math.Abs(EndAngle - StartAngle), 359.999);
        var end = StartAngle + delta;
        var (sx, sy) = Polar.Point(Cx, Cy, OuterRadius, StartAngle);
        var (ex, ey) = Polar.Point(Cx, Cy, OuterRadius, end);
        sink.MoveTo(sx, sy);
        sink.ArcTo(OuterRadius, Math.Abs(delta) > 180, StartAngle > end, ex, ey);
        if (InnerRadius > 0)
        {
            var (isx, isy) = Polar.Point(Cx, Cy, InnerRadius, StartAngle);
            var (iex, iey) = Polar.Point(Cx, Cy, InnerRadius, end);
            sink.LineTo(iex, iey);
            sink.ArcTo(InnerRadius, Math.Abs(delta) > 180, StartAngle <= end, isx, isy);
        }
        else
        {
            sink.LineTo(Cx, Cy);
        }

        sink.ClosePath();
    }

    private bool Cornered<TSink>(ref TSink sink, double corner)
        where TSink : struct, IPathSink
    {
        var sign = Math.Sign(EndAngle - StartAngle);
        var start = Tangent(OuterRadius, StartAngle, sign, false, corner);
        var finish = Tangent(OuterRadius, EndAngle, -sign, false, corner);
        var outerArc = CornerExternal ? Math.Abs(StartAngle - EndAngle) : Math.Abs(StartAngle - EndAngle) - start.Theta - finish.Theta;
        if (outerArc < 0)
        {
            if (!ForceCorner)
            {
                return false;
            }

            sink.MoveTo(start.LineX, start.LineY);
            sink.ArcTo(corner, false, true, start.LineX + (corner * 2), start.LineY);
            sink.ArcTo(corner, false, true, start.LineX, start.LineY);
            return true;
        }

        sink.MoveTo(start.LineX, start.LineY);
        sink.ArcTo(corner, false, sign < 0, start.CircleX, start.CircleY);
        sink.ArcTo(OuterRadius, outerArc > 180, sign < 0, finish.CircleX, finish.CircleY);
        sink.ArcTo(corner, false, sign < 0, finish.LineX, finish.LineY);
        if (InnerRadius > 0)
        {
            var innerStart = Tangent(InnerRadius, StartAngle, sign, true, corner);
            var innerFinish = Tangent(InnerRadius, EndAngle, -sign, true, corner);
            var innerArc = CornerExternal ? Math.Abs(StartAngle - EndAngle) : Math.Abs(StartAngle - EndAngle) - innerStart.Theta - innerFinish.Theta;
            if (innerArc < 0 && corner == 0)
            {
                sink.LineTo(Cx, Cy);
                sink.ClosePath();
                return true;
            }

            sink.LineTo(innerFinish.LineX, innerFinish.LineY);
            sink.ArcTo(corner, false, sign < 0, innerFinish.CircleX, innerFinish.CircleY);
            sink.ArcTo(InnerRadius, innerArc > 180, sign > 0, innerStart.CircleX, innerStart.CircleY);
            sink.ArcTo(corner, false, sign < 0, innerStart.LineX, innerStart.LineY);
            sink.ClosePath();
            return true;
        }

        sink.LineTo(Cx, Cy);
        sink.ClosePath();
        return true;
    }

    private (double CircleX, double CircleY, double LineX, double LineY, double Theta) Tangent(double radius, double angle, int sign, bool external, double corner)
    {
        var centerRadius = (corner * (external ? 1 : -1)) + radius;
        var theta = Math.Asin(corner / centerRadius) / Polar.Radian;
        var centerAngle = CornerExternal ? angle : angle + (sign * theta);
        var (circleX, circleY) = Polar.Point(Cx, Cy, radius, centerAngle);
        var (lineX, lineY) = Polar.Point(Cx, Cy, centerRadius * Math.Cos(theta * Polar.Radian), CornerExternal ? angle - (sign * theta) : angle);
        return (circleX, circleY, lineX, lineY, theta);
    }
}

/// <summary>
/// Recharts' <c>Polygon</c>: a closed path through the defined points, broken at the missing ones, back to the first
/// point before it closes; a polar grid's polygon (<paramref name="repeatFirst"/> false) closes without it.
/// </summary>
internal readonly struct PolygonShape(double[] x, double[] y, bool repeatFirst = true, bool connectNulls = false) : IShape
{
    /// <summary>Whether the path closes (Recharts fills only a polygon of one unbroken run of points).</summary>
    public bool Closed
    {
        get
        {
            var runs = 0;
            var open = false;
            for (var i = 0; i < x.Length; i++)
            {
                var valid = Valid(i);
                runs += valid && !open ? 1 : 0;
                open = valid;
            }

            return connectNulls ? runs > 0 : runs == 1 && !(Valid(0) && !Valid(x.Length - 1));
        }
    }

    public void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink
    {
        var segments = 0;
        var open = false;
        for (var i = 0; i < x.Length; i++)
        {
            if (!Valid(i))
            {
                open = connectNulls && open;
                continue;
            }

            if (!open)
            {
                segments++;
                sink.MoveTo(x[i], y[i]);
                open = true;
            }
            else
            {
                sink.LineTo(x[i], y[i]);
            }
        }

        if (repeatFirst && x.Length > 0 && Valid(0))
        {
            if (open)
            {
                sink.LineTo(x[0], y[0]);
            }
            else
            {
                segments++;
                sink.MoveTo(x[0], y[0]);
            }
        }

        if (segments == 1)
        {
            sink.ClosePath();
        }
    }

    private bool Valid(int i) => double.IsFinite(x[i]) && double.IsFinite(y[i]);
}

/// <summary>
/// Recharts' range <c>Polygon</c> (a radar whose values are <c>[low, high]</c>): the polygon of the upper ends without its
/// closing, a line to the polygon of the lower ends walked backwards (Recharts reverses its base points in place), closed
/// when that polygon is one unbroken run.
/// </summary>
internal readonly struct RangePolygonShape(double[] x, double[] y, double[] baseX, double[] baseY) : IShape
{
    /// <summary>Whether the path closes, so Recharts fills it.</summary>
    public bool Closed => Segments(baseX, baseY) == 1;

    public void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink
    {
        Walk(ref sink, x, y, reversed: false, lineFirst: false);
        if (Walk(ref sink, baseX, baseY, reversed: true, lineFirst: true) == 1)
        {
            sink.ClosePath();
        }
    }

    /// <summary>How many runs of valid points Recharts' <c>getParsedPoints</c> makes of a point list, its first point repeated at its end.</summary>
    private static int Segments(double[] x, double[] y)
    {
        var segments = 0;
        var open = false;
        for (var i = 0; i < x.Length; i++)
        {
            var valid = Valid(x, y, i);
            segments += valid && !open ? 1 : 0;
            open = valid;
        }

        return segments + (x.Length > 0 && Valid(x, y, 0) && !open ? 1 : 0);
    }

    private static int Walk<TSink>(ref TSink sink, double[] x, double[] y, bool reversed, bool lineFirst)
        where TSink : struct, IPathSink
    {
        var n = x.Length;
        var segments = 0;
        var open = false;
        for (var k = 0; k <= n && n > 0; k++)
        {
            var i = reversed ? n - 1 - (k % n) : k % n;
            if (!Valid(x, y, i))
            {
                if (k == n)
                {
                    break;
                }

                open = false;
                continue;
            }

            if (open)
            {
                sink.LineTo(x[i], y[i]);
                continue;
            }

            segments++;
            open = true;
            if (lineFirst && segments == 1)
            {
                sink.LineTo(x[i], y[i]);
            }
            else
            {
                sink.MoveTo(x[i], y[i]);
            }
        }

        return segments;
    }

    private static bool Valid(double[] x, double[] y, int i) => double.IsFinite(x[i]) && double.IsFinite(y[i]);
}

/// <summary>A straight segment drawn as a path, as Recharts' linear <c>Curve</c> through two points writes it.</summary>
internal readonly record struct SegmentShape(double X1, double Y1, double X2, double Y2) : IShape
{
    public void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink
    {
        sink.MoveTo(X1, Y1);
        sink.LineTo(X2, Y2);
    }
}
