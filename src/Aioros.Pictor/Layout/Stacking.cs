using Aioros.Graphs.Shapes;
using Aioros.Pictor.Data;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>A stack of series on one numeric axis (Recharts' stack group): its series and their bands, point by point.</summary>
internal sealed class StackGroup(string id, List<SeriesSpec> series)
{
    public string Id { get; } = id;

    public List<SeriesSpec> Series { get; } = series;

    /// <summary>The bands of series <c>i</c> at <c>i * Count + point</c>, null until stacked.</summary>
    public StackPoint[]? Bands { get; set; }

    /// <summary>The items stacked: every item of the data, as Recharts stacks the whole data before a brush's window.</summary>
    public int Count { get; set; }

    /// <summary>The first item shown (a brush's start), from which a point of the shown items counts.</summary>
    public int Start { get; set; }

    /// <summary>How many items are shown from <see cref="Start"/>.</summary>
    public int Shown { get; set; }

    /// <summary>Whether a series is one of the group's, by reference: two series written alike are two series.</summary>
    public bool Holds(SeriesSpec spec) => IndexOf(spec) >= 0;

    /// <summary>A series' band at a shown point; none past the stacked items (a series' own items outnumbering the chart's, where Recharts fails).</summary>
    public StackPoint Band(SeriesSpec spec, int point) => Start + point < Count ? Bands![(IndexOf(spec) * Count) + Start + point] : new StackPoint(double.NaN, double.NaN);

    private int IndexOf(SeriesSpec spec)
    {
        for (var i = 0; i < Series.Count; i++)
        {
            if (ReferenceEquals(Series[i], spec))
            {
                return i;
            }
        }

        return -1;
    }
}

/// <summary>
/// Recharts' stack groups (<c>getStackGroupsByAxisId</c>, <c>getStackedData</c>): series with a stack id share a group,
/// the others stand alone; once one series of an axis stacks, every group of the axis is stacked through d3's stack
/// with the chart's offset (Recharts' own <c>sign</c> and <c>positive</c> offsets included).
/// </summary>
internal static class Stacking
{
    /// <summary>The stack groups of an axis over every item of the data, the points from <paramref name="start"/> for <paramref name="shown"/> items being the ones shown (every item when <paramref name="shown"/> is negative).</summary>
    public static List<StackGroup> Groups(ChartSpec spec, string axisId, bool horizontal, ChartData data, int start = 0, int shown = -1) =>
        Groups(spec.Series, spec, axisId, horizontal, data, start, shown);

    /// <summary>The stack groups of series of the chart's (a polar chart's radial bars as bars), with the chart's stack order and offset.</summary>
    public static List<StackGroup> Groups(System.Collections.Immutable.ImmutableArray<SeriesSpec> series, ChartSpec spec, string axisId, bool horizontal, ChartData data, int start = 0, int shown = -1)
    {
        var groups = new List<StackGroup>();
        var stacks = false;
        for (var k = 0; k < series.Length; k++)
        {
            var item = series[spec.ReverseStackOrder ? series.Length - 1 - k : k];
            if (!item.Hide && (horizontal ? item.YAxisId : item.XAxisId) == axisId)
            {
                Join(groups, item);
                stacks |= item.StackId is not null;
            }
        }

        if (stacks)
        {
            foreach (var group in groups)
            {
                Stack(group, data, spec.StackOffset);
                (group.Start, group.Shown) = (start, shown < 0 ? group.Count : shown);
            }
        }

        return groups;
    }

    private static void Join(List<StackGroup> groups, SeriesSpec series)
    {
        var group = series.StackId is { } id ? groups.Find(candidate => candidate.Id == id) : null;
        if (group is null)
        {
            groups.Add(new StackGroup(series.StackId ?? string.Empty, [series]));
        }
        else
        {
            group.Series.Add(series);
        }
    }

    public static (double Min, double Max) Domain(List<StackGroup> groups)
    {
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var group in groups.Where(group => group.Bands is not null))
        {
            for (var i = 0; i < group.Series.Count; i++)
            {
                for (var point = group.Start; point < Math.Min(group.Start + group.Shown, group.Count); point++)
                {
                    var band = group.Bands![(i * group.Count) + point];
                    Include(band.Lower, ref min, ref max);
                    Include(band.Upper, ref min, ref max);
                }
            }
        }

        return (double.IsInfinity(min) ? 0 : min, double.IsInfinity(max) ? 0 : max);
    }

    private static void Include(double value, ref double min, ref double max)
    {
        if (!double.IsNaN(value))
        {
            min = Math.Min(min, value);
            max = Math.Max(max, value);
        }
    }

    private static void Stack(StackGroup group, ChartData data, StackOffsetKind offset)
    {
        var n = group.Series.Count;
        var m = data.Count;
        group.Count = m;
        var values = new double[n * m];
        for (var i = 0; i < n; i++)
        {
            var column = data.Numbers(group.Series[i].DataKey);
            for (var j = 0; j < m; j++)
            {
                values[(j * n) + i] = double.IsNaN(column[j]) ? 0 : column[j];
            }
        }

        var bands = new StackPoint[n * m];
        if (m > 0)
        {
            Stacks.Compute(values, n, bands, new int[n], StackOrder.None, Graph(offset));
        }

        if (offset is StackOffsetKind.Sign or StackOffsetKind.Positive)
        {
            Signed(bands, values, n, m, offset == StackOffsetKind.Positive);
        }

        group.Bands = bands;
    }

    private static StackOffset Graph(StackOffsetKind offset) => offset switch
    {
        StackOffsetKind.Expand => StackOffset.Expand,
        StackOffsetKind.Silhouette => StackOffset.Silhouette,
        StackOffsetKind.Wiggle => StackOffset.Wiggle,
        _ => StackOffset.None,
    };

    private static void Signed(StackPoint[] bands, double[] values, int n, int m, bool positiveOnly)
    {
        for (var j = 0; j < m; j++)
        {
            var positive = 0.0;
            var negative = 0.0;
            for (var i = 0; i < n; i++)
            {
                var value = values[(j * n) + i];
                if (value >= 0)
                {
                    bands[(i * m) + j] = new StackPoint(positive, positive + value);
                    positive += value;
                }
                else if (positiveOnly)
                {
                    bands[(i * m) + j] = new StackPoint(0, 0);
                }
                else
                {
                    bands[(i * m) + j] = new StackPoint(negative, negative + value);
                    negative += value;
                }
            }
        }
    }
}
