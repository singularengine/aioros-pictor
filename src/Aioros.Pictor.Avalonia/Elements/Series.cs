using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Aioros.Pictor;

/// <summary>What Recharts' graphical items share: a data key, colors, the axes they map through, labels and cells.</summary>
public abstract class Series : StyledElement, IChartElement, ISpecSource, IOwnItems, IClickSource, IAnimatedElement
{
    /// <summary>Raised on the desktop when the element's enter or update animation starts, before its begin delay (Recharts' <c>onAnimationStart</c>); a web chart, written without script, raises nothing.</summary>
    public event EventHandler? AnimationStarted;

    /// <summary>Raised on the desktop when the element's enter or update animation has moved to its end (Recharts' <c>onAnimationEnd</c>); a web chart raises nothing.</summary>
    public event EventHandler? AnimationEnded;

    Motion.Timing? IAnimatedElement.Timing => Hide ? null : Motion.Animations.Of(new Specs.SeriesSpec { Kind = Kind, Animation = Animated.Spec(this) });

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

    /// <summary>Defines <see cref="FillKey"/>.</summary>
    public static readonly StyledProperty<string?> FillKeyProperty = AvaloniaProperty.Register<Series, string?>(nameof(FillKey));

    /// <summary>Defines <see cref="OpacityKey"/>.</summary>
    public static readonly StyledProperty<string?> OpacityKeyProperty = AvaloniaProperty.Register<Series, string?>(nameof(OpacityKey));

    /// <summary>Defines <see cref="HrefKey"/>.</summary>
    public static readonly StyledProperty<string?> HrefKeyProperty = AvaloniaProperty.Register<Series, string?>(nameof(HrefKey));

    /// <summary>Defines <see cref="ClickCommand"/>.</summary>
    public static readonly StyledProperty<System.Windows.Input.ICommand?> ClickCommandProperty = Clickable.CommandProperty.AddOwner<Series>();

    /// <summary>Defines <see cref="IsAnimationActive"/>.</summary>
    public static readonly StyledProperty<bool> IsAnimationActiveProperty = Animated.IsActiveProperty.AddOwner<Series>();

    /// <summary>Defines <see cref="AnimationBegin"/>.</summary>
    public static readonly StyledProperty<double?> AnimationBeginProperty = Animated.BeginProperty.AddOwner<Series>();

    /// <summary>Defines <see cref="AnimationDuration"/>.</summary>
    public static readonly StyledProperty<double?> AnimationDurationProperty = Animated.DurationProperty.AddOwner<Series>();

    /// <summary>Defines <see cref="AnimationEasing"/>.</summary>
    public static readonly StyledProperty<AnimationEasing?> AnimationEasingProperty = Animated.EasingProperty.AddOwner<Series>();

    /// <summary>Defines <see cref="DataKey"/>.</summary>
    public static readonly StyledProperty<string> DataKeyProperty = AvaloniaProperty.Register<Series, string>(nameof(DataKey), string.Empty);

    /// <summary>Defines <see cref="Name"/>.</summary>
    public static new readonly StyledProperty<string?> NameProperty = AvaloniaProperty.Register<Series, string?>(nameof(Name));

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<object?> StrokeProperty = AvaloniaProperty.Register<Series, object?>(nameof(Stroke));

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double> StrokeWidthProperty = AvaloniaProperty.Register<Series, double>(nameof(StrokeWidth), 1);

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<object?> FillProperty = AvaloniaProperty.Register<Series, object?>(nameof(Fill));

    /// <summary>Defines <see cref="FillOpacity"/>.</summary>
    public static readonly StyledProperty<double?> FillOpacityProperty = AvaloniaProperty.Register<Series, double?>(nameof(FillOpacity));

    /// <summary>Defines <see cref="StrokeDashArray"/>.</summary>
    public static readonly StyledProperty<object?> StrokeDashArrayProperty = AvaloniaProperty.Register<Series, object?>(nameof(StrokeDashArray));

    /// <summary>Defines <see cref="XAxisId"/>.</summary>
    public static readonly StyledProperty<string> XAxisIdProperty = AvaloniaProperty.Register<Series, string>(nameof(XAxisId), "0");

    /// <summary>Defines <see cref="YAxisId"/>.</summary>
    public static readonly StyledProperty<string> YAxisIdProperty = AvaloniaProperty.Register<Series, string>(nameof(YAxisId), "0");

    /// <summary>Defines <see cref="Unit"/>.</summary>
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<Series, string?>(nameof(Unit));

    /// <summary>Defines <see cref="LegendType"/>.</summary>
    public static readonly StyledProperty<LegendIcon?> LegendTypeProperty = AvaloniaProperty.Register<Series, LegendIcon?>(nameof(LegendType));

    /// <summary>Defines <see cref="Hide"/>.</summary>
    public static readonly StyledProperty<bool> HideProperty = AvaloniaProperty.Register<Series, bool>(nameof(Hide));

