using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Aioros.Pictor;

/// <summary>What <see cref="XAxis"/> and <see cref="YAxis"/> share: Recharts' <c>CartesianAxis</c> props.</summary>
public abstract class Axis : StyledElement, IChartElement, ISpecSource, IClickSource
{
    /// <summary>Defines <see cref="ClickCommand"/>.</summary>
    public static readonly StyledProperty<System.Windows.Input.ICommand?> ClickCommandProperty = Clickable.CommandProperty.AddOwner<Axis>();

    /// <summary>
    /// The command a tick runs when clicked (Recharts' <c>onClick</c> on an axis, which a tick's group receives with the
    /// tick's payload), with the tick's value as its parameter: a category's text, or a number as JavaScript writes it.
    /// On the web each visible tick carries a button over its label and tick line; on the desktop the pointer runs it.
    /// </summary>
    public System.Windows.Input.ICommand? ClickCommand { get => GetValue(ClickCommandProperty); set => SetValue(ClickCommandProperty, value); }

    AvaloniaProperty IClickSource.CommandProperty => ClickCommandProperty;

    /// <summary>Defines <see cref="DataKey"/>.</summary>
    public static readonly StyledProperty<string?> DataKeyProperty = AvaloniaProperty.Register<Axis, string?>(nameof(DataKey));

    /// <summary>Defines <see cref="Type"/>.</summary>
    public static readonly StyledProperty<AxisType?> TypeProperty = AvaloniaProperty.Register<Axis, AxisType?>(nameof(Type));

    /// <summary>Defines <see cref="Orientation"/>.</summary>
    public static readonly StyledProperty<AxisOrientation?> OrientationProperty = AvaloniaProperty.Register<Axis, AxisOrientation?>(nameof(Orientation));

    /// <summary>Defines <see cref="Hide"/>.</summary>
    public static readonly StyledProperty<bool> HideProperty = AvaloniaProperty.Register<Axis, bool>(nameof(Hide));

    /// <summary>Defines <see cref="TickCount"/>.</summary>
    public static readonly StyledProperty<int> TickCountProperty = AvaloniaProperty.Register<Axis, int>(nameof(TickCount), 5);

    /// <summary>Defines <see cref="AllowDecimals"/>.</summary>
    public static readonly StyledProperty<bool> AllowDecimalsProperty = AvaloniaProperty.Register<Axis, bool>(nameof(AllowDecimals), true);

    /// <summary>Defines <see cref="Domain"/>.</summary>
    public static readonly StyledProperty<string?> DomainProperty = AvaloniaProperty.Register<Axis, string?>(nameof(Domain));

    /// <summary>Defines <see cref="AllowDataOverflow"/>.</summary>
    public static readonly StyledProperty<bool> AllowDataOverflowProperty = AvaloniaProperty.Register<Axis, bool>(nameof(AllowDataOverflow));

    /// <summary>Defines <see cref="Padding"/>.</summary>
    public static readonly StyledProperty<Thickness> PaddingProperty = AvaloniaProperty.Register<Axis, Thickness>(nameof(Padding));

    /// <summary>Defines <see cref="Reversed"/>.</summary>
    public static readonly StyledProperty<bool> ReversedProperty = AvaloniaProperty.Register<Axis, bool>(nameof(Reversed));

    /// <summary>Defines <see cref="Mirror"/>.</summary>
    public static readonly StyledProperty<bool> MirrorProperty = AvaloniaProperty.Register<Axis, bool>(nameof(Mirror));

    /// <summary>Defines <see cref="Interval"/>.</summary>
    public static readonly StyledProperty<string?> IntervalProperty = AvaloniaProperty.Register<Axis, string?>(nameof(Interval));

    /// <summary>Defines <see cref="MinTickGap"/>.</summary>
    public static readonly StyledProperty<double> MinTickGapProperty = AvaloniaProperty.Register<Axis, double>(nameof(MinTickGap), 5);

    /// <summary>Defines <see cref="TickSize"/>.</summary>
    public static readonly StyledProperty<double> TickSizeProperty = AvaloniaProperty.Register<Axis, double>(nameof(TickSize), 6);

    /// <summary>Defines <see cref="TickMargin"/>.</summary>
    public static readonly StyledProperty<double> TickMarginProperty = AvaloniaProperty.Register<Axis, double>(nameof(TickMargin), 2);

