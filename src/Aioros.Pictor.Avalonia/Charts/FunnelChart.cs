using System.Collections;
using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Aioros.Pictor;

/// <summary>Recharts' <c>FunnelChart</c>: its funnels, a tooltip per trapezoid and a legend.</summary>
public sealed class FunnelChart : Chart
{
    private protected override ChartKind Kind => ChartKind.Funnel;

    private protected override ChartSpec Complete(ChartSpec spec, SpecBuilder builder) => spec with { Funnels = [.. builder.Funnels] };
}

/// <summary>Recharts' <c>Funnel</c>: one trapezoid per item, as wide as its value against the largest.</summary>
public sealed class Funnel : StyledElement, IChartElement, ISpecSource, IOwnItems, IClickSource, IAnimatedElement
{
    /// <summary>Defines <see cref="ShapeTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ShapeTemplateProperty = AvaloniaProperty.Register<Funnel, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ShapeTemplate));

    /// <summary>Defines <see cref="ActiveShapeTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ActiveShapeTemplateProperty = AvaloniaProperty.Register<Funnel, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ActiveShapeTemplate));

    /// <summary>Defines <see cref="ActiveIndex"/>.</summary>
    public static readonly StyledProperty<int?> ActiveIndexProperty = AvaloniaProperty.Register<Funnel, int?>(nameof(ActiveIndex));

    /// <summary>Defines <see cref="ActiveIndexes"/>.</summary>
    public static readonly StyledProperty<string?> ActiveIndexesProperty = AvaloniaProperty.Register<Funnel, string?>(nameof(ActiveIndexes));

    /// <summary>The trapezoid drawn active whatever the pointer does (Recharts' <c>activeIndex</c>): its <see cref="ActiveShapeTemplate"/>, or its default shape in Recharts' active layer.</summary>
    public int? ActiveIndex { get => GetValue(ActiveIndexProperty); set => SetValue(ActiveIndexProperty, value); }

    /// <summary>The trapezoids drawn active whatever the pointer does, as Recharts' <c>activeIndex</c> array: indexes separated by spaces or commas.</summary>
    public string? ActiveIndexes { get => GetValue(ActiveIndexesProperty); set => SetValue(ActiveIndexesProperty, value); }
    /// <summary>Raised on the desktop when the element's enter or update animation starts, before its begin delay (Recharts' <c>onAnimationStart</c>); a web chart, written without script, raises nothing.</summary>
    public event EventHandler? AnimationStarted;

    /// <summary>Raised on the desktop when the element's enter or update animation has moved to its end (Recharts' <c>onAnimationEnd</c>); a web chart raises nothing.</summary>
    public event EventHandler? AnimationEnded;

    Motion.Timing? IAnimatedElement.Timing => Motion.Animations.Of(new Specs.FunnelSpec { Animation = Animated.Spec(this) });

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

    /// <summary>Defines <see cref="ClickCommand"/>.</summary>
    public static readonly StyledProperty<System.Windows.Input.ICommand?> ClickCommandProperty = Clickable.CommandProperty.AddOwner<Funnel>();

    /// <summary>Defines <see cref="IsAnimationActive"/>.</summary>
    public static readonly StyledProperty<bool> IsAnimationActiveProperty = Animated.IsActiveProperty.AddOwner<Funnel>();

    /// <summary>Defines <see cref="AnimationBegin"/>.</summary>
    public static readonly StyledProperty<double?> AnimationBeginProperty = Animated.BeginProperty.AddOwner<Funnel>();

    /// <summary>Defines <see cref="AnimationDuration"/>.</summary>
    public static readonly StyledProperty<double?> AnimationDurationProperty = Animated.DurationProperty.AddOwner<Funnel>();

    /// <summary>Defines <see cref="AnimationEasing"/>.</summary>
    public static readonly StyledProperty<AnimationEasing?> AnimationEasingProperty = Animated.EasingProperty.AddOwner<Funnel>();

    /// <summary>Defines <see cref="DataKey"/>.</summary>
    public static readonly StyledProperty<string> DataKeyProperty = AvaloniaProperty.Register<Funnel, string>(nameof(DataKey), "value");

    /// <summary>Defines <see cref="NameKey"/>.</summary>
    public static readonly StyledProperty<string> NameKeyProperty = AvaloniaProperty.Register<Funnel, string>(nameof(NameKey), "name");

