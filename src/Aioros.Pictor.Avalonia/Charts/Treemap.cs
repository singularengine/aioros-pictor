using Aioros.Pictor.Specs;
using Avalonia;

namespace Aioros.Pictor;

/// <summary>Recharts' <c>Treemap</c>: nested items squarified into rectangles, with its default content.</summary>
public sealed class Treemap : Chart, IAnimatedElement
{
    /// <summary>Raised on the desktop when the element's enter or update animation starts, before its begin delay (Recharts' <c>onAnimationStart</c>); a web chart, written without script, raises nothing.</summary>
    public event EventHandler? AnimationStarted;

    /// <summary>Raised on the desktop when the element's enter or update animation has moved to its end (Recharts' <c>onAnimationEnd</c>); a web chart raises nothing.</summary>
    public event EventHandler? AnimationEnded;

    private protected override IEnumerable<IAnimatedElement> AnimatedAsOne => [this];

    Motion.Timing? IAnimatedElement.Timing => Motion.Animations.Of(new Specs.TreemapSpec { Animation = Animated.Spec(this) });

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

    /// <summary>Defines <see cref="IsAnimationActive"/>.</summary>
    public static readonly StyledProperty<bool> IsAnimationActiveProperty = Animated.IsActiveProperty.AddOwner<Treemap>();

    /// <summary>Defines <see cref="IsUpdateAnimationActive"/>.</summary>
    public static readonly StyledProperty<bool> IsUpdateAnimationActiveProperty = AvaloniaProperty.Register<Treemap, bool>(nameof(IsUpdateAnimationActive), true);

    /// <summary>
    /// Recharts' <c>isUpdateAnimationActive</c>, true by default. Recharts passes it negated to each node's
    /// <c>Rectangle</c>, so turning it off, while the treemap animates, makes each default rectangle animate itself: its
    /// stroke drawn in over 1500 ms (the <c>Rectangle</c>'s own duration and <c>ease</c>) from 64 ms after it mounts.
    /// </summary>
    public bool IsUpdateAnimationActive { get => GetValue(IsUpdateAnimationActiveProperty); set => SetValue(IsUpdateAnimationActiveProperty, value); }

    /// <summary>Defines <see cref="AnimationBegin"/>.</summary>
    public static readonly StyledProperty<double?> AnimationBeginProperty = Animated.BeginProperty.AddOwner<Treemap>();

    /// <summary>Defines <see cref="AnimationDuration"/>.</summary>
    public static readonly StyledProperty<double?> AnimationDurationProperty = Animated.DurationProperty.AddOwner<Treemap>();

    /// <summary>Defines <see cref="AnimationEasing"/>.</summary>
    public static readonly StyledProperty<AnimationEasing?> AnimationEasingProperty = Animated.EasingProperty.AddOwner<Treemap>();

    /// <summary>Defines <see cref="DataKey"/>.</summary>
    public static readonly StyledProperty<string> DataKeyProperty = AvaloniaProperty.Register<Treemap, string>(nameof(DataKey), "value");

    /// <summary>Defines <see cref="NameKey"/>.</summary>
    public static readonly StyledProperty<string?> NameKeyProperty = AvaloniaProperty.Register<Treemap, string?>(nameof(NameKey));

    /// <summary>Defines <see cref="AspectRatio"/>.</summary>
    public static readonly StyledProperty<double> AspectRatioProperty = AvaloniaProperty.Register<Treemap, double>(nameof(AspectRatio), 0.5 * (1 + Math.Sqrt(5)));

    /// <summary>Defines <see cref="Type"/>.</summary>
    public static readonly StyledProperty<string> TypeProperty = AvaloniaProperty.Register<Treemap, string>(nameof(Type), "flat");

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<string?> FillProperty = AvaloniaProperty.Register<Treemap, string?>(nameof(Fill));

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<string?> StrokeProperty = AvaloniaProperty.Register<Treemap, string?>(nameof(Stroke));

    /// <summary>Defines <see cref="ColorPanel"/>.</summary>
    public static readonly StyledProperty<string?> ColorPanelProperty = AvaloniaProperty.Register<Treemap, string?>(nameof(ColorPanel));

