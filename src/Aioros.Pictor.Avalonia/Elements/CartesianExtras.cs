using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Aioros.Pictor;

/// <summary>Recharts' <c>ZAxis</c>: maps a third value of a scatter's points to the area of their symbols.</summary>
public sealed class ZAxis : StyledElement, IChartElement, ISpecSource
{
    /// <summary>Defines <see cref="ZAxisId"/>.</summary>
    public static readonly StyledProperty<string> ZAxisIdProperty = AvaloniaProperty.Register<ZAxis, string>(nameof(ZAxisId), "0");

    /// <summary>Defines <see cref="DataKey"/>.</summary>
    public static readonly StyledProperty<string?> DataKeyProperty = AvaloniaProperty.Register<ZAxis, string?>(nameof(DataKey));

    /// <summary>Defines <see cref="Range"/>.</summary>
    public static readonly StyledProperty<string> RangeProperty = AvaloniaProperty.Register<ZAxis, string>(nameof(Range), "64,64");

    /// <summary>Defines <see cref="Name"/>.</summary>
    public static new readonly StyledProperty<string?> NameProperty = AvaloniaProperty.Register<ZAxis, string?>(nameof(Name));

    /// <summary>Defines <see cref="Unit"/>.</summary>
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<ZAxis, string?>(nameof(Unit));

    /// <summary>Defines <see cref="Type"/>.</summary>
    public static readonly StyledProperty<AxisType> TypeProperty = AvaloniaProperty.Register<ZAxis, AxisType>(nameof(Type), AxisType.Number);

    /// <summary>Defines <see cref="Scale"/>.</summary>
    public static readonly StyledProperty<string?> ScaleProperty = AvaloniaProperty.Register<ZAxis, string?>(nameof(Scale));

    /// <summary>Defines <see cref="Domain"/>.</summary>
    public static readonly StyledProperty<string?> DomainProperty = AvaloniaProperty.Register<ZAxis, string?>(nameof(Domain));

    private readonly ChangeRelay _relay = new();

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>The id scatters refer to.</summary>
    public string ZAxisId { get => GetValue(ZAxisIdProperty); set => SetValue(ZAxisIdProperty, value); }

    /// <summary>The data key of the sizes.</summary>
    public string? DataKey { get => GetValue(DataKeyProperty); set => SetValue(DataKeyProperty, value); }

    /// <summary>The smallest and the largest symbol area (<c>60,400</c>); one value gives every symbol that area.</summary>
    public string Range { get => GetValue(RangeProperty); set => SetValue(RangeProperty, value); }

    /// <summary>The name shown in the tooltip.</summary>
    public new string? Name { get => GetValue(NameProperty); set => SetValue(NameProperty, value); }

