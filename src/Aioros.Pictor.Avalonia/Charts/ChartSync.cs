using Aioros.Pictor.Painting;
using Aioros.Pictor.Svg;
using Avalonia.Threading;

namespace Aioros.Pictor;

/// <summary>
/// The desktop's sync groups (Recharts' <c>syncId</c>): the charts in the visual tree with the same id show the hover
/// state matching the one hovered in any of them (the same index, or the same category for <c>syncMethod="value"</c>)
/// and a brush moved in one moves the brushes of the others, as Recharts' event center relays them. The groups are
/// touched on the UI thread only; a chart joins its group when it enters the visual tree and leaves it when it leaves.
/// </summary>
internal static class ChartSync
{
    private static readonly Dictionary<string, List<WeakReference<Chart>>> Groups = new(StringComparer.Ordinal);

    public static void Join(Chart chart, string id)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (!Groups.TryGetValue(id, out var group))
        {
            group = [];
            Groups[id] = group;
        }

        group.RemoveAll(member => !member.TryGetTarget(out var other) || ReferenceEquals(other, chart));
        group.Add(new WeakReference<Chart>(chart));
    }

    public static void Leave(Chart chart, string id)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (Groups.TryGetValue(id, out var group))
        {
            group.RemoveAll(member => !member.TryGetTarget(out var other) || ReferenceEquals(other, chart));
            if (group.Count == 0)
            {
                Groups.Remove(id);
            }
        }
    }

    /// <summary>Shows in every other chart of the group the state matching the one a chart hovers (none when it hovers none).</summary>
    public static void Hover(Chart chart, int state)
    {
        if (chart.SyncId is not { Length: > 0 } id)
        {
            return;
        }

        var label = state >= 0 && SyncMarkup.Categories(chart.Plot()) is { } categories && state < categories.Length ? categories[state] : null;
        foreach (var other in Others(chart, id))
        {
            other.Pointer.Show(Matching(other, state, label), raise: false);
        }
    }

    /// <summary>Moves the brush of every other chart of the group to a window, as Recharts' <c>applySyncEvent</c> does.</summary>
    public static void Window(Chart chart, int start, int end)
    {
        if (chart.SyncId is not { Length: > 0 } id)
        {
            return;
        }

        foreach (var other in Others(chart, id))
        {
            if (other.Elements.OfType<Brush>().LastOrDefault() is { } brush)
            {
                brush.StartIndex = start;
                brush.EndIndex = end;
            }
        }
    }

    private static int Matching(Chart chart, int state, string? label)
    {
        if (state < 0 || SyncMarkup.Categories(chart.Plot()) is not { } categories)
        {
            return ChartPainter.NoTips;
        }

        if (chart.SyncMethod is "value" or "Value")
        {
            var index = Array.IndexOf(categories, label);
            return index >= 0 ? index : ChartPainter.NoTips;
        }

        return state < categories.Length ? state : ChartPainter.NoTips;
    }

    private static List<Chart> Others(Chart chart, string id)
    {
        Dispatcher.UIThread.VerifyAccess();
        var others = new List<Chart>();
        if (Groups.TryGetValue(id, out var group))
        {
            foreach (var member in group)
            {
                if (member.TryGetTarget(out var other) && !ReferenceEquals(other, chart))
                {
                    others.Add(other);
                }
            }
        }

        return others;
    }
}
