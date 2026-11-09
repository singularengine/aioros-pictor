using System.Collections.Immutable;

namespace Aioros.Pictor.Specs;

/// <summary>The SVG element a node of custom content writes.</summary>
public enum ContentElement
{
    /// <summary>A <c>g</c> holding the nodes below it (an Avalonia panel or border).</summary>
    Group,

    /// <summary>A nested <c>svg</c> with a view box (an Avalonia <c>Viewbox</c>).</summary>
    Svg,

    /// <summary>A <c>path</c> (an Avalonia <c>Path</c>).</summary>
    Path,

    /// <summary>A <c>rect</c> (an Avalonia <c>Rectangle</c>, a border's box).</summary>
    Rect,

    /// <summary>A <c>circle</c> (an Avalonia <c>Ellipse</c> as wide as it is tall).</summary>
    Circle,

    /// <summary>An <c>ellipse</c>.</summary>
    Ellipse,

    /// <summary>A <c>line</c>.</summary>
    Line,

    /// <summary>A <c>polyline</c>.</summary>
    Polyline,

    /// <summary>A <c>polygon</c>.</summary>
    Polygon,

    /// <summary>A <c>text</c> (Pictor's <c>Text</c>, an Avalonia <c>TextBlock</c>).</summary>
    Text,

    /// <summary>Recharts' <c>Sector</c>: a <c>path</c> drawn from its center, radii and angles.</summary>
    Sector,

    /// <summary>Recharts' <c>Rectangle</c>: a <c>path</c> drawn from its box and corner radius.</summary>
    Rectangle,

    /// <summary>Recharts' <c>Symbols</c>: a <c>path</c> drawn as d3's symbol of a type and an area.</summary>
    Symbols,

    /// <summary>Recharts' <c>Trapezoid</c>.</summary>
    Trapezoid,

    /// <summary>Recharts' <c>Cross</c>.</summary>
    Cross,

    /// <summary>Recharts' <c>Dot</c>: a <c>circle</c> with Recharts' class.</summary>
    Dot,

    /// <summary>Recharts' <c>Polygon</c>: a closed <c>path</c> through points.</summary>
    PolygonPath,

    /// <summary>An HTML <c>div</c> of a tooltip's or a legend's content (an Avalonia panel or border), its children in a column or a row.</summary>
    Division,

    /// <summary>An HTML <c>p</c> of a tooltip's or a legend's content (an Avalonia <c>TextBlock</c>).</summary>
    Paragraph,

    /// <summary>A legend's entries inside its whole content (Pictor's <c>LegendItems</c>): each entry's template, or Recharts' default list.</summary>
    Items,
}

/// <summary>How a part of a text with holes is written.</summary>
public enum ContentPartKind
{
    /// <summary>The literal text.</summary>
    Literal,

    /// <summary>A text member of the item.</summary>
    Text,

    /// <summary>A number of the item, written as JavaScript writes it.</summary>
    Number,

    /// <summary>A number of the item through the .NET composite format the part's literal holds.</summary>
    Formatted,
}

/// <summary>A number of custom content: a constant plus a multiple of each of some number members of the item (an affine function of the item's props).</summary>
public sealed record ContentNumber
{
    /// <summary>
    /// What the sum of the members' multiples is divided by before the constant is added: a template's division by a
    /// whole number (<c>Value/1000</c>) is kept as a division, so the number is the one the template computes to the last digit.
    /// </summary>
    public double Divisor { get; init; } = 1;

    /// <summary>The constant term.</summary>
    public double Constant { get; init; }

    /// <summary>The members it depends on, by their index among the item's numbers.</summary>
    public ImmutableArray<int> Members { get; init; } = [];

    /// <summary>The multiple of each member, in the order of <see cref="Members"/>.</summary>
    public ImmutableArray<double> Coefficients { get; init; } = [];
}

/// <summary>A part of a text with holes.</summary>
public sealed record ContentPart
{
    /// <summary>What the part writes.</summary>
    public ContentPartKind Kind { get; init; }

