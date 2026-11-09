using Aioros.Pictor.Data;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>A chart laid out by its family's layout (cartesian, polar, funnel, treemap, Sankey, sunburst), ready to paint.</summary>
internal interface IChartPlot
{
    ChartSpec Spec { get; }

    /// <summary>How many hover states the chart has (categories or items); 0 when it shows no tooltip.</summary>
    int HoverCount { get; }

    /// <summary>The hover state shown before any pointer event (Recharts' <c>defaultIndex</c>), or -1.</summary>
    int DefaultTip => -1;

    /// <summary>Whether a click, not hovering, picks the hover state shown (Recharts' <c>trigger="click"</c>).</summary>
    bool TipsOnClick => Spec.Tooltip is { ClickTrigger: true };

    /// <summary>Whether moving off every state keeps the last one drawn (Recharts' <c>active={true}</c>).</summary>
    bool KeepsTip => Spec.Tooltip is { Active: true };
}

/// <summary>
/// Lays a chart out and paints it by its family: the one place that knows which layout and painter draw each kind of
/// chart. The painters are generic over the canvas, so every family is specialized for both targets.
/// </summary>
internal static class Plots
{
    public static IChartPlot Compute(ChartSpec spec, ChartData data) => spec.Kind switch
    {
        ChartKind.Pie or ChartKind.Radar or ChartKind.RadialBar => PolarLayout.Compute(spec, data),
        ChartKind.Funnel => FunnelPlot.Compute(spec, data),
        ChartKind.Treemap => TreemapPlot.Compute(spec, data),
        ChartKind.Sankey => SankeyPlot.Compute(spec, data),
        ChartKind.Sunburst => SunburstPlot.Compute(spec, data),
        _ => ChartLayout.Compute(spec.Compact ? spec with { Legend = null, Tooltip = null } : spec, data),
    };

    /// <summary>Paints the chart with every hover state (<see cref="ChartPainter.AllTips"/>), none (<see cref="ChartPainter.NoTips"/>) or one.</summary>
    public static void Paint<TCanvas>(ref TCanvas canvas, IChartPlot plot, int active)
        where TCanvas : struct, IChartCanvas
    {
        switch (plot)
        {
            case ChartLayout layout:
                ChartPainter.Paint(ref canvas, layout, active);
                break;
            case PolarLayout polar:
                PolarPainter.Paint(ref canvas, polar, active);
                break;
            case FunnelPlot funnel:
                FunnelPainter.Paint(ref canvas, funnel, active);
                break;
            case TreemapPlot treemap:
                TreemapPainter.Paint(ref canvas, treemap, active);
                break;
            case SankeyPlot sankey:
                SankeyPainter.Paint(ref canvas, sankey, active);
                break;
            case SunburstPlot sunburst:
                SunburstPainter.Paint(ref canvas, sunburst, active);
                break;
        }
    }

    /// <summary>Whether the desktop draws a hover state by painting the chart again with the state's shapes in place (a funnel's active shape under its labels) rather than over its recorded scene.</summary>
    public static bool InPlace(IChartPlot plot) => plot is FunnelPlot funnel && FunnelPainter.InPlace(funnel);

    /// <summary>The chart painted with a hover state's shapes in place and no hover layers, for <see cref="InPlace"/>.</summary>
    public static void PaintInPlace<TCanvas>(ref TCanvas canvas, IChartPlot plot, int active)
        where TCanvas : struct, IChartCanvas
    {
        if (plot is FunnelPlot funnel)
        {
            FunnelPainter.PaintInPlace(ref canvas, funnel, active);
        }
    }

    /// <summary>The desktop's hover state drawn under the recorded scene (a cartesian chart's cursor).</summary>
    public static void HoverBelow<TCanvas>(ref TCanvas canvas, IChartPlot plot, int active)
        where TCanvas : struct, IChartCanvas
    {
        switch (plot)
        {
            case ChartLayout layout:
                TipPainter.Cursors(ref canvas, layout, active);
                break;
        }
    }

