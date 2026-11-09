using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Aioros.Pictor;

/// <summary>Recharts' <c>CartesianGrid</c>: the lines behind the plot at the axes' ticks.</summary>
public sealed class CartesianGrid : StyledElement, IChartElement, ISpecSource
{
    /// <summary>Defines <see cref="Horizontal"/>.</summary>
    public static readonly StyledProperty<bool> HorizontalProperty = AvaloniaProperty.Register<CartesianGrid, bool>(nameof(Horizontal), true);

    /// <summary>Defines <see cref="Vertical"/>.</summary>
    public static readonly StyledProperty<bool> VerticalProperty = AvaloniaProperty.Register<CartesianGrid, bool>(nameof(Vertical), true);

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<CartesianGrid, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="StrokeDashArray"/>.</summary>
    public static readonly StyledProperty<object?> StrokeDashArrayProperty = AvaloniaProperty.Register<CartesianGrid, object?>(nameof(StrokeDashArray));

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<CartesianGrid, string?>(nameof(Fill));

    /// <summary>Defines <see cref="FillOpacity"/>.</summary>
    public static readonly StyledProperty<double?> FillOpacityProperty = AvaloniaProperty.Register<CartesianGrid, double?>(nameof(FillOpacity));

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> StrokeWidthProperty = AvaloniaProperty.Register<CartesianGrid, double?>(nameof(StrokeWidth));

    /// <summary>Defines <see cref="StrokeOpacity"/>.</summary>
    public static readonly StyledProperty<double?> StrokeOpacityProperty = AvaloniaProperty.Register<CartesianGrid, double?>(nameof(StrokeOpacity));

    /// <summary>Defines <see cref="HorizontalFill"/>.</summary>
    public static readonly StyledProperty<string?> HorizontalFillProperty = AvaloniaProperty.Register<CartesianGrid, string?>(nameof(HorizontalFill));

    /// <summary>Defines <see cref="VerticalFill"/>.</summary>
    public static readonly StyledProperty<string?> VerticalFillProperty = AvaloniaProperty.Register<CartesianGrid, string?>(nameof(VerticalFill));

    /// <summary>Defines <see cref="HorizontalPoints"/>.</summary>
    public static readonly StyledProperty<string?> HorizontalPointsProperty = AvaloniaProperty.Register<CartesianGrid, string?>(nameof(HorizontalPoints));

    /// <summary>Defines <see cref="VerticalPoints"/>.</summary>
    public static readonly StyledProperty<string?> VerticalPointsProperty = AvaloniaProperty.Register<CartesianGrid, string?>(nameof(VerticalPoints));

    /// <summary>Defines <see cref="SyncWithTicks"/>.</summary>
    public static readonly StyledProperty<bool> SyncWithTicksProperty = AvaloniaProperty.Register<CartesianGrid, bool>(nameof(SyncWithTicks));

    /// <summary>Defines <see cref="HorizontalValues"/>.</summary>
    public static readonly StyledProperty<string?> HorizontalValuesProperty = AvaloniaProperty.Register<CartesianGrid, string?>(nameof(HorizontalValues));

    /// <summary>Defines <see cref="VerticalValues"/>.</summary>
    public static readonly StyledProperty<string?> VerticalValuesProperty = AvaloniaProperty.Register<CartesianGrid, string?>(nameof(VerticalValues));

    /// <summary>Defines <see cref="X"/>.</summary>
    public static readonly StyledProperty<double?> XProperty = AvaloniaProperty.Register<CartesianGrid, double?>(nameof(X));

    /// <summary>Defines <see cref="Y"/>.</summary>
    public static readonly StyledProperty<double?> YProperty = AvaloniaProperty.Register<CartesianGrid, double?>(nameof(Y));

    /// <summary>Defines <see cref="GridWidth"/>.</summary>
    public static readonly StyledProperty<double?> GridWidthProperty = AvaloniaProperty.Register<CartesianGrid, double?>(nameof(GridWidth));

    /// <summary>Defines <see cref="GridHeight"/>.</summary>
    public static readonly StyledProperty<double?> GridHeightProperty = AvaloniaProperty.Register<CartesianGrid, double?>(nameof(GridHeight));

    private readonly ChangeRelay _relay = new();

    /// <summary>Creates a grid that follows its own classes (Recharts' <c>className</c> on its lines).</summary>
    public CartesianGrid() => _relay.FollowClasses(this);

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>Whether horizontal lines are drawn.</summary>
    public bool Horizontal { get => GetValue(HorizontalProperty); set => SetValue(HorizontalProperty, value); }

