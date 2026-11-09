using System.Collections.Immutable;

namespace Aioros.Pictor.Specs;

/// <summary>
/// What Recharts' polar charts (<c>PieChart</c>, <c>RadarChart</c>, <c>RadialBarChart</c>) add to a chart: the center and
/// radii of the polar coordinate system, the angle range, the polar grid and axes, and the pies, radars and radial bars.
/// </summary>
public sealed record PolarSpec
{
    /// <summary>The center's x: a number of pixels or a percentage of the chart's width.</summary>
    public string Cx { get; init; } = "50%";

    /// <summary>The center's y: a number of pixels or a percentage of the chart's height.</summary>
    public string Cy { get; init; } = "50%";

    /// <summary>The inner radius: pixels or a percentage of the largest radius the plot allows.</summary>
    public string InnerRadius { get; init; } = "0";

    /// <summary>The outer radius: pixels or a percentage of the largest radius the plot allows.</summary>
    public string OuterRadius { get; init; } = "80%";

    /// <summary>The angle the angle axis starts at, in degrees counterclockwise from 3 o'clock.</summary>
    public double StartAngle { get; init; }

    /// <summary>The angle the angle axis ends at.</summary>
    public double EndAngle { get; init; } = 360;

    /// <summary>Whether a radial bar chart lays its categories around the angle axis and its values along the radius (Recharts' <c>layout="centric"</c>); radial by default.</summary>
    public bool Centric { get; init; }

    /// <summary>The polar grid, if any.</summary>
    public PolarGridSpec? Grid { get; init; }

    /// <summary>The angle axes after <see cref="AngleAxis"/> (the first declared), each by its id (Recharts' <c>angleAxisId</c>).</summary>
    public ImmutableArray<PolarAngleAxisSpec> MoreAngleAxes { get; init; } = [];

    /// <summary>The radius axes after <see cref="RadiusAxis"/> (the first declared), each by its id (Recharts' <c>radiusAxisId</c>).</summary>
    public ImmutableArray<PolarRadiusAxisSpec> MoreRadiusAxes { get; init; } = [];

    /// <summary>The angle axis, if declared.</summary>
    public PolarAngleAxisSpec? AngleAxis { get; init; }

    /// <summary>The radius axis, if declared.</summary>
    public PolarRadiusAxisSpec? RadiusAxis { get; init; }

    /// <summary>The pies of a pie chart.</summary>
    public ImmutableArray<PieSpec> Pies { get; init; } = [];

    /// <summary>The radars of a radar chart.</summary>
    public ImmutableArray<RadarSpec> Radars { get; init; } = [];

    /// <summary>The bars of a radial bar chart.</summary>
    public ImmutableArray<RadialBarSpec> RadialBars { get; init; } = [];
}

/// <summary>Recharts' <c>PolarGrid</c>.</summary>
public sealed record PolarGridSpec
{
    /// <summary>Whether the concentric lines are circles (<c>circle</c>) rather than polygons (<c>polygon</c>).</summary>
    public bool Circle { get; init; }

    /// <summary>Whether the radial lines are drawn.</summary>
    public bool RadialLines { get; init; } = true;

    /// <summary>The lines' color.</summary>
    public string? Stroke { get; init; }

    /// <summary>The lines' dash pattern.</summary>
    public string? StrokeDashArray { get; init; }

    /// <summary>The lines' width.</summary>
    public double? StrokeWidth { get; init; }

    /// <summary>The lines' opacity.</summary>
    public double? StrokeOpacity { get; init; }

    /// <summary>The fill Recharts spreads onto the radial lines (the concentric lines are never filled).</summary>
    public string? Fill { get; init; }

    /// <summary>The angles of the radial lines, instead of the angle axis' ticks (Recharts' <c>polarAngles</c>).</summary>
    public ImmutableArray<double> PolarAngles { get; init; } = [];

    /// <summary>The radii of the concentric lines, instead of the radius axis' ticks (Recharts' <c>polarRadius</c>).</summary>
    public ImmutableArray<double> PolarRadius { get; init; } = [];

