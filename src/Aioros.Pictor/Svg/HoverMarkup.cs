using System.Buffers;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>
/// The compact markup of the web's hover states: inside the <c>pictor-hits</c> and <c>pictor-tips</c> layers the
/// cursors, the default active dots and the tooltips carry only what differs between them (positions, values, a series'
/// color), and the chart's style block gives what they share (the cursor's gray, the dot's white ring, the tooltip's
/// box). Every rule of the style block means the same thing in every chart, since a page's styles are global; what
/// differs per chart stays on the element.
/// </summary>
internal static class HoverMarkup
{
    /// <summary>What a chart's hover rules cover besides its states: its focus mode, a scatter chart's keys, whether its states draw cursors, and how its boxes follow the pointer.</summary>
    public readonly record struct StyleOptions(FocusMarkup.Mode Focus = FocusMarkup.Mode.None, bool Keys = false, bool Cursors = false, PointerFollow? Follow = null, bool InPlace = false);

    public enum Layer
    {
        None,
        Hits,
        Tips,
        Keys,
    }

    public static Layer LayerOf(ReadOnlySpan<byte> classes) =>
        classes.SequenceEqual("pictor-hits"u8) || classes.SequenceEqual("pictor-clicks"u8) ? Layer.Hits : classes.SequenceEqual("pictor-tips"u8) ? Layer.Tips : classes.SequenceEqual("pictor-keys"u8) ? Layer.Keys : Layer.None;

    /// <summary>Whether a group of the tips layer is left out, its children written in its place (an active dot's wrapper).</summary>
    public static bool Unwrapped(ReadOnlySpan<byte> classes) => classes.SequenceEqual("recharts-layer recharts-active-dot"u8);

    public static bool IsCursor(ReadOnlySpan<byte> classes) => classes.EndsWith("recharts-tooltip-cursor"u8);

    public static void Segment(MarkupBuffer output, double x1, double y1, double x2, double y2, in Paint paint, ReadOnlySpan<byte> classes, Theming.ChartTheme theme)
    {
        Raw(output, "<path"u8);
        CursorPaint(output, paint, classes, theme);
        Raw(output, " class=\""u8);
        Raw(output, classes);
        Raw(output, "\" d=\"M"u8);
        Number(output, x1);
        Raw(output, ","u8);
        Number(output, y1);
        Raw(output, "L"u8);
        Number(output, x2);
        Raw(output, ","u8);
        Number(output, y2);
        Raw(output, "\"/>"u8);
    }

    /// <summary>A cursor's paint: nothing when it is the default the style block gives, otherwise an inline style (which wins over it).</summary>
    public static void CursorPaint(MarkupBuffer output, in Paint paint, ReadOnlySpan<byte> classes, Theming.ChartTheme theme)
    {
        var band = classes.StartsWith("recharts-rectangle"u8);
        var plain = double.IsNaN(paint.StrokeWidth) && paint.Dash is null && double.IsNaN(paint.FillOpacity) && double.IsNaN(paint.StrokeOpacity);
        if (plain && (band ? paint.Stroke is null && paint.Fill == theme.CursorFill : paint.Stroke == theme.CursorStroke && paint.Fill is null or "none"))
        {
            return;
        }

        Raw(output, " style=\"stroke:"u8);
        Utf8Markup.Text(output, CssValues.Valid(paint.Stroke) ?? "none");
        Raw(output, ";fill:"u8);
        Utf8Markup.Text(output, CssValues.Valid(paint.Fill) ?? "none");
        if (!double.IsNaN(paint.StrokeWidth))
        {
            Raw(output, ";stroke-width:"u8);
            Number(output, paint.StrokeWidth);
        }

        if (paint.Dash is { } dash && CssValues.IsDash(dash))
        {
            Raw(output, ";stroke-dasharray:"u8);
            Utf8Markup.Text(output, dash);
        }

        Raw(output, "\""u8);
    }