    /// <summary>Defines <see cref="TickLine"/>.</summary>
    public static readonly StyledProperty<bool> TickLineProperty = AvaloniaProperty.Register<Axis, bool>(nameof(TickLine), true);

    /// <summary>Defines <see cref="AxisLine"/>.</summary>
    public static readonly StyledProperty<bool> AxisLineProperty = AvaloniaProperty.Register<Axis, bool>(nameof(AxisLine), true);

    /// <summary>Defines <see cref="Tick"/>.</summary>
    public static readonly StyledProperty<bool> TickProperty = AvaloniaProperty.Register<Axis, bool>(nameof(Tick), true);

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<Axis, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="TickFormat"/>.</summary>
    public static readonly StyledProperty<string?> TickFormatProperty = AvaloniaProperty.Register<Axis, string?>(nameof(TickFormat));

    /// <summary>Defines <see cref="TickFormatter"/>.</summary>
    public static readonly StyledProperty<Avalonia.Data.Converters.IValueConverter?> TickFormatterProperty = AvaloniaProperty.Register<Axis, Avalonia.Data.Converters.IValueConverter?>(nameof(TickFormatter));

    private readonly Formatted<Func<object?, int, string?>> _tickFormatter = new(Formatters.Tick);

    /// <summary>Defines <see cref="Unit"/>.</summary>
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<Axis, string?>(nameof(Unit));

    /// <summary>Defines <see cref="Scale"/>.</summary>
    public static readonly StyledProperty<string?> ScaleProperty = AvaloniaProperty.Register<Axis, string?>(nameof(Scale));

    /// <summary>Defines <see cref="Label"/>.</summary>
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<Axis, string?>(nameof(Label));

    /// <summary>Defines <see cref="Name"/>.</summary>
    public static new readonly StyledProperty<string?> NameProperty = AvaloniaProperty.Register<Axis, string?>(nameof(Name));

    /// <summary>Defines <see cref="LabelPosition"/>.</summary>
    public static readonly StyledProperty<LabelPosition?> LabelPositionProperty = AvaloniaProperty.Register<Axis, LabelPosition?>(nameof(LabelPosition));

    /// <summary>Defines <see cref="LabelAngle"/>; references and the polar radius axis share it.</summary>
    public static readonly StyledProperty<double> LabelAngleProperty = AvaloniaProperty.Register<Axis, double>(nameof(LabelAngle));

    /// <summary>Defines <see cref="LabelOffset"/>; references and the polar radius axis share it.</summary>
    public static readonly StyledProperty<double> LabelOffsetProperty = AvaloniaProperty.Register<Axis, double>(nameof(LabelOffset), 5);

    /// <summary>Defines <see cref="LabelFill"/>; references and the polar radius axis share it.</summary>
    public static readonly StyledProperty<string?> LabelFillProperty = AvaloniaProperty.Register<Axis, string?>(nameof(LabelFill));

    /// <summary>Defines <see cref="LabelFontSize"/>; references and the polar radius axis share it.</summary>
    public static readonly StyledProperty<double?> LabelFontSizeProperty = AvaloniaProperty.Register<Axis, double?>(nameof(LabelFontSize));

    /// <summary>Defines <see cref="Angle"/>.</summary>
    public static readonly StyledProperty<double> AngleProperty = AvaloniaProperty.Register<Axis, double>(nameof(Angle));

    /// <summary>Defines <see cref="TextAnchor"/>.</summary>
    public static readonly StyledProperty<TextAnchorKind?> TextAnchorProperty = AvaloniaProperty.Register<Axis, TextAnchorKind?>(nameof(TextAnchor));

    /// <summary>Defines <see cref="TickTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> TickTemplateProperty = AvaloniaProperty.Register<Axis, Avalonia.Controls.Templates.IDataTemplate?>(nameof(TickTemplate));

    /// <summary>Defines <see cref="TickFill"/>.</summary>
    public static readonly StyledProperty<string?> TickFillProperty = AvaloniaProperty.Register<Axis, string?>(nameof(TickFill));

    /// <summary>Defines <see cref="TickFontSize"/>.</summary>
    public static readonly StyledProperty<double?> TickFontSizeProperty = AvaloniaProperty.Register<Axis, double?>(nameof(TickFontSize));

