using System.Buffers;
using System.Collections.Immutable;
using System.Globalization;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Text;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Content;

/// <summary>A node of HTML content placed: its box inside its wrapper (margins left out), and a paragraph's text and size.</summary>
internal readonly record struct HtmlBox(ContentNode Node, double X, double Y, double Width, double Height, string? Text, double FontSize);

/// <summary>What stands for a template's <see cref="ContentElement.Items"/> node (a legend's entries): its markup, and its height at a width.</summary>
internal interface IHtmlItems
{
    /// <summary>Writes the entries' markup.</summary>
    void Write(Svg.MarkupBuffer output);

    /// <summary>The entries' height when they fill a width.</summary>
    double Height(double width);

    /// <summary>The widest entry's width, which a wrapper shrunk to its content makes room for (a tooltip's lines); a legend's entries fill the legend's width instead.</summary>
    double Width { get; }
}

/// <summary>
/// A tooltip's or a legend's custom content as HTML: its <c>div</c>s laid out as flex columns or rows with their gap,
/// margins, paddings, borders and fixed or minimum sizes, a column's items stretched across it and a row's down it
/// unless they align themselves, a row's free space taken by its items' auto margins, its <c>p</c>s one line each,
/// measured with Inter's metrics as the browser lays them out, so the tooltip is placed by its size as Recharts places
/// it, and the desktop draws the same boxes.
/// </summary>
internal static class HtmlContent
{
    /// <summary>Writes the nodes as HTML, each with its own inline style, its text's holes filled, an items node written by <paramref name="items"/>.</summary>
    public static void Write(Svg.MarkupBuffer output, ImmutableArray<ContentNode> nodes, in ContentItem item, IHtmlItems? items = null) =>
        Write(output, nodes, item, items, null);

    private static void Write(Svg.MarkupBuffer output, ImmutableArray<ContentNode> nodes, in ContentItem item, IHtmlItems? items, string? direction)
    {
        foreach (var node in nodes)
        {
            if (!item.Shows(node))
            {
                continue;
            }

            if (node.Element == ContentElement.Items)
            {
                items?.Write(output);
                continue;
            }

            var paragraph = node.Element == ContentElement.Paragraph;
            Raw(output, paragraph ? "<p"u8 : "<div"u8);
            if (Painting.ClassNames.Tokens(node.Class) is { } classes)
            {
                Attribute(output, " class"u8, classes);
            }

            Attribute(output, " style"u8, Style(node, item, direction));
            Raw(output, ">"u8);
            if (paragraph)
            {
                Svg.Utf8Markup.Text(output, Text(node, item));
            }

            Write(output, node.Children, item, items, Direction(node, item));
            Raw(output, paragraph ? "</p>"u8 : "</div>"u8);
        }
    }

    /// <summary>A list item of the nodes: one paragraph becomes the item's own style and text; anything else is written inside it.</summary>
    public static void Item(Svg.MarkupBuffer output, ImmutableArray<ContentNode> nodes, in ContentItem item)
    {
        if (nodes is [{ Element: ContentElement.Paragraph, Children.IsEmpty: true } paragraph] && item.Shows(paragraph))
        {
            Raw(output, "<li"u8);
            if (Painting.ClassNames.Tokens(paragraph.Class) is { } classes)
            {
                Attribute(output, " class"u8, classes);
            }

            Attribute(output, " style"u8, Style(paragraph, item));
            Raw(output, ">"u8);
            Svg.Utf8Markup.Text(output, Text(paragraph, item));
            Raw(output, "</li>"u8);
            return;
        }

        Raw(output, "<li>"u8);
        Write(output, nodes, item);
        Raw(output, "</li>"u8);
    }

    /// <summary>The nodes inline in a box (a nest treemap's index): one paragraph as a <c>span</c> with its style and text, anything else as it is.</summary>
    public static void Inline(Svg.MarkupBuffer output, ImmutableArray<ContentNode> nodes, in ContentItem item)
    {
        if (nodes is [{ Element: ContentElement.Paragraph, Children.IsEmpty: true } paragraph])
        {
            if (item.Shows(paragraph))
            {
                Raw(output, "<span"u8);
                if (Painting.ClassNames.Tokens(paragraph.Class) is { } classes)
                {
                    Attribute(output, " class"u8, classes);
                }

                Attribute(output, " style"u8, Style(paragraph, item));
                Raw(output, ">"u8);
                Svg.Utf8Markup.Text(output, Text(paragraph, item));
                Raw(output, "</span>"u8);
            }

            return;
        }

        Write(output, nodes, item);
    }