    /// <summary>Defines <see cref="Label"/>.</summary>
    public static readonly StyledProperty<LabelPosition?> LabelProperty = AvaloniaProperty.Register<Series, LabelPosition?>(nameof(Label));

    /// <summary>Defines <see cref="TooltipType"/>.</summary>
    public static readonly StyledProperty<string?> TooltipTypeProperty = AvaloniaProperty.Register<Series, string?>(nameof(TooltipType));

    /// <summary>Defines <see cref="Data"/>.</summary>
    public static readonly StyledProperty<System.Collections.IEnumerable?> DataProperty = AvaloniaProperty.Register<Series, System.Collections.IEnumerable?>(nameof(Data));

    /// <summary>Defines <see cref="Parts"/>.</summary>
    public static readonly StyledProperty<AvaloniaList<ISeriesPart>> PartsProperty = AvaloniaProperty.Register<Series, AvaloniaList<ISeriesPart>>(nameof(Parts));

    private readonly ChangeRelay _relay = new();

    private protected Series()
    {
        var parts = new AvaloniaList<ISeriesPart>();
        _relay.Follow(parts, this, LogicalChildren);
        _relay.FollowClasses(this);
        SetValue(PartsProperty, parts);
    }

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>Whether it animates in when the chart appears or its data changes (Recharts' <c>isAnimationActive</c>, on by default as in the browser).</summary>
    public bool IsAnimationActive { get => GetValue(IsAnimationActiveProperty); set => SetValue(IsAnimationActiveProperty, value); }

    /// <summary>The delay before it animates in, in milliseconds; Recharts' default for the kind when null.</summary>
    public double? AnimationBegin { get => GetValue(AnimationBeginProperty); set => SetValue(AnimationBeginProperty, value); }

    /// <summary>How long it animates in, in milliseconds; Recharts' default for the kind when null.</summary>
    public double? AnimationDuration { get => GetValue(AnimationDurationProperty); set => SetValue(AnimationDurationProperty, value); }

    /// <summary>How it eases in; Recharts' default for the kind when null.</summary>
    public AnimationEasing? AnimationEasing { get => GetValue(AnimationEasingProperty); set => SetValue(AnimationEasingProperty, value); }

    /// <summary>The data key of the series' values.</summary>
    public string DataKey { get => GetValue(DataKeyProperty); set => SetValue(DataKeyProperty, value); }

    /// <summary>The name in the legend and the tooltip; the data key by default.</summary>
    public new string? Name { get => GetValue(NameProperty); set => SetValue(NameProperty, value); }

    /// <summary>
    /// The stroke: a color as Recharts takes it (<c>#8884d8</c>, <c>url(#id)</c>), or an Avalonia brush, a solid color or a
    /// <c>LinearGradientBrush</c> (written on the web as a <c>linearGradient</c> in the chart's <c>defs</c>).
    /// </summary>
    public object? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The stroke width.</summary>
    public double StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    /// <summary>
    /// The fill: a color as Recharts takes it (<c>#8884d8</c>, <c>url(#id)</c>), or an Avalonia brush, a solid color or a
    /// <c>LinearGradientBrush</c> (written on the web as a <c>linearGradient</c> in the chart's <c>defs</c>, Recharts'
    /// gradient fill).
    /// </summary>
    public object? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The fill opacity.</summary>
    public double? FillOpacity { get => GetValue(FillOpacityProperty); set => SetValue(FillOpacityProperty, value); }

    /// <summary>The stroke's dash pattern (<c>5 5</c>).</summary>
    public object? StrokeDashArray { get => GetValue(StrokeDashArrayProperty); set => SetValue(StrokeDashArrayProperty, value); }

    /// <summary>The x-axis the series maps through.</summary>
    public string XAxisId { get => GetValue(XAxisIdProperty); set => SetValue(XAxisIdProperty, value); }

    /// <summary>The y-axis the series maps through.</summary>
    public string YAxisId { get => GetValue(YAxisIdProperty); set => SetValue(YAxisIdProperty, value); }

    /// <summary>A unit written after the values in the tooltip.</summary>
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    /// <summary>The legend icon.</summary>
    public LegendIcon? LegendType { get => GetValue(LegendTypeProperty); set => SetValue(LegendTypeProperty, value); }

    /// <summary>Whether the series is hidden.</summary>
    public bool Hide { get => GetValue(HideProperty); set => SetValue(HideProperty, value); }

    /// <summary>Shows each value as a label at this position (Recharts' <c>label</c>).</summary>
    public LabelPosition? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary><c>none</c> to leave the series' line out of the tooltip (Recharts' <c>tooltipType</c>).</summary>
    public string? TooltipType { get => GetValue(TooltipTypeProperty); set => SetValue(TooltipTypeProperty, value); }

    /// <summary>The series' own items (Recharts' <c>data</c> on a series); the chart's when null.</summary>
    public System.Collections.IEnumerable? Data { get => GetValue(DataProperty); set => SetValue(DataProperty, value); }

    /// <summary>The series' label lists, cells and error bars.</summary>
    [Content]
    public AvaloniaList<ISeriesPart> Parts => GetValue(PartsProperty);