    /// <summary>The desktop's hover state drawn over the recorded scene (active dots, active shapes, the tooltip).</summary>
    public static void HoverAbove<TCanvas>(ref TCanvas canvas, IChartPlot plot, int active, (double X, double Y)? pointer = null)
        where TCanvas : struct, IChartCanvas
    {
        switch (plot)
        {
            case ChartLayout layout:
                TipPainter.Tips(ref canvas, layout, active, pointer);
                break;
            case PolarLayout polar:
                PolarTips.Hover(ref canvas, polar, active, pointer);
                break;
            case FunnelPlot funnel:
                FunnelPainter.Tips(ref canvas, funnel, active);
                break;
            case TreemapPlot treemap:
                TreemapPainter.Tips(ref canvas, treemap, active);
                break;
            case SankeyPlot sankey:
                SankeyPainter.Tips(ref canvas, sankey, active);
                break;
            case SunburstPlot sunburst:
                SunburstPainter.Tips(ref canvas, sunburst, active);
                break;
        }
    }

    /// <summary>
    /// Whether the keyboard moves the chart's tooltip (Recharts' <c>AccessibilityManager</c>): a horizontal chart whose
    /// tooltip follows its category axis; Recharts' manager does nothing in a vertical or polar chart, nor for item
    /// tooltips, which a spoofed pointer cannot enter.
    /// </summary>
    public static bool Keyboard(IChartPlot plot) =>
        plot is ChartLayout { Vertical: false, Categories: not null, ItemTooltip: false, BarTooltip: false, HoverCount: > 0 } && plot.Spec.AccessibilityLayer;

    /// <summary>
    /// Whether a finger dragged across the chart moves its tooltip (Recharts' <c>handleTouchMove</c>, registered on the
    /// wrapper of a chart whose tooltip follows the axis and shows on hover): the categorical and the radar and radial bar
    /// charts; item tooltips do not move, nor a tooltip shown on click.
    /// </summary>
    public static bool Follows(IChartPlot plot) => plot.HoverCount > 0 && plot.Spec.Tooltip is { ClickTrigger: false } && plot switch
    {
        ChartLayout { ItemTooltip: false, BarTooltip: false } => true,
        PolarLayout { Spec.Kind: not ChartKind.Pie, ItemBars: false } => true,
        _ => false,
    };

    /// <summary>Whether the chart's tooltip boxes follow the pointer on the web (an axis tooltip shown on hover, as Recharts moves it with the mouse within a category).</summary>
    public static bool FollowsPointer(IChartPlot plot) => plot.HoverCount > 0 && plot.Spec.Tooltip is { } tooltip && plot switch
    {
        ChartLayout { ItemTooltip: false, BarTooltip: false } layout => TooltipBox.Follows(tooltip, TipPainter.Follow(layout)),
        PolarLayout { Spec.Kind: not ChartKind.Pie, ItemBars: false } polar => TooltipBox.Follows(tooltip, PolarTips.Followed(polar, 0, null).Follow),
        _ => false,
    };

    /// <summary>Whether the chart's hover states draw a cursor (Recharts' <c>Cursor</c>: a tooltip that follows the axis, or a scatter chart's cross).</summary>
    public static bool DrawsCursors(IChartPlot plot) => plot.Spec.Tooltip is { } tooltip && TipPainter.ShowsCursor(tooltip) && plot switch
    {
        ChartLayout { BarTooltip: false, ItemTooltip: false } => true,
        PolarLayout { Spec.Kind: not ChartKind.Pie, ItemBars: false } => true,
        _ => false,
    };

    /// <summary>The legend a chart draws, if any.</summary>
    public static LegendLayout? LegendOf(IChartPlot plot) => plot switch
    {
        ChartLayout layout => layout.Legend,
        PolarLayout polar => polar.Legend,
        FunnelPlot funnel => funnel.Legend,
        _ => null,
    };

    /// <summary>The hover state under a point of the chart, or <see cref="ChartPainter.NoTips"/>.</summary>
    public static int Hovered(IChartPlot plot, double x, double y) => plot switch
    {
        ChartLayout layout => TipPainter.Hovered(layout, x, y),
        PolarLayout polar => PolarTips.Hovered(polar, x, y),
        FunnelPlot funnel => FunnelPainter.Hovered(funnel, x, y),
        TreemapPlot treemap => TreemapPainter.Hovered(treemap, x, y),
        SankeyPlot sankey => SankeyPainter.Hovered(sankey, x, y),
        SunburstPlot sunburst => SunburstPainter.Hovered(sunburst, x, y),
        _ => ChartPainter.NoTips,
    };
}
