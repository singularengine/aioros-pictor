using System.Buffers;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>The chart's wrapper on the web: a responsive chart's container attributes, or a compact chart's surface alone.</summary>
internal static class WrapperMarkup
{
    /// <summary>The wrapper's end of class, its id and its style's start; a responsive chart's wrapper is its container and takes Recharts' <c>ResponsiveContainer</c> <c>className</c>, <c>id</c> and <c>style</c>.</summary>
    public static void Container(MarkupBuffer output, ChartSpec? container)
    {
        if (container?.ContainerClass is { } className)
        {
            Raw(output, " "u8);
            Utf8Markup.Text(output, className);
        }

        Raw(output, "\""u8);
        if (container?.ContainerId is { } id)
        {
            Attribute(output, " id"u8, id);
        }

        Raw(output, " style=\"position:relative;cursor:default;"u8);
        if (container?.ContainerStyle is { } style)
        {
            Utf8Markup.Text(output, style);
            Raw(output, ";"u8);
        }
    }

    /// <summary>Recharts' compact chart: its surface alone, with its title and description.</summary>
    public static void Compact(MarkupBuffer output, ChartSpec spec)
    {
        Raw(output, "<svg class=\"recharts-surface\""u8);
        Attribute(output, " width"u8, spec.Width);
        Attribute(output, " height"u8, spec.Height);
        Raw(output, " viewBox=\"0 0 "u8);
        Number(output, spec.Width);
        Raw(output, " "u8);
        Number(output, spec.Height);
        Raw(output, "\"><title>"u8);
        Utf8Markup.Text(output, spec.Title);
        Raw(output, "</title><desc>"u8);
        Utf8Markup.Text(output, spec.Desc);
        Raw(output, "</desc>"u8);
    }

    /// <summary>The wrapper's opening and classes (a responsive layout's width range, the hover state shown first, the sync id's class); the sync id's class.</summary>
    public static string? Classes(MarkupBuffer output, IChartPlot plot, string? variant)
    {
        Raw(output, "<div class=\"recharts-wrapper"u8);
        if (variant is not null)
        {
            Raw(output, " "u8);
            Utf8Markup.Text(output, variant);
        }

        if (plot.HoverCount > 0 && plot.DefaultTip >= 0)
        {
            Raw(output, " pictor-d"u8);
            Number(output, plot.DefaultTip);
        }

        if (Painting.ClassNames.Tokens(plot.Spec.ClassName) is { } className)
        {
            Raw(output, " "u8);
            Utf8Markup.Text(output, className);
        }

        var synced = Synced(plot);
        if (synced is not null)
        {
            Raw(output, " "u8);
            Utf8Markup.Text(output, synced);
        }

        return synced;
    }

    /// <summary>The class of the chart's sync id, when it syncs its hover states with other charts (a categorical chart with a tooltip shown on hover).</summary>
    public static string? Synced(IChartPlot plot) =>
        plot.Spec.SyncId is { Length: > 0 } id && plot.HoverCount > 0 && plot.Spec.Tooltip is { ClickTrigger: false } && SyncMarkup.Categories(plot) is not null
            ? SyncMarkup.Class(id)
            : null;

    /// <summary>What the surface carries for the page's behaviors: Recharts' tab stop, the framework's follow (for command buttons standing for hover states) and its pointer variables.</summary>
    public readonly record struct SurfaceBehaviors(bool TabStop, FocusMarkup.Mode Follow, bool Pointer);

    /// <summary>The wrapper's style (its container's, its size, the font size, the theme's custom properties) and its accessible name.</summary>
    public static void Style(MarkupBuffer output, ChartSpec spec, bool layout, Theming.ChartTheme theme)
    {
        Container(output, spec.Responsive && layout ? spec : null);
        if (spec.Responsive)
        {
            SurfaceMarkup.Responsive(output, spec);
        }
        else
        {
            Raw(output, "width:"u8);
            Number(output, spec.Width);
            Raw(output, "px;height:"u8);
            Number(output, spec.Height);
            Raw(output, "px"u8);
        }

        Raw(output, ";font-size:"u8);
        Number(output, spec.FontSize);
        Raw(output, "px"u8);
        ThemeMarkup.Wrapper(output, theme);
        if (spec.WrapperStyle is { Length: > 0 } style)
        {
            Raw(output, ";"u8);
            Utf8Markup.Text(output, style);
        }

        Raw(output, "\""u8);
        if (spec.Title is { } title)
        {
            Raw(output, " role=\"img\""u8);
            Attribute(output, " aria-label"u8, title);
        }
    }

    /// <summary>The surface's opening: its size, Recharts' focus and role with the accessibility layer, the page's behaviors, its view box, title and description.</summary>
    public static void Surface(MarkupBuffer output, ChartSpec spec, SurfaceBehaviors behaviors)
    {
        Raw(output, "><svg class=\"recharts-surface\""u8);
        if (spec.Responsive)
        {
            Raw(output, " width=\"100%\" height=\"100%\""u8);
        }
        else
        {
            Attribute(output, " width"u8, spec.Width);
            Attribute(output, " height"u8, spec.Height);
        }

        if (spec.AccessibilityLayer)
        {
            if (behaviors.TabStop)
            {
                Attribute(output, " tabindex"u8, spec.TabIndex ?? 0);
            }

            Attribute(output, " role"u8, spec.Role ?? "application");
        }

        if (behaviors.Follow != FocusMarkup.Mode.None)
        {
            FocusMarkup.Surface(output, behaviors.Follow);
        }

        if (behaviors.Pointer)
        {
            PointerFollow.Surface(output);
        }

        Raw(output, " viewBox=\"0 0 "u8);
        Number(output, spec.Width);
        Raw(output, " "u8);
        Number(output, spec.Height);
        Raw(output, "\" style=\"width:100%;height:100%;display:block\"><title>"u8);
        Utf8Markup.Text(output, spec.Title);
        Raw(output, "</title><desc>"u8);
        Utf8Markup.Text(output, spec.Desc);
        Raw(output, "</desc>"u8);
    }
}
