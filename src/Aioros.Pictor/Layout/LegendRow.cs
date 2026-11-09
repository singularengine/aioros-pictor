using System.Collections.Immutable;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>An entry's box in a legend's row: where it sits from the row's start, and its size.</summary>
internal readonly record struct LegendBox(double X, double Y, double Width, double Height);

/// <summary>
/// A framed legend's entries' templates in a wrapping row, as CSS's <c>flex-wrap</c> lays them out (shadcn/ui's
/// <c>ChartLegendContent</c>): each entry as wide as its content or its share of the row, a line broken where the next
/// entry and its gap pass the width, each line aligned as the legend, lines and entries the gap apart.
/// </summary>
internal readonly record struct LegendRow(double Gap, double Basis)
{
    /// <summary>The row the frame's items node asks for, or null when it lists the entries.</summary>
    public static LegendRow? Of(ContentSpec? frame) => frame is null ? null : Find(frame.Nodes);

    private static LegendRow? Find(ImmutableArray<ContentNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.Element == ContentElement.Items)
            {
                return Prop(node, "direction")?.Text == "row" ? new LegendRow(Number(node, "gap"), Number(node, "basis")) : null;
            }

            if (Find(node.Children) is { } row)
            {
                return row;
            }
        }

        return null;
    }

    private static ContentProp? Prop(ContentNode node, string name) => node.Attributes.FirstOrDefault(prop => prop.Name == name);

    private static double Number(ContentNode node, string name) => Prop(node, name) is { Numbers: [var number, ..] } ? number.Constant : 0;

    /// <summary>The entries' boxes in a row of a width, from the sizes of their content, and the row's height.</summary>
    public (List<LegendBox> Boxes, double Height) Lay(IReadOnlyList<(double Width, double Height)> sizes, double width, string align)
    {
        var lines = new List<(int Start, int End, double Width, double Height)>();
        var (start, lineWidth, lineHeight) = (0, 0.0, 0.0);
        for (var i = 0; i < sizes.Count; i++)
        {
            var entry = Width(sizes[i], width);
            if (i > start && lineWidth + Gap + entry > width)
            {
                lines.Add((start, i, lineWidth, lineHeight));
                (start, lineWidth, lineHeight) = (i, entry, sizes[i].Height);
                continue;
            }

            (lineWidth, lineHeight) = (i == start ? entry : lineWidth + Gap + entry, Math.Max(lineHeight, sizes[i].Height));
        }

        if (sizes.Count > 0)
        {
            lines.Add((start, sizes.Count, lineWidth, lineHeight));
        }

        var boxes = new List<LegendBox>(sizes.Count);
        var top = 0.0;
        foreach (var line in lines)
        {
            var x = align switch { "left" => 0, "right" => width - line.Width, _ => (width - line.Width) / 2 };
            for (var i = line.Start; i < line.End; i++)
            {
                var entry = Width(sizes[i], width);
                boxes.Add(new LegendBox(x, top, entry, line.Height));
                x += entry + Gap;
            }

            top += line.Height + Gap;
        }

        return (boxes, lines.Count == 0 ? 0 : top - Gap);
    }

    private double Width((double Width, double Height) size, double width) => Basis > 0 ? Basis * width : size.Width;
}
