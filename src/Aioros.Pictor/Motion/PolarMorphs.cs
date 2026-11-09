using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;

namespace Aioros.Pictor.Motion;

/// <summary>
/// The update animations of Recharts' polar series: a pie's sectors re-swept from their previous angles, a radar's
/// points from <c>prevPoints</c> by proportional index, a radial bar's angles from <c>prevData</c> by index. A range
/// radar does not animate (Recharts' <c>!isRange</c>).
/// </summary>
internal static class PolarMorphs
{
    public static Morph? Plan(PolarLayout from, PolarLayout to)
    {
        var same = Morph.SameData(from.Data, to.Data);
        var parts = new List<IMorphPart>();
        var pies = Each(from.Pies, to.Pies, parts, Pie);
        var radars = Each(from.Radars, to.Radars, parts, (previous, next) => Radar(previous, next, to.Cx, to.Cy, same));
        var bars = Each(from.RadialBars, to.RadialBars, parts, (previous, next) => RadialBar(previous, next, same));
        return Morph.Of(to.With(pies, radars, bars), parts);
    }

    /// <summary>
    /// Recharts' enter animation of a polar chart, which is its update from nothing: a pie's sectors grow from 0 packed
    /// from the start angle (<c>prevSectors</c> is empty), a radar's points come from the center, a radial bar sweeps
    /// from its start angle; a range radar does not animate. Null when nothing animates.
    /// </summary>
    public static Morph? Enter(PolarLayout to)
    {
        var none = PolarLayout.Empty(to);
        return Plan(none, to) is { } morph ? morph : null;
    }

    private static (PieLayout, IMorphPart?) Pie(PieLayout? previous, PieLayout next)
    {
        if (previous is null || next.Sectors.Length == 0 || Animations.Of(next.Spec) is not { } timing || (previous.Sectors.AsSpan().SequenceEqual(next.Sectors) && Morph.SameData(previous.Data, next.Data)))
        {
            return (next.Morphed(still: true), null);
        }

        var morph = new PieMorph(previous, next, timing);
        return (morph.Frame, morph);
    }

    private static (RadarLayout, IMorphPart?) Radar(RadarLayout? previous, RadarLayout next, double cx, double cy, bool same)
    {
        if (previous is null || next.IsRange || Animations.Of(next.Spec) is not { } timing || (same && Morph.Same(previous.X, next.X) && Morph.Same(previous.Y, next.Y)))
        {
            return (Radar(next, next.X, next.Y, still: true), null);
        }

        var morph = new RadarMorph(previous, next, cx, cy, timing);
        return (morph.Frame, morph);
    }

    private static (RadialBarLayout, IMorphPart?) RadialBar(RadialBarLayout? previous, RadialBarLayout next, bool same)
    {
        if (previous is null || Animations.Of(next.Spec) is not { } timing || (same && previous.Sectors.AsSpan().SequenceEqual(next.Sectors)))
        {
            return (new RadialBarLayout { Spec = next.Spec, Sectors = next.Sectors, Still = true }, null);
        }

        var morph = new RadialBarMorph(previous, next, timing);
        return (morph.Frame, morph);
    }

    /// <summary>Each new part with the previous one at its index: its frame, and its morph when it moves.</summary>
    private static List<T> Each<T>(List<T> from, List<T> to, List<IMorphPart> parts, Func<T?, T, (T Frame, IMorphPart? Morph)> morph)
        where T : class
    {
        var frames = new List<T>(to.Count);
        for (var i = 0; i < to.Count; i++)
        {
            var (frame, part) = morph(i < from.Count ? from[i] : null, to[i]);
            frames.Add(frame);
            if (part is not null)
            {
                parts.Add(part);
            }
        }

        return frames;
    }

    public static RadarLayout Radar(RadarLayout radar, double[] x, double[] y, bool still) => new()
    {
        Spec = radar.Spec,
        Values = radar.Values,
        X = x,
        Y = y,
        BaseX = radar.BaseX,
        BaseY = radar.BaseY,
        Lows = radar.Lows,
        Still = still,
    };
}

