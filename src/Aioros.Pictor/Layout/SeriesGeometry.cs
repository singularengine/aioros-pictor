using Aioros.Pictor.Data;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>A bar laid out: its rectangle, the value it shows and its background.</summary>
internal readonly record struct BarRect(double X, double Y, double Width, double Height, double Value);

/// <summary>A series laid out: its points (lines, areas, scatters) or its bars, through its axes.</summary>
internal sealed class SeriesLayout
{
    private (string Key, Dictionary<string, int> Index)? _firstItems;

    /// <summary>Each item's own valid fill, for a series that names the member; null otherwise.</summary>
    public string?[]? ItemFills => Spec.FillKey is { } key ? _itemFills ??= Painting.CssValues.Valid(Data.Texts(key)) : null;

    private string?[]? _itemFills;

    /// <summary>Each text's first item in a column of the series' data, built once per key (the category axis' key).</summary>
    public Dictionary<string, int> FirstItems(string key)
    {
        if (_firstItems is not { } known || known.Key != key)
        {
            _firstItems = known = (key, TextIndex.Of(Data.Texts(key)));
        }

        return known.Index;
    }

    public required SeriesSpec Spec { get; init; }

    public required AxisLayout XAxis { get; init; }

    public required AxisLayout YAxis { get; init; }

    /// <summary>The items the series reads: its own data, or the chart's.</summary>
    public required ChartData Data { get; init; }

    /// <summary>The series' values (a scatter's y values), NaN where missing.</summary>
    public double[] Values { get; init; } = [];

    /// <summary>A scatter's x values.</summary>
    public double[] XValues { get; init; } = [];

    /// <summary>A scatter's z values, when it has a z-axis with a data key.</summary>
    public double[] ZValues { get; init; } = [];

    /// <summary>A scatter's symbol areas.</summary>
    public double[] Sizes { get; init; } = [];

    public double[] X { get; init; } = [];

    public double[] Y { get; init; } = [];

    /// <summary>An area's baseline, point by point: its y in a horizontal chart, its x in a vertical one.</summary>
    public double[] Base { get; init; } = [];

    /// <summary>The lower ends of the series' range values (<c>[low, high]</c>), NaN where a value is a single number; empty when none is a pair.</summary>
    public double[] Lows { get; init; } = [];

    /// <summary>A stacked bar's value as Recharts holds it, the top of its band within the domain, where its error bars stand; empty when it is not stacked or has none.</summary>
    public double[] StackTops { get; init; } = [];

    /// <summary>Whether an area's values are ranges (Recharts' <c>isRange</c>): its baseline follows their lower ends and is stroked too.</summary>
    public bool IsRange => Lows.Length > 0;

    public bool[] Defined { get; init; } = [];

    public BarRect[] Bars { get; init; } = [];

    /// <summary>The axis the values map through.</summary>
    public required AxisLayout ValueAxis { get; init; }

    /// <summary>The theme the series takes its default colors from.</summary>
    public Theming.ChartTheme Theme { get; init; } = Theming.ChartTheme.Light;

    /// <summary>Whether the series stays where it is during an update animation of its chart (drawn as at rest, its dots and labels shown).</summary>
    public bool Still { get; init; }

    /// <summary>A copy of the series with other coordinates (a frame of an update animation, whose arrays it owns), or unchanged and still.</summary>
    public SeriesLayout Morphed(double[] x, double[] y, double[] bases, bool[] defined, BarRect[] bars, double[] sizes, bool still) => new()
    {
        Spec = Spec,
        XAxis = XAxis,
        YAxis = YAxis,
        ValueAxis = ValueAxis,
        Data = Data,
        Values = Values,
        XValues = XValues,
        ZValues = ZValues,
        Sizes = sizes,
        X = x,
        Y = y,
        Base = bases,
        Lows = Lows,
        StackTops = StackTops,
        Defined = defined,
        Bars = bars,
        Still = still,
        Theme = Theme,
    };


