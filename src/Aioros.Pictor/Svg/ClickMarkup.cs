using System.Buffers;
using Aioros.Pictor.Interaction;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>
/// A chart's click targets as HTML buttons over the surface, each the framework's command button with its parameter:
/// a <c>foreignObject</c> at the target's box (a rectangle, a circle with round corners, or the whole surface clipped
/// to a shape's path), transparent at rest and focusable. Hovering or focusing a button shows the hover state it stands
/// for (the rules link the button's class to its tooltip and cursor), so the tooltip keeps working over the buttons and
/// the keyboard reaches every item with its tooltip. Only the targets shown are written: Aioros numbers buttons apart
/// from fields, so the buttons a brush's window or a treemap's zoom leaves out shift no field's id.
/// </summary>
internal static class ClickMarkup
{
    private static ReadOnlySpan<byte> Rules => ".pictor-cmds foreignObject{overflow:visible}.pictor-cmds .pictor-p{pointer-events:none}.pictor-cmds button{display:block;width:100%;height:100%;margin:0;padding:0;border:0;background:none;cursor:pointer;pointer-events:auto;outline-offset:-2px}.pictor-cmds button:focus-visible{outline:2px solid currentColor}"u8;

    private static ReadOnlySpan<byte> OptionRules => ".pictor-cmds input{-webkit-appearance:none;appearance:none;display:block;width:100%;height:100%;margin:0;padding:0;border:0;background:none;cursor:pointer;pointer-events:auto;outline-offset:-2px}.pictor-cmds input:focus-visible{outline:2px solid currentColor}"u8;

    private static ReadOnlySpan<byte> LinkRules => ".pictor-cmds a{display:block;width:100%;height:100%;cursor:pointer;pointer-events:auto;outline-offset:-2px}.pictor-cmds a:focus-visible{outline:2px solid currentColor}"u8;

    public static void Write(MarkupBuffer output, ChartMarks? marks, List<ClickTarget> targets, IChartPlot plot, bool follow = false)
    {
        if (targets.Count == 0)
        {
            return;
        }

        Raw(output, "<g class=\"pictor-cmds\"><style>"u8);
        Raw(output, Rules);
        if (targets.Exists(static target => target.Option))
        {
            Raw(output, OptionRules);
        }

        if (targets.Exists(static target => target.Href is not null && !target.Hidden))
        {
            Raw(output, LinkRules);
        }

        if (plot.HoverCount > 0 && plot.Spec.Tooltip is { ClickTrigger: false })
        {
            States(output, targets);
        }

        Raw(output, "</style>"u8);
        var zoom = plot.Spec.Treemap?.Zoom ?? string.Empty;
        var chosen = false;
        foreach (var target in targets)
        {
            if (target.Option)
            {
                Option(output, marks, target, plot.Spec, ref chosen, zoom);
            }
            else if (!target.Hidden)
            {
                Target(output, marks, target, plot.Spec, follow);
            }
        }

        Raw(output, "</g>"u8);
    }

    /// <summary>
    /// The rules showing a hover state while one of its buttons is hovered (while nothing in the chart has visible focus,
    /// as a tap leaves its <c>:hover</c> behind) or has visible focus (the keyboard, or the framework's touch follow, for
    /// which the buttons of a chart whose tooltip follows the axis carry <c>tabindex</c>): its tip, and its cursor.
    /// </summary>
    private static void States(MarkupBuffer output, List<ClickTarget> targets)
    {
        var states = targets.Where(target => target.State >= 0 && !target.Hidden).Select(target => target.State).Distinct().Order().ToList();
        if (states.Count == 0)
        {
            return;
        }

        Raw(output, ".recharts-wrapper:not(:has(:focus-visible)){"u8);
        Entered(output, states, ":hover"u8);
        Raw(output, "}.recharts-wrapper{"u8);
        Entered(output, states, ":focus-visible"u8);
        Raw(output, "}"u8);
    }

    /// <summary>The tips and the cursors of the states whose button is hovered, or has visible focus.</summary>
    private static void Entered(MarkupBuffer output, List<int> states, ReadOnlySpan<byte> entered)
    {
        Selectors(output, states, entered, ") .pictor-tk"u8, default);
        Raw(output, "{display:inline}"u8);
        Selectors(output, states, entered, ") .pictor-c"u8, default);
        Raw(output, "{display:inline}"u8);
    }