    /// <summary>The literal text, or the format of a formatted number.</summary>
    public string? Literal { get; init; }

    /// <summary>The text member a text part reads, by its index among the item's texts.</summary>
    public int Member { get; init; } = -1;

    /// <summary>The number a number part writes.</summary>
    public ContentNumber? Value { get; init; }
}

/// <summary>
/// A prop of a node of custom content (an SVG attribute), by its SVG name: a number, a list of numbers (a path's coordinates under
/// its commands, a polyline's points, a transform's numbers under its operations, a view box), a text, or a text with
/// holes. A value read whole from a member (a color, a list of points) names it in <see cref="Member"/>.
/// </summary>
public sealed record ContentProp
{
    /// <summary>The attribute's SVG name; <c>#text</c> for a text's content.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The attribute's numbers.</summary>
    public ImmutableArray<ContentNumber> Numbers { get; init; } = [];

    /// <summary>The attribute's literal text: a text value, a path's command letters, a transform's operations.</summary>
    public string? Text { get; init; }

    /// <summary>A text with holes, written part by part.</summary>
    public ImmutableArray<ContentPart> Parts { get; init; } = [];

    /// <summary>The text member (a color) or the points member the whole value is read from; -1 for none.</summary>
    public int Member { get; init; } = -1;
}

/// <summary>When a node is written: while a number of the item is above zero (or at least zero).</summary>
public sealed record ContentCondition
{
    /// <summary>The number compared with zero.</summary>
    public ContentNumber Value { get; init; } = new();

    /// <summary>Whether zero itself shows the node.</summary>
    public bool Inclusive { get; init; }
}

/// <summary>A node of custom content: an SVG element, its attributes and the nodes inside it.</summary>
public sealed record ContentNode
{
    /// <summary>The element it writes.</summary>
    public ContentElement Element { get; init; }

    /// <summary>The element's class, if any.</summary>
    public string? Class { get; init; }

    /// <summary>The element's attributes, in the order they are written.</summary>
    public ImmutableArray<ContentProp> Attributes { get; init; } = [];

    /// <summary>The nodes inside it.</summary>
    public ImmutableArray<ContentNode> Children { get; init; } = [];

    /// <summary>When it is written: while every condition holds; always when there is none.</summary>
    public ImmutableArray<ContentCondition> When { get; init; } = [];
}

/// <summary>A refused template's content as Avalonia laid it out for one item: its call's key and its nodes, all constant.</summary>
public sealed record ResolvedContent
{
    /// <summary>The call's key: the item's index and state, as the painter numbers its calls.</summary>
    public long Key { get; init; }

    /// <summary>The nodes.</summary>
    public ImmutableArray<ContentNode> Nodes { get; init; } = [];
}

/// <summary>
/// A slot's XAML template compiled to SVG with holes: its nodes, whose numbers are affine functions of the item's props
/// and whose texts and colors read the item's members, written per item without Avalonia. A template the compiler
/// refuses keeps the reason; the page is then rendered by Avalonia, and the chart's spec carries what Avalonia laid out
/// for each item (<see cref="ChartSpec.Resolved"/>).
/// </summary>
public sealed record ContentSpec
{
    /// <summary>The nodes, in order.</summary>
    public ImmutableArray<ContentNode> Nodes { get; init; } = [];

    /// <summary>The members of the item's data the template reads (<c>Payload[key]</c>), after the slot's own numbers and texts.</summary>
    public ImmutableArray<string> Keys { get; init; } = [];

    /// <summary>Why the template could not be compiled; null when it was.</summary>
    public string? Refused { get; init; }
}

/// <summary>Recharts' <c>Customized</c>: custom content drawn with the chart's layout, in its place among the chart's children.</summary>
public sealed record CustomizedSpec
{
    /// <summary>The content, compiled from its XAML template.</summary>
    public ContentSpec Content { get; init; } = new();

    /// <summary>Its order among the chart's children.</summary>
    public int Order { get; init; }
}