    /// <summary>The order of the grid among the chart's children.</summary>
    public int Order { get; init; }
}

/// <summary>Recharts' <c>PolarAngleAxis</c>.</summary>
public sealed record PolarAngleAxisSpec
{
    /// <summary>The custom content drawn in place of each tick's label (Recharts' <c>tick</c> element), compiled from its XAML template.</summary>
    public ContentSpec? TickContent { get; init; }

    /// <summary>The axis' id, which radars and radial bars name (Recharts' <c>angleAxisId</c>).</summary>
    public string Id { get; init; } = "0";

    /// <summary>The data key of the axis' categories; the items' indexes when null.</summary>
    public string? DataKey { get; init; }

    /// <summary>Whether the axis line is a circle (<c>circle</c>) rather than a polygon through the ticks.</summary>
    public bool CircleLine { get; init; }

    /// <summary>Whether the axis line is drawn.</summary>
    public bool AxisLine { get; init; } = true;

    /// <summary>Whether the tick lines are drawn.</summary>
    public bool TickLine { get; init; } = true;

    /// <summary>Whether the tick labels are drawn.</summary>
    public bool Tick { get; init; } = true;

    /// <summary>The length of a tick line.</summary>
    public double TickSize { get; init; } = 8;

    /// <summary>Whether the ticks point inwards (<c>inner</c>) rather than outwards.</summary>
    public bool Inner { get; init; }

    /// <summary>The axis' color; Recharts gives none, so its line and tick lines are not stroked.</summary>
    public string? Stroke { get; init; }

    /// <summary>A .NET composite format for the tick labels.</summary>
    public string? TickFormat { get; init; }

    /// <summary>Recharts' <c>tickFormatter</c> as a function: a tick's text from its value and its index, over <see cref="TickFormat"/>; null keeps the format's text. Not written to JSON: <see cref="TickTexts"/> records what it wrote.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Func<object?, int, string?>? TickFormatter { get; init; }

    /// <summary>What <see cref="TickFormatter"/> wrote when the page was rendered, by <see cref="Text.FormatKeys.Tick"/>, for the web writer.</summary>
    public ImmutableDictionary<string, string>? TickTexts { get; init; }

    /// <summary>How the web's writer rebuilds <see cref="TickFormatter"/>'s converter (Aioros's plans file reference), run through the app's generated converters.</summary>
    public string? TickConverter { get; init; }

    /// <summary>Whether the axis is hidden.</summary>
    public bool Hide { get; init; }

    /// <summary>The order of the axis among the chart's children.</summary>
    public int Order { get; init; }

    /// <summary>Whether the axis maps numbers (a radial bar chart's values) or categories; null for Recharts' default, the categories of a radar and the values of radial bars.</summary>
    public AxisType? Type { get; init; }

    /// <summary>The domain's lower bound of a numeric axis, as a cartesian axis'.</summary>
    public string? DomainMin { get; init; }

    /// <summary>The domain's upper bound of a numeric axis, as a cartesian axis'.</summary>
    public string? DomainMax { get; init; }

    /// <summary>Whether a specified domain may cut the data.</summary>
    public bool AllowDataOverflow { get; init; }

    /// <summary>How many ticks a numeric axis aims for; null for d3's ten ticks over the domain (Recharts' angle axis has no default).</summary>
    public int? TickCount { get; init; }

    /// <summary>Whether a numeric axis' ticks may be fractional.</summary>
    public bool AllowDecimals { get; init; } = true;

    /// <summary>The tick lines' own SVG props (the <c>tickLine</c> object).</summary>
    public LineStyle? TickLineStyle { get; init; }

    /// <summary>The scale of a numeric axis: <c>auto</c>, <c>linear</c>, <c>log</c>, <c>pow</c>, <c>sqrt</c>, <c>time</c> or <c>utc</c>; null for <c>auto</c>.</summary>
    public string? Scale { get; init; }

    /// <summary>Whether the axis' range runs backwards.</summary>
    public bool Reversed { get; init; }

