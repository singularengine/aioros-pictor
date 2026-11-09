using System.Collections.Immutable;

namespace Aioros.Pictor.Specs;

/// <summary>Where an axis sits, Recharts' <c>orientation</c>.</summary>
public enum AxisOrientation
{
    /// <summary>Below the plot (an x-axis' default).</summary>
    Bottom,

    /// <summary>Above the plot.</summary>
    Top,

    /// <summary>Left of the plot (a y-axis' default).</summary>
    Left,

    /// <summary>Right of the plot.</summary>
    Right,
}

/// <summary>Whether an axis maps categories or numbers, Recharts' <c>type</c>.</summary>
public enum AxisType
{
    /// <summary>Categories: one tick per distinct value.</summary>
    Category,

    /// <summary>Numbers: a linear scale with nice ticks.</summary>
    Number,
}

/// <summary>Which ticks an axis keeps when they would overlap, Recharts' <c>interval</c>.</summary>
public enum TickInterval
{
    /// <summary>Keep the last tick and drop the ones that collide before it.</summary>
    PreserveEnd,

    /// <summary>Keep the first tick and drop the ones that collide after it.</summary>
    PreserveStart,

    /// <summary>Keep the first and the last ticks.</summary>
    PreserveStartEnd,

    /// <summary>Keep every tick (a number interval of 0); <see cref="AxisSpec.IntervalCount"/> skips some.</summary>
    Fixed,

    /// <summary>Keep the first tick and every n-th after it, with the smallest n that lets none collide (<c>equidistantPreserveStart</c>).</summary>
    EquidistantPreserveStart,
}

/// <summary>How a text is anchored at its point, SVG's <c>text-anchor</c>.</summary>
public enum TextAnchorKind
{
    /// <summary>The text starts at its point (<c>start</c>).</summary>
    Start,

    /// <summary>The text is centered on its point (<c>middle</c>).</summary>
    Middle,

    /// <summary>The text ends at its point (<c>end</c>).</summary>
    End,
}

/// <summary>Recharts' <c>padding</c> of a numeric axis given as a word: half the smallest gap between two values at each end.</summary>
public enum AxisPaddingKind
{
    /// <summary>The padding is <see cref="AxisSpec.PaddingStart"/> and <see cref="AxisSpec.PaddingEnd"/>.</summary>
    Fixed,

    /// <summary>Half the smallest distance between two values at each end (<c>gap</c>).</summary>
    Gap,

    /// <summary>The same half distance less the chart's bar category gap (<c>no-gap</c>).</summary>
    NoGap,
}

/// <summary>The SVG props of a line Recharts draws, its <c>axisLine</c> or <c>tickLine</c> object; a null prop keeps the axis'.</summary>
/// <param name="Stroke">The line's color.</param>
/// <param name="StrokeWidth">The line's width.</param>
/// <param name="StrokeDashArray">The line's dash pattern.</param>
public sealed record LineStyle(string? Stroke = null, double? StrokeWidth = null, string? StrokeDashArray = null);

/// <summary>An <c>XAxis</c> or a <c>YAxis</c>.</summary>
public sealed record AxisSpec
{
    /// <summary>The custom content drawn in place of each tick's label (Recharts' <c>tick</c> element), compiled from its XAML template.</summary>
    public ContentSpec? TickContent { get; init; }

    /// <summary>The key of the command its ticks run when clicked (Recharts' <c>onClick</c> on the axis' ticks), written as buttons on the web; none when no command is set.</summary>
    public string? ClickCommand { get; init; }
    /// <summary>The element's own classes after Recharts' (its <c>className</c>), or none.</summary>
    public string? ClassName { get; init; }

    /// <summary>SVG presentation attributes Recharts spreads onto the axis line, the tick lines and the tick texts, or none.</summary>
    public SvgPresentation? Presentation { get; init; }

    /// <summary>The axis id series refer to.</summary>
    public string Id { get; init; } = "0";

    /// <summary>The data key of the axis' values (categories of an x-axis, numbers of a numeric one).</summary>
    public string? DataKey { get; init; }

    /// <summary>The axis' type; x-axes default to categories and y-axes to numbers.</summary>
    public AxisType Type { get; init; }

