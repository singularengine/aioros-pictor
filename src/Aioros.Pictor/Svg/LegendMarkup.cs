using System.Buffers;
using Aioros.Graphs.Svg;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>
/// Recharts' default legend as markup: its HTML wrapper and list beside the surface (with the legend toggles' checkboxes),
/// or, for a responsive chart, its items inside the SVG.
/// </summary>
internal static class LegendMarkup
{
    public static void Write(MarkupBuffer output, LegendLayout legend, bool inline, ChartMarks? marks = null, (double Width, double Height) surface = default)
    {
        if (legend.Spec.Frame is not null || legend.Spec.Content is not null)
        {
            Templated(output, legend, inline, marks, surface);
            return;
        }

        if (inline)
        {
            InlineLegend(output, legend);
            return;
        }

        if (legend.Spec.Vertical)
        {
            ColumnWrapper(output, legend);
        }
        else
        {
            Raw(output, "<div class=\"recharts-legend-wrapper\" style=\"position:absolute;width:"u8);
            Number(output, legend.Width);
            Raw(output, "px"u8);
            Height(output, legend);
            Position(output, legend);

            if (legend.Spec.LineHeight is { } lineHeight)
            {
                Raw(output, ";line-height:"u8);
                Number(output, lineHeight);
                Raw(output, "px"u8);
            }

            Inactive(output, legend);
            Wrapper(output, legend);
            Raw(output, "\"><ul class=\"recharts-default-legend\" style=\"padding:0;margin:0;text-align:"u8);
            Utf8Markup.Text(output, legend.Spec.Align);
            Raw(output, "\">"u8);
        }

        for (var i = 0; i < legend.Items.Count; i++)
        {
            LegendItem(output, legend, i, marks);
        }

        Raw(output, "</ul>"u8);
        Interactions(output, legend);
        Raw(output, "</div>"u8);
    }

    /// <summary>The style blocks of a legend's command buttons and toggles, when it has them.</summary>
    private static void Interactions(MarkupBuffer output, LegendLayout legend)
    {
        if (legend.Spec.ClickCommand is not null)
        {
            Raw(output, "<style>.pictor-lc{position:absolute;left:0;top:0;width:100%;height:100%;margin:0;padding:0;border:0;background:none;cursor:pointer}.pictor-lc:focus-visible{outline:2px solid currentColor}</style>"u8);
        }

        if (legend.Spec.Toggles)
        {
            LegendToggles.Styles(output, legend);
        }
    }

    /// <summary>
    /// A legend with custom content: Recharts' wrapper at the legend's place, holding a list with the browser's default
    /// margins and indent, one item per entry with the entry's content (a content that is one text block is the item's
    /// own text, as Recharts' examples write <c>&lt;li&gt;{entry.value}&lt;/li&gt;</c>).
    /// </summary>
    private static void Pointed(MarkupBuffer output, bool inline)
    {
        if (inline)
        {
            Raw(output, ";pointer-events:auto"u8);
        }
    }

    private static void Custom(MarkupBuffer output, LegendLayout legend, ContentSpec content, bool inline)
    {
        Raw(output, "<div class=\"recharts-legend-wrapper\" style=\"position:absolute;width:"u8);
        Number(output, legend.Width);
        Raw(output, "px"u8);
        Height(output, legend);
        Position(output, legend);
        Pointed(output, inline);
        Raw(output, "\"><ul style=\"margin:1em 0;padding:0 0 0 40px\">"u8);
        var values = new Content.ContentValues(Content.ContentSlots.LegendEntry, content, null);
        foreach (var entry in legend.Items)
        {
            var item = ContentProps.LegendEntry(values, entry);
            Content.HtmlContent.Item(output, content.Nodes, item);
        }

        Raw(output, "</ul></div>"u8);
    }

    /// <summary>
    /// A legend whose content is a whole template: Recharts' wrapper holding the template's HTML, the entries where its
    /// items node stands, as Recharts' default list (<c>DefaultLegendContent</c>) or, with a template per entry, as a
    /// list of the entries' templates.
    /// </summary>
    /// <summary>A legend of templates: its HTML beside the surface, or, in a chart that scales its surface, in a <c>foreignObject</c> as large as the surface, so it scales with the plot it was laid out with.</summary>
    private static void Templated(MarkupBuffer output, LegendLayout legend, bool inline, ChartMarks? marks, (double Width, double Height) surface)
    {
        if (inline)
        {
            Raw(output, "<foreignObject x=\"0\" y=\"0\""u8);
            Attribute(output, " width"u8, surface.Width);
            Attribute(output, " height"u8, surface.Height);
            Raw(output, " style=\"pointer-events:none\">"u8);
        }

        if (legend.Spec.Frame is { } frame)
        {
            Framed(output, legend, frame, marks, inline);
        }
        else
        {
            Custom(output, legend, legend.Spec.Content!, inline);
        }

        if (inline)
        {
            Raw(output, "</foreignObject>"u8);
        }
    }

