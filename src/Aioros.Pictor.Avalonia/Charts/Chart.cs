using System.Globalization;
using System.Collections.Immutable;
using System.Collections.Specialized;
using Aioros.Pictor.Data;
using Aioros.Pictor.Drawing;
using Aioros.Pictor.Interaction;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;
using Aioros.Runtime.Rendering;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Aioros.Pictor;

/// <summary>
/// What every Recharts chart shares: the data, the inner margin, the title and the children. On the desktop the chart
/// draws itself, its static scene recorded once per layout and the hovered category or item drawn over it; on the web
/// Aioros writes it per request through <see cref="ChartRenderer"/>.
/// </summary>
/// <remarks>
/// The children are held by a styled property, so Aioros's plan checks their bindings as it checks the chart's own: a
/// child property bound to the model is followed on the desktop and makes the web plan fail with its name (the page is
/// then rendered by Avalonia per request), never written frozen.
/// </remarks>
public abstract partial class Chart : Control, IWrittenBySlot, Avalonia.Rendering.ICustomHitTest
{
    /// <summary>Defines <see cref="Data"/>.</summary>
    public static readonly StyledProperty<object?> DataProperty = AvaloniaProperty.Register<Chart, object?>(nameof(Data));

    /// <summary>Defines <see cref="Padding"/>.</summary>
    public static readonly StyledProperty<Thickness> PaddingProperty = AvaloniaProperty.Register<Chart, Thickness>(nameof(Padding), new Thickness(5));

    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<Chart, string?>(nameof(Title));

    /// <summary>Defines <see cref="Desc"/>.</summary>
    public static readonly StyledProperty<string?> DescProperty = AvaloniaProperty.Register<Chart, string?>(nameof(Desc));

    /// <summary>Defines <see cref="ContainerId"/>.</summary>
    public static readonly StyledProperty<string?> ContainerIdProperty = AvaloniaProperty.Register<Chart, string?>(nameof(ContainerId));

    /// <summary>Defines <see cref="ContainerClass"/>.</summary>
    public static readonly StyledProperty<string?> ContainerClassProperty = AvaloniaProperty.Register<Chart, string?>(nameof(ContainerClass));

    /// <summary>Defines <see cref="ContainerStyle"/>.</summary>
    public static readonly StyledProperty<string?> ContainerStyleProperty = AvaloniaProperty.Register<Chart, string?>(nameof(ContainerStyle));

    /// <summary>Defines <see cref="ResponsiveWidths"/>.</summary>
    public static readonly StyledProperty<string?> ResponsiveWidthsProperty = AvaloniaProperty.Register<Chart, string?>(nameof(ResponsiveWidths));

    /// <summary>Defines <see cref="Aspect"/>.</summary>
    public static readonly StyledProperty<double> AspectProperty = AvaloniaProperty.Register<Chart, double>(nameof(Aspect), double.NaN);

    /// <summary>Defines <see cref="Elements"/>.</summary>
    public static readonly StyledProperty<AvaloniaList<IChartElement>> ElementsProperty = AvaloniaProperty.Register<Chart, AvaloniaList<IChartElement>>(nameof(Elements));

    /// <summary>Defines <see cref="ClickCommand"/>.</summary>
    public static readonly StyledProperty<System.Windows.Input.ICommand?> ClickCommandProperty = AvaloniaProperty.Register<Chart, System.Windows.Input.ICommand?>(nameof(ClickCommand));

    /// <summary>Defines <see cref="ItemClickCommand"/>.</summary>
    public static readonly StyledProperty<System.Windows.Input.ICommand?> ItemClickCommandProperty = AvaloniaProperty.Register<Chart, System.Windows.Input.ICommand?>(nameof(ItemClickCommand));

    /// <summary>Defines <see cref="SyncId"/>.</summary>
    public static readonly StyledProperty<string?> SyncIdProperty = AvaloniaProperty.Register<Chart, string?>(nameof(SyncId));

