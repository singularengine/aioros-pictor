using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Data;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Charts;

public sealed partial class StandaloneDataTests
{
    private sealed record Leaf(string Name, double Size, IReadOnlyList<Leaf>? Children);

    private sealed record Node(string Name);

    private sealed record Link(int Source, int Target, double Value);

    private sealed record Flow(IReadOnlyList<Node> Nodes, IReadOnlyList<Link> Links);

    [Fact]
    [Trait("AC", "FUNNEL-03")]
    public async Task ZeroMissingAndEmptyFunnelItemsAreDrawnAsRechartsDrawsThemAsync()
    {
        var example = await Reference.ExampleAsync("FunnelChart");

        var lastZero = Funnel(example, "100", "50", "0");
        var missing = Funnel(example, "100", null, "50");
        var zero = Funnel(example, "100", "0", "50");
        var allZero = Funnel(example, "0", "0");
        var empty = Funnel(example);

        Assert.Equal((3, 2), (Count(lastZero, "recharts-funnel-trapezoid\""), Count(lastZero, "class=\"recharts-trapezoid\"")));
        Assert.Equal(zero, missing);
        Assert.Equal((2, 0), (Count(allZero, "recharts-funnel-trapezoid\""), Count(allZero, "class=\"recharts-trapezoid\"")));
        Assert.DoesNotContain("recharts-trapezoids", empty, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "TREEMAP-04")]
    public async Task TreemapRecordsAreReadThroughTheMemberLookupAsync()
    {
        var example = await Reference.ExampleAsync("SimpleTreemap");
        var records = Leaves((IEnumerable<object?>)example.Data!);
        var spec = example.Spec with { Treemap = example.Spec.Treemap! with { DataKey = "Size" } };

        var markup = Write(spec, records, static (owner, member) => owner == typeof(Leaf) ? member switch
        {
            "Name" => static item => ((Leaf)item).Name,
            "Size" => static item => ((Leaf)item).Size,
            "Children" => static item => ((Leaf)item).Children,
            _ => null,
        } : null);

        Assert.Equal(Reference.Pictor(example), markup);
    }

    [Fact]
    [Trait("AC", "TREEMAP-07")]
    public void LeavesWithoutAPositiveSizeGetNoArea()
    {
        object?[] leaves =
        [
            Item("name", "a", "size", 0.0), Item("name", "b", "size", -5.0), Item("name", "c", "size", "x"), Item("name", "d"),
            Item("name", "e", "size", 30.0), Item("name", "f", "size", 10.0),
        ];
        var spec = new ChartSpec { Kind = ChartKind.Treemap, Width = 500, Height = 375, Treemap = new TreemapSpec { DataKey = "size" } };

        var plot = TreemapPlot.Compute(spec, ChartData.From(leaves, static (_, _) => null));
        var markup = Write(spec, leaves, static (_, _) => null);
        var empty = Write(spec, Array.Empty<object>(), static (_, _) => null);

        var children = plot.Root.Children!;
        Assert.Equal(3, Count(markup, "class=\"recharts-rectangle\""));
        Assert.All(children.Take(4), child => Assert.Equal(0, child.Width * child.Height));
        Assert.Equal(500 * 375, (children[4].Width * children[4].Height) + (children[5].Width * children[5].Height));
        Assert.Equal(1, Count(empty, "class=\"recharts-rectangle\""));
        Assert.Contains("<text x=\"8\" y=\"194.5\" font-size=\"14\"></text>", empty, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "TREEMAP-08")]
    public async Task AResponsiveNestTreemapWritesItsNestIndexAsync()
    {
        var example = await Reference.ExampleAsync("NestedTreemap");

        var fixedSize = Write(example.Spec, example.Data, static (_, _) => null);
        var responsive = Write(example.Spec with { Responsive = true, Aspect = example.Spec.Width / example.Spec.Height }, example.Data, static (_, _) => null);

        Assert.Contains("</svg><div class=\"recharts-treemap-nest-index-wrapper\"", fixedSize, StringComparison.Ordinal);
        Assert.Contains("</svg><div class=\"recharts-treemap-nest-index-wrapper\"", responsive, StringComparison.Ordinal);
        Assert.EndsWith("</div></div></div>", responsive, StringComparison.Ordinal);
        Assert.Equal(1, Count(responsive, ">root</div>"));
    }