    private protected abstract SeriesKind Kind { get; }

    /// <summary>The command a click on one of its items runs, with the item's index in its data as the parameter (Recharts' <c>onClick</c>): a button over the item on the web, the pointer on the desktop.</summary>
    public string? HrefKey { get => GetValue(HrefKeyProperty); set => SetValue(HrefKeyProperty, value); }

    /// <summary>The item's member holding a bar's or a scatter point's own fill, as Recharts reads an item's <c>fill</c>; a <see cref="Cell"/> wins over it. A line's and an area's dots take none, as Recharts' never read it.</summary>
    public string? FillKey { get => GetValue(FillKeyProperty); set => SetValue(FillKeyProperty, value); }

    /// <summary>The item's member holding a bar's or a scatter point's own opacity.</summary>
    public string? OpacityKey { get => GetValue(OpacityKeyProperty); set => SetValue(OpacityKeyProperty, value); }

    /// <inheritdoc cref="IClickSource.CommandProperty" />
    public System.Windows.Input.ICommand? ClickCommand { get => GetValue(ClickCommandProperty); set => SetValue(ClickCommandProperty, value); }

    AvaloniaProperty IClickSource.CommandProperty => ClickCommandProperty;

    System.Collections.IEnumerable? IOwnItems.OwnItems => Data;

    void ISpecSource.Apply(SpecBuilder builder, int order)
    {
        var (labels, others) = LabelList.Of(Parts, Label is { } position ? new LabelSpec { Position = position } : null, builder);
        if (Data is { } data)
        {
            builder.SeriesData[builder.Series.Count] = data;
        }

        builder.Series.Add(Templates(builder, Complete(new SeriesSpec
        {
            ClassName = ChangeRelay.ClassName(this),
            Kind = Kind,
            DataKey = DataKey,
            Name = Name,
            Stroke = builder.Paint(Stroke),
            StrokeWidth = StrokeWidth,
            Fill = builder.Paint(Fill),
            FillOpacity = FillOpacity,
            StrokeDashArray = SpecText.Dashes(StrokeDashArray),
            XAxisId = XAxisId,
            YAxisId = YAxisId,
            Unit = Unit,
            LegendType = LegendType,
            Hide = Hide,
            TooltipNone = string.Equals(TooltipType, "none", StringComparison.OrdinalIgnoreCase),
            Animation = Animated.Spec(this),
            Labels = labels,
            LabelLists = others,
            Cells = [.. Parts.OfType<Cell>().Select(cell => cell.Spec())],
            ErrorBars = [.. Parts.OfType<ErrorBar>().Select(bar => bar.Spec())],
            Order = order,
            ClickCommand = Clickable.Key(this, order),
            HrefKey = HrefKey,
            FillKey = Kind is SeriesKind.Bar or SeriesKind.Scatter ? FillKey : null,
            OpacityKey = Kind is SeriesKind.Bar or SeriesKind.Scatter ? OpacityKey : null,
        })));
    }

    private protected abstract SeriesSpec Complete(SeriesSpec spec);

    /// <summary>The series' templates of custom content, compiled.</summary>
    private protected virtual SeriesSpec Templates(SpecBuilder builder, SeriesSpec spec) => spec;

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}

/// <summary>What <see cref="Line"/> and <see cref="Area"/> share: a curve through the points, dots and the active dot.</summary>
public abstract class PointSeries : Series
{
    /// <summary>Defines <see cref="Type"/>.</summary>
    public static readonly StyledProperty<CurveType> TypeProperty = AvaloniaProperty.Register<PointSeries, CurveType>(nameof(Type));

    /// <summary>Defines <see cref="Dot"/>.</summary>
    public static readonly StyledProperty<bool?> DotProperty = AvaloniaProperty.Register<PointSeries, bool?>(nameof(Dot));

    /// <summary>Defines <see cref="ActiveDot"/>.</summary>
    public static readonly StyledProperty<double> ActiveDotProperty = AvaloniaProperty.Register<PointSeries, double>(nameof(ActiveDot), 4);

    /// <summary>Defines <see cref="StackId"/>.</summary>
    public static readonly StyledProperty<string?> StackIdProperty = AvaloniaProperty.Register<PointSeries, string?>(nameof(StackId));

    /// <summary>Defines <see cref="ActiveDotFill"/>.</summary>
    public static readonly StyledProperty<string?> ActiveDotFillProperty = AvaloniaProperty.Register<PointSeries, string?>(nameof(ActiveDotFill));

    /// <summary>Defines <see cref="ActiveDotStroke"/>.</summary>
    public static readonly StyledProperty<string?> ActiveDotStrokeProperty = AvaloniaProperty.Register<PointSeries, string?>(nameof(ActiveDotStroke));

    /// <summary>Defines <see cref="ActiveDotStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> ActiveDotStrokeWidthProperty = AvaloniaProperty.Register<PointSeries, double?>(nameof(ActiveDotStrokeWidth));