    /// <summary>The axis' orientation.</summary>
    public AxisOrientation Orientation { get; init; }

    /// <summary>The space the axis takes across the plot: an x-axis' height (30) or a y-axis' width (60).</summary>
    public double Size { get; init; }

    /// <summary>The scale of a category axis, <c>band</c> or <c>point</c>; null picks Recharts' (points in line and area charts, bands with bars).</summary>
    public string? Scale { get; init; }

    /// <summary>Whether the axis is hidden; it still maps its series.</summary>
    public bool Hide { get; init; }

    /// <summary>How many ticks a numeric axis aims for.</summary>
    public int TickCount { get; init; } = 5;

    /// <summary>Whether a numeric axis' ticks may be fractional.</summary>
    public bool AllowDecimals { get; init; } = true;

    /// <summary>The domain's lower bound: a number, <c>auto</c>, <c>dataMin</c> or <c>dataMin - n</c>; null for Recharts' default.</summary>
    public string? DomainMin { get; init; }

    /// <summary>The domain's upper bound: a number, <c>auto</c>, <c>dataMax</c> or <c>dataMax + n</c>; null for Recharts' default.</summary>
    public string? DomainMax { get; init; }

    /// <summary>Whether a specified domain may cut the data.</summary>
    public bool AllowDataOverflow { get; init; }

    /// <summary>The padding at the start of the range (left or bottom).</summary>
    public double PaddingStart { get; init; }

    /// <summary>The padding at the end of the range (right or top).</summary>
    public double PaddingEnd { get; init; }

    /// <summary>Whether the range runs backwards.</summary>
    public bool Reversed { get; init; }

    /// <summary>Whether the axis is drawn inside the plot.</summary>
    public bool Mirror { get; init; }

    /// <summary>Which ticks are kept when they collide.</summary>
    public TickInterval Interval { get; init; }

    /// <summary>With <see cref="TickInterval.Fixed"/>, how many ticks to skip between two shown ones.</summary>
    public int IntervalCount { get; init; }

    /// <summary>The smallest gap between two tick labels.</summary>
    public double MinTickGap { get; init; } = 5;

    /// <summary>The length of a tick line.</summary>
    public double TickSize { get; init; } = 6;

    /// <summary>The gap between a tick line and its label.</summary>
    public double TickMargin { get; init; } = 2;

    /// <summary>Whether tick lines are drawn.</summary>
    public bool TickLine { get; init; } = true;

    /// <summary>Whether the axis line is drawn.</summary>
    public bool AxisLine { get; init; } = true;

    /// <summary>Whether tick labels are drawn.</summary>
    public bool Tick { get; init; } = true;

    /// <summary>The axis' color: its line's, its tick lines' and its labels'; the theme's when null.</summary>
    public string? Stroke { get; init; }

    /// <summary>A .NET composite format for tick labels (<c>{0:N0}</c>, <c>{0}k</c>); null writes the value as Recharts does.</summary>
    public string? TickFormat { get; init; }

    /// <summary>Recharts' <c>tickFormatter</c> as a function: a tick's text from its value (a category's text, a number or a date) and its index, over <see cref="TickFormat"/>; null keeps the format's text. Not written to JSON: <see cref="TickTexts"/> records what it wrote.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Func<object?, int, string?>? TickFormatter { get; init; }

    /// <summary>What <see cref="TickFormatter"/> wrote when the page was rendered, by <see cref="Text.FormatKeys.Tick"/>, for the web writer.</summary>
    public ImmutableDictionary<string, string>? TickTexts { get; init; }

    /// <summary>How the web's writer rebuilds <see cref="TickFormatter"/>'s converter (<c>type:</c> or <c>static:</c>, Aioros's plans file reference), run through the app's generated converters.</summary>
    public string? TickConverter { get; init; }

    /// <summary>A unit written after every tick label.</summary>
    public string? Unit { get; init; }

    /// <summary>The axis' label, if any.</summary>
    public LabelSpec? Label { get; init; }

    /// <summary>The <c>Label</c> children, drawn after <see cref="Label"/>.</summary>
    public ImmutableArray<LabelSpec> Labels { get; init; } = [];