    /// <summary>Writes Recharts' default active dot (a ring of 2 in the theme's background, white in Recharts') as a classed circle; false for any other circle.</summary>
    public static bool Dot(MarkupBuffer output, double cx, double cy, double r, in Paint paint, ReadOnlySpan<byte> classes, Theming.ChartTheme theme)
    {
        if (!classes.SequenceEqual("recharts-dot"u8) || paint.Stroke != theme.ActiveDotStroke || paint.StrokeWidth != 2 || paint.Fill is null
            || paint.Dash is not null || !double.IsNaN(paint.FillOpacity) || !double.IsNaN(paint.StrokeOpacity))
        {
            return false;
        }

        Raw(output, "<circle class=\"pictor-dot\""u8);
        Attribute(output, " r"u8, r);
        Attribute(output, " fill"u8, paint.Fill);
        Attribute(output, " cx"u8, cx);
        Attribute(output, " cy"u8, cy);
        Raw(output, "/>"u8);
        return true;
    }

    /// <summary>Recharts' default tooltip: its box, its label and one line per entry in the entry's color, with the tooltip's own styles and classes when it has them.</summary>
    /// <remarks>A box that follows the pointer stands at 0 along that coordinate, its rule placing it there, and its <c>foreignObject</c> takes the box's measured extent along it (which the overflowing box does not need, and the reference comparison reads).</remarks>
    public static void Tooltip(MarkupBuffer output, double x, double y, string label, ReadOnlySpan<TooltipEntry> entries, TooltipSpec spec, in TipFollow follow = default)
    {
        Box(output, x, y, spec, follow);
        Raw(output, "><div class=\"recharts-default-tooltip"u8);
        if (ClassNames.Tokens(spec.WrapperClassName) is { } wrapperClass)
        {
            Raw(output, " "u8);
            Utf8Markup.Text(output, wrapperClass);
        }

        Raw(output, "\""u8);
        if (spec.ContentStyle is { } content)
        {
            Attribute(output, " style"u8, content);
        }

        if (spec.AccessibilityLayer)
        {
            Raw(output, " role=\"status\" aria-live=\"assertive\""u8);
        }

        Raw(output, "><p"u8);
        if (ClassNames.Tokens(spec.LabelClassName) is { } labelClass)
        {
            Attribute(output, " class"u8, labelClass);
        }

        if (spec.LabelStyle is { } labelStyle)
        {
            Attribute(output, " style"u8, labelStyle);
        }

        Raw(output, ">"u8);
        Utf8Markup.Text(output, label);
        Raw(output, "</p><ul>"u8);
        foreach (var entry in entries)
        {
            Raw(output, "<li"u8);
            TargetClass(output, entry.Target);
            Raw(output, " style=\"color:"u8);
            Utf8Markup.Text(output, CssValues.Valid(entry.Color) ?? "inherit");
            if (spec.ItemStyle is { } item)
            {
                Raw(output, ";"u8);
                Utf8Markup.Text(output, item);
            }

            Raw(output, "\">"u8);
            Utf8Markup.Text(output, entry.Name);
            Utf8Markup.Text(output, spec.Separator);
            Utf8Markup.Text(output, entry.Value);
            Raw(output, "</li>"u8);
        }

        Raw(output, "</ul></div></foreignObject>"u8);
    }

    /// <summary>The box's <c>foreignObject</c> up to its content: at its place, or at 0 along what it follows with its measured extent, and its custom properties and wrapper style.</summary>
    /// <summary>A tooltip's custom content as HTML in the box the default content would take, its wrapper shrunk to its content as Recharts' wrapper is.</summary>
    public static void Content(MarkupBuffer output, double x, double y, System.Collections.Immutable.ImmutableArray<ContentNode> nodes, in Content.ContentItem item, TooltipSpec spec, in TipFollow follow, TooltipLines? lines = null)
    {
        Box(output, x, y, spec, follow);
        Raw(output, "><div class=\"pictor-content\" style=\"display:inline-block;white-space:nowrap\">"u8);
        Pictor.Content.HtmlContent.Write(output, nodes, item, lines);
        Raw(output, "</div></foreignObject>"u8);
    }