    /// <summary>A unit written after the values in the tooltip.</summary>
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    /// <summary>Whether the sizes come from numbers or from categories (each a band of the range).</summary>
    public AxisType Type { get => GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>The scale of numeric sizes: <c>auto</c>, <c>linear</c>, <c>log</c>, <c>pow</c> or <c>sqrt</c>.</summary>
    public string? Scale { get => GetValue(ScaleProperty); set => SetValue(ScaleProperty, value); }

    /// <summary>The domain as two bounds, as an axis' (<c>0, auto</c> by default).</summary>
    public string? Domain { get => GetValue(DomainProperty); set => SetValue(DomainProperty, value); }

    void ISpecSource.Apply(SpecBuilder builder, int order)
    {
        var range = SpecText.Numbers(Range);
        var (min, max) = SpecText.Domain(Domain);
        builder.ZAxes.Add(new ZAxisSpec
        {
            Id = ZAxisId,
            DataKey = DataKey,
            RangeMin = range.IsEmpty ? 64 : range[0],
            RangeMax = range.Length > 1 ? range[1] : range.IsEmpty ? 64 : range[0],
            Name = Name,
            Unit = Unit,
            Type = Type,
            Scale = Scale,
            DomainMin = min,
            DomainMax = max,
        });
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}

/// <summary>Recharts' <c>ReferenceArea</c>: a rectangle between two x values, two y values, or both.</summary>
public sealed class ReferenceArea : StyledElement, IChartElement, ISpecSource
{
    /// <summary>Defines <see cref="ShapeTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ShapeTemplateProperty = AvaloniaProperty.Register<ReferenceArea, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ShapeTemplate));

    /// <summary>The custom content drawn in place of the reference's shape (Recharts' <c>shape</c> element): a template whose data context is its <see cref="ReferenceAreaContext"/>.</summary>
    public Avalonia.Controls.Templates.IDataTemplate? ShapeTemplate { get => GetValue(ShapeTemplateProperty); set => SetValue(ShapeTemplateProperty, value); }

    /// <summary>Defines <see cref="X1"/>.</summary>
    public static readonly StyledProperty<string?> X1Property = AvaloniaProperty.Register<ReferenceArea, string?>(nameof(X1));

    /// <summary>Defines <see cref="X2"/>.</summary>
    public static readonly StyledProperty<string?> X2Property = AvaloniaProperty.Register<ReferenceArea, string?>(nameof(X2));

    /// <summary>Defines <see cref="Y1"/>.</summary>
    public static readonly StyledProperty<string?> Y1Property = AvaloniaProperty.Register<ReferenceArea, string?>(nameof(Y1));

    /// <summary>Defines <see cref="Y2"/>.</summary>
    public static readonly StyledProperty<string?> Y2Property = AvaloniaProperty.Register<ReferenceArea, string?>(nameof(Y2));

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<ReferenceArea, string?>(nameof(Fill));

    /// <summary>Defines <see cref="FillOpacity"/>.</summary>
    public static readonly StyledProperty<double> FillOpacityProperty = AvaloniaProperty.Register<ReferenceArea, double>(nameof(FillOpacity), 0.5);

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string> StrokeProperty = AvaloniaProperty.Register<ReferenceArea, string>(nameof(Stroke), "none");

    /// <summary>Defines <see cref="StrokeOpacity"/>.</summary>
    public static readonly StyledProperty<double?> StrokeOpacityProperty = AvaloniaProperty.Register<ReferenceArea, double?>(nameof(StrokeOpacity));

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double> StrokeWidthProperty = AvaloniaProperty.Register<ReferenceArea, double>(nameof(StrokeWidth), 1);

    /// <summary>Defines <see cref="Label"/>.</summary>
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<ReferenceArea, string?>(nameof(Label));

    /// <summary>Defines <see cref="LabelPosition"/>.</summary>
    public static readonly StyledProperty<LabelPosition?> LabelPositionProperty = AvaloniaProperty.Register<ReferenceArea, LabelPosition?>(nameof(LabelPosition));

    /// <summary>Defines <see cref="LabelAngle"/>.</summary>
    public static readonly StyledProperty<double> LabelAngleProperty = Axis.LabelAngleProperty.AddOwner<ReferenceArea>();

    /// <summary>Defines <see cref="LabelOffset"/>.</summary>
    public static readonly StyledProperty<double> LabelOffsetProperty = Axis.LabelOffsetProperty.AddOwner<ReferenceArea>();

    /// <summary>Defines <see cref="LabelFill"/>.</summary>
    public static readonly StyledProperty<string?> LabelFillProperty = Axis.LabelFillProperty.AddOwner<ReferenceArea>();

    /// <summary>Defines <see cref="LabelFontSize"/>.</summary>
    public static readonly StyledProperty<double?> LabelFontSizeProperty = Axis.LabelFontSizeProperty.AddOwner<ReferenceArea>();

    /// <summary>Defines <see cref="XAxisId"/>.</summary>
    public static readonly StyledProperty<string> XAxisIdProperty = AvaloniaProperty.Register<ReferenceArea, string>(nameof(XAxisId), "0");

    /// <summary>Defines <see cref="YAxisId"/>.</summary>
    public static readonly StyledProperty<string> YAxisIdProperty = AvaloniaProperty.Register<ReferenceArea, string>(nameof(YAxisId), "0");

    /// <summary>Defines <see cref="IfOverflow"/>.</summary>
    public static readonly StyledProperty<string> IfOverflowProperty = AvaloniaProperty.Register<ReferenceArea, string>(nameof(IfOverflow), "discard");

    /// <summary>Defines <see cref="Radius"/>.</summary>
    public static readonly StyledProperty<object?> RadiusProperty = AvaloniaProperty.Register<ReferenceArea, object?>(nameof(Radius));

    private readonly ChangeRelay _relay = new();

    /// <summary>Creates the element, following its label children.</summary>
    public ReferenceArea() => _relay.Follow(Labels, this, LogicalChildren);

    /// <summary>The element's <c>Label</c> children, drawn after its <see cref="Label"/> text.</summary>
    [Content]
    public AvaloniaList<Label> Labels { get; } = [];

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>The left x value (a category or a number); the plot's edge when null.</summary>
    public string? X1 { get => GetValue(X1Property); set => SetValue(X1Property, value); }

    /// <summary>The right x value; the plot's edge when null.</summary>
    public string? X2 { get => GetValue(X2Property); set => SetValue(X2Property, value); }

    /// <summary>The first y value; the plot's edge when null.</summary>
    public string? Y1 { get => GetValue(Y1Property); set => SetValue(Y1Property, value); }

    /// <summary>The second y value; the plot's edge when null.</summary>
    public string? Y2 { get => GetValue(Y2Property); set => SetValue(Y2Property, value); }

    /// <summary>The fill.</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The fill opacity.</summary>
    public double FillOpacity { get => GetValue(FillOpacityProperty); set => SetValue(FillOpacityProperty, value); }

    /// <summary>The stroke.</summary>
    public string Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The stroke opacity.</summary>
    public double? StrokeOpacity { get => GetValue(StrokeOpacityProperty); set => SetValue(StrokeOpacityProperty, value); }

    /// <summary>The stroke width.</summary>
    public double StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    /// <summary>The area's label.</summary>
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Where the label sits; centered by default.</summary>
    public LabelPosition? LabelPosition { get => GetValue(LabelPositionProperty); set => SetValue(LabelPositionProperty, value); }

    /// <summary>The label's rotation in degrees (Recharts' label <c>angle</c>).</summary>
    public double LabelAngle { get => GetValue(LabelAngleProperty); set => SetValue(LabelAngleProperty, value); }

    /// <summary>The label's distance from its box (Recharts' label <c>offset</c>, 5 by default).</summary>
    public double LabelOffset { get => GetValue(LabelOffsetProperty); set => SetValue(LabelOffsetProperty, value); }

    /// <summary>The label's color (Recharts' label <c>fill</c>).</summary>
    public string? LabelFill { get => GetValue(LabelFillProperty); set => SetValue(LabelFillProperty, value); }

    /// <summary>The label's font size; the chart's by default.</summary>
    public double? LabelFontSize { get => GetValue(LabelFontSizeProperty); set => SetValue(LabelFontSizeProperty, value); }

    /// <summary>The x-axis the area maps through.</summary>
    public string XAxisId { get => GetValue(XAxisIdProperty); set => SetValue(XAxisIdProperty, value); }

    /// <summary>The y-axis the area maps through.</summary>
    public string YAxisId { get => GetValue(YAxisIdProperty); set => SetValue(YAxisIdProperty, value); }

    /// <summary>What a value outside the domain does: <c>discard</c>, <c>visible</c>, <c>hidden</c> or <c>extendDomain</c>.</summary>
    public string IfOverflow { get => GetValue(IfOverflowProperty); set => SetValue(IfOverflowProperty, value); }

    /// <summary>The rectangle's corner radius: one value, or four (top left, top right, bottom right, bottom left).</summary>
    public object? Radius { get => GetValue(RadiusProperty); set => SetValue(RadiusProperty, value); }

    void ISpecSource.Apply(SpecBuilder builder, int order) => builder.ReferenceAreas.Add(new ReferenceAreaSpec
    {
        ShapeContent = builder.Content(ShapeTemplate, Content.ContentSlots.ReferenceArea, this, nameof(ShapeTemplate)),
        Radius = SpecText.Numbers(Radius),
        X1 = X1,
        X2 = X2,
        Y1 = Y1,
        Y2 = Y2,
        Fill = Fill,
        FillOpacity = FillOpacity,
        Stroke = Stroke,
        StrokeOpacity = StrokeOpacity,
        StrokeWidth = StrokeWidth,
        Label = SpecText.Label(Label, LabelPosition, this),
        Labels = Pictor.Label.Specs(Labels, builder),
        XAxisId = XAxisId,
        YAxisId = YAxisId,
        IfOverflow = SpecText.Overflow(IfOverflow),
        Order = order,
    });

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}

/// <summary>Recharts' <c>ReferenceDot</c>: a circle at an x and a y value.</summary>
public sealed class ReferenceDot : StyledElement, IChartElement, ISpecSource
{
    /// <summary>Defines <see cref="ShapeTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ShapeTemplateProperty = AvaloniaProperty.Register<ReferenceDot, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ShapeTemplate));

    /// <summary>The custom content drawn in place of the reference's shape (Recharts' <c>shape</c> element): a template whose data context is its <see cref="ReferenceDotContext"/>.</summary>
    public Avalonia.Controls.Templates.IDataTemplate? ShapeTemplate { get => GetValue(ShapeTemplateProperty); set => SetValue(ShapeTemplateProperty, value); }

    /// <summary>Defines <see cref="X"/>.</summary>
    public static readonly StyledProperty<string?> XProperty = AvaloniaProperty.Register<ReferenceDot, string?>(nameof(X));

    /// <summary>Defines <see cref="Y"/>.</summary>
    public static readonly StyledProperty<string?> YProperty = AvaloniaProperty.Register<ReferenceDot, string?>(nameof(Y));

    /// <summary>Defines <see cref="R"/>.</summary>
    public static readonly StyledProperty<double> RProperty = AvaloniaProperty.Register<ReferenceDot, double>(nameof(R), 10);

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<ReferenceDot, string?>(nameof(Fill));

    /// <summary>Defines <see cref="FillOpacity"/>.</summary>
    public static readonly StyledProperty<double> FillOpacityProperty = AvaloniaProperty.Register<ReferenceDot, double>(nameof(FillOpacity), 1);

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<ReferenceDot, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double> StrokeWidthProperty = AvaloniaProperty.Register<ReferenceDot, double>(nameof(StrokeWidth), 1);

    /// <summary>Defines <see cref="Label"/>.</summary>
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<ReferenceDot, string?>(nameof(Label));

    /// <summary>Defines <see cref="LabelPosition"/>.</summary>
    public static readonly StyledProperty<LabelPosition?> LabelPositionProperty = AvaloniaProperty.Register<ReferenceDot, LabelPosition?>(nameof(LabelPosition));

    /// <summary>Defines <see cref="LabelAngle"/>.</summary>
    public static readonly StyledProperty<double> LabelAngleProperty = Axis.LabelAngleProperty.AddOwner<ReferenceDot>();

    /// <summary>Defines <see cref="LabelOffset"/>.</summary>
    public static readonly StyledProperty<double> LabelOffsetProperty = Axis.LabelOffsetProperty.AddOwner<ReferenceDot>();

    /// <summary>Defines <see cref="LabelFill"/>.</summary>
    public static readonly StyledProperty<string?> LabelFillProperty = Axis.LabelFillProperty.AddOwner<ReferenceDot>();

    /// <summary>Defines <see cref="LabelFontSize"/>.</summary>
    public static readonly StyledProperty<double?> LabelFontSizeProperty = Axis.LabelFontSizeProperty.AddOwner<ReferenceDot>();

    /// <summary>Defines <see cref="XAxisId"/>.</summary>
    public static readonly StyledProperty<string> XAxisIdProperty = AvaloniaProperty.Register<ReferenceDot, string>(nameof(XAxisId), "0");

    /// <summary>Defines <see cref="YAxisId"/>.</summary>
    public static readonly StyledProperty<string> YAxisIdProperty = AvaloniaProperty.Register<ReferenceDot, string>(nameof(YAxisId), "0");

    /// <summary>Defines <see cref="IfOverflow"/>.</summary>
    public static readonly StyledProperty<string> IfOverflowProperty = AvaloniaProperty.Register<ReferenceDot, string>(nameof(IfOverflow), "discard");

    private readonly ChangeRelay _relay = new();

    /// <summary>Creates the element, following its label children.</summary>
    public ReferenceDot() => _relay.Follow(Labels, this, LogicalChildren);

    /// <summary>The element's <c>Label</c> children, drawn after its <see cref="Label"/> text.</summary>
    [Content]
    public AvaloniaList<Label> Labels { get; } = [];

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>The x value (a category or a number).</summary>
    public string? X { get => GetValue(XProperty); set => SetValue(XProperty, value); }

    /// <summary>The y value.</summary>
    public string? Y { get => GetValue(YProperty); set => SetValue(YProperty, value); }

    /// <summary>The radius.</summary>
    public double R { get => GetValue(RProperty); set => SetValue(RProperty, value); }

    /// <summary>The fill.</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The fill opacity.</summary>
    public double FillOpacity { get => GetValue(FillOpacityProperty); set => SetValue(FillOpacityProperty, value); }

    /// <summary>The stroke.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The stroke width.</summary>
    public double StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    /// <summary>The dot's label.</summary>
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Where the label sits; centered by default.</summary>
    public LabelPosition? LabelPosition { get => GetValue(LabelPositionProperty); set => SetValue(LabelPositionProperty, value); }

    /// <summary>The label's rotation in degrees (Recharts' label <c>angle</c>).</summary>
    public double LabelAngle { get => GetValue(LabelAngleProperty); set => SetValue(LabelAngleProperty, value); }

    /// <summary>The label's distance from its box (Recharts' label <c>offset</c>, 5 by default).</summary>
    public double LabelOffset { get => GetValue(LabelOffsetProperty); set => SetValue(LabelOffsetProperty, value); }

    /// <summary>The label's color (Recharts' label <c>fill</c>).</summary>
    public string? LabelFill { get => GetValue(LabelFillProperty); set => SetValue(LabelFillProperty, value); }

    /// <summary>The label's font size; the chart's by default.</summary>
    public double? LabelFontSize { get => GetValue(LabelFontSizeProperty); set => SetValue(LabelFontSizeProperty, value); }

    /// <summary>The x-axis the dot maps through.</summary>
    public string XAxisId { get => GetValue(XAxisIdProperty); set => SetValue(XAxisIdProperty, value); }

    /// <summary>The y-axis the dot maps through.</summary>
    public string YAxisId { get => GetValue(YAxisIdProperty); set => SetValue(YAxisIdProperty, value); }

    /// <summary>What a value outside the domain does: <c>discard</c>, <c>visible</c>, <c>hidden</c> or <c>extendDomain</c>.</summary>
    public string IfOverflow { get => GetValue(IfOverflowProperty); set => SetValue(IfOverflowProperty, value); }

    void ISpecSource.Apply(SpecBuilder builder, int order) => builder.ReferenceDots.Add(new ReferenceDotSpec
    {
        ShapeContent = builder.Content(ShapeTemplate, Content.ContentSlots.ReferenceDot, this, nameof(ShapeTemplate)),
        X = X,
        Y = Y,
        R = R,
        Fill = Fill,
        FillOpacity = FillOpacity,
        Stroke = Stroke,
        StrokeWidth = StrokeWidth,
        Label = SpecText.Label(Label, LabelPosition, this),
        Labels = Pictor.Label.Specs(Labels, builder),
        XAxisId = XAxisId,
        YAxisId = YAxisId,
        IfOverflow = SpecText.Overflow(IfOverflow),
        Order = order,
    });

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}

/// <summary>Recharts' <c>ErrorBar</c>: a whisker around each value of its series.</summary>
public sealed class ErrorBar : StyledElement, ISeriesPart
{
    /// <summary>Defines <see cref="DataKey"/>.</summary>
    public static readonly StyledProperty<string> DataKeyProperty = AvaloniaProperty.Register<ErrorBar, string>(nameof(DataKey), string.Empty);

    /// <summary>Defines <see cref="Width"/>.</summary>
    public static readonly StyledProperty<double> WidthProperty = AvaloniaProperty.Register<ErrorBar, double>(nameof(Width), 5);

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double> StrokeWidthProperty = AvaloniaProperty.Register<ErrorBar, double>(nameof(StrokeWidth), 1.5);

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<ErrorBar, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="Direction"/>.</summary>
    public static readonly StyledProperty<string?> DirectionProperty = AvaloniaProperty.Register<ErrorBar, string?>(nameof(Direction));

    private readonly ChangeRelay _relay = new();

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>The data key of the errors.</summary>
    public string DataKey { get => GetValue(DataKeyProperty); set => SetValue(DataKeyProperty, value); }

    /// <summary>Half the width of the whisker's ends.</summary>
    public double Width { get => GetValue(WidthProperty); set => SetValue(WidthProperty, value); }

    /// <summary>The stroke width.</summary>
    public double StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    /// <summary>The stroke color.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>A scatter's direction, <c>x</c> or <c>y</c>; other series follow the chart's layout.</summary>
    public string? Direction { get => GetValue(DirectionProperty); set => SetValue(DirectionProperty, value); }

    internal ErrorBarSpec Spec() => new() { DataKey = DataKey, Width = Width, StrokeWidth = StrokeWidth, Stroke = Stroke, AlongX = Direction is "x" or "X", Presentation = Presentation.Of(this) };

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}
