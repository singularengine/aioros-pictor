using System.Collections;
using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Aioros.Pictor;

/// <summary>What the children of a polar chart share: they raise <see cref="Changed"/> when a property changes.</summary>
public abstract class PolarElement : StyledElement, IChartElement, ISpecSource
{
    private protected PolarElement()
    {
    }

    private protected ChangeRelay Relay { get; } = new();

    /// <inheritdoc />
    public event EventHandler? Changed { add => Relay.Changed += value; remove => Relay.Changed -= value; }

    void ISpecSource.Apply(SpecBuilder builder, int order) => Apply(builder, order);

    private protected abstract void Apply(SpecBuilder builder, int order);

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        Relay.Raise(this);
    }
}

/// <summary>Recharts' <c>PolarGrid</c>: concentric polygons or circles at the radius axis' ticks and radial lines at the angle axis'.</summary>
public sealed class PolarGrid : PolarElement
{
    /// <summary>Defines <see cref="GridType"/>.</summary>
    public static readonly StyledProperty<string> GridTypeProperty = AvaloniaProperty.Register<PolarGrid, string>(nameof(GridType), "polygon");

    /// <summary>Defines <see cref="RadialLines"/>.</summary>
    public static readonly StyledProperty<bool> RadialLinesProperty = AvaloniaProperty.Register<PolarGrid, bool>(nameof(RadialLines), true);

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<PolarGrid, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="StrokeDashArray"/>.</summary>
    public static readonly StyledProperty<object?> StrokeDashArrayProperty = AvaloniaProperty.Register<PolarGrid, object?>(nameof(StrokeDashArray));

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> StrokeWidthProperty = AvaloniaProperty.Register<PolarGrid, double?>(nameof(StrokeWidth));

    /// <summary>Defines <see cref="StrokeOpacity"/>.</summary>
    public static readonly StyledProperty<double?> StrokeOpacityProperty = AvaloniaProperty.Register<PolarGrid, double?>(nameof(StrokeOpacity));

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<PolarGrid, string?>(nameof(Fill));

    /// <summary>Defines <see cref="PolarAngles"/>.</summary>
    public static readonly StyledProperty<string?> PolarAnglesProperty = AvaloniaProperty.Register<PolarGrid, string?>(nameof(PolarAngles));

    /// <summary>Defines <see cref="PolarRadius"/>.</summary>
    public static readonly StyledProperty<string?> PolarRadiusProperty = AvaloniaProperty.Register<PolarGrid, string?>(nameof(PolarRadius));

    /// <summary>The concentric lines: <c>polygon</c> or <c>circle</c>.</summary>
    public string GridType { get => GetValue(GridTypeProperty); set => SetValue(GridTypeProperty, value); }

    /// <summary>Whether the radial lines are drawn.</summary>
    public bool RadialLines { get => GetValue(RadialLinesProperty); set => SetValue(RadialLinesProperty, value); }

    /// <summary>The lines' color.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The lines' dash pattern.</summary>
    public object? StrokeDashArray { get => GetValue(StrokeDashArrayProperty); set => SetValue(StrokeDashArrayProperty, value); }

    /// <summary>The lines' width.</summary>
    public double? StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    /// <summary>The lines' opacity.</summary>
    public double? StrokeOpacity { get => GetValue(StrokeOpacityProperty); set => SetValue(StrokeOpacityProperty, value); }

    /// <summary>The fill Recharts spreads onto the radial lines (the concentric lines are never filled).</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The angles of the radial lines, instead of the angle axis' ticks (Recharts' <c>polarAngles</c>).</summary>
    public string? PolarAngles { get => GetValue(PolarAnglesProperty); set => SetValue(PolarAnglesProperty, value); }

    /// <summary>The radii of the concentric lines, instead of the radius axis' ticks (Recharts' <c>polarRadius</c>).</summary>
    public string? PolarRadius { get => GetValue(PolarRadiusProperty); set => SetValue(PolarRadiusProperty, value); }

    private protected override void Apply(SpecBuilder builder, int order) => builder.Polar = builder.Polar with
    {
        Grid = new PolarGridSpec
        {
            Circle = GridType == "circle",
            RadialLines = RadialLines,
            Stroke = Stroke,
            StrokeDashArray = SpecText.Dashes(StrokeDashArray),
            StrokeWidth = StrokeWidth,
            StrokeOpacity = StrokeOpacity,
            Fill = Fill,
            PolarAngles = SpecText.Numbers(PolarAngles),
            PolarRadius = SpecText.Numbers(PolarRadius),
            Order = order,
        },
    };
}

/// <summary>Recharts' <c>PolarAngleAxis</c>: the categories around the circle.</summary>
public sealed class PolarAngleAxis : PolarElement
{
    /// <summary>Defines <see cref="AngleAxisId"/>.</summary>
    public static readonly StyledProperty<string> AngleAxisIdProperty = AvaloniaProperty.Register<PolarAngleAxis, string>(nameof(AngleAxisId), "0");

    /// <summary>Defines <see cref="TickTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> TickTemplateProperty = AvaloniaProperty.Register<PolarAngleAxis, Avalonia.Controls.Templates.IDataTemplate?>(nameof(TickTemplate));

    /// <summary>
    /// The custom content drawn in place of each tick's label (Recharts' <c>tick</c> element): a template whose data
    /// context is the tick's <see cref="TickContext"/> (its point past the tick line, the chart's center and radius).
    /// </summary>
    public Avalonia.Controls.Templates.IDataTemplate? TickTemplate { get => GetValue(TickTemplateProperty); set => SetValue(TickTemplateProperty, value); }

    /// <summary>The axis' id, which radars and radial bars name (Recharts' <c>angleAxisId</c>), <c>0</c> by default.</summary>
    public string AngleAxisId { get => GetValue(AngleAxisIdProperty); set => SetValue(AngleAxisIdProperty, value); }

    /// <summary>Defines <see cref="DataKey"/>.</summary>
    public static readonly StyledProperty<string?> DataKeyProperty = AvaloniaProperty.Register<PolarAngleAxis, string?>(nameof(DataKey));

    /// <summary>Defines <see cref="AxisLineType"/>.</summary>
    public static readonly StyledProperty<string> AxisLineTypeProperty = AvaloniaProperty.Register<PolarAngleAxis, string>(nameof(AxisLineType), "polygon");

    /// <summary>Defines <see cref="AxisLine"/>.</summary>
    public static readonly StyledProperty<bool> AxisLineProperty = AvaloniaProperty.Register<PolarAngleAxis, bool>(nameof(AxisLine), true);

    /// <summary>Defines <see cref="TickLine"/>.</summary>
    public static readonly StyledProperty<bool> TickLineProperty = AvaloniaProperty.Register<PolarAngleAxis, bool>(nameof(TickLine), true);

    /// <summary>Defines <see cref="Tick"/>.</summary>
    public static readonly StyledProperty<bool> TickProperty = AvaloniaProperty.Register<PolarAngleAxis, bool>(nameof(Tick), true);

    /// <summary>Defines <see cref="TickSize"/>.</summary>
    public static readonly StyledProperty<double> TickSizeProperty = AvaloniaProperty.Register<PolarAngleAxis, double>(nameof(TickSize), 8);

    /// <summary>Defines <see cref="Orientation"/>.</summary>
    public static readonly StyledProperty<string> OrientationProperty = AvaloniaProperty.Register<PolarAngleAxis, string>(nameof(Orientation), "outer");

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<PolarAngleAxis, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="TickFormat"/>.</summary>
    public static readonly StyledProperty<string?> TickFormatProperty = AvaloniaProperty.Register<PolarAngleAxis, string?>(nameof(TickFormat));

    /// <summary>Defines <see cref="TickFormatter"/>.</summary>
    public static readonly StyledProperty<Avalonia.Data.Converters.IValueConverter?> TickFormatterProperty = AvaloniaProperty.Register<PolarAngleAxis, Avalonia.Data.Converters.IValueConverter?>(nameof(TickFormatter));

    private readonly Formatted<Func<object?, int, string?>> _tickFormatter = new(Formatters.Tick);

    /// <summary>Defines <see cref="Hide"/>.</summary>
    public static readonly StyledProperty<bool> HideProperty = AvaloniaProperty.Register<PolarAngleAxis, bool>(nameof(Hide));

    /// <summary>Defines <see cref="Type"/>.</summary>
    public static readonly StyledProperty<AxisType?> TypeProperty = AvaloniaProperty.Register<PolarAngleAxis, AxisType?>(nameof(Type));

    /// <summary>Defines <see cref="Domain"/>.</summary>
    public static readonly StyledProperty<string?> DomainProperty = AvaloniaProperty.Register<PolarAngleAxis, string?>(nameof(Domain));

    /// <summary>Defines <see cref="TickCount"/>.</summary>
    public static readonly StyledProperty<int?> TickCountProperty = AvaloniaProperty.Register<PolarAngleAxis, int?>(nameof(TickCount));

    /// <summary>Defines <see cref="AllowDecimals"/>.</summary>
    public static readonly StyledProperty<bool> AllowDecimalsProperty = AvaloniaProperty.Register<PolarAngleAxis, bool>(nameof(AllowDecimals), true);

    /// <summary>Defines <see cref="AllowDataOverflow"/>.</summary>
    public static readonly StyledProperty<bool> AllowDataOverflowProperty = AvaloniaProperty.Register<PolarAngleAxis, bool>(nameof(AllowDataOverflow));

    /// <summary>Defines <see cref="TickLineStroke"/>.</summary>
    public static readonly StyledProperty<string?> TickLineStrokeProperty = AvaloniaProperty.Register<PolarAngleAxis, string?>(nameof(TickLineStroke));

    /// <summary>Defines <see cref="TickLineStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> TickLineStrokeWidthProperty = AvaloniaProperty.Register<PolarAngleAxis, double?>(nameof(TickLineStrokeWidth));

    /// <summary>Defines <see cref="Scale"/>.</summary>
    public static readonly StyledProperty<string?> ScaleProperty = AvaloniaProperty.Register<PolarAngleAxis, string?>(nameof(Scale));

    /// <summary>Defines <see cref="Reversed"/>.</summary>
    public static readonly StyledProperty<bool> ReversedProperty = AvaloniaProperty.Register<PolarAngleAxis, bool>(nameof(Reversed));

    /// <summary>Defines <see cref="Ticks"/>.</summary>
    public static readonly StyledProperty<string?> TicksProperty = AvaloniaProperty.Register<PolarAngleAxis, string?>(nameof(Ticks));

    /// <summary>Defines <see cref="AllowDuplicatedCategory"/>.</summary>
    public static readonly StyledProperty<bool> AllowDuplicatedCategoryProperty = AvaloniaProperty.Register<PolarAngleAxis, bool>(nameof(AllowDuplicatedCategory), true);

    /// <summary>Defines <see cref="TickFill"/>.</summary>
    public static readonly StyledProperty<string?> TickFillProperty = AvaloniaProperty.Register<PolarAngleAxis, string?>(nameof(TickFill));

