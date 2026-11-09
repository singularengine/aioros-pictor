using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>Recharts' legend payload of the polar charts: a pie chart's sectors, a radial bar chart's items, a radar chart's radars.</summary>
internal static class PolarLegend
{
    public static List<LegendEntry> Entries(PolarLayout layout, LegendSpec legend, List<PieLayout>? pies) => layout.Spec.Kind switch
    {
        ChartKind.Pie => PieEntries(legend, pies ?? []),
        ChartKind.RadialBar => BarEntries(layout.Data, layout.Polar.RadialBars, legend),
        _ => [.. layout.Polar.Radars.Select((radar, target) => new LegendEntry(radar.Name ?? radar.DataKey, radar.Stroke is { } stroke && stroke != "none" ? stroke : radar.Fill ?? string.Empty, legend.IconType ?? radar.LegendType, target, radar.Hide))],
    };

    /// <summary>Recharts' legend of a pie chart: every pie's sectors, a hidden pie's too (it is laid out, not drawn), which no toggle reaches.</summary>
    private static List<LegendEntry> PieEntries(LegendSpec legend, List<PieLayout> pies)
    {
        var entries = new List<LegendEntry>();
        var target = 0;
        foreach (var pie in pies)
        {
            foreach (var item in pie.Items)
            {
                entries.Add(new LegendEntry(item.Name, item.Fill, legend.IconType ?? pie.Spec.LegendType, pie.Spec.Hide ? -1 : target++));
            }
        }

        return entries;
    }

    /// <summary>
    /// Recharts 3's legend of a radial bar chart (<c>selectRadialBarLegendPayload</c>): every radial bar's, a hidden one's
    /// too, lists each data entry by its <c>name</c> and <c>fill</c> fields, and the legend orders the entries by their
    /// names (its default <c>itemSorter="value"</c>, a stable sort that puts the entries without a name last).
    /// </summary>
    private static List<LegendEntry> BarEntries(Data.ChartData data, IReadOnlyList<RadialBarSpec> bars, LegendSpec legend)
    {
        var names = data.Texts("name");
        var fills = Painting.CssValues.Valid(data.Texts("fill"));
        var entries = new List<(string? Name, LegendEntry Entry)>();
        foreach (var bar in bars)
        {
            for (var i = 0; i < data.Count; i++)
            {
                entries.Add((names[i], new LegendEntry(names[i] ?? string.Empty, fills[i] ?? string.Empty, legend.IconType ?? bar.LegendType, bar.Hide ? -1 : i)));
            }
        }

        return [.. entries.OrderBy(static entry => entry.Name is null).ThenBy(static entry => entry.Name ?? string.Empty, StringComparer.Ordinal).Select(static entry => entry.Entry)];
    }
}