    /// <summary>The order of the axis among the chart's children, which is the order Recharts draws them in.</summary>
    public int Order { get; init; }

    /// <summary>The axis' name in a scatter's tooltip; its data key when null.</summary>
    public string? Name { get; init; }

    /// <summary>The rotation of the tick labels in degrees (Recharts' <c>angle</c>).</summary>
    public double Angle { get; init; }

    /// <summary>The anchor of the tick labels; null for the orientation's (Recharts' <c>textAnchor</c>).</summary>
    public TextAnchorKind? TextAnchor { get; init; }

    /// <summary>The color of the tick labels (the <c>tick</c> object's <c>fill</c>); the axis' stroke when null.</summary>
    public string? TickFill { get; init; }

    /// <summary>The font size of the tick labels (the <c>tick</c> object's <c>fontSize</c>); the chart's when null.</summary>
    public double? TickFontSize { get; init; }

    /// <summary>The axis line's own SVG props (Recharts' <c>axisLine</c> object).</summary>
    public LineStyle? AxisLineStyle { get; init; }

    /// <summary>The tick lines' own SVG props (Recharts' <c>tickLine</c> object).</summary>
    public LineStyle? TickLineStyle { get; init; }

    /// <summary>The ticks the axis shows instead of its computed ones (Recharts' <c>ticks</c>), as text: numbers on a numeric axis, categories on a category one.</summary>
    public ImmutableArray<string> Ticks { get; init; } = [];

    /// <summary>Whether the padding is fixed or derived from the gaps between the values (Recharts' <c>padding="gap"</c> or <c>"no-gap"</c>).</summary>
    public AxisPaddingKind PaddingKind { get; init; }

    /// <summary>Whether a category that appears twice gets two ticks (Recharts' default) or one.</summary>
    public bool AllowDuplicatedCategory { get; init; } = true;

    /// <summary>Whether hidden series still count in the axis' domain.</summary>
    public bool IncludeHidden { get; init; }

    /// <summary>The categories of a category axis given by its domain, which replace the data's when they contain every one of them.</summary>
    public ImmutableArray<string> CategoryDomain { get; init; } = [];
}

/// <summary>A series: a <c>Line</c>, an <c>Area</c> or a <c>Bar</c>.</summary>
public enum SeriesKind
{
    /// <summary><c>Line</c>.</summary>
    Line,

    /// <summary><c>Area</c>.</summary>
    Area,

    /// <summary><c>Bar</c>.</summary>
    Bar,

    /// <summary><c>Scatter</c>: a circle per point, in a composed chart.</summary>
    Scatter,
}

/// <summary>The curve between a line's or an area's points, Recharts' <c>type</c>.</summary>
public enum CurveType
{
    /// <summary><c>linear</c>.</summary>
    Linear,

    /// <summary><c>monotone</c> (d3's monotoneX).</summary>
    Monotone,

    /// <summary><c>monotoneX</c>.</summary>
    MonotoneX,

    /// <summary><c>monotoneY</c>.</summary>
    MonotoneY,

    /// <summary><c>natural</c>.</summary>
    Natural,

    /// <summary><c>step</c>.</summary>
    Step,

    /// <summary><c>stepBefore</c>.</summary>
    StepBefore,

    /// <summary><c>stepAfter</c>.</summary>
    StepAfter,

    /// <summary><c>basis</c>.</summary>
    Basis,

    /// <summary><c>linearClosed</c>.</summary>
    LinearClosed,

    /// <summary><c>basisClosed</c>.</summary>
    BasisClosed,

    /// <summary><c>basisOpen</c>.</summary>
    BasisOpen,

    /// <summary><c>bump</c>: d3's bumpX, or bumpY in a vertical chart.</summary>
    Bump,

    /// <summary><c>bumpX</c>.</summary>
    BumpX,

    /// <summary><c>bumpY</c>.</summary>
    BumpY,
}

/// <summary>The icon a series shows in the legend, Recharts' <c>legendType</c>.</summary>
public enum LegendIcon
{
    /// <summary>A line with a circle (<c>line</c>).</summary>
    Line,