    /// <summary>Defines <see cref="TickFontSize"/>.</summary>
    public static readonly StyledProperty<double?> TickFontSizeProperty = AvaloniaProperty.Register<PolarAngleAxis, double?>(nameof(TickFontSize));

    /// <summary>Defines <see cref="AxisLineStroke"/>.</summary>
    public static readonly StyledProperty<string?> AxisLineStrokeProperty = AvaloniaProperty.Register<PolarAngleAxis, string?>(nameof(AxisLineStroke));

    /// <summary>Defines <see cref="AxisLineStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> AxisLineStrokeWidthProperty = AvaloniaProperty.Register<PolarAngleAxis, double?>(nameof(AxisLineStrokeWidth));

    /// <summary>The data key of the categories.</summary>
    public string? DataKey { get => GetValue(DataKeyProperty); set => SetValue(DataKeyProperty, value); }

    /// <summary>The axis line: <c>polygon</c> through the ticks or <c>circle</c>.</summary>
    public string AxisLineType { get => GetValue(AxisLineTypeProperty); set => SetValue(AxisLineTypeProperty, value); }

    /// <summary>Whether the axis line is drawn.</summary>
    public bool AxisLine { get => GetValue(AxisLineProperty); set => SetValue(AxisLineProperty, value); }

    /// <summary>Whether the tick lines are drawn.</summary>
    public bool TickLine { get => GetValue(TickLineProperty); set => SetValue(TickLineProperty, value); }

    /// <summary>Whether the tick labels are drawn.</summary>
    public bool Tick { get => GetValue(TickProperty); set => SetValue(TickProperty, value); }

    /// <summary>The length of a tick line.</summary>
    public double TickSize { get => GetValue(TickSizeProperty); set => SetValue(TickSizeProperty, value); }

    /// <summary>Where the ticks point: <c>outer</c> or <c>inner</c>.</summary>
    public string Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>The axis' color; none by default, as in Recharts.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>A .NET composite format for the tick labels.</summary>
    public string? TickFormat { get => GetValue(TickFormatProperty); set => SetValue(TickFormatProperty, value); }

    /// <summary>Recharts' <c>tickFormatter</c>: a converter of a tick's value to its text, its index as the parameter, over <see cref="TickFormat"/>.</summary>
    public Avalonia.Data.Converters.IValueConverter? TickFormatter { get => GetValue(TickFormatterProperty); set => SetValue(TickFormatterProperty, value); }

    /// <summary>Whether the axis is hidden.</summary>
    public bool Hide { get => GetValue(HideProperty); set => SetValue(HideProperty, value); }

    /// <summary>Numbers (a radial bar chart's values, with a domain and nice ticks) or categories; Recharts' default by chart when unset.</summary>
    public AxisType? Type { get => GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>The domain of a numeric axis as two bounds, as a cartesian axis'.</summary>
    public string? Domain { get => GetValue(DomainProperty); set => SetValue(DomainProperty, value); }

    /// <summary>How many ticks a numeric axis aims for (Recharts' nice ticks); unset, d3's ten ticks over the domain, as Recharts' angle axis has no default.</summary>
    public int? TickCount { get => GetValue(TickCountProperty); set => SetValue(TickCountProperty, value); }

    /// <summary>Whether a numeric axis' ticks may be fractional.</summary>
    public bool AllowDecimals { get => GetValue(AllowDecimalsProperty); set => SetValue(AllowDecimalsProperty, value); }

    /// <summary>Whether a specified domain may cut the data.</summary>
    public bool AllowDataOverflow { get => GetValue(AllowDataOverflowProperty); set => SetValue(AllowDataOverflowProperty, value); }

    /// <summary>The tick lines' color (the <c>tickLine</c> object's <c>stroke</c>).</summary>
    public string? TickLineStroke { get => GetValue(TickLineStrokeProperty); set => SetValue(TickLineStrokeProperty, value); }

    /// <summary>The tick lines' width (the <c>tickLine</c> object's <c>strokeWidth</c>).</summary>
    public double? TickLineStrokeWidth { get => GetValue(TickLineStrokeWidthProperty); set => SetValue(TickLineStrokeWidthProperty, value); }

    /// <summary>The scale of a numeric axis: <c>auto</c>, <c>linear</c>, <c>log</c>, <c>pow</c> or <c>sqrt</c>.</summary>
    public string? Scale { get => GetValue(ScaleProperty); set => SetValue(ScaleProperty, value); }

    /// <summary>Whether the axis' range runs backwards.</summary>
    public bool Reversed { get => GetValue(ReversedProperty); set => SetValue(ReversedProperty, value); }

    /// <summary>The ticks to show instead of the computed ones: numbers or categories separated by commas.</summary>
    public string? Ticks { get => GetValue(TicksProperty); set => SetValue(TicksProperty, value); }

    /// <summary>Whether a category that appears twice gets two ticks (the default) or one.</summary>
    public bool AllowDuplicatedCategory { get => GetValue(AllowDuplicatedCategoryProperty); set => SetValue(AllowDuplicatedCategoryProperty, value); }

    /// <summary>The color of the tick labels (the <c>tick</c> object's <c>fill</c>).</summary>
    public string? TickFill { get => GetValue(TickFillProperty); set => SetValue(TickFillProperty, value); }

    /// <summary>The font size of the tick labels (the <c>tick</c> object's <c>fontSize</c>).</summary>
    public double? TickFontSize { get => GetValue(TickFontSizeProperty); set => SetValue(TickFontSizeProperty, value); }

    /// <summary>The axis line's color (the <c>axisLine</c> object's <c>stroke</c>).</summary>
    public string? AxisLineStroke { get => GetValue(AxisLineStrokeProperty); set => SetValue(AxisLineStrokeProperty, value); }

    /// <summary>The axis line's width (the <c>axisLine</c> object's <c>strokeWidth</c>).</summary>
    public double? AxisLineStrokeWidth { get => GetValue(AxisLineStrokeWidthProperty); set => SetValue(AxisLineStrokeWidthProperty, value); }

    private protected override void Apply(SpecBuilder builder, int order) => builder.Polar = builder.Polar.AngleAxis is null
        ? builder.Polar with { AngleAxis = Spec(order, builder) }
        : builder.Polar with { MoreAngleAxes = builder.Polar.MoreAngleAxes.Add(Spec(order, builder)) };

    private PolarAngleAxisSpec Spec(int order, SpecBuilder builder) => new()
    {
        TickContent = builder.Content(TickTemplate, Content.ContentSlots.Tick, this, nameof(TickTemplate)),
        Id = AngleAxisId,
        DataKey = DataKey,
        CircleLine = AxisLineType == "circle",
        AxisLine = AxisLine,
        TickLine = TickLine,
        Tick = Tick,
        TickSize = TickSize,
        Inner = Orientation == "inner",
        Stroke = Stroke,
        TickFormat = TickFormat,
        TickFormatter = _tickFormatter.Of(TickFormatter),
        TickConverter = Formatters.Reference(TickFormatter),
        Hide = Hide,
        Order = order,
        Type = Type,
        DomainMin = SpecText.Domain(Domain).Min,
        DomainMax = SpecText.Domain(Domain).Max,
        TickCount = TickCount,
        AllowDecimals = AllowDecimals,
        AllowDataOverflow = AllowDataOverflow,
        TickLineStyle = SpecText.Line(TickLineStroke, TickLineStrokeWidth, null),
        Scale = Scale,
        Reversed = Reversed,
        Ticks = SpecText.Texts(Ticks),
        AllowDuplicatedCategory = AllowDuplicatedCategory,
        TickFill = TickFill,
        TickFontSize = TickFontSize,
        AxisLineStyle = SpecText.Line(AxisLineStroke, AxisLineStrokeWidth, null),
    };
}

/// <summary>Recharts' <c>PolarRadiusAxis</c>: the values along a radius.</summary>
public sealed class PolarRadiusAxis : PolarElement
{
    /// <summary>Defines <see cref="RadiusAxisId"/>.</summary>
    public static readonly StyledProperty<string> RadiusAxisIdProperty = AvaloniaProperty.Register<PolarRadiusAxis, string>(nameof(RadiusAxisId), "0");

    /// <summary>Defines <see cref="TickTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> TickTemplateProperty = AvaloniaProperty.Register<PolarRadiusAxis, Avalonia.Controls.Templates.IDataTemplate?>(nameof(TickTemplate));

    /// <summary>
    /// The custom content drawn in place of each tick's label (Recharts' <c>tick</c> element): a template whose data
    /// context is the tick's <see cref="TickContext"/> (its point on the axis, the chart's center and the axis' angle).
    /// </summary>
    public Avalonia.Controls.Templates.IDataTemplate? TickTemplate { get => GetValue(TickTemplateProperty); set => SetValue(TickTemplateProperty, value); }

    /// <summary>The axis' id, which radars and radial bars name (Recharts' <c>radiusAxisId</c>), <c>0</c> by default.</summary>
    public string RadiusAxisId { get => GetValue(RadiusAxisIdProperty); set => SetValue(RadiusAxisIdProperty, value); }

    /// <summary>Creates the axis, following its label children.</summary>
    public PolarRadiusAxis() => Relay.Follow(Labels, this, LogicalChildren);

    /// <summary>The axis' <c>Label</c> children, drawn after its <see cref="Label"/> text.</summary>
    [Content]
    public AvaloniaList<Label> Labels { get; } = [];

    /// <summary>Defines <see cref="Angle"/>.</summary>
    public static readonly StyledProperty<double> AngleProperty = AvaloniaProperty.Register<PolarRadiusAxis, double>(nameof(Angle));

    /// <summary>Defines <see cref="DataKey"/>.</summary>
    public static readonly StyledProperty<string?> DataKeyProperty = AvaloniaProperty.Register<PolarRadiusAxis, string?>(nameof(DataKey));

    /// <summary>Defines <see cref="Type"/>.</summary>
    public static readonly StyledProperty<AxisType> TypeProperty = AvaloniaProperty.Register<PolarRadiusAxis, AxisType>(nameof(Type), AxisType.Number);

    /// <summary>Defines <see cref="Orientation"/>.</summary>
    public static readonly StyledProperty<string> OrientationProperty = AvaloniaProperty.Register<PolarRadiusAxis, string>(nameof(Orientation), "right");

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<PolarRadiusAxis, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="AxisLine"/>.</summary>
    public static readonly StyledProperty<bool> AxisLineProperty = AvaloniaProperty.Register<PolarRadiusAxis, bool>(nameof(AxisLine), true);

    /// <summary>Defines <see cref="Tick"/>.</summary>
    public static readonly StyledProperty<bool> TickProperty = AvaloniaProperty.Register<PolarRadiusAxis, bool>(nameof(Tick), true);

