using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>
/// Recharts' vertical placement of a Sankey's nodes (<c>updateYOfTree</c>, <c>resolveCollisions</c>,
/// <c>relaxLeftToRight</c>, <c>relaxRightToLeft</c>, <c>updateYOfLinks</c>), operation for operation; its sorts are
/// JavaScript's stable sorts.
/// </summary>
internal static class SankeyRelaxation
{
    private const int ShortList = 32;

    public static void Run(SankeyNode[] nodes, SankeyLink[] links, List<int>[] columns, SankeySpec sankey, double height)
    {
        var ratio = double.PositiveInfinity;
        foreach (var column in columns)
        {
            var sum = 0.0;
            foreach (var node in column)
            {
                sum += nodes[node].Value;
            }

            ratio = Math.Min(ratio, (height - ((column.Count - 1) * sankey.NodePadding)) / sum);
        }

        foreach (var column in columns)
        {
            for (var i = 0; i < column.Count; i++)
            {
                nodes[column[i]].Y = i;
                nodes[column[i]].Dy = nodes[column[i]].Value * ratio;
            }
        }

        foreach (var link in links)
        {
            link.Dy = link.Value * ratio;
        }

        Collisions(nodes, columns, height, sankey.NodePadding, sankey.Sort);
        var alpha = 1.0;
        for (var i = 1; i <= sankey.Iterations; i++)
        {
            alpha *= 0.99;
            RightToLeft(nodes, links, columns, alpha);
            Collisions(nodes, columns, height, sankey.NodePadding, sankey.Sort);
            LeftToRight(nodes, links, columns, alpha);
            Collisions(nodes, columns, height, sankey.NodePadding, sankey.Sort);
        }

        Stack(nodes, links);
    }

    private static void Collisions(SankeyNode[] nodes, List<int>[] columns, double height, double padding, bool sort)
    {
        foreach (var column in columns)
        {
            if (sort)
            {
                StableSort(column, new NodeY(nodes));
            }

            var y0 = 0.0;
            foreach (var index in column)
            {
                var node = nodes[index];
                var dy = y0 - node.Y;
                if (dy > 0)
                {
                    node.Y += dy;
                }

                y0 = node.Y + node.Dy + padding;
            }

            y0 = height + padding;
            for (var j = column.Count - 1; j >= 0; j--)
            {
                var node = nodes[column[j]];
                var dy = node.Y + node.Dy + padding - y0;
                if (!(dy > 0))
                {
                    break;
                }

                node.Y -= dy;
                y0 = node.Y;
            }
        }
    }

    private static void LeftToRight(SankeyNode[] nodes, SankeyLink[] links, List<int>[] columns, double alpha)
    {
        foreach (var column in columns)
        {
            Relax(nodes, links, column, alpha, targets: false);
        }
    }

    private static void RightToLeft(SankeyNode[] nodes, SankeyLink[] links, List<int>[] columns, double alpha)
    {
        for (var i = columns.Length - 1; i >= 0; i--)
        {
            Relax(nodes, links, columns[i], alpha, targets: true);
        }
    }

    private static void Relax(SankeyNode[] nodes, SankeyLink[] links, List<int> column, double alpha, bool targets)
    {
        for (var j = 0; j < column.Count; j++)
        {
            var node = nodes[column[j]];
            var joined = targets ? node.TargetLinks : node.SourceLinks;
            if (joined.Count > 0 && Weighted(nodes, links, joined, targets) is var weighted && double.IsFinite(weighted))
            {
                node.Y += (weighted - node.Center) * alpha;
            }
        }
    }

    private static double Weighted(SankeyNode[] nodes, SankeyLink[] links, List<int> joined, bool targets)
    {
        var sum = 0.0;
        var weighted = 0.0;
        foreach (var link in joined)
        {
            sum += links[link].Value;
            weighted += nodes[targets ? links[link].Target : links[link].Source].Center * links[link].Value;
        }

        return weighted / sum;
    }

    private static void Stack(SankeyNode[] nodes, SankeyLink[] links)
    {
        foreach (var node in nodes)
        {
            StableSort(node.TargetLinks, new LinkY(nodes, links, Targets: true));
            StableSort(node.SourceLinks, new LinkY(nodes, links, Targets: false));
            var sy = 0.0;
            foreach (var link in node.TargetLinks)
            {
                links[link].Sy = sy;
                sy += links[link].Dy;
            }

            var ty = 0.0;
            foreach (var link in node.SourceLinks)
            {
                links[link].Ty = ty;
                ty += links[link].Dy;
            }
        }
    }

    /// <summary>
    /// JavaScript's stable sort by a key (<c>(a, b) =&gt; a.y - b.y</c>): a stable O(n log n) sort when every key is a
    /// number, the insertion sort otherwise, whose order a NaN key (which compares as neither) decides. A short list is
    /// insertion sorted either way: over numbers both orders are the one stable order, and it allocates nothing.
    /// </summary>
    private static void StableSort<TKey>(List<int> items, TKey key)
        where TKey : struct, ISortKey
    {
        if (items.Count <= ShortList)
        {
            Insertion(items, key);
            return;
        }

        var keys = new (double Key, int Order, int Item)[items.Count];
        var numbers = true;
        for (var i = 0; i < keys.Length; i++)
        {
            keys[i] = (key.Of(items[i]), i, items[i]);
            numbers &= !double.IsNaN(keys[i].Key);
        }

        if (numbers)
        {
            Array.Sort(keys, static (a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : a.Order.CompareTo(b.Order));
            for (var i = 0; i < keys.Length; i++)
            {
                items[i] = keys[i].Item;
            }

            return;
        }

        Insertion(items, key);
    }

    private static void Insertion<TKey>(List<int> items, TKey key)
        where TKey : struct, ISortKey
    {
        for (var i = 1; i < items.Count; i++)
        {
            var item = items[i];
            var value = key.Of(item);
            var j = i - 1;
            while (j >= 0 && key.Of(items[j]) - value > 0)
            {
                items[j + 1] = items[j];
                j--;
            }

            items[j + 1] = item;
        }
    }

    /// <summary>What a sort orders by: a node's or a link's other end's top.</summary>
    private interface ISortKey
    {
        double Of(int item);
    }

    /// <summary>A node's top.</summary>
    private readonly record struct NodeY(SankeyNode[] Nodes) : ISortKey
    {
        public double Of(int item) => Nodes[item].Y;
    }

    /// <summary>The top of the node a link reaches (its target) or leaves (its source).</summary>
    private readonly record struct LinkY(SankeyNode[] Nodes, SankeyLink[] Links, bool Targets) : ISortKey
    {
        public double Of(int item) => Nodes[Targets ? Links[item].Target : Links[item].Source].Y;
    }
}
