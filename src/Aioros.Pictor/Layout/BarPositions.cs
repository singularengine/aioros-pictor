using System.Globalization;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>Where a bar sits in its band and how wide it is.</summary>
internal readonly record struct BarSlot(double Offset, double Size);

/// <summary>
/// Recharts' <c>getBarSizeList</c> and <c>getBarPosition</c>: the bars of a category axis share its band, one slot
/// per stack group (stacked bars share their group's slot), separated by <c>barGap</c> inside a band padded by
/// <c>barCategoryGap</c>.
/// </summary>
internal static class BarPositions
{
    public static Dictionary<SeriesSpec, BarSlot> Compute(ChartSpec spec, IReadOnlyList<StackGroup> groups, double bandSize, double axisSize)
    {
        var entries = new List<(List<SeriesSpec> Bars, double? Size)>();
        foreach (var group in groups)
        {
            var bars = group.Series.FindAll(series => series.Kind == SeriesKind.Bar);
            if (bars.Count > 0)
            {
                var size = bars[0].BarSize ?? spec.BarSize;
                entries.Add((bars, size is null ? null : Percent(size, axisSize)));
            }
        }

        var slots = new Dictionary<SeriesSpec, BarSlot>(ReferenceEqualityComparer.Instance);
        if (entries.Count == 0)
        {
            return slots;
        }

        var gap = Percent(spec.BarGap, bandSize, clamp: true);
        if (entries[0].Size is not null)
        {
            Sized(entries, bandSize, gap, slots);
        }
        else
        {
            Shared(entries, bandSize, gap, Percent(spec.BarCategoryGap, bandSize, clamp: true), spec.MaxBarSize, slots);
        }

        return slots;
    }

    public static double Percent(string value, double total, bool clamp = false)
    {
        var trimmed = value.AsSpan().Trim();
        double result;
        if (trimmed.EndsWith('%'))
        {
            result = double.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) ? total * percent / 100 : 0;
        }
        else
        {
            result = double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : 0;
        }

        return clamp && result > total ? total : result;
    }

    private static void Sized(List<(List<SeriesSpec> Bars, double? Size)> entries, double bandSize, double gap, Dictionary<SeriesSpec, BarSlot> slots)
    {
        var count = entries.Count;
        var full = bandSize / count;
        var useFull = false;
        var sum = 0.0;
        foreach (var entry in entries)
        {
            sum += entry.Size ?? 0;
        }

        sum += (count - 1) * gap;
        if (sum >= bandSize)
        {
            sum -= (count - 1) * gap;
            gap = 0;
        }

        if (sum >= bandSize && full > 0)
        {
            useFull = true;
            full *= 0.9;
            sum = count * full;
        }

        var previousOffset = Truncate((bandSize - sum) / 2) - gap;
        var previousSize = 0.0;
        foreach (var (bars, size) in entries)
        {
            var slot = new BarSlot(previousOffset + previousSize + gap, useFull ? full : size ?? 0);
            foreach (var bar in bars)
            {
                slots[bar] = slot;
            }

            previousOffset = slot.Offset;
            previousSize = slot.Size;
        }
    }

    /// <summary>Bars without a size share the band; each bar caps every slot at its own <c>maxBarSize</c> (the chart's when it has none), as Recharts lays the band out once per bar.</summary>
    private static void Shared(List<(List<SeriesSpec> Bars, double? Size)> entries, double bandSize, double gap, double categoryGap, double? maxBarSize, Dictionary<SeriesSpec, BarSlot> slots)
    {
        var count = entries.Count;
        if (bandSize - (2 * categoryGap) - ((count - 1) * gap) <= 0)
        {
            gap = 0;
        }

        var original = (bandSize - (2 * categoryGap) - ((count - 1) * gap)) / count;
        if (original > 1)
        {
            original = Truncate(original);
        }

        for (var i = 0; i < count; i++)
        {
            foreach (var bar in entries[i].Bars)
            {
                var size = (bar.MaxBarSize ?? maxBarSize) is { } max ? Math.Min(original, max) : original;
                slots[bar] = new BarSlot(categoryGap + ((original + gap) * i) + ((original - size) / 2), size);
            }
        }
    }

    private static double Truncate(double value) => (int)value;
}
