using System.Buffers;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>
/// The web's legend toggles without script: each item is a label holding a hidden checkbox (<c>pictor-tK</c>), and the
/// legend's style rules hide what a checked item stands for (everything marked <c>pictor-sK</c> in the chart: the
/// series, its tooltip entries and its active shapes) and draw the item in Recharts' inactive color. The rules are
/// scoped to their chart by <c>:has()</c> on its wrapper and read the same in every chart.
/// </summary>
internal static class LegendToggles
{
    public static void Checkbox(MarkupBuffer output, int target)
    {
        Span<byte> classes = stackalloc byte[24];
        Raw(output, "<label><input type=\"checkbox\" hidden class=\""u8);
        Raw(output, classes[..Toggles.Class(classes, "pictor-t"u8, target)]);
        Raw(output, "\">"u8);
    }

    public static void Styles(MarkupBuffer output, LegendLayout legend)
    {
        Raw(output, "<style>.recharts-legend-item label{cursor:pointer}.recharts-legend-item:has(:checked) .recharts-legend-item-text{color:var(--pictor-inactive,#ccc)!important}.recharts-legend-item:has(:checked) .recharts-surface [fill]:not([fill=none]){fill:var(--pictor-inactive,#ccc)}.recharts-legend-item:has(:checked) .recharts-surface [stroke]:not([stroke=none]){stroke:var(--pictor-inactive,#ccc)}"u8);
        var targets = legend.Items.Select(item => item.Target).Where(target => target >= 0).ToList();
        if (targets.Count > 0)
        {
            Hidden(output, targets, "."u8);
            Raw(output, "{visibility:hidden}"u8);
            Hidden(output, targets, " li."u8);
            Raw(output, "{display:none}"u8);
        }

        Raw(output, "</style>"u8);
    }

    /// <summary>
    /// The selectors of what checked items stand for. The drawing is hidden by its visibility, which, unlike its display,
    /// does not restart the enter animations of what is shown again (Firefox replays them); a tooltip's entry is taken out
    /// of its list, as Recharts leaves a hidden series out of the payload.
    /// </summary>
    private static void Hidden(MarkupBuffer output, List<int> targets, ReadOnlySpan<byte> scope)
    {
        Span<byte> classes = stackalloc byte[24];
        for (var i = 0; i < targets.Count; i++)
        {
            Raw(output, i == 0 ? ".recharts-wrapper:has(.pictor-t"u8 : ",.recharts-wrapper:has(.pictor-t"u8);
            Number(output, targets[i]);
            Raw(output, ":checked)"u8);
            Raw(output, scope[0] == (byte)'.' ? " ."u8 : scope);
            Raw(output, classes[..Toggles.Class(classes, "pictor-s"u8, targets[i])]);
        }
    }
}
