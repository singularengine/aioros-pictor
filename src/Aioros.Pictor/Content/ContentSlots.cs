using System.Collections.Immutable;

namespace Aioros.Pictor.Content;

/// <summary>What a member of a template's context holds, which tells the compiler how to probe it.</summary>
internal enum MemberKind
{
    /// <summary>A number.</summary>
    Number,

    /// <summary>A whole number (an index, a depth).</summary>
    Whole,

    /// <summary>A flag, held as 0 or 1.</summary>
    Flag,

    /// <summary>A text.</summary>
    Text,

    /// <summary>A color, as Recharts takes it (<c>#8884d8</c>, <c>red</c>).</summary>
    Color,

    /// <summary>A text anchor: <c>start</c>, <c>middle</c> or <c>end</c>.</summary>
    Anchor,
}

/// <summary>A member of a template's context: its name in XAML and what it holds.</summary>
internal readonly record struct ContentMember(string Name, MemberKind Kind);

/// <summary>
/// The props Recharts passes to one kind of custom content, as the members of its template's context: its numbers, its
/// texts, and whether it carries a list of points. The painter fills them per item in this order; a compiled template's
/// holes name them by index.
/// </summary>
internal sealed record ContentSlot(string Name, ImmutableArray<ContentMember> Numbers, ImmutableArray<ContentMember> Texts, bool Points = false)
{
    /// <summary>Whether the slot's content is HTML (a tooltip's or a legend's) rather than SVG.</summary>
    public bool Html { get; init; }

    /// <summary>The index of a number member by its name, or -1.</summary>
    public int IndexOfNumber(string name) => IndexOf(Numbers, name);

    /// <summary>The index of a text member by its name, or -1.</summary>
    public int IndexOfText(string name) => IndexOf(Texts, name);

    private static int IndexOf(ImmutableArray<ContentMember> members, string name)
    {
        for (var i = 0; i < members.Length; i++)
        {
            if (members[i].Name == name)
            {
                return i;
            }
        }

        return -1;
    }
}

/// <summary>The slots of custom content, with their members in the order the painter fills them.</summary>
internal static class ContentSlots
{
    private static ContentMember N(string name) => new(name, MemberKind.Number);

    private static ContentMember W(string name) => new(name, MemberKind.Whole);

    private static ContentMember F(string name) => new(name, MemberKind.Flag);

    private static ContentMember T(string name) => new(name, MemberKind.Text);

    private static ContentMember C(string name) => new(name, MemberKind.Color);

    private static ContentMember A(string name) => new(name, MemberKind.Anchor);

    /// <summary>A bar's <c>shape</c> and <c>activeBar</c>: its rectangle, value, index and paint, and its background's rectangle.</summary>
    public static ContentSlot Bar { get; } = new(nameof(Bar),
        [N("X"), N("Y"), N("Width"), N("Height"), N("Value"), W("Index"), N("BackgroundX"), N("BackgroundY"), N("BackgroundWidth"), N("BackgroundHeight")],
        [C("Fill"), C("Stroke"), T("Name"), T("DataKey")]);

    /// <summary>A line's or an area's <c>dot</c> and <c>activeDot</c>: its center, radius, paint, value and index.</summary>
    public static ContentSlot Dot { get; } = new(nameof(Dot),
        [N("Cx"), N("Cy"), N("R"), N("StrokeWidth"), N("Value"), W("Index")],
        [C("Fill"), C("Stroke"), T("Name"), T("DataKey")]);

    /// <summary>
    /// A label's <c>content</c>: the box it is placed around (a point's box is empty; a polar label's point is where the
    /// default label sits, with its sector's center, radii and angles), its value and text, its own fill and its series' name.
    /// </summary>
    public static ContentSlot Label { get; } = new(nameof(Label),
        [N("X"), N("Y"), N("Width"), N("Height"), N("Value"), W("Index"), N("Offset"), N("Cx"), N("Cy"), N("InnerRadius"), N("OuterRadius"), N("StartAngle"), N("EndAngle")],
        [T("Text"), C("Fill"), T("Name")]);

    /// <summary>
    /// An axis' <c>tick</c>: the label's point, the axis' box, the tick's index and coordinate, its text and paint; a polar
    /// axis' also the chart's center, the angle axis' radius and the radius axis' angle.
    /// </summary>
    public static ContentSlot Tick { get; } = new(nameof(Tick),
        [N("X"), N("Y"), N("Width"), N("Height"), W("Index"), N("Coordinate"), W("VisibleTicksCount"), N("Cx"), N("Cy"), N("Radius"), N("Angle")],
        [T("Value"), C("Fill"), C("Stroke"), A("TextAnchor"), T("Orientation")]);

    /// <summary>
    /// A pie's or a radial bar's <c>shape</c> and <c>activeShape</c>: Recharts' sector props, with the cosine and sine of
    /// the middle angle (as Recharts' examples compute them), its side (1 on the right, -1 on the left), the point on the
    /// outer radius at the middle angle, and the text anchor of that side.
    /// </summary>
    public static ContentSlot Sector { get; } = new(nameof(Sector),
        [N("Cx"), N("Cy"), N("InnerRadius"), N("OuterRadius"), N("StartAngle"), N("EndAngle"), N("MidAngle"), N("Percent"), N("Value"), W("Index"), N("CornerRadius"),
            N("Cos"), N("Sin"), N("Side"), N("OuterX"), N("OuterY")],
        [C("Fill"), C("Stroke"), T("Name"), A("TextAnchor")]);

    /// <summary>A funnel's <c>shape</c> and <c>activeShape</c>: Recharts' trapezoid props.</summary>
    public static ContentSlot Trapezoid { get; } = new(nameof(Trapezoid),
        [N("X"), N("Y"), N("UpperWidth"), N("LowerWidth"), N("Height"), N("Value"), W("Index"), N("Percent")],
        [C("Fill"), C("Stroke"), T("Name")]);