    /// <summary>Whether vertical lines are drawn.</summary>
    public bool Vertical { get => GetValue(VerticalProperty); set => SetValue(VerticalProperty, value); }

    /// <summary>The lines' color.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The lines' dash pattern (<c>3 3</c>).</summary>
    public object? StrokeDashArray { get => GetValue(StrokeDashArrayProperty); set => SetValue(StrokeDashArrayProperty, value); }

    /// <summary>The plot's background.</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The background's and stripes' fill opacity.</summary>
    public double? FillOpacity { get => GetValue(FillOpacityProperty); set => SetValue(FillOpacityProperty, value); }

    /// <summary>The lines' width.</summary>
    public double? StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    /// <summary>The lines' opacity.</summary>
    public double? StrokeOpacity { get => GetValue(StrokeOpacityProperty); set => SetValue(StrokeOpacityProperty, value); }

    /// <summary>The fills the horizontal stripes cycle through, separated by spaces or commas (Recharts' <c>horizontalFill</c>).</summary>
    public string? HorizontalFill { get => GetValue(HorizontalFillProperty); set => SetValue(HorizontalFillProperty, value); }

    /// <summary>The fills the vertical stripes cycle through (Recharts' <c>verticalFill</c>).</summary>
    public string? VerticalFill { get => GetValue(VerticalFillProperty); set => SetValue(VerticalFillProperty, value); }

    /// <summary>The y of every horizontal line, instead of the y-axis' ticks (Recharts' <c>horizontalPoints</c>).</summary>
    public string? HorizontalPoints { get => GetValue(HorizontalPointsProperty); set => SetValue(HorizontalPointsProperty, value); }

    /// <summary>The x of every vertical line (Recharts' <c>verticalPoints</c>).</summary>
    public string? VerticalPoints { get => GetValue(VerticalPointsProperty); set => SetValue(VerticalPointsProperty, value); }

    /// <summary>Whether the lines are at the ticks only, without the plot's edges (Recharts' <c>syncWithTicks</c>).</summary>
    public bool SyncWithTicks { get => GetValue(SyncWithTicksProperty); set => SetValue(SyncWithTicksProperty, value); }

    /// <summary>The y-axis values the horizontal lines stand at, separated by commas (Recharts' <c>horizontalValues</c>); kept as the axis keeps its ticks, without the plot's edges.</summary>
    public string? HorizontalValues { get => GetValue(HorizontalValuesProperty); set => SetValue(HorizontalValuesProperty, value); }

    /// <summary>The x-axis values the vertical lines stand at, separated by commas (Recharts' <c>verticalValues</c>).</summary>
    public string? VerticalValues { get => GetValue(VerticalValuesProperty); set => SetValue(VerticalValuesProperty, value); }

    /// <summary>The grid's left edge; the plot's by default.</summary>
    public double? X { get => GetValue(XProperty); set => SetValue(XProperty, value); }

    /// <summary>The grid's top edge; the plot's by default.</summary>
    public double? Y { get => GetValue(YProperty); set => SetValue(YProperty, value); }

    /// <summary>The grid's width (Recharts' <c>width</c>); the plot's by default.</summary>
    public double? GridWidth { get => GetValue(GridWidthProperty); set => SetValue(GridWidthProperty, value); }

    /// <summary>The grid's height (Recharts' <c>height</c>); the plot's by default.</summary>
    public double? GridHeight { get => GetValue(GridHeightProperty); set => SetValue(GridHeightProperty, value); }

    void ISpecSource.Apply(SpecBuilder builder, int order) => builder.Grid = new GridSpec
    {
        Presentation = Presentation.Of(this),
        ClassName = ChangeRelay.ClassName(this),
        Horizontal = Horizontal,
        Vertical = Vertical,
        Stroke = Stroke,
        StrokeDashArray = SpecText.Dashes(StrokeDashArray),
        Fill = Fill,
        FillOpacity = FillOpacity,
        StrokeWidth = StrokeWidth,
        StrokeOpacity = StrokeOpacity,
        HorizontalFill = SpecText.Words(HorizontalFill),
        VerticalFill = SpecText.Words(VerticalFill),
        HorizontalPoints = SpecText.Numbers(HorizontalPoints),
        VerticalPoints = SpecText.Numbers(VerticalPoints),
        SyncWithTicks = SyncWithTicks,
        HorizontalValues = SpecText.Texts(HorizontalValues),
        VerticalValues = SpecText.Texts(VerticalValues),
        X = X,
        Y = Y,
        Width = GridWidth,
        Height = GridHeight,
        Order = order,
    };

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}

