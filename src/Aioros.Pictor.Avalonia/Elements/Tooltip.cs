using Aioros.Pictor.Specs;
using Avalonia;

namespace Aioros.Pictor;

/// <summary>
/// A tooltip's <c>ContentTemplate</c> that brings the template of its lines, used where the tooltip sets no
/// <see cref="Tooltip.ItemTemplate"/> (a design system's tooltip content that writes its own rows, as shadcn/ui's
/// <c>ChartTooltipContent</c> does), as an items control's theme brings its item template.
/// </summary>
public interface ITooltipLinesTemplate
{
    /// <summary>The template of each line where the content holds a <see cref="TooltipItems"/>.</summary>
    Avalonia.Controls.Templates.IDataTemplate? ItemTemplate { get; }
}

/// <summary>Recharts' <c>Tooltip</c> with its default content: shown for the hovered (or clicked) category or item.</summary>
public sealed class Tooltip : StyledElement, IChartElement, ISpecSource
{
    /// <summary>Defines <see cref="Separator"/>.</summary>
    public static readonly StyledProperty<string?> SeparatorProperty = AvaloniaProperty.Register<Tooltip, string?>(nameof(Separator));

    /// <summary>Defines <see cref="ContentTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ContentTemplateProperty = AvaloniaProperty.Register<Tooltip, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>
    /// The custom content of the box (Recharts' <c>content</c> element): a template whose data context is the tooltip's
    /// <see cref="TooltipContentContext"/> (the label, the first line and the active item), made of panels, borders and
    /// text blocks, written as HTML on the web and placed by its size as Recharts places it.
    /// </summary>
    public Avalonia.Controls.Templates.IDataTemplate? ContentTemplate { get => GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

    /// <summary>Defines <see cref="ItemTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ItemTemplateProperty = AvaloniaProperty.Register<Tooltip, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ItemTemplate));

    /// <summary>
    /// Each line of the box where <see cref="ContentTemplate"/> holds a <see cref="TooltipItems"/>: a template whose data
    /// context is the line's <see cref="TooltipItemContext"/>, written once per line of the payload, as shadcn/ui's
    /// <c>ChartTooltipContent</c> maps <c>payload</c> to its rows.
    /// </summary>
    public Avalonia.Controls.Templates.IDataTemplate? ItemTemplate { get => GetValue(ItemTemplateProperty); set => SetValue(ItemTemplateProperty, value); }

    /// <summary>Defines <see cref="Cursor"/>.</summary>
    public static readonly StyledProperty<bool> CursorProperty = AvaloniaProperty.Register<Tooltip, bool>(nameof(Cursor), true);

    /// <summary>Defines <see cref="CursorStroke"/>.</summary>
    public static readonly StyledProperty<string?> CursorStrokeProperty = AvaloniaProperty.Register<Tooltip, string?>(nameof(CursorStroke));

    /// <summary>Defines <see cref="CursorFill"/>.</summary>
    public static readonly StyledProperty<string?> CursorFillProperty = AvaloniaProperty.Register<Tooltip, string?>(nameof(CursorFill));

    /// <summary>Defines <see cref="CursorStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> CursorStrokeWidthProperty = AvaloniaProperty.Register<Tooltip, double?>(nameof(CursorStrokeWidth));

    /// <summary>Defines <see cref="CursorStrokeDashArray"/>.</summary>
    public static readonly StyledProperty<object?> CursorStrokeDashArrayProperty = AvaloniaProperty.Register<Tooltip, object?>(nameof(CursorStrokeDashArray));

    /// <summary>Defines <see cref="CursorFillOpacity"/>.</summary>
    public static readonly StyledProperty<double?> CursorFillOpacityProperty = AvaloniaProperty.Register<Tooltip, double?>(nameof(CursorFillOpacity));

    /// <summary>Defines <see cref="CursorStrokeOpacity"/>.</summary>
    public static readonly StyledProperty<double?> CursorStrokeOpacityProperty = AvaloniaProperty.Register<Tooltip, double?>(nameof(CursorStrokeOpacity));

    /// <summary>Defines <see cref="Format"/>.</summary>
    public static readonly StyledProperty<string?> FormatProperty = AvaloniaProperty.Register<Tooltip, string?>(nameof(Format));

    /// <summary>Defines <see cref="LabelFormat"/>.</summary>
    public static readonly StyledProperty<string?> LabelFormatProperty = AvaloniaProperty.Register<Tooltip, string?>(nameof(LabelFormat));

    /// <summary>Defines <see cref="Formatter"/>.</summary>
    public static readonly StyledProperty<Avalonia.Data.Converters.IValueConverter?> FormatterProperty = AvaloniaProperty.Register<Tooltip, Avalonia.Data.Converters.IValueConverter?>(nameof(Formatter));

    /// <summary>Defines <see cref="LabelFormatter"/>.</summary>
    public static readonly StyledProperty<Avalonia.Data.Converters.IValueConverter?> LabelFormatterProperty = AvaloniaProperty.Register<Tooltip, Avalonia.Data.Converters.IValueConverter?>(nameof(LabelFormatter));

    private readonly Formatted<Func<object?, string, string?>> _formatter = new(Formatters.Value);

    private readonly Formatted<Func<object?, string?>> _labelFormatter = new(Formatters.Label);

    /// <summary>Defines <see cref="FilterNull"/>.</summary>
    public static readonly StyledProperty<bool> FilterNullProperty = AvaloniaProperty.Register<Tooltip, bool>(nameof(FilterNull), true);

    /// <summary>Defines <see cref="IncludeHidden"/>.</summary>
    public static readonly StyledProperty<bool> IncludeHiddenProperty = AvaloniaProperty.Register<Tooltip, bool>(nameof(IncludeHidden));

    /// <summary>Defines <see cref="ItemSorter"/>.</summary>
    public static readonly StyledProperty<TooltipSorter> ItemSorterProperty = AvaloniaProperty.Register<Tooltip, TooltipSorter>(nameof(ItemSorter));

    /// <summary>Defines <see cref="PayloadUniqBy"/>.</summary>
    public static readonly StyledProperty<bool> PayloadUniqByProperty = AvaloniaProperty.Register<Tooltip, bool>(nameof(PayloadUniqBy));

    /// <summary>Defines <see cref="Shared"/>.</summary>
    public static readonly StyledProperty<bool?> SharedProperty = AvaloniaProperty.Register<Tooltip, bool?>(nameof(Shared));

    /// <summary>Defines <see cref="Trigger"/>.</summary>
    public static readonly StyledProperty<TooltipTrigger> TriggerProperty = AvaloniaProperty.Register<Tooltip, TooltipTrigger>(nameof(Trigger));

    /// <summary>Defines <see cref="DefaultIndex"/>.</summary>
    public static readonly StyledProperty<int?> DefaultIndexProperty = AvaloniaProperty.Register<Tooltip, int?>(nameof(DefaultIndex));

    /// <summary>Defines <see cref="Active"/>.</summary>
    public static readonly StyledProperty<bool?> ActiveProperty = AvaloniaProperty.Register<Tooltip, bool?>(nameof(Active));

    /// <summary>Defines <see cref="Offset"/>.</summary>
    public static readonly StyledProperty<double> OffsetProperty = AvaloniaProperty.Register<Tooltip, double>(nameof(Offset), 10);

    /// <summary>Defines <see cref="AllowEscapeViewBoxX"/>.</summary>
    public static readonly StyledProperty<bool> AllowEscapeViewBoxXProperty = AvaloniaProperty.Register<Tooltip, bool>(nameof(AllowEscapeViewBoxX));

    /// <summary>Defines <see cref="AllowEscapeViewBoxY"/>.</summary>
    public static readonly StyledProperty<bool> AllowEscapeViewBoxYProperty = AvaloniaProperty.Register<Tooltip, bool>(nameof(AllowEscapeViewBoxY));

    /// <summary>Defines <see cref="ReverseDirectionX"/>.</summary>
    public static readonly StyledProperty<bool> ReverseDirectionXProperty = AvaloniaProperty.Register<Tooltip, bool>(nameof(ReverseDirectionX));

    /// <summary>Defines <see cref="ReverseDirectionY"/>.</summary>
    public static readonly StyledProperty<bool> ReverseDirectionYProperty = AvaloniaProperty.Register<Tooltip, bool>(nameof(ReverseDirectionY));

    /// <summary>Defines <see cref="PositionX"/>.</summary>
    public static readonly StyledProperty<double?> PositionXProperty = AvaloniaProperty.Register<Tooltip, double?>(nameof(PositionX));

    /// <summary>Defines <see cref="PositionY"/>.</summary>
    public static readonly StyledProperty<double?> PositionYProperty = AvaloniaProperty.Register<Tooltip, double?>(nameof(PositionY));

    /// <summary>Defines <see cref="WrapperStyle"/>.</summary>
    public static readonly StyledProperty<string?> WrapperStyleProperty = AvaloniaProperty.Register<Tooltip, string?>(nameof(WrapperStyle));

    /// <summary>Defines <see cref="ContentStyle"/>.</summary>
    public static readonly StyledProperty<string?> ContentStyleProperty = AvaloniaProperty.Register<Tooltip, string?>(nameof(ContentStyle));

    /// <summary>Defines <see cref="LabelStyle"/>.</summary>
    public static readonly StyledProperty<string?> LabelStyleProperty = AvaloniaProperty.Register<Tooltip, string?>(nameof(LabelStyle));

    /// <summary>Defines <see cref="ItemStyle"/>.</summary>
    public static readonly StyledProperty<string?> ItemStyleProperty = AvaloniaProperty.Register<Tooltip, string?>(nameof(ItemStyle));

    /// <summary>Defines <see cref="WrapperClassName"/>.</summary>
    public static readonly StyledProperty<string?> WrapperClassNameProperty = AvaloniaProperty.Register<Tooltip, string?>(nameof(WrapperClassName));

    /// <summary>Defines <see cref="LabelClassName"/>.</summary>
    public static readonly StyledProperty<string?> LabelClassNameProperty = AvaloniaProperty.Register<Tooltip, string?>(nameof(LabelClassName));

    /// <summary>Defines <see cref="AccessibilityLayer"/>.</summary>
    public static readonly StyledProperty<bool> AccessibilityLayerProperty = AvaloniaProperty.Register<Tooltip, bool>(nameof(AccessibilityLayer));

    private readonly ChangeRelay _relay = new();

    /// <inheritdoc />
    public event EventHandler? Changed { add => _relay.Changed += value; remove => _relay.Changed -= value; }

    /// <summary>The text between a series' name and its value.</summary>
    public string? Separator { get => GetValue(SeparatorProperty); set => SetValue(SeparatorProperty, value); }

    /// <summary>Whether a cursor marks the hovered category (Recharts' <c>cursor={false}</c> hides it).</summary>
    public bool Cursor { get => GetValue(CursorProperty); set => SetValue(CursorProperty, value); }

    /// <summary>The cursor's stroke (Recharts' <c>cursor={{stroke}}</c>).</summary>
    public string? CursorStroke { get => GetValue(CursorStrokeProperty); set => SetValue(CursorStrokeProperty, value); }

    /// <summary>The cursor's fill (a band cursor's <c>#ccc</c> by default).</summary>
    public string? CursorFill { get => GetValue(CursorFillProperty); set => SetValue(CursorFillProperty, value); }

    /// <summary>The cursor's stroke width.</summary>
    public double? CursorStrokeWidth { get => GetValue(CursorStrokeWidthProperty); set => SetValue(CursorStrokeWidthProperty, value); }

    /// <summary>The cursor's dash pattern (Recharts' <c>cursor={{ strokeDasharray: '3 3' }}</c>).</summary>
    public object? CursorStrokeDashArray { get => GetValue(CursorStrokeDashArrayProperty); set => SetValue(CursorStrokeDashArrayProperty, value); }

    /// <summary>The cursor's fill opacity.</summary>
    public double? CursorFillOpacity { get => GetValue(CursorFillOpacityProperty); set => SetValue(CursorFillOpacityProperty, value); }

    /// <summary>The cursor's stroke opacity.</summary>
    public double? CursorStrokeOpacity { get => GetValue(CursorStrokeOpacityProperty); set => SetValue(CursorStrokeOpacityProperty, value); }

    /// <summary>A .NET composite format for the values (Recharts' <c>formatter</c>, <c>{0:N0}</c>).</summary>
    public string? Format { get => GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>A .NET composite format for the label (Recharts' <c>labelFormatter</c>, <c>Day {0}</c>).</summary>
    public string? LabelFormat { get => GetValue(LabelFormatProperty); set => SetValue(LabelFormatProperty, value); }

    /// <summary>Recharts' <c>formatter</c>: a converter of a line's value to its text, its series' name as the parameter, over <see cref="Format"/>.</summary>
    public Avalonia.Data.Converters.IValueConverter? Formatter { get => GetValue(FormatterProperty); set => SetValue(FormatterProperty, value); }

    /// <summary>Recharts' <c>labelFormatter</c>: a converter of the active category to the label's text, over <see cref="LabelFormat"/>.</summary>
    public Avalonia.Data.Converters.IValueConverter? LabelFormatter { get => GetValue(LabelFormatterProperty); set => SetValue(LabelFormatterProperty, value); }

    /// <summary>Whether entries without a value are left out (Recharts' <c>filterNull</c>).</summary>
    public bool FilterNull { get => GetValue(FilterNullProperty); set => SetValue(FilterNullProperty, value); }

    /// <summary>Whether hidden series keep their entries (Recharts' <c>includeHidden</c>).</summary>
    public bool IncludeHidden { get => GetValue(IncludeHiddenProperty); set => SetValue(IncludeHiddenProperty, value); }

    /// <summary>How the entries are ordered (Recharts' <c>itemSorter</c> given as a key).</summary>
    public TooltipSorter ItemSorter { get => GetValue(ItemSorterProperty); set => SetValue(ItemSorterProperty, value); }

    /// <summary>Whether entries of the same data key are shown once (Recharts' <c>payloadUniqBy</c>).</summary>
    public bool PayloadUniqBy { get => GetValue(PayloadUniqByProperty); set => SetValue(PayloadUniqByProperty, value); }

    /// <summary>False for one tooltip per bar in a bar chart or per sector in a radial bar chart (Recharts' <c>shared={false}</c>); line, area and composed charts always share it.</summary>
    public bool? Shared { get => GetValue(SharedProperty); set => SetValue(SharedProperty, value); }

    /// <summary>What shows the tooltip: hovering, or clicking (Recharts' <c>trigger</c>).</summary>
    public TooltipTrigger Trigger { get => GetValue(TriggerProperty); set => SetValue(TriggerProperty, value); }

    /// <summary>The category shown before any pointer event (Recharts' <c>defaultIndex</c>).</summary>
    public int? DefaultIndex { get => GetValue(DefaultIndexProperty); set => SetValue(DefaultIndexProperty, value); }

    /// <summary>False to never show the box and the cursor; true to keep the last one after the pointer leaves (Recharts' <c>active</c>).</summary>
    public bool? Active { get => GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }

    /// <summary>The distance between the box and the point it follows (Recharts' <c>offset</c>).</summary>
    public double Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    /// <summary>Whether the box may leave the plot along x instead of being flipped (Recharts' <c>allowEscapeViewBox.x</c>).</summary>
    public bool AllowEscapeViewBoxX { get => GetValue(AllowEscapeViewBoxXProperty); set => SetValue(AllowEscapeViewBoxXProperty, value); }

    /// <summary>Whether the box may leave the plot along y (Recharts' <c>allowEscapeViewBox.y</c>).</summary>
    public bool AllowEscapeViewBoxY { get => GetValue(AllowEscapeViewBoxYProperty); set => SetValue(AllowEscapeViewBoxYProperty, value); }

    /// <summary>Whether the box goes left of its point first (Recharts' <c>reverseDirection.x</c>).</summary>
    public bool ReverseDirectionX { get => GetValue(ReverseDirectionXProperty); set => SetValue(ReverseDirectionXProperty, value); }

    /// <summary>Whether the box goes above its point first (Recharts' <c>reverseDirection.y</c>).</summary>
    public bool ReverseDirectionY { get => GetValue(ReverseDirectionYProperty); set => SetValue(ReverseDirectionYProperty, value); }

    /// <summary>A fixed x of the box (Recharts' <c>position.x</c>).</summary>
    public double? PositionX { get => GetValue(PositionXProperty); set => SetValue(PositionXProperty, value); }

    /// <summary>A fixed y of the box (Recharts' <c>position.y</c>).</summary>
    public double? PositionY { get => GetValue(PositionYProperty); set => SetValue(PositionYProperty, value); }

    /// <summary>CSS declarations of the box's wrapper (Recharts' <c>wrapperStyle</c>; web only).</summary>
    public string? WrapperStyle { get => GetValue(WrapperStyleProperty); set => SetValue(WrapperStyleProperty, value); }

    /// <summary>CSS declarations of the box (Recharts' <c>contentStyle</c>): the desktop draws its background, border, border radius and padding.</summary>
    public string? ContentStyle { get => GetValue(ContentStyleProperty); set => SetValue(ContentStyleProperty, value); }

    /// <summary>CSS declarations of the label (Recharts' <c>labelStyle</c>): the desktop draws its color.</summary>
    public string? LabelStyle { get => GetValue(LabelStyleProperty); set => SetValue(LabelStyleProperty, value); }

    /// <summary>CSS declarations of each entry (Recharts' <c>itemStyle</c>): the desktop draws its color.</summary>
    public string? ItemStyle { get => GetValue(ItemStyleProperty); set => SetValue(ItemStyleProperty, value); }

    /// <summary>Classes added to the box (Recharts' <c>wrapperClassName</c>; web only).</summary>
    public string? WrapperClassName { get => GetValue(WrapperClassNameProperty); set => SetValue(WrapperClassNameProperty, value); }

    /// <summary>Classes added to the label (Recharts' <c>labelClassName</c>; web only).</summary>
    public string? LabelClassName { get => GetValue(LabelClassNameProperty); set => SetValue(LabelClassNameProperty, value); }

    /// <summary>Whether the box is announced to assistive technologies (Recharts' <c>accessibilityLayer</c>; web only).</summary>
    public bool AccessibilityLayer { get => GetValue(AccessibilityLayerProperty); set => SetValue(AccessibilityLayerProperty, value); }

    void ISpecSource.Apply(SpecBuilder builder, int order) => builder.Tooltip = new TooltipSpec
    {
        Content = builder.Content(ContentTemplate, Content.ContentSlots.Tooltip, this, nameof(ContentTemplate)),
        ItemContent = builder.Content(ItemTemplate ?? (ContentTemplate as ITooltipLinesTemplate)?.ItemTemplate, Content.ContentSlots.TooltipItem, this, nameof(ItemTemplate)),
        Separator = Separator ?? builder.Theme.TooltipSeparator,
        Cursor = Cursor,
        CursorStroke = CursorStroke,
        CursorFill = CursorFill,
        CursorStrokeWidth = CursorStrokeWidth,
        CursorStrokeDashArray = SpecText.Dashes(CursorStrokeDashArray),
        CursorFillOpacity = CursorFillOpacity,
        CursorStrokeOpacity = CursorStrokeOpacity,
        Format = Format,
        LabelFormat = LabelFormat,
        Formatter = _formatter.Of(Formatter),
        LabelFormatter = _labelFormatter.Of(LabelFormatter),
        Converter = Formatters.Reference(Formatter),
        LabelConverter = Formatters.Reference(LabelFormatter),
        FilterNull = FilterNull,
        IncludeHidden = IncludeHidden,
        ItemSorter = ItemSorter switch { TooltipSorter.DataKey => "dataKey", TooltipSorter.Value => "value", TooltipSorter.Name => "name", _ => null },
        PayloadUniqBy = PayloadUniqBy,
        Shared = Shared,
        ClickTrigger = Trigger == TooltipTrigger.Click,
        DefaultIndex = DefaultIndex,
        Active = Active,
        Offset = Offset,
        AllowEscapeX = AllowEscapeViewBoxX,
        AllowEscapeY = AllowEscapeViewBoxY,
        ReverseX = ReverseDirectionX,
        ReverseY = ReverseDirectionY,
        PositionX = PositionX,
        PositionY = PositionY,
        WrapperStyle = WrapperStyle,
        ContentStyle = ContentStyle,
        LabelStyle = LabelStyle,
        ItemStyle = ItemStyle,
        WrapperClassName = WrapperClassName,
        LabelClassName = LabelClassName,
        AccessibilityLayer = AccessibilityLayer,
    };

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _relay.Raise(this);
    }
}

/// <summary>How a tooltip orders its lines, Recharts' <c>itemSorter</c> given as a key.</summary>
public enum TooltipSorter
{
    /// <summary>In the order of the series (no <c>itemSorter</c>).</summary>
    None,

    /// <summary>By data key (<c>itemSorter="dataKey"</c>).</summary>
    DataKey,

    /// <summary>By value, smallest first (<c>itemSorter="value"</c>).</summary>
    Value,

    /// <summary>By name (<c>itemSorter="name"</c>).</summary>
    Name,
}

/// <summary>What shows a tooltip, Recharts' <c>trigger</c>.</summary>
public enum TooltipTrigger
{
    /// <summary>Hovering a category or an item (<c>hover</c>).</summary>
    Hover,

    /// <summary>Clicking a category or an item; it stays until another is clicked (<c>click</c>).</summary>
    Click,
}