    public string Stroke => StrokeOf(Spec, Theme);

    public string Fill => FillOf(Spec, Theme);

    /// <summary>The series' color in the legend and the tooltip, as Recharts' <c>getMainColorOfGraphicItem</c> picks it.</summary>
    public string Color => ColorOf(Spec, Theme);

    /// <summary>A series' stroke, or its kind's default.</summary>
    public static string StrokeOf(SeriesSpec spec, Theming.ChartTheme theme) => spec.Stroke ?? (spec.Kind is SeriesKind.Bar or SeriesKind.Scatter ? "none" : theme.SeriesColor);

    /// <summary>A series' fill, or its kind's default.</summary>
    public static string FillOf(SeriesSpec spec, Theming.ChartTheme theme) => spec.Fill ?? (spec.Kind == SeriesKind.Line ? theme.DotFill : theme.SeriesColor);

    /// <summary>A series' main color, laid out or hidden: a line's stroke, an area's stroke or fill, a bar's or a scatter's fill.</summary>
    public static string ColorOf(SeriesSpec spec, Theming.ChartTheme theme) => spec.Kind switch
    {
        SeriesKind.Line => StrokeOf(spec, theme),
        SeriesKind.Area => StrokeOf(spec, theme) == "none" ? FillOf(spec, theme) : StrokeOf(spec, theme),
        _ => FillOf(spec, theme),
    };

    public string Name => Spec.Name ?? Spec.DataKey;

    /// <summary>How many points a scatter shows, one hover state each.</summary>
    public int PointCount
    {
        get
        {
            if (Spec.Kind != SeriesKind.Scatter)
            {
                return 0;
            }

            var count = 0;
            foreach (var defined in Defined)
            {
                count += defined ? 1 : 0;
            }

            return count;
        }
    }
}

/// <summary>
/// The geometry of every series, as Recharts' <c>getComposedData</c> of Line, Area, Bar and Scatter computes it, in a
/// horizontal chart (categories along x) or a vertical one (categories down y).
/// </summary>
internal static class SeriesGeometry
{
    private static readonly Dictionary<SeriesSpec, BarSlot> NoSlots = new(ReferenceEqualityComparer.Instance);

    public static void Compute(ChartLayout layout, Dictionary<(bool IsX, string Id), List<StackGroup>> stacks)
    {
        var slots = Slots(layout, stacks);
        foreach (var series in layout.Spec.Series.Where(series => !series.Hide))
        {
            var x = layout.XAxis(series.XAxisId);
            var y = layout.YAxis(series.YAxisId);
            var value = layout.ValueAxis(series);
            var group = stacks.TryGetValue((layout.Vertical, value.Spec.Id), out var groups) ? StackOf(groups, series) : null;
            layout.Series.Add(series.Kind switch
            {
                SeriesKind.Bar => Bars(layout, series, x, y, group, slots.TryGetValue(series, out var slot) ? slot : new BarSlot(0, 0)),
                SeriesKind.Scatter when layout.Spec.Kind == ChartKind.Scatter => ScatterGeometry.Points(layout, series, x, y),
                _ => Points(layout, series, x, y, group),
            });
        }
    }

