using System.Collections.Immutable;
using Aioros.Pictor.Theming;

namespace Aioros.Pictor.Specs;

/// <summary>Which of Recharts' charts a spec describes.</summary>
public enum ChartKind
{
    /// <summary><c>LineChart</c>.</summary>
    Line,

    /// <summary><c>AreaChart</c>.</summary>
    Area,

    /// <summary><c>BarChart</c>.</summary>
    Bar,

    /// <summary><c>ComposedChart</c>.</summary>
    Composed,

    /// <summary><c>ScatterChart</c>.</summary>
    Scatter,

    /// <summary><c>PieChart</c>.</summary>
    Pie,

    /// <summary><c>RadarChart</c>.</summary>
    Radar,

    /// <summary><c>RadialBarChart</c>.</summary>
    RadialBar,

    /// <summary><c>FunnelChart</c>.</summary>
    Funnel,

    /// <summary><c>Treemap</c>.</summary>
    Treemap,

    /// <summary><c>Sankey</c>.</summary>
    Sankey,

    /// <summary><c>SunburstChart</c>.</summary>
    Sunburst,
}

/// <summary>The space between the chart's edge and its plot and axes, Recharts' <c>margin</c>.</summary>
/// <param name="Top">The top margin.</param>
/// <param name="Right">The right margin.</param>
/// <param name="Bottom">The bottom margin.</param>
/// <param name="Left">The left margin.</param>
public readonly record struct Edges(double Top, double Right, double Bottom, double Left)
{
    /// <summary>Recharts' default margin, 5 on every side.</summary>
    public static Edges Default { get; } = new(5, 5, 5, 5);
}

/// <summary>
/// Everything a cartesian chart draws except its data: the chart, its axes, grid, series, references, legend and
/// tooltip as the XAML declares them. The Avalonia control builds it; the web writer reads it back from the plan.
/// </summary>
public sealed record ChartSpec
{
    /// <summary>The chart's kind.</summary>
    public ChartKind Kind { get; init; }

    /// <summary>The chart's width in pixels.</summary>
    public double Width { get; init; } = 500;

    /// <summary>The chart's height in pixels.</summary>
    public double Height { get; init; } = 300;

    /// <summary>Whether the chart fills its parent (Recharts' <c>ResponsiveContainer</c>): on the web its SVG scales with its view box.</summary>
    public bool Responsive { get; init; }

    /// <summary>A responsive chart's width over its height (Recharts' <c>ResponsiveContainer</c> <c>aspect</c>), kept by the web as the wrapper's <c>aspect-ratio</c>.</summary>
    public double? Aspect { get; init; }

    /// <summary>A responsive chart's smallest width (<c>ResponsiveContainer</c> <c>minWidth</c>, the control's <c>MinWidth</c>).</summary>
    public double? MinWidth { get; init; }

    /// <summary>Whether a chart that fills its parent's width is laid out <see cref="Height"/> high at each of its <see cref="ResponsiveWidths"/>, each filling the width at its own proportion.</summary>
    public bool FixedHeight { get; init; }

    /// <summary>A responsive chart's smallest height (<c>minHeight</c>, the control's <c>MinHeight</c>).</summary>
    public double? MinHeight { get; init; }

    /// <summary>A responsive chart's largest height (<c>maxHeight</c>, the control's <c>MaxHeight</c>).</summary>
    public double? MaxHeight { get; init; }

    /// <summary>The chart's margin.</summary>
    public Edges Margin { get; init; } = Edges.Default;

    /// <summary>The font size of the chart's texts.</summary>
    public double FontSize { get; init; } = 14;

    /// <summary>The gap between categories of bars: a number of pixels, or a percentage of the band (<c>10%</c>).</summary>
    public string BarCategoryGap { get; init; } = "10%";

    /// <summary>The gap between bars of one category, in pixels or a percentage.</summary>
    public string BarGap { get; init; } = "4";

    /// <summary>The width of every bar, when fixed: a number, or a percentage of the category axis' length.</summary>
    public string? BarSize { get; init; }

