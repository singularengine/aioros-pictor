using System.Globalization;
using Aioros.Graphs.Scales;
using Aioros.Pictor.Data;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Ticks;

namespace Aioros.Pictor.Layout;

/// <summary>A scatter chart's z-axis laid out: the scale from its values to symbol areas.</summary>
internal sealed class ZAxisLayout
{
    public required ZAxisSpec Spec { get; init; }

    public ValueScale? Scale { get; init; }

    /// <summary>The categories of a category z-axis, each a band of the range.</summary>
    public BandScale? Bands { get; init; }

    public string?[] Categories { get; init; } = [];

    private Dictionary<string, int>? _bands;

    /// <summary>
    /// The symbol area of an item (Recharts' <c>Scatter</c>: <c>zAxis.scale(z)</c>), or the range's lower bound when the
    /// axis reads no value or a falsy one.
    /// </summary>
    public double Size(ChartData data, int index)
    {
        if (Spec.DataKey is not { } key)
        {
            return Spec.RangeMin;
        }

        if (Bands is not null)
        {
            var category = data.Texts(key)[index];
            var band = category is null or "" ? -1 : TextIndex.Find(_bands ??= TextIndex.Of(Categories), category);
            return category is null or "" ? Spec.RangeMin : band < 0 ? double.NaN : Bands.Map(band);
        }

        var z = data.Numbers(key)[index];
        return Scale is not { } scale || double.IsNaN(z) || z == 0 ? Spec.RangeMin : scale.Map(z);
    }
}

