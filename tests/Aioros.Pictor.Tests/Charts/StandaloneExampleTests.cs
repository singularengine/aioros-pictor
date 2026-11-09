namespace Aioros.Pictor.Tests.Charts;

public sealed class StandaloneExampleTests
{
    [Fact]
    [Trait("AC", "FUNNEL-01")]
    public async Task AFunnelOfTrapezoidsWithLabelsMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("FunnelChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "FUNNEL-02")]
    public async Task AReversedFunnelWithALegendMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("ReversedFunnelChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "TREEMAP-01")]
    public async Task ASquarifiedTreemapMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("SimpleTreemap");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "TREEMAP-02")]
    public async Task ATreemapColoredFromItsPanelMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("ColorPanelTreemap");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "TREEMAP-03")]
    public async Task ANestTreemapShowsItsFirstLevelAndItsIndexAsync()
    {
        var verdict = await Reference.CompareAsync("NestedTreemap");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "SANKEY-01")]
    public async Task ASankeyMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("SimpleSankey");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "SANKEY-02")]
    public async Task AnUnsortedSankeyWithWideNodesMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("BudgetSankey");

        Assert.Empty(verdict.Differences);
    }
}