    [Fact]
    [Trait("AC", "SANKEY-03")]
    public async Task SankeyRecordsAreReadThroughTheMemberLookupAsync()
    {
        var example = await Reference.ExampleAsync("SimpleSankey");
        var data = (IReadOnlyDictionary<string, object?>)example.Data!;
        var flow = new Flow(
            [.. ((IEnumerable<object?>)data["nodes"]!).Cast<IReadOnlyDictionary<string, object?>>().Select(node => new Node((string)node["name"]!))],
            [.. ((IEnumerable<object?>)data["links"]!).Cast<IReadOnlyDictionary<string, object?>>().Select(link => new Link((int)(double)link["source"]!, (int)(double)link["target"]!, (double)link["value"]!))]);

        var markup = Write(example.Spec, flow, static (owner, member) => (owner.Name, member) switch
        {
            (nameof(Flow), "Nodes") => static item => ((Flow)item).Nodes,
            (nameof(Flow), "Links") => static item => ((Flow)item).Links,
            (nameof(Node), "Name") => static item => ((Node)item).Name,
            (nameof(Link), "Source") => static item => ((Link)item).Source,
            (nameof(Link), "Target") => static item => ((Link)item).Target,
            (nameof(Link), "Value") => static item => ((Link)item).Value,
            _ => null,
        });

        Assert.Equal(Reference.Pictor(example), markup);
    }

    [Fact]
    [Trait("AC", "SANKEY-04")]
    public async Task LinksToMissingNodesAreLeftOutAsync()
    {
        var example = await Reference.ExampleAsync("SimpleSankey");
        object?[] nodes = [Item("name", "a"), Item("name", "b"), Item("name", "c")];
        object?[] valid = [Item("source", 0.0, "target", 1.0, "value", 5.0), Item("source", 1.0, "target", 2.0, "value", 3.0)];
        object?[] broken = [.. valid, Item("source", 1.0, "target", 7.0, "value", 2.0), Item("source", -1.0, "target", 0.0, "value", 2.0)];

        var expected = Write(example.Spec, Item("nodes", nodes, "links", valid), static (_, _) => null);
        var actual = Write(example.Spec, Item("nodes", nodes, "links", broken), static (_, _) => null);
        var empty = Write(example.Spec, Item("nodes", Array.Empty<object>(), "links", broken), static (_, _) => null);

        Assert.Equal(expected, actual);
        Assert.Equal(2, Count(actual, "class=\"recharts-sankey-link\""));
        Assert.Contains("<g class=\"recharts-layer recharts-sankey-links\"></g><g class=\"recharts-layer recharts-sankey-nodes\"></g>", empty, StringComparison.Ordinal);
    }

    private static string Funnel(RechartsExample example, params string?[] values)
    {
        var table = new DataTable
        {
            Count = values.Length,
            Columns = ImmutableDictionary<string, ImmutableArray<string?>>.Empty
                .Add("value", [.. values])
                .Add("name", [.. values.Select((_, i) => "Item " + i)]),
        };
        return Write(example.Spec with { Funnels = [example.Spec.Funnels[0] with { Data = table }] }, null, static (_, _) => null);
    }

    private static List<Leaf> Leaves(IEnumerable<object?> items) =>
        [.. items.Cast<IReadOnlyDictionary<string, object?>>().Select(item => new Leaf(
            (string)item["name"]!,
            item.TryGetValue("size", out var size) ? (double)size! : 0,
            item.TryGetValue("children", out var children) ? Leaves((IEnumerable<object?>)children!) : null))];

    private static Dictionary<string, object?> Item(params object?[] pairs)
    {
        var item = new Dictionary<string, object?>(StringComparer.Ordinal);
        for (var i = 0; i < pairs.Length; i += 2)
        {
            item[(string)pairs[i]!] = pairs[i + 1];
        }

        return item;
    }

    private static string Write(ChartSpec spec, object? data, DataMembers members)
    {
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, data, members);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static int Count(string markup, string text) => Regex.Count(markup, Regex.Escape(text));
}