/// <summary>Recharts' <c>ReferenceLine</c>: a vertical line at a category or value of the x-axis, or a horizontal one at a value of the y-axis.</summary>
public sealed class ReferenceLine : StyledElement, IChartElement, ISpecSource
{
    /// <summary>Defines <see cref="ShapeTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ShapeTemplateProperty = AvaloniaProperty.Register<ReferenceLine, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ShapeTemplate));

    /// <summary>The custom content drawn in place of the reference's shape (Recharts' <c>shape</c> element): a template whose data context is its <see cref="ReferenceLineContext"/>.</summary>
    public Avalonia.Controls.Templates.IDataTemplate? ShapeTemplate { get => GetValue(ShapeTemplateProperty); set => SetValue(ShapeTemplateProperty, value); }

    /// <summary>Defines <see cref="X"/>.</summary>
    public static readonly StyledProperty<string?> XProperty = AvaloniaProperty.Register<ReferenceLine, string?>(nameof(X));

    /// <summary>Defines <see cref="Y"/>.</summary>
    public static readonly StyledProperty<string?> YProperty = AvaloniaProperty.Register<ReferenceLine, string?>(nameof(Y));

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<ReferenceLine, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double> StrokeWidthProperty = AvaloniaProperty.Register<ReferenceLine, double>(nameof(StrokeWidth), 1);

    /// <summary>Defines <see cref="StrokeDashArray"/>.</summary>
    public static readonly StyledProperty<object?> StrokeDashArrayProperty = AvaloniaProperty.Register<ReferenceLine, object?>(nameof(StrokeDashArray));

    /// <summary>Defines <see cref="Label"/>.</summary>
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<ReferenceLine, string?>(nameof(Label));

    /// <summary>Defines <see cref="LabelPosition"/>.</summary>
    public static readonly StyledProperty<LabelPosition?> LabelPositionProperty = AvaloniaProperty.Register<ReferenceLine, LabelPosition?>(nameof(LabelPosition));

    /// <summary>Defines <see cref="LabelAngle"/>.</summary>
    public static readonly StyledProperty<double> LabelAngleProperty = Axis.LabelAngleProperty.AddOwner<ReferenceLine>();

    /// <summary>Defines <see cref="LabelOffset"/>.</summary>
    public static readonly StyledProperty<double> LabelOffsetProperty = Axis.LabelOffsetProperty.AddOwner<ReferenceLine>();

    /// <summary>Defines <see cref="LabelFill"/>.</summary>
    public static readonly StyledProperty<string?> LabelFillProperty = Axis.LabelFillProperty.AddOwner<ReferenceLine>();

    /// <summary>Defines <see cref="LabelFontSize"/>.</summary>
    public static readonly StyledProperty<double?> LabelFontSizeProperty = Axis.LabelFontSizeProperty.AddOwner<ReferenceLine>();

    /// <summary>Defines <see cref="XAxisId"/>.</summary>
    public static readonly StyledProperty<string> XAxisIdProperty = AvaloniaProperty.Register<ReferenceLine, string>(nameof(XAxisId), "0");

    /// <summary>Defines <see cref="YAxisId"/>.</summary>
    public static readonly StyledProperty<string> YAxisIdProperty = AvaloniaProperty.Register<ReferenceLine, string>(nameof(YAxisId), "0");

    /// <summary>Defines <see cref="IfOverflow"/>.</summary>
    public static readonly StyledProperty<string> IfOverflowProperty = AvaloniaProperty.Register<ReferenceLine, string>(nameof(IfOverflow), "discard");

    /// <summary>Defines <see cref="Position"/>.</summary>
    public static readonly StyledProperty<BandPosition> PositionProperty = AvaloniaProperty.Register<ReferenceLine, BandPosition>(nameof(Position), BandPosition.Middle);

    /// <summary>Defines <see cref="X1"/>.</summary>
    public static readonly StyledProperty<string?> X1Property = AvaloniaProperty.Register<ReferenceLine, string?>(nameof(X1));

    /// <summary>Defines <see cref="Y1"/>.</summary>
    public static readonly StyledProperty<string?> Y1Property = AvaloniaProperty.Register<ReferenceLine, string?>(nameof(Y1));

    /// <summary>Defines <see cref="X2"/>.</summary>
    public static readonly StyledProperty<string?> X2Property = AvaloniaProperty.Register<ReferenceLine, string?>(nameof(X2));

