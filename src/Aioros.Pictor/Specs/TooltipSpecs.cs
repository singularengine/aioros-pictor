using System.Collections.Immutable;

namespace Aioros.Pictor.Specs;

/// <summary>Recharts' <c>Tooltip</c> with its default content.</summary>
public sealed record TooltipSpec
{
    /// <summary>The custom content of the box (Recharts' <c>content</c> element), compiled from its XAML template as HTML.</summary>
    public ContentSpec? Content { get; init; }

    /// <summary>The custom content of each line where <see cref="Content"/> holds its items node (Pictor's <c>TooltipItems</c>), one per line of the payload.</summary>
    public ContentSpec? ItemContent { get; init; }

    /// <summary>The text between a series' name and its value.</summary>
    public string Separator { get; init; } = " : ";

    /// <summary>Whether a cursor marks the hovered category.</summary>
    public bool Cursor { get; init; } = true;

    /// <summary>A .NET composite format for the values (Recharts' <c>formatter</c>).</summary>
    public string? Format { get; init; }

    /// <summary>A .NET composite format for the label (Recharts' <c>labelFormatter</c>).</summary>
    public string? LabelFormat { get; init; }

    /// <summary>Recharts' <c>formatter</c> as a function: a line's text from its value and its series' name, over <see cref="Format"/>; null keeps the format's text. Not written to JSON: <see cref="Texts"/> records what it wrote.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Func<object?, string, string?>? Formatter { get; init; }

    /// <summary>Recharts' <c>labelFormatter</c> as a function: the label's text from the active category, over <see cref="LabelFormat"/>; null keeps the format's text. Not written to JSON: <see cref="LabelTexts"/> records what it wrote.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Func<object?, string?>? LabelFormatter { get; init; }

    /// <summary>How the web's writer rebuilds <see cref="Formatter"/>'s converter (<c>type:</c> or <c>static:</c>, Aioros's plans file reference), run through the app's generated converters.</summary>
    public string? Converter { get; init; }

    /// <summary>How the web's writer rebuilds <see cref="LabelFormatter"/>'s converter.</summary>
    public string? LabelConverter { get; init; }

    /// <summary>What <see cref="Formatter"/> wrote when the page was rendered, by <see cref="Text.FormatKeys.Value"/>, for the web writer.</summary>
    public ImmutableDictionary<string, string>? Texts { get; init; }

    /// <summary>What <see cref="LabelFormatter"/> wrote when the page was rendered, by <see cref="Text.FormatKeys.Label"/>.</summary>
    public ImmutableDictionary<string, string>? LabelTexts { get; init; }

    /// <summary>The cursor's dash pattern (Recharts' <c>cursor={{strokeDasharray}}</c>).</summary>
    public string? CursorStrokeDashArray { get; init; }

    /// <summary>The cursor's stroke; Recharts' <c>#ccc</c> (none for a band) when null.</summary>
    public string? CursorStroke { get; init; }

    /// <summary>The cursor's fill; none for a line, <c>#ccc</c> for a band when null.</summary>
    public string? CursorFill { get; init; }

    /// <summary>The cursor's stroke width.</summary>
    public double? CursorStrokeWidth { get; init; }

    /// <summary>The cursor's fill opacity.</summary>
    public double? CursorFillOpacity { get; init; }

    /// <summary>The cursor's stroke opacity.</summary>
    public double? CursorStrokeOpacity { get; init; }

    /// <summary>Whether entries without a value are left out (Recharts' <c>filterNull</c>, on by default).</summary>
    public bool FilterNull { get; init; } = true;

    /// <summary>Whether hidden series keep their entries (Recharts' <c>includeHidden</c>).</summary>
    public bool IncludeHidden { get; init; }

    /// <summary>The key the entries are ordered by (Recharts' <c>itemSorter</c>: <c>dataKey</c>, <c>value</c> or <c>name</c>); their series' order when null.</summary>
    public string? ItemSorter { get; init; }

    /// <summary>Whether entries of the same data key are shown once (Recharts' <c>payloadUniqBy={true}</c>).</summary>
    public bool PayloadUniqBy { get; init; }

    /// <summary>Whether a chart that allows both shows one tooltip per category (true) or per item (false); null for the chart's default.</summary>
    public bool? Shared { get; init; }

    /// <summary>Whether a click shows the tooltip (Recharts' <c>trigger="click"</c>) rather than hovering.</summary>
    public bool ClickTrigger { get; init; }

    /// <summary>The category shown before any pointer event (Recharts' <c>defaultIndex</c>).</summary>
    public int? DefaultIndex { get; init; }

    /// <summary>False to never show the box and the cursor (the active shapes still show); null to follow the pointer.</summary>
    public bool? Active { get; init; }

    /// <summary>The distance between the anchor and the box (Recharts' <c>offset</c>).</summary>
    public double Offset { get; init; } = 10;

    /// <summary>Whether the box may leave the plot along x rather than being flipped (Recharts' <c>allowEscapeViewBox.x</c>).</summary>
    public bool AllowEscapeX { get; init; }

    /// <summary>Whether the box may leave the plot along y (Recharts' <c>allowEscapeViewBox.y</c>).</summary>
    public bool AllowEscapeY { get; init; }