    /// <summary>Defines <see cref="AxisLineStroke"/>.</summary>
    public static readonly StyledProperty<string?> AxisLineStrokeProperty = AvaloniaProperty.Register<Axis, string?>(nameof(AxisLineStroke));

    /// <summary>Defines <see cref="AxisLineStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> AxisLineStrokeWidthProperty = AvaloniaProperty.Register<Axis, double?>(nameof(AxisLineStrokeWidth));

    /// <summary>Defines <see cref="AxisLineStrokeDashArray"/>.</summary>
    public static readonly StyledProperty<object?> AxisLineStrokeDashArrayProperty = AvaloniaProperty.Register<Axis, object?>(nameof(AxisLineStrokeDashArray));

    /// <summary>Defines <see cref="TickLineStroke"/>.</summary>
    public static readonly StyledProperty<string?> TickLineStrokeProperty = AvaloniaProperty.Register<Axis, string?>(nameof(TickLineStroke));

    /// <summary>Defines <see cref="TickLineStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> TickLineStrokeWidthProperty = AvaloniaProperty.Register<Axis, double?>(nameof(TickLineStrokeWidth));

    /// <summary>Defines <see cref="TickLineStrokeDashArray"/>.</summary>
    public static readonly StyledProperty<object?> TickLineStrokeDashArrayProperty = AvaloniaProperty.Register<Axis, object?>(nameof(TickLineStrokeDashArray));

    /// <summary>Defines <see cref="Ticks"/>.</summary>
    public static readonly StyledProperty<string?> TicksProperty = AvaloniaProperty.Register<Axis, string?>(nameof(Ticks));

    /// <summary>Defines <see cref="PaddingKind"/>.</summary>
    public static readonly StyledProperty<AxisPaddingKind> PaddingKindProperty = AvaloniaProperty.Register<Axis, AxisPaddingKind>(nameof(PaddingKind));

    /// <summary>Defines <see cref="AllowDuplicatedCategory"/>.</summary>
    public static readonly StyledProperty<bool> AllowDuplicatedCategoryProperty = AvaloniaProperty.Register<Axis, bool>(nameof(AllowDuplicatedCategory), true);

    /// <summary>Defines <see cref="IncludeHidden"/>.</summary>
    public static readonly StyledProperty<bool> IncludeHiddenProperty = AvaloniaProperty.Register<Axis, bool>(nameof(IncludeHidden));

    private readonly ChangeRelay _relay = new();

    private protected Axis()
    {
        _relay.Follow(Labels, this, LogicalChildren);
        _relay.FollowClasses(this);
    }

    /// <summary>The axis' <c>Label</c> children, drawn after its <see cref="Label"/> text.</summary>
    [Content]
    public AvaloniaList<Label> Labels { get; } = [];

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>The data key of the axis' values.</summary>
    public string? DataKey { get => GetValue(DataKeyProperty); set => SetValue(DataKeyProperty, value); }

    /// <summary>Categories or numbers; an x-axis defaults to categories and a y-axis to numbers.</summary>
    public AxisType? Type { get => GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>Where the axis sits.</summary>
    public AxisOrientation? Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Whether the axis is hidden; it still maps its series.</summary>
    public bool Hide { get => GetValue(HideProperty); set => SetValue(HideProperty, value); }

    /// <summary>How many ticks a numeric axis aims for.</summary>
    public int TickCount { get => GetValue(TickCountProperty); set => SetValue(TickCountProperty, value); }

    /// <summary>Whether a numeric axis' ticks may be fractional.</summary>
    public bool AllowDecimals { get => GetValue(AllowDecimalsProperty); set => SetValue(AllowDecimalsProperty, value); }

    /// <summary>The domain as Recharts' two bounds: <c>0, auto</c>, <c>dataMin - 10, dataMax + 10</c>, <c>0, 100</c>.</summary>
    public string? Domain { get => GetValue(DomainProperty); set => SetValue(DomainProperty, value); }

    /// <summary>Whether a specified domain may cut the data.</summary>
    public bool AllowDataOverflow { get => GetValue(AllowDataOverflowProperty); set => SetValue(AllowDataOverflowProperty, value); }

    /// <summary>The padding at the ends of the axis' range (left and right for an x-axis, top and bottom for a y-axis).</summary>
    public Thickness Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }

    /// <summary>Whether the range runs backwards.</summary>
    public bool Reversed { get => GetValue(ReversedProperty); set => SetValue(ReversedProperty, value); }

