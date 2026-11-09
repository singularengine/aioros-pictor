using Aioros.Pictor.Demo;

namespace Aioros.Pictor.Tests.Details;

public sealed class FidelityTests
{
    [Fact]
    [Trait("AC", "DETAIL-01")]
    public async Task ABandScaleAxisTurnsItsTooltipAtRechartsTicksAsync()
    {
        for (var interaction = 0; interaction < 3; interaction++)
        {
            None(Reference.Differences(await Reference.HoverAsync("LineBarAreaComposedChart", interaction), 0.5));
        }

        Assert.True((await Reference.CompareAsync("LineBarAreaComposedChart")).Matches);
    }

    [Fact]
    [Trait("AC", "DETAIL-02")]
    public async Task ATreemapWithoutNameKeyShowsNoNameInItsTooltipAsync()
    {
        var hover = await Reference.HoverAsync("ColorPanelTreemap", 0);

        None(Reference.Differences(hover, 0.5));
        Assert.Contains(">axis : ", Reference.Pictor(await Reference.ExampleAsync("NestedTreemap")), StringComparison.Ordinal);
        Assert.Contains(">Axis</text>", hover.Pictor, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "DETAIL-03")]
    public async Task ANestTreemapWithoutNameKeyReadsRootInItsIndexAsync()
    {
        var example = await Reference.ExampleAsync("DetailsNestTreemapWithoutNameKey");
        var zoomed = Reference.Pictor(example with { Spec = example.Spec with { Treemap = example.Spec.Treemap! with { Zoom = "0" } } });
        var named = await Reference.ExampleAsync("NestedTreemap");
        var namedZoom = Reference.Pictor(named with { Spec = named.Spec with { Treemap = named.Spec.Treemap! with { Zoom = "0" } } });
        var recharts = await Reference.RechartsAsync("DetailsNestTreemapWithoutNameKey.0");

        Assert.Equal(Boxes(recharts), Boxes(zoomed));
        Assert.Equal(["root", "root"], Boxes(zoomed));
        Assert.Equal(["root", "axis"], Boxes(namedZoom));
        var spec = example.Spec with { Treemap = example.Spec.Treemap with { Zoom = "0" } };
        var state = Painting.Plots.Hovered(Painting.Plots.Compute(spec, Pictor.Data.ChartData.From(example.Data, static (_, _) => null)), 200, 100);
        var tip = HoverComparison.PictorTip(zoomed, state);
        Assert.Empty(HoverComparison.Compare(HoverComparison.RechartsTip(await Reference.RechartsAsync("DetailsNestTreemapWithoutNameKey.1")), tip, 0.5));
        Assert.StartsWith(" : ", tip.Items[0].Text, StringComparison.Ordinal);
    }

    private static List<string> Boxes(string markup) =>
        [.. System.Text.RegularExpressions.Regex.Matches(markup, "recharts-treemap-nest-index-box\"[^>]*>([^<]*)<").Where(match => !match.Value.Contains("display:none", StringComparison.Ordinal)).Select(match => match.Groups[1].Value)];

    internal static void None(List<string> differences) => Assert.True(differences.Count == 0, string.Join("\n", differences));
}