    /// <summary>The ticks the axis shows instead of its computed ones, as text: numbers on a numeric axis, categories on a category one.</summary>
    public ImmutableArray<string> Ticks { get; init; } = [];

    /// <summary>Whether a category that appears twice gets two ticks (Recharts' default) or one.</summary>
    public bool AllowDuplicatedCategory { get; init; } = true;

    /// <summary>The color of the tick labels (the <c>tick</c> object's <c>fill</c>).</summary>
    public string? TickFill { get; init; }

    /// <summary>The font size of the tick labels (the <c>tick</c> object's <c>fontSize</c>).</summary>
    public double? TickFontSize { get; init; }

    /// <summary>The axis line's own SVG props (the <c>axisLine</c> object).</summary>
    public LineStyle? AxisLineStyle { get; init; }
}

/// <summary>Recharts' <c>PolarRadiusAxis</c>.</summary>
public sealed record PolarRadiusAxisSpec
{
    /// <summary>The custom content drawn in place of each tick's label (Recharts' <c>tick</c> element), compiled from its XAML template.</summary>
    public ContentSpec? TickContent { get; init; }

    /// <summary>The axis' id, which radars and radial bars name (Recharts' <c>radiusAxisId</c>).</summary>
    public string Id { get; init; } = "0";

    /// <summary>The angle the axis is drawn at, in degrees.</summary>
    public double Angle { get; init; }

    /// <summary>The data key of a categorical radius axis' categories (a radial bar chart's).</summary>
    public string? DataKey { get; init; }

    /// <summary>Whether a radial bar chart's radius axis is of type <c>category</c> (its bands start at their ticks).</summary>
    public bool Category { get; init; }

    /// <summary>The tick labels' anchor side: <c>left</c>, <c>right</c> or <c>middle</c>.</summary>
    public string Orientation { get; init; } = "right";

    /// <summary>The axis' color.</summary>
    public string? Stroke { get; init; }

    /// <summary>Whether the axis line is drawn.</summary>
    public bool AxisLine { get; init; } = true;

    /// <summary>Whether the tick labels are drawn.</summary>
    public bool Tick { get; init; } = true;

    /// <summary>How many ticks the axis aims for.</summary>
    public int TickCount { get; init; } = 5;

    /// <summary>Whether the ticks may be fractional.</summary>
    public bool AllowDecimals { get; init; } = true;

    /// <summary>The domain's lower bound (a number, <c>auto</c>, <c>dataMin</c>); null for Recharts' 0.</summary>
    public string? DomainMin { get; init; }

    /// <summary>The domain's upper bound (a number, <c>auto</c>, <c>dataMax</c>); null for Recharts' <c>auto</c>.</summary>
    public string? DomainMax { get; init; }

    /// <summary>Whether a specified domain may cut the data.</summary>
    public bool AllowDataOverflow { get; init; }

    /// <summary>A .NET composite format for the tick labels.</summary>
    public string? TickFormat { get; init; }

    /// <summary>Recharts' <c>tickFormatter</c> as a function: a tick's text from its value and its index, over <see cref="TickFormat"/>; null keeps the format's text. Not written to JSON: <see cref="TickTexts"/> records what it wrote.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Func<object?, int, string?>? TickFormatter { get; init; }

    /// <summary>What <see cref="TickFormatter"/> wrote when the page was rendered, by <see cref="Text.FormatKeys.Tick"/>, for the web writer.</summary>
    public ImmutableDictionary<string, string>? TickTexts { get; init; }

    /// <summary>How the web's writer rebuilds <see cref="TickFormatter"/>'s converter (Aioros's plans file reference), run through the app's generated converters.</summary>
    public string? TickConverter { get; init; }

    /// <summary>The axis' label, if any.</summary>
    public LabelSpec? Label { get; init; }

    /// <summary>The <c>Label</c> children, drawn after <see cref="Label"/>.</summary>
    public ImmutableArray<LabelSpec> Labels { get; init; } = [];

    /// <summary>Whether the axis is hidden (it still maps the values).</summary>
    public bool Hide { get; init; }

