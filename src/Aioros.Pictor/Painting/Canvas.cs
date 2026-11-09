using Aioros.Graphs.Paths;
using Aioros.Pictor.Layout;

namespace Aioros.Pictor.Painting;

/// <summary>How a shape is stroked and filled; a null color is left out (Recharts' default for the element applies).</summary>
internal readonly record struct Paint(string? Stroke, double StrokeWidth, string? Fill, double FillOpacity = double.NaN, string? Dash = null, double StrokeOpacity = double.NaN)
{
    /// <summary>The shape's opacity, or NaN.</summary>
    public double Opacity { get; init; } = double.NaN;

    /// <summary>The stroke's ends (SVG's <c>stroke-linecap</c>), or null.</summary>
    public string? LineCap { get; init; }

    /// <summary>The stroke's corners (SVG's <c>stroke-linejoin</c>), or null.</summary>
    public string? LineJoin { get; init; }

    /// <summary>The stroke's miter limit, or NaN.</summary>
    public double MiterLimit { get; init; } = double.NaN;

    /// <summary>The paint with an element's presentation attributes over it (Recharts spreads them after its own props).</summary>
    public Paint With(Specs.SvgPresentation? presentation) => presentation is null ? this : this with
    {
        Opacity = presentation.Opacity ?? Opacity,
        StrokeOpacity = presentation.StrokeOpacity ?? StrokeOpacity,
        LineCap = presentation.StrokeLinecap ?? LineCap,
        LineJoin = presentation.StrokeLinejoin ?? LineJoin,
        MiterLimit = presentation.StrokeMiterlimit ?? MiterLimit,
    };

    public static Paint Stroked(string stroke, double width = double.NaN, string? dash = null) => new(stroke, width, "none", double.NaN, dash);

    public static Paint Filled(string fill, double opacity = double.NaN) => new(null, double.NaN, fill, opacity);
}

/// <summary>
/// How a text is drawn: its anchor, its vertical shift in ems (of its first line; a text of several lines, separated by
/// <c>\n</c>, advances 1 em per line as Recharts' <c>Text</c> does), its color and size, its rotation, whether the web
/// writes Recharts' <c>alignment-baseline="middle"</c> (which Chromium ignores on a <c>text</c>, so nothing moves), and
/// whether its size is its own (written as <c>font-size</c>) rather than the chart's; a label's weight and stroke are
/// written as attributes too.
/// </summary>
internal readonly record struct TextPaint(TextAnchor Anchor, double Dy, string Fill, double FontSize, double Angle = 0, bool MiddleBaseline = false, bool OwnSize = false)
{
    /// <summary>The text's stroke, or null.</summary>
    public string? Stroke { get; init; }

    /// <summary>The text's stroke width, or NaN.</summary>
    public double StrokeWidth { get; init; } = double.NaN;

    /// <summary>The text's weight, or null.</summary>
    public string? FontWeight { get; init; }

    /// <summary>The text's style (<c>italic</c>), or null.</summary>
    public string? FontStyle { get; init; }

    /// <summary>The text's opacity, or NaN.</summary>
    public double Opacity { get; init; } = double.NaN;

    /// <summary>The text with an element's presentation attributes over it: its weight, style and opacity.</summary>
    public TextPaint With(Specs.SvgPresentation? presentation) => presentation is null ? this : this with
    {
        FontWeight = presentation.FontWeight ?? FontWeight,
        FontStyle = presentation.FontStyle ?? FontStyle,
        Opacity = presentation.Opacity ?? Opacity,
    };
}

internal enum TextAnchor
{
    Start,
    Middle,
    End,
}

/// <summary>A shape that writes its path into any sink: curves, areas and rectangles.</summary>
internal interface IShape
{
    /// <summary>The fraction digits the web writes its coordinates with: 3 as d3-path rounds them, null for every digit.</summary>
    static virtual int? Digits => 3;

    void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink;
}

