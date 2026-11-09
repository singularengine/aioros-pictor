using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Data;
using Avalonia.Metadata;

namespace Aioros.Pictor;

/// <summary>
/// Recharts' <c>Brush</c>: a strip under the x-axis whose two travellers select the window of items the chart shows.
/// On the desktop the travellers and the slide between them are dragged, updating <see cref="StartIndex"/> and
/// <see cref="EndIndex"/> (bindable both ways); on the web its travellers are two range inputs posted as Aioros's
/// fields, bound to the same properties, and the page's answer is the chart over the new window.
/// </summary>
public sealed class Brush : StyledElement, IChartElement, ISpecSource
{
    /// <summary>Defines <see cref="TravellerTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> TravellerTemplateProperty = AvaloniaProperty.Register<Brush, Avalonia.Controls.Templates.IDataTemplate?>(nameof(TravellerTemplate));

    /// <summary>Defines <see cref="DataKey"/>.</summary>
    public static readonly StyledProperty<string?> DataKeyProperty = AvaloniaProperty.Register<Brush, string?>(nameof(DataKey));

    /// <summary>Defines <see cref="Height"/>.</summary>
    public static readonly StyledProperty<double> HeightProperty = AvaloniaProperty.Register<Brush, double>(nameof(Height), 40);

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<Brush, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<Brush, string?>(nameof(Fill));

    /// <summary>Defines <see cref="TravellerWidth"/>.</summary>
    public static readonly StyledProperty<double> TravellerWidthProperty = AvaloniaProperty.Register<Brush, double>(nameof(TravellerWidth), 5);

    /// <summary>Defines <see cref="Gap"/>.</summary>
    public static readonly StyledProperty<int> GapProperty = AvaloniaProperty.Register<Brush, int>(nameof(Gap), 1);