    /// <summary>The order of the axis among the chart's children.</summary>
    public int Order { get; init; }

    /// <summary>The scale of a numeric axis: <c>auto</c>, <c>linear</c>, <c>log</c>, <c>pow</c>, <c>sqrt</c>, <c>time</c> or <c>utc</c>; null for <c>auto</c>.</summary>
    public string? Scale { get; init; }

    /// <summary>Whether the axis' range runs backwards.</summary>
    public bool Reversed { get; init; }

    /// <summary>The ticks the axis shows instead of its computed ones, as text: numbers on a numeric axis, categories on a category one.</summary>
    public ImmutableArray<string> Ticks { get; init; } = [];

    /// <summary>Whether a category that appears twice gets two ticks (Recharts' default) or one.</summary>
    public bool AllowDuplicatedCategory { get; init; } = true;

    /// <summary>The color of the tick labels (the <c>tick</c> object's <c>fill</c>).</summary>
    public string? TickFill { get; init; }

    /// <summary>The font size of the tick labels (the <c>tick</c> object's <c>fontSize</c>).</summary>
    public double? TickFontSize { get; init; }

    /// <summary>The axis line's own SVG props (the <c>axisLine</c> object).</summary>
    public LineStyle? AxisLineStyle { get; init; }
}

/// <summary>Props Recharts merges over a sector's own when it is active (an <c>activeShape</c> given as an object).</summary>
public sealed record SectorShapeSpec
{
    /// <summary>The fill, or null to keep the sector's.</summary>
    public string? Fill { get; init; }

    /// <summary>The stroke, or null to keep the sector's.</summary>
    public string? Stroke { get; init; }

    /// <summary>The inner radius in pixels, or null to keep the sector's.</summary>
    public double? InnerRadius { get; init; }

    /// <summary>The outer radius in pixels, or null to keep the sector's.</summary>
    public double? OuterRadius { get; init; }

    /// <summary>The corner radius, or null to keep the sector's.</summary>
    public string? CornerRadius { get; init; }

    /// <summary>The fill opacity, or null to keep the sector's.</summary>
    public double? FillOpacity { get; init; }

    /// <summary>The opacity, or null.</summary>
    public double? Opacity { get; init; }

    /// <summary>The stroke width, or null to keep the sector's.</summary>
    public double? StrokeWidth { get; init; }
}

/// <summary>Recharts' <c>Pie</c>.</summary>
public sealed record PieSpec
{
    /// <summary>The pie layer's tab index on the web (Recharts' <c>rootTabIndex</c>, 0: a tab stop), or null for none.</summary>
    public int? RootTabIndex { get; init; } = 0;

    /// <summary>The element's own classes after Recharts' (its <c>className</c>), or none.</summary>
    public string? ClassName { get; init; }

    /// <summary>The key of the command its items run when clicked (Recharts' <c>onClick</c>), written as buttons on the web; none when no command is set.</summary>
    public string? ClickCommand { get; init; }

    /// <summary>The data key of the sectors' values (Recharts' <c>value</c> when empty).</summary>
    public string DataKey { get; init; } = "value";

    /// <summary>The data key of the sectors' names.</summary>
    public string NameKey { get; init; } = "name";

    /// <summary>The data key of each item's own fill, as Recharts spreads an item's <c>fill</c> onto its sector; none when null.</summary>
    public string? FillKey { get; init; }

    /// <summary>The data key of each item's own opacity, as Recharts spreads an item's <c>opacity</c> onto its sector; none when null.</summary>
    public string? OpacityKey { get; init; }

    /// <summary>The pie's own items; the chart's data when null.</summary>
    public DataTable? Data { get; init; }

    /// <summary>The center's x: pixels from the plot's left, or a percentage of the plot's width.</summary>
    public string Cx { get; init; } = "50%";

    /// <summary>The center's y: pixels from the plot's top, or a percentage of the plot's height.</summary>
    public string Cy { get; init; } = "50%";

    /// <summary>The inner radius: pixels or a percentage of the largest radius.</summary>
    public string InnerRadius { get; init; } = "0";