    /// <summary>A flat line (<c>plainline</c>).</summary>
    PlainLine,

    /// <summary>A square (<c>square</c>).</summary>
    Square,

    /// <summary>A rectangle (<c>rect</c>).</summary>
    Rect,

    /// <summary>A circle (<c>circle</c>).</summary>
    Circle,

    /// <summary>No legend entry (<c>none</c>).</summary>
    None,

    /// <summary>A cross (<c>cross</c>, d3's symbol).</summary>
    Cross,

    /// <summary>A diamond (<c>diamond</c>).</summary>
    Diamond,

    /// <summary>A star (<c>star</c>).</summary>
    Star,

    /// <summary>A triangle (<c>triangle</c>).</summary>
    Triangle,

    /// <summary>A wye (<c>wye</c>).</summary>
    Wye,
}

/// <summary>One series of a cartesian chart.</summary>
public sealed record SeriesSpec
{
    /// <summary>Whether a line's points with no previous point come from twice the plot's width and half its height in an update (Recharts' <c>animateNewValues</c>, on by default); off, they stand at their place from the start.</summary>
    public bool AnimateNewValues { get; init; } = true;

    /// <summary>The element's own classes after Recharts' (its <c>className</c>), or none.</summary>
    public string? ClassName { get; init; }

    /// <summary>SVG presentation attributes Recharts spreads onto a line's or an area's curves and dots, or none.</summary>
    public SvgPresentation? Presentation { get; init; }

    /// <summary>The key of the command its items run when clicked (Recharts' <c>onClick</c>), written as buttons on the web; none when no command is set.</summary>
    public string? ClickCommand { get; init; }

    /// <summary>The series' kind.</summary>
    public SeriesKind Kind { get; init; }

    /// <summary>The data key of the series' values.</summary>
    public string DataKey { get; init; } = string.Empty;

    /// <summary>The name shown in the legend and the tooltip; the data key when null.</summary>
    public string? Name { get; init; }

    /// <summary>The curve of a line or an area.</summary>
    public CurveType Curve { get; init; }

    /// <summary>The stroke color; null for the kind's default.</summary>
    public string? Stroke { get; init; }

    /// <summary>The stroke width.</summary>
    public double StrokeWidth { get; init; } = 1;

    /// <summary>The fill color; null for the kind's default.</summary>
    public string? Fill { get; init; }

    /// <summary>The fill opacity; null for the kind's default (0.6 for an area).</summary>
    public double? FillOpacity { get; init; }

    /// <summary>The stroke's dash pattern, as SVG writes it (<c>5 5</c>).</summary>
    public string? StrokeDashArray { get; init; }

    /// <summary>Whether a line or an area shows a dot at every point.</summary>
    public bool Dot { get; init; }

    /// <summary>The dots' own props (Recharts' <c>dot</c> given as an object), merged over the series' paint.</summary>
    public ActiveStyle? DotProps { get; init; }

    /// <summary>Whether a line's or an area's dots are clipped with it on an axis that allows data overflow (Recharts' <c>dot.clipDot</c>); off, they are clipped to the plot grown by a dot.</summary>
    public bool ClipDot { get; init; } = true;

    /// <summary>Whether the hovered point shows an active dot.</summary>
    public bool ActiveDot { get; init; } = true;

    /// <summary>The radius of the hovered point's dot.</summary>
    public double ActiveDotRadius { get; init; } = 4;

    /// <summary>The stack the series belongs to, if any.</summary>
    public string? StackId { get; init; }

    /// <summary>The x-axis the series maps through.</summary>
    public string XAxisId { get; init; } = "0";

    /// <summary>The y-axis the series maps through.</summary>
    public string YAxisId { get; init; } = "0";

    /// <summary>A bar's fixed width: a number, or a percentage of the category axis' length.</summary>
    public string? BarSize { get; init; }

    /// <summary>A bar's largest width.</summary>
    public double? MaxBarSize { get; init; }

    /// <summary>A bar's corner radius: top-left, top-right, bottom-right, bottom-left.</summary>
    public ImmutableArray<double> Radius { get; init; } = [];

    /// <summary>Whether a bar draws its band's background.</summary>
    public string? Background { get; init; }