    private static void Framed(MarkupBuffer output, LegendLayout legend, ContentSpec frame, ChartMarks? marks, bool inline)
    {
        Raw(output, "<div class=\"recharts-legend-wrapper\" style=\"position:absolute;width:"u8);
        Number(output, legend.Width);
        Raw(output, "px"u8);
        Height(output, legend);
        Position(output, legend);
        Wrapper(output, legend);
        Pointed(output, inline);
        Raw(output, "\">"u8);
        var values = new Content.ContentValues(Content.ContentSlots.Legend, frame, null);
        var item = ContentProps.Legend(values, legend.Items.Count, legend.Width, legend.Spec);
        Content.HtmlContent.Write(output, frame.Nodes, item, new LegendLayout.LegendFlow(0, legend.Spec, legend.Items, writer => Entries(writer, legend, marks)));
        Interactions(output, legend);
        Raw(output, "</div>"u8);
    }

    /// <summary>A framed legend's entries: Recharts' default list, or the entries' templates in a list or a row.</summary>
    private static void Entries(MarkupBuffer output, LegendLayout legend, ChartMarks? marks)
    {
        if (legend.Spec.Content is { } content)
        {
            if (Layout.LegendRow.Of(legend.Spec.Frame) is { } row)
            {
                Row(output, legend, content, row);
                return;
            }

            Raw(output, "<ul style=\"margin:1em 0;padding:0 0 0 40px\">"u8);
            var values = new Content.ContentValues(Content.ContentSlots.LegendEntry, content, null);
            foreach (var entry in legend.Items)
            {
                Content.HtmlContent.Item(output, content.Nodes, ContentProps.LegendEntry(values, entry));
            }

            Raw(output, "</ul>"u8);
            return;
        }

        Raw(output, "<ul class=\"recharts-default-legend\" style=\"padding:0;margin:0;text-align:"u8);
        Utf8Markup.Text(output, legend.Spec.Align);
        Raw(output, "\">"u8);
        for (var i = 0; i < legend.Items.Count; i++)
        {
            LegendItem(output, legend, i, marks);
        }

        Raw(output, "</ul>"u8);
    }

    /// <summary>A framed legend's entries' templates in a wrapping row aligned as the legend, each centered in its share of the row.</summary>
    private static void Row(MarkupBuffer output, LegendLayout legend, ContentSpec content, Layout.LegendRow row)
    {
        Raw(output, "<div style=\"display:flex;flex-wrap:wrap;justify-content:"u8);
        Raw(output, legend.Spec.Align switch { "left" => "flex-start"u8, "right" => "flex-end"u8, _ => "center"u8 });
        Raw(output, ";gap:"u8);
        Number(output, row.Gap);
        Raw(output, "px\">"u8);
        var values = new Content.ContentValues(Content.ContentSlots.LegendEntry, content, null);
        foreach (var entry in legend.Items)
        {
            Raw(output, "<div style=\"display:flex;justify-content:center"u8);
            if (row.Basis > 0)
            {
                Raw(output, ";flex-basis:"u8);
                Number(output, row.Basis * 100);
                Raw(output, "%"u8);
            }

            Raw(output, "\">"u8);
            Content.HtmlContent.Write(output, content.Nodes, ContentProps.LegendEntry(values, entry));
            Raw(output, "</div>"u8);
        }

        Raw(output, "</div>"u8);
    }

    private static void ColumnWrapper(MarkupBuffer output, LegendLayout legend)
    {
        Raw(output, "<div class=\"recharts-legend-wrapper\" style=\"position:absolute;"u8);
        if (legend.Spec.Width is { } width)
        {
            Raw(output, "width:"u8);
            Number(output, width);
            Raw(output, "px"u8);
        }
        else
        {
            Raw(output, "width:auto"u8);
        }

        Height(output, legend);
        Position(output, legend);

        Inactive(output, legend);
        Wrapper(output, legend);
        Raw(output, "\"><ul class=\"recharts-default-legend\" style=\"padding:0;margin:0;text-align:left\">"u8);
    }