    /// <summary>
    /// A node's inline style: a <c>div</c>'s flex direction, size, gap, box, background and shadow, a <c>p</c>'s line,
    /// font, color, size and weight, and how it aligns itself in its flex container's <paramref name="direction"/>.
    /// </summary>
    private static string Style(ContentNode node, in ContentItem item, string? direction = null)
    {
        var style = new System.Text.StringBuilder();
        if (node.Element == ContentElement.Division)
        {
            Division(style, node, item);
        }
        else
        {
            Paragraph(style, node, item);
        }

        style.Append("margin:").Append(EdgeText(node, "margin", item)).Append(';');
        if (Number(node, "opacity", item, 1) is var opacity and < 1)
        {
            style.Append(CultureInfo.InvariantCulture, $"opacity:{opacity};");
        }

        Alignment(style, node, item, direction);
        return style.ToString();
    }

    private static void Division(System.Text.StringBuilder style, ContentNode node, in ContentItem item)
    {
        style.Append("display:flex;flex-direction:").Append(Direction(node, item)).Append(';');
        if (Find(node, "width") is not null || Find(node, "height") is not null || Find(node, "min-width") is not null)
        {
            style.Append("box-sizing:border-box;");
        }

        Length(style, "width", Number(node, "width", item, 0));
        Length(style, "height", Number(node, "height", item, 0));
        Length(style, "min-width", Number(node, "min-width", item, 0));
        Length(style, "gap", Number(node, "gap", item, 0));
        Edges(style, "padding", node, item);
        if (Paint(node, "background", item) is { } background)
        {
            style.Append("background:").Append(background).Append(';');
        }

        if (Number(node, "border-width", item, 0) is var width and > 0 && Paint(node, "border-color", item) is { } border)
        {
            style.Append(CultureInfo.InvariantCulture, $"border:{Pixels(width)} {Value(node, "border-style", item) ?? "solid"} {border};");
        }

        Length(style, "border-radius", Number(node, "border-radius", item, 0));
        if (Value(node, "box-shadow", item) is { } shadow && IsShadow(shadow))
        {
            style.Append("box-shadow:").Append(shadow).Append(';');
        }
    }

    private static void Paragraph(System.Text.StringBuilder style, ContentNode node, in ContentItem item)
    {
        style.Append("white-space:nowrap;");
        if (Find(node, "line-height") is not null)
        {
            Length(style, "line-height", Number(node, "line-height", item, 0));
        }
        else
        {
            style.Append("line-height:normal;");
        }

        if (Value(node, "font-family", item) is { } family && Families(family) is { } names)
        {
            style.Append("font-family:").Append(names).Append(';');
        }

        if (Paint(node, "color", item) is { } color)
        {
            style.Append("color:").Append(color).Append(';');
        }

        if (Find(node, "font-size") is not null)
        {
            Length(style, "font-size", Number(node, "font-size", item, 0));
        }

        if (Find(node, "font-weight") is { } weight)
        {
            style.Append("font-weight:").Append(ContentMarkup.Value(weight, item)).Append(';');
        }
    }

    /// <summary>
    /// How a node aligns itself in its flex container, as Avalonia aligns a child in its stack panel: across a column
    /// with <c>align-self</c>, down a row with <c>align-self</c> and along it with auto margins (a right-aligned item
    /// at the row's end, as <c>margin-left:auto</c> puts it).
    /// </summary>
    private static void Alignment(System.Text.StringBuilder style, ContentNode node, in ContentItem item, string? direction)
    {
        if (direction is null)
        {
            return;
        }

        var row = direction == "row";
        if (Value(node, row ? "v-align" : "h-align", item) is { } cross)
        {
            style.Append("align-self:").Append(cross switch { "center" => "center", "left" or "top" => "flex-start", _ => "flex-end" }).Append(';');
        }

        if (row && Value(node, "h-align", item) is { } along and not "left")
        {
            style.Append(along == "center" ? "margin-left:auto;margin-right:auto;" : "margin-left:auto;");
        }
    }

