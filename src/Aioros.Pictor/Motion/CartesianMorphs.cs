using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Motion;

/// <summary>
/// The update animations of Recharts' cartesian series: a line's or an area's points from <c>prevPoints</c> by
/// proportional index, a bar from <c>prevData</c> by index, a scatter's symbols by index; a series whose geometry did
/// not change, or that does not animate, stays where it is.
/// </summary>
internal static class CartesianMorphs
{
    public static Morph? Plan(ChartLayout from, ChartLayout to)
    {
        var parts = new List<IMorphPart>();
        var frames = new List<SeriesLayout>(to.Series.Count);
        foreach (var next in to.Series)
        {
            var previous = Previous(from, to, next);
            var timing = Animations.Of(next.Spec);
            IMorphPart? part = previous is null || timing is not { } value || Unchanged(previous, next) ? null : next.Spec.Kind switch
            {
                SeriesKind.Bar => new BarMorph(previous, next, to.Vertical, value),
                SeriesKind.Scatter => new ScatterMorph(previous, next, value),
                SeriesKind.Line => new PointMorph(previous, next, value, next.Spec.AnimateNewValues ? (to.PlotWidth * 2, to.PlotHeight / 2) : null),
                _ => new PointMorph(previous, next, value, null),
            };
            if (part is null)
            {
                frames.Add(next.Morphed(next.X, next.Y, next.Base, next.Defined, next.Bars, next.Sizes, still: true));
                continue;
            }

            parts.Add(part);
            frames.Add(((ISeriesMorph)part).Frame);
        }

        return Morph.Of(to.WithSeries(frames), parts);
    }

    /// <summary>The series drawn before the update at the same place among the chart's children, of the same kind.</summary>
    private static SeriesLayout? Previous(ChartLayout from, ChartLayout to, SeriesLayout next)
    {
        var index = -1;
        for (var i = 0; i < to.Spec.Series.Length; i++)
        {
            if (ReferenceEquals(to.Spec.Series[i], next.Spec))
            {
                index = i;
                break;
            }
        }

        if (index < 0 || index >= from.Spec.Series.Length || from.Spec.Series[index].Kind != next.Spec.Kind)
        {
            return null;
        }

        var spec = from.Spec.Series[index];
        return from.Series.Find(series => ReferenceEquals(series.Spec, spec));
    }

    private static bool Unchanged(SeriesLayout a, SeriesLayout b) =>
        Morph.SameData(a.Data, b.Data) && Morph.Same(a.X, b.X) && Morph.Same(a.Y, b.Y) && Morph.Same(a.Base, b.Base) && Morph.Same(a.Sizes, b.Sizes) && a.Bars.AsSpan().SequenceEqual(b.Bars);
}

/// <summary>A series' update animation, with the frame series it moves.</summary>
internal interface ISeriesMorph
{
    SeriesLayout Frame { get; }
}

/// <summary>
/// Recharts' <c>Line</c> and <c>Area</c> update: each new point moves from the previous point at its index scaled by
/// the previous count over the new (<c>Math.floor(index * prevPoints.length / points.length)</c>), a missing coordinate
/// counting as 0 as JavaScript reads <c>null</c>; an area's baseline moves the same way; with no previous point, a
/// line's points come from twice the plot's width and half its height (<c>animateNewValues</c>), an area's stay.
/// </summary>
internal sealed class PointMorph : IMorphPart, ISeriesMorph
{
    private readonly SeriesLayout _from;
    private readonly SeriesLayout _to;
    private readonly Timing _timing;
    private readonly (double X, double Y)? _entering;

    public PointMorph(SeriesLayout from, SeriesLayout to, Timing timing, (double X, double Y)? entering)
    {
        (_from, _to, _timing, _entering) = (from, to, timing, entering);
        var count = to.X.Length;
        Frame = to.Morphed(new double[count], new double[count], new double[to.Base.Length], new bool[count], to.Bars, to.Sizes, still: false);
    }

    public SeriesLayout Frame { get; }

    public double End => _timing.End;