    /// <summary>The outer radius: pixels or a percentage of the largest radius.</summary>
    public string OuterRadius { get; init; } = "80%";

    /// <summary>The angle the first sector starts at.</summary>
    public double StartAngle { get; init; }

    /// <summary>The angle the last sector ends at.</summary>
    public double EndAngle { get; init; } = 360;

    /// <summary>The angle between two sectors.</summary>
    public double PaddingAngle { get; init; }

    /// <summary>The smallest angle a non-zero sector takes.</summary>
    public double MinAngle { get; init; }

    /// <summary>The corner radius: pixels or a percentage of the ring's width.</summary>
    public string? CornerRadius { get; init; }

    /// <summary>The sectors' fill.</summary>
    public string? Fill { get; init; }

    /// <summary>The sectors' stroke.</summary>
    public string? Stroke { get; init; }

    /// <summary>Whether each sector shows its value outside it (Recharts' <c>label</c>).</summary>
    public bool Label { get; init; }

    /// <summary>The distance from the outer radius to a label (Recharts' <c>label.offsetRadius</c>).</summary>
    public double LabelOffsetRadius { get; init; } = 20;

    /// <summary>Whether a line joins each label to its sector.</summary>
    public bool LabelLine { get; init; } = true;

    /// <summary>The pie's label lists (Recharts' <c>LabelList</c> children), drawn after its labels.</summary>
    public ImmutableArray<LabelSpec> LabelLists { get; init; } = [];

    /// <summary>The fills and strokes of individual sectors (Recharts' <c>Cell</c>), by index.</summary>
    public ImmutableArray<CellSpec> Cells { get; init; } = [];

    /// <summary>The sector drawn active whatever the pointer does (Recharts' <c>activeIndex</c>).</summary>
    public int? ActiveIndex { get; init; }

    /// <summary>What an active sector looks like (Recharts' <c>activeShape</c> object); hovering a sector makes it active.</summary>
    public SectorShapeSpec? ActiveShape { get; init; }

    /// <summary>The sectors drawn active whatever the pointer does, when <c>activeIndex</c> is an array.</summary>
    public ImmutableArray<int> ActiveIndexes { get; init; } = [];

    /// <summary>What the other sectors look like while some are active (Recharts' <c>inactiveShape</c> object).</summary>
    public SectorShapeSpec? InactiveShape { get; init; }

    /// <summary>Whether each sector's stroke is its fill (Recharts' <c>blendStroke</c>).</summary>
    public bool BlendStroke { get; init; }

    /// <summary>The labels' own props (Recharts' <c>label</c> object: fill, size, weight, stroke), when the pie is labelled.</summary>
    public LabelSpec? LabelProps { get; init; }

    /// <summary>The label lines' own stroke (Recharts' <c>labelLine</c> object); the sector's fill when null.</summary>
    public string? LabelLineStroke { get; init; }

    /// <summary>The label lines' stroke width.</summary>
    public double? LabelLineStrokeWidth { get; init; }

    /// <summary>The label lines' dash pattern.</summary>
    public string? LabelLineStrokeDashArray { get; init; }

    /// <summary>The legend icon of the sectors.</summary>
    public LegendIcon LegendType { get; init; } = LegendIcon.Rect;

    /// <summary>Whether the pie is hidden.</summary>
    public bool Hide { get; init; }

    /// <summary>Whether the tooltip leaves its entries out (Recharts' <c>tooltipType="none"</c>).</summary>
    public bool TooltipNone { get; init; }

    /// <summary>The order of the pie among the chart's children.</summary>
    public int Order { get; init; }

    /// <summary>The enter animation; null for Recharts' defaults of the kind.</summary>
    public AnimationSpec? Animation { get; init; }

    /// <summary>The custom content drawn in place of each sector (Recharts' <c>shape</c> element), compiled from its XAML template.</summary>
    public ContentSpec? ShapeContent { get; init; }

    /// <summary>The custom content drawn in place of an active sector (Recharts' <c>activeShape</c> element or function).</summary>
    public ContentSpec? ActiveShapeContent { get; init; }
}