    /// <summary>Recharts' default position of the wrapper, along each direction its <c>wrapperStyle</c> does not set.</summary>
    private static void Position(MarkupBuffer output, LegendLayout legend)
    {
        if (!Placed(legend, "left", "right"))
        {
            Raw(output, legend.Spec.Align == "right" ? ";right:"u8 : ";left:"u8);
            Number(output, legend.Spec.Align == "right" ? legend.RightEdge : legend.Left);
            Raw(output, "px"u8);
        }

        if (!Placed(legend, "top", "bottom"))
        {
            Raw(output, legend.Spec.VerticalAlign == "bottom" ? ";bottom:"u8 : ";top:"u8);
            Number(output, legend.Edge);
            Raw(output, "px"u8);
        }
    }

    /// <summary>Whether the legend's <c>wrapperStyle</c> places it along one direction, which drops Recharts' default position there.</summary>
    private static bool Placed(LegendLayout legend, ReadOnlySpan<char> start, ReadOnlySpan<char> end) =>
        !Painting.Css.Value(legend.Spec.WrapperStyle, start).IsEmpty || !Painting.Css.Value(legend.Spec.WrapperStyle, end).IsEmpty;

    /// <summary>The legend's <c>wrapperStyle</c>, after its default style, as Recharts spreads it last.</summary>
    private static void Wrapper(MarkupBuffer output, LegendLayout legend)
    {
        if (legend.Spec.WrapperStyle is { } style)
        {
            Raw(output, ";"u8);
            Utf8Markup.Text(output, style);
        }
    }

    /// <summary>The wrapper's height: Recharts' <c>height</c> prop, or <c>auto</c>.</summary>
    private static void Height(MarkupBuffer output, LegendLayout legend)
    {
        if (legend.Spec.Height is { } height)
        {
            Raw(output, ";height:"u8);
            Number(output, height);
            Raw(output, "px"u8);
        }
        else
        {
            Raw(output, ";height:auto"u8);
        }
    }

    /// <summary>A toggled legend's inactive color when it is not Recharts' default, as the custom property its style rules read.</summary>
    private static void Inactive(MarkupBuffer output, LegendLayout legend)
    {
        if (legend.Spec.Toggles && legend.Inactive != Theming.ChartTheme.Light.LegendInactive)
        {
            Raw(output, ";--pictor-inactive:"u8);
            Utf8Markup.Text(output, legend.Inactive);
        }
    }

