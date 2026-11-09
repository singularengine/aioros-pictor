using Aioros.Markup;
using System.Collections.Immutable;
using System.Windows.Input;
using Aioros.Pictor.Interaction;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;

namespace Aioros.Pictor;

/// <summary>
/// What the pointer does to a desktop chart, as Recharts' handlers do in the browser: the hovered (or clicked) category or
/// item, the legend toggles, the brush's drag and its texts, and the clicks on the chart, its items, its legend and a
/// nest treemap's nodes and index, which run the same commands with the same parameters as the web's buttons.
/// </summary>
internal sealed class ChartPointer(Chart chart)
{
    private readonly Dictionary<string, Geometry> _paths = new(StringComparer.Ordinal);
    private ITimer? _brushLeave;
    private (string Key, string Parameter)? _pressed;

    /// <summary>The hovered or clicked hover state, or a negative number.</summary>
    public int Active { get; set; } = ChartPainter.NoTips;

    /// <summary>Where the pointer (a mouse or a finger) stands over the hovered state, which an axis tooltip's box follows as Recharts' does; null when the keyboard, a click or a synced chart chose the state.</summary>
    public Point? At { get; private set; }

    /// <summary>Whether the pointer has left the chart once (which drops the tooltip's default index).</summary>
    public bool Left { get; private set; }

    /// <summary>The legend entries toggled off.</summary>
    public ImmutableArray<int> Concealed { get; private set; } = [];

    /// <summary>The brush's drag in progress.</summary>
    public BrushDrag? Drag { get; private set; }

    /// <summary>Whether the brush's end texts are shown.</summary>
    public bool BrushTexts { get; private set; }

    /// <summary>Raised when the hovered state changes, with the state (a synced chart shows the same one).</summary>
    public event EventHandler<int>? Hovered;

    public void Pressed(PointerPressedEventArgs e)
    {
        var point = e.GetPosition(chart);
        var plot = chart.Plot();
        _pressed = Command(plot, point);
        if (Plots.LegendOf(plot) is { } legend && LegendItemAt(legend, point) is { } item)
        {
            if (legend.Spec.ClickCommand is null && legend.Spec.Toggles && item.Toggles)
            {
                Concealed = Concealed.Contains(item.Target) ? Concealed.Remove(item.Target) : Concealed.Add(item.Target);
                chart.Invalidate();
            }

            e.Handled = true;
        }
        else if (plot is ChartLayout { Brush: { } brush } && BrushDrag.Start(brush, point.X, point.Y) is { } drag)
        {
            Drag = drag;
            e.Pointer.Capture(chart);
            e.Handled = true;
        }
        else if (plot.TipsOnClick && Plots.Hovered(plot, point.X, point.Y) is var clicked and >= 0)
        {
            Active = clicked;
            chart.InvalidateVisual();
        }
        else if (e.Pointer.Type == PointerType.Touch && !plot.TipsOnClick)
        {
            Hover(plot, point);
        }
    }

