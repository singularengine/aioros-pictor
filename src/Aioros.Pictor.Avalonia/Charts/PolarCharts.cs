using Aioros.Pictor.Specs;
using Avalonia;

namespace Aioros.Pictor;

/// <summary>
/// What Recharts' polar charts share (<c>generateCategoricalChart</c> with angle and radius axes): the center, the radii
/// and the angle range of the polar coordinate system, on top of what every chart has.
/// </summary>
public abstract class PolarChart : Chart
{
    /// <summary>Defines <see cref="Cx"/>.</summary>
    public static readonly StyledProperty<string> CxProperty = AvaloniaProperty.Register<PolarChart, string>(nameof(Cx), "50%");

    /// <summary>Defines <see cref="Cy"/>.</summary>
    public static readonly StyledProperty<string> CyProperty = AvaloniaProperty.Register<PolarChart, string>(nameof(Cy), "50%");

    /// <summary>Defines <see cref="InnerRadius"/>.</summary>
    public static readonly StyledProperty<string> InnerRadiusProperty = AvaloniaProperty.Register<PolarChart, string>(nameof(InnerRadius), "0");

    /// <summary>Defines <see cref="OuterRadius"/>.</summary>
    public static readonly StyledProperty<string> OuterRadiusProperty = AvaloniaProperty.Register<PolarChart, string>(nameof(OuterRadius), "80%");

    /// <summary>Defines <see cref="StartAngle"/>.</summary>
    public static readonly StyledProperty<double?> StartAngleProperty = AvaloniaProperty.Register<PolarChart, double?>(nameof(StartAngle));

    /// <summary>Defines <see cref="EndAngle"/>.</summary>
    public static readonly StyledProperty<double?> EndAngleProperty = AvaloniaProperty.Register<PolarChart, double?>(nameof(EndAngle));

    private protected PolarChart()
    {
    }

    /// <summary>The center's x: pixels or a percentage of the chart's width.</summary>
    public string Cx { get => GetValue(CxProperty); set => SetValue(CxProperty, value); }

    /// <summary>The center's y: pixels or a percentage of the chart's height.</summary>
    public string Cy { get => GetValue(CyProperty); set => SetValue(CyProperty, value); }

    /// <summary>The inner radius: pixels or a percentage of the largest radius the plot allows.</summary>
    public string InnerRadius { get => GetValue(InnerRadiusProperty); set => SetValue(InnerRadiusProperty, value); }

    /// <summary>The outer radius: pixels or a percentage of the largest radius the plot allows.</summary>
    public string OuterRadius { get => GetValue(OuterRadiusProperty); set => SetValue(OuterRadiusProperty, value); }

    /// <summary>The angle the angle axis starts at, counterclockwise from 3 o'clock; Recharts' default for the chart when unset.</summary>
    public double? StartAngle { get => GetValue(StartAngleProperty); set => SetValue(StartAngleProperty, value); }

    /// <summary>The angle the angle axis ends at; Recharts' default for the chart when unset.</summary>
    public double? EndAngle { get => GetValue(EndAngleProperty); set => SetValue(EndAngleProperty, value); }

    private protected virtual (double Start, double End) DefaultAngles => (0, 360);

    private protected override ChartSpec Complete(ChartSpec spec, SpecBuilder builder) => spec with
    {
        Polar = builder.Polar with
        {
            Cx = Cx,
            Cy = Cy,
            InnerRadius = InnerRadius,
            OuterRadius = OuterRadius,
            StartAngle = StartAngle ?? DefaultAngles.Start,
            EndAngle = EndAngle ?? DefaultAngles.End,
        },
    };
}

/// <summary>Recharts' <c>PieChart</c>: one or more pies, each from its own data or the chart's.</summary>
public sealed class PieChart : PolarChart
{
    private protected override ChartKind Kind => ChartKind.Pie;
}

/// <summary>Recharts' <c>RadarChart</c>: radars over an angle axis of categories, from 12 o'clock clockwise.</summary>
public sealed class RadarChart : PolarChart
{
    private protected override ChartKind Kind => ChartKind.Radar;

    private protected override (double Start, double End) DefaultAngles => (90, -270);
}

/// <summary>Recharts' <c>RadialBarChart</c>: bars along arcs, one band of the radius per category.</summary>
public sealed class RadialBarChart : PolarChart
{
    /// <summary>Defines <see cref="BarSize"/>.</summary>
    public static readonly StyledProperty<double?> BarSizeProperty = AvaloniaProperty.Register<RadialBarChart, double?>(nameof(BarSize));

    /// <summary>Defines <see cref="BarGap"/>.</summary>
    public static readonly StyledProperty<string> BarGapProperty = AvaloniaProperty.Register<RadialBarChart, string>(nameof(BarGap), "4");

    /// <summary>Defines <see cref="BarCategoryGap"/>.</summary>
    public static readonly StyledProperty<string> BarCategoryGapProperty = AvaloniaProperty.Register<RadialBarChart, string>(nameof(BarCategoryGap), "10%");

    /// <summary>Defines <see cref="MaxBarSize"/>.</summary>
    public static readonly StyledProperty<double?> MaxBarSizeProperty = AvaloniaProperty.Register<RadialBarChart, double?>(nameof(MaxBarSize));

    /// <summary>Defines <see cref="Layout"/>.</summary>
    public static readonly StyledProperty<string> LayoutProperty = AvaloniaProperty.Register<RadialBarChart, string>(nameof(Layout), "radial");

    /// <summary>Defines <see cref="StackOffset"/>.</summary>
    public static readonly StyledProperty<StackOffsetKind> StackOffsetProperty = AvaloniaProperty.Register<RadialBarChart, StackOffsetKind>(nameof(StackOffset));

    /// <summary><c>radial</c> (the default: categories along the radius, values around) or <c>centric</c> (categories around the angle axis, values along the radius), Recharts' <c>layout</c>.</summary>
    public string Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>How stacked radial bars are offset (Recharts' <c>stackOffset</c>): none, expand, silhouette, wiggle, sign or positive.</summary>
    public StackOffsetKind StackOffset { get => GetValue(StackOffsetProperty); set => SetValue(StackOffsetProperty, value); }

    /// <summary>The largest width of a bar across the radius (Recharts' <c>maxBarSize</c>).</summary>
    public double? MaxBarSize { get => GetValue(MaxBarSizeProperty); set => SetValue(MaxBarSizeProperty, value); }

    /// <summary>The width of every bar across the radius.</summary>
    public double? BarSize { get => GetValue(BarSizeProperty); set => SetValue(BarSizeProperty, value); }

    /// <summary>The gap between bars of one category.</summary>
    public string BarGap { get => GetValue(BarGapProperty); set => SetValue(BarGapProperty, value); }

    /// <summary>The gap between categories: pixels or a percentage of the band.</summary>
    public string BarCategoryGap { get => GetValue(BarCategoryGapProperty); set => SetValue(BarCategoryGapProperty, value); }

    private protected override ChartKind Kind => ChartKind.RadialBar;

    private protected override ChartSpec Complete(ChartSpec spec, SpecBuilder builder) =>
        base.Complete(spec, builder) is var complete
            ? complete with
            {
                BarSize = BarSize?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                MaxBarSize = MaxBarSize,
                BarGap = BarGap,
                BarCategoryGap = BarCategoryGap,
                StackOffset = StackOffset,
                Polar = complete.Polar! with { Centric = string.Equals(Layout, "centric", StringComparison.OrdinalIgnoreCase) },
            }
            : complete;
}