    /// <summary>Defines <see cref="TickCount"/>.</summary>
    public static readonly StyledProperty<int> TickCountProperty = AvaloniaProperty.Register<PolarRadiusAxis, int>(nameof(TickCount), 5);

    /// <summary>Defines <see cref="AllowDecimals"/>.</summary>
    public static readonly StyledProperty<bool> AllowDecimalsProperty = AvaloniaProperty.Register<PolarRadiusAxis, bool>(nameof(AllowDecimals), true);

    /// <summary>Defines <see cref="Domain"/>.</summary>
    public static readonly StyledProperty<string?> DomainProperty = AvaloniaProperty.Register<PolarRadiusAxis, string?>(nameof(Domain));

    /// <summary>Defines <see cref="AllowDataOverflow"/>.</summary>
    public static readonly StyledProperty<bool> AllowDataOverflowProperty = AvaloniaProperty.Register<PolarRadiusAxis, bool>(nameof(AllowDataOverflow));

    /// <summary>Defines <see cref="TickFormat"/>.</summary>
    public static readonly StyledProperty<string?> TickFormatProperty = AvaloniaProperty.Register<PolarRadiusAxis, string?>(nameof(TickFormat));

    /// <summary>Defines <see cref="TickFormatter"/>.</summary>
    public static readonly StyledProperty<Avalonia.Data.Converters.IValueConverter?> TickFormatterProperty = AvaloniaProperty.Register<PolarRadiusAxis, Avalonia.Data.Converters.IValueConverter?>(nameof(TickFormatter));

    private readonly Formatted<Func<object?, int, string?>> _tickFormatter = new(Formatters.Tick);

    /// <summary>Defines <see cref="Label"/>.</summary>
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<PolarRadiusAxis, string?>(nameof(Label));

    /// <summary>Defines <see cref="Hide"/>.</summary>
    public static readonly StyledProperty<bool> HideProperty = AvaloniaProperty.Register<PolarRadiusAxis, bool>(nameof(Hide));

    /// <summary>Defines <see cref="Scale"/>.</summary>
    public static readonly StyledProperty<string?> ScaleProperty = AvaloniaProperty.Register<PolarRadiusAxis, string?>(nameof(Scale));

    /// <summary>Defines <see cref="Reversed"/>.</summary>
    public static readonly StyledProperty<bool> ReversedProperty = AvaloniaProperty.Register<PolarRadiusAxis, bool>(nameof(Reversed));

    /// <summary>Defines <see cref="Ticks"/>.</summary>
    public static readonly StyledProperty<string?> TicksProperty = AvaloniaProperty.Register<PolarRadiusAxis, string?>(nameof(Ticks));

    /// <summary>Defines <see cref="AllowDuplicatedCategory"/>.</summary>
    public static readonly StyledProperty<bool> AllowDuplicatedCategoryProperty = AvaloniaProperty.Register<PolarRadiusAxis, bool>(nameof(AllowDuplicatedCategory), true);

    /// <summary>Defines <see cref="TickFill"/>.</summary>
    public static readonly StyledProperty<string?> TickFillProperty = AvaloniaProperty.Register<PolarRadiusAxis, string?>(nameof(TickFill));

    /// <summary>Defines <see cref="TickFontSize"/>.</summary>
    public static readonly StyledProperty<double?> TickFontSizeProperty = AvaloniaProperty.Register<PolarRadiusAxis, double?>(nameof(TickFontSize));

    /// <summary>Defines <see cref="AxisLineStroke"/>.</summary>
    public static readonly StyledProperty<string?> AxisLineStrokeProperty = AvaloniaProperty.Register<PolarRadiusAxis, string?>(nameof(AxisLineStroke));

    /// <summary>Defines <see cref="AxisLineStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> AxisLineStrokeWidthProperty = AvaloniaProperty.Register<PolarRadiusAxis, double?>(nameof(AxisLineStrokeWidth));

    /// <summary>Defines <see cref="LabelPosition"/>.</summary>
    public static readonly StyledProperty<LabelPosition?> LabelPositionProperty = AvaloniaProperty.Register<PolarRadiusAxis, LabelPosition?>(nameof(LabelPosition));

    /// <summary>Defines <see cref="LabelAngle"/>.</summary>
    public static readonly StyledProperty<double> LabelAngleProperty = Axis.LabelAngleProperty.AddOwner<PolarRadiusAxis>();

    /// <summary>Defines <see cref="LabelOffset"/>.</summary>
    public static readonly StyledProperty<double> LabelOffsetProperty = Axis.LabelOffsetProperty.AddOwner<PolarRadiusAxis>();

    /// <summary>Defines <see cref="LabelFill"/>.</summary>
    public static readonly StyledProperty<string?> LabelFillProperty = Axis.LabelFillProperty.AddOwner<PolarRadiusAxis>();

    /// <summary>Defines <see cref="LabelFontSize"/>.</summary>
    public static readonly StyledProperty<double?> LabelFontSizeProperty = Axis.LabelFontSizeProperty.AddOwner<PolarRadiusAxis>();

    /// <summary>The angle the axis is drawn at, in degrees.</summary>
    public double Angle { get => GetValue(AngleProperty); set => SetValue(AngleProperty, value); }

    /// <summary>The data key of a radial bar chart's categories.</summary>
    public string? DataKey { get => GetValue(DataKeyProperty); set => SetValue(DataKeyProperty, value); }

    /// <summary>The axis' type; a radial bar chart's bands start at their ticks with <c>Category</c>.</summary>
    public AxisType Type { get => GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>The tick labels' side: <c>left</c>, <c>right</c> or <c>middle</c>.</summary>
    public string Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>The axis' color.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>Whether the axis line is drawn.</summary>
    public bool AxisLine { get => GetValue(AxisLineProperty); set => SetValue(AxisLineProperty, value); }

    /// <summary>Whether the tick labels are drawn.</summary>
    public bool Tick { get => GetValue(TickProperty); set => SetValue(TickProperty, value); }

    /// <summary>How many ticks the axis aims for.</summary>
    public int TickCount { get => GetValue(TickCountProperty); set => SetValue(TickCountProperty, value); }

    /// <summary>Whether the ticks may be fractional.</summary>
    public bool AllowDecimals { get => GetValue(AllowDecimalsProperty); set => SetValue(AllowDecimalsProperty, value); }

    /// <summary>The domain as <c>min,max</c> (numbers, <c>auto</c>, <c>dataMin</c>, <c>dataMax</c>), Recharts' <c>[0, auto]</c> by default.</summary>
    public string? Domain { get => GetValue(DomainProperty); set => SetValue(DomainProperty, value); }

    /// <summary>Whether a specified domain may cut the data.</summary>
    public bool AllowDataOverflow { get => GetValue(AllowDataOverflowProperty); set => SetValue(AllowDataOverflowProperty, value); }

    /// <summary>A .NET composite format for the tick labels.</summary>
    public string? TickFormat { get => GetValue(TickFormatProperty); set => SetValue(TickFormatProperty, value); }

    /// <summary>Recharts' <c>tickFormatter</c>: a converter of a tick's value to its text, its index as the parameter, over <see cref="TickFormat"/>.</summary>
    public Avalonia.Data.Converters.IValueConverter? TickFormatter { get => GetValue(TickFormatterProperty); set => SetValue(TickFormatterProperty, value); }

    /// <summary>The axis' label.</summary>
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Whether the axis is hidden (it still maps the values).</summary>
    public bool Hide { get => GetValue(HideProperty); set => SetValue(HideProperty, value); }

    /// <summary>The scale of a numeric axis: <c>auto</c>, <c>linear</c>, <c>log</c>, <c>pow</c> or <c>sqrt</c>.</summary>
    public string? Scale { get => GetValue(ScaleProperty); set => SetValue(ScaleProperty, value); }

    /// <summary>Whether the axis' range runs backwards.</summary>
    public bool Reversed { get => GetValue(ReversedProperty); set => SetValue(ReversedProperty, value); }

    /// <summary>The ticks to show instead of the computed ones: numbers or categories separated by commas.</summary>
    public string? Ticks { get => GetValue(TicksProperty); set => SetValue(TicksProperty, value); }

    /// <summary>Whether a category that appears twice gets two ticks (the default) or one.</summary>
    public bool AllowDuplicatedCategory { get => GetValue(AllowDuplicatedCategoryProperty); set => SetValue(AllowDuplicatedCategoryProperty, value); }

    /// <summary>The color of the tick labels (the <c>tick</c> object's <c>fill</c>).</summary>
    public string? TickFill { get => GetValue(TickFillProperty); set => SetValue(TickFillProperty, value); }

    /// <summary>The font size of the tick labels (the <c>tick</c> object's <c>fontSize</c>).</summary>
    public double? TickFontSize { get => GetValue(TickFontSizeProperty); set => SetValue(TickFontSizeProperty, value); }

    /// <summary>The axis line's color (the <c>axisLine</c> object's <c>stroke</c>).</summary>
    public string? AxisLineStroke { get => GetValue(AxisLineStrokeProperty); set => SetValue(AxisLineStrokeProperty, value); }

    /// <summary>The axis line's width (the <c>axisLine</c> object's <c>strokeWidth</c>).</summary>
    public double? AxisLineStrokeWidth { get => GetValue(AxisLineStrokeWidthProperty); set => SetValue(AxisLineStrokeWidthProperty, value); }

    /// <summary>Where the label sits: <c>Outside</c>, <c>Center</c>, <c>CenterTop</c>, <c>CenterBottom</c>, <c>InsideStart</c>, <c>InsideEnd</c>, <c>End</c>, or the middle of the axis by default.</summary>
    public LabelPosition? LabelPosition { get => GetValue(LabelPositionProperty); set => SetValue(LabelPositionProperty, value); }

    /// <summary>The label's rotation in degrees (Recharts' label <c>angle</c>).</summary>
    public double LabelAngle { get => GetValue(LabelAngleProperty); set => SetValue(LabelAngleProperty, value); }

    /// <summary>The label's distance from the axis (Recharts' label <c>offset</c>, 5 by default).</summary>
    public double LabelOffset { get => GetValue(LabelOffsetProperty); set => SetValue(LabelOffsetProperty, value); }

    /// <summary>The label's color (Recharts' label <c>fill</c>).</summary>
    public string? LabelFill { get => GetValue(LabelFillProperty); set => SetValue(LabelFillProperty, value); }

    /// <summary>The label's font size; the chart's by default.</summary>
    public double? LabelFontSize { get => GetValue(LabelFontSizeProperty); set => SetValue(LabelFontSizeProperty, value); }

    private protected override void Apply(SpecBuilder builder, int order) => builder.Polar = builder.Polar.RadiusAxis is null
        ? builder.Polar with { RadiusAxis = Spec(order, builder) }
        : builder.Polar with { MoreRadiusAxes = builder.Polar.MoreRadiusAxes.Add(Spec(order, builder)) };

    private PolarRadiusAxisSpec Spec(int order, SpecBuilder builder)
    {
        var (min, max) = SpecText.Domain(Domain);
        return new PolarRadiusAxisSpec
        {
            TickContent = builder.Content(TickTemplate, Content.ContentSlots.Tick, this, nameof(TickTemplate)),
            Id = RadiusAxisId,
            Angle = Angle,
            DataKey = DataKey,
            Category = Type == AxisType.Category,
            Orientation = Orientation,
            Stroke = Stroke,
            AxisLine = AxisLine,
            Tick = Tick,
            TickCount = TickCount,
            AllowDecimals = AllowDecimals,
            DomainMin = min,
            DomainMax = max,
            AllowDataOverflow = AllowDataOverflow,
            TickFormat = TickFormat,
            TickFormatter = _tickFormatter.Of(TickFormatter),
            TickConverter = Formatters.Reference(TickFormatter),
            Label = SpecText.Label(Label, LabelPosition, this),
            Labels = Pictor.Label.Specs(Labels, builder),
            Hide = Hide,
            Order = order,
            Scale = Scale,
            Reversed = Reversed,
            Ticks = SpecText.Texts(Ticks),
            AllowDuplicatedCategory = AllowDuplicatedCategory,
            TickFill = TickFill,
            TickFontSize = TickFontSize,
            AxisLineStyle = SpecText.Line(AxisLineStroke, AxisLineStrokeWidth, null),
        };
    }
}

/// <summary>Props merged over a pie's sector when it is active or inactive (Recharts' <c>activeShape</c> or <c>inactiveShape</c> given as an object).</summary>
public sealed class ActiveShape : AvaloniaObject
{
    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<ActiveShape, string?>(nameof(Fill));

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<ActiveShape, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="InnerRadius"/>.</summary>
    public static readonly StyledProperty<double?> InnerRadiusProperty = AvaloniaProperty.Register<ActiveShape, double?>(nameof(InnerRadius));