    /// <summary>Whether the axis is drawn inside the plot.</summary>
    public bool Mirror { get => GetValue(MirrorProperty); set => SetValue(MirrorProperty, value); }

    /// <summary>Which ticks are kept when labels collide: <c>preserveEnd</c>, <c>preserveStart</c>, <c>preserveStartEnd</c> or a number of ticks to skip.</summary>
    public string? Interval { get => GetValue(IntervalProperty); set => SetValue(IntervalProperty, value); }

    /// <summary>The smallest gap between two tick labels.</summary>
    public double MinTickGap { get => GetValue(MinTickGapProperty); set => SetValue(MinTickGapProperty, value); }

    /// <summary>The length of a tick line.</summary>
    public double TickSize { get => GetValue(TickSizeProperty); set => SetValue(TickSizeProperty, value); }

    /// <summary>The gap between a tick line and its label.</summary>
    public double TickMargin { get => GetValue(TickMarginProperty); set => SetValue(TickMarginProperty, value); }

    /// <summary>Whether tick lines are drawn.</summary>
    public bool TickLine { get => GetValue(TickLineProperty); set => SetValue(TickLineProperty, value); }

    /// <summary>Whether the axis line is drawn.</summary>
    public bool AxisLine { get => GetValue(AxisLineProperty); set => SetValue(AxisLineProperty, value); }

    /// <summary>Whether tick labels are drawn.</summary>
    public bool Tick { get => GetValue(TickProperty); set => SetValue(TickProperty, value); }

    /// <summary>The axis' color.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>A .NET composite format for tick labels (<c>{0:N0}</c>, <c>{0}k</c>).</summary>
    public string? TickFormat { get => GetValue(TickFormatProperty); set => SetValue(TickFormatProperty, value); }

    /// <summary>
    /// Recharts' <c>tickFormatter</c>: a converter of a tick's value (a category's text, a number or a date) to its text,
    /// its index as the parameter, over <see cref="TickFormat"/>; a function from code through <c>FuncValueConverter</c>.
    /// </summary>
    public Avalonia.Data.Converters.IValueConverter? TickFormatter { get => GetValue(TickFormatterProperty); set => SetValue(TickFormatterProperty, value); }

    /// <summary>A unit written after every tick label.</summary>
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    /// <summary>The scale of a category axis, <c>band</c> or <c>point</c>.</summary>
    public string? Scale { get => GetValue(ScaleProperty); set => SetValue(ScaleProperty, value); }

    /// <summary>The axis' label.</summary>
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Where the axis' label sits.</summary>
    public LabelPosition? LabelPosition { get => GetValue(LabelPositionProperty); set => SetValue(LabelPositionProperty, value); }

    /// <summary>The axis' name in a scatter's tooltip; its data key by default.</summary>
    public new string? Name { get => GetValue(NameProperty); set => SetValue(NameProperty, value); }

    /// <summary>The label's rotation in degrees (Recharts' label <c>angle</c>, -90 for the usual y-axis label).</summary>
    public double LabelAngle { get => GetValue(LabelAngleProperty); set => SetValue(LabelAngleProperty, value); }

    /// <summary>The label's distance from the axis' box (Recharts' label <c>offset</c>, 5 by default).</summary>
    public double LabelOffset { get => GetValue(LabelOffsetProperty); set => SetValue(LabelOffsetProperty, value); }

    /// <summary>The label's color (Recharts' label <c>fill</c>, <c>#808080</c> by default).</summary>
    public string? LabelFill { get => GetValue(LabelFillProperty); set => SetValue(LabelFillProperty, value); }

    /// <summary>The label's font size; the chart's by default.</summary>
    public double? LabelFontSize { get => GetValue(LabelFontSizeProperty); set => SetValue(LabelFontSizeProperty, value); }

    /// <summary>The rotation of the tick labels in degrees (Recharts' <c>angle</c>); collisions are measured on the rotated labels.</summary>
    public double Angle { get => GetValue(AngleProperty); set => SetValue(AngleProperty, value); }

    /// <summary>The anchor of the tick labels (Recharts' <c>textAnchor</c>, usually <c>End</c> with a negative angle); the orientation's by default.</summary>
    public TextAnchorKind? TextAnchor { get => GetValue(TextAnchorProperty); set => SetValue(TextAnchorProperty, value); }