    /// <summary>Defines <see cref="FillKey"/>.</summary>
    public static readonly StyledProperty<string?> FillKeyProperty = AvaloniaProperty.Register<Funnel, string?>(nameof(FillKey));

    /// <summary>Defines <see cref="OpacityKey"/>.</summary>
    public static readonly StyledProperty<string?> OpacityKeyProperty = AvaloniaProperty.Register<Funnel, string?>(nameof(OpacityKey));

    /// <summary>Defines <see cref="StrokeKey"/>.</summary>
    public static readonly StyledProperty<string?> StrokeKeyProperty = AvaloniaProperty.Register<Funnel, string?>(nameof(StrokeKey));

    /// <summary>Defines <see cref="Name"/>.</summary>
    public static new readonly StyledProperty<string?> NameProperty = AvaloniaProperty.Register<Funnel, string?>(nameof(Name));

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<Funnel, string?>(nameof(Fill));

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<Funnel, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="Reversed"/>.</summary>
    public static readonly StyledProperty<bool> ReversedProperty = AvaloniaProperty.Register<Funnel, bool>(nameof(Reversed));

    /// <summary>Defines <see cref="LastShapeType"/>.</summary>
    public static readonly StyledProperty<string> LastShapeTypeProperty = AvaloniaProperty.Register<Funnel, string>(nameof(LastShapeType), "triangle");

    /// <summary>Defines <see cref="Width"/>.</summary>
    public static readonly StyledProperty<string?> WidthProperty = AvaloniaProperty.Register<Funnel, string?>(nameof(Width));

    /// <summary>Defines <see cref="LegendType"/>.</summary>
    public static readonly StyledProperty<LegendIcon> LegendTypeProperty = AvaloniaProperty.Register<Funnel, LegendIcon>(nameof(LegendType), LegendIcon.Rect);

    /// <summary>Defines <see cref="Hide"/>.</summary>
    public static readonly StyledProperty<bool> HideProperty = AvaloniaProperty.Register<Funnel, bool>(nameof(Hide));

    /// <summary>Defines <see cref="TooltipType"/>.</summary>
    public static readonly StyledProperty<string?> TooltipTypeProperty = AvaloniaProperty.Register<Funnel, string?>(nameof(TooltipType));

    /// <summary>Defines <see cref="Data"/>.</summary>
    public static readonly StyledProperty<IEnumerable?> DataProperty = AvaloniaProperty.Register<Funnel, IEnumerable?>(nameof(Data));

    /// <summary>Defines <see cref="Parts"/>.</summary>
    public static readonly StyledProperty<AvaloniaList<ISeriesPart>> PartsProperty = AvaloniaProperty.Register<Funnel, AvaloniaList<ISeriesPart>>(nameof(Parts));

    private readonly ChangeRelay _relay = new();

