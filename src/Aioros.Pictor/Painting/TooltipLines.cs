using System.Collections.Immutable;
using Aioros.Pictor.Content;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Painting;

/// <summary>
/// A tooltip's lines where its content's items node stands (Pictor's <c>TooltipItems</c>): its <c>ItemTemplate</c> once
/// per line of the payload, as shadcn/ui's <c>ChartTooltipContent</c> maps <c>payload</c> to rows, in a flex column with
/// the node's gap, each line stretched across the column.
/// </summary>
internal sealed class TooltipLines(ContentSpec item, TooltipEntry[] entries, double fontSize, Data.ChartData? data, double gap) : IHtmlItems
{
    private readonly ContentValues _values = new(ContentSlots.TooltipItem, item, data);

    /// <summary>The lines of a tooltip whose content holds an items node, or null.</summary>
    public static TooltipLines? Of(TooltipSpec spec, ContentSpec content, ReadOnlySpan<TooltipEntry> entries, double fontSize, Data.ChartData? data) =>
        spec.ItemContent is { } item && Gap(content.Nodes) is { } gap ? new TooltipLines(item, entries.ToArray(), fontSize, data, gap) : null;

    public double Width
    {
        get
        {
            var width = 0.0;
            for (var i = 0; i < entries.Length; i++)
            {
                width = Math.Max(width, HtmlContent.Measure(item.Nodes, Item(i), fontSize).Width);
            }

            return width;
        }
    }

    public double Height(double width)
    {
        var height = Math.Max(entries.Length - 1, 0) * gap;
        for (var i = 0; i < entries.Length; i++)
        {
            height += HtmlContent.Measure(item.Nodes, Item(i), fontSize).Height;
        }

        return height;
    }

    public void Write(Svg.MarkupBuffer output)
    {
        Raw(output, "<div style=\"display:flex;flex-direction:column;gap:"u8);
        Number(output, gap);
        Raw(output, "px\">"u8);
        for (var i = 0; i < entries.Length; i++)
        {
            HtmlContent.Write(output, item.Nodes, Item(i));
        }

        Raw(output, "</div>"u8);
    }

    /// <summary>How many lines there are.</summary>
    public int Count => entries.Length;

    /// <summary>A line's boxes in a column at a point, for the desktop to draw at once (the line's item shares its buffers with the next), and the next line's top.</summary>
    public List<HtmlBox> Arrange(int index, double x, double y, double width, out ContentItem line, out double next)
    {
        line = Item(index);
        next = y + HtmlContent.Measure(item.Nodes, line, fontSize).Height + gap;
        return HtmlContent.ArrangeAcross(item.Nodes, line, fontSize, x, y, width);
    }

    private ContentItem Item(int index) => ContentProps.TooltipItem(_values, index, entries[index]);

    private static double? Gap(ImmutableArray<ContentNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.Element == ContentElement.Items)
            {
                return node.Attributes.FirstOrDefault(static prop => prop.Name == "gap") is { Numbers: [var number, ..] } ? number.Constant : 0;
            }

            if (Gap(node.Children) is { } gap)
            {
                return gap;
            }
        }

        return null;
    }
}
