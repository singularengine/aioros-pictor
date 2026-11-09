using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Aioros.Pictor;

/// <summary>
/// Recharts on a touch screen: a tap shows the state under the finger (the browser's compatibility mouse events), a
/// drag moves an axis tooltip with the finger (<c>handleTouchMove</c>) while an item's stays (items take no touch
/// events), lifting the finger keeps it, and a tap elsewhere in the window hides it (the chart's <c>mouseleave</c>).
/// </summary>
internal static class ChartTouch
{
    /// <summary>Whether a finger dragged over the chart moves its tooltip: an axis tooltip's category follows it, an item's stays.</summary>
    public static bool Follows(IChartPlot plot) => plot switch
    {
        ChartLayout layout => layout.Categories is not null && !layout.ItemTooltip && !layout.BarTooltip,
        PolarLayout polar => polar.Spec.Kind != Specs.ChartKind.Pie && !polar.ItemBars,
        _ => false,
    };

    /// <summary>Whether a touch press in the window landed outside the chart.</summary>
    public static bool Outside(Visual chart, PointerPressedEventArgs e) =>
        e.Pointer.Type == PointerType.Touch && !(e.Source is Visual source && (ReferenceEquals(source, chart) || chart.IsVisualAncestorOf(source)));

    /// <summary>Follows the touch presses of the chart's window, to hide its tooltip when the finger lands elsewhere.</summary>
    public static TopLevel? Watch(Visual chart, EventHandler<PointerPressedEventArgs> pressed)
    {
        var window = TopLevel.GetTopLevel(chart);
        window?.AddHandler(InputElement.PointerPressedEvent, pressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        return window;
    }

    /// <summary>Stops following the window's presses.</summary>
    public static void Unwatch(TopLevel? window, EventHandler<PointerPressedEventArgs> pressed) =>
        window?.RemoveHandler(InputElement.PointerPressedEvent, pressed);
}
