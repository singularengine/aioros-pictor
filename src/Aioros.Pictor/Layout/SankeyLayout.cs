using Aioros.Pictor.Data;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>A Sankey node laid out: its column, its box, its value and the links that leave and reach it.</summary>
internal sealed class SankeyNode
{
    public string? Name { get; init; }

    public double Value { get; set; }

    public int Depth { get; set; }

    public double X { get; set; } = double.NaN;

    public double Dx { get; set; } = double.NaN;

    public double Y { get; set; }

    public double Dy { get; set; }

    /// <summary>The links that leave the node (Recharts' <c>targetLinks</c>) and the nodes they reach.</summary>
    public required List<int> TargetLinks { get; init; }

    public required List<int> TargetNodes { get; init; }

    /// <summary>The links that reach the node (Recharts' <c>sourceLinks</c>) and the nodes they leave.</summary>
    public required List<int> SourceLinks { get; init; }

    public required List<int> SourceNodes { get; init; }

    public double Center => Y + (Dy / 2);
}

/// <summary>A Sankey link laid out: its ends, its value, its width and where it leaves and reaches its nodes.</summary>
internal sealed class SankeyLink
{
    public int Source { get; init; }

    public int Target { get; init; }

    public double Value { get; init; }

    public double Dy { get; set; }

    public double Sy { get; set; }

    public double Ty { get; set; }
}

/// <summary>
/// A Sankey laid out as Recharts lays it out (<c>computeData</c>): depths from the sources, columns across the width,
/// heights by one value ratio, then relaxation passes right to left and left to right with collisions resolved after
/// each, and the links stacked on their nodes in the order of the nodes they join.
/// </summary>
internal sealed class SankeyPlot : IChartPlot
{
    private SankeyPlot(ChartSpec spec, SankeySpec sankey, SankeyNode[] nodes, SankeyLink[] links)
    {
        Spec = spec;
        Sankey = sankey;
        Nodes = nodes;
        Links = links;
    }

    public ChartSpec Spec { get; }

    public SankeySpec Sankey { get; }

    public SankeyNode[] Nodes { get; }

    public SankeyLink[] Links { get; }

    /// <summary>Links first, then nodes, the order they are drawn in.</summary>
    public int HoverCount => Spec.Tooltip is null ? 0 : Links.Length + Nodes.Length;

    public static SankeyPlot Compute(ChartSpec spec, ChartData data)
    {
        var sankey = spec.Sankey ?? new SankeySpec();
        var nodeData = data.Member(data.Resolve("nodes", root: true));
        var linkData = data.Member(data.Resolve("links", root: true));
        var links = ReadLinks(linkData, nodeData.Count);
        var names = nodeData.Texts(nodeData.Resolve(sankey.NameKey));
        var nodes = Joined(links, names);

        var width = spec.Width - spec.Margin.Left - spec.Margin.Right;
        var height = spec.Height - spec.Margin.Top - spec.Margin.Bottom;
        if (nodes.Length > 0)
        {
            Arrange(nodes, links, sankey, width, height);
        }

        return new SankeyPlot(spec, sankey, nodes, links);
    }

    private static SankeyLink[] ReadLinks(ChartData data, int nodes)
    {
        var sources = data.Numbers(data.Resolve("source"));
        var targets = data.Numbers(data.Resolve("target"));
        var values = data.Numbers(data.Resolve("value"));
        var links = new List<SankeyLink>(data.Count);
        for (var i = 0; i < data.Count; i++)
        {
            if (Index(sources[i], nodes) is { } source && Index(targets[i], nodes) is { } target)
            {
                links.Add(new SankeyLink { Source = source, Target = target, Value = double.IsNaN(values[i]) ? 0 : values[i] });
            }
        }

        return [.. links];
    }

    private static int? Index(double value, int count) => value >= 0 && value < count && value == Math.Floor(value) ? (int)value : null;