    private static void Selectors(MarkupBuffer output, List<int> states, ReadOnlySpan<byte> entered, ReadOnlySpan<byte> middle, ReadOnlySpan<byte> end)
    {
        for (var i = 0; i < states.Count; i++)
        {
            Raw(output, i == 0 ? "&:has(.pictor-h"u8 : ",&:has(.pictor-h"u8);
            Number(output, states[i]);
            Raw(output, entered);
            Raw(output, middle);
            Number(output, states[i]);
            Raw(output, end);
        }
    }

    private static void Target(MarkupBuffer output, ChartMarks? marks, ClickTarget target, ChartSpec spec, bool follow)
    {
        Raw(output, "<foreignObject"u8);
        Box(output, target, spec);
        if (target.Href is { } href)
        {
            Raw(output, "><a"u8);
            Attribute(output, " href"u8, href);
        }
        else
        {
            Raw(output, "><button"u8);
        }

        Shape(output, target);
        State(output, target, follow && target.Href is null);
        Attribute(output, " aria-label"u8, target.Label);
        if (target.Href is not null)
        {
            Raw(output, "></a></foreignObject>"u8);
            return;
        }

        marks?.Command(target.Key, target.Parameter);
        Raw(output, "></button></foreignObject>"u8);
    }

    private static void Shape(MarkupBuffer output, ClickTarget target)
    {
        if (target.Kind == ClickShapeKind.Circle)
        {
            Raw(output, " style=\"border-radius:50%\""u8);
        }
        else if (target.Kind == ClickShapeKind.Path)
        {
            Raw(output, " style=\"clip-path:path('"u8);
            Utf8Markup.Text(output, target.Path!);
            Raw(output, "')\""u8);
        }
    }

    private static void State(MarkupBuffer output, ClickTarget target, bool follow)
    {
        if (target.State >= 0)
        {
            Raw(output, follow ? " tabindex=\"0\" class=\"pictor-h"u8 : " class=\"pictor-h"u8);
            Number(output, target.State);
            Raw(output, "\""u8);
        }
    }

    /// <summary>
    /// An option of a choice field (a nest treemap's node and its zoom): a transparent radio over the target's box, the
    /// first one standing for the current zoom checked (the node zoomed into, written hidden, so every post carries the
    /// zoom), each one off the zoom written hidden, so the field's options are the same whatever the zoom; choosing one
    /// posts the form with its value, which the plan binds to the zoom.
    /// </summary>
    private static void Option(MarkupBuffer output, ChartMarks? marks, ClickTarget target, ChartSpec spec, ref bool chosen, string zoom)
    {
        Raw(output, "<foreignObject"u8);
        if (target.Hidden)
        {
            Raw(output, " width=\"0\" height=\"0\"><input type=\"radio\" hidden"u8);
        }
        else
        {
            Box(output, target, spec);
            Raw(output, "><input type=\"radio\""u8);
            if (target.State >= 0)
            {
                Raw(output, " class=\"pictor-h"u8);
                Number(output, target.State);
                Raw(output, "\""u8);
            }
        }

        if (!chosen && target.Parameter == zoom)
        {
            Raw(output, " checked"u8);
            chosen = true;
        }

        Attribute(output, " aria-label"u8, target.Label);
        marks?.Option(target.Key, target.Parameter);
        Raw(output, "></foreignObject>"u8);
    }

    private static void Box(MarkupBuffer output, ClickTarget target, ChartSpec spec)
    {
        var classes = target.Kind == ClickShapeKind.Path;
        if (classes || target.Toggle >= 0)
        {
            Span<byte> buffer = stackalloc byte[24];
            Raw(output, " class=\""u8);
            Raw(output, classes ? "pictor-p"u8 : default);
            if (target.Toggle >= 0)
            {
                Raw(output, classes ? " "u8 : default);
                Raw(output, buffer[..Toggles.Class(buffer, "pictor-s"u8, target.Toggle)]);
            }

            Raw(output, "\""u8);
        }

        var (x, y, width, height) = target.Kind switch
        {
            ClickShapeKind.Path => (0, 0, spec.Width, spec.Height),
            ClickShapeKind.Circle => (target.X - (target.Width / 2), target.Y - (target.Height / 2), target.Width, target.Height),
            _ => (target.X, target.Y, target.Width, target.Height),
        };
        Attribute(output, " x"u8, x);
        Attribute(output, " y"u8, y);
        Attribute(output, " width"u8, width);
        Attribute(output, " height"u8, height);
    }
}
