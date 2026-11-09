namespace Aioros.Pictor.Layout;

/// <summary>A polar chart's hover states and the lookups its tooltips make, built once per layout instead of searched per state.</summary>
internal static class PolarHovers
{
    /// <summary>
    /// The hover states: a pie chart's sectors, a radial bar chart's drawn sectors with an item tooltip, or the
    /// categories; none without a tooltip, and none in a radial bar chart without a shown radial bar.
    /// </summary>
    public static int Count(PolarLayout layout)
    {
        if (layout.Spec.Tooltip is null || (layout.Spec.Kind == Specs.ChartKind.RadialBar && layout.RadialBars.Count == 0))
        {
            return 0;
        }

        var count = 0;
        if (layout.Spec.Kind == Specs.ChartKind.Pie)
        {
            foreach (var pie in layout.Pies)
            {
                count += pie.Sectors.Length;
            }

            return count;
        }

        if (!layout.ItemBars)
        {
            return layout.Categories.Length;
        }

        foreach (var bar in layout.RadialBars)
        {
            count += bar.Sectors.Count(static sector => sector.Shape.Drawn);
        }

        return count;
    }

    /// <summary>The radial bar and sector of each item hover state: the drawn sectors in order, bar after bar.</summary>
    public static (RadialBarLayout Bar, int Index)[] ItemStates(List<RadialBarLayout> bars) =>
        [.. bars.SelectMany(static bar => Enumerable.Range(0, bar.Sectors.Length).Where(i => bar.Sectors[i].Shape.Drawn).Select(i => (bar, i)))];

    /// <summary>The categories of a category axis: the data's, deduplicated when duplicates are not allowed (Recharts' <c>getAxisMapByAxes</c>).</summary>
    public static string?[] Distinct(string?[] texts, bool allowDuplicates)
    {
        if (allowDuplicates)
        {
            return texts;
        }

        var seen = new HashSet<string?>(texts.Length, StringComparer.Ordinal);
        return [.. texts.Where(seen.Add)];
    }

    /// <summary>Each text's first index (as <c>indexOf</c> finds it), a missing text as the empty one.</summary>
    public static Dictionary<string, int> FirstIndexes(IReadOnlyList<string?> texts)
    {
        var first = new Dictionary<string, int>(texts.Count, StringComparer.Ordinal);
        for (var i = 0; i < texts.Count; i++)
        {
            first.TryAdd(texts[i] ?? string.Empty, i);
        }

        return first;
    }
}
