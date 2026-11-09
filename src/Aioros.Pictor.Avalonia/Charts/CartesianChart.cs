using Aioros.Pictor.Specs;
using Avalonia;

namespace Aioros.Pictor;

/// <summary>
/// What Recharts' cartesian charts share (<c>generateCategoricalChart</c> with x and y axes): the bar gaps and sizes and
/// the stack offset, on top of what every chart has.
/// </summary>
public abstract class CartesianChart : Chart
{
    /// <summary>Defines <see cref="BarCategoryGap"/>.</summary>
    public static readonly StyledProperty<string> BarCategoryGapProperty = AvaloniaProperty.Register<CartesianChart, string>(nameof(BarCategoryGap), "10%");

    /// <summary>Defines <see cref="BarGap"/>.</summary>
    public static readonly StyledProperty<string> BarGapProperty = AvaloniaProperty.Register<CartesianChart, string>(nameof(BarGap), "4");

    /// <summary>Defines <see cref="Compact"/>.</summary>
    public static readonly StyledProperty<bool> CompactProperty = AvaloniaProperty.Register<CartesianChart, bool>(nameof(Compact));

    /// <summary>Defines <see cref="ReverseStackOrder"/>.</summary>
    public static readonly StyledProperty<bool> ReverseStackOrderProperty = AvaloniaProperty.Register<CartesianChart, bool>(nameof(ReverseStackOrder));

    /// <summary>Defines <see cref="BarSize"/>.</summary>
    public static readonly StyledProperty<string?> BarSizeProperty = AvaloniaProperty.Register<CartesianChart, string?>(nameof(BarSize));

    /// <summary>Defines <see cref="MaxBarSize"/>.</summary>
    public static readonly StyledProperty<double?> MaxBarSizeProperty = AvaloniaProperty.Register<CartesianChart, double?>(nameof(MaxBarSize));

    /// <summary>Defines <see cref="StackOffset"/>.</summary>
    public static readonly StyledProperty<StackOffsetKind> StackOffsetProperty = AvaloniaProperty.Register<CartesianChart, StackOffsetKind>(nameof(StackOffset));

    /// <summary>Defines <see cref="BaseValue"/>.</summary>
    public static readonly StyledProperty<string?> BaseValueProperty = AvaloniaProperty.Register<CartesianChart, string?>(nameof(BaseValue));

    /// <summary>Defines <see cref="Layout"/>.</summary>
    public static readonly StyledProperty<ChartLayoutKind> LayoutProperty = AvaloniaProperty.Register<CartesianChart, ChartLayoutKind>(nameof(Layout));

    private protected CartesianChart()
    {
    }

    /// <summary>The gap between categories of bars: pixels or a percentage of the band.</summary>
    public string BarCategoryGap { get => GetValue(BarCategoryGapProperty); set => SetValue(BarCategoryGapProperty, value); }

    /// <summary>The gap between bars of one category.</summary>
    public string BarGap { get => GetValue(BarGapProperty); set => SetValue(BarGapProperty, value); }

    /// <summary>The width of every bar, when fixed: a number, or a percentage of the category axis' length (<c>20%</c>).</summary>
    public string? BarSize { get => GetValue(BarSizeProperty); set => SetValue(BarSizeProperty, value); }

    /// <summary>The largest width of a bar.</summary>
    public double? MaxBarSize { get => GetValue(MaxBarSizeProperty); set => SetValue(MaxBarSizeProperty, value); }

    /// <summary>Whether stacks, bar groups, the legend and the tooltip take the series in reverse order (Recharts' <c>reverseStackOrder</c>).</summary>
    public bool ReverseStackOrder { get => GetValue(ReverseStackOrderProperty); set => SetValue(ReverseStackOrderProperty, value); }

    /// <summary>Whether the chart is its surface alone, without the web wrapper, the legend or the tooltip (Recharts' <c>compact</c>).</summary>
    public bool Compact { get => GetValue(CompactProperty); set => SetValue(CompactProperty, value); }

    /// <summary>How stacked series are offset.</summary>
    public StackOffsetKind StackOffset { get => GetValue(StackOffsetProperty); set => SetValue(StackOffsetProperty, value); }

    /// <summary>The baseline of the chart's areas that set none (Recharts' chart-level <c>baseValue</c>): a number, <c>dataMin</c> or <c>dataMax</c>.</summary>
    public string? BaseValue { get => GetValue(BaseValueProperty); set => SetValue(BaseValueProperty, value); }

    /// <summary>Whether categories run along the x-axis (<c>Horizontal</c>, the default) or down the y-axis (<c>Vertical</c>).</summary>
    public ChartLayoutKind Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    private protected override ChartSpec Complete(ChartSpec spec, SpecBuilder builder) => spec with
    {
        BarCategoryGap = BarCategoryGap,
        BarGap = BarGap,
        BarSize = BarSize,
        MaxBarSize = MaxBarSize,
        StackOffset = StackOffset,
        BaseValue = BaseValue,
        ReverseStackOrder = ReverseStackOrder,
        Compact = Compact,
        Layout = Layout,
        ZAxes = [.. builder.ZAxes],
        ReferenceAreas = [.. builder.ReferenceAreas],
        ReferenceDots = [.. builder.ReferenceDots],
        Brush = builder.Brush,
        Series = OwnData(spec, builder),
    };

    /// <summary>The series with their own items read into tables of every key the chart reads.</summary>
    private static System.Collections.Immutable.ImmutableArray<SeriesSpec> OwnData(ChartSpec spec, SpecBuilder builder)
    {
        if (builder.SeriesData.Count == 0)
        {
            return spec.Series;
        }

        var keys = spec.XAxes.Select(axis => axis.DataKey)
            .Concat(spec.YAxes.Select(axis => axis.DataKey))
            .Concat(builder.ZAxes.Select(axis => axis.DataKey))
            .Concat(spec.Series.SelectMany(series => (IEnumerable<string?>)[series.DataKey, series.Labels?.DataKey, series.FillKey, series.OpacityKey, series.HrefKey, .. series.ErrorBars.Select(bar => bar.DataKey)]))
            .Concat(Content.ContentWalk.All(spec).SelectMany(content => content.Keys))
            .Where(key => !string.IsNullOrEmpty(key))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var series = spec.Series.ToBuilder();
        foreach (var (index, items) in builder.SeriesData)
        {
            series[index] = series[index] with { Data = DataTable.Read(items, keys, builder.Members) };
        }

        return series.ToImmutable();
    }
}

/// <summary>Recharts' <c>LineChart</c>.</summary>
public sealed class LineChart : CartesianChart
{
    private protected override ChartKind Kind => ChartKind.Line;
}

/// <summary>Recharts' <c>AreaChart</c>.</summary>
public sealed class AreaChart : CartesianChart
{
    private protected override ChartKind Kind => ChartKind.Area;
}

/// <summary>Recharts' <c>BarChart</c>.</summary>
public sealed class BarChart : CartesianChart
{
    private protected override ChartKind Kind => ChartKind.Bar;
}

/// <summary>Recharts' <c>ScatterChart</c>: points on two number axes, sized by a z-axis, with a tooltip per point.</summary>
public sealed class ScatterChart : CartesianChart
{
    private protected override ChartKind Kind => ChartKind.Scatter;
}

/// <summary>Recharts' <c>ComposedChart</c>: lines, areas, bars and scatters together.</summary>
public sealed class ComposedChart : CartesianChart
{
    private protected override ChartKind Kind => ChartKind.Composed;
}
