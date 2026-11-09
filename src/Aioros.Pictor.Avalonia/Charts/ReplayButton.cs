using Aioros.Pictor.Svg;
using Aioros.Runtime.Rendering;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Aioros.Pictor;

/// <summary>
/// A button that plays the enter animations of the charts on its page (its window) again, as remounting a Recharts
/// chart does. On the web it needs no script and no request: a checkbox in a label whose every change restarts the
/// charts' CSS animations.
/// </summary>
public sealed class ReplayButton : Button, IWrittenBySlot
{
    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(Button);

    /// <inheritdoc />
    public WrittenSlot? WrittenSlot => WrittenSlot.Of<ReplaySlotWriter>(Content?.ToString() ?? "Replay", null);

    /// <inheritdoc />
    protected override void OnClick()
    {
        base.OnClick();
        if (TopLevel.GetTopLevel(this) is { } window)
        {
            foreach (var chart in window.GetVisualDescendants().OfType<Chart>())
            {
                chart.ReplayEnter();
            }
        }
    }
}
