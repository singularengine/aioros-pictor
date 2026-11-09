using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Aioros.Pictor;

/// <summary>Recharts' <c>Legend</c> with its default content: the series' entries, or its own (<see cref="Payload"/>).</summary>
public sealed class Legend : StyledElement, IChartElement, ISpecSource, IClickSource
{
    /// <summary>Defines <see cref="ContentTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ContentTemplateProperty = AvaloniaProperty.Register<Legend, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>Defines <see cref="Template"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> TemplateProperty = AvaloniaProperty.Register<Legend, Avalonia.Controls.Templates.IDataTemplate?>(nameof(Template));

    /// <summary>Defines <see cref="ClickCommand"/>.</summary>
    public static readonly StyledProperty<System.Windows.Input.ICommand?> ClickCommandProperty = Clickable.CommandProperty.AddOwner<Legend>();

    /// <summary>Defines <see cref="IconSize"/>.</summary>
    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<Legend, double>(nameof(IconSize), 14);

    /// <summary>Defines <see cref="Align"/>.</summary>
    public static readonly StyledProperty<string> AlignProperty = AvaloniaProperty.Register<Legend, string>(nameof(Align), "center");

    /// <summary>Defines <see cref="VerticalAlign"/>.</summary>
    public static readonly StyledProperty<string> VerticalAlignProperty = AvaloniaProperty.Register<Legend, string>(nameof(VerticalAlign), "bottom");

    /// <summary>Defines <see cref="IconType"/>.</summary>
    public static readonly StyledProperty<LegendIcon?> IconTypeProperty = AvaloniaProperty.Register<Legend, LegendIcon?>(nameof(IconType));

    /// <summary>Defines <see cref="Layout"/>.</summary>
    public static readonly StyledProperty<string> LayoutProperty = AvaloniaProperty.Register<Legend, string>(nameof(Layout), "horizontal");

    /// <summary>Defines <see cref="Toggles"/>.</summary>
    public static readonly StyledProperty<bool> TogglesProperty = AvaloniaProperty.Register<Legend, bool>(nameof(Toggles));

    /// <summary>Defines <see cref="LineHeight"/>.</summary>
    public static readonly StyledProperty<double?> LineHeightProperty = AvaloniaProperty.Register<Legend, double?>(nameof(LineHeight));

    /// <summary>Defines <see cref="InactiveColor"/>.</summary>
    public static readonly StyledProperty<string?> InactiveColorProperty = AvaloniaProperty.Register<Legend, string?>(nameof(InactiveColor));

    /// <summary>Defines <see cref="Format"/>.</summary>
    public static readonly StyledProperty<string?> FormatProperty = AvaloniaProperty.Register<Legend, string?>(nameof(Format));

    /// <summary>Defines <see cref="Width"/>.</summary>
    public static readonly StyledProperty<double?> WidthProperty = AvaloniaProperty.Register<Legend, double?>(nameof(Width));

    /// <summary>Defines <see cref="Height"/>.</summary>
    public static readonly StyledProperty<double?> HeightProperty = AvaloniaProperty.Register<Legend, double?>(nameof(Height));

    /// <summary>Defines <see cref="PayloadUniqBy"/>.</summary>
    public static readonly StyledProperty<bool> PayloadUniqByProperty = AvaloniaProperty.Register<Legend, bool>(nameof(PayloadUniqBy));

    /// <summary>Defines <see cref="WrapperStyle"/>.</summary>
    public static readonly StyledProperty<string?> WrapperStyleProperty = AvaloniaProperty.Register<Legend, string?>(nameof(WrapperStyle));

    /// <summary>Defines <see cref="Payload"/>.</summary>
    public static readonly StyledProperty<AvaloniaList<ISeriesPart>> PayloadProperty = AvaloniaProperty.Register<Legend, AvaloniaList<ISeriesPart>>(nameof(Payload));

    private readonly ChangeRelay _relay = new();

    /// <summary>Creates a legend listing the series.</summary>
    public Legend()
    {
        var payload = new AvaloniaList<ISeriesPart>();
        _relay.Follow(payload, this, LogicalChildren);
        SetValue(PayloadProperty, payload);
    }

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>The icons' size.</summary>
    public double IconSize { get => GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }

    /// <summary>The horizontal alignment: <c>left</c>, <c>center</c> or <c>right</c>.</summary>
    public string Align { get => GetValue(AlignProperty); set => SetValue(AlignProperty, value); }

    /// <summary>The vertical alignment: <c>top</c>, <c>middle</c> or <c>bottom</c>.</summary>
    public string VerticalAlign { get => GetValue(VerticalAlignProperty); set => SetValue(VerticalAlignProperty, value); }

    /// <summary>One icon for every item.</summary>
    public LegendIcon? IconType { get => GetValue(IconTypeProperty); set => SetValue(IconTypeProperty, value); }

    /// <summary>Whether the items run in a row (<c>horizontal</c>) or one per line (<c>vertical</c>).</summary>
    public string Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>
    /// Whether a click on an item hides or shows its series (or a pie's or radial bar's item, or a funnel), without laying
    /// the chart out again; the web does it with a checkbox and CSS, the desktop with the click. Recharts leaves legend clicks to the app.
    /// </summary>
    public bool Toggles { get => GetValue(TogglesProperty); set => SetValue(TogglesProperty, value); }

    /// <summary>The line height of the items (Recharts' <c>wrapperStyle={{lineHeight}}</c>); the font's by default.</summary>
    public double? LineHeight { get => GetValue(LineHeightProperty); set => SetValue(LineHeightProperty, value); }

