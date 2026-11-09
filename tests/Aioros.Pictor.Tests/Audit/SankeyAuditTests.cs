using Aioros.Pictor.Data;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;

namespace Aioros.Pictor.Tests.Audit;

public sealed class SankeyAuditTests
{
    [Fact]
    [Trait("AC", "AUDIT-34")]
    public async Task SankeyDepthsAreRechartsWithoutWalkingEveryPathAsync()
    {
        var example = await Reference.ExampleAsync("BudgetSankey");
        var random = new Random(7);
        for (var round = 0; round < 40; round++)
        {
            var count = random.Next(2, 14);
            var links = new List<(int, int)>();
            for (var i = 0; i < count * 2; i++)
            {
                var (a, b) = (random.Next(count), random.Next(count));
                if (a != b)
                {
                    links.Add(round % 4 == 0 ? (a, b) : (Math.Min(a, b), Math.Max(a, b)));
                }
            }

            Assert.Equal(Recursive(count, links), Depths(example, count, links));
        }

        const int layers = 30;
        List<(int, int)> layered = [.. Enumerable.Range(0, layers - 1).SelectMany(layer => from a in Enumerable.Range(0, 4) from b in Enumerable.Range(0, 4) select ((layer * 4) + a, ((layer + 1) * 4) + b))];
        Assert.Equal(Enumerable.Range(0, layers * 4).Select(node => node / 4), Depths(example, layers * 4, layered));
    }

    private static int[] Depths(RechartsExample example, int count, List<(int Source, int Target)> links)
    {
        var data = new Dictionary<string, object?>
        {
            ["nodes"] = Enumerable.Range(0, count).Select(i => (object?)new Dictionary<string, object?> { ["name"] = "n" + i }).ToList(),
            ["links"] = links.Select(link => (object?)new Dictionary<string, object?> { ["source"] = link.Source, ["target"] = link.Target, ["value"] = 1 }).ToList(),
        };
        return [.. SankeyPlot.Compute(example.Spec, ChartData.From(data, static (_, _) => null)).Nodes.Select(node => node.Depth)];
    }

    /// <summary>Recharts' recursion as Pictor ran it before (every path from every node without a source, stopped after as many levels as nodes), then the last column for nodes without targets.</summary>
    private static int[] Recursive(int count, List<(int Source, int Target)> links)
    {
        var depths = new int[count];
        void Deepen(int node, int limit)
        {
            if (limit < 0)
            {
                return;
            }

            foreach (var (_, target) in links.Where(link => link.Source == node))
            {
                depths[target] = Math.Max(depths[node] + 1, depths[target]);
                Deepen(target, limit - 1);
            }
        }

        foreach (var root in Enumerable.Range(0, count).Where(node => !links.Exists(link => link.Target == node)))
        {
            Deepen(root, count);
        }

        var max = depths.Max();
        return max >= 1 ? [.. Enumerable.Range(0, count).Select(node => links.Exists(link => link.Source == node) ? depths[node] : max)] : depths;
    }
}