    /// <summary>The background rectangles' stroke, stroke width and fill opacity (Recharts' <c>background</c> object), or null.</summary>
    public CellSpec? BackgroundProps { get; init; }

    /// <summary>The background rectangles' corner radius (the <c>background</c> object's <c>radius</c>; none of the bar's).</summary>
    public ImmutableArray<double> BackgroundRadius { get; init; } = [];

    /// <summary>The smallest height a bar is drawn with.</summary>
    public double MinPointSize { get; init; }

    /// <summary>The item's member holding the address a bar or a point links to, or none.</summary>
    public string? HrefKey { get; init; }

    /// <summary>The item's member holding a bar's own fill (the <c>fill</c> Recharts reads from an item), or none.</summary>
    public string? FillKey { get; init; }

    /// <summary>The item's member holding a bar's own opacity, or none.</summary>
    public string? OpacityKey { get; init; }

    /// <summary>The fills of individual bars (Recharts' <c>Cell</c>), by data index; an empty entry keeps the series' fill.</summary>
    public ImmutableArray<CellSpec> Cells { get; init; } = [];

    /// <summary>The labels of the series' points (Recharts' <c>LabelList</c> or <c>label</c>), if any.</summary>
    public LabelSpec? Labels { get; init; }

    /// <summary>The series' further label lists, drawn after <see cref="Labels"/> (Recharts draws the <c>label</c> prop's list, then every <c>LabelList</c> child).</summary>
    public ImmutableArray<LabelSpec> LabelLists { get; init; } = [];

    /// <summary>A unit written after the series' values in the tooltip.</summary>
    public string? Unit { get; init; }

    /// <summary>The legend icon; null for the kind's (a line for lines and areas, a rectangle for bars).</summary>
    public LegendIcon? LegendType { get; init; }

    /// <summary>
    /// An area's baseline when its values are single numbers (Recharts' <c>baseValue</c>): a number, <c>dataMin</c> or
    /// <c>dataMax</c> (the value axis' domain ends); null for the chart's, then Recharts' default (0, or the domain's end
    /// nearest 0).
    /// </summary>
    public string? BaseValue { get; init; }

    /// <summary>Whether a line or an area connects across missing values.</summary>
    public bool ConnectNulls { get; init; }

    /// <summary>Whether the series is hidden (it keeps its legend entry).</summary>
    public bool Hide { get; init; }

    /// <summary>Whether the series writes no tooltip line (Recharts' <c>tooltipType="none"</c>); it still counts as content.</summary>
    public bool TooltipNone { get; init; }

    /// <summary>The order of the series among the chart's children.</summary>
    public int Order { get; init; }

    /// <summary>The series' own items (Recharts' <c>data</c> on a series), or null to read the chart's.</summary>
    public DataTable? Data { get; init; }

    /// <summary>The z-axis a scatter maps its sizes through.</summary>
    public string ZAxisId { get; init; } = "0";

    /// <summary>The symbol of a scatter's points.</summary>
    public ScatterShape Shape { get; init; }

    /// <summary>Whether a scatter joins its points with a line (Recharts' <c>line</c>).</summary>
    public bool JointLine { get; init; }

    /// <summary>The curve of a scatter's joint line (Recharts' <c>lineJointType</c>).</summary>
    public CurveType LineJointType { get; init; }

    /// <summary>Whether a scatter's line joins its points or fits them (Recharts' <c>lineType</c>).</summary>
    public ScatterLineType LineType { get; init; }

    /// <summary>The stroke of a scatter's line (Recharts' <c>line</c> props); the scatter's fill when null.</summary>
    public string? LineStroke { get; init; }

    /// <summary>The stroke width of a scatter's line, when set.</summary>
    public double? LineStrokeWidth { get; init; }

    /// <summary>The dash pattern of a scatter's line, when set.</summary>
    public string? LineStrokeDashArray { get; init; }

    /// <summary>The series' error bars.</summary>
    public ImmutableArray<ErrorBarSpec> ErrorBars { get; init; } = [];

    /// <summary>How the hovered bar is drawn (Recharts' <c>activeBar</c>), if it is drawn differently.</summary>
    public ActiveStyle? ActiveBar { get; init; }