    /// <summary>A finger touched the window outside the chart: its tooltip hides, as Recharts' wrapper's <c>mouseleave</c> hides it on a touch screen.</summary>
    public void WindowPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!ChartTouch.Outside(chart, e) || chart.Plot() is { TipsOnClick: true } or { KeepsTip: true })
        {
            return;
        }

        Left = true;
        Show(ChartPainter.NoTips);
    }

    public void Released(PointerReleasedEventArgs e)
    {
        if (Drag is not null)
        {
            Drag = null;
            e.Pointer.Capture(null);
            chart.Invalidate();
            return;
        }

        var at = e.GetPosition(chart);
        Web.SetHref(chart, Link(at));
        var released = Command(chart.Plot(), at);
        if (released is { } command && command == _pressed && chart.Command(command.Key) is { } run && run.CanExecute(command.Parameter))
        {
            run.Execute(command.Parameter);
            e.Handled = true;
        }

        _pressed = null;
    }

    public void Moved(PointerEventArgs e)
    {
        var point = e.GetPosition(chart);
        if (Drag is { } drag)
        {
            DragTo(drag, point.X);
            return;
        }

        HoverBrush(point);
        var plot = chart.Plot();
        LinkCursor(plot, point);
        if (plot.TipsOnClick || (e.Pointer.Type == PointerType.Touch && !ChartTouch.Follows(plot)))
        {
            return;
        }

        Hover(plot, point);
    }

    private void Hover(IChartPlot plot, Point point)
    {
        var active = Plots.Hovered(plot, point.X, point.Y);
        if (active < 0 && Active >= 0 && plot.KeepsTip)
        {
            return;
        }

        if (active >= 0 && active == Active && At != point)
        {
            At = point;
            chart.InvalidateVisual();
            return;
        }

        Show(active, at: point);
    }

    public void Exited()
    {
        if (chart.Plot() is ChartLayout { Brush: { } brush })
        {
            LeaveBrush(brush);
        }

        if (chart.Plot() is { TipsOnClick: true } or { KeepsTip: true })
        {
            return;
        }

        Left = true;
        Show(ChartPainter.NoTips);
    }

    /// <summary>Shows a hover state (a synced chart's, or none), raising <see cref="Hovered"/> when it changes.</summary>
    public void Show(int active, bool raise = true, Point? at = null)
    {
        if (active == Active)
        {
            return;
        }

        Active = active;
        At = active >= 0 ? at : null;
        chart.InvalidateVisual();
        if (active >= 0)
        {
            chart.KeysPointed();
        }

        if (raise)
        {
            Hovered?.Invoke(this, active);
        }
    }

    /// <summary>Forgets the shapes of the last layout's targets.</summary>
    public void Forget() => _paths.Clear();

    /// <summary>The command under a point: the topmost click target, a legend item's, or a nest treemap index box's.</summary>
    private (string Key, string Parameter)? Command(IChartPlot plot, Point point)
    {
        if (Plots.LegendOf(plot) is { Spec.ClickCommand: { } legendKey } legend && LegendItemAt(legend, point) is { } item)
        {
            return (legendKey, ClickTargets.Index(item.Index));
        }

        if (plot is TreemapPlot { Treemap: { Nest: true, ZoomCommand: { } zoom } } treemap && Box(treemap, point) is { } box)
        {
            return (zoom, box.Path);
        }

        var targets = chart.Clicks();
        for (var i = targets.Count - 1; i >= 0; i--)
        {
            if (targets[i].Contains(point.X, point.Y, InPath))
            {
                return targets[i].Href is null ? (targets[i].Key, targets[i].Parameter) : null;
            }
        }

        return null;
    }

    /// <summary>The hand over a link, as the web's anchor shows it, for a chart with links.</summary>
    private void LinkCursor(IChartPlot plot, Point point)
    {
        if (!Interactions.Links(plot.Spec))
        {
            return;
        }

        var over = Link(point) is not null;
        if (over != _overLink)
        {
            _overLink = over;
            chart.Cursor = over ? new Cursor(StandardCursorType.Hand) : null;
        }
    }

    private bool _overLink;

    /// <summary>The address of the topmost link under a point, for Aioros's desktop target to follow, or none.</summary>
    private string? Link(Point point) => chart.Clicks().LastOrDefault(target => target.Contains(point.X, point.Y, InPath))?.Href;

    private bool InPath(string path, double x, double y)
    {
        if (!_paths.TryGetValue(path, out var geometry))
        {
            geometry = StreamGeometry.Parse(path);
            _paths[path] = geometry;
        }

        return geometry.FillContains(new Point(x, y));
    }

    private static NestBox? Box(TreemapPlot plot, Point point)
    {
        var top = plot.Spec.Height + 8;
        var line = Text.InterMetrics.LineHeight(plot.Spec.FontSize);
        if (point.Y < top || point.Y > top + line)
        {
            return null;
        }

        foreach (var (box, left, width) in NestBoxes.Place(plot.Trail, plot.Spec.FontSize, plot.Spec.Width, name => Text.InterMetrics.Width(name, plot.Spec.FontSize)))
        {
            if (point.X >= left && point.X <= left + width)
            {
                return box;
            }
        }

        return null;
    }

    private static LegendItem? LegendItemAt(LegendLayout legend, Point point)
    {
        foreach (var item in legend.Items)
        {
            if (point.X >= item.X && point.X <= item.X + item.Width && point.Y >= item.Y && point.Y <= item.Y + legend.RowHeight)
            {
                return item;
            }
        }

        return null;
    }

    private void DragTo(BrushDrag drag, double x)
    {
        if (chart.Plot() is not ChartLayout { Brush: { } brush } || chart.Elements.OfType<Brush>().LastOrDefault() is not { } element)
        {
            return;
        }

        var (start, end) = drag.Move(brush, x);
        if (start != brush.StartIndex || end != brush.EndIndex)
        {
            element.StartIndex = start;
            element.EndIndex = end;
            ChartSync.Window(chart, start, end);
        }
        else
        {
            chart.Invalidate();
        }
    }

    /// <summary>
    /// Recharts' brush texts on hover: shown when the pointer enters a traveller or the slide, hidden
    /// <see cref="Specs.BrushSpec.LeaveTimeOut"/> milliseconds after it leaves the brush (through the chart's clock).
    /// </summary>
    private void HoverBrush(Point point)
    {
        if (chart.Plot() is not ChartLayout { Brush: { } brush })
        {
            return;
        }

        if (BrushDrag.Start(brush, point.X, point.Y) is not null)
        {
            _brushLeave?.Dispose();
            _brushLeave = null;
            if (!BrushTexts)
            {
                BrushTexts = true;
                chart.Invalidate();
            }
        }
        else if (point.X < brush.X || point.X > brush.X + brush.Width || point.Y < brush.Y || point.Y > brush.Y + brush.Height)
        {
            LeaveBrush(brush);
        }
    }

    private void LeaveBrush(BrushLayout brush)
    {
        if (!BrushTexts || _brushLeave is not null)
        {
            return;
        }

        _brushLeave = BrushDrag.Leave(chart.Time, brush.Spec.LeaveTimeOut, () => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _brushLeave?.Dispose();
            _brushLeave = null;
            BrushTexts = false;
            chart.Invalidate();
        }));
    }

    /// <summary>The command a key names on a chart: its own, its items', a nest treemap's zoom, or one of its children's.</summary>
    public static ICommand? Resolve(Chart chart, string key) => key switch
    {
        ChartSlot.Click => chart.ClickCommand,
        ChartSlot.ItemClick => chart.ItemClickCommand,
        ChartSlot.Zoom => (chart as Treemap)?.ZoomCommand,
        _ when key.StartsWith('c') && int.TryParse(key.AsSpan(1), System.Globalization.CultureInfo.InvariantCulture, out var order) && order < chart.Elements.Count
            && chart.Elements[order] is IClickSource source and AvaloniaObject target => target.GetValue(source.CommandProperty) as ICommand,
        _ => null,
    };
}