    /// <summary>Defines <see cref="SyncMethod"/>.</summary>
    public static readonly StyledProperty<string> SyncMethodProperty = AvaloniaProperty.Register<Chart, string>(nameof(SyncMethod), "index");

    /// <summary>Defines <see cref="Role"/>.</summary>
    public static readonly StyledProperty<string?> RoleProperty = AvaloniaProperty.Register<Chart, string?>(nameof(Role));

    /// <summary>Defines <see cref="WrapperStyle"/>.</summary>
    public static readonly StyledProperty<string?> WrapperStyleProperty = AvaloniaProperty.Register<Chart, string?>(nameof(WrapperStyle));

    /// <summary>Defines <see cref="AccessibilityLayer"/>.</summary>
    public static readonly StyledProperty<bool> AccessibilityLayerProperty = AvaloniaProperty.Register<Chart, bool>(nameof(AccessibilityLayer));

    /// <summary>Defines the <c>ReduceMotion</c> attached property.</summary>
    public static readonly AttachedProperty<bool> ReduceMotionProperty = AvaloniaProperty.RegisterAttached<Chart, Visual, bool>("ReduceMotion", inherits: true);

    internal static readonly DataMembers Members = static (owner, member) => MemberAccessors.Getter(owner, member);

    private IChartPlot? _plot;
    private DrawingGroup? _scene;
    private ChartPaints? _paints;
    private INotifyCollectionChanged? _observed;
    private readonly ItemFollower _follower;
    private bool _attached;
    private ChartData? _data;
    private List<ClickTarget>? _clicks;
    private IChartPlot? _clicked;
    private readonly ChartMotion _motion = new();
    private readonly ChartKeys _keys;

    static Chart()
    {
        TextElement.FontSizeProperty.OverrideDefaultValue<Chart>(14);
        ChartSpecs.Focus(AccessibilityLayerProperty);
    }

    private protected Chart()
    {
        _motion.Events = new ChartAnimationEvents(() => Elements.OfType<IAnimatedElement>().Concat(AnimatedAsOne));
        var elements = new AvaloniaList<IChartElement>();
        EventHandler changed = (_, _) => ElementChanged();
        var followed = new ChildChanges<IChartElement>(element => element.Changed += changed, element => element.Changed -= changed);
        elements.CollectionChanged += (_, _) =>
        {
            followed.Follow(elements);
            Invalidate();
        };
        SetValue(ElementsProperty, elements);
        _scope = new ChartScope(this, LogicalChildren, elements);
        ClipToBounds = true;
        _follower = new ItemFollower(Update, Dispatcher);
        Pointer = new ChartPointer(this);
        _keys = new ChartKeys(this);
        Pointer.Hovered += (_, state) => ChartSync.Hover(this, state);
    }

    /// <summary>The items the chart draws: a list of objects whose members the data keys name (a Sankey's nodes and links).</summary>
    public object? Data { get => GetValue(DataProperty); set => SetValue(DataProperty, value); }