    /// <summary>Defines <see cref="ConnectNulls"/>.</summary>
    public static readonly StyledProperty<bool> ConnectNullsProperty = AvaloniaProperty.Register<PointSeries, bool>(nameof(ConnectNulls));

    /// <summary>Defines <see cref="DotRadius"/>.</summary>
    public static readonly StyledProperty<double?> DotRadiusProperty = AvaloniaProperty.Register<PointSeries, double?>(nameof(DotRadius));

    /// <summary>Defines <see cref="DotFill"/>.</summary>
    public static readonly StyledProperty<string?> DotFillProperty = AvaloniaProperty.Register<PointSeries, string?>(nameof(DotFill));

    /// <summary>Defines <see cref="DotStroke"/>.</summary>
    public static readonly StyledProperty<string?> DotStrokeProperty = AvaloniaProperty.Register<PointSeries, string?>(nameof(DotStroke));

    /// <summary>Defines <see cref="DotStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> DotStrokeWidthProperty = AvaloniaProperty.Register<PointSeries, double?>(nameof(DotStrokeWidth));

    /// <summary>Defines <see cref="DotFillOpacity"/>.</summary>
    public static readonly StyledProperty<double?> DotFillOpacityProperty = AvaloniaProperty.Register<PointSeries, double?>(nameof(DotFillOpacity));

    /// <summary>Defines <see cref="DotStrokeDashArray"/>.</summary>
    public static readonly StyledProperty<object?> DotStrokeDashArrayProperty = AvaloniaProperty.Register<PointSeries, object?>(nameof(DotStrokeDashArray));

    /// <summary>Defines <see cref="ClipDot"/>.</summary>
    public static readonly StyledProperty<bool> ClipDotProperty = AvaloniaProperty.Register<PointSeries, bool>(nameof(ClipDot), true);

    /// <summary>Defines <see cref="DotTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> DotTemplateProperty = AvaloniaProperty.Register<PointSeries, IDataTemplate?>(nameof(DotTemplate));

    /// <summary>Defines <see cref="ActiveDotTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> ActiveDotTemplateProperty = AvaloniaProperty.Register<PointSeries, IDataTemplate?>(nameof(ActiveDotTemplate));

    private protected PointSeries()
    {
    }

    /// <summary>The dots' radius (Recharts' <c>dot={{r}}</c>); 3 by default.</summary>
    public double? DotRadius { get => GetValue(DotRadiusProperty); set => SetValue(DotRadiusProperty, value); }

    /// <summary>The dots' fill (Recharts' <c>dot={{fill}}</c>).</summary>
    public string? DotFill { get => GetValue(DotFillProperty); set => SetValue(DotFillProperty, value); }

    /// <summary>The dots' stroke.</summary>
    public string? DotStroke { get => GetValue(DotStrokeProperty); set => SetValue(DotStrokeProperty, value); }

    /// <summary>The dots' stroke width.</summary>
    public double? DotStrokeWidth { get => GetValue(DotStrokeWidthProperty); set => SetValue(DotStrokeWidthProperty, value); }

    /// <summary>The dots' fill opacity.</summary>
    public double? DotFillOpacity { get => GetValue(DotFillOpacityProperty); set => SetValue(DotFillOpacityProperty, value); }

    /// <summary>The dots' dash pattern.</summary>
    public object? DotStrokeDashArray { get => GetValue(DotStrokeDashArrayProperty); set => SetValue(DotStrokeDashArrayProperty, value); }