    /// <summary>Defines <see cref="Zoom"/>.</summary>
    public static readonly StyledProperty<string?> ZoomProperty = AvaloniaProperty.Register<Treemap, string?>(nameof(Zoom), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Defines <see cref="ZoomCommand"/>.</summary>
    public static readonly StyledProperty<System.Windows.Input.ICommand?> ZoomCommandProperty = AvaloniaProperty.Register<Treemap, System.Windows.Input.ICommand?>(nameof(ZoomCommand));

    /// <summary>Defines <see cref="ContentTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ContentTemplateProperty = AvaloniaProperty.Register<Treemap, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>Defines <see cref="NestIndexTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> NestIndexTemplateProperty = AvaloniaProperty.Register<Treemap, Avalonia.Controls.Templates.IDataTemplate?>(nameof(NestIndexTemplate));

    /// <summary>
    /// The custom content drawn for each node (Recharts' <c>content</c> element), the root included: a template whose
    /// data context is the node's <see cref="TreemapNodeContext"/> (its box, depth, index, value and panel color).
    /// </summary>
    public Avalonia.Controls.Templates.IDataTemplate? ContentTemplate { get => GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

    /// <summary>The custom content drawn for each entry of a nest treemap's trail (Recharts' <c>nestIndexContent</c>), with its <see cref="NestIndexContext"/>.</summary>
    public Avalonia.Controls.Templates.IDataTemplate? NestIndexTemplate { get => GetValue(NestIndexTemplateProperty); set => SetValue(NestIndexTemplateProperty, value); }

    /// <summary>Creates a treemap, which zooms itself by default.</summary>
    public Treemap() => SetValue(ZoomCommandProperty, new Zooming(this));

    /// <summary>Whether it animates in when the chart appears or its data changes (Recharts' <c>isAnimationActive</c>, on by default as in the browser).</summary>
    public bool IsAnimationActive { get => GetValue(IsAnimationActiveProperty); set => SetValue(IsAnimationActiveProperty, value); }

    /// <summary>The delay before it animates in, in milliseconds; Recharts' default for the kind when null.</summary>
    public double? AnimationBegin { get => GetValue(AnimationBeginProperty); set => SetValue(AnimationBeginProperty, value); }

    /// <summary>How long it animates in, in milliseconds; Recharts' default for the kind when null.</summary>
    public double? AnimationDuration { get => GetValue(AnimationDurationProperty); set => SetValue(AnimationDurationProperty, value); }

    /// <summary>How it eases in; Recharts' default for the kind when null.</summary>
    public AnimationEasing? AnimationEasing { get => GetValue(AnimationEasingProperty); set => SetValue(AnimationEasingProperty, value); }

    /// <summary>The data key of the leaves' values.</summary>
    public string DataKey { get => GetValue(DataKeyProperty); set => SetValue(DataKeyProperty, value); }

    /// <summary>The data key of the names the tooltip and the nest index show; none by default, as Recharts' (the tooltip shows no name, the nest index reads <c>root</c>), while the nodes' labels read their <c>name</c>.</summary>
    public string? NameKey { get => GetValue(NameKeyProperty); set => SetValue(NameKeyProperty, value); }

    /// <summary>The ratio of width to height the rectangles aim for; the golden ratio by default.</summary>
    public double AspectRatio { get => GetValue(AspectRatioProperty); set => SetValue(AspectRatioProperty, value); }

    /// <summary><c>flat</c> (every level) or <c>nest</c> (the first level, with Recharts' nest index).</summary>
    public string Type { get => GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>The rectangles' fill; the color panel's colors by default.</summary>
    public string? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The rectangles' stroke, white by default; when set, the names are stroked with it too, as in Recharts.</summary>
    public string? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The colors of the first two levels, separated by commas; Recharts' panel by default.</summary>
    public string? ColorPanel { get => GetValue(ColorPanelProperty); set => SetValue(ColorPanelProperty, value); }

    /// <summary>
    /// A nest treemap's zoomed node (Recharts' <c>currentRoot</c>), as the indexes of the nodes from the root separated by
    /// dots (<c>2.1</c>); the root when empty. Bindable both ways: a click on a node with children zooms into it and a click
    /// on a box of the nest index zooms out to it, on the desktop natively and on the web through <see cref="ZoomCommand"/>.
    /// </summary>
    public string? Zoom { get => GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }

    /// <summary>
    /// The command a nest treemap's nodes with children and its index's boxes run, with the node's path as the parameter:
    /// by default the treemap's own, which sets <see cref="Zoom"/>. On the web it is a button posted to the server; bind it
    /// to a command of the model that sets the bound zoom for the page's plan to answer the post without Avalonia.
    /// </summary>
    public System.Windows.Input.ICommand? ZoomCommand { get => GetValue(ZoomCommandProperty); set => SetValue(ZoomCommandProperty, value); }

    /// <summary>Whether the treemap shows one level with Recharts' nest index.</summary>
    internal bool IsNest => Type == "nest";

    private protected override ChartKind Kind => ChartKind.Treemap;

    private protected override ChartSpec Complete(ChartSpec spec, SpecBuilder builder) => spec with
    {
        Treemap = new TreemapSpec
        {
            DataKey = DataKey,
            NameKey = NameKey,
            AspectRatio = AspectRatio,
            Nest = IsNest,
            Zoom = IsNest ? Zoom : null,
            ZoomCommand = !IsNest ? null : ChartSlot.ZoomsByChoice(this) ? ChartSlot.ZoomPath : ChartSlot.Zoom,
            ZoomChoice = ChartSlot.ZoomsByChoice(this),
            Fill = Fill,
            Stroke = Stroke,
            ColorPanel = string.IsNullOrWhiteSpace(ColorPanel) ? [] : [.. ColorPanel.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)],
            Animation = Animated.Spec(this),
            UpdateAnimation = IsUpdateAnimationActive,
            Content = builder.Content(ContentTemplate, Content.ContentSlots.TreemapNode, this, nameof(ContentTemplate)),
            NestIndexContent = builder.Content(NestIndexTemplate, Content.ContentSlots.NestIndex, this, nameof(NestIndexTemplate)),
        },
    };
}

/// <summary>A treemap's own zoom: the parameter is the node's path, which becomes the treemap's <see cref="Treemap.Zoom"/>.</summary>
internal sealed class Zooming(Treemap treemap) : System.Windows.Input.ICommand
{
    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => parameter is string;

    /// <inheritdoc />
    public void Execute(object? parameter) => treemap.Zoom = parameter as string;
}