    /// <summary>The space between the chart's edge and its plot (Recharts' <c>margin</c>), 5 on every side by default.</summary>
    public Thickness Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }

    /// <summary>The chart's accessible name.</summary>
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The chart's description, the SVG's <c>desc</c> on the web (Recharts' <c>desc</c>).</summary>
    public string? Desc { get => GetValue(DescProperty); set => SetValue(DescProperty, value); }

    /// <summary>A responsive chart's container <c>id</c> on the web page (Recharts' <c>ResponsiveContainer</c> <c>id</c>).</summary>
    public string? ContainerId { get => GetValue(ContainerIdProperty); set => SetValue(ContainerIdProperty, value); }

    /// <summary>A responsive chart's container class on the web page, for the page's CSS (<c>ResponsiveContainer</c> <c>className</c>).</summary>
    public string? ContainerClass { get => GetValue(ContainerClassProperty); set => SetValue(ContainerClassProperty, value); }

    /// <summary>A responsive chart's container CSS declarations on the web page, before the sizes the chart computes (<c>ResponsiveContainer</c> <c>style</c>).</summary>
    public string? ContainerStyle { get => GetValue(ContainerStyleProperty); set => SetValue(ContainerStyleProperty, value); }

    /// <summary>
    /// Recharts' <c>accessibilityLayer</c>: the chart takes focus, focus shows the active category's tooltip (the
    /// tooltip's default index, or the first) and the left and right arrow keys move it along a horizontal chart's
    /// categories. On the web the framework's roving focus moves the focus among a horizontal chart's categories, with
    /// the active one alone in the tab order.
    /// </summary>
    public bool AccessibilityLayer { get => GetValue(AccessibilityLayerProperty); set => SetValue(AccessibilityLayerProperty, value); }

    /// <summary>The web surface's role with <see cref="AccessibilityLayer"/> (Recharts' <c>role</c>, <c>application</c> by default); its tab index is the control's <see cref="InputElement.TabIndex"/> when set (Recharts' <c>tabIndex</c>, 0 by default).</summary>
    public string? Role { get => GetValue(RoleProperty); set => SetValue(RoleProperty, value); }

    /// <summary>CSS declarations written last in the web wrapper's style (Recharts' chart <c>style</c>); the chart's own classes (<see cref="StyledElement.Classes"/>, its pseudo-classes left out) are written after <c>recharts-wrapper</c>, as Recharts' <c>className</c>.</summary>
    public string? WrapperStyle { get => GetValue(WrapperStyleProperty); set => SetValue(WrapperStyleProperty, value); }

    /// <summary>
    /// For a chart that fills its parent on the web: the widths it is laid out at (<c>360 720 1080</c>), the browser
    /// showing the layout nearest the width it gets through container queries, so ticks and text keep their size
    /// instead of scaling with the view box. The desktop always lays the chart out at its real size.
    /// </summary>
    public string? ResponsiveWidths { get => GetValue(ResponsiveWidthsProperty); set => SetValue(ResponsiveWidthsProperty, value); }

    /// <summary>
    /// For a chart without a fixed height, its width over its height (none when NaN, the default) (Recharts' <c>ResponsiveContainer</c> <c>aspect</c>):
    /// the chart takes the height its width gives, at most its <c>MaxHeight</c>; the web keeps the ratio at any width.
    /// </summary>
    public double Aspect { get => GetValue(AspectProperty); set => SetValue(AspectProperty, value); }

    /// <summary>
    /// The command a click on the chart runs (Recharts' chart <c>onClick</c>), with the clicked category's index in the
    /// data as its parameter (a hover state's index for a chart whose states are items; a node for a treemap, a Sankey or
    /// a sunburst): a button over each category on the web, the pointer on the desktop.
    /// </summary>
    public System.Windows.Input.ICommand? ClickCommand { get => GetValue(ClickCommandProperty); set => SetValue(ClickCommandProperty, value); }

    /// <summary>
    /// The command a click on any of the chart's items runs when its own element sets none (Recharts' item <c>onClick</c>),
    /// with the item's index in its data (a treemap node's path, a Sankey's <c>node:N</c> or <c>link:N</c>, a sunburst
    /// node's index) as its parameter: a button over each item on the web, the pointer on the desktop.
    /// </summary>
    public System.Windows.Input.ICommand? ItemClickCommand { get => GetValue(ItemClickCommandProperty); set => SetValue(ItemClickCommandProperty, value); }

    /// <summary>
    /// The charts with the same id show the same active category (Recharts' <c>syncId</c>): hovering one shows the
    /// matching tooltip and cursor in the others, and a brush moved in one moves theirs. On the web through page-wide
    /// CSS rules, without script; on the desktop through the charts' sync group.
    /// </summary>
    public string? SyncId { get => GetValue(SyncIdProperty); set => SetValue(SyncIdProperty, value); }

    /// <summary>How a synced chart finds its active category: <c>index</c> (the same index, Recharts' default) or <c>value</c> (the same category).</summary>
    public string SyncMethod { get => GetValue(SyncMethodProperty); set => SetValue(SyncMethodProperty, value); }

    /// <summary>The chart's children: axes, grids, series, references, legend and tooltip, in the order Recharts draws them.</summary>
    [Content]
    public AvaloniaList<IChartElement> Elements => GetValue(ElementsProperty);

    /// <inheritdoc />
    public WrittenSlot? WrittenSlot => ChartSlot.Of(this);

    private protected abstract ChartKind Kind { get; }

    /// <summary>The chart's compiled templates of custom content.</summary>
    internal ContentTemplates Templates => _scope.Templates;

    /// <summary>How many times the chart was laid out.</summary>
    internal int Layouts { get; private set; }

    /// <summary>Whether the enter animation has played to its end.</summary>
    internal bool EnterPlayed => _motion.EnterPlayed;

    /// <summary>Whether an update animation is drawing its frames.</summary>
    internal bool Updating => _motion.Updating;

    /// <summary>Plays the enter animation again (a <see cref="ReplayButton"/>).</summary>
    internal void ReplayEnter()
    {
        _motion.Replay();
        Invalidate();
    }

    /// <summary>How many times a change raised on another thread was posted to the UI thread.</summary>
    internal int Posts => _follower.Posts;


    /// <summary>
    /// The hover state drawn: the hovered (or clicked) category or item, else the tooltip's default index until the
    /// pointer first leaves the chart (Recharts' <c>defaultIndex</c>), else a negative number.
    /// </summary>
    internal int ActiveIndex => Pointer.Active >= 0 || Pointer.Left || _plot is null ? Pointer.Active : _plot.DefaultTip;

    /// <summary>The clock the desktop's enter animations read (the system's; a test's own).</summary>
    internal TimeProvider Time { get => _motion.Time; set => _motion.Time = value; }

    /// <summary>
    /// Whether charts in a visual tree draw their final state at once instead of Recharts' enter animations: set it on a
    /// window or the application's root from the operating system's reduced-motion preference (Avalonia does not report
    /// it). The web follows the browser's <c>prefers-reduced-motion</c> on its own.
    /// </summary>
    /// <param name="visual">The chart or one of its ancestors.</param>
    /// <returns>Whether motion is reduced.</returns>
    public static bool GetReduceMotion(Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        return visual.GetValue(ReduceMotionProperty);
    }

    /// <summary>Sets <c>ReduceMotion</c> on a chart or an ancestor of charts.</summary>
    /// <param name="visual">The chart or one of its ancestors.</param>
    /// <param name="value">Whether motion is reduced.</param>
    public static void SetReduceMotion(Visual visual, bool value)
    {
        ArgumentNullException.ThrowIfNull(visual);
        visual.SetValue(ReduceMotionProperty, value);
    }

    /// <summary>The chart's spec at a size, as the web writer receives it.</summary>
    internal ChartSpec Spec(double width, double height)
    {
        var builder = ChartSpecs.Build(_scope.Builder(Members), Elements);
        var spec = Complete(ChartSpecs.Of(this, Kind, width, height, builder), builder);
        return ChartSpecs.Interactive(this, builder.Gradients.Count == 0 ? spec : spec with { Gradients = [.. builder.Gradients] });
    }

    /// <summary>The family's own props and children added to the spec the chart's elements built.</summary>
    private protected abstract ChartSpec Complete(ChartSpec spec, SpecBuilder builder);

    /// <summary>The chart laid out at its current size, computed once per data, size and spec change.</summary>
    internal IChartPlot Plot()
    {
        if (_plot is { } plot)
        {
            return plot;
        }

        var data = ChartData.From(Data, Members);
        _data = data;
        plot = Plots.Compute(Spec(Bounds.Width, Bounds.Height), data);
        Layouts++;
        if (_attached)
        {
            _follower.Follow(data, Elements);
        }

        if ((Pointer.Drag is not null || Pointer.BrushTexts) && plot is ChartLayout { Brush: { } brush } layout)
        {
            if (Pointer.Drag is { } drag)
            {
                (brush.StartX, brush.EndX) = (drag.StartX, drag.EndX);
            }

            brush.Texts = (brush.TextAt(layout.WholeData, brush.StartIndex), brush.TextAt(layout.WholeData, brush.EndIndex));
        }

        _plot = plot;
        return plot;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var plot = Plot();
        var paints = _paints ??= _scope.Paints(plot.Spec);
        var active = ActiveIndex;
        if (active >= 0)
        {
            var below = new AvaloniaCanvas(context, paints);
            Plots.HoverBelow(ref below, plot, active);
        }
        else if (_keys.Cross >= 0 && plot is ChartLayout keyed && ScatterKeys.Applies(plot))
        {
            var below = new AvaloniaCanvas(context, paints);
            ScatterKeys.Paint(ref below, keyed, _keys.Cross);
        }

        if (_motion.Frame(plot, GetReduceMotion(this)) is { } frame)
        {
            var moving = new AvaloniaCanvas(context, paints, frame.Motion);
            Plots.Paint(ref moving, frame.Plot, ChartPainter.NoTips);
            Above(context, plot, paints);
            TopLevel.GetTopLevel(this)?.RequestAnimationFrame(_ => InvalidateVisual());
            return;
        }

        if (active >= 0 && Plots.InPlace(plot))
        {
            var hovered = new AvaloniaCanvas(context, paints);
            Plots.PaintInPlace(ref hovered, plot, active);
            Above(context, plot, paints);
            return;
        }

        if (_scene is null)
        {
            var scene = new DrawingGroup();
            using (var recording = scene.Open())
            {
                var canvas = new AvaloniaCanvas(recording, paints);
                Plots.Paint(ref canvas, plot, ChartPainter.NoTips);
            }

            _scene = scene;
        }

        _scene.Draw(context);
        Above(context, plot, paints);
    }

    private void Above(DrawingContext context, IChartPlot plot, ChartPaints paints)
    {
        if (ActiveIndex is var active and >= 0)
        {
            var above = new AvaloniaCanvas(context, paints, tooltips: active != _keys.Dismissed);
            Plots.HoverAbove(ref above, plot, active, Pointer.At is { } at ? (at.X, at.Y) : null);
        }
    }

    /// <summary>The height an aspect ratio gives a width, at most the chart's <c>MaxHeight</c>.</summary>
    private double _fitted;

    internal double AspectHeight(double width, double aspect)
    {
        var height = width / aspect < MaxHeight ? width / aspect : MaxHeight;
        height = height < MinHeight ? MinHeight : height;
        return height < _fitted ? _fitted : height;
    }

    /// <summary>A peer that reads the chart's name and data to assistive technologies.</summary>
    /// <returns>The peer.</returns>
    protected override Avalonia.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new Automation.ChartAutomationPeer(this);

    /// <summary>The whole chart takes the pointer, so hovering an empty part of the plot still shows its category.</summary>
    /// <param name="point">The point, in the chart's coordinates.</param>
    /// <returns>Whether the point is inside the chart.</returns>
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 500 : availableSize.Width;
        _fitted = 0;
        if (Aspect is var aspect and > 0)
        {
            _fitted = ChartSpecs.FittedHeight(this, width, AspectHeight(width, aspect));
            return new Size(width, AspectHeight(width, aspect));
        }

        return new Size(width, double.IsInfinity(availableSize.Height) ? 300 : availableSize.Height);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        ArgumentNullException.ThrowIfNull(change);
        base.OnPropertyChanged(change);
        if (change.Property == SyncIdProperty && _attached)
        {
            Rejoin(change.GetOldValue<string?>(), change.GetNewValue<string?>());
        }

        if (change.Property == DataProperty)
        {
            Observe(_attached ? change.NewValue as INotifyCollectionChanged : null);
            Update();
        }
        else if (change.Property == BoundsProperty || change.Property == PaddingProperty)
        {
            if (change.Property == PaddingProperty || change.GetOldValue<Rect>().Size != change.GetNewValue<Rect>().Size)
            {
                Update();
            }
        }

        else if (change.Property == TextElement.FontSizeProperty || change.Property == TextElement.FontFamilyProperty)
        {
            _paints = null;
            Invalidate();
        }
        else if ((change.Property.OwnerType.IsAssignableTo(typeof(Chart)) && change.Property != ElementsProperty) || Animated.Owns(change.Property))
        {
            Invalidate();
        }
    }

    /// <summary>The chart itself when it animates as one element (a treemap), for its animation events.</summary>
    private protected virtual IEnumerable<IAnimatedElement> AnimatedAsOne => [];

    /// <summary>The legend entries the desktop has toggled off.</summary>
    internal ImmutableArray<int> Concealed => Pointer.Concealed;

    /// <summary>What the pointer does to the chart.</summary>
    internal ChartPointer Pointer { get; }

    /// <summary>The key step whose cross a scatter chart's keyboard draws, or a negative number.</summary>
    internal int KeyCross => _keys.Cross;

    /// <summary>The pointer entered an item: the keyboard's cross goes.</summary>
    internal void KeysPointed() => _keys.Pointed();

    /// <summary>The click targets of the chart as laid out now, the same as the web's buttons; none when no click command is set.</summary>
    internal List<ClickTarget> Clicks()
    {
        var plot = Plot();
        if (_clicks is { } clicks && ReferenceEquals(_clicked, plot))
        {
            return clicks;
        }

        Pointer.Forget();
        _clicked = plot;
        _clicks = Interactions.Clicks(plot.Spec) ? ClickTargets.Of(plot, _data!) : [];
        return _clicks;
    }

    /// <summary>The command a target's key names.</summary>
    internal System.Windows.Input.ICommand? Command(string key) => ChartPointer.Resolve(this, key);

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);
        Pointer.Pressed(e);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerReleased(e);
        Pointer.Released(e);
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerMoved(e);
        Pointer.Moved(e);
    }

    /// <summary>Recharts' focus with <c>accessibilityLayer</c>: the active category's tooltip shows.</summary>
    /// <param name="e">The event's arguments.</param>
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        _keys.Focused();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnKeyDown(e);
        _keys.Pressed(e);
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        Pointer.Exited();
    }

    private void Rejoin(string? old, string? joined)
    {
        if (old is { Length: > 0 })
        {
            ChartSync.Leave(this, old);
        }

        if (joined is { Length: > 0 })
        {
            ChartSync.Join(this, joined);
        }
    }

    private void Observe(INotifyCollectionChanged? collection)
    {
        if (_observed is not null)
        {
            _observed.CollectionChanged -= OnDataChanged;
        }

        _observed = collection;
        if (collection is not null)
        {
            collection.CollectionChanged += OnDataChanged;
        }
    }

    /// <summary>
    /// Follows the data while the chart is in the visual tree: its collection, its items, the lists below them and its
    /// series' own data; out of the tree it holds no subscription, and it reads its data again when it comes back.
    /// </summary>
    /// <param name="e">The event's arguments.</param>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        if (SyncId is { Length: > 0 } sync)
        {
            ChartSync.Join(this, sync);
        }

        Observe(Data as INotifyCollectionChanged);
        _keys.Attach();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        if (SyncId is { Length: > 0 } sync)
        {
            ChartSync.Leave(this, sync);
        }

        Observe(null);
        _keys.Detach();
        _follower.Clear();
    }

    private void OnDataChanged(object? sender, NotifyCollectionChangedEventArgs e) => _follower.ListChanged(e);

    /// <summary>
    /// A child changed: drawn at once, as Recharts keeps its <c>animationId</c> when only its children change; a chart
    /// without data of its own (a pie chart whose pies carry theirs) animates, as Recharts counts a new update then.
    /// </summary>
    private void ElementChanged()
    {
        if (Data is null)
        {
            Update();
        }
        else
        {
            Invalidate();
        }
    }

    /// <summary>The data, the size or the inner margin changed: laid out again, animating from the layout drawn last (Recharts' update animation).</summary>
    private void Update()
    {
        Invalidate();
        _motion.Update();
    }

    internal void Invalidate()
    {
        _plot = null;
        _scene = null;
        _motion.Reset();
        InvalidateVisual();
    }
}