    /// <summary>Defines <see cref="Y2"/>.</summary>
    public static readonly StyledProperty<string?> Y2Property = AvaloniaProperty.Register<ReferenceLine, string?>(nameof(Y2));

    private readonly ChangeRelay _relay = new();

    /// <summary>Creates the element, following its label children.</summary>
    public ReferenceLine() => _relay.Follow(Labels, this, LogicalChildren);

    /// <summary>The element's <c>Label</c> children, drawn after its <see cref="Label"/> text.</summary>
    [Content]
    public AvaloniaList<Label> Labels { get; } = [];

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>The x value (a category, or a number on a numeric axis) of a vertical line.</summary>
    public string? X { get => GetValue(XProperty); set => SetValue(XProperty, value); }

    /// <summary>The y value of a horizontal line.</summary>
    public string? Y { get => GetValue(YProperty); set => SetValue(YProperty, value); }

    /// <summary>The line's color.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The line's width.</summary>
    public double StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    /// <summary>The line's dash pattern.</summary>
    public object? StrokeDashArray { get => GetValue(StrokeDashArrayProperty); set => SetValue(StrokeDashArrayProperty, value); }

    /// <summary>The line's label.</summary>
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Where the label sits; centered on the line by default.</summary>
    public LabelPosition? LabelPosition { get => GetValue(LabelPositionProperty); set => SetValue(LabelPositionProperty, value); }

    /// <summary>The label's rotation in degrees (Recharts' label <c>angle</c>).</summary>
    public double LabelAngle { get => GetValue(LabelAngleProperty); set => SetValue(LabelAngleProperty, value); }

    /// <summary>The label's distance from its box (Recharts' label <c>offset</c>, 5 by default).</summary>
    public double LabelOffset { get => GetValue(LabelOffsetProperty); set => SetValue(LabelOffsetProperty, value); }

    /// <summary>The label's color (Recharts' label <c>fill</c>).</summary>
    public string? LabelFill { get => GetValue(LabelFillProperty); set => SetValue(LabelFillProperty, value); }

    /// <summary>The label's font size; the chart's by default.</summary>
    public double? LabelFontSize { get => GetValue(LabelFontSizeProperty); set => SetValue(LabelFontSizeProperty, value); }

    /// <summary>The x-axis the line maps through.</summary>
    public string XAxisId { get => GetValue(XAxisIdProperty); set => SetValue(XAxisIdProperty, value); }

    /// <summary>The y-axis the line maps through.</summary>
    public string YAxisId { get => GetValue(YAxisIdProperty); set => SetValue(YAxisIdProperty, value); }

    /// <summary>What a value outside the domain does: <c>discard</c> (the line is dropped), <c>visible</c> (drawn where it falls), <c>hidden</c> (clipped to the plot) or <c>extendDomain</c>.</summary>
    public string IfOverflow { get => GetValue(IfOverflowProperty); set => SetValue(IfOverflowProperty, value); }

    /// <summary>Where on a category's band the line's values fall: its start, middle (the default) or end (Recharts' <c>position</c>).</summary>
    public BandPosition Position { get => GetValue(PositionProperty); set => SetValue(PositionProperty, value); }

    /// <summary>The x value of a segment's first point (Recharts' <c>segment[0].x</c>); a segment is drawn when <see cref="X"/> and <see cref="Y"/> are unset.</summary>
    public string? X1 { get => GetValue(X1Property); set => SetValue(X1Property, value); }

    /// <summary>The y value of a segment's first point.</summary>
    public string? Y1 { get => GetValue(Y1Property); set => SetValue(Y1Property, value); }

    /// <summary>The x value of a segment's second point.</summary>
    public string? X2 { get => GetValue(X2Property); set => SetValue(X2Property, value); }

    /// <summary>The y value of a segment's second point.</summary>
    public string? Y2 { get => GetValue(Y2Property); set => SetValue(Y2Property, value); }

    void ISpecSource.Apply(SpecBuilder builder, int order) => builder.References.Add(new ReferenceLineSpec
    {
        ShapeContent = builder.Content(ShapeTemplate, Content.ContentSlots.ReferenceLine, this, nameof(ShapeTemplate)),
        X = X,
        Y = Y,
        Stroke = Stroke,
        StrokeWidth = StrokeWidth,
        StrokeDashArray = SpecText.Dashes(StrokeDashArray),
        Label = SpecText.Label(Label, LabelPosition, this),
        Labels = Pictor.Label.Specs(Labels, builder),
        XAxisId = XAxisId,
        YAxisId = YAxisId,
        IfOverflow = SpecText.Overflow(IfOverflow),
        Position = Position,
        Segment = X1 is null && Y1 is null && X2 is null && Y2 is null ? null : new ReferenceSegment(X1, Y1, X2, Y2),
        Order = order,
    });

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}

