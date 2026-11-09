using Avalonia;
using Avalonia.Automation.Peers;

namespace Aioros.Pictor.Automation;

/// <summary>One hover state of a chart (a category or an item) as assistive technologies read it: the text of its tooltip.</summary>
internal sealed class ChartItemAutomationPeer(ChartAutomationPeer chart, string text, int index, int count) : AutomationPeer
{
    /// <summary>The state's place among the chart's states.</summary>
    public int Index => index;

    protected override void BringIntoViewCore()
    {
    }

    protected override string? GetAcceleratorKeyCore() => null;

    protected override string? GetAccessKeyCore() => null;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

    protected override string? GetAutomationIdCore() => null;

    protected override Rect GetBoundingRectangleCore() => chart.GetBoundingRectangle();

    protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore() => [];

    protected override string GetClassNameCore() => "ChartItem";

    protected override AutomationPeer? GetLabeledByCore() => null;

    protected override string? GetNameCore() => text;

    protected override string? GetHelpTextCore() => $"{index + 1} of {count}";

    protected override AutomationPeer? GetParentCore() => chart;

    protected override bool HasKeyboardFocusCore() => false;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    protected override bool IsEnabledCore() => true;

    protected override bool IsKeyboardFocusableCore() => false;

    protected override void SetFocusCore()
    {
    }

    protected override bool ShowContextMenuCore() => false;

    protected override bool TrySetParent(AutomationPeer? parent) => false;
}