/// <summary>
/// Recharts' <c>Pie</c> update: the sectors packed again from the first one's start angle, each sweeping from its
/// previous span to its new one (from 0 when it had none), the pie's signed padding angle added before every sector
/// but the first.
/// </summary>
internal sealed class PieMorph(PieLayout from, PieLayout to, Timing timing) : IMorphPart
{
    public PieLayout Frame { get; } = to.Morphed(still: false);

    public double End => timing.End;

    public void At(double elapsed)
    {
        var t = timing.Progress(elapsed);
        var sectors = Frame.Sectors;
        var padding = Math.Sign(to.Spec.EndAngle - to.Spec.StartAngle) * to.Spec.PaddingAngle;
        var current = to.Sectors[0].Shape.StartAngle;
        for (var i = 0; i < sectors.Length; i++)
        {
            var sector = to.Sectors[i];
            var span = sector.Shape.EndAngle - sector.Shape.StartAngle;
            var previous = i < from.Sectors.Length ? from.Sectors[i].Shape.EndAngle - from.Sectors[i].Shape.StartAngle : 0;
            var start = current + (i > 0 ? padding : 0);
            var end = start + Morph.Lerp(previous, span, t);
            sectors[i] = sector with { Shape = sector.Shape with { StartAngle = start, EndAngle = end } };
            current = end;
        }
    }
}

/// <summary>Recharts' <c>Radar</c> update: each point from the previous point at its proportional index, or from the center when the radar had none.</summary>
internal sealed class RadarMorph : IMorphPart
{
    private readonly RadarLayout _from;
    private readonly RadarLayout _to;
    private readonly double _cx;
    private readonly double _cy;
    private readonly Timing _timing;

    public RadarMorph(RadarLayout from, RadarLayout to, double cx, double cy, Timing timing)
    {
        (_from, _to, _cx, _cy, _timing) = (from, to, cx, cy, timing);
        Frame = PolarMorphs.Radar(to, new double[to.X.Length], new double[to.Y.Length], still: false);
    }

    public RadarLayout Frame { get; }

    public double End => _timing.End;

    public void At(double elapsed)
    {
        var t = _timing.Progress(elapsed);
        var (x, y) = (Frame.X, Frame.Y);
        var previous = _from.X.Length;
        var factor = (double)previous / x.Length;
        for (var i = 0; i < x.Length; i++)
        {
            var p = (int)Math.Floor(i * factor);
            var (fx, fy) = previous > 0 ? (_from.X[p], _from.Y[p]) : (_cx, _cy);
            x[i] = Morph.Lerp(fx, _to.X[i], t);
            y[i] = Morph.Lerp(fy, _to.Y[i], t);
        }
    }
}

/// <summary>Recharts' <c>RadialBar</c> update: each sector's start and end angles from the previous sector at its index; a sector with none sweeps from its start angle.</summary>
internal sealed class RadialBarMorph : IMorphPart
{
    private readonly RadialBarLayout _from;
    private readonly RadialBarLayout _to;
    private readonly Timing _timing;

    public RadialBarMorph(RadialBarLayout from, RadialBarLayout to, Timing timing)
    {
        (_from, _to, _timing) = (from, to, timing);
        Frame = new RadialBarLayout { Spec = to.Spec, Sectors = new RadialSector[to.Sectors.Length] };
    }

    public RadialBarLayout Frame { get; }

    public double End => _timing.End;

    public void At(double elapsed)
    {
        var t = _timing.Progress(elapsed);
        var sectors = Frame.Sectors;
        for (var i = 0; i < sectors.Length; i++)
        {
            var sector = _to.Sectors[i];
            var shape = sector.Shape;
            sectors[i] = sector with
            {
                Shape = i < _from.Sectors.Length
                    ? shape with { StartAngle = Morph.Lerp(_from.Sectors[i].Shape.StartAngle, shape.StartAngle, t), EndAngle = Morph.Lerp(_from.Sectors[i].Shape.EndAngle, shape.EndAngle, t) }
                    : shape with { EndAngle = Morph.Lerp(shape.StartAngle, shape.EndAngle, t) },
            };
        }
    }
}