/// <summary>Recharts' <c>LabelList</c>: a label at every point, bar or sector of its series; a series may hold several.</summary>
public sealed class LabelList : StyledElement, ISeriesPart
{
    /// <summary>Defines <see cref="DataKey"/>.</summary>
    public static readonly StyledProperty<string?> DataKeyProperty = AvaloniaProperty.Register<LabelList, string?>(nameof(DataKey));

    /// <summary>Defines <see cref="Position"/>.</summary>
    public static readonly StyledProperty<LabelPosition?> PositionProperty = AvaloniaProperty.Register<LabelList, LabelPosition?>(nameof(Position));

    /// <summary>Defines <see cref="PositionX"/>.</summary>
    public static readonly StyledProperty<string?> PositionXProperty = AvaloniaProperty.Register<LabelList, string?>(nameof(PositionX));

    /// <summary>Defines <see cref="PositionY"/>.</summary>
    public static readonly StyledProperty<string?> PositionYProperty = AvaloniaProperty.Register<LabelList, string?>(nameof(PositionY));

    /// <summary>Defines <see cref="Offset"/>.</summary>
    public static readonly StyledProperty<double> OffsetProperty = AvaloniaProperty.Register<LabelList, double>(nameof(Offset), 5);

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<LabelList, string?>(nameof(Fill));

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<LabelList, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> StrokeWidthProperty = AvaloniaProperty.Register<LabelList, double?>(nameof(StrokeWidth));

    /// <summary>Defines <see cref="FontSize"/>.</summary>
    public static readonly StyledProperty<double?> FontSizeProperty = AvaloniaProperty.Register<LabelList, double?>(nameof(FontSize));

    /// <summary>Defines <see cref="FontWeight"/>.</summary>
    public static readonly StyledProperty<string?> FontWeightProperty = AvaloniaProperty.Register<LabelList, string?>(nameof(FontWeight));

    /// <summary>Defines <see cref="Angle"/>.</summary>
    public static readonly StyledProperty<double> AngleProperty = AvaloniaProperty.Register<LabelList, double>(nameof(Angle));

    /// <summary>Defines <see cref="ClockWise"/>.</summary>
    public static readonly StyledProperty<bool> ClockWiseProperty = AvaloniaProperty.Register<LabelList, bool>(nameof(ClockWise));

    /// <summary>Defines <see cref="TextBreakAll"/>.</summary>
    public static readonly StyledProperty<bool> TextBreakAllProperty = AvaloniaProperty.Register<LabelList, bool>(nameof(TextBreakAll));

    /// <summary>Defines <see cref="Format"/>.</summary>
    public static readonly StyledProperty<string?> FormatProperty = AvaloniaProperty.Register<LabelList, string?>(nameof(Format));

    /// <summary>Defines <see cref="ContentTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ContentTemplateProperty = AvaloniaProperty.Register<LabelList, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ContentTemplate));

    private readonly ChangeRelay _relay = new();

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>The data key of the labels' values; the series' by default.</summary>
    public string? DataKey { get => GetValue(DataKeyProperty); set => SetValue(DataKeyProperty, value); }

    /// <summary>Where each label sits; centered by default (the ring's middle on a polar shape).</summary>
    public LabelPosition? Position { get => GetValue(PositionProperty); set => SetValue(PositionProperty, value); }

    /// <summary>With <see cref="PositionY"/>, a position given as an offset into the shape's box (Recharts' <c>position={{x, y}}</c>): pixels or a percentage.</summary>
    public string? PositionX { get => GetValue(PositionXProperty); set => SetValue(PositionXProperty, value); }

    /// <summary>The y of a position given as an offset into the shape's box.</summary>
    public string? PositionY { get => GetValue(PositionYProperty); set => SetValue(PositionYProperty, value); }