    /// <summary>The color of an inactive item: a hidden series' or a toggled one (Recharts' <c>inactiveColor</c>).</summary>
    public string? InactiveColor { get => GetValue(InactiveColorProperty); set => SetValue(InactiveColorProperty, value); }

    /// <summary>A .NET composite format for the items' texts (Recharts' <c>formatter</c>, <c>{0} (k)</c>).</summary>
    public string? Format { get => GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>The legend's width (Recharts' <c>width</c>); the chart's less its margins for a horizontal legend by default.</summary>
    public double? Width { get => GetValue(WidthProperty); set => SetValue(WidthProperty, value); }

    /// <summary>A vertical legend's height (Recharts' <c>height</c>).</summary>
    public double? Height { get => GetValue(HeightProperty); set => SetValue(HeightProperty, value); }

    /// <summary>Whether entries of the same text are listed once (Recharts' <c>payloadUniqBy</c>).</summary>
    public bool PayloadUniqBy { get => GetValue(PayloadUniqByProperty); set => SetValue(PayloadUniqByProperty, value); }

    /// <summary>
    /// CSS declarations of the legend's box (Recharts' <c>wrapperStyle</c>): its <c>left</c>, <c>right</c>, <c>top</c>,
    /// <c>bottom</c> and <c>line-height</c> place it on both targets; the web writes the rest as they are.
    /// </summary>
    public string? WrapperStyle { get => GetValue(WrapperStyleProperty); set => SetValue(WrapperStyleProperty, value); }

    /// <summary>The legend's own entries instead of the series' (Recharts' <c>payload</c>).</summary>
    [Content]
    public AvaloniaList<ISeriesPart> Payload => GetValue(PayloadProperty);

    /// <summary>The command a click on a legend item runs, with the item's index in the legend as the parameter (Recharts' legend <c>onClick</c>); it takes the click instead of <see cref="Toggles"/>.</summary>
    public System.Windows.Input.ICommand? ClickCommand { get => GetValue(ClickCommandProperty); set => SetValue(ClickCommandProperty, value); }

    AvaloniaProperty IClickSource.CommandProperty => ClickCommandProperty;

    /// <summary>
    /// The custom content of each entry (Recharts' <c>content</c> element, written per entry): a template whose data
    /// context is the entry's <see cref="LegendEntryContext"/>, written as HTML in a list (a single text block is the
    /// list item's own text); the legend's toggles and clicks are the default content's.
    /// </summary>
    public Avalonia.Controls.Templates.IDataTemplate? ContentTemplate { get => GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

    /// <summary>
    /// The custom content of the whole legend (Recharts' <c>content</c> element that is not one item per entry, such as
    /// a frame and a title around <c>DefaultLegendContent</c>): a template whose data context is the legend's
    /// <see cref="LegendContext"/>, written as HTML, holding a <see cref="LegendItems"/> where the entries go (Recharts'
    /// default list, or each entry's <see cref="ContentTemplate"/> in a list), as an items control's template holds its
    /// items presenter.
    /// </summary>
    public Avalonia.Controls.Templates.IDataTemplate? Template { get => GetValue(TemplateProperty); set => SetValue(TemplateProperty, value); }

    void ISpecSource.Apply(SpecBuilder builder, int order) => builder.Legend = new LegendSpec
    {
        Content = builder.Content(ContentTemplate, Content.ContentSlots.LegendEntry, this, nameof(ContentTemplate)),
        Frame = builder.Content(Template, Content.ContentSlots.Legend, this, nameof(Template)),
        ClickCommand = Clickable.Key(this, order),
        IconSize = IconSize,
        Align = Align,
        VerticalAlign = VerticalAlign,
        IconType = IconType,
        Vertical = Layout == "vertical",
        Toggles = Toggles,
        LineHeight = LineHeight,
        InactiveColor = InactiveColor,
        Format = Format,
        Width = Width,
        Height = Height,
        PayloadUniqBy = PayloadUniqBy,
        WrapperStyle = WrapperStyle,
        Payload = [.. Payload.OfType<LegendPayload>().Select(entry => new LegendPayloadSpec { Value = entry.Value, Color = entry.Color, Type = entry.Type, Inactive = entry.Inactive })],
    };

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}

/// <summary>An entry of a legend's own payload (Recharts' <c>payload</c> item): a text, a color and an icon.</summary>
public sealed class LegendPayload : StyledElement, ISeriesPart
{
    /// <summary>Defines <see cref="Value"/>.</summary>
    public static readonly StyledProperty<string> ValueProperty = AvaloniaProperty.Register<LegendPayload, string>(nameof(Value), string.Empty);

    /// <summary>Defines <see cref="Color"/>.</summary>
    public static readonly StyledProperty<string?> ColorProperty = AvaloniaProperty.Register<LegendPayload, string?>(nameof(Color));

    /// <summary>Defines <see cref="Type"/>.</summary>
    public static readonly StyledProperty<LegendIcon?> TypeProperty = AvaloniaProperty.Register<LegendPayload, LegendIcon?>(nameof(Type));

    /// <summary>Defines <see cref="Inactive"/>.</summary>
    public static readonly StyledProperty<bool> InactiveProperty = AvaloniaProperty.Register<LegendPayload, bool>(nameof(Inactive));

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <summary>The entry's text.</summary>
    public string Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The icon's and text's color.</summary>
    public string? Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>The icon; a circle when unset, as Recharts draws an entry without a type.</summary>
    public LegendIcon? Type { get => GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>Whether the entry is drawn in the legend's inactive color.</summary>
    public bool Inactive { get => GetValue(InactiveProperty); set => SetValue(InactiveProperty, value); }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