    private static void Box(MarkupBuffer output, double x, double y, TooltipSpec spec, in TipFollow follow)
    {
        var (alongX, alongY) = (follow.Axis == TipAxis.X || follow.Polar, follow.Axis == TipAxis.Y || follow.Polar);
        Raw(output, "<foreignObject"u8);
        Attribute(output, " x"u8, alongX ? 0 : x);
        Attribute(output, " y"u8, alongY ? 0 : y);
        Attribute(output, " width"u8, !alongX ? 1 : follow.Polar ? follow.Width : follow.Size);
        Attribute(output, " height"u8, alongY ? follow.Size : 1);
        if (follow.Polar || spec.WrapperStyle is not null)
        {
            Raw(output, " style=\""u8);
            if (follow.Polar)
            {
                PointerFollow.Tip(output, follow);
                Raw(output, spec.WrapperStyle is null ? default : ";"u8);
            }

            Utf8Markup.Text(output, spec.WrapperStyle);
            Raw(output, "\""u8);
        }

    }

    /// <summary>A toggled entry's class (<c>pictor-sK</c>), when the entry has a target.</summary>
    public static void TargetClass(MarkupBuffer output, int target)
    {
        if (target < 0)
        {
            return;
        }

        Span<byte> classes = stackalloc byte[24];
        Raw(output, " class=\""u8);
        Raw(output, classes[..Toggles.Class(classes, "pictor-s"u8, target)]);
        Raw(output, "\""u8);
    }

    /// <summary>
    /// The rules the hover states share, then the rules showing each state (<see cref="HoverLayers"/> for the markup):
    /// its tip, a later sibling of its hit group (<c>.pictor-hk2:hover~.pictor-tk2</c>), and its cursor below the series
    /// through the surface (<c>:has(&gt;.pictor-hover&gt;.pictor-hk2:hover)&gt;.pictor-cursors&gt;.pictor-c2</c>); every
    /// subject is a class, which the browser matches against those groups alone (rules linking two layers through a
    /// <c>:has()</c> on the wrapper made a page of many charts restyle several times slower). Hovered, or focused after a
    /// click for a tooltip shown on click (whose hit groups take focus). A chart with a default state is classed
    /// <c>pictor-dK</c> and shows state K while no state is entered. Hidden tips are left out of the layout
    /// (<c>display:none</c>), which Chromium lays out 15 to 30% faster than hidden boxes. On a touch screen
    /// (<c>hover:none</c>) the hit groups are clickable to the browser (<c>cursor:pointer</c>), so a tap gives them the
    /// hover state as Recharts' listeners make its wrapper clickable (WebKit sends its emulated mouse events, and so
    /// <c>:hover</c>, only to clickable elements). A chart whose groups follow the finger and the keyboard
    /// (<see cref="FocusMarkup"/>) also shows the state of the group with visible focus; hovering shows a state only
    /// while nothing in the chart has visible focus, since a tap leaves its <c>:hover</c> where the finger was. With the
    /// keyboard layer, the category the keyboard left (Escape) keeps its cursor and dots without the box, as Recharts'
    /// dismissed tooltip, and the default state does not come back. A scatter chart's keyboard layer
    /// (<see cref="Painting.ScatterKeys"/>) shows the cross of its focused step, and the cross of the step the keyboard
    /// left while nothing in the chart is hovered or focused.
    /// </summary>
    /// <summary>
    /// A chart's hover and sync rules (an earlier layout of a responsive chart with as many states wrote them already when
    /// <paramref name="repeated"/>; the sync rules go to <paramref name="page"/> when the chart is written into a page),
    /// and the class of each category's value when the chart syncs by value.
    /// </summary>
    public static string?[]? Rules(MarkupBuffer output, IChartPlot plot, string? synced, StyleOptions options, bool repeated, List<SyncRules>? page = null)
    {
        if (plot.HoverCount > 0 && !repeated)
        {
            Styles(output, plot.HoverCount, options.Focus == FocusMarkup.Mode.Click, plot.DefaultTip, options);
        }

        if (synced is null)
        {
            return null;
        }

        var categories = SyncMarkup.Categories(plot)!;
        var byValue = plot.Spec.SyncMethod is "value" or "Value";
        if (page is not null)
        {
            var rules = new SyncRules(synced, categories, byValue);
            if (!page.Exists(kept => kept.Key == rules.Key))
            {
                page.Add(rules);
            }
        }
        else if (!repeated)
        {
            SyncMarkup.Rules(output, synced, categories, byValue);
        }

        return byValue ? [.. categories.Select(SyncMarkup.ValueClass)] : null;
    }