    /// <summary>How the hovered point's dot is drawn beyond its radius (Recharts' <c>activeDot</c> props).</summary>
    public ActiveStyle? ActiveDotStyle { get; init; }

    /// <summary>The bar or symbol drawn active whatever the pointer does (Recharts' <c>activeIndex</c>), by its index among the drawn items.</summary>
    public int? ActiveIndex { get; init; }

    /// <summary>The enter animation; null for Recharts' defaults of the kind.</summary>
    public AnimationSpec? Animation { get; init; }

    /// <summary>The custom content drawn in place of each bar or symbol (Recharts' <c>shape</c> element), compiled from its XAML template.</summary>
    public ContentSpec? ShapeContent { get; init; }

    /// <summary>The custom content drawn in place of the hovered bar or symbol (Recharts' <c>activeBar</c> or <c>activeShape</c> element).</summary>
    public ContentSpec? ActiveShapeContent { get; init; }

    /// <summary>The custom content drawn at each point of a line or an area (Recharts' <c>dot</c> element).</summary>
    public ContentSpec? DotContent { get; init; }

    /// <summary>The custom content drawn at the hovered point (Recharts' <c>activeDot</c> element).</summary>
    public ContentSpec? ActiveDotContent { get; init; }
}

/// <summary>A bar's (a sector's, a symbol's, a trapezoid's) own paint, Recharts' <c>Cell</c>.</summary>
/// <param name="Fill">The fill, or null for the series'.</param>
/// <param name="Stroke">The stroke, or null for the series'.</param>
public sealed record CellSpec(string? Fill, string? Stroke = null)
{
    /// <summary>The stroke width, or null for the series'.</summary>
    public double? StrokeWidth { get; init; }

    /// <summary>The fill opacity, or null for the series'.</summary>
    public double? FillOpacity { get; init; }

    /// <summary>The shape's opacity, or null.</summary>
    public double? Opacity { get; init; }

    /// <summary>The stroke's dash pattern, or null for the series'.</summary>
    public string? StrokeDashArray { get; init; }
}

/// <summary>Where a label sits relative to its shape, Recharts' <c>position</c>.</summary>
public enum LabelPosition
{
    /// <summary>Above (<c>top</c>).</summary>
    Top,

    /// <summary>Below (<c>bottom</c>).</summary>
    Bottom,

    /// <summary>Left of it (<c>left</c>).</summary>
    Left,

    /// <summary>Right of it (<c>right</c>).</summary>
    Right,

    /// <summary>At its center (<c>center</c>).</summary>
    Center,

    /// <summary>Inside, at the top (<c>insideTop</c>).</summary>
    InsideTop,

    /// <summary>Inside, at the bottom (<c>insideBottom</c>).</summary>
    InsideBottom,

    /// <summary>Inside, at the left (<c>insideLeft</c>).</summary>
    InsideLeft,

    /// <summary>Inside, at the right (<c>insideRight</c>).</summary>
    InsideRight,

    /// <summary>Inside, at the top left (<c>insideTopLeft</c>).</summary>
    InsideTopLeft,

    /// <summary>Inside, at the top right (<c>insideTopRight</c>).</summary>
    InsideTopRight,

    /// <summary>Inside, at the bottom left (<c>insideBottomLeft</c>).</summary>
    InsideBottomLeft,

    /// <summary>Inside, at the bottom right (<c>insideBottomRight</c>).</summary>
    InsideBottomRight,

    /// <summary>Inside (<c>inside</c>).</summary>
    Inside,

    /// <summary>Outside (<c>outside</c>).</summary>
    Outside,

    /// <summary>At the start of a line (<c>start</c>).</summary>
    Start,

    /// <summary>In the middle of a line (<c>middle</c>).</summary>
    Middle,

    /// <summary>At the end of a line (<c>end</c>).</summary>
    End,

    /// <summary>Inside a radial bar, at its start, along its arc (<c>insideStart</c>).</summary>
    InsideStart,

    /// <summary>Inside a radial bar, at its end, along its arc (<c>insideEnd</c>).</summary>
    InsideEnd,

