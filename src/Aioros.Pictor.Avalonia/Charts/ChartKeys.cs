using Aioros.Pictor.Painting;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Aioros.Pictor;

/// <summary>
/// Recharts' keyboard on a desktop chart: its <c>AccessibilityManager</c> (with <c>accessibilityLayer</c>, focus shows
/// the active category's tooltip, the default index or the first, and <c>ArrowRight</c> and <c>ArrowLeft</c> move it
/// within the categories of a horizontal chart; no other key moves it) and its tooltip's <c>Escape</c>, which hides the
/// tooltip's box, wherever in the window the key is pressed, until the tooltip moves; and the window's touches, which
/// hide the tooltip when a finger lands outside the chart.
/// </summary>
internal sealed class ChartKeys(Chart chart)
{
    private int _key = -1;
    private TopLevel? _window;

    /// <summary>The hover state whose tooltip box Escape hid, or a negative number.</summary>
    public int Dismissed { get; private set; } = ChartPainter.NoTips;

    /// <summary>The key step of a scatter chart whose cross the keyboard draws (Recharts' spoofed pointer), or a negative number.</summary>
    public int Cross { get; private set; } = ChartPainter.NoTips;

    /// <summary>The pointer entered an item: the keyboard's cross goes, as Recharts' item handlers replace the spoofed pointer's state.</summary>
    public void Pointed()
    {
        if (Cross >= 0)
        {
            Cross = ChartPainter.NoTips;
            chart.InvalidateVisual();
        }
    }

    /// <summary>The active category after a key, or null when the key does not move it (Recharts clamps it within the categories).</summary>
    public static int? Move(Key key, int active, int count) => key switch
    {
        Key.Right => Math.Min(active + 1, count - 1),
        Key.Left => Math.Max(active - 1, 0),
        _ => null,
    };

    /// <summary>The chart took focus: the active category's tooltip shows.</summary>
    public void Focused()
    {
        var plot = chart.Plot();
        if (Plots.Keyboard(plot))
        {
            Activate(_key < 0 ? Math.Max(plot.DefaultTip, 0) : _key);
        }
        else if (ScatterKeys.Applies(plot))
        {
            Step(_key < 0 ? Math.Max(plot.DefaultTip, 0) : _key);
        }
    }

    /// <summary>A key went down in the focused chart: the arrows move the active category.</summary>
    public void Pressed(KeyEventArgs e)
    {
        var plot = chart.Plot();
        if (Plots.Keyboard(plot) && Move(e.Key, Math.Max(_key, 0), plot.HoverCount) is { } next)
        {
            Activate(next);
            e.Handled = true;
        }
        else if (ScatterKeys.Applies(plot) && ScatterKeys.Move(e.Key == Key.Right, e.Key == Key.Left, Math.Max(_key, 0), ScatterKeys.Points((Layout.ChartLayout)plot).X.Length) is { } step)
        {
            Step(step);
            e.Handled = true;
        }
    }

    private void Step(int step)
    {
        _key = step;
        Cross = step;
        chart.InvalidateVisual();
    }

    /// <summary>Follows the window's keys and touches while the chart is in it.</summary>
    public void Attach()
    {
        _window = ChartTouch.Watch(chart, chart.Pointer.WindowPressed);
        _window?.AddHandler(InputElement.KeyDownEvent, OnWindowKey, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    /// <summary>Stops following the window.</summary>
    public void Detach()
    {
        ChartTouch.Unwatch(_window, chart.Pointer.WindowPressed);
        _window?.RemoveHandler(InputElement.KeyDownEvent, OnWindowKey);
        _window = null;
    }

    private void Activate(int index)
    {
        _key = Math.Clamp(index, 0, chart.Plot().HoverCount - 1);
        chart.Pointer.Show(_key);
    }

    private void OnWindowKey(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && chart.ActiveIndex >= 0 && Dismissed != chart.ActiveIndex)
        {
            Dismissed = chart.ActiveIndex;
            chart.InvalidateVisual();
        }
    }
}