    /// <summary>The curve between the points (Recharts' <c>type</c>).</summary>
    public CurveType Type { get => GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>Whether a dot marks every point; on for lines, off for areas by default.</summary>
    public bool? Dot { get => GetValue(DotProperty); set => SetValue(DotProperty, value); }

    /// <summary>The radius of the hovered point's dot; 0 hides it.</summary>
    public double ActiveDot { get => GetValue(ActiveDotProperty); set => SetValue(ActiveDotProperty, value); }

    /// <summary>The stack the series belongs to.</summary>
    public string? StackId { get => GetValue(StackIdProperty); set => SetValue(StackIdProperty, value); }

    /// <summary>The hovered point's dot fill (Recharts' <c>activeDot.fill</c>); the series' color by default.</summary>
    public string? ActiveDotFill { get => GetValue(ActiveDotFillProperty); set => SetValue(ActiveDotFillProperty, value); }

    /// <summary>The hovered point's dot stroke (Recharts' <c>activeDot.stroke</c>); white by default.</summary>
    public string? ActiveDotStroke { get => GetValue(ActiveDotStrokeProperty); set => SetValue(ActiveDotStrokeProperty, value); }

    /// <summary>The hovered point's dot stroke width (Recharts' <c>activeDot.strokeWidth</c>); 2 by default.</summary>
    public double? ActiveDotStrokeWidth { get => GetValue(ActiveDotStrokeWidthProperty); set => SetValue(ActiveDotStrokeWidthProperty, value); }

    /// <summary>Whether the curve connects across missing values.</summary>
    public bool ConnectNulls { get => GetValue(ConnectNullsProperty); set => SetValue(ConnectNullsProperty, value); }

    /// <summary>
    /// The custom content drawn at each point (Recharts' <c>dot</c> element): a template whose data context is the point's
    /// <see cref="DotContext"/>; setting it shows the dots.
    /// </summary>
    public IDataTemplate? DotTemplate { get => GetValue(DotTemplateProperty); set => SetValue(DotTemplateProperty, value); }

    /// <summary>The custom content drawn at the hovered point (Recharts' <c>activeDot</c> element), with its <see cref="DotContext"/>.</summary>
    public IDataTemplate? ActiveDotTemplate { get => GetValue(ActiveDotTemplateProperty); set => SetValue(ActiveDotTemplateProperty, value); }

    private ActiveStyle? DotProps() => DotRadius is null && DotFill is null && DotStroke is null && DotStrokeWidth is null && DotFillOpacity is null && DotStrokeDashArray is null
        ? null
        : new ActiveStyle { R = DotRadius, Fill = DotFill, Stroke = DotStroke, StrokeWidth = DotStrokeWidth, FillOpacity = DotFillOpacity, StrokeDashArray = SpecText.Dashes(DotStrokeDashArray) };

    /// <summary>
    /// On an axis that allows data overflow, whether the dots are clipped with the curve (Recharts' <c>dot.clipDot</c>,
    /// on by default); off, they are clipped to the plot grown by a dot, so a dot on the plot's edge is drawn whole.
    /// </summary>
    public bool ClipDot { get => GetValue(ClipDotProperty); set => SetValue(ClipDotProperty, value); }

    private protected override SeriesSpec Complete(SeriesSpec spec) => spec with
    {
        Presentation = Presentation.Of(this),
        Curve = Type,
        Dot = Dot ?? (DotProps() is not null || DotTemplate is not null || Kind == SeriesKind.Line),
        DotProps = DotProps(),
        ActiveDot = ActiveDot > 0,
        ActiveDotRadius = ActiveDot,
        ActiveDotStyle = ActiveDotFill is null && ActiveDotStroke is null && ActiveDotStrokeWidth is null ? null : new ActiveStyle(ActiveDotFill, ActiveDotStroke, ActiveDotStrokeWidth),
        StackId = StackId,
        ConnectNulls = ConnectNulls,
        ClipDot = ClipDot,
    };

    private protected override SeriesSpec Templates(SpecBuilder builder, SeriesSpec spec) => spec with
    {
        DotContent = builder.Content(DotTemplate, Content.ContentSlots.Dot, this, nameof(DotTemplate)),
        ActiveDotContent = builder.Content(ActiveDotTemplate, Content.ContentSlots.Dot, this, nameof(ActiveDotTemplate)),
    };
}

/// <summary>Recharts' <c>Line</c>.</summary>
public sealed class Line : PointSeries
{
    /// <summary>Defines <see cref="AnimateNewValues"/>.</summary>
    public static readonly StyledProperty<bool> AnimateNewValuesProperty = AvaloniaProperty.Register<Line, bool>(nameof(AnimateNewValues), true);

    /// <summary>Whether points with no previous point come from twice the plot's width and half its height in an update animation (Recharts' <c>animateNewValues</c>, on by default); off, they stand at their place.</summary>
    public bool AnimateNewValues { get => GetValue(AnimateNewValuesProperty); set => SetValue(AnimateNewValuesProperty, value); }

    private protected override SeriesKind Kind => SeriesKind.Line;

    private protected override SeriesSpec Complete(SeriesSpec spec) => base.Complete(spec) with { AnimateNewValues = AnimateNewValues };
}

/// <summary>Recharts' <c>Area</c>; a data key whose values are pairs <c>[low, high]</c> (a list, an array or a tuple) draws a range area.</summary>
public sealed class Area : PointSeries
{
    /// <summary>Defines <see cref="BaseValue"/>.</summary>
    public static readonly StyledProperty<string?> BaseValueProperty = AvaloniaProperty.Register<Area, string?>(nameof(BaseValue));

    /// <summary>
    /// The baseline when the values are single numbers (Recharts' <c>baseValue</c>): a number, <c>dataMin</c> or
    /// <c>dataMax</c>; the chart's <see cref="CartesianChart.BaseValue"/> when null.
    /// </summary>
    public string? BaseValue { get => GetValue(BaseValueProperty); set => SetValue(BaseValueProperty, value); }

    private protected override SeriesKind Kind => SeriesKind.Area;

    private protected override SeriesSpec Complete(SeriesSpec spec) => base.Complete(spec) with { BaseValue = BaseValue };
}

/// <summary>Recharts' <c>Scatter</c>: a symbol at every point, in a scatter chart or a composed chart.</summary>
public sealed class Scatter : Series
{
    /// <summary>Defines <see cref="Shape"/>.</summary>
    public static readonly StyledProperty<ScatterShape> ShapeProperty = AvaloniaProperty.Register<Scatter, ScatterShape>(nameof(Shape));

