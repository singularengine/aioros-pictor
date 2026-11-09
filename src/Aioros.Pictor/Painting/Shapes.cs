using System.Collections.Immutable;
using Aioros.Graphs.Paths;
using Aioros.Graphs.Shapes;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>A line through points with a curve (Recharts' <c>Curve</c> without a baseline).</summary>
internal readonly struct CurveShape(Curve curve, double[] x, double[] y, bool[] defined) : IShape
{
    public void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink => Line.Write(ref sink, curve, x, y, defined);
}

/// <summary>An area between a top line and a baseline (Recharts' <c>Curve</c> with a baseline, d3's area).</summary>
internal readonly struct AreaShape(Curve curve, double[] x, double[] baseline, double[] top, bool[] defined) : IShape
{
    public void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink => Area.Write(ref sink, curve, x, baseline, top, defined);
}

/// <summary>An area of a vertical chart, between a baseline x and a top x at each y (d3's <c>areaY</c>, Recharts' vertical <c>Curve</c>).</summary>
internal readonly struct VerticalAreaShape(Curve curve, double[] baseline, double[] y, double[] top, bool[] defined) : IShape
{
    public void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink => Area.Write(ref sink, curve, baseline, y, y, defined, top);
}

/// <summary>d3's symbol of a type and an area at a center (Recharts' <c>Symbols</c>).</summary>
internal readonly struct SymbolShape(SymbolType type, double size, double cx, double cy) : IShape
{
    public void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink
    {
        var context = new PathContext<TSink>(ref sink);
        Symbol.Write(ref context, type, size, cx, cy);
    }
}

/// <summary>Recharts' <c>Cross</c>: a vertical line through x over a height and a horizontal one through y over a width.</summary>
internal readonly struct CrossShape(double x, double y, double left, double top, double width, double height) : IShape
{
    public void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink
    {
        sink.MoveTo(x, top);
        sink.LineTo(x, top + height);
        sink.MoveTo(left, y);
        sink.LineTo(left + width, y);
    }
}

/// <summary>Recharts' <c>Rectangle</c>: a rectangle with its own radius per corner (top left, top right, bottom right, bottom left).</summary>
internal readonly struct RectShape(double x, double y, double width, double height, ImmutableArray<double> radius) : IShape
{
    /// <summary>Whether a corner is rounded, so a bar growing in is drawn again at each size rather than scaled.</summary>
    public bool Rounded => !radius.IsDefaultOrEmpty && radius.Any(corner => corner > 0);

    /// <summary>
    /// The rectangle part way through Recharts' bar enter animation at an eased progress: a horizontal chart's bar
    /// <c>y + height - h</c> high by <c>h</c>, a vertical chart's as wide as <c>width</c> times the progress, its
    /// corners drawn again at that size (their radius clamped to half of it).
    /// </summary>
    public RectShape Grown(bool vertical, double progress) => vertical
        ? new RectShape(x, y, width * progress, height, radius)
        : new RectShape(x, y + height - (height * progress), width, height * progress, radius);

    public void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink
    {
        var max = Math.Min(Math.Abs(width) / 2, Math.Abs(height) / 2);
        if (radius.IsDefaultOrEmpty || max <= 0)
        {
            sink.Rect(x, y, width, height);
            return;
        }

        var ySign = height >= 0 ? 1 : -1;
        var xSign = width >= 0 ? 1 : -1;
        var sweep = (height >= 0 && width >= 0) || (height < 0 && width < 0);
        Span<double> corners = stackalloc double[4];
        for (var i = 0; i < 4; i++)
        {
            corners[i] = Math.Min(max, radius.Length == 1 ? radius[0] : i < radius.Length ? radius[i] : 0);
        }

        sink.MoveTo(x, y + (ySign * corners[0]));
        Arc(ref sink, corners[0], sweep, x + (xSign * corners[0]), y);
        sink.LineTo(x + width - (xSign * corners[1]), y);
        Arc(ref sink, corners[1], sweep, x + width, y + (ySign * corners[1]));
        sink.LineTo(x + width, y + height - (ySign * corners[2]));
        Arc(ref sink, corners[2], sweep, x + width - (xSign * corners[2]), y + height);
        sink.LineTo(x + (xSign * corners[3]), y + height);
        Arc(ref sink, corners[3], sweep, x, y + height - (ySign * corners[3]));
        sink.ClosePath();
    }

    private static void Arc<TSink>(ref TSink sink, double radius, bool sweep, double x, double y)
        where TSink : struct, IPathSink
    {
        if (radius > 0)
        {
            sink.ArcTo(radius, false, sweep, x, y);
        }
    }
}

internal static class Curves
{
    /// <summary>The curve Aioros's morph rebuilds a moving line or band with (0 linear, 1 monotone along x, 2 along y), or null for a curve it does not draw.</summary>
    public static byte? Morph(CurveType type, bool vertical) => type switch
    {
        CurveType.Linear => 0,
        CurveType.Monotone => vertical ? (byte)2 : (byte)1,
        CurveType.MonotoneX => 1,
        CurveType.MonotoneY => 2,
        _ => null,
    };

    /// <summary>The curve of a series; Recharts' <c>monotone</c> and <c>bump</c> are d3's monotoneY and bumpY in a vertical chart.</summary>
    public static Curve Of(CurveType type, bool vertical) => vertical && type switch
    {
        CurveType.Monotone => true,
        CurveType.Bump => true,
        _ => false,
    } ? type == CurveType.Monotone ? Curve.MonotoneY : Curve.BumpY : Of(type);

    public static Curve Of(CurveType type) => type switch
    {
        CurveType.Monotone or CurveType.MonotoneX => Curve.MonotoneX,
        CurveType.MonotoneY => Curve.MonotoneY,
        CurveType.Natural => Curve.Natural,
        CurveType.Step => Curve.Step,
        CurveType.StepBefore => Curve.StepBefore,
        CurveType.StepAfter => Curve.StepAfter,
        CurveType.Basis => Curve.Basis,
        CurveType.LinearClosed => Curve.LinearClosed,
        CurveType.BasisClosed => Curve.BasisClosed,
        CurveType.BasisOpen => Curve.BasisOpen,
        CurveType.Bump or CurveType.BumpX => Curve.BumpX,
        CurveType.BumpY => Curve.BumpY,
        _ => Curve.Linear,
    };
}