    private static void LegendItem(MarkupBuffer output, LegendLayout legend, int i, ChartMarks? marks)
    {
        var item = legend.Items[i];
        Raw(output, "<li class=\"recharts-legend-item legend-item-"u8);
        Number(output, item.Index);
        if (item.Inactive)
        {
            Raw(output, " inactive"u8);
        }

        Raw(output, legend.Spec.Vertical ? "\" style=\"display:block;margin-right:10px"u8 : "\" style=\"display:inline-block;margin-right:10px"u8);
        Raw(output, legend.Spec.ClickCommand is null ? "\">"u8 : ";position:relative\">"u8);
        var toggle = legend.Spec.Toggles && item.Toggles;
        if (toggle)
        {
            LegendToggles.Checkbox(output, item.Target);
        }

        Raw(output, "<svg class=\"recharts-surface\""u8);
        Attribute(output, " width"u8, legend.Spec.IconSize);
        Attribute(output, " height"u8, legend.Spec.IconSize);
        Raw(output, " style=\"display:inline-block;vertical-align:middle;margin-right:4px\" viewBox=\"0 0 32 32\"><title></title><desc></desc>"u8);
        Icon(output, item);
        Raw(output, "</svg><span class=\"recharts-legend-item-text\" style=\"color:"u8);
        Utf8Markup.Text(output, Painting.CssValues.Valid(item.Color) ?? "inherit");
        Raw(output, "\">"u8);
        Utf8Markup.Text(output, item.Name);
        Raw(output, toggle ? "</span></label>"u8 : "</span>"u8);
        if (legend.Spec.ClickCommand is { } key)
        {
            Raw(output, "<button class=\"pictor-lc\""u8);
            Attribute(output, " aria-label"u8, item.Name);
            marks?.Command(key, item.Index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Raw(output, "></button>"u8);
        }

        Raw(output, "</li>"u8);
    }

    /// <summary>
    /// A responsive chart's legend, drawn inside its SVG as it scales with it; with toggles, each item it can toggle holds its
    /// hidden checkbox in a <c>foreignObject</c> over the item (an SVG cannot hold a label), read by the same rules as the
    /// HTML legend's, the item's text drawn in the inactive color while checked.
    /// </summary>
    private static void InlineLegend(MarkupBuffer output, LegendLayout legend)
    {
        var size = legend.Spec.IconSize;
        Raw(output, "<g class=\"recharts-legend-wrapper\""u8);
        if (legend.Spec.Toggles && legend.Inactive != Theming.ChartTheme.Light.LegendInactive)
        {
            Raw(output, " style=\"--pictor-inactive:"u8);
            Utf8Markup.Text(output, legend.Inactive);
            Raw(output, "\""u8);
        }

        Raw(output, ">"u8);
        for (var i = 0; i < legend.Items.Count; i++)
        {
            var item = legend.Items[i];
            var middle = item.Y + (legend.RowHeight / 2);
            Raw(output, "<g class=\"recharts-legend-item legend-item-"u8);
            Number(output, item.Index);
            Raw(output, "\"><svg class=\"recharts-surface\""u8);
            Attribute(output, " x"u8, item.X);
            Attribute(output, " y"u8, middle - (size / 2));
            Attribute(output, " width"u8, size);
            Attribute(output, " height"u8, size);
            Raw(output, " viewBox=\"0 0 32 32\">"u8);
            Icon(output, item);
            Raw(output, "</svg><text class=\"recharts-legend-item-text\""u8);
            Attribute(output, " x"u8, item.X + size + 4);
            Attribute(output, " y"u8, middle);
            Attribute(output, " fill"u8, item.Color);
            Raw(output, " dy=\"0.355em\">"u8);
            Utf8Markup.Text(output, item.Name);
            Raw(output, "</text>"u8);
            if (legend.Spec.Toggles && item.Toggles)
            {
                Raw(output, "<foreignObject"u8);
                Attribute(output, " x"u8, item.X);
                Attribute(output, " y"u8, item.Y);
                Attribute(output, " width"u8, item.Width);
                Attribute(output, " height"u8, legend.RowHeight);
                Raw(output, ">"u8);
                LegendToggles.Checkbox(output, item.Target);
                Raw(output, "</label></foreignObject>"u8);
            }

            Raw(output, "</g>"u8);
        }

        if (legend.Spec.Toggles)
        {
            LegendToggles.Styles(output, legend);
            Raw(output, "<style>.recharts-legend-item>foreignObject>label{display:block;width:100%;height:100%}.recharts-legend-item:has(:checked) text.recharts-legend-item-text{fill:var(--pictor-inactive,#ccc)}</style>"u8);
        }

        Raw(output, "</g>"u8);
    }

    /// <summary>Recharts' <c>DefaultLegendContent</c> icon: a line, a plain line, a rectangle, or a d3 symbol of 32 px in diameter.</summary>
    private static void Icon(MarkupBuffer output, in LegendItem item)
    {
        var color = item.Color;
        switch (item.Icon)
        {
            case LegendIcon.PlainLine:
                Raw(output, "<line stroke-width=\"4\" fill=\"none\""u8);
                Attribute(output, " stroke"u8, color);
                if (item.Dash is { } dash)
                {
                    Attribute(output, " stroke-dasharray"u8, dash);
                }

                Raw(output, " x1=\"0\" y1=\"16\" x2=\"32\" y2=\"16\" class=\"recharts-legend-icon\"/>"u8);
                break;
            case LegendIcon.Line:
                Raw(output, "<path stroke-width=\"4\" fill=\"none\""u8);
                Attribute(output, " stroke"u8, color);
                Raw(output, " d=\"M0,16h10.666666666666666\n            A5.333333333333333,5.333333333333333,0,1,1,21.333333333333332,16\n            H32M21.333333333333332,16\n            A5.333333333333333,5.333333333333333,0,1,1,10.666666666666666,16\" class=\"recharts-legend-icon\"/>"u8);
                break;
            case LegendIcon.Rect:
                Raw(output, "<path stroke=\"none\""u8);
                Attribute(output, " fill"u8, color);
                Raw(output, " d=\"M0,4h32v24h-32z\" class=\"recharts-legend-icon\"/>"u8);
                break;
            default:
                Raw(output, "<path"u8);
                Attribute(output, " fill"u8, color);
                Raw(output, " class=\"recharts-symbols\" transform=\"translate(16, 16)\" d=\""u8);
                var sink = new SvgPathWriter(output);
                LegendIcons.Symbol(item.Icon).Write(ref sink);
                Raw(output, "\"/>"u8);
                break;
        }
    }
}