/// <summary>
/// An arc of 359° that a text follows: from <paramref name="Angle"/> at <paramref name="Radius"/> around the center,
/// towards greater angles (counterclockwise on the screen, SVG's sweep flag 0) when <paramref name="Counterclockwise"/>.
/// </summary>
internal readonly record struct ArcPath(double Cx, double Cy, double Radius, double Angle, bool Counterclockwise)
{
    public double EndAngle => Angle + (Counterclockwise ? 359 : -359);
}

/// <summary>A rectangle that elements are clipped to (Recharts' <c>clipPath</c> of a <c>rect</c>).</summary>
internal readonly record struct ClipRect(double X, double Y, double Width, double Height);

/// <summary>The coordinate a tooltip box follows the pointer along, or none for a box fixed at its coordinate.</summary>
internal enum TipAxis : byte
{
    None,
    X,
    Y,

    /// <summary>A polar chart's box at the pointer's distance from the center along its category's angle (Recharts' <c>centric</c> layout: radars, centric radial bars).</summary>
    Radius,

    /// <summary>A polar chart's box at the pointer's angle on its category's radius (Recharts' <c>radial</c> layout: radial bars).</summary>
    Angle,
}

/// <summary>
/// How a tooltip box follows the pointer (Recharts' axis tooltip, whose <c>activeCoordinate</c> keeps the pointer's
/// coordinate across the category axis): along which coordinate, over which view box (its start and length along it),
/// where it stands without a pointer (the coordinate the box is placed from then), and the box's measured extent along it;
/// a polar box follows along both coordinates, from its category's angle or radius (<paramref name="Param"/>), and
/// carries its measured width too.
/// </summary>
internal readonly record struct TipFollow(TipAxis Axis, double Start, double Length, double Rest, double Size = 1, double Param = 0, double Width = 1)
{
    /// <summary>Whether the box follows along both coordinates (a polar chart's).</summary>
    public bool Polar => Axis is TipAxis.Radius or TipAxis.Angle;
}

/// <summary>A tooltip's content: the hovered category and one entry per series.</summary>
internal readonly record struct TooltipEntry(string Name, string Value, string Color, int Target = -1);

/// <summary>
/// Where the painter draws: the SVG writer on the web, Avalonia's drawing context on the desktop. The painter is
/// generic over it, so every call is specialized for the target.
/// </summary>
internal interface IChartCanvas
{
    void Begin(IChartPlot plot);

    void Open(ReadOnlySpan<byte> classes);

    /// <summary>The tab index the next layer takes on the web (Recharts' pie's <c>rootTabIndex</c>, its sectors' <c>-1</c>); nothing on the desktop.</summary>
    void TabIndex(int index)
    {
    }

    /// <summary>Opens a layer drawn at an opacity as a whole (an error bar's group with Recharts' spread <c>opacity</c>); NaN opens a plain layer.</summary>
    void OpenFaded(ReadOnlySpan<byte> classes, double opacity) => Open(classes);

    /// <summary>Opens a group the page names as a slider (a brush traveller: Recharts' <c>role="slider"</c>, <c>aria-label</c> and <c>aria-valuenow</c>); <see cref="Close"/> ends it.</summary>
    void OpenSlider(ReadOnlySpan<byte> classes, string label, double value);

    /// <summary>Opens a series' layer that animates in with its timing (Recharts' enter animation); <see cref="Close"/> ends it.</summary>
    void OpenMotion(ReadOnlySpan<byte> classes, in Motion.Timing timing);

    /// <summary>How the next shape or layer moves while its series animates in.</summary>
    void Animate(in Motion.Step step);

    /// <summary>An arc that grows a sector while its series animates in (the web's; the desktop grows the sectors themselves).</summary>
    void Grow(in Motion.GrowingArc arc);

    /// <summary>How the next animated series' layer moves on the web when the framework morphs it into new data (nothing on the desktop, which plays its own update).</summary>
    void MorphLayer(in Motion.MorphLayer layer);

    /// <summary>The numbers the next shape or circle moves by on the web when the framework morphs it into new data (nothing on the desktop).</summary>
    void Morph(in Motion.MorphMark mark);

    void Close();