    /// <summary>Defines <see cref="OuterRadius"/>.</summary>
    public static readonly StyledProperty<double?> OuterRadiusProperty = AvaloniaProperty.Register<ActiveShape, double?>(nameof(OuterRadius));

    /// <summary>Defines <see cref="CornerRadius"/>.</summary>
    public static readonly StyledProperty<string?> CornerRadiusProperty = AvaloniaProperty.Register<ActiveShape, string?>(nameof(CornerRadius));

    /// <summary>Defines <see cref="FillOpacity"/>.</summary>
    public static readonly StyledProperty<double?> FillOpacityProperty = AvaloniaProperty.Register<ActiveShape, double?>(nameof(FillOpacity));

    /// <summary>Defines <see cref="Opacity"/>.</summary>
    public static readonly StyledProperty<double?> OpacityProperty = AvaloniaProperty.Register<ActiveShape, double?>(nameof(Opacity));

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> StrokeWidthProperty = AvaloniaProperty.Register<ActiveShape, double?>(nameof(StrokeWidth));

    /// <summary>Raised when a property changes.</summary>
    public event EventHandler? Changed;

    /// <summary>The sector's fill opacity (an <c>inactiveShape</c>'s usual <c>{fillOpacity: 0.5}</c>).</summary>
    public double? FillOpacity { get => GetValue(FillOpacityProperty); set => SetValue(FillOpacityProperty, value); }

    /// <summary>The sector's opacity.</summary>
    public double? Opacity { get => GetValue(OpacityProperty); set => SetValue(OpacityProperty, value); }

    /// <summary>The sector's stroke width.</summary>
    public double? StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }
    /// <summary>The active sector's fill.</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The active sector's stroke.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The active sector's inner radius, in pixels.</summary>
    public double? InnerRadius { get => GetValue(InnerRadiusProperty); set => SetValue(InnerRadiusProperty, value); }

    /// <summary>The active sector's outer radius, in pixels.</summary>
    public double? OuterRadius { get => GetValue(OuterRadiusProperty); set => SetValue(OuterRadiusProperty, value); }

    /// <summary>The active sector's corner radius.</summary>
    public string? CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    internal SectorShapeSpec Spec() => new() { Fill = Fill, Stroke = Stroke, InnerRadius = InnerRadius, OuterRadius = OuterRadius, CornerRadius = CornerRadius, FillOpacity = FillOpacity, Opacity = Opacity, StrokeWidth = StrokeWidth };

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>What <see cref="Pie"/> and <see cref="RadialBar"/> share: a data key, a fill, a legend icon and the cells.</summary>
public abstract class PolarSeries : PolarElement, IClickSource
{
    /// <summary>Defines <see cref="ClickCommand"/>.</summary>
    public static readonly StyledProperty<System.Windows.Input.ICommand?> ClickCommandProperty = Clickable.CommandProperty.AddOwner<PolarSeries>();

    /// <summary>Defines <see cref="IsAnimationActive"/>.</summary>
    public static readonly StyledProperty<bool> IsAnimationActiveProperty = Animated.IsActiveProperty.AddOwner<PolarSeries>();

    /// <summary>Defines <see cref="AnimationBegin"/>.</summary>
    public static readonly StyledProperty<double?> AnimationBeginProperty = Animated.BeginProperty.AddOwner<PolarSeries>();

    /// <summary>Defines <see cref="AnimationDuration"/>.</summary>
    public static readonly StyledProperty<double?> AnimationDurationProperty = Animated.DurationProperty.AddOwner<PolarSeries>();

    /// <summary>Defines <see cref="AnimationEasing"/>.</summary>
    public static readonly StyledProperty<AnimationEasing?> AnimationEasingProperty = Animated.EasingProperty.AddOwner<PolarSeries>();

    /// <summary>Defines <see cref="DataKey"/>.</summary>
    public static readonly StyledProperty<string> DataKeyProperty = AvaloniaProperty.Register<PolarSeries, string>(nameof(DataKey), "value");

    /// <summary>Defines <see cref="FillKey"/>.</summary>
    public static readonly StyledProperty<string?> FillKeyProperty = AvaloniaProperty.Register<PolarSeries, string?>(nameof(FillKey));

    /// <summary>Defines <see cref="OpacityKey"/>.</summary>
    public static readonly StyledProperty<string?> OpacityKeyProperty = AvaloniaProperty.Register<PolarSeries, string?>(nameof(OpacityKey));

    /// <summary>Defines <see cref="LegendType"/>.</summary>
    public static readonly StyledProperty<LegendIcon> LegendTypeProperty = AvaloniaProperty.Register<PolarSeries, LegendIcon>(nameof(LegendType), LegendIcon.Rect);

    /// <summary>Defines <see cref="Hide"/>.</summary>
    public static readonly StyledProperty<bool> HideProperty = AvaloniaProperty.Register<PolarSeries, bool>(nameof(Hide));

    /// <summary>Defines <see cref="TooltipType"/>.</summary>
    public static readonly StyledProperty<string?> TooltipTypeProperty = AvaloniaProperty.Register<PolarSeries, string?>(nameof(TooltipType));

    /// <summary>Defines <see cref="Cells"/>.</summary>
    public static readonly StyledProperty<AvaloniaList<ISeriesPart>> CellsProperty = AvaloniaProperty.Register<PolarSeries, AvaloniaList<ISeriesPart>>(nameof(Cells));

    /// <summary>Defines <see cref="ShapeTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ShapeTemplateProperty = AvaloniaProperty.Register<PolarSeries, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ShapeTemplate));

    /// <summary>Defines <see cref="ActiveShapeTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ActiveShapeTemplateProperty = AvaloniaProperty.Register<PolarSeries, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ActiveShapeTemplate));

    private protected PolarSeries()
    {
        var cells = new AvaloniaList<ISeriesPart>();
        Relay.Follow(cells, this, LogicalChildren);
        Relay.FollowClasses(this);
        SetValue(CellsProperty, cells);
    }

    /// <summary>The command a click on one of its items runs, with the item's index in its data as the parameter (Recharts' <c>onClick</c>): a button over the item on the web, the pointer on the desktop.</summary>
    public System.Windows.Input.ICommand? ClickCommand { get => GetValue(ClickCommandProperty); set => SetValue(ClickCommandProperty, value); }

    AvaloniaProperty IClickSource.CommandProperty => ClickCommandProperty;

    /// <summary>Whether it animates in when the chart appears or its data changes (Recharts' <c>isAnimationActive</c>, on by default as in the browser).</summary>
    public bool IsAnimationActive { get => GetValue(IsAnimationActiveProperty); set => SetValue(IsAnimationActiveProperty, value); }

    /// <summary>The delay before it animates in, in milliseconds; Recharts' default for the kind when null.</summary>
    public double? AnimationBegin { get => GetValue(AnimationBeginProperty); set => SetValue(AnimationBeginProperty, value); }

    /// <summary>How long it animates in, in milliseconds; Recharts' default for the kind when null.</summary>
    public double? AnimationDuration { get => GetValue(AnimationDurationProperty); set => SetValue(AnimationDurationProperty, value); }

    /// <summary>How it eases in; Recharts' default for the kind when null.</summary>
    public AnimationEasing? AnimationEasing { get => GetValue(AnimationEasingProperty); set => SetValue(AnimationEasingProperty, value); }

    /// <summary>The data key of the values.</summary>
    public string DataKey { get => GetValue(DataKeyProperty); set => SetValue(DataKeyProperty, value); }

    /// <summary>The data key of each item's own fill (Recharts spreads an item's <c>fill</c> onto its shape).</summary>
    public string? FillKey { get => GetValue(FillKeyProperty); set => SetValue(FillKeyProperty, value); }

    /// <summary>The data key of each item's own opacity, as Recharts spreads an item's <c>opacity</c> onto its shape.</summary>
    public string? OpacityKey { get => GetValue(OpacityKeyProperty); set => SetValue(OpacityKeyProperty, value); }

    /// <summary>The legend icon.</summary>
    public LegendIcon LegendType { get => GetValue(LegendTypeProperty); set => SetValue(LegendTypeProperty, value); }

    /// <summary>Whether the series is hidden.</summary>
    public bool Hide { get => GetValue(HideProperty); set => SetValue(HideProperty, value); }

    /// <summary><c>none</c> to leave the series' entries out of the tooltip (Recharts' <c>tooltipType</c>).</summary>
    public string? TooltipType { get => GetValue(TooltipTypeProperty); set => SetValue(TooltipTypeProperty, value); }

    /// <summary>The colors of individual items (Recharts' <c>Cell</c>), in data order, and the label lists (<c>LabelList</c>).</summary>
    [Content]
    public AvaloniaList<ISeriesPart> Cells => GetValue(CellsProperty);

    /// <summary>
    /// The custom content drawn in place of each sector (Recharts' <c>shape</c> element): a template whose data context is
    /// the sector's <see cref="SectorContext"/>, made of Avalonia's shapes and Pictor's template shapes (<see cref="Sector"/>).
    /// </summary>
    public Avalonia.Controls.Templates.IDataTemplate? ShapeTemplate { get => GetValue(ShapeTemplateProperty); set => SetValue(ShapeTemplateProperty, value); }

