using Aioros.Pictor.Layout;

namespace Aioros.Pictor.Motion;

/// <summary>
/// Recharts' <c>Funnel</c> update: each trapezoid from the previous one at its index (x, y, both widths and height
/// interpolated); a trapezoid with none grows from its center.
/// </summary>
internal sealed class FunnelMorph : IMorphPart
{
    private readonly FunnelSeries _from;
    private readonly FunnelSeries _to;
    private readonly Timing _timing;

    private FunnelMorph(FunnelSeries from, FunnelSeries to, Timing timing)
    {
        (_from, _to, _timing) = (from, to, timing);
        Frame = new FunnelSeries { Spec = to.Spec, Data = to.Data, Items = new Trapezoid[to.Items.Length] };
    }

    public FunnelSeries Frame { get; }

    public double End => _timing.End;

    public static Morph? Plan(FunnelPlot from, FunnelPlot to)
    {
        var parts = new List<IMorphPart>();
        var funnels = new List<FunnelSeries>(to.Funnels.Count);
        for (var i = 0; i < to.Funnels.Count; i++)
        {
            var next = to.Funnels[i];
            var previous = i < from.Funnels.Count ? from.Funnels[i] : null;
            if (previous is null || Animations.Of(next.Spec) is not { } timing || Unchanged(previous, next))
            {
                funnels.Add(new FunnelSeries { Spec = next.Spec, Data = next.Data, Items = next.Items, Still = true });
                continue;
            }

            var morph = new FunnelMorph(previous, next, timing);
            parts.Add(morph);
            funnels.Add(morph.Frame);
        }

        return Morph.Of(to.With(funnels), parts);
    }

    public void At(double elapsed)
    {
        var t = _timing.Progress(elapsed);
        var items = Frame.Items;
        for (var i = 0; i < items.Length; i++)
        {
            var item = _to.Items[i];
            if (i < _from.Items.Length)
            {
                var prev = _from.Items[i];
                items[i] = item with
                {
                    X = Morph.Lerp(prev.X, item.X, t),
                    Y = Morph.Lerp(prev.Y, item.Y, t),
                    UpperWidth = Morph.Lerp(prev.UpperWidth, item.UpperWidth, t),
                    LowerWidth = Morph.Lerp(prev.LowerWidth, item.LowerWidth, t),
                    Height = Morph.Lerp(prev.Height, item.Height, t),
                };
            }
            else
            {
                items[i] = item with
                {
                    X = Morph.Lerp(item.X + (item.UpperWidth / 2), item.X, t),
                    Y = Morph.Lerp(item.Y + (item.Height / 2), item.Y, t),
                    UpperWidth = Morph.Lerp(0, item.UpperWidth, t),
                    LowerWidth = Morph.Lerp(0, item.LowerWidth, t),
                    Height = Morph.Lerp(0, item.Height, t),
                };
            }
        }
    }

    private static bool Unchanged(FunnelSeries a, FunnelSeries b)
    {
        if (a.Items.Length != b.Items.Length || !Morph.SameData(a.Data, b.Data))
        {
            return false;
        }

        for (var i = 0; i < a.Items.Length; i++)
        {
            var (p, n) = (a.Items[i], b.Items[i]);
            if (p.X != n.X || p.Y != n.Y || p.UpperWidth != n.UpperWidth || p.LowerWidth != n.LowerWidth || p.Height != n.Height)
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Recharts' <c>Treemap</c> update. Each node is keyed by its place and name (<c>recharts-treemap-node-x-y-name</c>)
/// among its parent's children: a node whose key the previous render had keeps its element and resizes from its
/// previous rectangle (react-smooth runs the update from the previous <c>to</c> without the begin delay); every other
/// node is mounted again and slides in as it enters. The nodes below depth 2 wait for the end, as when entering.
/// </summary>
internal sealed class TreemapMorph : IMorphPart
{
    private readonly List<(TreemapNode Frame, TreemapNode From, TreemapNode To)> _kept = [];
    private readonly Timing _resize;
    private readonly double _end;
    private readonly double _lag;

    /// <summary>
    /// A treemap's resize; with <c>isUpdateAnimationActive</c> off, each node's <c>Rectangle</c> restarts its own
    /// animation from its previous target whenever the node's frame moves, so it shows the node one animation frame late
    /// (recorded, <c>TreemapRectanglesValues</c>).
    /// </summary>
    private TreemapMorph(Timing timing, double lag)
    {
        _resize = timing with { Begin = 0 };
        _end = timing.End + lag;
        _lag = lag;
    }

    public double End => _end;

    public static Morph? Plan(TreemapPlot from, TreemapPlot to)
    {
        if (Animations.Of(to.Treemap) is not { } timing)
        {
            return null;
        }

        var morph = new TreemapMorph(timing, to.Treemap.UpdateAnimation || to.Treemap.Content is not null ? 0 : 16);
        var changed = false;
        var root = morph.Copy(from.Root, to.Root, kept: true, ref changed);
        return changed ? Morph.Of(to.With(root), [morph], enters: true) : null;
    }

    public void At(double elapsed)
    {
        var t = _resize.Progress(elapsed - _lag);
        foreach (var (frame, from, to) in _kept)
        {
            frame.Width = Morph.Lerp(from.Width, to.Width, t);
            frame.Height = Morph.Lerp(from.Height, to.Height, t);
        }
    }

    private TreemapNode Copy(TreemapNode? previous, TreemapNode node, bool kept, ref bool changed)
    {
        changed |= !kept || previous!.Width != node.Width || previous.Height != node.Height;
        var children = node.Children is { } nodes ? new TreemapNode[nodes.Length] : null;
        for (var i = 0; i < (children?.Length ?? 0); i++)
        {
            var child = node.Children![i];
            var match = kept ? previous!.Children?.FirstOrDefault(old => old.X == child.X && old.Y == child.Y && old.Name == child.Name) : null;
            children![i] = Copy(match, child, match is not null, ref changed);
        }

        var copy = new TreemapNode
        {
            Name = node.Name,
            Value = node.Value,
            Depth = node.Depth,
            Index = node.Index,
            Children = children,
            X = node.X,
            Y = node.Y,
            Width = node.Width,
            Height = node.Height,
            Area = node.Area,
            Kept = kept,
        };
        if (kept && (previous!.Width != node.Width || previous.Height != node.Height))
        {
            _kept.Add((copy, previous, node));
        }

        return copy;
    }
}