    /// <summary>At the center of a polar box, hanging below it (<c>centerTop</c>); the center elsewhere.</summary>
    CenterTop,

    /// <summary>At the center of a polar box, standing on it (<c>centerBottom</c>); the center elsewhere.</summary>
    CenterBottom,
}

/// <summary>A <c>Label</c> or a <c>LabelList</c>.</summary>
public sealed record LabelSpec
{
    /// <summary>The custom content drawn in place of each label (Recharts' <c>content</c> element), compiled from its XAML template.</summary>
    public ContentSpec? Content { get; init; }

    /// <summary>A fixed text (a <c>Label</c>'s <c>value</c>); null shows the values (a <c>LabelList</c>).</summary>
    public string? Value { get; init; }

    /// <summary>The data key of a label list's values; the series' when null.</summary>
    public string? DataKey { get; init; }

    /// <summary>The position; null for the element's default.</summary>
    public LabelPosition? Position { get; init; }

    /// <summary>The distance from the shape.</summary>
    public double Offset { get; init; } = 5;

    /// <summary>The text color; null for the default.</summary>
    public string? Fill { get; init; }

    /// <summary>The font size; null for the chart's.</summary>
    public double? FontSize { get; init; }

    /// <summary>The rotation in degrees.</summary>
    public double Angle { get; init; }

    /// <summary>A .NET composite format for the values.</summary>
    public string? Format { get; init; }

    /// <summary>The text's weight (<c>bold</c>, <c>600</c>); the page's when null.</summary>
    public string? FontWeight { get; init; }

    /// <summary>The text's stroke; the shape's (a pie sector's) or none when null.</summary>
    public string? Stroke { get; init; }

    /// <summary>The text's stroke width.</summary>
    public double? StrokeWidth { get; init; }

    /// <summary>Whether a label along a radial bar's arc runs clockwise (Recharts' <c>clockWise</c>).</summary>
    public bool ClockWise { get; init; }

    /// <summary>The x of a position given as an offset into the shape's box (Recharts' <c>position={{x, y}}</c>): pixels or a percentage.</summary>
    public string? PositionX { get; init; }

    /// <summary>The y of a position given as an offset into the shape's box: pixels or a percentage.</summary>
    public string? PositionY { get; init; }

    /// <summary>Whether a label wrapped to its shape's width breaks between any two characters (Recharts' <c>textBreakAll</c>).</summary>
    public bool BreakAll { get; init; }
}

/// <summary>Recharts' <c>CartesianGrid</c>.</summary>
public sealed record GridSpec
{
    /// <summary>The element's own classes after Recharts' (its <c>className</c>), or none.</summary>
    public string? ClassName { get; init; }

    /// <summary>SVG presentation attributes Recharts spreads onto the grid's lines, or none.</summary>
    public SvgPresentation? Presentation { get; init; }

    /// <summary>Whether horizontal lines are drawn.</summary>
    public bool Horizontal { get; init; } = true;

    /// <summary>Whether vertical lines are drawn.</summary>
    public bool Vertical { get; init; } = true;

    /// <summary>The lines' color; the theme's when null.</summary>
    public string? Stroke { get; init; }

    /// <summary>The lines' dash pattern.</summary>
    public string? StrokeDashArray { get; init; }

    /// <summary>A background fill of the plot, if any.</summary>
    public string? Fill { get; init; }

    /// <summary>The background's and the stripes' fill opacity (and the lines' attribute, as Recharts spreads it).</summary>
    public double? FillOpacity { get; init; }

    /// <summary>The lines' width.</summary>
    public double? StrokeWidth { get; init; }

    /// <summary>The lines' opacity.</summary>
    public double? StrokeOpacity { get; init; }

    /// <summary>The fills the horizontal stripes cycle through (Recharts' <c>horizontalFill</c>).</summary>
    public ImmutableArray<string> HorizontalFill { get; init; } = [];

    /// <summary>The fills the vertical stripes cycle through (Recharts' <c>verticalFill</c>).</summary>
    public ImmutableArray<string> VerticalFill { get; init; } = [];

