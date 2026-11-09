using System.Buffers;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>The markup of the standalone charts' own elements: a treemap's names and nest index, a sunburst's named groups and texts.</summary>
internal static class ItemMarkup
{
    public static void PlainText(MarkupBuffer output, double x, double y, string? text, double fontSize, string? fill, string? stroke)
    {
        Raw(output, "<text"u8);
        Attribute(output, " x"u8, x);
        Attribute(output, " y"u8, y);
        Attribute(output, " font-size"u8, fontSize);
        if (fill is not null)
        {
            Attribute(output, " fill"u8, fill);
        }

        if (stroke is not null)
        {
            Attribute(output, " stroke"u8, stroke);
        }

        Raw(output, ">"u8);
        Utf8Markup.Text(output, text);
        Raw(output, "</text>"u8);
    }

    /// <summary>The key of the field carrying a nest treemap's zoom in its posts.</summary>
    public const string ZoomPath = "zoom-path";

    /// <summary>
    /// Recharts' nest index: a box per node of the zoomed node's trail. When the treemap zooms through a command, every
    /// box holds a button over it running the command with its node's path.
    /// </summary>
    public static void NestIndex(MarkupBuffer output, Layout.NestBox[] trail, string? zoom, ChartMarks? marks, Theming.ChartTheme theme, ContentSpec? content = null, bool choice = false)
    {
        if (zoom is not null && choice)
        {
            ChoiceIndex(output, trail, zoom, marks, theme, content);
        }
        else
        {
            CommandIndex(output, trail, zoom, marks, theme, content);
        }
    }

    private static void CommandIndex(MarkupBuffer output, Layout.NestBox[] trail, string? zoom, ChartMarks? marks, Theming.ChartTheme theme, ContentSpec? content)
    {

        var values = content is null ? null : new Content.ContentValues(Content.ContentSlots.NestIndex, content, null);
        Raw(output, "<div class=\"recharts-treemap-nest-index-wrapper\" style=\"margin-top:8px;text-align:center\">"u8);
        if (zoom is not null)
        {
            Raw(output, "<input type=\"hidden\" class=\"pictor-off\""u8);
            Attribute(output, " value"u8, trail.LastOrDefault(box => box.Shown).Path ?? string.Empty);
            marks?.Field(ZoomPath);
            Raw(output, ">"u8);
        }

        if (zoom is not null)
        {
            Raw(output, "<style>.pictor-zoom{position:absolute;left:0;top:0;width:100%;height:100%;margin:0;padding:0;border:0;background:none;cursor:pointer}.pictor-zoom:focus-visible{outline:2px solid currentColor}</style>"u8);
        }

        for (var i = 0; i < trail.Length; i++)
        {
            var box = trail[i];
            if (!box.Shown)
            {
                continue;
            }

            Raw(output, "<div class=\"recharts-treemap-nest-index-box\""u8);
            BoxStyle(output, theme);
            Raw(output, zoom is null ? ";margin-right:3px\">"u8 : ";margin-right:3px;position:relative\">"u8);
            BoxContent(output, values, i, box);

            if (zoom is not null)
            {
                Raw(output, "<button class=\"pictor-zoom\""u8);
                Attribute(output, " aria-label"u8, box.Name);
                marks?.Command(zoom, box.Path);
                Raw(output, "></button>"u8);
            }

            Raw(output, "</div>"u8);
        }

        Raw(output, "</div>"u8);
    }

