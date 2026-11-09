using Aioros.Pictor.Data;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;

namespace Aioros.Pictor.Motion;

/// <summary>A part of a chart that an update animation moves from its previous geometry to its new one.</summary>
internal interface IMorphPart
{
    /// <summary>When the part's movement ends, in milliseconds after the update.</summary>
    double End { get; }

    /// <summary>Moves the part's frame geometry to where it is at a time after the update.</summary>
    void At(double elapsed);
}

/// <summary>
/// Recharts' update animation of a chart (each series' <c>prevPoints</c>, <c>prevData</c> or <c>prevSectors</c>
/// interpolated to its new geometry by its own timing): a frame plot that shares the new layout's axes, legend and
/// data and owns the arrays its moving parts are interpolated into, so drawing a frame allocates nothing more.
/// </summary>
internal sealed class Morph
{
    private readonly IMorphPart[] _parts;
    private readonly IChartPlot _frame;

    private Morph(IChartPlot frame, IMorphPart[] parts, bool enters)
    {
        _frame = frame;
        _parts = parts;
        Enters = enters;
        End = parts.Max(part => part.End);
    }

    /// <summary>When the last part's movement ends, in milliseconds after the update.</summary>
    public double End { get; }

    /// <summary>Whether a frame plays enter movements too (a treemap's nodes that are new slide in as they enter).</summary>
    public bool Enters { get; }

    /// <summary>The chart as the update animation draws it at a time after the update.</summary>
    public IChartPlot Frame(double elapsed)
    {
        foreach (var part in _parts)
        {
            part.At(elapsed);
        }

        return _frame;
    }

    /// <summary>The update animation from a chart's previous layout to its new one, or null when nothing it animates has moved.</summary>
    public static Morph? Plan(IChartPlot from, IChartPlot to) => (from, to) switch
    {
        (ChartLayout previous, ChartLayout next) => CartesianMorphs.Plan(previous, next),
        (PolarLayout previous, PolarLayout next) => PolarMorphs.Plan(previous, next),
        (FunnelPlot previous, FunnelPlot next) => FunnelMorph.Plan(previous, next),
        (TreemapPlot previous, TreemapPlot next) => TreemapMorph.Plan(previous, next),
        _ => null,
    };

    /// <summary>A morph of a frame plot moved by its parts, or null when no part moves.</summary>
    public static Morph? Of(IChartPlot frame, List<IMorphPart> parts, bool enters = false) =>
        parts.Count == 0 ? null : new Morph(frame, [.. parts], enters);

    /// <summary>d3's <c>interpolateNumber</c> at <paramref name="t"/>, as Recharts' animations interpolate.</summary>
    public static double Lerp(double a, double b, double t) => (a * (1 - t)) + (b * t);

    /// <summary>A coordinate as JavaScript's <c>+value</c> reads Recharts' missing one (<c>null</c>): 0.</summary>
    public static double Js(double value) => double.IsNaN(value) ? 0 : value;

    /// <summary>
    /// Whether a part's items are the same as before in every column its layout read: Recharts compares each point with
    /// its item (lodash's <c>isEqual</c> over the points and their <c>payload</c>), so a part whose shapes stay animates
    /// anyway when an item it was drawn from changed (its dots and labels wait), and stays only when its items did not.
    /// </summary>
    public static bool SameData(ChartData a, ChartData b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (var (key, values) in b.NumbersRead)
        {
            if (!Same(a.Numbers(key), values))
            {
                return false;
            }
        }

        foreach (var (key, texts) in b.TextsRead)
        {
            if (!a.Texts(key).AsSpan().SequenceEqual(texts))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Whether two coordinate arrays hold the same values (NaN equal to NaN), as lodash's <c>isEqual</c> compares them.</summary>
    public static bool Same(double[] a, double[] b) => a.AsSpan().SequenceEqual(b);
}
