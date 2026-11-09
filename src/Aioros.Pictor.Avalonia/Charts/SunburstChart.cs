using Aioros.Pictor.Specs;
using Avalonia;

namespace Aioros.Pictor;

/// <summary>
/// Recharts' <c>SunburstChart</c>: its data is the root node, each ring a level of its children (member <c>children</c>,
/// or <c>Children</c> on a C# type), each sector a node whose arc is its value (<see cref="DataKey"/>) over the root's;
/// a node's own fill is read from the member <see cref="FillKey"/> names.
/// </summary>
public sealed class SunburstChart : Chart
{
    /// <summary>Defines <see cref="DataKey"/>.</summary>
    public static readonly StyledProperty<string> DataKeyProperty = AvaloniaProperty.Register<SunburstChart, string>(nameof(DataKey), "value");

    /// <summary>Defines <see cref="NameKey"/>.</summary>
    public static readonly StyledProperty<string> NameKeyProperty = AvaloniaProperty.Register<SunburstChart, string>(nameof(NameKey), "name");

    /// <summary>Defines <see cref="FillKey"/>.</summary>
    public static readonly StyledProperty<string?> FillKeyProperty = AvaloniaProperty.Register<SunburstChart, string?>(nameof(FillKey));

    /// <summary>Defines <see cref="SectorPadding"/>.</summary>
    public static readonly StyledProperty<double> SectorPaddingProperty = AvaloniaProperty.Register<SunburstChart, double>(nameof(SectorPadding), 2);

    /// <summary>Defines <see cref="RingPadding"/>.</summary>
    public static readonly StyledProperty<double> RingPaddingProperty = AvaloniaProperty.Register<SunburstChart, double>(nameof(RingPadding), 2);

    /// <summary>Defines <see cref="InnerRadius"/>.</summary>
    public static readonly StyledProperty<double> InnerRadiusProperty = AvaloniaProperty.Register<SunburstChart, double>(nameof(InnerRadius), 50);

    /// <summary>Defines <see cref="OuterRadius"/>.</summary>
    public static readonly StyledProperty<double?> OuterRadiusProperty = AvaloniaProperty.Register<SunburstChart, double?>(nameof(OuterRadius));

    /// <summary>Defines <see cref="Cx"/>.</summary>
    public static readonly StyledProperty<double?> CxProperty = AvaloniaProperty.Register<SunburstChart, double?>(nameof(Cx));

    /// <summary>Defines <see cref="Cy"/>.</summary>
    public static readonly StyledProperty<double?> CyProperty = AvaloniaProperty.Register<SunburstChart, double?>(nameof(Cy));

    /// <summary>Defines <see cref="StartAngle"/>.</summary>
    public static readonly StyledProperty<double> StartAngleProperty = AvaloniaProperty.Register<SunburstChart, double>(nameof(StartAngle));

    /// <summary>Defines <see cref="EndAngle"/>.</summary>
    public static readonly StyledProperty<double> EndAngleProperty = AvaloniaProperty.Register<SunburstChart, double>(nameof(EndAngle), 360);

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<SunburstChart, string?>(nameof(Fill));

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<SunburstChart, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="TextFontFamily"/>.</summary>
    public static readonly StyledProperty<string?> TextFontFamilyProperty = AvaloniaProperty.Register<SunburstChart, string?>(nameof(TextFontFamily));

    /// <summary>Defines <see cref="TextFontWeight"/>.</summary>
    public static readonly StyledProperty<string?> TextFontWeightProperty = AvaloniaProperty.Register<SunburstChart, string?>(nameof(TextFontWeight), TextOptionsSpec.Default.FontWeight);

    /// <summary>Defines <see cref="TextPaintOrder"/>.</summary>
    public static readonly StyledProperty<string?> TextPaintOrderProperty = AvaloniaProperty.Register<SunburstChart, string?>(nameof(TextPaintOrder), TextOptionsSpec.Default.PaintOrder);

    /// <summary>Defines <see cref="TextFontSize"/>.</summary>
    public static readonly StyledProperty<string?> TextFontSizeProperty = AvaloniaProperty.Register<SunburstChart, string?>(nameof(TextFontSize), TextOptionsSpec.Default.FontSize);

    /// <summary>Defines <see cref="TextStroke"/>.</summary>
    public static readonly StyledProperty<string?> TextStrokeProperty = AvaloniaProperty.Register<SunburstChart, string?>(nameof(TextStroke), TextOptionsSpec.Default.Stroke);

    /// <summary>Defines <see cref="TextFill"/>.</summary>
    public static readonly StyledProperty<string?> TextFillProperty = AvaloniaProperty.Register<SunburstChart, string?>(nameof(TextFill), TextOptionsSpec.Default.Fill);

    /// <summary>Defines <see cref="TextPointerEvents"/>.</summary>
    public static readonly StyledProperty<string?> TextPointerEventsProperty = AvaloniaProperty.Register<SunburstChart, string?>(nameof(TextPointerEvents), TextOptionsSpec.Default.PointerEvents);