    void Line(double x1, double y1, double x2, double y2, in Paint paint, ReadOnlySpan<byte> classes);

    void Shape<TShape>(in TShape shape, in Paint paint, ReadOnlySpan<byte> classes)
        where TShape : struct, IShape;

    void Circle(double cx, double cy, double r, in Paint paint, ReadOnlySpan<byte> classes);

    void Text(double x, double y, string text, in TextPaint paint, ReadOnlySpan<byte> classes);

    void EndSurface();

    void Legend(LegendLayout legend);

    /// <summary>The area that shows a category's tooltip while hovered (the web's hit target; nothing on the desktop).</summary>
    void Hit(double x, double y, double width, double height);

    /// <summary>A shape that shows an item's tooltip while hovered (the web's hit target; nothing on the desktop).</summary>
    void HitShape<TShape>(in TShape shape)
        where TShape : struct, IShape;

    /// <summary>A tooltip box at its place (and, on the web, following the pointer as <paramref name="follow"/> says).</summary>
    void Tooltip(double x, double y, string label, ReadOnlySpan<TooltipEntry> entries, Specs.TooltipSpec spec, in TipFollow follow);

    /// <summary>A text along a circular arc (Recharts' radial bar label on a <c>textPath</c>), centered on the arc's line.</summary>
    void ArcText(in ArcPath arc, string text, in TextPaint paint, ReadOnlySpan<byte> classes);

    void End();

    /// <summary>A bare <c>text</c> element at its baseline, anchored at its start, painted as its surface (black unless the chart sets a fill or a stroke, a treemap's names).</summary>
    void PlainText(double x, double y, string? text, double fontSize, string? fill, string? stroke);

    /// <summary>Recharts' nest index of a treemap below its surface, which ends at <paramref name="top"/> in a chart of the given size: the boxes of its trail (the web also writes the boxes off the trail, hidden, when they zoom through commands), each its name or its custom content.</summary>
    void NestIndex(double top, double width, double height, NestBox[] trail, Specs.ContentSpec? content);

    /// <summary>Opens a focusable group named for assistive technologies (a sunburst node's <c>g</c> with <c>aria-label</c> and <c>tabindex</c>); <see cref="Close"/> ends it.</summary>
    void OpenItem(string name);

    /// <summary>Recharts' <c>Text</c> centered on a point with SVG text props of its own (a sunburst's <c>textOptions</c>).</summary>
    void OptionsText(double x, double y, string text, Specs.TextOptionsSpec options);

    /// <summary>A <c>rect</c> element (a brush's background, slide and travellers).</summary>
    void Rect(double x, double y, double width, double height, in Paint paint, ReadOnlySpan<byte> classes);

    /// <summary>Defines a rectangle that elements drawn after it may be clipped to (an SVG <c>clipPath</c> in a <c>defs</c>).</summary>
    void DefineClip(in ClipRect clip);

    /// <summary>Opens a nested surface at a place in the chart (Recharts' compact chart, a brush's panorama), drawn in its own coordinates and clipped to its size.</summary>
    void OpenSurface(double x, double y, double width, double height);

    /// <summary>Closes the nested surface.</summary>
    void CloseSurface();

    /// <summary>The brush's travellers as the fields the web posts (range inputs over the strip); nothing on the desktop, which drags them.</summary>
    void BrushFields(BrushLayout brush, string startText, string endText);

    /// <summary>Clips the next layer (until it is closed) or the next shape, line, circle or rectangle to a defined rectangle.</summary>
    void ClipNext(in ClipRect clip);

    /// <summary>Custom content (a slot's XAML template compiled to SVG with holes) for one item, in place of its default shape.</summary>
    void Content(Specs.ContentSpec content, in Content.ContentItem item);

    /// <summary>A tooltip's custom content as HTML (a slot's XAML template compiled with holes), its wrapper's corner at a point, following the pointer as the tooltip's default box does.</summary>
    void Html(double x, double y, Specs.ContentSpec content, in Content.ContentItem item, Specs.TooltipSpec tooltip, in TipFollow follow, TooltipLines? lines);
}