    /// <summary>
    /// The custom content drawn in place of each tick's label (Recharts' <c>tick</c> element): a template whose data
    /// context is the tick's <see cref="TickContext"/> (its label's point, its text and the axis' paint); the tick lines
    /// stay the axis'.
    /// </summary>
    public Avalonia.Controls.Templates.IDataTemplate? TickTemplate { get => GetValue(TickTemplateProperty); set => SetValue(TickTemplateProperty, value); }

    /// <summary>The color of the tick labels (the <c>tick</c> object's <c>fill</c>); the axis' stroke by default.</summary>
    public string? TickFill { get => GetValue(TickFillProperty); set => SetValue(TickFillProperty, value); }

    /// <summary>The font size of the tick labels (the <c>tick</c> object's <c>fontSize</c>); the chart's by default.</summary>
    public double? TickFontSize { get => GetValue(TickFontSizeProperty); set => SetValue(TickFontSizeProperty, value); }

    /// <summary>The axis line's color (the <c>axisLine</c> object's <c>stroke</c>); the axis' stroke by default.</summary>
    public string? AxisLineStroke { get => GetValue(AxisLineStrokeProperty); set => SetValue(AxisLineStrokeProperty, value); }

    /// <summary>The axis line's width (the <c>axisLine</c> object's <c>strokeWidth</c>).</summary>
    public double? AxisLineStrokeWidth { get => GetValue(AxisLineStrokeWidthProperty); set => SetValue(AxisLineStrokeWidthProperty, value); }

    /// <summary>The axis line's dash pattern (the <c>axisLine</c> object's <c>strokeDasharray</c>).</summary>
    public object? AxisLineStrokeDashArray { get => GetValue(AxisLineStrokeDashArrayProperty); set => SetValue(AxisLineStrokeDashArrayProperty, value); }

    /// <summary>The tick lines' color (the <c>tickLine</c> object's <c>stroke</c>); the axis' stroke by default.</summary>
    public string? TickLineStroke { get => GetValue(TickLineStrokeProperty); set => SetValue(TickLineStrokeProperty, value); }

    /// <summary>The tick lines' width (the <c>tickLine</c> object's <c>strokeWidth</c>).</summary>
    public double? TickLineStrokeWidth { get => GetValue(TickLineStrokeWidthProperty); set => SetValue(TickLineStrokeWidthProperty, value); }

    /// <summary>The tick lines' dash pattern (the <c>tickLine</c> object's <c>strokeDasharray</c>).</summary>
    public object? TickLineStrokeDashArray { get => GetValue(TickLineStrokeDashArrayProperty); set => SetValue(TickLineStrokeDashArrayProperty, value); }

    /// <summary>The ticks to show instead of the computed ones (Recharts' <c>ticks</c>): numbers or categories separated by commas.</summary>
    public string? Ticks { get => GetValue(TicksProperty); set => SetValue(TicksProperty, value); }

    /// <summary>Whether a numeric axis pads its ends by half the smallest gap between its values (Recharts' <c>padding="gap"</c> or <c>"no-gap"</c>) instead of <see cref="Padding"/>.</summary>
    public AxisPaddingKind PaddingKind { get => GetValue(PaddingKindProperty); set => SetValue(PaddingKindProperty, value); }

    /// <summary>Whether a category that appears twice gets two ticks (the default) or one.</summary>
    public bool AllowDuplicatedCategory { get => GetValue(AllowDuplicatedCategoryProperty); set => SetValue(AllowDuplicatedCategoryProperty, value); }

    /// <summary>Whether hidden series still count in the axis' domain.</summary>
    public bool IncludeHidden { get => GetValue(IncludeHiddenProperty); set => SetValue(IncludeHiddenProperty, value); }

    private protected abstract bool IsX { get; }

    private protected abstract string Id { get; }

    private protected abstract double Size { get; }