    /// <summary>The stacked group holding a series, if any.</summary>
    private static StackGroup? StackOf(List<StackGroup> groups, SeriesSpec series)
    {
        for (var i = 0; i < groups.Count; i++)
        {
            if (groups[i].Bands is not null && groups[i].Holds(series))
            {
                return groups[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Recharts' <c>getBarSizeList</c>: the stack groups of every numeric axis, in the order of its object keys (integer
    /// ids ascending, then the others as the first visible series names them), share the band of the category axis their
    /// first bar maps through.
    /// </summary>
    private static Dictionary<SeriesSpec, BarSlot> Slots(ChartLayout layout, Dictionary<(bool IsX, string Id), List<StackGroup>> stacks)
    {
        var series = layout.Spec.Series;
        if (!series.Any(static item => item.Kind == SeriesKind.Bar && !item.Hide))
        {
            return NoSlots;
        }

        var vertical = layout.Vertical;
        var slots = new Dictionary<SeriesSpec, BarSlot>(ReferenceEqualityComparer.Instance);
        foreach (var (category, groups) in ByCategory(stacks, JsKeys(series, vertical, layout.Spec.ReverseStackOrder), vertical))
        {
            var axis = vertical ? layout.YAxis(category) : layout.XAxis(category);
            foreach (var pair in BarPositions.Compute(layout.Spec, groups, axis.BandSize, vertical ? axis.Height : axis.Width))
            {
                slots[pair.Key] = pair.Value;
            }
        }

        return slots;
    }

    /// <summary>The stack groups holding bars, value axis after value axis, under the category axis of their first bar.</summary>
    private static List<(string Category, List<StackGroup> Groups)> ByCategory(Dictionary<(bool IsX, string Id), List<StackGroup>> stacks, List<string> ids, bool vertical)
    {
        var byCategory = new List<(string Category, List<StackGroup> Groups)>(1);
        foreach (var id in ids)
        {
            if (stacks.TryGetValue((vertical, id), out var groups))
            {
                Add(byCategory, groups, vertical);
            }
        }

        return byCategory;
    }

    private static void Add(List<(string Category, List<StackGroup> Groups)> byCategory, List<StackGroup> groups, bool vertical)
    {
        foreach (var group in groups)
        {
            if (group.Series.Find(static item => item.Kind == SeriesKind.Bar) is not { } bar)
            {
                continue;
            }

            var category = vertical ? bar.YAxisId : bar.XAxisId;
            var index = 0;
            while (index < byCategory.Count && byCategory[index].Category != category)
            {
                index++;
            }

            if (index == byCategory.Count)
            {
                byCategory.Add((category, [group]));
            }
            else
            {
                byCategory[index].Groups.Add(group);
            }
        }
    }

    /// <summary>The visible series' value axis ids in the order JavaScript lists an object's keys: integer keys ascending, then the others in insertion order (the series reversed with <c>reverseStackOrder</c>).</summary>
    private static List<string> JsKeys(System.Collections.Immutable.ImmutableArray<SeriesSpec> series, bool vertical, bool reversed)
    {
        var ids = new List<string>(1);
        for (var i = 0; i < series.Length; i++)
        {
            var item = series[reversed ? series.Length - 1 - i : i];
            var id = vertical ? item.XAxisId : item.YAxisId;
            if (!item.Hide && !ids.Contains(id))
            {
                ids.Add(id);
            }
        }

        if (ids.Count > 1)
        {
            var integers = ids.FindAll(IsIndex);
            integers.Sort(static (a, b) => uint.Parse(a, System.Globalization.CultureInfo.InvariantCulture).CompareTo(uint.Parse(b, System.Globalization.CultureInfo.InvariantCulture)));
            integers.AddRange(ids.FindAll(static id => !IsIndex(id)));
            return integers;
        }

        return ids;
    }

    private static bool IsIndex(string id) =>
        uint.TryParse(id, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value) && value < uint.MaxValue && value.ToString(System.Globalization.CultureInfo.InvariantCulture) == id;

    /// <summary>The coordinate of an item along a category axis (its band's start), or of its number on a numeric one.</summary>
    public static double Category(ChartData data, AxisLayout axis, int index) =>
        axis.IsCategory ? axis.Category(index) : axis.Map(data.Numbers(axis.Spec.DataKey ?? string.Empty)[index]);

    /// <summary>Recharts' <c>getCateCoordinateOfLine</c>: a point sits at the tick of its own category when the axis drops duplicated categories, else at its index.</summary>
    private static int LineCategory(ChartData data, AxisLayout axis, int index)
    {
        if (!axis.IsCategory || axis.Spec.AllowDuplicatedCategory || axis.Spec.DataKey is not { } key || data.Texts(key)[index] is not { } value)
        {
            return index;
        }

        var tick = axis.CategoryIndex(value);
        return tick < 0 ? index : tick;
    }

    private static SeriesLayout Points(ChartLayout layout, SeriesSpec series, AxisLayout x, AxisLayout y, StackGroup? group)
    {
        var data = layout.SeriesData(series);
        var count = data.Count;
        var values = data.Numbers(series.DataKey);
        var stacked = group is not null && series.Kind == SeriesKind.Area;
        var lows = stacked ? [] : data.Lows(series.DataKey);
        var (category, value) = layout.Vertical ? (y, x) : (x, y);
        var along = new double[count];
        var across = new double[count];
        var bases = new double[count];
        var defined = new bool[count];
        var baseValue = series.Kind == SeriesKind.Area ? AreaBase(layout, series, value) : BaseValue(value);
        var baseline = value.Map(baseValue);
        for (var i = 0; i < count; i++)
        {
            along[i] = Category(data, category, LineCategory(data, category, i)) + (category.IsCategory ? category.BandSize / 2 : 0);
            (across[i], bases[i]) = stacked ? Stacked(group!.Band(series, i), values[i], value) : (value.Map(values[i]), baseline);

            if (lows.Length > 0)
            {
                bases[i] = RangeBase(value, lows[i], baseValue, layout.Vertical || !double.IsNaN(across[i]));
            }

            defined[i] = !double.IsNaN(across[i]) && !double.IsNaN(along[i]);
        }

        var (xs, ys) = layout.Vertical ? (across, along) : (along, across);
        return new SeriesLayout { Spec = series, Theme = layout.Spec.Colors, XAxis = x, YAxis = y, ValueAxis = value, Data = data, Values = values, X = xs, Y = ys, Base = bases, Defined = defined, Lows = lows, StackTops = stacked ? Tops(group!, series, count) : [] };
    }

    /// <summary>A range area's baseline at a point: its value's lower end, or the base value for a single number; none where a horizontal area's point is missing.</summary>
    private static double RangeBase(AxisLayout axis, double low, double baseValue, bool shown) =>
        shown ? axis.Map(double.IsNaN(low) ? baseValue : low) : double.NaN;

    /// <summary>Recharts' <c>Area.getBaseValue</c>: the area's own <c>baseValue</c> or the chart's, a number or an end of the value axis' domain.</summary>
    private static double AreaBase(ChartLayout layout, SeriesSpec series, AxisLayout axis)
    {
        var text = series.BaseValue ?? layout.Spec.BaseValue;
        if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number))
        {
            return number;
        }

        if (axis.IsCategory)
        {
            return BaseValue(axis);
        }

        return text switch
        {
            "dataMin" => Math.Min(axis.DomainMin, axis.DomainMax),
            "dataMax" => Math.Max(axis.DomainMin, axis.DomainMax),
            _ => BaseValue(axis),
        };
    }

    /// <summary>The tops of a stacked area's bands, which its label lists show; none without such a label list.</summary>
    private static double[] Tops(StackGroup group, SeriesSpec series, int count)
    {
        if (!LabelsValues(series))
        {
            return [];
        }

        var tops = new double[count];
        for (var i = 0; i < count; i++)
        {
            tops[i] = group.Band(series, i).Upper;
        }

        return tops;
    }

    /// <summary>Whether a stacked bar needs its stack's tops: its error bars stand on them and its label lists show them.</summary>
    private static bool NeedsTops(SeriesSpec series) => !series.ErrorBars.IsEmpty || LabelsValues(series);

    /// <summary>Whether a label list shows the series' own values (no data key of its own): stacked, Recharts' <c>LabelList</c> shows the stack's top, <c>last(value)</c>.</summary>
    private static bool LabelsValues(SeriesSpec series) =>
        series.Labels is { DataKey: null } || series.LabelLists.Any(label => label.DataKey is null);

    private static (double Top, double Base) Stacked(Graphs.Shapes.StackPoint band, double value, AxisLayout axis) =>
        (double.IsNaN(value) ? double.NaN : axis.Map(band.Upper), axis.Map(band.Lower));

    private static SeriesLayout Bars(ChartLayout layout, SeriesSpec series, AxisLayout x, AxisLayout y, StackGroup? group, BarSlot slot)
    {
        var data = layout.SeriesData(series);
        var count = data.Count;
        var values = data.Numbers(series.DataKey);
        var lows = group is not null ? [] : data.Lows(series.DataKey);
        var (category, value) = layout.Vertical ? (y, x) : (x, y);
        var bars = new BarRect[count];
        var tops = group is not null && NeedsTops(series) ? new double[count] : [];
        var baseValue = BarBase(value);
        for (var i = 0; i < count; i++)
        {
            var (lower, upper) = group is not null ? Truncate(group.Band(series, i), value) : (lows.Length > 0 && !double.IsNaN(lows[i]) ? lows[i] : baseValue, values[i]);
            if (tops.Length > 0)
            {
                tops[i] = upper;
            }

            var position = Category(data, category, i) + slot.Offset;
            bars[i] = layout.Vertical
                ? Horizontal(value.Map(lower), value.Map(upper), position, slot.Size, series.MinPointSize, values[i])
                : Upright(value.Map(lower), value.Map(upper), position, slot.Size, series.MinPointSize, values[i]);
        }

        return new SeriesLayout { Spec = series, Theme = layout.Spec.Colors, XAxis = x, YAxis = y, ValueAxis = value, Data = data, Values = values, Bars = bars, Lows = lows, StackTops = tops };
    }

    private static BarRect Upright(double baseCoordinate, double valueCoordinate, double x, double width, double minPointSize, double value)
    {
        var top = double.IsNaN(valueCoordinate) ? baseCoordinate : valueCoordinate;
        var height = baseCoordinate - valueCoordinate;
        height = double.IsNaN(height) ? 0 : height;
        if (Math.Abs(minPointSize) > 0 && Math.Abs(height) < Math.Abs(minPointSize))
        {
            var delta = Math.Sign(height == 0 ? minPointSize : height) * (Math.Abs(minPointSize) - Math.Abs(height));
            top -= delta;
            height += delta;
        }

        return new BarRect(x, top, width, height, value);
    }

    private static BarRect Horizontal(double baseCoordinate, double valueCoordinate, double y, double height, double minPointSize, double value)
    {
        var width = valueCoordinate - baseCoordinate;
        width = double.IsNaN(width) ? 0 : width;
        if (Math.Abs(minPointSize) > 0 && Math.Abs(width) < Math.Abs(minPointSize))
        {
            width += Math.Sign(width == 0 ? minPointSize : width) * (Math.Abs(minPointSize) - Math.Abs(width));
        }

        return new BarRect(baseCoordinate, y, width, height, value);
    }

    private static (double Lower, double Upper) Truncate(Graphs.Shapes.StackPoint band, AxisLayout axis)
    {
        var min = Math.Min(axis.DomainMin, axis.DomainMax);
        var max = Math.Max(axis.DomainMin, axis.DomainMax);
        var lower = double.IsNaN(band.Lower) || band.Lower < min ? min : band.Lower;
        var upper = double.IsNaN(band.Upper) || band.Upper > max ? max : band.Upper;
        return (Math.Min(lower, max), Math.Max(upper, min));
    }

    private static double BarBase(AxisLayout axis)
    {
        var min = Math.Min(axis.DomainMin, axis.DomainMax);
        var max = Math.Max(axis.DomainMin, axis.DomainMax);
        if (min <= 0 && max >= 0)
        {
            return 0;
        }

        return max < 0 ? max : min;
    }

    private static double BaseValue(AxisLayout axis)
    {
        var max = Math.Max(axis.DomainMin, axis.DomainMax);
        return max < 0 ? max : Math.Max(Math.Min(axis.DomainMin, axis.DomainMax), 0);
    }
}
