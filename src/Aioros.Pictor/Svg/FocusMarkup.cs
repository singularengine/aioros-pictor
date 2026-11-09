using System.Buffers;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>
/// How the web's hit groups take focus, for the framework's touch follow and roving focus. A tooltip shown on click
/// focuses its groups on a click. A tooltip that follows the axis has its hit layer marked <c>data-aioros-follow</c>
/// (<c>="capture"</c> in a vertical chart, which follows a vertical finger, and in a polar one, which follows a finger
/// around its center, so the page does not scroll under it) and its groups focusable by script, so the
/// group under a finger takes focus as Recharts' <c>handleTouchMove</c> moves the tooltip; with the keyboard layer in a
/// horizontal chart the layer is also a horizontal roving group whose active category alone is in the tab order, as
/// Recharts' <c>AccessibilityManager</c> moves the tooltip with the arrows. Item tooltips take no focus.
/// </summary>
internal static class FocusMarkup
{
    public enum Mode : byte
    {
        None,
        Click,
        Follow,
        Capture,
        Keys,
    }

    public static Mode Of(IChartPlot plot) => plot.HoverCount == 0 ? Mode.None
        : plot.Spec.Tooltip?.ClickTrigger == true ? Mode.Click
        : !Plots.Follows(plot) ? Mode.None
        : Plots.Keyboard(plot) ? Mode.Keys
        : Captures(plot) ? Mode.Capture : Mode.Follow;

    /// <summary>Whether a finger's drag is the chart's alone (no page scroll under it): a vertical chart's, or a polar chart's around its center.</summary>
    private static bool Captures(IChartPlot plot) => plot is ChartLayout { Vertical: true } or PolarLayout;

    /// <summary>Whether the hit groups show their state while they have visible focus (the script's focus after a touch, or the keyboard's).</summary>
    public static bool Follows(Mode mode) => mode >= Mode.Follow;

    /// <summary>
    /// Whether the follow is written on the surface rather than on the hit layer: a chart whose command buttons stand for
    /// hover states covers its hit groups with them, so a finger over a button focuses the button.
    /// </summary>
    public static bool OnSurface(Mode mode, List<Interaction.ClickTarget>? clicks) =>
        Follows(mode) && clicks is not null && clicks.Exists(static target => target.State >= 0 && !target.Hidden);

    /// <summary>The surface's touch follow, for a chart whose command buttons stand for its hover states.</summary>
    public static void Surface(MarkupBuffer output, Mode mode) =>
        Raw(output, mode == Mode.Capture ? " data-aioros-follow=\"capture\""u8 : " data-aioros-follow"u8);

    /// <summary>
    /// The hover layer's attributes (the group holding the hit groups and the tips): the touch follow (unless the surface
    /// carries it), and with the keyboard layer the roving focus that Escape leaves and Home and End do not move, as
    /// Recharts' <c>AccessibilityManager</c> reads only the left and right arrows.
    /// </summary>
    public static void Layer(MarkupBuffer output, Mode mode, bool onSurface)
    {
        Raw(output, (mode, onSurface) switch
        {
            (Mode.Keys, true) => " data-aioros-roving=\"horizontal\" data-aioros-roving-escape data-aioros-roving-ends=\"off\""u8,
            (_, true) => default,
            (Mode.Follow, _) => " data-aioros-follow"u8,
            (Mode.Capture, _) => " data-aioros-follow=\"capture\""u8,
            (Mode.Keys, _) => " data-aioros-follow data-aioros-roving=\"horizontal\" data-aioros-roving-escape data-aioros-roving-ends=\"off\""u8,
            _ => default,
        });
    }

    /// <summary>
    /// A hit group's opening tag: focusable, the active category alone in the tab order with the keyboard layer. The
    /// server writes that tab stop as <c>tabindex="00"</c> (the integer 0), which the framework's roving focus rewrites
    /// as <c>"0"</c> once a category takes the focus, so the style block can tell a category the keyboard left (Recharts
    /// keeps its cursor and dots after Escape) from the tab stop nobody visited.
    /// </summary>
    public static ReadOnlySpan<byte> Group(Mode mode, bool active) => mode switch
    {
        Mode.None => "<g"u8,
        Mode.Keys when active => "<g tabindex=\"00\""u8,
        _ => "<g tabindex=\"-1\""u8,
    };
}