    /// <summary>
    /// The nest index of a treemap zoomed through a choice field: the trail's boxes alone (the field's options are the
    /// nodes', whatever the zoom, and its value travels in the checked one), each holding a transparent radio over it
    /// whose value is its node's path, so a click zooms out to it without a command.
    /// </summary>
    private static void ChoiceIndex(MarkupBuffer output, Layout.NestBox[] trail, string key, ChartMarks? marks, Theming.ChartTheme theme, ContentSpec? content)
    {
        var values = content is null ? null : new Content.ContentValues(Content.ContentSlots.NestIndex, content, null);
        Raw(output, "<div class=\"recharts-treemap-nest-index-wrapper\" style=\"margin-top:8px;text-align:center\"><style>.pictor-zoom{position:absolute;left:0;top:0;width:100%;height:100%;margin:0;padding:0;border:0;background:none;cursor:pointer;-webkit-appearance:none;appearance:none}.pictor-zoom:focus-visible{outline:2px solid currentColor}</style>"u8);
        for (var i = 0; i < trail.Length; i++)
        {
            var box = trail[i];
            if (!box.Shown)
            {
                continue;
            }

            Raw(output, "<div class=\"recharts-treemap-nest-index-box\""u8);
            BoxStyle(output, theme);
            Raw(output, ";margin-right:3px;position:relative\">"u8);
            BoxContent(output, values, i, box);
            Raw(output, "<input type=\"radio\" class=\"pictor-zoom\""u8);
            Attribute(output, " aria-label"u8, box.Name);
            marks?.Option(key, box.Path);
            Raw(output, "></div>"u8);
        }

        Raw(output, "</div>"u8);
    }

    /// <summary>A trail box's content: its custom content (Recharts' <c>nestIndexContent</c>), or its name.</summary>
    private static void BoxContent(MarkupBuffer output, Content.ContentValues? values, int index, in Layout.NestBox box)
    {
        if (values is not null)
        {
            Content.HtmlContent.Inline(output, values.Spec.Nodes, Painting.ContentProps.NestIndex(values, index, box));
            return;
        }

        Utf8Markup.Text(output, box.Name);
    }

    /// <summary>A box's style: Recharts' inline box.</summary>
    private static void BoxStyle(MarkupBuffer output, Theming.ChartTheme theme)
    {
        Raw(output, " style=\"cursor:pointer;display:inline-block;padding:0 7px;background:"u8);
        Utf8Markup.Text(output, Painting.CssValues.Valid(theme.NestIndexBackground) ?? Theming.ChartTheme.Light.NestIndexBackground);
        Raw(output, ";color:"u8);
        Utf8Markup.Text(output, Painting.CssValues.Valid(theme.NestIndexText) ?? Theming.ChartTheme.Light.NestIndexText);
    }

    public static void Item(MarkupBuffer output, string name)
    {
        Raw(output, "<g"u8);
        Attribute(output, " aria-label"u8, name);
        Raw(output, " tabindex=\"0\">"u8);
    }

    /// <summary>Recharts' <c>Text</c> with its options spread first, then its middle baseline, its point, its class, its anchor and its fill.</summary>
    public static void OptionsText(MarkupBuffer output, double x, double y, string text, TextOptionsSpec options, string fill)
    {
        Raw(output, "<text"u8);
        Optional(output, " font-family"u8, options.FontFamily);
        Optional(output, " font-weight"u8, options.FontWeight);
        Optional(output, " paint-order"u8, options.PaintOrder);
        Optional(output, " font-size"u8, options.FontSize);
        Optional(output, " stroke"u8, options.Stroke);
        Optional(output, " pointer-events"u8, options.PointerEvents);
        Raw(output, " alignment-baseline=\"middle\""u8);
        Attribute(output, " x"u8, x);
        Attribute(output, " y"u8, y);
        Raw(output, " class=\"recharts-text\" text-anchor=\"middle\""u8);
        Attribute(output, " fill"u8, options.Fill ?? fill);
        Raw(output, "><tspan"u8);
        Attribute(output, " x"u8, x);
        Raw(output, " dy=\"0em\">"u8);
        Utf8Markup.Text(output, text);
        Raw(output, "</tspan></text>"u8);
    }

    private static void Optional(MarkupBuffer output, ReadOnlySpan<byte> name, string? value)
    {
        if (value is not null)
        {
            Attribute(output, name, value);
        }
    }
}
