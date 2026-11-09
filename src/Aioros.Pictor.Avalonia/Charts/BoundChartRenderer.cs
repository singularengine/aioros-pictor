using Aioros.Pictor.Svg;
using Aioros.Runtime.Rendering;

[assembly: SlotRenderer<Aioros.Pictor.BoundChartRenderer>]

namespace Aioros.Pictor;

/// <summary>
/// The renderer Aioros calls for a chart whose bound values are Avalonia's own (a color, a brush, a thickness, a corner
/// radius): Pictor's <see cref="ChartRenderer"/>, reading those values as the chart's spec reads them on the desktop, so a
/// bound brush is written in the same color or gradient on both targets. A chart without such a value is written by
/// <see cref="ChartRenderer"/>.
/// </summary>
public sealed class BoundChartRenderer : ISlotRenderer
{
    /// <inheritdoc />
    public static SlotRender Create(string argument, MemberLookup members) => ChartRenderer.Create(argument, members, AvaloniaValues.Plain);

    /// <inheritdoc cref="ChartRenderer.Decode"/>
    public static bool Decode(string key, string posted, ReadOnlySpan<object?> current, Span<string?> decoded) => ChartRenderer.Decode(key, posted, current, decoded);
}