    /// <summary>Each node with its links in and out, in the links' order, and its value (the larger of what flows in and out), from one pass over the links.</summary>
    private static SankeyNode[] Joined(SankeyLink[] links, string?[] names)
    {
        var nodes = new SankeyNode[names.Length];
        for (var i = 0; i < nodes.Length; i++)
        {
            nodes[i] = new SankeyNode { Name = names[i], TargetLinks = [], TargetNodes = [], SourceLinks = [], SourceNodes = [] };
        }

        var sources = new double[nodes.Length];
        var targets = new double[nodes.Length];
        for (var i = 0; i < links.Length; i++)
        {
            var (from, to) = (nodes[links[i].Source], nodes[links[i].Target]);
            from.TargetNodes.Add(links[i].Target);
            from.TargetLinks.Add(i);
            targets[links[i].Source] += links[i].Value;
            to.SourceNodes.Add(links[i].Source);
            to.SourceLinks.Add(i);
            sources[links[i].Target] += links[i].Value;
        }

        for (var i = 0; i < nodes.Length; i++)
        {
            nodes[i].Value = Math.Max(sources[i], targets[i]);
        }

        return nodes;
    }

    private static void Arrange(SankeyNode[] nodes, SankeyLink[] links, SankeySpec sankey, double width, double height)
    {
        Depths(nodes);

        var maxDepth = nodes.Max(node => node.Depth);
        if (maxDepth >= 1)
        {
            var childWidth = (width - sankey.NodeWidth) / maxDepth;
            foreach (var node in nodes)
            {
                node.Depth = node.TargetNodes.Count == 0 ? maxDepth : node.Depth;
                node.X = node.Depth * childWidth;
                node.Dx = sankey.NodeWidth;
            }
        }

        var columns = new List<int>[nodes.Max(node => node.Depth) + 1];
        for (var i = 0; i < nodes.Length; i++)
        {
            (columns[nodes[i].Depth] ??= []).Add(i);
        }

        foreach (var column in columns)
        {
            column?.TrimExcess();
        }

        SankeyRelaxation.Run(nodes, links, [.. columns.Select(column => column ?? [])], sankey, height);
    }

    /// <summary>
    /// Recharts' <c>updateDepthOfTargets</c> from every node without a source: each target one deeper than its deepest
    /// source. Without a cycle that is each node's longest path from a source, taken once per link in topological order
    /// (Kahn's), where the recursion walked every path (exponential in a layered graph); with a cycle the recursion is kept,
    /// stopped after as many levels as there are nodes, so the depths are the same either way.
    /// </summary>
    private static void Depths(SankeyNode[] nodes)
    {
        var pending = new int[nodes.Length];
        var ready = new Queue<int>();
        for (var i = 0; i < nodes.Length; i++)
        {
            pending[i] = nodes[i].SourceNodes.Count;
            if (pending[i] == 0)
            {
                ready.Enqueue(i);
            }
        }

        var done = 0;
        while (ready.TryDequeue(out var index))
        {
            done++;
            var node = nodes[index];
            foreach (var target in node.TargetNodes)
            {
                nodes[target].Depth = Math.Max(node.Depth + 1, nodes[target].Depth);
                if (--pending[target] == 0)
                {
                    ready.Enqueue(target);
                }
            }
        }

        if (done == nodes.Length)
        {
            return;
        }

        foreach (var node in nodes)
        {
            node.Depth = 0;
        }

        foreach (var node in nodes.Where(node => node.SourceNodes.Count == 0))
        {
            Deepen(nodes, node, nodes.Length);
        }
    }

    private static void Deepen(SankeyNode[] nodes, SankeyNode node, int limit)
    {
        if (limit < 0)
        {
            return;
        }

        foreach (var target in node.TargetNodes)
        {
            nodes[target].Depth = Math.Max(node.Depth + 1, nodes[target].Depth);
            Deepen(nodes, nodes[target], limit - 1);
        }
    }
}