    /// <summary>The distance from the point or the bar.</summary>
    public double Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    /// <summary>The labels' color; the shape's (a cell's, a sector's) or gray by default.</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The labels' stroke; a pie sector's stroke by default, as Recharts spreads it onto its labels.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The labels' stroke width.</summary>
    public double? StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    /// <summary>The labels' own font size.</summary>
    public double? FontSize { get => GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

    /// <summary>The labels' weight (<c>bold</c>, <c>600</c>).</summary>
    public string? FontWeight { get => GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }

    /// <summary>The labels' rotation in degrees.</summary>
    public double Angle { get => GetValue(AngleProperty); set => SetValue(AngleProperty, value); }

    /// <summary>Whether a label along a radial bar's arc runs clockwise.</summary>
    public bool ClockWise { get => GetValue(ClockWiseProperty); set => SetValue(ClockWiseProperty, value); }

    /// <summary>Whether a label wrapped to its bar's width breaks between any two characters.</summary>
    public bool TextBreakAll { get => GetValue(TextBreakAllProperty); set => SetValue(TextBreakAllProperty, value); }

    /// <summary>A .NET composite format for the values.</summary>
    public string? Format { get => GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>
    /// The custom content drawn in place of each label (Recharts' <c>content</c> element): a template whose data context
    /// is the label's <see cref="LabelContext"/> (the box it is placed around, its value and text). Drawn on the label
    /// lists of lines, areas and bars; the label lists of pies, radial bars, radars and funnels draw their default labels.
    /// </summary>
    public Avalonia.Controls.Templates.IDataTemplate? ContentTemplate { get => GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

    internal LabelSpec Spec(SpecBuilder builder) => new()
    {
        Content = builder.Content(ContentTemplate, Content.ContentSlots.Label, this, nameof(ContentTemplate)),
        DataKey = DataKey,
        Position = Position,
        PositionX = PositionX,
        PositionY = PositionY,
        Offset = Offset,
        Fill = Fill,
        Stroke = Stroke,
        StrokeWidth = StrokeWidth,
        FontSize = FontSize,
        FontWeight = FontWeight,
        Angle = Angle,
        ClockWise = ClockWise,
        BreakAll = TextBreakAll,
        Format = Format,
    };

    /// <summary>The label lists of a series' parts, after its implicit <c>label</c> list if it has one: the first, then the others.</summary>
    internal static (LabelSpec? First, System.Collections.Immutable.ImmutableArray<LabelSpec> Others) Of(IEnumerable<ISeriesPart> parts, LabelSpec? implicitLabel, SpecBuilder builder)
    {
        var lists = parts.OfType<LabelList>().Select(list => list.Spec(builder)).ToList();
        if (implicitLabel is not null)
        {
            lists.Insert(0, implicitLabel);
        }

        return lists.Count == 0 ? (null, []) : (lists[0], [.. lists.Skip(1)]);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}

/// <summary>Recharts' <c>Cell</c>: the colors of one bar, in data order.</summary>
public sealed class Cell : StyledElement, ISeriesPart
{
    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<Cell, string?>(nameof(Fill));

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<Cell, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> StrokeWidthProperty = AvaloniaProperty.Register<Cell, double?>(nameof(StrokeWidth));

    /// <summary>Defines <see cref="FillOpacity"/>.</summary>
    public static readonly StyledProperty<double?> FillOpacityProperty = AvaloniaProperty.Register<Cell, double?>(nameof(FillOpacity));

    /// <summary>Defines <see cref="Opacity"/>.</summary>
    public static readonly StyledProperty<double?> OpacityProperty = AvaloniaProperty.Register<Cell, double?>(nameof(Opacity));

    /// <summary>Defines <see cref="StrokeDashArray"/>.</summary>
    public static readonly StyledProperty<object?> StrokeDashArrayProperty = AvaloniaProperty.Register<Cell, object?>(nameof(StrokeDashArray));

    private readonly ChangeRelay _relay = new();

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>The bar's fill.</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The bar's stroke.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The shape's stroke width.</summary>
    public double? StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    /// <summary>The shape's fill opacity.</summary>
    public double? FillOpacity { get => GetValue(FillOpacityProperty); set => SetValue(FillOpacityProperty, value); }

    /// <summary>The shape's opacity.</summary>
    public double? Opacity { get => GetValue(OpacityProperty); set => SetValue(OpacityProperty, value); }

    /// <summary>The shape's dash pattern.</summary>
    public object? StrokeDashArray { get => GetValue(StrokeDashArrayProperty); set => SetValue(StrokeDashArrayProperty, value); }

    internal CellSpec Spec() => new(Fill, Stroke) { StrokeWidth = StrokeWidth, FillOpacity = FillOpacity, Opacity = Opacity, StrokeDashArray = SpecText.Dashes(StrokeDashArray) };

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}
