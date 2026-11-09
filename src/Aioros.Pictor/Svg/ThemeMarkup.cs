using System.Buffers;
using Aioros.Pictor.Theming;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>
/// The theme's colors that the shared style block reads (the cursor, the active dot's ring, the tooltip's box and
/// label), written on the chart's wrapper as custom properties, and its font. The style block's rules fall back to
/// Recharts' defaults, so a chart in <see cref="ChartTheme.Light"/> writes none of them and every rule means the same
/// thing in every chart of a page.
/// </summary>
internal static class ThemeMarkup
{
    public static void Wrapper(MarkupBuffer output, ChartTheme theme)
    {
        if (ReferenceEquals(theme, ChartTheme.Light))
        {
            return;
        }

        var light = ChartTheme.Light;
        if (theme.FontFamily is { } font && Painting.CssValues.IsFontFamily(font))
        {
            Raw(output, ";font-family:"u8);
            Utf8Markup.Text(output, font);
        }

        Variable(output, ";--pictor-cursor:"u8, theme.CursorStroke, light.CursorStroke);
        Variable(output, ";--pictor-band:"u8, theme.CursorFill, light.CursorFill);
        Variable(output, ";--pictor-ring:"u8, theme.ActiveDotStroke, light.ActiveDotStroke);
        Variable(output, ";--pictor-tip-bg:"u8, theme.TooltipBackground, light.TooltipBackground);
        Variable(output, ";--pictor-tip-border:"u8, theme.TooltipBorder, light.TooltipBorder);
        Variable(output, ";--pictor-tip-label:"u8, theme.TooltipLabel, light.TooltipLabel);
    }

    private static void Variable(MarkupBuffer output, ReadOnlySpan<byte> declaration, string value, string light)
    {
        if (value != light && Painting.CssValues.IsColor(value))
        {
            Raw(output, declaration);
            Utf8Markup.Text(output, value);
        }
    }
}