/// <summary>
/// Recharts' axis maps (<c>getAxisMapByAxes</c>): each axis' scale kind and domain from the data its series display (their
/// own data, or the chart's), the stacks, the error bars of the values' axis and the reference elements that extend it,
/// then the specified domain and recharts-scale's nice ticks.
/// </summary>
internal readonly struct AxisDomains(ChartLayout layout, Dictionary<(bool IsX, string Id), List<StackGroup>> stacks)
{
    public AxisLayout Axis(AxisSpec spec, bool isX, bool pointScale)
    {
        if (spec.Type == AxisType.Category)
        {
            var categories = spec.DataKey is { } key ? Categories(spec, Texts(Displayed(spec.Id, isX), key)) : [.. Enumerable.Range(0, layout.Data.Count).Select(index => index.ToString(CultureInfo.InvariantCulture))];
            return new AxisLayout { Spec = spec, IsX = isX, Kind = spec.Scale switch { "band" => ScaleKind.Band, "point" => ScaleKind.Point, _ => pointScale ? ScaleKind.Point : ScaleKind.Band }, Categories = categories };
        }

        var axis = new AxisLayout { Spec = spec, IsX = isX, Kind = ValueScale.Kind(spec.Scale) };
        var (min, max) = SpecifiedTicks(spec, References(spec, isX, DataDomain(axis, spec, isX)));

        var low = Bound(spec.DomainMin ?? "0", min, max, spec.AllowDataOverflow, lower: true);
        var high = Bound(spec.DomainMax ?? "auto", min, max, spec.AllowDataOverflow, lower: false);
        axis.Spread = high - low;
        if (!ValueScale.IsNice(spec.Scale))
        {
            axis.CategoricalTicks = isX != layout.Vertical && spec.DataKey is { } key ? Values(Displayed(spec.Id, isX), key) : null;
        }
        else
        {
            (low, high) = Nice(axis, low, high);
        }

        axis.DomainMin = low;
        axis.DomainMax = high;
        return axis;
    }

    public ZAxisLayout ZAxis(ZAxisSpec spec)
    {
        if (spec.DataKey is not { } key)
        {
            return new ZAxisLayout { Spec = spec };
        }

        var displayed = Displayed(layout.Spec.Series.Where(series => series.ZAxisId == spec.Id));
        if (spec.Type == AxisType.Category)
        {
            var categories = Distinct([.. Texts(displayed, key).Where(text => !string.IsNullOrEmpty(text))]);
            return new ZAxisLayout { Spec = spec, Categories = categories, Bands = BandScale.Create(categories.Length, spec.RangeMin, spec.RangeMax) };
        }

        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var data in displayed)
        {
            (min, max) = Extent(data.Numbers(key), min, max);
        }

        var low = Bound(spec.DomainMin ?? "0", min, max, overflow: false, lower: true);
        var high = Bound(spec.DomainMax ?? "auto", min, max, overflow: false, lower: false);
        return new ZAxisLayout { Spec = spec, Scale = ValueScale.Create(ValueScale.Kind(spec.Scale), low, high, spec.RangeMin, spec.RangeMax) };
    }

    /// <summary>
    /// A category axis' categories as Recharts' <c>getAxisMapByAxes</c> derives them: the data's, deduplicated when
    /// duplicates are not allowed (the specified domain's when there is one), and replaced by the specified domain when it
    /// holds every one of them and the data has no duplicate kept by index.
    /// </summary>
    private static string?[] Categories(AxisSpec spec, string?[] data)
    {
        var specified = spec.CategoryDomain;
        if (!spec.AllowDuplicatedCategory)
        {
            return Distinct(specified.IsEmpty ? data : [.. specified]);
        }

        if (specified.IsEmpty || Distinct(data).Length != data.Length)
        {
            return data;
        }

        var known = new HashSet<string>(specified, StringComparer.Ordinal);
        return Array.TrueForAll(data, category => category is not null && known.Contains(category)) ? [.. specified] : data;
    }

    private static string?[] Distinct(string?[] values)
    {
        var seen = new HashSet<string?>(values.Length, StringComparer.Ordinal);
        return [.. values.Where(seen.Add)];
    }

    /// <summary>recharts-scale's nice ticks of an <c>auto</c> scale (<c>getTicksOfScale</c>): an automatic bound rounds the domain to them, a fixed domain keeps its bounds; a tick count of 0 (falsy) asks for none.</summary>
    private static (double Low, double High) Nice(AxisLayout axis, double low, double high)
    {
        var spec = axis.Spec;
        if (spec.TickCount == 0)
        {
            return (low, high);
        }

        if ((spec.DomainMin ?? "0") != "auto" && (spec.DomainMax ?? "auto") != "auto")
        {
            axis.NiceTicks = NiceTicks.FixedDomain(low, high, spec.TickCount, spec.AllowDecimals);
            return (low, high);
        }

        var ticks = NiceTicks.Compute(low, high, spec.TickCount, spec.AllowDecimals);
        axis.NiceTicks = ticks;
        return ticks.Length > 0 ? (ticks.Min(), ticks.Max()) : (low, high);
    }

    /// <summary>A data key's values across the displayed items, in order.</summary>
    private static double[] Values(List<ChartData> displayed, string key)
    {
        if (displayed.Count == 1)
        {
            return displayed[0].Numbers(key);
        }

        var values = new List<double>();
        foreach (var data in displayed)
        {
            values.AddRange(data.Numbers(key));
        }

        return [.. values];
    }

    private static bool OnAxis(SeriesSpec series, string id, bool isX) => (isX ? series.XAxisId : series.YAxisId) == id;

    /// <summary>The items an axis reads: those of its series that carry their own data, or the chart's (Recharts' <c>getDisplayedData</c>).</summary>
    private List<ChartData> Displayed(string id, bool isX)
    {
        List<ChartData>? own = null;
        var all = layout.Spec.Series;
        for (var i = 0; i < all.Length; i++)
        {
            if (all[i].Data is { Count: > 0 } && OnAxis(all[i], id, isX))
            {
                (own ??= []).Add(layout.SeriesData(all[i]));
            }
        }

        return own ?? layout.ChartDataList;
    }

    private List<ChartData> Displayed(IEnumerable<SeriesSpec> series)
    {
        var own = series.Where(item => item.Data is { Count: > 0 }).Select(layout.SeriesData).ToList();
        return own.Count > 0 ? own : layout.ChartDataList;
    }

    private static string?[] Texts(List<ChartData> displayed, string key) =>
        displayed.Count == 1 ? displayed[0].Texts(key) : [.. displayed.SelectMany(data => data.Texts(key))];

    /// <summary>The smallest distance between two of an axis' values, sorted, which its <c>gap</c> padding halves (a missing value counts as 0, as in Recharts).</summary>
    private static double SmallestGap(List<ChartData> displayed, string key)
    {
        var values = new List<double>();
        foreach (var data in displayed)
        {
            foreach (var value in data.Numbers(key))
            {
                values.Add(double.IsNaN(value) ? 0 : value);
            }
        }

        values.Sort();
        var smallest = double.PositiveInfinity;
        for (var i = 1; i < values.Count; i++)
        {
            smallest = Math.Min(values[i] - values[i - 1], smallest);
        }

        return smallest;
    }

    private (double Min, double Max) DataDomain(AxisLayout axis, AxisSpec spec, bool isX)
    {
        var displayed = Displayed(spec.Id, isX);
        var groups = Stacking.Groups(layout.Spec, spec.Id, horizontal: !isX, layout.WholeData, layout.WindowStart, layout.Data.Count);
        stacks[(isX, spec.Id)] = groups;
        if (spec.DataKey is { } key)
        {
            var domain = (double.PositiveInfinity, double.NegativeInfinity);
            foreach (var data in displayed)
            {
                domain = Extent(data.Numbers(key), domain.Item1, domain.Item2);
            }

            axis.Gap = spec.PaddingKind == AxisPaddingKind.Fixed ? double.PositiveInfinity : SmallestGap(displayed, key);
            return AxisErrors(displayed, spec.Id, key, isX, spec.IncludeHidden) ?? domain;
        }

        if (groups.Exists(group => group.Bands is not null))
        {
            return layout.Spec.StackOffset == StackOffsetKind.Expand ? (0, 1) : Stacking.Domain(groups);
        }

        return SeriesDomain(displayed, spec, isX);
    }

    /// <summary>The extent of the values of the series an axis maps (Recharts' <c>getDomainOfItemsWithSameAxis</c>), the hidden ones with <c>includeHidden</c>.</summary>
    private (double Min, double Max) SeriesDomain(List<ChartData> displayed, AxisSpec spec, bool isX)
    {
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var series in layout.Spec.Series)
        {
            if ((series.Hide && !spec.IncludeHidden) || !OnAxis(series, spec.Id, isX))
            {
                continue;
            }

            (min, max) = ErrorBars(displayed, series, series.DataKey, isX) is { } errors
                ? (Math.Min(min, errors.Min), Math.Max(max, errors.Max))
                : ExtentOf(displayed, series.DataKey, min, max);
        }

        return (min, max);
    }

    private static (double Min, double Max) ExtentOf(List<ChartData> displayed, string key, double min, double max)
    {
        foreach (var data in displayed)
        {
            (min, max) = Extent(data.Numbers(key), min, max);
            (min, max) = Extent(data.Lows(key), min, max);
        }

        return (min, max);
    }

    /// <summary>The domain of an axis' data key widened by the error bars of its visible series, or null when none has any.</summary>
    private (double Min, double Max)? AxisErrors(List<ChartData> displayed, string id, string key, bool isX, bool includeHidden)
    {
        (double Min, double Max)? domain = null;
        foreach (var series in layout.Spec.Series)
        {
            if ((!series.Hide || includeHidden) && OnAxis(series, id, isX) && ErrorBars(displayed, series, key, isX) is { } errors)
            {
                domain = domain is { } known ? (Math.Min(known.Min, errors.Min), Math.Max(known.Max, errors.Max)) : errors;
            }
        }

        return domain;
    }

    /// <summary>Recharts' <c>getDomainOfErrorBars</c>: the values plus and minus their errors, on the values' axis only.</summary>
    private (double Min, double Max)? ErrorBars(List<ChartData> displayed, SeriesSpec series, string key, bool isX)
    {
        if (isX != layout.Vertical || series.ErrorBars.IsEmpty)
        {
            return null;
        }

        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var data in displayed)
        {
            (min, max) = ErrorExtent(data, series, key, min, max);
        }

        return (min, max);
    }

    private static (double Min, double Max) ErrorExtent(ChartData data, SeriesSpec item, string key, double min, double max)
    {
        var values = data.Numbers(key);
        var lows = data.Lows(key);
        for (var i = 0; i < values.Length; i++)
        {
            if (double.IsNaN(values[i]))
            {
                continue;
            }

            var (first, last) = lows.Length > 0 && !double.IsNaN(lows[i]) ? (Math.Min(lows[i], values[i]), Math.Max(lows[i], values[i])) : (values[i], values[i]);
            var lower = double.PositiveInfinity;
            var upper = double.NegativeInfinity;
            foreach (var bar in item.ErrorBars)
            {
                var (below, above) = Error(data, bar.DataKey, i);
                lower = Math.Min(lower, first - Math.Abs(below));
                upper = Math.Max(upper, last + Math.Abs(above));
            }

            min = Math.Min(min, lower);
            max = Math.Max(max, upper);
        }

        return (min, max);
    }

    /// <summary>An item's error below and above its value: both ends of a <c>[low, high]</c> pair, or one value for both; 0 where missing.</summary>
    internal static (double Below, double Above) Error(ChartData data, string key, int index)
    {
        var above = data.Numbers(key)[index];
        var lows = data.Lows(key);
        var below = lows.Length > 0 && !double.IsNaN(lows[index]) ? lows[index] : above;
        return (double.IsNaN(below) ? 0 : below, double.IsNaN(above) ? 0 : above);
    }

    private (double Min, double Max) References(AxisSpec spec, bool isX, (double Min, double Max) domain)
    {
        var chart = layout.Spec;
        if (chart.ReferenceLines.IsEmpty && chart.ReferenceDots.IsEmpty && chart.ReferenceAreas.IsEmpty)
        {
            return domain;
        }

        var lines = chart.ReferenceLines.Where(line => line.IfOverflow == OverflowKind.ExtendDomain)
            .Select(line => (Id: Pick(isX, line.XAxisId, line.YAxisId), First: Pick(isX, line.X, line.Y), Second: Pick(isX, line.X, line.Y)));
        var dots = chart.ReferenceDots.Where(dot => dot.IfOverflow == OverflowKind.ExtendDomain)
            .Select(dot => (Id: Pick(isX, dot.XAxisId, dot.YAxisId), First: Pick(isX, dot.X, dot.Y), Second: Pick(isX, dot.X, dot.Y)));
        var areas = chart.ReferenceAreas.Where(area => area.IfOverflow == OverflowKind.ExtendDomain)
            .Select(area => (Id: Pick(isX, area.XAxisId, area.YAxisId), First: Pick(isX, area.X1, area.Y1), Second: Pick(isX, area.X2, area.Y2)));
        foreach (var (_, first, second) in lines.Concat(dots).Concat(areas).Where(reference => reference.Id == spec.Id))
        {
            if (Parse(first) is { } a && Parse(second) is { } b)
            {
                domain = (Math.Min(domain.Min, Math.Min(a, b)), Math.Max(domain.Max, Math.Max(a, b)));
            }
        }

        return domain;
    }

    /// <summary>An explicit ticks list widens the domain to its numbers (the end of Recharts' <c>detectReferenceElementsDomain</c>).</summary>
    private static (double Min, double Max) SpecifiedTicks(AxisSpec spec, (double Min, double Max) domain)
    {
        foreach (var tick in spec.Ticks)
        {
            if (Parse(tick) is { } value)
            {
                domain = (Math.Min(domain.Min, value), Math.Max(domain.Max, value));
            }
        }

        return domain;
    }

    private static string? Pick(bool isX, string? x, string? y) => isX ? x : y;

    private static double? Parse(string? text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static (double Min, double Max) Extent(double[] values, double min, double max)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (!double.IsNaN(values[i]))
            {
                min = Math.Min(min, values[i]);
                max = Math.Max(max, values[i]);
            }
        }

        return (min, max);
    }

    internal static double Bound(string text, double min, double max, bool overflow, bool lower)
    {
        var data = lower ? min : max;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            data = overflow ? number : lower ? Math.Min(number, data) : Math.Max(number, data);
        }
        else if (Shift(text, lower ? "dataMin" : "dataMax") is { } shift)
        {
            data = lower ? data - shift : data + shift;
        }

        return double.IsInfinity(data) ? 0 : data;
    }

    /// <summary>The n of Recharts' <c>dataMin - n</c> or <c>dataMax + n</c> (its <c>MIN_VALUE_REG</c> and <c>MAX_VALUE_REG</c>: the minimum only subtracts, the maximum only adds).</summary>
    private static double? Shift(string text, string name)
    {
        var trimmed = text.Replace(" ", string.Empty, StringComparison.Ordinal);
        var sign = name == "dataMin" ? '-' : '+';
        if (trimmed.Length <= name.Length + 1 || !trimmed.StartsWith(name, StringComparison.Ordinal) || trimmed[name.Length] != sign)
        {
            return null;
        }

        var number = trimmed[(name.Length + 1)..];
        return IsUnsignedDecimal(number) ? double.Parse(number, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture) : null;
    }

    /// <summary>Recharts' <c>([0-9]+([.]{1}[0-9]+){0,1})</c>: digits, then a point and digits or nothing.</summary>
    private static bool IsUnsignedDecimal(string text)
    {
        var point = text.IndexOf('.', StringComparison.Ordinal);
        var whole = point < 0 ? text.AsSpan() : text.AsSpan(0, point);
        var fraction = point < 0 ? "0".AsSpan() : text.AsSpan(point + 1);
        return !whole.IsEmpty && !fraction.IsEmpty && !whole.ContainsAnyExceptInRange('0', '9') && !fraction.ContainsAnyExceptInRange('0', '9');
    }
}