    void ISpecSource.Apply(SpecBuilder builder, int order)
    {
        var (min, max) = Type == AxisType.Category || (Type is null && IsX) ? (null, null) : SpecText.Domain(Domain);
        var (interval, count) = SpecText.Interval(Interval);
        var spec = new AxisSpec
        {
            Presentation = Presentation.Of(this),
            ClassName = ChangeRelay.ClassName(this),
            Id = Id,
            DataKey = DataKey,
            Type = Type ?? (IsX ? AxisType.Category : AxisType.Number),
            Orientation = Orientation ?? (IsX ? AxisOrientation.Bottom : AxisOrientation.Left),
            Size = double.IsNaN(Size) ? 0 : Size,
            Hide = Hide,
            TickCount = TickCount,
            AllowDecimals = AllowDecimals,
            DomainMin = min,
            DomainMax = max,
            AllowDataOverflow = AllowDataOverflow,
            PaddingStart = IsX ? Padding.Left : Padding.Bottom,
            PaddingEnd = IsX ? Padding.Right : Padding.Top,
            Reversed = Reversed,
            Mirror = Mirror,
            Interval = interval,
            IntervalCount = count,
            MinTickGap = MinTickGap,
            TickSize = TickSize,
            TickMargin = TickMargin,
            TickLine = TickLine,
            AxisLine = AxisLine,
            Tick = Tick,
            Stroke = Stroke,
            TickFormat = TickFormat,
            TickFormatter = _tickFormatter.Of(TickFormatter),
            TickConverter = Formatters.Reference(TickFormatter),
            Unit = Unit,
            Scale = Scale,
            Label = SpecText.Label(Label, LabelPosition, this),
            Labels = Pictor.Label.Specs(Labels, builder),
            TickContent = builder.Content(TickTemplate, Content.ContentSlots.Tick, this, nameof(TickTemplate)),
            ClickCommand = Clickable.Key(this, order),
            Name = Name,
            Order = order,
            Angle = Angle,
            TextAnchor = TextAnchor,
            TickFill = TickFill,
            TickFontSize = TickFontSize,
            AxisLineStyle = SpecText.Line(AxisLineStroke, AxisLineStrokeWidth, SpecText.Dashes(AxisLineStrokeDashArray)),
            TickLineStyle = SpecText.Line(TickLineStroke, TickLineStrokeWidth, SpecText.Dashes(TickLineStrokeDashArray)),
            Ticks = SpecText.Texts(Ticks),
            PaddingKind = PaddingKind,
            AllowDuplicatedCategory = AllowDuplicatedCategory,
            IncludeHidden = IncludeHidden,
            CategoryDomain = Type == AxisType.Category || (Type is null && IsX) ? SpecText.Texts(Domain) : [],
        };
        (IsX ? builder.XAxes : builder.YAxes).Add(spec);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}

/// <summary>Recharts' <c>XAxis</c>: categories below the plot by default.</summary>
public sealed class XAxis : Axis
{
    /// <summary>Defines <see cref="XAxisId"/>.</summary>
    public static readonly StyledProperty<string> XAxisIdProperty = AvaloniaProperty.Register<XAxis, string>(nameof(XAxisId), "0");

    /// <summary>Defines <see cref="Height"/>.</summary>
    public static readonly StyledProperty<double> HeightProperty = AvaloniaProperty.Register<XAxis, double>(nameof(Height), double.NaN);

    /// <summary>The id series refer to.</summary>
    public string XAxisId { get => GetValue(XAxisIdProperty); set => SetValue(XAxisIdProperty, value); }

    /// <summary>The axis' height (30 by default).</summary>
    public double Height { get => GetValue(HeightProperty); set => SetValue(HeightProperty, value); }

    private protected override bool IsX => true;

    private protected override string Id => XAxisId;

    private protected override double Size => Height;
}

/// <summary>Recharts' <c>YAxis</c>: numbers left of the plot by default.</summary>
public sealed class YAxis : Axis
{
    /// <summary>Defines <see cref="YAxisId"/>.</summary>
    public static readonly StyledProperty<string> YAxisIdProperty = AvaloniaProperty.Register<YAxis, string>(nameof(YAxisId), "0");

    /// <summary>Defines <see cref="Width"/>.</summary>
    public static readonly StyledProperty<double> WidthProperty = AvaloniaProperty.Register<YAxis, double>(nameof(Width), double.NaN);

    /// <summary>The id series refer to.</summary>
    public string YAxisId { get => GetValue(YAxisIdProperty); set => SetValue(YAxisIdProperty, value); }

    /// <summary>The axis' width (60 by default).</summary>
    public double Width { get => GetValue(WidthProperty); set => SetValue(WidthProperty, value); }

    private protected override bool IsX => false;

    private protected override string Id => YAxisId;

    private protected override double Size => Width;
}