    /// <summary>A scatter's <c>shape</c> and <c>activeShape</c>: the symbol's center and box, its area and the point's values.</summary>
    public static ContentSlot Scatter { get; } = new(nameof(Scatter),
        [N("Cx"), N("Cy"), N("X"), N("Y"), N("Width"), N("Height"), N("Size"), W("Index"), N("XValue"), N("YValue"), N("ZValue")],
        [C("Fill"), C("Stroke"), T("Name")]);

    /// <summary>A radar's <c>shape</c>: its center, its points, and its paint.</summary>
    public static ContentSlot Radar { get; } = new(nameof(Radar),
        [N("Cx"), N("Cy"), N("FillOpacity")],
        [C("Fill"), C("Stroke"), T("Name")], Points: true);

    /// <summary>A treemap's <c>content</c>: a node's box, depth, index and value, whether it has children, and its name and paint.</summary>
    public static ContentSlot TreemapNode { get; } = new(nameof(TreemapNode),
        [N("X"), N("Y"), N("Width"), N("Height"), W("Depth"), W("Index"), N("Value"), F("HasChildren"), F("IsLeaf")],
        [T("Name"), C("Fill"), C("Stroke")]);

    /// <summary>A treemap's <c>nestIndexContent</c>: an entry of the trail, its index and name.</summary>
    public static ContentSlot NestIndex { get; } = new(nameof(NestIndex),
        [W("Index"), W("Depth")],
        [T("Name")]) { Html = true };

    /// <summary>A Sankey's <c>node</c>: its box, index, value and depth, the chart's width, and its name and paint.</summary>
    public static ContentSlot SankeyNode { get; } = new(nameof(SankeyNode),
        [N("X"), N("Y"), N("Width"), N("Height"), W("Index"), N("Value"), W("Depth"), N("ContainerWidth")],
        [T("Name"), C("Fill"), C("Stroke")]);

    /// <summary>A Sankey's <c>link</c>: Recharts' link props (its ends, their control points and its width), its index and value, and its nodes' names.</summary>
    public static ContentSlot SankeyLink { get; } = new(nameof(SankeyLink),
        [N("SourceX"), N("SourceY"), N("SourceControlX"), N("TargetX"), N("TargetY"), N("TargetControlX"), N("LinkWidth"), W("Index"), N("Value")],
        [T("SourceName"), T("TargetName"), C("Stroke")]);

    /// <summary>A reference line's <c>shape</c>: its segment and paint.</summary>
    public static ContentSlot ReferenceLine { get; } = new(nameof(ReferenceLine),
        [N("X1"), N("Y1"), N("X2"), N("Y2"), N("StrokeWidth")],
        [C("Stroke"), C("Fill")]);

    /// <summary>A reference area's <c>shape</c>: its rectangle and paint.</summary>
    public static ContentSlot ReferenceArea { get; } = new(nameof(ReferenceArea),
        [N("X"), N("Y"), N("Width"), N("Height")],
        [C("Fill"), C("Stroke")]);

    /// <summary>A reference dot's <c>shape</c>: its center, radius and paint.</summary>
    public static ContentSlot ReferenceDot { get; } = new(nameof(ReferenceDot),
        [N("Cx"), N("Cy"), N("R")],
        [C("Fill"), C("Stroke")]);

    /// <summary>A brush's <c>traveller</c>: its rectangle and paint.</summary>
    public static ContentSlot Traveller { get; } = new(nameof(Traveller),
        [N("X"), N("Y"), N("Width"), N("Height")],
        [C("Fill"), C("Stroke")]);

    /// <summary>A tooltip's <c>content</c>: the point it is anchored at, the active item's index, how many lines the default content would show, the label, and the name, value and color of its first four lines.</summary>
    public static ContentSlot Tooltip { get; } = new(nameof(Tooltip),
        [N("X"), N("Y"), W("Index"), W("Count")],
        [T("Label"), T("Name"), T("Value"), C("Color"), T("Name2"), T("Value2"), C("Color2"), T("Name3"), T("Value3"), C("Color3"), T("Name4"), T("Value4"), C("Color4")]) { Html = true };

    /// <summary>A tooltip's line for its <c>ItemTemplate</c>, one per line of the payload where the content's <c>TooltipItems</c> stands (Recharts' <c>payload.map</c>): its index, name, value and color.</summary>
    public static ContentSlot TooltipItem { get; } = new(nameof(TooltipItem),
        [W("Index")],
        [T("Name"), T("Value"), C("Color")]) { Html = true };

    /// <summary>How many of a tooltip's lines its content's members carry.</summary>
    public const int TooltipLines = 4;

    /// <summary>A legend's <c>content</c>, for each entry: its index, whether it is toggled off, its text, color, icon and data key.</summary>
    public static ContentSlot LegendEntry { get; } = new(nameof(LegendEntry),
        [W("Index"), F("Inactive")],
        [T("Value"), C("Color"), T("Type"), T("DataKey")]) { Html = true };

    /// <summary>A legend's whole content: how many entries it lists and its width, its alignment and layout.</summary>
    public static ContentSlot Legend { get; } = new(nameof(Legend),
        [W("Count"), N("Width")],
        [T("Align"), T("Layout")]) { Html = true };

    /// <summary>A <c>Customized</c> child: the chart's size and its plot's box.</summary>
    public static ContentSlot Layout { get; } = new(nameof(Layout),
        [N("Width"), N("Height"), N("Left"), N("Top"), N("PlotWidth"), N("PlotHeight")],
        []);
}
