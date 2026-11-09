using System.Buffers.Text;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>
/// Legend toggles: what a toggled entry hides is marked with its entry's class (<c>pictor-sK</c>) on the web, where a
/// checkbox in the legend hides it through CSS, and skipped on the desktop, where the chart's spec lists what is
/// toggled off (<see cref="ChartSpec.Concealed"/>). Neither lays the chart out again.
/// </summary>
internal static class Toggles
{
    /// <summary>Whether the web marks what the legend toggles: toggles are on (a responsive chart's legend inside its SVG holds its checkboxes in foreign objects).</summary>
    public static bool Marked(ChartSpec spec) => spec.Legend is { Toggles: true };

    /// <summary>Whether the desktop has toggled a target off.</summary>
    public static bool Concealed(ChartSpec spec, int target) => !spec.Concealed.IsDefaultOrEmpty && spec.Concealed.Contains(target);

    /// <summary>Opens a group marked with a target's class when the web marks toggles; returns whether it did.</summary>
    public static bool Open<TCanvas>(ref TCanvas canvas, ChartSpec spec, int target)
        where TCanvas : struct, IChartCanvas
    {
        if (!Marked(spec) || target < 0)
        {
            return false;
        }

        Span<byte> classes = stackalloc byte[24];
        canvas.Open(classes[..Class(classes, "pictor-s"u8, target)]);
        return true;
    }

    /// <summary>Writes a prefix and a target's index (<c>pictor-s3</c>) and returns its length.</summary>
    public static int Class(Span<byte> buffer, ReadOnlySpan<byte> prefix, int target)
    {
        prefix.CopyTo(buffer);
        Utf8Formatter.TryFormat(target, buffer[prefix.Length..], out var written);
        return prefix.Length + written;
    }
}