    /// <summary>The y of every horizontal line, instead of the y-axis' ticks (Recharts' <c>horizontalPoints</c>).</summary>
    public ImmutableArray<double> HorizontalPoints { get; init; } = [];

    /// <summary>The x of every vertical line, instead of the x-axis' ticks (Recharts' <c>verticalPoints</c>).</summary>
    public ImmutableArray<double> VerticalPoints { get; init; } = [];

    /// <summary>Whether the lines are the ticks' only, without the plot's edges (Recharts' <c>syncWithTicks</c>).</summary>
    public bool SyncWithTicks { get; init; }

    /// <summary>The y-axis values the horizontal lines stand at, kept as the axis keeps its ticks (Recharts' <c>horizontalValues</c>).</summary>
    public ImmutableArray<string> HorizontalValues { get; init; } = [];

    /// <summary>The x-axis values the vertical lines stand at, kept as the axis keeps its ticks (Recharts' <c>verticalValues</c>).</summary>
    public ImmutableArray<string> VerticalValues { get; init; } = [];

    /// <summary>The grid's left edge; the plot's when null.</summary>
    public double? X { get; init; }

    /// <summary>The grid's top edge; the plot's when null.</summary>
    public double? Y { get; init; }

    /// <summary>The grid's width; the plot's when null.</summary>
    public double? Width { get; init; }

    /// <summary>The grid's height; the plot's when null.</summary>
    public double? Height { get; init; }

    /// <summary>The order of the grid among the chart's children.</summary>
    public int Order { get; init; }
}

/// <summary>Recharts' <c>ReferenceLine</c>: a vertical line at an x value or a horizontal one at a y value.</summary>
public sealed record ReferenceLineSpec
{
    /// <summary>The custom content drawn in place of the reference's shape (Recharts' <c>shape</c> element), compiled from its XAML template.</summary>
    public ContentSpec? ShapeContent { get; init; }

    /// <summary>The x value of a vertical line: a category or a number, as text.</summary>
    public string? X { get; init; }

    /// <summary>The y value of a horizontal line, as text.</summary>
    public string? Y { get; init; }

    /// <summary>The line's color; the theme's when null.</summary>
    public string? Stroke { get; init; }

    /// <summary>The line's width.</summary>
    public double StrokeWidth { get; init; } = 1;

    /// <summary>The line's dash pattern.</summary>
    public string? StrokeDashArray { get; init; }

    /// <summary>The line's label, if any.</summary>
    public LabelSpec? Label { get; init; }

    /// <summary>The <c>Label</c> children, drawn after <see cref="Label"/>.</summary>
    public ImmutableArray<LabelSpec> Labels { get; init; } = [];

    /// <summary>The x-axis the line maps through.</summary>
    public string XAxisId { get; init; } = "0";

    /// <summary>The y-axis the line maps through.</summary>
    public string YAxisId { get; init; } = "0";

    /// <summary>What a value outside its axis' domain does to the line, Recharts' <c>ifOverflow</c>.</summary>
    public OverflowKind IfOverflow { get; init; }

    /// <summary>Where on a category's band the line's values fall, Recharts' <c>position</c>.</summary>
    public BandPosition Position { get; init; } = BandPosition.Middle;

    /// <summary>A line between two points instead of across the plot, Recharts' <c>segment</c>.</summary>
    public ReferenceSegment? Segment { get; init; }

    /// <summary>The order of the line among the chart's children.</summary>
    public int Order { get; init; }
}

/// <summary>Where a value falls on a category's band, Recharts' <c>position</c> of a reference line.</summary>
public enum BandPosition
{
    /// <summary>At the band's start (<c>start</c>).</summary>
    Start,

    /// <summary>At the band's middle (<c>middle</c>, the default).</summary>
    Middle,

    /// <summary>At the band's end (<c>end</c>).</summary>
    End,
}

/// <summary>The two points of a reference line's <c>segment</c>, each an x and a y value as text (a category or a number).</summary>
/// <param name="X1">The first point's x value.</param>
/// <param name="Y1">The first point's y value.</param>
/// <param name="X2">The second point's x value.</param>
/// <param name="Y2">The second point's y value.</param>
public sealed record ReferenceSegment(string? X1, string? Y1, string? X2, string? Y2);
