using System.Buffers;
using System.Collections.Immutable;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>
/// The gradients a chart's paints name, written as SVG's <c>linearGradient</c>s in a <c>defs</c> at the top of the
/// surface, as Recharts writes the <c>defs</c> a chart is given. A responsive chart's layouts each define their own
/// copy, named with the layout's class, since a gradient defined inside a hidden layout paints nothing in the browser.
/// </summary>
internal static class GradientMarkup
{
    public static void Defs(MarkupBuffer output, ImmutableArray<GradientSpec> gradients, string? variant)
    {
        if (gradients.IsDefaultOrEmpty)
        {
            return;
        }

        Raw(output, "<defs>"u8);
        foreach (var gradient in gradients)
        {
            Raw(output, "<linearGradient"u8);
            Attribute(output, " id"u8, Named(gradient.Id, variant));
            Attribute(output, " x1"u8, gradient.X1);
            Attribute(output, " y1"u8, gradient.Y1);
            Attribute(output, " x2"u8, gradient.X2);
            Attribute(output, " y2"u8, gradient.Y2);
            if (gradient.UserSpace)
            {
                Raw(output, " gradientUnits=\"userSpaceOnUse\""u8);
            }

            Raw(output, ">"u8);
            foreach (var stop in gradient.Stops)
            {
                Raw(output, "<stop offset=\""u8);
                Number(output, Math.Round(stop.Offset * 100, 4));
                Raw(output, "%\""u8);
                Attribute(output, " stop-color"u8, stop.Color);
                Attribute(output, " stop-opacity"u8, stop.Opacity);
                Raw(output, "/>"u8);
            }

            Raw(output, "</linearGradient>"u8);
        }

        Raw(output, "</defs>"u8);
    }

    /// <summary>A paint as the layout writes it: a gradient's reference renamed for the layout, any other color as it is.</summary>
    public static string Paint(string color, string? variant) =>
        variant is not null && color.StartsWith("url(#", StringComparison.Ordinal) && color.EndsWith(')')
            ? "url(#" + Named(color[5..^1], variant) + ")"
            : color;

    private static string Named(string id, string? variant) => variant is null ? id : id + "-" + variant;
}