    /// <summary>The custom content drawn in place of an active sector (Recharts' <c>activeShape</c> element or function), with its <see cref="SectorContext"/>.</summary>
    public Avalonia.Controls.Templates.IDataTemplate? ActiveShapeTemplate { get => GetValue(ActiveShapeTemplateProperty); set => SetValue(ActiveShapeTemplateProperty, value); }

    /// <summary>The series' templates of custom content, compiled.</summary>
    private protected (ContentSpec? Shape, ContentSpec? Active) Contents(SpecBuilder builder) =>
        (builder.Content(ShapeTemplate, Content.ContentSlots.Sector, this, nameof(ShapeTemplate)), builder.Content(ActiveShapeTemplate, Content.ContentSlots.Sector, this, nameof(ActiveShapeTemplate)));

    /// <summary>The data members the series' templates read, which its own data's table must hold.</summary>
    private protected static IEnumerable<string> Keys((ContentSpec? Shape, ContentSpec? Active) contents) =>
        (contents.Shape?.Keys ?? []).Concat(contents.Active?.Keys ?? []);

    private protected System.Collections.Immutable.ImmutableArray<CellSpec> CellSpecs() => [.. Cells.OfType<Cell>().Select(cell => cell.Spec())];
}

/// <summary>Recharts' <c>Pie</c>: sectors in proportion to the values, from its own data or the chart's.</summary>
public sealed class Pie : PolarSeries, IOwnItems, IAnimatedElement
{
    /// <summary>Raised on the desktop when the element's enter or update animation starts, before its begin delay (Recharts' <c>onAnimationStart</c>); a web chart, written without script, raises nothing.</summary>
    public event EventHandler? AnimationStarted;

    /// <summary>Raised on the desktop when the element's enter or update animation has moved to its end (Recharts' <c>onAnimationEnd</c>); a web chart raises nothing.</summary>
    public event EventHandler? AnimationEnded;

    Motion.Timing? IAnimatedElement.Timing => Motion.Animations.Of(new Specs.PieSpec { Animation = Animated.Spec(this) });

    void IAnimatedElement.RaiseAnimation(bool ended)
    {
        if (ended)
        {
            AnimationEnded?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            AnimationStarted?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Defines <see cref="RootTabIndex"/>.</summary>
    public static readonly StyledProperty<int?> RootTabIndexProperty = AvaloniaProperty.Register<Pie, int?>(nameof(RootTabIndex), 0);

    /// <summary>The pie layer's tab index on the web (Recharts' <c>rootTabIndex</c>): 0 by default, a tab stop of its own as Recharts' pie, null for none.</summary>
    public int? RootTabIndex { get => GetValue(RootTabIndexProperty); set => SetValue(RootTabIndexProperty, value); }

    /// <summary>Defines <see cref="Data"/>.</summary>
    public static readonly StyledProperty<IEnumerable?> DataProperty = AvaloniaProperty.Register<Pie, IEnumerable?>(nameof(Data));

    /// <summary>Defines <see cref="NameKey"/>.</summary>
    public static readonly StyledProperty<string> NameKeyProperty = AvaloniaProperty.Register<Pie, string>(nameof(NameKey), "name");

    /// <summary>Defines <see cref="Cx"/>.</summary>
    public static readonly StyledProperty<string> CxProperty = AvaloniaProperty.Register<Pie, string>(nameof(Cx), "50%");

    /// <summary>Defines <see cref="Cy"/>.</summary>
    public static readonly StyledProperty<string> CyProperty = AvaloniaProperty.Register<Pie, string>(nameof(Cy), "50%");

    /// <summary>Defines <see cref="InnerRadius"/>.</summary>
    public static readonly StyledProperty<string> InnerRadiusProperty = AvaloniaProperty.Register<Pie, string>(nameof(InnerRadius), "0");

    /// <summary>Defines <see cref="OuterRadius"/>.</summary>
    public static readonly StyledProperty<string> OuterRadiusProperty = AvaloniaProperty.Register<Pie, string>(nameof(OuterRadius), "80%");

    /// <summary>Defines <see cref="StartAngle"/>.</summary>
    public static readonly StyledProperty<double> StartAngleProperty = AvaloniaProperty.Register<Pie, double>(nameof(StartAngle));

    /// <summary>Defines <see cref="EndAngle"/>.</summary>
    public static readonly StyledProperty<double> EndAngleProperty = AvaloniaProperty.Register<Pie, double>(nameof(EndAngle), 360);

    /// <summary>Defines <see cref="PaddingAngle"/>.</summary>
    public static readonly StyledProperty<double> PaddingAngleProperty = AvaloniaProperty.Register<Pie, double>(nameof(PaddingAngle));

    /// <summary>Defines <see cref="MinAngle"/>.</summary>
    public static readonly StyledProperty<double> MinAngleProperty = AvaloniaProperty.Register<Pie, double>(nameof(MinAngle));

    /// <summary>Defines <see cref="CornerRadius"/>.</summary>
    public static readonly StyledProperty<string?> CornerRadiusProperty = AvaloniaProperty.Register<Pie, string?>(nameof(CornerRadius));

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<Pie, string?>(nameof(Fill));

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<Pie, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="Label"/>.</summary>
    public static readonly StyledProperty<bool> LabelProperty = AvaloniaProperty.Register<Pie, bool>(nameof(Label));

    /// <summary>Defines <see cref="LabelLine"/>.</summary>
    public static readonly StyledProperty<bool> LabelLineProperty = AvaloniaProperty.Register<Pie, bool>(nameof(LabelLine), true);

    /// <summary>Defines <see cref="ActiveIndex"/>.</summary>
    public static readonly StyledProperty<int?> ActiveIndexProperty = AvaloniaProperty.Register<Pie, int?>(nameof(ActiveIndex));

    /// <summary>Defines <see cref="ActiveShape"/>.</summary>
    public static readonly StyledProperty<ActiveShape?> ActiveShapeProperty = AvaloniaProperty.Register<Pie, ActiveShape?>(nameof(ActiveShape));

    /// <summary>Defines <see cref="ActiveIndexes"/>.</summary>
    public static readonly StyledProperty<string?> ActiveIndexesProperty = AvaloniaProperty.Register<Pie, string?>(nameof(ActiveIndexes));

    /// <summary>Defines <see cref="InactiveShape"/>.</summary>
    public static readonly StyledProperty<ActiveShape?> InactiveShapeProperty = AvaloniaProperty.Register<Pie, ActiveShape?>(nameof(InactiveShape));

    /// <summary>Defines <see cref="BlendStroke"/>.</summary>
    public static readonly StyledProperty<bool> BlendStrokeProperty = AvaloniaProperty.Register<Pie, bool>(nameof(BlendStroke));

    /// <summary>Defines <see cref="LabelOffsetRadius"/>.</summary>
    public static readonly StyledProperty<double> LabelOffsetRadiusProperty = AvaloniaProperty.Register<Pie, double>(nameof(LabelOffsetRadius), 20);

    /// <summary>Defines <see cref="LabelFill"/>.</summary>
    public static readonly StyledProperty<string?> LabelFillProperty = AvaloniaProperty.Register<Pie, string?>(nameof(LabelFill));

    /// <summary>Defines <see cref="LabelFontSize"/>.</summary>
    public static readonly StyledProperty<double?> LabelFontSizeProperty = AvaloniaProperty.Register<Pie, double?>(nameof(LabelFontSize));

    /// <summary>Defines <see cref="LabelFontWeight"/>.</summary>
    public static readonly StyledProperty<string?> LabelFontWeightProperty = AvaloniaProperty.Register<Pie, string?>(nameof(LabelFontWeight));

    /// <summary>Defines <see cref="LabelStroke"/>.</summary>
    public static readonly StyledProperty<string?> LabelStrokeProperty = AvaloniaProperty.Register<Pie, string?>(nameof(LabelStroke));

    /// <summary>Defines <see cref="LabelLineStroke"/>.</summary>
    public static readonly StyledProperty<string?> LabelLineStrokeProperty = AvaloniaProperty.Register<Pie, string?>(nameof(LabelLineStroke));

    /// <summary>Defines <see cref="LabelLineStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> LabelLineStrokeWidthProperty = AvaloniaProperty.Register<Pie, double?>(nameof(LabelLineStrokeWidth));

    /// <summary>Defines <see cref="LabelLineStrokeDashArray"/>.</summary>
    public static readonly StyledProperty<object?> LabelLineStrokeDashArrayProperty = AvaloniaProperty.Register<Pie, object?>(nameof(LabelLineStrokeDashArray));
    /// <summary>The sectors drawn active, when there are several (Recharts' <c>activeIndex</c> as an array): <c>0 2</c>.</summary>
    public string? ActiveIndexes { get => GetValue(ActiveIndexesProperty); set => SetValue(ActiveIndexesProperty, value); }

    /// <summary>What the other sectors look like while some are active (Recharts' <c>inactiveShape</c> object).</summary>
    public ActiveShape? InactiveShape { get => GetValue(InactiveShapeProperty); set => SetValue(InactiveShapeProperty, value); }

    /// <summary>Whether each sector's stroke is its fill (Recharts' <c>blendStroke</c>).</summary>
    public bool BlendStroke { get => GetValue(BlendStrokeProperty); set => SetValue(BlendStrokeProperty, value); }

    /// <summary>The distance from the outer radius to a label (Recharts' <c>label.offsetRadius</c>).</summary>
    public double LabelOffsetRadius { get => GetValue(LabelOffsetRadiusProperty); set => SetValue(LabelOffsetRadiusProperty, value); }

    /// <summary>The labels' fill; each sector's by default.</summary>
    public string? LabelFill { get => GetValue(LabelFillProperty); set => SetValue(LabelFillProperty, value); }

    /// <summary>The labels' font size.</summary>
    public double? LabelFontSize { get => GetValue(LabelFontSizeProperty); set => SetValue(LabelFontSizeProperty, value); }

    /// <summary>The labels' weight.</summary>
    public string? LabelFontWeight { get => GetValue(LabelFontWeightProperty); set => SetValue(LabelFontWeightProperty, value); }

    /// <summary>The labels' stroke.</summary>
    public string? LabelStroke { get => GetValue(LabelStrokeProperty); set => SetValue(LabelStrokeProperty, value); }

    /// <summary>The label lines' stroke (Recharts' <c>labelLine</c> object); each sector's fill by default.</summary>
    public string? LabelLineStroke { get => GetValue(LabelLineStrokeProperty); set => SetValue(LabelLineStrokeProperty, value); }

    /// <summary>The label lines' stroke width.</summary>
    public double? LabelLineStrokeWidth { get => GetValue(LabelLineStrokeWidthProperty); set => SetValue(LabelLineStrokeWidthProperty, value); }

    /// <summary>The label lines' dash pattern.</summary>
    public object? LabelLineStrokeDashArray { get => GetValue(LabelLineStrokeDashArrayProperty); set => SetValue(LabelLineStrokeDashArrayProperty, value); }
    /// <summary>The pie's own items (Recharts' <c>data</c>); the chart's data when null.</summary>
    public IEnumerable? Data { get => GetValue(DataProperty); set => SetValue(DataProperty, value); }

    IEnumerable? IOwnItems.OwnItems => Data;

    /// <summary>The data key of the sectors' names.</summary>
    public string NameKey { get => GetValue(NameKeyProperty); set => SetValue(NameKeyProperty, value); }

    /// <summary>The center's x: pixels from the plot's left, or a percentage of its width.</summary>
    public string Cx { get => GetValue(CxProperty); set => SetValue(CxProperty, value); }

    /// <summary>The center's y: pixels from the plot's top, or a percentage of its height.</summary>
    public string Cy { get => GetValue(CyProperty); set => SetValue(CyProperty, value); }

    /// <summary>The inner radius: pixels or a percentage of the largest radius.</summary>
    public string InnerRadius { get => GetValue(InnerRadiusProperty); set => SetValue(InnerRadiusProperty, value); }

    /// <summary>The outer radius: pixels or a percentage of the largest radius.</summary>
    public string OuterRadius { get => GetValue(OuterRadiusProperty); set => SetValue(OuterRadiusProperty, value); }

    /// <summary>The angle the first sector starts at, counterclockwise from 3 o'clock.</summary>
    public double StartAngle { get => GetValue(StartAngleProperty); set => SetValue(StartAngleProperty, value); }

    /// <summary>The angle the last sector ends at.</summary>
    public double EndAngle { get => GetValue(EndAngleProperty); set => SetValue(EndAngleProperty, value); }

    /// <summary>The angle between two sectors.</summary>
    public double PaddingAngle { get => GetValue(PaddingAngleProperty); set => SetValue(PaddingAngleProperty, value); }

    /// <summary>The smallest angle a non-zero sector takes.</summary>
    public double MinAngle { get => GetValue(MinAngleProperty); set => SetValue(MinAngleProperty, value); }

    /// <summary>The corner radius: pixels or a percentage of the ring's width.</summary>
    public string? CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>The sectors' fill.</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The sectors' stroke.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>Whether each sector shows its value outside it.</summary>
    public bool Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Whether a line joins each label to its sector.</summary>
    public bool LabelLine { get => GetValue(LabelLineProperty); set => SetValue(LabelLineProperty, value); }

    /// <summary>The sector drawn active whatever the pointer does.</summary>
    public int? ActiveIndex { get => GetValue(ActiveIndexProperty); set => SetValue(ActiveIndexProperty, value); }

    /// <summary>What an active sector looks like; with a tooltip, hovering a sector makes it active.</summary>
    public ActiveShape? ActiveShape { get => GetValue(ActiveShapeProperty); set => SetValue(ActiveShapeProperty, value); }

    private protected override void Apply(SpecBuilder builder, int order)
    {
        var contents = Contents(builder);
        builder.Polar = builder.Polar with { Pies = builder.Polar.Pies.Add(Spec(builder, order, contents)) };
    }

    private PieSpec Spec(SpecBuilder builder, int order, (ContentSpec? Shape, ContentSpec? Active) contents) => new()
        {
            ShapeContent = contents.Shape,
            ActiveShapeContent = contents.Active,
            ClassName = ChangeRelay.ClassName(this),
            RootTabIndex = RootTabIndex,
            ClickCommand = Clickable.Key(this, order),
            DataKey = DataKey,
            NameKey = NameKey,
            FillKey = FillKey,
            OpacityKey = OpacityKey,
            Data = Data is { } items ? DataTable.Read(items, [DataKey, NameKey, FillKey, OpacityKey, .. Cells.OfType<LabelList>().Select(list => list.DataKey), .. Keys(contents)], builder.Members) : null,
            Cx = Cx,
            Cy = Cy,
            InnerRadius = InnerRadius,
            OuterRadius = OuterRadius,
            StartAngle = StartAngle,
            EndAngle = EndAngle,
            PaddingAngle = PaddingAngle,
            MinAngle = MinAngle,
            CornerRadius = CornerRadius,
            Fill = Fill,
            Stroke = Stroke,
            Label = Label,
            LabelLine = LabelLine,
            Cells = CellSpecs(),
            LabelLists = [.. Cells.OfType<LabelList>().Select(list => list.Spec(builder))],
            ActiveIndex = ActiveIndex,
            ActiveShape = ActiveShape?.Spec(),
            ActiveIndexes = [.. SpecText.Numbers(ActiveIndexes).Select(index => (int)index)],
            InactiveShape = InactiveShape?.Spec(),
            BlendStroke = BlendStroke,
            LabelOffsetRadius = LabelOffsetRadius,
            LabelProps = LabelFill is null && LabelFontSize is null && LabelFontWeight is null && LabelStroke is null ? null : new LabelSpec { Fill = LabelFill, FontSize = LabelFontSize, FontWeight = LabelFontWeight, Stroke = LabelStroke },
            LabelLineStroke = LabelLineStroke,
            LabelLineStrokeWidth = LabelLineStrokeWidth,
            LabelLineStrokeDashArray = SpecText.Dashes(LabelLineStrokeDashArray),
            LegendType = LegendType,
            Hide = Hide,
            TooltipNone = string.Equals(TooltipType, "none", StringComparison.OrdinalIgnoreCase),
            Animation = Animated.Spec(this),
            Order = order,
        };

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ActiveShapeProperty || change.Property == InactiveShapeProperty)
        {
            if (change.OldValue is ActiveShape old)
            {
                old.Changed -= OnShapeChanged;
            }

            if (change.NewValue is ActiveShape shape)
            {
                shape.Changed += OnShapeChanged;
            }
        }
    }