    /// <summary>Defines <see cref="Line"/>.</summary>
    public static readonly StyledProperty<bool> LineProperty = AvaloniaProperty.Register<Scatter, bool>(nameof(Line));

    /// <summary>Defines <see cref="LineJointType"/>.</summary>
    public static readonly StyledProperty<CurveType> LineJointTypeProperty = AvaloniaProperty.Register<Scatter, CurveType>(nameof(LineJointType));

    /// <summary>Defines <see cref="ZAxisId"/>.</summary>
    public static readonly StyledProperty<string> ZAxisIdProperty = AvaloniaProperty.Register<Scatter, string>(nameof(ZAxisId), "0");

    /// <summary>Defines <see cref="LineType"/>.</summary>
    public static readonly StyledProperty<ScatterLineType> LineTypeProperty = AvaloniaProperty.Register<Scatter, ScatterLineType>(nameof(LineType));

    /// <summary>Defines <see cref="LineStroke"/>.</summary>
    public static readonly StyledProperty<string?> LineStrokeProperty = AvaloniaProperty.Register<Scatter, string?>(nameof(LineStroke));

    /// <summary>Defines <see cref="LineStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> LineStrokeWidthProperty = AvaloniaProperty.Register<Scatter, double?>(nameof(LineStrokeWidth));

    /// <summary>Defines <see cref="LineStrokeDashArray"/>.</summary>
    public static readonly StyledProperty<object?> LineStrokeDashArrayProperty = AvaloniaProperty.Register<Scatter, object?>(nameof(LineStrokeDashArray));

    /// <summary>Defines <see cref="ShapeTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> ShapeTemplateProperty = AvaloniaProperty.Register<Scatter, IDataTemplate?>(nameof(ShapeTemplate));

    /// <summary>Defines <see cref="ActiveShapeTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> ActiveShapeTemplateProperty = AvaloniaProperty.Register<Scatter, IDataTemplate?>(nameof(ActiveShapeTemplate));

    /// <summary>Defines <see cref="ActiveIndex"/>.</summary>
    public static readonly StyledProperty<int?> ActiveIndexProperty = AvaloniaProperty.Register<Scatter, int?>(nameof(ActiveIndex));

    /// <summary>The point drawn active whatever the pointer does (Recharts' <c>activeIndex</c>): its <see cref="ActiveShapeTemplate"/>, or its default symbol in Recharts' active layer; hovering another point leaves it active.</summary>
    public int? ActiveIndex { get => GetValue(ActiveIndexProperty); set => SetValue(ActiveIndexProperty, value); }

    /// <summary>The symbol at each point (Recharts' <c>shape</c>).</summary>
    public ScatterShape Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }

    /// <summary>Whether a line joins the points (Recharts' <c>line</c>).</summary>
    public bool Line { get => GetValue(LineProperty); set => SetValue(LineProperty, value); }

    /// <summary>The curve of the joint line (Recharts' <c>lineJointType</c>).</summary>
    public CurveType LineJointType { get => GetValue(LineJointTypeProperty); set => SetValue(LineJointTypeProperty, value); }

    /// <summary>The z-axis the symbols' areas map through.</summary>
    public string ZAxisId { get => GetValue(ZAxisIdProperty); set => SetValue(ZAxisIdProperty, value); }

    /// <summary>Whether the line joins the points (<c>Joint</c>) or is their least-squares fit (<c>Fitting</c>), Recharts' <c>lineType</c>.</summary>
    public ScatterLineType LineType { get => GetValue(LineTypeProperty); set => SetValue(LineTypeProperty, value); }

    /// <summary>The line's stroke (Recharts' <c>line</c> props); the scatter's fill by default.</summary>
    public string? LineStroke { get => GetValue(LineStrokeProperty); set => SetValue(LineStrokeProperty, value); }

    /// <summary>The line's stroke width.</summary>
    public double? LineStrokeWidth { get => GetValue(LineStrokeWidthProperty); set => SetValue(LineStrokeWidthProperty, value); }

    /// <summary>The line's dash pattern.</summary>
    public object? LineStrokeDashArray { get => GetValue(LineStrokeDashArrayProperty); set => SetValue(LineStrokeDashArrayProperty, value); }

    /// <summary>
    /// The custom content drawn in place of each symbol (Recharts' <c>shape</c> element): a template whose data context is
    /// the point's <see cref="ScatterShapeContext"/>.
    /// </summary>
    public IDataTemplate? ShapeTemplate { get => GetValue(ShapeTemplateProperty); set => SetValue(ShapeTemplateProperty, value); }

    /// <summary>The custom content drawn in place of the hovered symbol (Recharts' <c>activeShape</c> element), with its <see cref="ScatterShapeContext"/>.</summary>
    public IDataTemplate? ActiveShapeTemplate { get => GetValue(ActiveShapeTemplateProperty); set => SetValue(ActiveShapeTemplateProperty, value); }

