using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>A shape's tooltip anchor, what its tooltip says and the box Recharts keeps the tooltip in.</summary>
internal readonly record struct ItemTip(double X, double Y, string Name, double Value, bool None = false);

/// <summary>
/// Recharts' tooltip of one item (a trapezoid, a treemap node, a Sankey node or link): its default content, a name and a
/// value with no label, placed 10 px right of and below the item's anchor and flipped to stay inside the view box, as
/// <c>getTooltipTranslateByAxis</c> places it.
/// </summary>
internal static class ItemTips
{
    /// <summary>Writes an item's tooltip: its default content, or the tooltip's template with the item's index in its data (whose payload a template reads), when it has one.</summary>
    public static void Tooltip<TCanvas>(ref TCanvas canvas, ChartSpec spec, in ItemTip tip, double left, double top, double width, double height, Data.ChartData? data = null, int index = -1)
        where TCanvas : struct, IChartCanvas
    {
        var tooltip = spec.Tooltip!;
        var item = new TooltipItem(tip.Name, "value", tip.Value, null, spec.Colors.TooltipText, None: tip.None);
        var payload = TooltipBox.Finish(tooltip, new Span<TooltipItem>(ref item));
        var entry = default(TooltipEntry);
        var count = TooltipBox.Entries(tooltip, new ReadOnlySpan<TooltipItem>(in item)[..payload], new Span<TooltipEntry>(ref entry));
        canvas.Open(default);
        TooltipBox.Write(ref canvas, tooltip, spec.FontSize, tip.X, tip.Y, string.Empty, new ReadOnlySpan<TooltipEntry>(in entry)[..count], payload, (left, top, width, height), default, ContentProps.Of(tooltip.Content, Content.ContentSlots.Tooltip, data), index);
        canvas.Close();
    }
}