    /// <summary>Whether a text is a list of CSS shadows as the desktop reads them: <c>inset</c>, four lengths and a color each.</summary>
    public static bool IsShadow(string shadow)
    {
        foreach (var part in shadow.Split(", "))
        {
            var tokens = part.Split(' ');
            var lengths = tokens[0] == "inset" ? tokens[1..] : tokens;
            if (lengths.Length != 5 || !lengths[..4].All(static length => double.TryParse(length.EndsWith("px", StringComparison.Ordinal) ? length[..^2] : length, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) || !Painting.CssValues.IsColor(lengths[4]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>A font family's names as CSS writes them: each family's name after its source (<c>avares://App/Assets/Fonts#Geist Mono</c> is <c>Geist Mono</c>).</summary>
    private static string? Families(string family)
    {
        var names = string.Join(", ", family.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(static part => part[(part.LastIndexOf('#') + 1)..].Trim()));
        return Painting.CssValues.IsFontFamily(names) ? names : null;
    }

    private static string Direction(ContentNode node, in ContentItem item) => Find(node, "direction") is { } direction ? ContentMarkup.Value(direction, item) ?? "column" : "column";

    private static string? Value(ContentNode node, string name, in ContentItem item) => Find(node, name) is { } prop ? ContentMarkup.Value(prop, item) : null;

    private static void Length(System.Text.StringBuilder style, string name, double value)
    {
        if (value != 0)
        {
            style.Append(name).Append(':').Append(Pixels(value)).Append(';');
        }
    }

    private static void Edges(System.Text.StringBuilder style, string name, ContentNode node, in ContentItem item)
    {
        if (Edge(node, name, item) is var (top, right, bottom, left) && (top != 0 || right != 0 || bottom != 0 || left != 0))
        {
            style.Append(name).Append(':').Append(EdgeText(node, name, item)).Append(';');
        }
    }

    private static string EdgeText(ContentNode node, string name, in ContentItem item)
    {
        var (top, right, bottom, left) = Edge(node, name, item);
        return $"{Pixels(top)} {Pixels(right)} {Pixels(bottom)} {Pixels(left)}";
    }

    private static string Pixels(double value) => value == 0 ? "0" : ContentTexts.Number(value) + "px";

    private static string? Paint(ContentNode node, string name, in ContentItem item) =>
        Find(node, name) is { } prop && Painting.CssValues.Valid(ContentMarkup.Value(prop, item)) is { } paint && paint != "none" ? paint : null;

    /// <summary>
    /// The size of the nodes laid out in a column, as the tooltip's or the legend's wrapper takes it (shrunk to its
    /// content); an items node fills the width its containers leave of <paramref name="width"/> (a legend's).
    /// </summary>
    public static (double Width, double Height) Measure(ImmutableArray<ContentNode> nodes, in ContentItem item, double fontSize, IHtmlItems? items = null, double width = 0)
    {
        var flow = new Flow(fontSize, items);
        var (measured, height) = (0.0, 0.0);
        foreach (var node in nodes)
        {
            if (item.Shows(node))
            {
                var (w, h) = Outer(node, item, flow, width);
                (measured, height) = (Math.Max(measured, w), height + h);
            }
        }

        return (measured, height);
    }

    /// <summary>The boxes of the nodes laid out in a column at a point, for the desktop to draw (an items node's box filling what its containers leave of <paramref name="width"/>).</summary>
    public static List<HtmlBox> Arrange(ImmutableArray<ContentNode> nodes, in ContentItem item, double fontSize, double x, double y, IHtmlItems? items = null, double width = 0)
    {
        var flow = new Flow(fontSize, items);
        var boxes = new List<HtmlBox>();
        var top = y;
        foreach (var node in nodes)
        {
            if (item.Shows(node))
            {
                top += Place(node, item, flow, x, top, width, boxes).Height;
            }
        }

        return boxes;
    }

    /// <summary>The boxes of the nodes laid out in a column of a width at a point, each stretched across it unless it aligns itself, as a flex column's items are (a tooltip's lines in its items node).</summary>
    public static List<HtmlBox> ArrangeAcross(ImmutableArray<ContentNode> nodes, in ContentItem item, double fontSize, double x, double y, double width)
    {
        var flow = new Flow(fontSize, null);
        var boxes = new List<HtmlBox>();
        var top = y;
        foreach (var node in nodes)
        {
            if (item.Shows(node))
            {
                var (w, h) = Outer(node, item, flow, width);
                var (offset, across) = Cross(node, Value(node, "h-align", item), width, w, "width");
                Place(node, item, flow, x + offset, top, width, boxes, (across, h));
                top += h;
            }
        }

        return boxes;
    }

    /// <summary>What sizes a layout: the font size, and what stands for an items node.</summary>
    private readonly record struct Flow(double FontSize, IHtmlItems? Items);

    /// <summary>
    /// Places a node with its margins at a point, in a container of an inner width, at the outer size its container
    /// gives it (its own size where it gives none); returns its outer size.
    /// </summary>
    private static (double Width, double Height) Place(ContentNode node, in ContentItem item, in Flow flow, double x, double y, double available, List<HtmlBox> boxes, (double Width, double Height)? size = null)
    {
        var fontSize = flow.FontSize;
        var (mt, mr, mb, ml) = Edge(node, "margin", item);
        var (width, height) = size ?? Outer(node, item, flow, available);
        var (left, top) = (x + ml, y + mt);
        var (innerWidth, innerHeight) = (width - ml - mr, height - mt - mb);
        if (node.Element == ContentElement.Items)
        {
            boxes.Add(new HtmlBox(node, left, top, innerWidth, innerHeight, null, fontSize));
            return (width, height);
        }

        if (node.Element == ContentElement.Paragraph)
        {
            boxes.Add(new HtmlBox(node, left, top, innerWidth, innerHeight, Text(node, item), Size(node, item, fontSize)));
            return (width, height);
        }

        boxes.Add(new HtmlBox(node, left, top, innerWidth, innerHeight, null, fontSize));
        var (pt, pr, pb, pl) = Edge(node, "padding", item);
        var border = Number(node, "border-width", item, 0);
        var row = Direction(node, item) == "row";
        var gap = Number(node, "gap", item, 0);
        var (cx, cy) = (left + border + pl, top + border + pt);
        var (contentWidth, contentHeight) = (innerWidth - pl - pr - (2 * border), innerHeight - pt - pb - (2 * border));
        var inner = Inner(node, item, available);
        var children = new List<(ContentNode Node, double Width, double Height)>();
        foreach (var child in node.Children)
        {
            if (item.Shows(child))
            {
                var (w, h) = Outer(child, item, flow, inner);
                children.Add((child, w, h));
            }
        }

        var share = row ? Share(children, item, contentWidth - children.Sum(static child => child.Width) - (Math.Max(children.Count - 1, 0) * gap)) : 0;
        foreach (var (child, w, h) in children)
        {
            if (row)
            {
                var along = Value(child, "h-align", item);
                cx += along is "right" or "center" ? share : 0;
                var (offset, height2) = Cross(child, Value(child, "v-align", item), contentHeight, h, "height");
                Place(child, item, flow, cx, cy + offset, inner, boxes, (w, height2));
                cx += w + gap + (along == "center" ? share : 0);
            }
            else
            {
                var (offset, width2) = Cross(child, Value(child, "h-align", item), contentWidth, w, "width");
                Place(child, item, flow, cx + offset, cy, inner, boxes, (width2, h));
                cy += h + gap;
            }
        }

        return (width, height);
    }

    /// <summary>What each auto margin of a row's items takes of its free space: a right-aligned item has one, a centered one two.</summary>
    private static double Share(List<(ContentNode Node, double Width, double Height)> children, in ContentItem item, double free)
    {
        var margins = 0;
        foreach (var child in children)
        {
            margins += Value(child.Node, "h-align", item) switch { "right" => 1, "center" => 2, _ => 0 };
        }

        return margins == 0 ? 0 : Math.Max(free, 0) / margins;
    }

    /// <summary>
    /// Where an item sits across its flex line and how long it is across it: at the line's start, center or end as it
    /// aligns itself, or stretched across it (a division without a fixed size; a paragraph keeps its line).
    /// </summary>
    private static (double Offset, double Length) Cross(ContentNode node, string? align, double line, double length, string fixedName) => align switch
    {
        "center" => ((line - length) / 2, length),
        "right" or "bottom" => (line - length, length),
        "left" or "top" => (0, length),
        _ when node.Element == ContentElement.Division && Find(node, fixedName) is null => (0, Math.Max(line, length)),
        _ => (0, length),
    };

    /// <summary>The width a division leaves its children of the width it is given: less its margins, paddings and borders.</summary>
    private static double Inner(ContentNode node, in ContentItem item, double available)
    {
        var (_, mr, _, ml) = Edge(node, "margin", item);
        var (_, pr, _, pl) = Edge(node, "padding", item);
        return Math.Max(available - ml - mr - pl - pr - (2 * Number(node, "border-width", item, 0)), 0);
    }

    /// <summary>A node's size with its margins, in a container of an inner width (which an items node fills); a division's fixed or minimum size holds its border box.</summary>
    private static (double Width, double Height) Outer(ContentNode node, in ContentItem item, in Flow flow, double available)
    {
        var fontSize = flow.FontSize;
        var (mt, mr, mb, ml) = Edge(node, "margin", item);
        if (node.Element == ContentElement.Items)
        {
            var filled = Math.Max(available, flow.Items?.Width ?? 0);
            return (filled, flow.Items?.Height(filled) ?? 0);
        }

        if (node.Element == ContentElement.Paragraph)
        {
            var size = Size(node, item, fontSize);
            var line = Find(node, "line-height") is not null ? Number(node, "line-height", item, 0) : InterMetrics.LineHeight(size);
            return (InterMetrics.Width(Text(node, item) ?? string.Empty, size) + ml + mr, line + mt + mb);
        }

        var row = Direction(node, item) == "row";
        var gap = Number(node, "gap", item, 0);
        var inner = Inner(node, item, available);
        var (width, height, count) = (0.0, 0.0, 0);
        foreach (var child in node.Children)
        {
            if (item.Shows(child))
            {
                var (w, h) = Outer(child, item, flow, inner);
                (width, height) = row ? (width + w, Math.Max(height, h)) : (Math.Max(width, w), height + h);
                count++;
            }
        }

        var gaps = Math.Max(count - 1, 0) * gap;
        (width, height) = row ? (width + gaps, height) : (width, height + gaps);
        var (pt, pr, pb, pl) = Edge(node, "padding", item);
        var border = 2 * Number(node, "border-width", item, 0);
        var (boxWidth, boxHeight) = (width + pl + pr + border, height + pt + pb + border);
        boxWidth = Find(node, "width") is not null ? Math.Max(Number(node, "width", item, 0), pl + pr + border) : Math.Max(boxWidth, Number(node, "min-width", item, 0));
        boxHeight = Find(node, "height") is not null ? Math.Max(Number(node, "height", item, 0), pt + pb + border) : boxHeight;
        return (boxWidth + ml + mr, boxHeight + mt + mb);
    }

    private static string? Text(ContentNode node, in ContentItem item) => Find(node, ContentNames.Content) is { } text ? ContentMarkup.Value(text, item) : null;

    private static double Size(ContentNode node, in ContentItem item, double fontSize) => Number(node, "font-size", item, fontSize);

    private static (double Top, double Right, double Bottom, double Left) Edge(ContentNode node, string name, in ContentItem item) =>
        Find(node, name) is { Numbers.Length: 4 } prop
            ? (item.Evaluate(prop.Numbers[0]), item.Evaluate(prop.Numbers[1]), item.Evaluate(prop.Numbers[2]), item.Evaluate(prop.Numbers[3]))
            : (0, 0, 0, 0);

    private static double Number(ContentNode node, string name, in ContentItem item, double fallback) =>
        Find(node, name) is { Numbers.Length: > 0 } prop ? item.Evaluate(prop.Numbers[0]) : fallback;

    private static ContentProp? Find(ContentNode node, string name)
    {
        foreach (var prop in node.Attributes)
        {
            if (prop.Name == name)
            {
                return prop;
            }
        }

        return null;
    }
}