    private protected override SeriesKind Kind => SeriesKind.Scatter;

    private protected override SeriesSpec Templates(SpecBuilder builder, SeriesSpec spec) => spec with
    {
        ShapeContent = builder.Content(ShapeTemplate, Content.ContentSlots.Scatter, this, nameof(ShapeTemplate)),
        ActiveShapeContent = builder.Content(ActiveShapeTemplate, Content.ContentSlots.Scatter, this, nameof(ActiveShapeTemplate)),
    };

    private protected override SeriesSpec Complete(SeriesSpec spec) => spec with
    {
        Shape = Shape,
        JointLine = Line,
        LineJointType = LineJointType,
        LineType = LineType,
        LineStroke = LineStroke,
        LineStrokeWidth = LineStrokeWidth,
        LineStrokeDashArray = SpecText.Dashes(LineStrokeDashArray),
        ZAxisId = ZAxisId,
        ActiveIndex = ActiveIndex,
    };
}

/// <summary>Recharts' <c>Bar</c>.</summary>
public sealed class Bar : Series
{
    /// <summary>Defines <see cref="StackId"/>.</summary>
    public static readonly StyledProperty<string?> StackIdProperty = AvaloniaProperty.Register<Bar, string?>(nameof(StackId));

    /// <summary>Defines <see cref="BarSize"/>.</summary>
    public static readonly StyledProperty<string?> BarSizeProperty = AvaloniaProperty.Register<Bar, string?>(nameof(BarSize));

    /// <summary>Defines <see cref="MaxBarSize"/>.</summary>
    public static readonly StyledProperty<double?> MaxBarSizeProperty = AvaloniaProperty.Register<Bar, double?>(nameof(MaxBarSize));

    /// <summary>Defines <see cref="Radius"/>.</summary>
    public static readonly StyledProperty<object?> RadiusProperty = AvaloniaProperty.Register<Bar, object?>(nameof(Radius));

    /// <summary>Defines <see cref="Background"/>.</summary>
    public static readonly StyledProperty<string?> BackgroundProperty = AvaloniaProperty.Register<Bar, string?>(nameof(Background));

    /// <summary>Defines <see cref="MinPointSize"/>.</summary>
    public static readonly StyledProperty<double> MinPointSizeProperty = AvaloniaProperty.Register<Bar, double>(nameof(MinPointSize));

    /// <summary>Defines <see cref="BackgroundStroke"/>.</summary>
    public static readonly StyledProperty<string?> BackgroundStrokeProperty = AvaloniaProperty.Register<Bar, string?>(nameof(BackgroundStroke));

    /// <summary>Defines <see cref="BackgroundStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> BackgroundStrokeWidthProperty = AvaloniaProperty.Register<Bar, double?>(nameof(BackgroundStrokeWidth));

    /// <summary>Defines <see cref="BackgroundFillOpacity"/>.</summary>
    public static readonly StyledProperty<double?> BackgroundFillOpacityProperty = AvaloniaProperty.Register<Bar, double?>(nameof(BackgroundFillOpacity));

    /// <summary>Defines <see cref="BackgroundRadius"/>.</summary>
    public static readonly StyledProperty<object?> BackgroundRadiusProperty = AvaloniaProperty.Register<Bar, object?>(nameof(BackgroundRadius));

    /// <summary>Defines <see cref="ActiveFill"/>.</summary>
    public static readonly StyledProperty<string?> ActiveFillProperty = AvaloniaProperty.Register<Bar, string?>(nameof(ActiveFill));

    /// <summary>Defines <see cref="ShapeTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> ShapeTemplateProperty = AvaloniaProperty.Register<Bar, IDataTemplate?>(nameof(ShapeTemplate));

    /// <summary>Defines <see cref="ActiveShapeTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> ActiveShapeTemplateProperty = AvaloniaProperty.Register<Bar, IDataTemplate?>(nameof(ActiveShapeTemplate));

    /// <summary>Defines <see cref="ActiveIndex"/>.</summary>
    public static readonly StyledProperty<int?> ActiveIndexProperty = AvaloniaProperty.Register<Bar, int?>(nameof(ActiveIndex));

    /// <summary>
    /// The bar drawn active whatever the pointer does (Recharts' <c>activeIndex</c>, an index among the displayed items):
    /// in Recharts' active layer, with its <see cref="ActiveShapeTemplate"/> or its active props, its background too;
    /// hovering another category leaves it active.
    /// </summary>
    public int? ActiveIndex { get => GetValue(ActiveIndexProperty); set => SetValue(ActiveIndexProperty, value); }

    /// <summary>Defines <see cref="ActiveStroke"/>.</summary>
    public static readonly StyledProperty<string?> ActiveStrokeProperty = AvaloniaProperty.Register<Bar, string?>(nameof(ActiveStroke));

    /// <summary>Defines <see cref="ActiveStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> ActiveStrokeWidthProperty = AvaloniaProperty.Register<Bar, double?>(nameof(ActiveStrokeWidth));