    private void OnShapeChanged(object? sender, EventArgs e) => Relay.Raise(this);
}

/// <summary>Recharts' <c>Radar</c>: a polygon through each category's value.</summary>
public sealed class Radar : PolarElement, IClickSource, IAnimatedElement
{
    /// <summary>Raised on the desktop when the element's enter or update animation starts, before its begin delay (Recharts' <c>onAnimationStart</c>); a web chart, written without script, raises nothing.</summary>
    public event EventHandler? AnimationStarted;

    /// <summary>Raised on the desktop when the element's enter or update animation has moved to its end (Recharts' <c>onAnimationEnd</c>); a web chart raises nothing.</summary>
    public event EventHandler? AnimationEnded;

    Motion.Timing? IAnimatedElement.Timing => Motion.Animations.Of(new Specs.RadarSpec { Animation = Animated.Spec(this) });

    void IAnimatedElement.RaiseAnimation(bool ended)
    {
        if (ended)
        {
            AnimationEnded?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            AnimationStarted?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Defines <see cref="RadiusAxisId"/>.</summary>
    public static readonly StyledProperty<string> RadiusAxisIdProperty = AvaloniaProperty.Register<Radar, string>(nameof(RadiusAxisId), "0");

    /// <summary>Defines <see cref="ShapeTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ShapeTemplateProperty = AvaloniaProperty.Register<Radar, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ShapeTemplate));

    /// <summary>The radius axis the radar maps through (Recharts' <c>radiusAxisId</c>).</summary>
    public string RadiusAxisId { get => GetValue(RadiusAxisIdProperty); set => SetValue(RadiusAxisIdProperty, value); }

    /// <summary>
    /// The custom content drawn in place of the radar's polygon (Recharts' <c>shape</c> element): a template whose data
    /// context is the radar's <see cref="RadarShapeContext"/> (its points, center and paint); its dots stay the radar's.
    /// </summary>
    public Avalonia.Controls.Templates.IDataTemplate? ShapeTemplate { get => GetValue(ShapeTemplateProperty); set => SetValue(ShapeTemplateProperty, value); }

    /// <summary>Defines <see cref="AngleAxisId"/>.</summary>
    public static readonly StyledProperty<string> AngleAxisIdProperty = AvaloniaProperty.Register<Radar, string>(nameof(AngleAxisId), "0");

    /// <summary>The angle axis the radar maps through (Recharts' <c>angleAxisId</c>).</summary>
    public string AngleAxisId { get => GetValue(AngleAxisIdProperty); set => SetValue(AngleAxisIdProperty, value); }
    /// <summary>Defines <see cref="ClickCommand"/>.</summary>
    public static readonly StyledProperty<System.Windows.Input.ICommand?> ClickCommandProperty = Clickable.CommandProperty.AddOwner<Radar>();

    /// <summary>Defines <see cref="IsAnimationActive"/>.</summary>
    public static readonly StyledProperty<bool> IsAnimationActiveProperty = Animated.IsActiveProperty.AddOwner<Radar>();

    /// <summary>Defines <see cref="AnimationBegin"/>.</summary>
    public static readonly StyledProperty<double?> AnimationBeginProperty = Animated.BeginProperty.AddOwner<Radar>();

    /// <summary>Defines <see cref="AnimationDuration"/>.</summary>
    public static readonly StyledProperty<double?> AnimationDurationProperty = Animated.DurationProperty.AddOwner<Radar>();

    /// <summary>Defines <see cref="AnimationEasing"/>.</summary>
    public static readonly StyledProperty<AnimationEasing?> AnimationEasingProperty = Animated.EasingProperty.AddOwner<Radar>();

    /// <summary>Defines <see cref="DataKey"/>.</summary>
    public static readonly StyledProperty<string> DataKeyProperty = AvaloniaProperty.Register<Radar, string>(nameof(DataKey), string.Empty);

    /// <summary>Defines <see cref="Name"/>.</summary>
    public static new readonly StyledProperty<string?> NameProperty = AvaloniaProperty.Register<Radar, string?>(nameof(Name));

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<Radar, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<Radar, string?>(nameof(Fill));

    /// <summary>Defines <see cref="FillOpacity"/>.</summary>
    public static readonly StyledProperty<double?> FillOpacityProperty = AvaloniaProperty.Register<Radar, double?>(nameof(FillOpacity));

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> StrokeWidthProperty = AvaloniaProperty.Register<Radar, double?>(nameof(StrokeWidth));

    /// <summary>Defines <see cref="Dot"/>.</summary>
    public static readonly StyledProperty<bool> DotProperty = AvaloniaProperty.Register<Radar, bool>(nameof(Dot));

    /// <summary>Defines <see cref="ActiveDot"/>.</summary>
    public static readonly StyledProperty<double> ActiveDotProperty = AvaloniaProperty.Register<Radar, double>(nameof(ActiveDot), 4);

    /// <summary>Defines <see cref="Unit"/>.</summary>
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<Radar, string?>(nameof(Unit));

    /// <summary>Defines <see cref="LegendType"/>.</summary>
    public static readonly StyledProperty<LegendIcon> LegendTypeProperty = AvaloniaProperty.Register<Radar, LegendIcon>(nameof(LegendType), LegendIcon.Rect);

    /// <summary>Defines <see cref="Hide"/>.</summary>
    public static readonly StyledProperty<bool> HideProperty = AvaloniaProperty.Register<Radar, bool>(nameof(Hide));

    /// <summary>Defines <see cref="TooltipType"/>.</summary>
    public static readonly StyledProperty<string?> TooltipTypeProperty = AvaloniaProperty.Register<Radar, string?>(nameof(TooltipType));

    /// <summary>Defines <see cref="DotRadius"/>.</summary>
    public static readonly StyledProperty<double?> DotRadiusProperty = AvaloniaProperty.Register<Radar, double?>(nameof(DotRadius));

    /// <summary>Defines <see cref="DotFill"/>.</summary>
    public static readonly StyledProperty<string?> DotFillProperty = AvaloniaProperty.Register<Radar, string?>(nameof(DotFill));

    /// <summary>Defines <see cref="DotStroke"/>.</summary>
    public static readonly StyledProperty<string?> DotStrokeProperty = AvaloniaProperty.Register<Radar, string?>(nameof(DotStroke));

    /// <summary>Defines <see cref="DotStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> DotStrokeWidthProperty = AvaloniaProperty.Register<Radar, double?>(nameof(DotStrokeWidth));

    /// <summary>Defines <see cref="DotFillOpacity"/>.</summary>
    public static readonly StyledProperty<double?> DotFillOpacityProperty = AvaloniaProperty.Register<Radar, double?>(nameof(DotFillOpacity));

    /// <summary>Defines <see cref="DotStrokeDashArray"/>.</summary>
    public static readonly StyledProperty<object?> DotStrokeDashArrayProperty = AvaloniaProperty.Register<Radar, object?>(nameof(DotStrokeDashArray));

    /// <summary>Defines <see cref="ActiveDotFill"/>.</summary>
    public static readonly StyledProperty<string?> ActiveDotFillProperty = AvaloniaProperty.Register<Radar, string?>(nameof(ActiveDotFill));

    /// <summary>Defines <see cref="ActiveDotStroke"/>.</summary>
    public static readonly StyledProperty<string?> ActiveDotStrokeProperty = AvaloniaProperty.Register<Radar, string?>(nameof(ActiveDotStroke));

    /// <summary>Defines <see cref="ActiveDotStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> ActiveDotStrokeWidthProperty = AvaloniaProperty.Register<Radar, double?>(nameof(ActiveDotStrokeWidth));

    /// <summary>Defines <see cref="ConnectNulls"/>.</summary>
    public static readonly StyledProperty<bool> ConnectNullsProperty = AvaloniaProperty.Register<Radar, bool>(nameof(ConnectNulls));

    /// <summary>Defines <see cref="Label"/>.</summary>
    public static readonly StyledProperty<LabelPosition?> LabelProperty = AvaloniaProperty.Register<Radar, LabelPosition?>(nameof(Label));
    /// <summary>Defines <see cref="Parts"/>.</summary>
    public static readonly StyledProperty<AvaloniaList<ISeriesPart>> PartsProperty = AvaloniaProperty.Register<Radar, AvaloniaList<ISeriesPart>>(nameof(Parts));

    /// <summary>Creates a radar.</summary>
    public Radar()
    {
        var parts = new AvaloniaList<ISeriesPart>();
        Relay.Follow(parts, this, LogicalChildren);
        SetValue(PartsProperty, parts);
    }

    /// <summary>The command a click on one of its items runs, with the item's index in its data as the parameter (Recharts' <c>onClick</c>): a button over the item on the web, the pointer on the desktop.</summary>
    public System.Windows.Input.ICommand? ClickCommand { get => GetValue(ClickCommandProperty); set => SetValue(ClickCommandProperty, value); }

    AvaloniaProperty IClickSource.CommandProperty => ClickCommandProperty;

    /// <summary>Whether it animates in when the chart appears or its data changes (Recharts' <c>isAnimationActive</c>, on by default as in the browser).</summary>
    public bool IsAnimationActive { get => GetValue(IsAnimationActiveProperty); set => SetValue(IsAnimationActiveProperty, value); }

    /// <summary>The delay before it animates in, in milliseconds; Recharts' default for the kind when null.</summary>
    public double? AnimationBegin { get => GetValue(AnimationBeginProperty); set => SetValue(AnimationBeginProperty, value); }

    /// <summary>How long it animates in, in milliseconds; Recharts' default for the kind when null.</summary>
    public double? AnimationDuration { get => GetValue(AnimationDurationProperty); set => SetValue(AnimationDurationProperty, value); }

    /// <summary>How it eases in; Recharts' default for the kind when null.</summary>
    public AnimationEasing? AnimationEasing { get => GetValue(AnimationEasingProperty); set => SetValue(AnimationEasingProperty, value); }

    /// <summary>The dots' radius (Recharts' <c>dot={{r}}</c>); 3 by default.</summary>
    public double? DotRadius { get => GetValue(DotRadiusProperty); set => SetValue(DotRadiusProperty, value); }

    /// <summary>The dots' fill.</summary>
    public string? DotFill { get => GetValue(DotFillProperty); set => SetValue(DotFillProperty, value); }

    /// <summary>The dots' stroke.</summary>
    public string? DotStroke { get => GetValue(DotStrokeProperty); set => SetValue(DotStrokeProperty, value); }

    /// <summary>The dots' stroke width.</summary>
    public double? DotStrokeWidth { get => GetValue(DotStrokeWidthProperty); set => SetValue(DotStrokeWidthProperty, value); }

    /// <summary>The dots' fill opacity.</summary>
    public double? DotFillOpacity { get => GetValue(DotFillOpacityProperty); set => SetValue(DotFillOpacityProperty, value); }

    /// <summary>The dots' dash pattern.</summary>
    public object? DotStrokeDashArray { get => GetValue(DotStrokeDashArrayProperty); set => SetValue(DotStrokeDashArrayProperty, value); }

    /// <summary>The hovered category's dot fill (Recharts' <c>activeDot.fill</c>); the radar's color by default.</summary>
    public string? ActiveDotFill { get => GetValue(ActiveDotFillProperty); set => SetValue(ActiveDotFillProperty, value); }

    /// <summary>The hovered category's dot stroke; white by default.</summary>
    public string? ActiveDotStroke { get => GetValue(ActiveDotStrokeProperty); set => SetValue(ActiveDotStrokeProperty, value); }

    /// <summary>The hovered category's dot stroke width; 2 by default.</summary>
    public double? ActiveDotStrokeWidth { get => GetValue(ActiveDotStrokeWidthProperty); set => SetValue(ActiveDotStrokeWidthProperty, value); }

    /// <summary>Whether the polygon runs through the points around missing values (Recharts' <c>connectNulls</c>).</summary>
    public bool ConnectNulls { get => GetValue(ConnectNullsProperty); set => SetValue(ConnectNullsProperty, value); }

    /// <summary>Shows each value as a label at this position around its point (Recharts' <c>label</c>).</summary>
    public LabelPosition? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    /// <summary>The radar's label lists (Recharts' <c>LabelList</c> children).</summary>
    [Content]
    public AvaloniaList<ISeriesPart> Parts => GetValue(PartsProperty);

    /// <summary>The data key of the values.</summary>
    public string DataKey { get => GetValue(DataKeyProperty); set => SetValue(DataKeyProperty, value); }

    /// <summary>The name in the legend and the tooltip.</summary>
    public new string? Name { get => GetValue(NameProperty); set => SetValue(NameProperty, value); }

    /// <summary>The polygon's stroke.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The polygon's fill.</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The fill's opacity.</summary>
    public double? FillOpacity { get => GetValue(FillOpacityProperty); set => SetValue(FillOpacityProperty, value); }

    /// <summary>The stroke's width.</summary>
    public double? StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    /// <summary>Whether a dot marks every point.</summary>
    public bool Dot { get => GetValue(DotProperty); set => SetValue(DotProperty, value); }

    /// <summary>The radius of the hovered category's dot; 0 hides it.</summary>
    public double ActiveDot { get => GetValue(ActiveDotProperty); set => SetValue(ActiveDotProperty, value); }

    /// <summary>A unit written after the values in the tooltip.</summary>
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    /// <summary>The legend icon.</summary>
    public LegendIcon LegendType { get => GetValue(LegendTypeProperty); set => SetValue(LegendTypeProperty, value); }

    /// <summary>Whether the radar is hidden.</summary>
    public bool Hide { get => GetValue(HideProperty); set => SetValue(HideProperty, value); }

    /// <summary><c>none</c> to leave the radar out of the tooltip (Recharts' <c>tooltipType</c>).</summary>
    public string? TooltipType { get => GetValue(TooltipTypeProperty); set => SetValue(TooltipTypeProperty, value); }

    private protected override void Apply(SpecBuilder builder, int order) => builder.Polar = builder.Polar with
    {
        Radars = builder.Polar.Radars.Add(new RadarSpec
        {
            ShapeContent = builder.Content(ShapeTemplate, Content.ContentSlots.Radar, this, nameof(ShapeTemplate)),
            AngleAxisId = AngleAxisId,
            RadiusAxisId = RadiusAxisId,
            ClickCommand = Clickable.Key(this, order),
            DataKey = DataKey,
            Name = Name,
            Stroke = Stroke,
            Fill = Fill,
            FillOpacity = FillOpacity,
            StrokeWidth = StrokeWidth,
            Dot = Dot || DotRadius is not null || DotFill is not null || DotStroke is not null || DotStrokeWidth is not null || DotFillOpacity is not null || DotStrokeDashArray is not null,
            DotProps = DotRadius is null && DotFill is null && DotStroke is null && DotStrokeWidth is null && DotFillOpacity is null && DotStrokeDashArray is null
                ? null
                : new ActiveStyle { R = DotRadius, Fill = DotFill, Stroke = DotStroke, StrokeWidth = DotStrokeWidth, FillOpacity = DotFillOpacity, StrokeDashArray = SpecText.Dashes(DotStrokeDashArray) },
            ActiveDot = ActiveDot > 0,
            ActiveDotRadius = ActiveDot,
            ActiveDotStyle = ActiveDotFill is null && ActiveDotStroke is null && ActiveDotStrokeWidth is null ? null : new ActiveStyle(ActiveDotFill, ActiveDotStroke, ActiveDotStrokeWidth),
            ConnectNulls = ConnectNulls,
            Labels = LabelList.Of(Parts, Label is { } position ? new LabelSpec { Position = position } : null, builder).First,
            LabelLists = LabelList.Of(Parts, Label is { } at ? new LabelSpec { Position = at } : null, builder).Others,
            Unit = Unit,
            LegendType = LegendType,
            Hide = Hide,
            TooltipNone = string.Equals(TooltipType, "none", StringComparison.OrdinalIgnoreCase),
            Animation = Animated.Spec(this),
            Order = order,
        }),
    };
}

/// <summary>Recharts' <c>RadialBar</c>: a bar along an arc per category, in the radius axis' bands.</summary>
public sealed class RadialBar : PolarSeries, IAnimatedElement
{
    /// <summary>Raised on the desktop when the element's enter or update animation starts, before its begin delay (Recharts' <c>onAnimationStart</c>); a web chart, written without script, raises nothing.</summary>
    public event EventHandler? AnimationStarted;

    /// <summary>Raised on the desktop when the element's enter or update animation has moved to its end (Recharts' <c>onAnimationEnd</c>); a web chart raises nothing.</summary>
    public event EventHandler? AnimationEnded;

    Motion.Timing? IAnimatedElement.Timing => Motion.Animations.Of(new Specs.RadialBarSpec { Animation = Animated.Spec(this) });

    void IAnimatedElement.RaiseAnimation(bool ended)
    {
        if (ended)
        {
            AnimationEnded?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            AnimationStarted?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Defines <see cref="RadiusAxisId"/>.</summary>
    public static readonly StyledProperty<string> RadiusAxisIdProperty = AvaloniaProperty.Register<RadialBar, string>(nameof(RadiusAxisId), "0");

    /// <summary>Defines <see cref="ActiveIndex"/>.</summary>
    public static readonly StyledProperty<int?> ActiveIndexProperty = AvaloniaProperty.Register<RadialBar, int?>(nameof(ActiveIndex));

    /// <summary>Recharts 2's <c>activeIndex</c>, which Recharts 3's <c>RadialBar</c> ignores and Pictor with it: the hovered ring takes the <c>ActiveShapeTemplate</c>.</summary>
    public int? ActiveIndex { get => GetValue(ActiveIndexProperty); set => SetValue(ActiveIndexProperty, value); }

    /// <summary>The radius axis the bars map through (Recharts' <c>radiusAxisId</c>).</summary>
    public string RadiusAxisId { get => GetValue(RadiusAxisIdProperty); set => SetValue(RadiusAxisIdProperty, value); }

    /// <summary>Defines <see cref="AngleAxisId"/>.</summary>
    public static readonly StyledProperty<string> AngleAxisIdProperty = AvaloniaProperty.Register<RadialBar, string>(nameof(AngleAxisId), "0");

    /// <summary>The angle axis the bars map through (Recharts' <c>angleAxisId</c>).</summary>
    public string AngleAxisId { get => GetValue(AngleAxisIdProperty); set => SetValue(AngleAxisIdProperty, value); }

    /// <summary>Defines <see cref="Name"/>.</summary>
    public static new readonly StyledProperty<string?> NameProperty = AvaloniaProperty.Register<RadialBar, string?>(nameof(Name));

    /// <summary>Defines <see cref="NameKey"/>.</summary>
    public static readonly StyledProperty<string> NameKeyProperty = AvaloniaProperty.Register<RadialBar, string>(nameof(NameKey), "name");

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<RadialBar, string?>(nameof(Fill));

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<RadialBar, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="Background"/>.</summary>
    public static readonly StyledProperty<string?> BackgroundProperty = AvaloniaProperty.Register<RadialBar, string?>(nameof(Background));

    /// <summary>Defines <see cref="BarSize"/>.</summary>
    public static readonly StyledProperty<double?> BarSizeProperty = AvaloniaProperty.Register<RadialBar, double?>(nameof(BarSize));

    /// <summary>Defines <see cref="MinPointSize"/>.</summary>
    public static readonly StyledProperty<double> MinPointSizeProperty = AvaloniaProperty.Register<RadialBar, double>(nameof(MinPointSize));

    /// <summary>Defines <see cref="CornerRadius"/>.</summary>
    public static readonly StyledProperty<string?> CornerRadiusProperty = AvaloniaProperty.Register<RadialBar, string?>(nameof(CornerRadius));

    /// <summary>Defines <see cref="Label"/>.</summary>
    public static readonly StyledProperty<LabelPosition?> LabelProperty = AvaloniaProperty.Register<RadialBar, LabelPosition?>(nameof(Label));

    /// <summary>Defines <see cref="LabelFill"/>.</summary>
    public static readonly StyledProperty<string?> LabelFillProperty = AvaloniaProperty.Register<RadialBar, string?>(nameof(LabelFill));

    /// <summary>Defines <see cref="Unit"/>.</summary>
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<RadialBar, string?>(nameof(Unit));

    /// <summary>Defines <see cref="MaxBarSize"/>.</summary>
    public static readonly StyledProperty<double?> MaxBarSizeProperty = AvaloniaProperty.Register<RadialBar, double?>(nameof(MaxBarSize));

    /// <summary>Defines <see cref="StackId"/>.</summary>
    public static readonly StyledProperty<string?> StackIdProperty = AvaloniaProperty.Register<RadialBar, string?>(nameof(StackId));

    /// <summary>Defines <see cref="ForceCornerRadius"/>.</summary>
    public static readonly StyledProperty<bool> ForceCornerRadiusProperty = AvaloniaProperty.Register<RadialBar, bool>(nameof(ForceCornerRadius));

    /// <summary>Defines <see cref="CornerIsExternal"/>.</summary>
    public static readonly StyledProperty<bool> CornerIsExternalProperty = AvaloniaProperty.Register<RadialBar, bool>(nameof(CornerIsExternal));

    /// <summary>Defines <see cref="BackgroundStroke"/>.</summary>
    public static readonly StyledProperty<string?> BackgroundStrokeProperty = AvaloniaProperty.Register<RadialBar, string?>(nameof(BackgroundStroke));

    /// <summary>Defines <see cref="BackgroundStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> BackgroundStrokeWidthProperty = AvaloniaProperty.Register<RadialBar, double?>(nameof(BackgroundStrokeWidth));

    /// <summary>Defines <see cref="BackgroundFillOpacity"/>.</summary>
    public static readonly StyledProperty<double?> BackgroundFillOpacityProperty = AvaloniaProperty.Register<RadialBar, double?>(nameof(BackgroundFillOpacity));
    /// <summary>The bars' largest width across the radius.</summary>
    public double? MaxBarSize { get => GetValue(MaxBarSizeProperty); set => SetValue(MaxBarSizeProperty, value); }

    /// <summary>The stack the bars belong to: bars of one stack share a band and pile up along the angle.</summary>
    public string? StackId { get => GetValue(StackIdProperty); set => SetValue(StackIdProperty, value); }

    /// <summary>Whether a bar too narrow for its corners is drawn as a rounded cap anyway (Recharts' <c>forceCornerRadius</c>).</summary>
    public bool ForceCornerRadius { get => GetValue(ForceCornerRadiusProperty); set => SetValue(ForceCornerRadiusProperty, value); }

    /// <summary>Whether the bars' corners round outwards (Recharts' <c>cornerIsExternal</c>).</summary>
    public bool CornerIsExternal { get => GetValue(CornerIsExternalProperty); set => SetValue(CornerIsExternalProperty, value); }

    /// <summary>The background's stroke (Recharts' <c>background</c> object).</summary>
    public string? BackgroundStroke { get => GetValue(BackgroundStrokeProperty); set => SetValue(BackgroundStrokeProperty, value); }

    /// <summary>The background's stroke width.</summary>
    public double? BackgroundStrokeWidth { get => GetValue(BackgroundStrokeWidthProperty); set => SetValue(BackgroundStrokeWidthProperty, value); }

    /// <summary>The background's fill opacity.</summary>
    public double? BackgroundFillOpacity { get => GetValue(BackgroundFillOpacityProperty); set => SetValue(BackgroundFillOpacityProperty, value); }
    /// <summary>The name in the tooltip; the data key by default.</summary>
    public new string? Name { get => GetValue(NameProperty); set => SetValue(NameProperty, value); }

    /// <summary>The data key of each bar's name in the legend.</summary>
    public string NameKey { get => GetValue(NameKeyProperty); set => SetValue(NameKeyProperty, value); }

    /// <summary>The bars' fill.</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The bars' stroke.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The fill of the full ring behind each bar (Recharts' <c>background</c>; <c>#eee</c> is Recharts' default).</summary>
    public string? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    /// <summary>The bars' width across the radius.</summary>
    public double? BarSize { get => GetValue(BarSizeProperty); set => SetValue(BarSizeProperty, value); }

    /// <summary>The smallest angle a bar is drawn with.</summary>
    public double MinPointSize { get => GetValue(MinPointSizeProperty); set => SetValue(MinPointSizeProperty, value); }

    /// <summary>The corner radius of the bars and their backgrounds.</summary>
    public string? CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>Shows each value as a label at this position (Recharts' <c>label</c>; <c>InsideStart</c> follows the arc).</summary>
    public LabelPosition? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>The labels' color; each bar's by default.</summary>
    public string? LabelFill { get => GetValue(LabelFillProperty); set => SetValue(LabelFillProperty, value); }

    /// <summary>A unit written after the values in the tooltip.</summary>
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    private (LabelSpec? First, System.Collections.Immutable.ImmutableArray<LabelSpec> Others) Lists(SpecBuilder builder) =>
        LabelList.Of(Cells, Label is { } position ? new LabelSpec { Position = position, Fill = LabelFill } : null, builder);

    private protected override void Apply(SpecBuilder builder, int order) => builder.Polar = builder.Polar with
    {
        RadialBars = builder.Polar.RadialBars.Add(new RadialBarSpec
        {
            ShapeContent = builder.Content(ShapeTemplate, Content.ContentSlots.Sector, this, nameof(ShapeTemplate)),
            ActiveShapeContent = builder.Content(ActiveShapeTemplate, Content.ContentSlots.Sector, this, nameof(ActiveShapeTemplate)),
            ActiveIndex = ActiveIndex,
            AngleAxisId = AngleAxisId,
            RadiusAxisId = RadiusAxisId,
            ClickCommand = Clickable.Key(this, order),
            DataKey = DataKey,
            Name = Name,
            NameKey = NameKey,
            FillKey = FillKey,
            OpacityKey = OpacityKey,
            Fill = Fill,
            Stroke = Stroke,
            Background = Background,
            BackgroundProps = BackgroundStroke is null && BackgroundStrokeWidth is null && BackgroundFillOpacity is null ? null : new CellSpec(Background, BackgroundStroke) { StrokeWidth = BackgroundStrokeWidth, FillOpacity = BackgroundFillOpacity },
            MaxBarSize = MaxBarSize,
            StackId = StackId,
            ForceCornerRadius = ForceCornerRadius,
            CornerIsExternal = CornerIsExternal,
            BarSize = BarSize,
            MinPointSize = MinPointSize,
            CornerRadius = CornerRadius,
            Labels = Lists(builder).First,
            LabelLists = Lists(builder).Others,
            Cells = CellSpecs(),
            Unit = Unit,
            LegendType = LegendType,
            Hide = Hide,
            TooltipNone = string.Equals(TooltipType, "none", StringComparison.OrdinalIgnoreCase),
            Animation = Animated.Spec(this),
            Order = order,
        }),
    };
}