    public static void Styles(MarkupBuffer output, int count, bool click, int initial, StyleOptions options = default)
    {
        var (focus, keys, cursors, follow, inPlace) = options;
        Raw(output, "<style>.pictor-hit{fill:transparent}.pictor-cursors .recharts-tooltip-cursor{stroke:var(--pictor-cursor,#ccc);fill:none}.pictor-cursors .recharts-rectangle.recharts-tooltip-cursor{stroke:none;fill:var(--pictor-band,#ccc)}.pictor-dot{stroke:var(--pictor-ring,#fff);stroke-width:2px}.pictor-cursors>g,.pictor-hover>[class^=pictor-tk]{display:none}.pictor-cursors,.pictor-hover>[class^=pictor-tk],.recharts-line,.recharts-area,.recharts-bar,.recharts-scatter,.recharts-reference-line,.recharts-reference-area,.recharts-reference-dot,.recharts-label-list{pointer-events:none}.pictor-hover>g{outline:none}@media (hover:none){.pictor-hover>g{cursor:pointer}}.pictor-hover foreignObject{overflow:visible}.recharts-default-tooltip{display:inline-block;margin:0;padding:10px;background-color:var(--pictor-tip-bg,#fff);border:1px solid var(--pictor-tip-border,#ccc);white-space:nowrap}.recharts-default-tooltip p,.recharts-default-tooltip ul{margin:0;padding:0}.recharts-default-tooltip>p{color:var(--pictor-tip-label)}.recharts-default-tooltip li{display:block;padding:4px 0}"u8);
        var entered = click ? ":focus"u8 : ":hover"u8;
        Raw(output, click ? ".pictor-hover>:focus+g{display:inline}"u8 : ".recharts-wrapper:not(:has(:focus-visible)) .pictor-hover>:hover+g{display:inline}"u8);
        if (click)
        {
            Ignored(output, initial);
        }

        if (cursors && !click)
        {
            Raw(output, ".recharts-wrapper:not(:has(:focus-visible)){"u8);
            CursorRules(output, count, "svg:has(>g>.pictor-hk"u8, entered);
            Raw(output, "}"u8);
        }
        else if (cursors)
        {
            CursorRules(output, count, "svg:has(>g>.pictor-ck"u8, entered);
        }

        if (FocusMarkup.Follows(focus))
        {
            Raw(output, ".pictor-hover>:focus-visible+g{display:inline}"u8);
            if (cursors)
            {
                CursorRules(output, count, "svg:has(>g>.pictor-hk"u8, ":focus-visible"u8);
            }
        }

        if (focus == FocusMarkup.Mode.Keys)
        {
            Raw(output, ".recharts-surface:has(>.pictor-hover>[tabindex=\"0\"]):not(:has(>.pictor-hover>:is(:hover,:focus))){&>.pictor-hover foreignObject{display:none}&>.pictor-hover>[tabindex=\"0\"]+g{display:inline}"u8);
            if (cursors)
            {
                CursorRules(output, count, "&:has(>g>.pictor-hk"u8, "[tabindex=\"0\"]"u8);
            }

            Raw(output, "}"u8);
        }

        if (initial >= 0)
        {
            Initial(output, click, initial);
        }

        if (inPlace)
        {
            InPlaceRules(output, count, click, FocusMarkup.Follows(focus));
        }

        if (keys)
        {
            Raw(output, ".pictor-keys{pointer-events:none}.pictor-keys .recharts-tooltip-cursor{stroke:var(--pictor-cursor,#ccc);fill:none;visibility:hidden}.pictor-keys>g{outline:none}.pictor-keys>g:focus-visible>*,.recharts-wrapper:not(:has(.pictor-hover>:hover,.pictor-keys>:focus)) .pictor-keys>[tabindex=\"0\"]>*{visibility:visible}"u8);
        }

        follow?.Rule(output);
        Raw(output, "</style>"u8);
    }