    /// <summary>The largest width a bar may take.</summary>
    public double? MaxBarSize { get; init; }

    /// <summary>How stacked series are offset.</summary>
    public StackOffsetKind StackOffset { get; init; }

    /// <summary>The baseline of the chart's areas that set none (Recharts' chart-level <c>baseValue</c>): a number, <c>dataMin</c> or <c>dataMax</c>.</summary>
    public string? BaseValue { get; init; }

    /// <summary>The grid behind the plot, if any.</summary>
    public GridSpec? Grid { get; init; }

    /// <summary>The horizontal axes.</summary>
    public ImmutableArray<AxisSpec> XAxes { get; init; } = [];

    /// <summary>The vertical axes.</summary>
    public ImmutableArray<AxisSpec> YAxes { get; init; } = [];

    /// <summary>The series, in the order they are drawn.</summary>
    public ImmutableArray<SeriesSpec> Series { get; init; } = [];

    /// <summary>The reference lines.</summary>
    public ImmutableArray<ReferenceLineSpec> ReferenceLines { get; init; } = [];

    /// <summary>The legend, if any.</summary>
    public LegendSpec? Legend { get; init; }

    /// <summary>The tooltip, if any.</summary>
    public TooltipSpec? Tooltip { get; init; }

    /// <summary>The surface's role with the keyboard layer (Recharts' <c>role</c>, <c>application</c> when null).</summary>
    public string? Role { get; init; }

    /// <summary>The surface's tab index with the keyboard layer (Recharts' <c>tabIndex</c>, 0 when null); the roving categories of a horizontal chart keep their own.</summary>
    public int? TabIndex { get; init; }

    /// <summary>The chart's own classes, written after <c>recharts-wrapper</c> on the web (Recharts' <c>className</c>), or none.</summary>
    public string? ClassName { get; init; }

    /// <summary>CSS declarations written last in the wrapper's style on the web (Recharts' chart <c>style</c>), or none.</summary>
    public string? WrapperStyle { get; init; }

    /// <summary>The chart's accessible name, written as the SVG's <c>title</c> (Recharts' <c>title</c>).</summary>
    public string? Title { get; init; }

    /// <summary>The chart's description, written as the SVG's <c>desc</c> (Recharts' <c>desc</c>).</summary>
    public string? Desc { get; init; }

    /// <summary>Whether a cartesian chart is the surface alone, without its wrapper, legend or tooltip box (Recharts' <c>compact</c>).</summary>
    public bool Compact { get; init; }

    /// <summary>A responsive chart's container <c>id</c> on the web (Recharts' <c>ResponsiveContainer</c> <c>id</c>).</summary>
    public string? ContainerId { get; init; }

    /// <summary>A responsive chart's container class on the web, after Pictor's own (<c>ResponsiveContainer</c> <c>className</c>).</summary>
    public string? ContainerClass { get; init; }

    /// <summary>A responsive chart's container CSS declarations on the web, before the sizes it computes (<c>ResponsiveContainer</c> <c>style</c>).</summary>
    public string? ContainerStyle { get; init; }

    /// <summary>
    /// Recharts' <c>accessibilityLayer</c>: the surface takes focus (<c>tabindex="0"</c>, <c>role="application"</c>), focus
    /// shows the tooltip of the active category (the default index, or the first), and the arrow keys move it along a
    /// horizontal chart's categories; on the web a horizontal chart's categories are a roving focus group (the framework's
    /// <c>data-aioros-roving</c>) whose active category alone is in the tab order, in place of the surface.
    /// </summary>
    public bool AccessibilityLayer { get; init; }

    /// <summary>
    /// The widths a responsive chart is laid out at on the web, each shown by a container query over the widths nearest
    /// it; empty to lay it out once at <see cref="Width"/> and scale it.
    /// </summary>
    public ImmutableArray<double> ResponsiveWidths { get; init; } = [];

    /// <summary>A polar chart's center, radii, grid, axes, pies, radars and radial bars.</summary>
    public PolarSpec? Polar { get; init; }
    /// <summary>The funnels of a funnel chart.</summary>
    public ImmutableArray<FunnelSpec> Funnels { get; init; } = [];