/// <summary>Recharts' <c>Radar</c>.</summary>
public sealed record RadarSpec
{
    /// <summary>The custom content drawn in place of the radar's polygon (Recharts' <c>shape</c> element), compiled from its XAML template.</summary>
    public ContentSpec? ShapeContent { get; init; }

    /// <summary>The angle axis it maps through (Recharts' <c>angleAxisId</c>).</summary>
    public string AngleAxisId { get; init; } = "0";

    /// <summary>The radius axis it maps through (Recharts' <c>radiusAxisId</c>).</summary>
    public string RadiusAxisId { get; init; } = "0";
    /// <summary>The key of the command its items run when clicked (Recharts' <c>onClick</c>), written as buttons on the web; none when no command is set.</summary>
    public string? ClickCommand { get; init; }

    /// <summary>The data key of the radar's values.</summary>
    public string DataKey { get; init; } = string.Empty;

    /// <summary>The name in the legend and the tooltip; the data key when null.</summary>
    public string? Name { get; init; }

    /// <summary>The polygon's stroke; none when null.</summary>
    public string? Stroke { get; init; }

    /// <summary>The polygon's fill; SVG's black when null, as in Recharts.</summary>
    public string? Fill { get; init; }

    /// <summary>The fill's opacity.</summary>
    public double? FillOpacity { get; init; }

    /// <summary>The stroke's width.</summary>
    public double? StrokeWidth { get; init; }

    /// <summary>Whether a dot marks every point.</summary>
    public bool Dot { get; init; }

    /// <summary>Whether the hovered category shows an active dot.</summary>
    public bool ActiveDot { get; init; } = true;

    /// <summary>The active dot's radius.</summary>
    public double ActiveDotRadius { get; init; } = 4;

    /// <summary>The dots' own props (Recharts' <c>dot</c> given as an object).</summary>
    public ActiveStyle? DotProps { get; init; }

    /// <summary>How the active dot is drawn beyond its radius (Recharts' <c>activeDot</c> props).</summary>
    public ActiveStyle? ActiveDotStyle { get; init; }

    /// <summary>Whether the polygon runs through the points around missing values (Recharts' <c>connectNulls</c>).</summary>
    public bool ConnectNulls { get; init; }

    /// <summary>The labels of the points (Recharts' <c>label</c> or <c>LabelList</c>), if any.</summary>
    public LabelSpec? Labels { get; init; }

    /// <summary>The radar's further label lists, drawn after <see cref="Labels"/>.</summary>
    public ImmutableArray<LabelSpec> LabelLists { get; init; } = [];

    /// <summary>A unit written after the values in the tooltip.</summary>
    public string? Unit { get; init; }

    /// <summary>The legend icon.</summary>
    public LegendIcon LegendType { get; init; } = LegendIcon.Rect;

    /// <summary>Whether the radar is hidden (it keeps its legend entry).</summary>
    public bool Hide { get; init; }

    /// <summary>Whether the tooltip leaves its entries out (Recharts' <c>tooltipType="none"</c>).</summary>
    public bool TooltipNone { get; init; }

    /// <summary>The order of the radar among the chart's children.</summary>
    public int Order { get; init; }

    /// <summary>The enter animation; null for Recharts' defaults of the kind.</summary>
    public AnimationSpec? Animation { get; init; }
}

/// <summary>Recharts' <c>RadialBar</c>.</summary>
public sealed record RadialBarSpec
{
    /// <summary>The custom content drawn in place of each bar (Recharts' <c>shape</c> element), compiled from its XAML template.</summary>
    public ContentSpec? ShapeContent { get; init; }

    /// <summary>The custom content drawn in place of the hovered bar (Recharts' <c>activeShape</c> element), compiled from its XAML template.</summary>
    public ContentSpec? ActiveShapeContent { get; init; }

    /// <summary>Recharts 2's <c>activeIndex</c>, which Recharts 3's <c>RadialBar</c> ignores and Pictor with it: the hovered ring is the active one.</summary>
    public int? ActiveIndex { get; init; }

