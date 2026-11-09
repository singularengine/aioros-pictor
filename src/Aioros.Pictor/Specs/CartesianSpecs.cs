using System.Collections.Immutable;

namespace Aioros.Pictor.Specs;

/// <summary>How a cartesian chart lays its categories out, Recharts' <c>layout</c>.</summary>
public enum ChartLayoutKind
{
    /// <summary>Categories along the x-axis, values up the y-axis (<c>horizontal</c>, the default).</summary>
    Horizontal,

    /// <summary>Categories down the y-axis, values along the x-axis (<c>vertical</c>).</summary>
    Vertical,
}

/// <summary>The symbol a scatter draws at each point, Recharts' <c>shape</c> (d3's symbols).</summary>
public enum ScatterShape
{
    /// <summary><c>circle</c>.</summary>
    Circle,

    /// <summary><c>cross</c>.</summary>
    Cross,

    /// <summary><c>diamond</c>.</summary>
    Diamond,

    /// <summary><c>square</c>.</summary>
    Square,

    /// <summary><c>star</c>.</summary>
    Star,

    /// <summary><c>triangle</c>.</summary>
    Triangle,

    /// <summary><c>wye</c>.</summary>
    Wye,
}

/// <summary>How a scatter's line runs, Recharts' <c>lineType</c>.</summary>
public enum ScatterLineType
{
    /// <summary>Through every point, with the line's curve (<c>joint</c>, the default).</summary>
    Joint,

    /// <summary>The least-squares line between the smallest and the largest x (<c>fitting</c>).</summary>
    Fitting,
}

/// <summary>Recharts' <c>ZAxis</c>: maps a third value of a scatter's points to the area of their symbols.</summary>
public sealed record ZAxisSpec
{
    /// <summary>The axis id scatters refer to.</summary>
    public string Id { get; init; } = "0";

    /// <summary>The data key of the sizes; null gives every point the range's lower bound.</summary>
    public string? DataKey { get; init; }

    /// <summary>The smallest symbol area.</summary>
    public double RangeMin { get; init; } = 64;

    /// <summary>The largest symbol area.</summary>
    public double RangeMax { get; init; } = 64;

    /// <summary>The name shown in the tooltip.</summary>
    public string? Name { get; init; }

    /// <summary>A unit written after the values in the tooltip.</summary>
    public string? Unit { get; init; }

    /// <summary>Whether the sizes come from numbers (a numeric scale) or categories (a band scale over the range).</summary>
    public AxisType Type { get; init; } = AxisType.Number;

    /// <summary>The scale of numeric sizes: <c>auto</c> or <c>linear</c>, <c>log</c>, <c>pow</c>, <c>sqrt</c>; null for <c>auto</c>.</summary>
    public string? Scale { get; init; }

    /// <summary>The domain's lower bound, as an axis' (<c>0</c> by default).</summary>
    public string? DomainMin { get; init; }

    /// <summary>The domain's upper bound, as an axis' (<c>auto</c> by default).</summary>
    public string? DomainMax { get; init; }
}

/// <summary>Recharts' <c>ErrorBar</c>: a whisker around each value of its series.</summary>
public sealed record ErrorBarSpec
{
    /// <summary>SVG presentation attributes Recharts spreads onto each error bar's group (its opacity) and lines, or none.</summary>
    public SvgPresentation? Presentation { get; init; }

    /// <summary>The data key of the errors: one value, or a lower and an upper one.</summary>
    public string DataKey { get; init; } = string.Empty;

    /// <summary>Half the width of the whisker's ends.</summary>
    public double Width { get; init; } = 5;

    /// <summary>The stroke width.</summary>
    public double StrokeWidth { get; init; } = 1.5;

    /// <summary>The stroke color.</summary>
    public string? Stroke { get; init; }

    /// <summary>Whether the bars run along the x-axis (a scatter's <c>direction="x"</c>); otherwise along the values' axis.</summary>
    public bool AlongX { get; init; }
}

/// <summary>What a value outside its axis' domain does to a reference element, Recharts' <c>ifOverflow</c>.</summary>
public enum OverflowKind
{
    /// <summary>The element is dropped (<c>discard</c>).</summary>
    Discard,

    /// <summary>The element is drawn where it falls (<c>visible</c>).</summary>
    Visible,

    /// <summary>The element is clipped to the plot (<c>hidden</c>).</summary>
    Hidden,

    /// <summary>The domain grows to include it (<c>extendDomain</c>).</summary>
    ExtendDomain,
}

