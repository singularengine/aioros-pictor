using System.Windows.Input;
using Avalonia;

namespace Aioros.Pictor;

/// <summary>Recharts' item <c>onClick</c> as a command, shared by every element whose items take clicks (series, pies, radars, radial bars, funnels, the legend).</summary>
internal static class Clickable
{
    public static readonly AttachedProperty<ICommand?> CommandProperty = AvaloniaProperty.RegisterAttached<Series, AvaloniaObject, ICommand?>("ClickCommand");

    /// <summary>The key of an element's command, by its place among the chart's children, when its command is set; none otherwise, so nothing is written for it.</summary>
    public static string? Key(AvaloniaObject element, int order) => element.IsSet(CommandProperty) ? ChartSlot.CommandKey(order) : null;
}