    /// <summary>A treemap's own props.</summary>
    public TreemapSpec? Treemap { get; init; }

    /// <summary>A Sankey's own props.</summary>
    public SankeySpec? Sankey { get; init; }

    /// <summary>A sunburst's own props.</summary>
    public SunburstSpec? Sunburst { get; init; }

    /// <summary>Whether categories run along the x-axis (<c>horizontal</c>) or down the y-axis (<c>vertical</c>).</summary>
    public ChartLayoutKind Layout { get; init; }

    /// <summary>The z-axes of a scatter chart.</summary>
    public ImmutableArray<ZAxisSpec> ZAxes { get; init; } = [];

    /// <summary>The reference areas.</summary>
    public ImmutableArray<ReferenceAreaSpec> ReferenceAreas { get; init; } = [];

    /// <summary>The reference dots.</summary>
    public ImmutableArray<ReferenceDotSpec> ReferenceDots { get; init; } = [];

    /// <summary>The gradients the chart's paints name as <c>url(#id)</c> (Recharts' <c>linearGradient</c> in the chart's <c>defs</c>).</summary>
    public ImmutableArray<GradientSpec> Gradients { get; init; } = [];

    /// <summary>The chart's <c>Customized</c> children: custom content drawn with its layout.</summary>
    public ImmutableArray<CustomizedSpec> Customized { get; init; } = [];

    /// <summary>
    /// The content Avalonia laid out for each item of a template the compiler refused, by the painter's call, when the page
    /// is rendered by Avalonia (the engine path): the web writes it as it is.
    /// </summary>
    public ImmutableArray<ResolvedContent> Resolved { get; init; } = [];

    /// <summary>The brush, if any.</summary>
    public BrushSpec? Brush { get; init; }

    /// <summary>
    /// The legend entries toggled off on the desktop (series, or items for a pie), drawn hidden without laying the chart
    /// out again; the web toggles them in the browser, so its specs never carry any.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public ImmutableArray<int> Concealed { get; init; } = [];

    /// <summary>The key of the command a click on the chart runs with the active category's or item's index (Recharts' chart <c>onClick</c>), or none.</summary>
    public string? ClickCommand { get; init; }

    /// <summary>The key of the command a click on any graphical item runs with the item's index (or a node's path), when its series sets none, or none.</summary>
    public string? ItemClickCommand { get; init; }

    /// <summary>The charts sharing this id show the same active category (Recharts' <c>syncId</c>), or none.</summary>
    public string? SyncId { get; init; }

    /// <summary>How a synced chart finds its active category: by the same index (<c>index</c>, Recharts' default) or the same category value (<c>value</c>).</summary>
    public string? SyncMethod { get; init; }

    /// <summary>The theme's defaults for what the chart's elements leave unset; Recharts' (<see cref="ChartTheme.Light"/>) when null.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public ChartTheme? Theme { get; init; }

    /// <summary>The theme the chart is drawn with: <see cref="Theme"/>, or Recharts' defaults.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public ChartTheme Colors => Theme ?? ChartTheme.Light;

    /// <summary>Whether stacks, bar groups, the legend and the tooltip take the series in reverse order (Recharts' <c>reverseStackOrder</c>); they are still drawn in declaration order.</summary>
    public bool ReverseStackOrder { get; init; }
}

/// <summary>Recharts' <c>stackOffset</c>.</summary>
public enum StackOffsetKind
{
    /// <summary>Stacked from zero (<c>none</c>).</summary>
    None,

    /// <summary>Normalized to the range 0 to 1 (<c>expand</c>).</summary>
    Expand,

    /// <summary>Positive values stacked above zero, negative ones below (<c>sign</c>).</summary>
    Sign,

    /// <summary>Centered around zero (<c>silhouette</c>).</summary>
    Silhouette,

    /// <summary>Minimizing weighted wiggle (<c>wiggle</c>).</summary>
    Wiggle,

    /// <summary>Positive values stacked above zero, negative ones below, each separately (<c>positive</c>).</summary>
    Positive,
}
