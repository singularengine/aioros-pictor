using Aioros.Pictor.Data;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>
/// The height a chart that keeps an aspect ratio takes so its legend, its axes and a readable plot fit one above the
/// other: Recharts takes the legend's height from the plot, which leaves none when a narrow chart's legend wraps, so the
/// layout grows as a browser's flex-wrap grows the box around it.
/// </summary>
internal static class ChartFit
{
    private const double MinimumPlotHeight = 40;

    /// <summary>The chart laid out, at the height that holds it when the height its aspect gives cannot.</summary>
    public static IChartPlot Compute(ChartSpec spec, ChartData data)
    {
        var plot = Plots.Compute(spec, data);
        return Needed(plot) is { } height ? Plots.Compute(spec with { Height = height, Aspect = spec.Width / height }, data) : plot;
    }

    /// <summary>The height the plot's chart needs, when that is more than it has.</summary>
    public static double? Needed(IChartPlot plot)
    {
        if (plot is not ChartLayout { Spec: { Responsive: true, Aspect: not null } spec, Legend: { Spec: { Vertical: false } } legend } layout || legend.Spec.VerticalAlign == "middle")
        {
            return null;
        }

        var axes = layout.XAxes.Where(static axis => !axis.Spec.Hide).Sum(static axis => axis.Height);
        var needed = Math.Min(Math.Ceiling(spec.Margin.Top + spec.Margin.Bottom + legend.Height + axes + (spec.Brush?.Height ?? 0) + MinimumPlotHeight), spec.MaxHeight ?? double.PositiveInfinity);
        return needed > spec.Height ? needed : null;
    }
}