    /// <summary>Whether the box goes left of its anchor first (Recharts' <c>reverseDirection.x</c>).</summary>
    public bool ReverseX { get; init; }

    /// <summary>Whether the box goes above its anchor first (Recharts' <c>reverseDirection.y</c>).</summary>
    public bool ReverseY { get; init; }

    /// <summary>A fixed x of the box (Recharts' <c>position.x</c>).</summary>
    public double? PositionX { get; init; }

    /// <summary>A fixed y of the box (Recharts' <c>position.y</c>).</summary>
    public double? PositionY { get; init; }

    /// <summary>CSS declarations of the box's wrapper (Recharts' <c>wrapperStyle</c>).</summary>
    public string? WrapperStyle { get; init; }

    /// <summary>CSS declarations of the box (Recharts' <c>contentStyle</c>).</summary>
    public string? ContentStyle { get; init; }

    /// <summary>CSS declarations of the label (Recharts' <c>labelStyle</c>).</summary>
    public string? LabelStyle { get; init; }

    /// <summary>CSS declarations of each entry (Recharts' <c>itemStyle</c>).</summary>
    public string? ItemStyle { get; init; }

    /// <summary>Classes added to the box (Recharts' <c>wrapperClassName</c>).</summary>
    public string? WrapperClassName { get; init; }

    /// <summary>Classes added to the label (Recharts' <c>labelClassName</c>).</summary>
    public string? LabelClassName { get; init; }

    /// <summary>Whether the box is announced to assistive technologies (Recharts' <c>accessibilityLayer</c>: <c>role="status"</c>, <c>aria-live</c>).</summary>
    public bool AccessibilityLayer { get; init; }
}

/// <summary>Recharts' <c>Legend</c> with its default content.</summary>
public sealed record LegendSpec
{
    /// <summary>The custom content of each entry (Recharts' <c>content</c> element, written per entry), compiled from its XAML template as HTML.</summary>
    public ContentSpec? Content { get; init; }

    /// <summary>The custom content of the whole legend (Recharts' <c>content</c> element that is not one item per entry), its entries where its <see cref="ContentElement.Items"/> node stands.</summary>
    public ContentSpec? Frame { get; init; }

    /// <summary>The key of the command its items run when clicked (Recharts' <c>onClick</c>), written as buttons on the web; none when no command is set.</summary>
    public string? ClickCommand { get; init; }

    /// <summary>The icons' size.</summary>
    public double IconSize { get; init; } = 14;

    /// <summary>Whether the items run in a row (<c>horizontal</c>) or a column.</summary>
    public bool Vertical { get; init; }

    /// <summary>The horizontal alignment: <c>left</c>, <c>center</c> or <c>right</c>.</summary>
    public string Align { get; init; } = "center";

    /// <summary>The vertical alignment: <c>top</c>, <c>middle</c> or <c>bottom</c>.</summary>
    public string VerticalAlign { get; init; } = "bottom";

    /// <summary>One icon for every item, overriding the series'.</summary>
    public LegendIcon? IconType { get; init; }

    /// <summary>The line height of the legend's items (Recharts' <c>wrapperStyle</c> <c>lineHeight</c>); the font's when null.</summary>
    public double? LineHeight { get; init; }

    /// <summary>Whether a click on an item hides or shows what it stands for (a Pictor feature; Recharts leaves clicks to the app).</summary>
    public bool Toggles { get; init; }

    /// <summary>The color of an inactive item (a hidden series' or a toggled one), Recharts' <c>inactiveColor</c>.</summary>
    public string? InactiveColor { get; init; }

    /// <summary>A .NET composite format for the items' texts (Recharts' <c>formatter</c>).</summary>
    public string? Format { get; init; }

    /// <summary>The legend's own entries instead of the series' (Recharts' <c>payload</c>); empty for the series'.</summary>
    public ImmutableArray<LegendPayloadSpec> Payload { get; init; } = [];

    /// <summary>The width of the legend's box (Recharts' <c>width</c>); the chart's width less its margins for a horizontal legend when null.</summary>
    public double? Width { get; init; }

    /// <summary>The height of a vertical legend's box (Recharts' <c>height</c>), which the plot gives it.</summary>
    public double? Height { get; init; }

    /// <summary>Whether entries of the same text are listed once (Recharts' <c>payloadUniqBy={true}</c>).</summary>
    public bool PayloadUniqBy { get; init; }

    /// <summary>CSS declarations of the legend's box (Recharts' <c>wrapperStyle</c>): its <c>left</c>, <c>right</c>, <c>top</c>, <c>bottom</c> and <c>line-height</c> place it on both targets, the rest is written on the web.</summary>
    public string? WrapperStyle { get; init; }
}

/// <summary>An entry of a legend's own payload (Recharts' <c>payload</c> item).</summary>
public sealed record LegendPayloadSpec
{
    /// <summary>The entry's text.</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>The icon's and the text's color.</summary>
    public string? Color { get; init; }

    /// <summary>The icon; a circle when null, as Recharts' <c>Symbols</c> draws an entry without a type.</summary>
    public LegendIcon? Type { get; init; }

    /// <summary>Whether the entry is drawn in the inactive color.</summary>
    public bool Inactive { get; init; }
}