    /// <summary>Each state's cursor, below the series, while its hit group is in a state (<c>:hover</c>, <c>:focus</c>...): a rule per state through the surface, from <paramref name="hit"/> (the surface's <c>:has(</c> and the group's class prefix).</summary>
    private static void CursorRules(MarkupBuffer output, int count, ReadOnlySpan<byte> hit, ReadOnlySpan<byte> entered)
    {
        for (var i = 0; i < count; i++)
        {
            Raw(output, i == 0 ? default : ","u8);
            Raw(output, hit);
            Number(output, i);
            Raw(output, entered);
            Raw(output, ")>g>.pictor-c"u8);
            Number(output, i);
        }

        Raw(output, "{display:inline}"u8);
    }

    /// <summary>
    /// The rules of the shapes a state draws in place (<c>pictor-akK</c>, a funnel's active shape in its item's layer):
    /// hidden at rest, shown while state K is entered with the shape before them hidden, as Recharts renders the item
    /// with its active shape in place of its own.
    /// </summary>
    private static void InPlaceRules(MarkupBuffer output, int count, bool click, bool focused)
    {
        Raw(output, "[class^=pictor-ak]{display:none}"u8);
        if (click)
        {
            Shown(output, count, "svg:has(>g>.pictor-ck"u8, ":focus"u8);
            return;
        }

        Raw(output, ".recharts-wrapper:not(:has(:focus-visible)){"u8);
        Shown(output, count, "svg:has(>g>.pictor-hk"u8, ":hover"u8);
        Raw(output, "}"u8);
        if (focused)
        {
            Shown(output, count, "svg:has(>g>.pictor-hk"u8, ":focus-visible"u8);
        }
    }

    /// <summary>The two rules of the in-place shapes of every state entered so (<paramref name="entered"/> on its hit group): the group shown, the shape before it hidden.</summary>
    private static void Shown(MarkupBuffer output, int count, ReadOnlySpan<byte> hit, ReadOnlySpan<byte> entered)
    {
        Selectors(output, count, hit, entered, ") .pictor-ak"u8, default);
        Raw(output, "{display:inline}"u8);
        Selectors(output, count, hit, entered, ") :has(+.pictor-ak"u8, ")"u8);
        Raw(output, "{display:none}"u8);
    }

    private static void Selectors(MarkupBuffer output, int count, ReadOnlySpan<byte> hit, ReadOnlySpan<byte> entered, ReadOnlySpan<byte> subject, ReadOnlySpan<byte> end)
    {
        for (var i = 0; i < count; i++)
        {
            Raw(output, i == 0 ? default : ","u8);
            Raw(output, hit);
            Number(output, i);
            Raw(output, entered);
            Raw(output, subject);
            Number(output, i);
            Raw(output, end);
        }
    }

    /// <summary>The default state's rules: its tip and its cursor while no state is entered, in a chart classed <c>pictor-dK</c>.</summary>
    /// <summary>
    /// A tooltip shown on click ignores the pointer, as Recharts' <c>trigger="click"</c> does: the page-wide rule that shows
    /// a hovered state's tip (written by the page's other charts) is undone for a hovered state that is not focused, more
    /// specifically, the default tip excepted.
    /// </summary>
    private static void Ignored(MarkupBuffer output, int initial)
    {
        Raw(output, ".recharts-wrapper .recharts-surface>.pictor-hover.pictor-click>:hover:not(:focus)+g"u8);
        if (initial >= 0)
        {
            Raw(output, ":not(.pictor-tk"u8);
            Number(output, initial);
            Raw(output, ")"u8);
        }

        Raw(output, "{display:none}"u8);
    }

    private static void Initial(MarkupBuffer output, bool click, int initial)
    {
        var free = click ? ":not(:has(.pictor-hover>:focus))"u8 : ":not(:has(.pictor-hover>:is(:hover,[tabindex=\"0\"]),:focus-visible))"u8;
        Raw(output, ".pictor-d"u8);
        Number(output, initial);
        Raw(output, free);
        Raw(output, " .pictor-tk"u8);
        Number(output, initial);
        Raw(output, "{display:inline}.pictor-d"u8);
        Number(output, initial);
        Raw(output, free);
        Raw(output, " .pictor-c"u8);
        Number(output, initial);
        Raw(output, "{display:inline}"u8);
    }
}