    /// <summary>The stack the bar belongs to.</summary>
    public string? StackId { get => GetValue(StackIdProperty); set => SetValue(StackIdProperty, value); }

    /// <summary>The bars' fixed width: a number, or a percentage of the category axis' length (<c>20%</c>).</summary>
    public string? BarSize { get => GetValue(BarSizeProperty); set => SetValue(BarSizeProperty, value); }

    /// <summary>The bars' largest width.</summary>
    public double? MaxBarSize { get => GetValue(MaxBarSizeProperty); set => SetValue(MaxBarSizeProperty, value); }

    /// <summary>The corner radius: one value, or four (top left, top right, bottom right, bottom left).</summary>
    public object? Radius { get => GetValue(RadiusProperty); set => SetValue(RadiusProperty, value); }

    /// <summary>A fill drawn behind each bar over the plot's height.</summary>
    public string? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    /// <summary>The background rectangles' stroke (Recharts' <c>background</c> object).</summary>
    public string? BackgroundStroke { get => GetValue(BackgroundStrokeProperty); set => SetValue(BackgroundStrokeProperty, value); }

    /// <summary>The background rectangles' stroke width.</summary>
    public double? BackgroundStrokeWidth { get => GetValue(BackgroundStrokeWidthProperty); set => SetValue(BackgroundStrokeWidthProperty, value); }

    /// <summary>The background rectangles' fill opacity.</summary>
    public double? BackgroundFillOpacity { get => GetValue(BackgroundFillOpacityProperty); set => SetValue(BackgroundFillOpacityProperty, value); }

    /// <summary>The background rectangles' corner radius: one value or four; the bar's own radius does not reach them.</summary>
    public object? BackgroundRadius { get => GetValue(BackgroundRadiusProperty); set => SetValue(BackgroundRadiusProperty, value); }

    /// <summary>The smallest height a bar is drawn with.</summary>
    public double MinPointSize { get => GetValue(MinPointSizeProperty); set => SetValue(MinPointSizeProperty, value); }

    /// <summary>The hovered bar's fill (Recharts' <c>activeBar.fill</c>).</summary>
    public string? ActiveFill { get => GetValue(ActiveFillProperty); set => SetValue(ActiveFillProperty, value); }

    /// <summary>The hovered bar's stroke (Recharts' <c>activeBar.stroke</c>).</summary>
    public string? ActiveStroke { get => GetValue(ActiveStrokeProperty); set => SetValue(ActiveStrokeProperty, value); }

    /// <summary>The hovered bar's stroke width (Recharts' <c>activeBar.strokeWidth</c>).</summary>
    public double? ActiveStrokeWidth { get => GetValue(ActiveStrokeWidthProperty); set => SetValue(ActiveStrokeWidthProperty, value); }

    /// <summary>
    /// The custom content drawn in place of each bar (Recharts' <c>shape</c> element): a template whose data context is
    /// the bar's <see cref="BarShapeContext"/>, made of Avalonia's shapes and Pictor's template shapes. The web writes
    /// it as SVG with holes compiled once per plan; a template it cannot compile is drawn by Avalonia.
    /// </summary>
    public IDataTemplate? ShapeTemplate { get => GetValue(ShapeTemplateProperty); set => SetValue(ShapeTemplateProperty, value); }

    /// <summary>The custom content drawn in place of the hovered bar (Recharts' <c>activeBar</c> element), with its <see cref="BarShapeContext"/>.</summary>
    public IDataTemplate? ActiveShapeTemplate { get => GetValue(ActiveShapeTemplateProperty); set => SetValue(ActiveShapeTemplateProperty, value); }

    private protected override SeriesKind Kind => SeriesKind.Bar;

    private protected override SeriesSpec Complete(SeriesSpec spec) => spec with
    {
        StackId = StackId,
        BarSize = BarSize,
        MaxBarSize = MaxBarSize,
        Radius = SpecText.Numbers(Radius),
        Background = Background,
        BackgroundProps = BackgroundStroke is null && BackgroundStrokeWidth is null && BackgroundFillOpacity is null ? null : new CellSpec(null, BackgroundStroke) { StrokeWidth = BackgroundStrokeWidth, FillOpacity = BackgroundFillOpacity },
        BackgroundRadius = SpecText.Numbers(BackgroundRadius),
        MinPointSize = MinPointSize,
        ActiveDot = false,
        ActiveBar = ActiveFill is null && ActiveStroke is null && ActiveStrokeWidth is null ? null : new ActiveStyle(ActiveFill, ActiveStroke, ActiveStrokeWidth),
        ActiveIndex = ActiveIndex,
    };

    private protected override SeriesSpec Templates(SpecBuilder builder, SeriesSpec spec) => spec with
    {
        ShapeContent = builder.Content(ShapeTemplate, Content.ContentSlots.Bar, this, nameof(ShapeTemplate)),
        ActiveShapeContent = builder.Content(ActiveShapeTemplate, Content.ContentSlots.Bar, this, nameof(ActiveShapeTemplate)),
    };
}