    /// <summary>The angle axis it maps through (Recharts' <c>angleAxisId</c>).</summary>
    public string AngleAxisId { get; init; } = "0";

    /// <summary>The radius axis it maps through (Recharts' <c>radiusAxisId</c>).</summary>
    public string RadiusAxisId { get; init; } = "0";
    /// <summary>The key of the command its items run when clicked (Recharts' <c>onClick</c>), written as buttons on the web; none when no command is set.</summary>
    public string? ClickCommand { get; init; }

    /// <summary>The data key of the bars' values.</summary>
    public string DataKey { get; init; } = string.Empty;

    /// <summary>The name in the tooltip; the data key when null.</summary>
    public string? Name { get; init; }

    /// <summary>The data key of each bar's name in the legend (Recharts reads the items' <c>name</c>).</summary>
    public string NameKey { get; init; } = "name";

    /// <summary>The data key of each item's own fill, as Recharts spreads an item's <c>fill</c> onto its bar; none when null.</summary>
    public string? FillKey { get; init; }

    /// <summary>The data key of each bar's own opacity, as Recharts spreads an item's <c>opacity</c> onto its bar; none when null.</summary>
    public string? OpacityKey { get; init; }

    /// <summary>The bars' fill; none when null (SVG's black), as in Recharts.</summary>
    public string? Fill { get; init; }

    /// <summary>The bars' stroke.</summary>
    public string? Stroke { get; init; }

    /// <summary>The background's fill (Recharts' <c>background</c>, <c>#eee</c> when true); no background when null.</summary>
    public string? Background { get; init; }

    /// <summary>The bars' width across the radius.</summary>
    public double? BarSize { get; init; }

    /// <summary>The bars' largest width.</summary>
    public double? MaxBarSize { get; init; }

    /// <summary>The stack the bars belong to (Recharts' <c>stackId</c>): bars of one stack share a band and pile up along the angle.</summary>
    public string? StackId { get; init; }

    /// <summary>Whether a bar too narrow for its corners is drawn as a rounded cap anyway (Recharts' <c>forceCornerRadius</c>).</summary>
    public bool ForceCornerRadius { get; init; }

    /// <summary>Whether the bars' corners round outwards (Recharts' <c>cornerIsExternal</c>).</summary>
    public bool CornerIsExternal { get; init; }

    /// <summary>The background's other props (Recharts' <c>background</c> object: stroke, stroke width, opacities), its fill being <see cref="Background"/>.</summary>
    public CellSpec? BackgroundProps { get; init; }

    /// <summary>The smallest angle a bar is drawn with (Recharts' <c>minPointSize</c>).</summary>
    public double MinPointSize { get; init; }

    /// <summary>The corner radius of the bars and their backgrounds.</summary>
    public string? CornerRadius { get; init; }

    /// <summary>The bars' labels (Recharts' <c>label</c>), if any.</summary>
    public LabelSpec? Labels { get; init; }

    /// <summary>The bars' further label lists (Recharts' <c>LabelList</c> children), drawn after <see cref="Labels"/>.</summary>
    public ImmutableArray<LabelSpec> LabelLists { get; init; } = [];

    /// <summary>The fills of individual bars (Recharts' <c>Cell</c>), by index.</summary>
    public ImmutableArray<CellSpec> Cells { get; init; } = [];

    /// <summary>A unit written after the values in the tooltip.</summary>
    public string? Unit { get; init; }

    /// <summary>The legend icon of the bars.</summary>
    public LegendIcon LegendType { get; init; } = LegendIcon.Rect;

    /// <summary>Whether the bars are hidden.</summary>
    public bool Hide { get; init; }

    /// <summary>Whether the tooltip leaves its entries out (Recharts' <c>tooltipType="none"</c>).</summary>
    public bool TooltipNone { get; init; }

    /// <summary>The order of the bars among the chart's children.</summary>
    public int Order { get; init; }

    /// <summary>The enter animation; null for Recharts' defaults of the kind.</summary>
    public AnimationSpec? Animation { get; init; }
}
