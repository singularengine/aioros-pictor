using Aioros.Pictor.Painting;
using Avalonia.Automation;
using Avalonia.Automation.Peers;

namespace Aioros.Pictor.Automation;

/// <summary>
/// What assistive technologies read of a chart: its name (its <c>Title</c>, else its automation name), a summary of its
/// family, series and hover states, and one item per hover state (each category or item) named by the text its tooltip
/// shows, its label then each series' name and value, as Recharts' <c>accessibilityLayer</c> lets a screen reader reach
/// the tooltip's content.
/// </summary>
internal sealed class ChartAutomationPeer(Chart owner) : ControlAutomationPeer(owner)
{
    private IReadOnlyList<AutomationPeer>? _items;
    private IChartPlot? _plot;

    private new Chart Owner => (Chart)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

    protected override string GetClassNameCore() => Owner.GetType().Name;

    protected override string? GetNameCore() => Owner.Title ?? base.GetNameCore() ?? Owner.GetType().Name;

    protected override string GetHelpTextCore()
    {
        var plot = Owner.Plot();
        var help = AutomationProperties.GetHelpText(Owner);
        return string.IsNullOrEmpty(help) ? $"{Owner.GetType().Name}, {plot.HoverCount} {(plot.HoverCount == 1 ? "item" : "items")}" : help;
    }

    protected override bool IsKeyboardFocusableCore() => Owner.Focusable;

    protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore()
    {
        var plot = Owner.Plot();
        if (_items is not null && ReferenceEquals(plot, _plot))
        {
            return _items;
        }

        var texts = TipTexts.Of(plot);
        var items = new AutomationPeer[texts.Length];
        for (var i = 0; i < texts.Length; i++)
        {
            items[i] = new ChartItemAutomationPeer(this, texts[i], i, texts.Length);
        }

        (_plot, _items) = (plot, items);
        return items;
    }

    /// <summary>The chart's data changed: its items are read again.</summary>
    public void Refresh()
    {
        _items = null;
        InvalidateChildren();
    }
}
