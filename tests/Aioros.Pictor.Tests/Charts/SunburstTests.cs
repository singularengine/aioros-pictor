using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Data;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Charts;

public sealed class SunburstTests
{
    private sealed record Part(string Name, double Size, string? Fill, IReadOnlyList<Part>? Children);

    [Fact]
    [Trait("AC", "SUNBURST-01")]
    public async Task ASunburstOfFiveLevelsMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("SimpleSunburst");

        Assert.Empty(verdict.Differences);
        Assert.Equal(verdict.Recharts, verdict.Compared);
    }

    [Fact]
    [Trait("AC", "SUNBURST-02")]
    public async Task APartialSunburstWithItsOwnPropsMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("CustomSunburst");

        Assert.Empty(verdict.Differences);
        Assert.Equal(verdict.Recharts, verdict.Compared);
    }

    [Fact]
    [Trait("AC", "SUNBURST-03")]
    public async Task SunburstRecordsAreReadThroughTheMemberLookupAsync()
    {
        var example = await Reference.ExampleAsync("CustomSunburst");
        var root = Record((IReadOnlyDictionary<string, object?>)example.Data!);
        var spec = example.Spec with { Sunburst = example.Spec.Sunburst! with { DataKey = "Size" } };

        var markup = Write(spec, root, static (owner, member) => owner == typeof(Part) ? member switch
        {
            "Name" => static item => ((Part)item).Name,
            "Size" => static item => ((Part)item).Size,
            "Fill" => static item => ((Part)item).Fill,
            "Children" => static item => ((Part)item).Children,
            _ => null,
        } : null);

        Assert.Equal(Reference.Pictor(example), markup);
    }

    [Fact]
    [Trait("AC", "SUNBURST-04")]
    public void ZeroMissingAndTextValuesAreDrawnWithoutFailing()
    {
        var spec = new ChartSpec { Kind = ChartKind.Sunburst, Width = 400, Height = 400, Sunburst = new SunburstSpec() };
        var root = Item("name", "root", "value", 100.0, "children", new object?[] { Item("name", "a", "value", 0.0), Item("name", "b"), Item("name", "c", "value", "x"), Item("name", "d", "value", 20.0) });

        var plot = SunburstPlot.Compute(spec, ChartData.From(root, static (_, _) => null));
        var markup = Write(spec, root, static (_, _) => null);
        var valueless = Write(spec, Item("name", "root", "children", new object?[] { Item("name", "d", "value", 20.0) }), static (_, _) => null);
        var childless = Write(spec, Item("name", "root", "value", 100.0), static (_, _) => null);

        Assert.Equal(1, Count(markup, "class=\"recharts-sector\""));
        Assert.Equal(2, Count(markup, "class=\"recharts-text\""));
        Assert.Contains(">0</tspan>", markup, StringComparison.Ordinal);
        Assert.Equal((0.0, 72.0), (plot.Nodes[3].Start, plot.Nodes[3].Arc));
        Assert.Equal([null, null], plot.Nodes[1..3].Select(node => node.Text));
        Assert.Equal(0, Count(valueless, "class=\"recharts-sector\""));
        Assert.Contains("<g class=\"recharts-layer recharts-sunburst\"></g>", childless, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "SUNBURST-07")]
    public async Task TooltipsReadTheValueAndAreAnchoredByNameAsync()
    {
        var custom = await Reference.ExampleAsync("CustomSunburst");
        var simple = await Reference.ExampleAsync("SimpleSunburst");

        var customMarkup = Reference.Pictor(custom);
        var plot = (SunburstPlot)Painting.Plots.Compute(simple.Spec, ChartData.From(simple.Data, static (_, _) => null));
        var named = plot.Nodes.Where(node => node.Name == "third level child").ToList();
        var anchors = Regex.Matches(Reference.Pictor(simple), "<foreignObject x=\"([^\"]+)\" y=\"([^\"]+)\"[^>]*><div class=\"recharts-default-tooltip\"><p></p><ul><li style=\"color:#000\">third level child : 5</li>")
            .Select(match => (match.Groups[1].Value, match.Groups[2].Value)).ToList();

        Assert.Equal(7, Count(customMarkup, "pictor-hit\""));
        Assert.Equal(0, Count(customMarkup, "class=\"recharts-default-tooltip\""));
        Assert.Equal(3, named.Count);
        Assert.Equal(3, anchors.Count);
        Assert.Single(anchors.Distinct());
        Assert.All(named, node => Assert.Equal((named[^1].TipX, named[^1].TipY), (node.TipX, node.TipY)));
        Assert.NotEqual(named[0].Start, named[^1].Start);
    }

    [Fact]
    [Trait("AC", "SUNBURST-09")]
    public async Task ASunburstDoesNotAnimateAsync()
    {
        var example = await Reference.ExampleAsync("SimpleSunburst");

        var markup = Reference.Pictor(example);

        Assert.DoesNotContain("pictor-a", markup, StringComparison.Ordinal);
        Assert.Equal(0, Motion.Animations.End(example.Spec));
    }

    [Fact]
    [Trait("AC", "SUNBURST-10")]
    public async Task ANestedValueChangeIsWrittenAgainAsync()
    {
        var sunburst = await Reference.ExampleAsync("SimpleSunburst");
        var treemap = await Reference.ExampleAsync("SimpleTreemap");
        var sankey = await Reference.ExampleAsync("SimpleSankey");

        var sunburstData = (IReadOnlyDictionary<string, object?>)sunburst.Data!;
        var changedSunburst = Replace(sunburstData, "children", 0, child => Replace(child, "children", 0, leaf => With(leaf, "value", 12.0)));
        var treemapData = (IReadOnlyList<object?>)treemap.Data!;
        List<object?> changedTreemap = [Replace((IReadOnlyDictionary<string, object?>)treemapData[0]!, "children", 1, leaf => With(leaf, "size", 1.0)), .. treemapData.Skip(1)];
        var sankeyData = (IReadOnlyDictionary<string, object?>)sankey.Data!;
        var changedSankey = Replace(sankeyData, "links", 1, link => With(link, "value", 10000.0));
        var renamedSunburst = Replace(sunburstData, "children", 1, child => Replace(child, "children", 0, leaf => With(leaf, "name", "renamed")));
        var first = (IReadOnlyDictionary<string, object?>)treemapData[0]!;
        List<object?> grownTreemap = [With(first, "children", ((IEnumerable<object?>)first["children"]!).Append(new Dictionary<string, object?> { ["name"] = "Added", ["size"] = 5000.0 }).ToList()), .. treemapData.Skip(1)];

        foreach (var (example, original, changed) in new (Demo.RechartsExample, object, object)[]
        {
            (sunburst, sunburstData, changedSunburst), (sunburst, sunburstData, renamedSunburst), (treemap, treemapData, changedTreemap), (treemap, treemapData, grownTreemap), (sankey, sankeyData, changedSankey),
        })
        {
            var output = new ChartOutput(example.Spec, static (_, _) => null);
            var written = Write(output, original);
            Write(output, original);
            var kept = Write(output, original);
            var renders = output.Renders;
            var after = Write(output, changed);

            Assert.Equal(written, kept);
            Assert.Equal(2, renders);
            Assert.Equal(3, output.Renders);
            Assert.NotEqual(written, after);
            Assert.Equal(Write(example.Spec, changed, static (_, _) => null), after);
        }
    }

    private static Part Record(IReadOnlyDictionary<string, object?> item) => new(
        (string)item["name"]!,
        (double)item["size"]!,
        item.TryGetValue("fill", out var fill) ? (string?)fill : null,
        item.TryGetValue("children", out var children) ? [.. ((IEnumerable<object?>)children!).Cast<IReadOnlyDictionary<string, object?>>().Select(Record)] : null);

    private static Dictionary<string, object?> Item(params object?[] pairs)
    {
        var item = new Dictionary<string, object?>(StringComparer.Ordinal);
        for (var i = 0; i < pairs.Length; i += 2)
        {
            item[(string)pairs[i]!] = pairs[i + 1];
        }

        return item;
    }

    private static Dictionary<string, object?> With(IReadOnlyDictionary<string, object?> item, string key, object? value) =>
        new(item, StringComparer.Ordinal) { [key] = value };

    private static Dictionary<string, object?> Replace(IReadOnlyDictionary<string, object?> item, string key, int index, Func<IReadOnlyDictionary<string, object?>, IReadOnlyDictionary<string, object?>> change)
    {
        var list = ((IEnumerable<object?>)item[key]!).ToList();
        list[index] = change((IReadOnlyDictionary<string, object?>)list[index]!);
        return With(item, key, list);
    }

    private static int Count(string markup, string text) => Regex.Count(markup, Regex.Escape(text));

    private static string Write(ChartSpec spec, object? data, DataMembers members)
    {
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, data, members);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string Write(ChartOutput output, object data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        output.Write(data, buffer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