    /// <summary>Creates a funnel.</summary>
    public Funnel()
    {
        var parts = new AvaloniaList<ISeriesPart>();
        _relay.Follow(parts, this, LogicalChildren);
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

    /// <summary>The data key of the values.</summary>
    public string DataKey { get => GetValue(DataKeyProperty); set => SetValue(DataKeyProperty, value); }

    /// <summary>The data key of the items' names.</summary>
    public string NameKey { get => GetValue(NameKeyProperty); set => SetValue(NameKeyProperty, value); }

    /// <summary>The key of each item's own fill (Recharts reads an item's <c>fill</c>); none by default.</summary>
    public string? FillKey { get => GetValue(FillKeyProperty); set => SetValue(FillKeyProperty, value); }

    /// <summary>The data key of each item's own opacity, as Recharts spreads an item's <c>opacity</c> onto its trapezoid.</summary>
    public string? OpacityKey { get => GetValue(OpacityKeyProperty); set => SetValue(OpacityKeyProperty, value); }

    /// <summary>The key of each item's own stroke; none by default.</summary>
    public string? StrokeKey { get => GetValue(StrokeKeyProperty); set => SetValue(StrokeKeyProperty, value); }

    /// <summary>The funnel's name in the legend; the data key by default.</summary>
    public new string? Name { get => GetValue(NameProperty); set => SetValue(NameProperty, value); }

    /// <summary>The trapezoids' fill where an item (its <c>fill</c> member) and its cell have none.</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The trapezoids' stroke.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>Whether the widest item is at the bottom.</summary>
    public bool Reversed { get => GetValue(ReversedProperty); set => SetValue(ReversedProperty, value); }

    /// <summary>The last item's shape: <c>triangle</c> (to a point) or <c>rectangle</c>.</summary>
    public string LastShapeType { get => GetValue(LastShapeTypeProperty); set => SetValue(LastShapeTypeProperty, value); }

    /// <summary>The funnel's width in pixels or as a percentage of the plot (Recharts' <c>width</c>); the whole plot by default.</summary>
    public string? Width { get => GetValue(WidthProperty); set => SetValue(WidthProperty, value); }

    /// <summary>The legend icon.</summary>
    public LegendIcon LegendType { get => GetValue(LegendTypeProperty); set => SetValue(LegendTypeProperty, value); }

    /// <summary>Whether the funnel is hidden.</summary>
    public bool Hide { get => GetValue(HideProperty); set => SetValue(HideProperty, value); }

    /// <summary><c>none</c> to leave the funnel's entries out of the tooltip (Recharts' <c>tooltipType</c>).</summary>
    public string? TooltipType { get => GetValue(TooltipTypeProperty); set => SetValue(TooltipTypeProperty, value); }

    /// <summary>The funnel's own items (Recharts' <c>data</c>); the chart's data when null.</summary>
    public IEnumerable? Data { get => GetValue(DataProperty); set => SetValue(DataProperty, value); }

    /// <summary>The command a click on one of its items runs, with the item's index in its data as the parameter (Recharts' <c>onClick</c>): a button over the item on the web, the pointer on the desktop.</summary>
    public System.Windows.Input.ICommand? ClickCommand { get => GetValue(ClickCommandProperty); set => SetValue(ClickCommandProperty, value); }

    AvaloniaProperty IClickSource.CommandProperty => ClickCommandProperty;

    IEnumerable? IOwnItems.OwnItems => Data;

    /// <summary>The funnel's label list and cells.</summary>
    [Content]
    public AvaloniaList<ISeriesPart> Parts => GetValue(PartsProperty);

    /// <summary>
    /// The custom content drawn in place of each trapezoid (Recharts' <c>shape</c> element): a template whose data context
    /// is the trapezoid's <see cref="TrapezoidContext"/>.
    /// </summary>
    public Avalonia.Controls.Templates.IDataTemplate? ShapeTemplate { get => GetValue(ShapeTemplateProperty); set => SetValue(ShapeTemplateProperty, value); }

    /// <summary>The custom content drawn in place of the hovered trapezoid (Recharts' <c>activeShape</c> element), with its <see cref="TrapezoidContext"/>.</summary>
    public Avalonia.Controls.Templates.IDataTemplate? ActiveShapeTemplate { get => GetValue(ActiveShapeTemplateProperty); set => SetValue(ActiveShapeTemplateProperty, value); }

    void ISpecSource.Apply(SpecBuilder builder, int order)
    {
        var (labels, others) = LabelList.Of(Parts, null, builder);
        var shape = builder.Content(ShapeTemplate, Content.ContentSlots.Trapezoid, this, nameof(ShapeTemplate));
        var active = builder.Content(ActiveShapeTemplate, Content.ContentSlots.Trapezoid, this, nameof(ActiveShapeTemplate));
        builder.Funnels.Add(new FunnelSpec
        {
            ShapeContent = shape,
            ActiveShapeContent = active,
            ActiveIndex = ActiveIndex,
            ActiveIndexes = [.. SpecText.Numbers(ActiveIndexes).Select(index => (int)index)],
            ClickCommand = Clickable.Key(this, order),
            DataKey = DataKey,
            NameKey = NameKey,
            Name = Name,
            Fill = Fill,
            Stroke = Stroke,
            Reversed = Reversed,
            LastRectangle = LastShapeType == "rectangle",
            Width = Width,
            LegendType = LegendType,
            Hide = Hide,
            TooltipNone = string.Equals(TooltipType, "none", StringComparison.OrdinalIgnoreCase),
            Animation = Animated.Spec(this),
            Labels = labels,
            LabelLists = others,
            Cells = [.. Parts.OfType<Cell>().Select(cell => cell.Spec())],
            FillKey = FillKey,
            OpacityKey = OpacityKey,
            StrokeKey = StrokeKey,
            Data = Data is { } data ? DataTable.Read(data, [DataKey, NameKey, FillKey, OpacityKey, StrokeKey, labels?.DataKey, .. shape?.Keys ?? [], .. active?.Keys ?? []], builder.Members) : null,
        });
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}