    /// <summary>Defines <see cref="StartIndex"/>.</summary>
    public static readonly StyledProperty<int?> StartIndexProperty = AvaloniaProperty.Register<Brush, int?>(nameof(StartIndex), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="EndIndex"/>.</summary>
    public static readonly StyledProperty<int?> EndIndexProperty = AvaloniaProperty.Register<Brush, int?>(nameof(EndIndex), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="TickFormat"/>.</summary>
    public static readonly StyledProperty<string?> TickFormatProperty = AvaloniaProperty.Register<Brush, string?>(nameof(TickFormat));

    /// <summary>Defines <see cref="AlwaysShowText"/>.</summary>
    public static readonly StyledProperty<bool> AlwaysShowTextProperty = AvaloniaProperty.Register<Brush, bool>(nameof(AlwaysShowText));

    /// <summary>Defines <see cref="AriaLabel"/>.</summary>
    public static readonly StyledProperty<string?> AriaLabelProperty = AvaloniaProperty.Register<Brush, string?>(nameof(AriaLabel));

    /// <summary>Defines <see cref="LeaveTimeOut"/>.</summary>
    public static readonly StyledProperty<double> LeaveTimeOutProperty = AvaloniaProperty.Register<Brush, double>(nameof(LeaveTimeOut), 1000);

    /// <summary>Defines <see cref="X"/>.</summary>
    public static readonly StyledProperty<double?> XProperty = AvaloniaProperty.Register<Brush, double?>(nameof(X));

    /// <summary>Defines <see cref="Y"/>.</summary>
    public static readonly StyledProperty<double?> YProperty = AvaloniaProperty.Register<Brush, double?>(nameof(Y));

    /// <summary>Defines <see cref="Width"/>.</summary>
    public static readonly StyledProperty<double?> WidthProperty = AvaloniaProperty.Register<Brush, double?>(nameof(Width));

    /// <summary>Defines <see cref="Padding"/>.</summary>
    public static readonly StyledProperty<Thickness> PaddingProperty = AvaloniaProperty.Register<Brush, Thickness>(nameof(Padding), new Thickness(1));

    /// <summary>Defines <see cref="Panorama"/>.</summary>
    public static readonly StyledProperty<CartesianChart?> PanoramaProperty = AvaloniaProperty.Register<Brush, CartesianChart?>(nameof(Panorama));

    private readonly ChangeRelay _relay = new();
    private readonly ChildChanges<IChartElement> _panoramaElements;

    /// <summary>A brush with Recharts' defaults.</summary>
    public Brush()
    {
        EventHandler changed = (_, _) => _relay.Raise(this);
        _panoramaElements = new ChildChanges<IChartElement>(element => element.Changed += changed, element => element.Changed -= changed);
    }

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>The data key of the texts shown at the window's ends while the brush is dragged.</summary>
    public string? DataKey { get => GetValue(DataKeyProperty); set => SetValue(DataKeyProperty, value); }

    /// <summary>The strip's height, which the plot gives up below the x-axis.</summary>
    public double Height { get => GetValue(HeightProperty); set => SetValue(HeightProperty, value); }

    /// <summary>The color of the frame, the slide and the travellers.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The strip's background.</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The width of a traveller.</summary>
    public double TravellerWidth { get => GetValue(TravellerWidthProperty); set => SetValue(TravellerWidthProperty, value); }

    /// <summary>How many items an index moves by at least.</summary>
    public int Gap { get => GetValue(GapProperty); set => SetValue(GapProperty, value); }

    /// <summary>The first item of the window; the first item when unset.</summary>
    public int? StartIndex { get => GetValue(StartIndexProperty); set => SetValue(StartIndexProperty, value); }

    /// <summary>The last item of the window; the last item when unset.</summary>
    public int? EndIndex { get => GetValue(EndIndexProperty); set => SetValue(EndIndexProperty, value); }

    /// <summary>A .NET composite format for the texts at the window's ends (Recharts' <c>tickFormatter</c>), given the item's text: <c>{0}</c>, <c>Day {0}</c>.</summary>
    public string? TickFormat { get => GetValue(TickFormatProperty); set => SetValue(TickFormatProperty, value); }

    /// <summary>Whether the texts at the window's ends are shown at rest too, not only while the brush moves (Recharts' <c>alwaysShowText</c>).</summary>
    public bool AlwaysShowText { get => GetValue(AlwaysShowTextProperty); set => SetValue(AlwaysShowTextProperty, value); }

    /// <summary>The travellers' accessible name on the web (Recharts' <c>ariaLabel</c>); <c>Min value: …, Max value: …</c> from the items' <c>name</c> by default.</summary>
    public string? AriaLabel { get => GetValue(AriaLabelProperty); set => SetValue(AriaLabelProperty, value); }

    /// <summary>On the desktop, how long the end texts stay after the pointer leaves the brush, in milliseconds (Recharts' <c>leaveTimeOut</c>); they show when it enters a traveller or the slide.</summary>
    public double LeaveTimeOut { get => GetValue(LeaveTimeOutProperty); set => SetValue(LeaveTimeOutProperty, value); }

    /// <summary>The strip's left edge in the chart; the plot's left edge when unset.</summary>
    public double? X { get => GetValue(XProperty); set => SetValue(XProperty, value); }

    /// <summary>The strip's top edge in the chart; under the x-axis when unset.</summary>
    public double? Y { get => GetValue(YProperty); set => SetValue(YProperty, value); }

    /// <summary>The strip's width; the plot's when unset.</summary>
    public double? Width { get => GetValue(WidthProperty); set => SetValue(WidthProperty, value); }

    /// <summary>The panorama's margin inside the strip (Recharts' <c>padding</c>), 1 on every side by default.</summary>
    public Thickness Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }

    /// <summary>
    /// A small chart of every item drawn inside the strip (Recharts' panorama: the brush's chart child), laid out at the
    /// strip's size with <see cref="Padding"/> as its margin; its own data is the chart's.
    /// </summary>
    [Content]
    public CartesianChart? Panorama { get => GetValue(PanoramaProperty); set => SetValue(PanoramaProperty, value); }

    /// <summary>
    /// The custom content drawn for each traveller (Recharts' <c>traveller</c> element): a template whose data context is
    /// the traveller's <see cref="TravellerContext"/> (its rectangle and the brush's paint); on the web the range inputs
    /// that move the brush stay over it.
    /// </summary>
    public Avalonia.Controls.Templates.IDataTemplate? TravellerTemplate { get => GetValue(TravellerTemplateProperty); set => SetValue(TravellerTemplateProperty, value); }

    void ISpecSource.Apply(SpecBuilder builder, int order) => builder.Brush = new BrushSpec
    {
        TravellerContent = builder.Content(TravellerTemplate, Content.ContentSlots.Traveller, this, nameof(TravellerTemplate)),
        TickFormat = TickFormat,
        AlwaysShowText = AlwaysShowText,
        AriaLabel = AriaLabel,
        LeaveTimeOut = LeaveTimeOut,
        X = X,
        Y = Y,
        Width = Width,
        Padding = new Edges(Padding.Top, Padding.Right, Padding.Bottom, Padding.Left),
        Panorama = Panorama?.Spec(0, 0),
        DataKey = DataKey,
        Height = Height,
        Stroke = Stroke,
        Fill = Fill,
        TravellerWidth = TravellerWidth,
        Gap = Gap,
        StartIndex = StartIndex,
        EndIndex = EndIndex,
        Order = order,
    };

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        ArgumentNullException.ThrowIfNull(change);
        base.OnPropertyChanged(change);
        if (change.Property == PanoramaProperty)
        {
            if (change.OldValue is CartesianChart old)
            {
                old.Elements.CollectionChanged -= OnPanoramaElements;
            }

            var panorama = change.NewValue as CartesianChart;
            if (panorama is not null)
            {
                panorama.Elements.CollectionChanged += OnPanoramaElements;
            }

            _panoramaElements.Follow(panorama?.Elements ?? []);
        }

        _relay.Raise(this);
    }

    private void OnPanoramaElements(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        _panoramaElements.Follow(Panorama?.Elements ?? []);
        _relay.Raise(this);
    }
}