    public void At(double elapsed)
    {
        var t = _timing.Progress(elapsed);
        var (x, y, bases, defined) = (Frame.X, Frame.Y, Frame.Base, Frame.Defined);
        var count = x.Length;
        var previous = _from.X.Length;
        var factor = (double)previous / count;
        for (var i = 0; i < count; i++)
        {
            if (previous > 0)
            {
                var p = (int)Math.Floor(i * factor);
                x[i] = Morph.Lerp(Morph.Js(_from.X[p]), Morph.Js(_to.X[i]), t);
                y[i] = Morph.Lerp(Morph.Js(_from.Y[p]), Morph.Js(_to.Y[i]), t);
                Base(bases, i, p, t);
                defined[i] = true;
            }
            else if (_entering is { } from)
            {
                x[i] = Morph.Lerp(from.X, Morph.Js(_to.X[i]), t);
                y[i] = Morph.Lerp(from.Y, Morph.Js(_to.Y[i]), t);
                Base(bases, i, -1, t);
                defined[i] = true;
            }
            else
            {
                (x[i], y[i], defined[i]) = (_to.X[i], _to.Y[i], _to.Defined[i]);
                Base(bases, i, -1, t);
            }
        }
    }

    private void Base(double[] bases, int i, int p, double t)
    {
        if (i < bases.Length)
        {
            bases[i] = p >= 0 && p < _from.Base.Length ? Morph.Lerp(Morph.Js(_from.Base[p]), Morph.Js(_to.Base[i]), t) : _to.Base[i];
        }
    }
}

/// <summary>
/// Recharts' <c>Bar</c> update: each bar moves from the previous bar at its index (x, y, width and height
/// interpolated); a bar with none grows from its base, its height from 0 at its bottom edge (its width from 0 in a
/// vertical chart).
/// </summary>
internal sealed class BarMorph : IMorphPart, ISeriesMorph
{
    private readonly SeriesLayout _from;
    private readonly SeriesLayout _to;
    private readonly bool _vertical;
    private readonly Timing _timing;

    public BarMorph(SeriesLayout from, SeriesLayout to, bool vertical, Timing timing)
    {
        (_from, _to, _vertical, _timing) = (from, to, vertical, timing);
        Frame = to.Morphed(to.X, to.Y, to.Base, to.Defined, new BarRect[to.Bars.Length], to.Sizes, still: false);
    }

    public SeriesLayout Frame { get; }

    public double End => _timing.End;

    public void At(double elapsed)
    {
        var t = _timing.Progress(elapsed);
        var bars = Frame.Bars;
        for (var i = 0; i < bars.Length; i++)
        {
            var bar = _to.Bars[i];
            if (i < _from.Bars.Length)
            {
                var prev = _from.Bars[i];
                bars[i] = bar with { X = Morph.Lerp(prev.X, bar.X, t), Y = Morph.Lerp(prev.Y, bar.Y, t), Width = Morph.Lerp(prev.Width, bar.Width, t), Height = Morph.Lerp(prev.Height, bar.Height, t) };
            }
            else if (!_vertical)
            {
                var height = Morph.Lerp(0, bar.Height, t);
                bars[i] = bar with { Y = bar.Y + bar.Height - height, Height = height };
            }
            else
            {
                bars[i] = bar with { Width = Morph.Lerp(0, bar.Width, t) };
            }
        }
    }
}

/// <summary>Recharts' <c>Scatter</c> update: each symbol moves from the previous one at its index (center and area interpolated); a symbol with none grows its area from 0.</summary>
internal sealed class ScatterMorph : IMorphPart, ISeriesMorph
{
    private const double DefaultSize = 64;
    private readonly SeriesLayout _from;
    private readonly SeriesLayout _to;
    private readonly Timing _timing;

    public ScatterMorph(SeriesLayout from, SeriesLayout to, Timing timing)
    {
        (_from, _to, _timing) = (from, to, timing);
        var count = to.X.Length;
        Frame = to.Morphed(new double[count], new double[count], to.Base, new bool[count], to.Bars, new double[count], still: false);
    }

    public SeriesLayout Frame { get; }

    public double End => _timing.End;

    public void At(double elapsed)
    {
        var t = _timing.Progress(elapsed);
        var (x, y, sizes, defined) = (Frame.X, Frame.Y, Frame.Sizes, Frame.Defined);
        for (var i = 0; i < x.Length; i++)
        {
            var size = Size(_to, i);
            if (i < _from.X.Length)
            {
                x[i] = Morph.Lerp(_from.X[i], _to.X[i], t);
                y[i] = Morph.Lerp(_from.Y[i], _to.Y[i], t);
                sizes[i] = Morph.Lerp(Size(_from, i), size, t);
            }
            else
            {
                (x[i], y[i], sizes[i]) = (_to.X[i], _to.Y[i], Morph.Lerp(0, size, t));
            }

            defined[i] = _to.Defined[i] && !double.IsNaN(x[i]) && !double.IsNaN(y[i]);
        }
    }

    private static double Size(SeriesLayout series, int index) => index < series.Sizes.Length ? series.Sizes[index] : DefaultSize;
}
