using System.Collections;
using System.Collections.Immutable;
using Aioros.Graphs.Formatting;
using Aioros.Pictor.Data;

namespace Aioros.Pictor.Specs;

/// <summary>
/// Items carried inside a spec as columns of texts, numbers written as JavaScript writes them: the data a series holds
/// itself (Recharts' <c>data</c> on a <c>Pie</c>, a <c>Scatter</c> or a <c>Funnel</c>), which reaches the web writer with
/// the spec rather than through the chart's bound <c>Data</c>.
/// </summary>
public sealed record DataTable
{
    /// <summary>The number of items.</summary>
    public int Count { get; init; }

    /// <summary>The values of every key the series reads, one per item, null where an item has none.</summary>
    public ImmutableDictionary<string, ImmutableArray<string?>> Columns { get; init; } = ImmutableDictionary<string, ImmutableArray<string?>>.Empty;

    /// <summary>The columns of the given keys, read from a list of items (a key the items lack is read from its capitalized member, <see cref="ChartData.Resolve"/>).</summary>
    /// <param name="items">The items.</param>
    /// <param name="keys">The keys the series reads.</param>
    /// <param name="members">How the items' members are read.</param>
    /// <returns>The table.</returns>
    public static DataTable Read(IEnumerable? items, IEnumerable<string?> keys, DataMembers members)
    {
        var data = ChartData.From(items, members);
        var columns = ImmutableDictionary.CreateBuilder<string, ImmutableArray<string?>>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            if (key is null || columns.ContainsKey(key))
            {
                continue;
            }

            var numbers = data.Numbers(data.Resolve(key));
            var texts = data.Texts(data.Resolve(key));
            var column = ImmutableArray.CreateBuilder<string?>(data.Count);
            for (var i = 0; i < data.Count; i++)
            {
                column.Add(texts[i] ?? (double.IsNaN(numbers[i]) ? null : JsNumber.ToShortestString(numbers[i])));
            }

            columns[key] = column.MoveToImmutable();
        }

        return new DataTable { Count = data.Count, Columns = columns.ToImmutable() };
    }

    /// <summary>
    /// Whether a list of items reads into this table (<see cref="Read"/>), compared on the live items without building a
    /// table: each key resolved from the first item as the read resolves it, a number against its text by its value, any
    /// other value by its text, so a series' own items bound to the model cost no table per request while they hold.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="keys">The keys the series reads.</param>
    /// <param name="members">How the items' members are read.</param>
    /// <returns>Whether the items give this table.</returns>
    public bool Matches(IEnumerable? items, ImmutableArray<string> keys, DataMembers members)
    {
        var list = items as IList ?? ChartData.From(items, members).Items.ToArray();
        if (list.Count != Count || Columns.Count != keys.Distinct(StringComparer.Ordinal).Count())
        {
            return false;
        }

        object? first = null;
        for (var i = 0; i < list.Count && first is null; i++)
        {
            first = list[i];
        }

        foreach (var (key, column) in Columns)
        {
            var resolved = ChartData.ResolveOn(first, key, members);
            Type? type = null;
            Func<object, object?>? getter = null;
            for (var i = 0; i < list.Count; i++)
            {
                if (!Same(ChartData.Read(list[i], resolved, members, ref type, ref getter), column[i]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool Same(object? value, string? text) => value switch
    {
        null => text is null,
        string live => string.Equals(live, text, StringComparison.Ordinal),
        double or float or int or long or short or byte or decimal or uint or ulong => text is not null
            && double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            && BitConverter.DoubleToInt64Bits(parsed) == BitConverter.DoubleToInt64Bits(ChartData.Number(value)),
        _ => string.Equals(ChartData.Text(value), text, StringComparison.Ordinal),
    };
}