    /// <summary>The data key of the nodes' values, which size their arcs and are written on them; <c>value</c> by default.</summary>
    public string DataKey { get => GetValue(DataKeyProperty); set => SetValue(DataKeyProperty, value); }

    /// <summary>The data key of the nodes' names; <c>name</c>, which Recharts reads, by default.</summary>
    public string NameKey { get => GetValue(NameKeyProperty); set => SetValue(NameKeyProperty, value); }

    /// <summary>The data key of a node's own fill, which its descendants inherit (Recharts reads <c>fill</c>, so <c>FillKey="fill"</c> or <c>"Fill"</c> reads as Recharts does); none by default, so a model without it is not probed for it.</summary>
    public string? FillKey { get => GetValue(FillKeyProperty); set => SetValue(FillKeyProperty, value); }

    /// <summary>The width of the stroke between the sectors (Recharts' <c>padding</c>, a name <see cref="Chart.Padding"/> takes), 2 by default.</summary>
    public double SectorPadding { get => GetValue(SectorPaddingProperty); set => SetValue(SectorPaddingProperty, value); }

    /// <summary>The gap between a ring and the next, 2 by default.</summary>
    public double RingPadding { get => GetValue(RingPaddingProperty); set => SetValue(RingPaddingProperty, value); }

    /// <summary>The radius of the hole in the middle, 50 by default.</summary>
    public double InnerRadius { get => GetValue(InnerRadiusProperty); set => SetValue(InnerRadiusProperty, value); }

    /// <summary>The radius the rings share out; half the chart's smaller side when null.</summary>
    public double? OuterRadius { get => GetValue(OuterRadiusProperty); set => SetValue(OuterRadiusProperty, value); }

    /// <summary>The center's abscissa; the middle of the chart when null.</summary>
    public double? Cx { get => GetValue(CxProperty); set => SetValue(CxProperty, value); }

    /// <summary>The center's ordinate; the middle of the chart when null.</summary>
    public double? Cy { get => GetValue(CyProperty); set => SetValue(CyProperty, value); }

    /// <summary>The angle the first sector starts at, in degrees, 0 by default.</summary>
    public double StartAngle { get => GetValue(StartAngleProperty); set => SetValue(StartAngleProperty, value); }

    /// <summary>The angle the root's value spans, in degrees from 0 as Recharts scales it (not from <see cref="StartAngle"/>), 360 by default.</summary>
    public double EndAngle { get => GetValue(EndAngleProperty); set => SetValue(EndAngleProperty, value); }

    /// <summary>The fill of the nodes without a fill of their own or inherited, <c>#333</c> by default.</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The sectors' stroke, <c>#FFF</c> by default.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The values' font family (Recharts' <c>textOptions.fontFamily</c>); the chart's when null.</summary>
    public string? TextFontFamily { get => GetValue(TextFontFamilyProperty); set => SetValue(TextFontFamilyProperty, value); }

    /// <summary>The values' font weight, <c>bold</c> by default.</summary>
    public string? TextFontWeight { get => GetValue(TextFontWeightProperty); set => SetValue(TextFontWeightProperty, value); }

    /// <summary>The values' paint order, <c>stroke fill</c> (the outline under the fill) by default.</summary>
    public string? TextPaintOrder { get => GetValue(TextPaintOrderProperty); set => SetValue(TextPaintOrderProperty, value); }

    /// <summary>The values' font size as CSS writes it, <c>.75rem</c> by default.</summary>
    public string? TextFontSize { get => GetValue(TextFontSizeProperty); set => SetValue(TextFontSizeProperty, value); }

    /// <summary>The values' outline, <c>#FFF</c> by default; null for none.</summary>
    public string? TextStroke { get => GetValue(TextStrokeProperty); set => SetValue(TextStrokeProperty, value); }

    /// <summary>The values' color, <c>black</c> by default; Recharts' <c>#808080</c> when null.</summary>
    public string? TextFill { get => GetValue(TextFillProperty); set => SetValue(TextFillProperty, value); }

    /// <summary>The values' pointer events on the web, <c>none</c> by default.</summary>
    public string? TextPointerEvents { get => GetValue(TextPointerEventsProperty); set => SetValue(TextPointerEventsProperty, value); }

    private protected override ChartKind Kind => ChartKind.Sunburst;

    private protected override ChartSpec Complete(ChartSpec spec, SpecBuilder builder) => spec with
    {
        Sunburst = new SunburstSpec
        {
            DataKey = DataKey,
            NameKey = NameKey,
            FillKey = FillKey,
            Padding = SectorPadding,
            RingPadding = RingPadding,
            InnerRadius = InnerRadius,
            OuterRadius = OuterRadius,
            Cx = Cx,
            Cy = Cy,
            StartAngle = StartAngle,
            EndAngle = EndAngle,
            Fill = Fill,
            Stroke = Stroke,
            TextOptions = new TextOptionsSpec
            {
                FontFamily = TextFontFamily,
                FontWeight = TextFontWeight,
                PaintOrder = TextPaintOrder,
                FontSize = TextFontSize,
                Stroke = TextStroke,
                Fill = TextFill,
                PointerEvents = TextPointerEvents,
            },
        },
    };
}