/// <summary>Recharts' <c>ReferenceArea</c>: a rectangle between two x values, two y values, or both.</summary>
public sealed record ReferenceAreaSpec
{
    /// <summary>The custom content drawn in place of the reference's shape (Recharts' <c>shape</c> element), compiled from its XAML template.</summary>
    public ContentSpec? ShapeContent { get; init; }

    /// <summary>The left x value, as text; null for the plot's edge.</summary>
    public string? X1 { get; init; }

    /// <summary>The right x value, as text; null for the plot's edge.</summary>
    public string? X2 { get; init; }

    /// <summary>The first y value, as text; null for the plot's edge.</summary>
    public string? Y1 { get; init; }

    /// <summary>The second y value, as text; null for the plot's edge.</summary>
    public string? Y2 { get; init; }

    /// <summary>The fill.</summary>
    public string? Fill { get; init; }

    /// <summary>The fill opacity.</summary>
    public double FillOpacity { get; init; } = 0.5;

    /// <summary>The stroke.</summary>
    public string Stroke { get; init; } = "none";

    /// <summary>The stroke opacity, when set.</summary>
    public double? StrokeOpacity { get; init; }

    /// <summary>The stroke width.</summary>
    public double StrokeWidth { get; init; } = 1;

    /// <summary>The area's label, if any.</summary>
    public LabelSpec? Label { get; init; }

    /// <summary>The <c>Label</c> children, drawn after <see cref="Label"/>.</summary>
    public ImmutableArray<LabelSpec> Labels { get; init; } = [];

    /// <summary>The rectangle's corner radius (Recharts' <c>radius</c>, a <c>Rectangle</c> prop): one value, or top left, top right, bottom right, bottom left.</summary>
    public ImmutableArray<double> Radius { get; init; } = [];

    /// <summary>The x-axis the area maps through.</summary>
    public string XAxisId { get; init; } = "0";

    /// <summary>The y-axis the area maps through.</summary>
    public string YAxisId { get; init; } = "0";

    /// <summary>What a value outside the domain does.</summary>
    public OverflowKind IfOverflow { get; init; }

    /// <summary>The order of the area among the chart's children.</summary>
    public int Order { get; init; }
}

/// <summary>Recharts' <c>ReferenceDot</c>: a circle at an x and a y value.</summary>
public sealed record ReferenceDotSpec
{
    /// <summary>The custom content drawn in place of the reference's shape (Recharts' <c>shape</c> element), compiled from its XAML template.</summary>
    public ContentSpec? ShapeContent { get; init; }

    /// <summary>The x value, as text.</summary>
    public string? X { get; init; }

    /// <summary>The y value, as text.</summary>
    public string? Y { get; init; }

    /// <summary>The radius.</summary>
    public double R { get; init; } = 10;

    /// <summary>The fill.</summary>
    public string? Fill { get; init; }

    /// <summary>The fill opacity.</summary>
    public double FillOpacity { get; init; } = 1;

    /// <summary>The stroke; the theme's when null.</summary>
    public string? Stroke { get; init; }

    /// <summary>The stroke width.</summary>
    public double StrokeWidth { get; init; } = 1;

    /// <summary>The dot's label, if any.</summary>
    public LabelSpec? Label { get; init; }

    /// <summary>The <c>Label</c> children, drawn after <see cref="Label"/>.</summary>
    public ImmutableArray<LabelSpec> Labels { get; init; } = [];

    /// <summary>The x-axis the dot maps through.</summary>
    public string XAxisId { get; init; } = "0";

    /// <summary>The y-axis the dot maps through.</summary>
    public string YAxisId { get; init; } = "0";

    /// <summary>What a value outside the domain does.</summary>
    public OverflowKind IfOverflow { get; init; }

    /// <summary>The order of the dot among the chart's children.</summary>
    public int Order { get; init; }
}

/// <summary>How the hovered bar or dot, or every dot, is drawn (Recharts' <c>activeBar</c>, <c>activeDot</c> or <c>dot</c> as props); null keeps the default.</summary>
/// <param name="Fill">The fill.</param>
/// <param name="Stroke">The stroke.</param>
/// <param name="StrokeWidth">The stroke width.</param>
public sealed record ActiveStyle(string? Fill = null, string? Stroke = null, double? StrokeWidth = null)
{
    /// <summary>A dot's radius (Recharts' <c>dot={{r}}</c>); 3 when null.</summary>
    public double? R { get; init; }

    /// <summary>The fill opacity.</summary>
    public double? FillOpacity { get; init; }

    /// <summary>The stroke's dash pattern.</summary>
    public string? StrokeDashArray { get; init; }
}
