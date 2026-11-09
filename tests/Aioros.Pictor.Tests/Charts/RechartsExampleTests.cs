namespace Aioros.Pictor.Tests.Charts;

public sealed class RechartsExampleTests
{
    [Fact]
    [Trait("AC", "CHARTS-01")]
    public async Task ALineChartWithGridAxesTooltipAndLegendMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("SimpleLineChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-02")]
    public async Task DashedLinesAndTheirDotsMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("DashedLineChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-03")]
    public async Task AChartWithoutAxesMapsThroughHiddenDefaultAxesAsync()
    {
        var verdict = await Reference.CompareAsync("TinyLineChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-04")]
    public async Task TwoYAxesOnBothSidesMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("BiaxialLineChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-05")]
    public async Task ReferenceLinesAtACategoryAndAValueMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("LineChartWithReferenceLines");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-06")]
    public async Task LabelListsAndSeriesLabelsMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("CustomizedLabelLineChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-07")]
    public async Task AnAreaToItsBaselineMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("SimpleAreaChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-08")]
    public async Task StackedAreasMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("StackedAreaChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-09")]
    public async Task GroupedBarsShareTheirBandAsRechartsPlacesThemAsync()
    {
        var verdict = await Reference.CompareAsync("SimpleBarChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-10")]
    public async Task StackedBarsMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("StackedBarChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-11")]
    public async Task AStackBesideABarMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("MixBarChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-12")]
    public async Task NegativeBarsHangBelowZeroAsInRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("PositiveAndNegativeBarChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-13")]
    public async Task CellsColorBarsAndTheirLabelsAsync()
    {
        var verdict = await Reference.CompareAsync("BarChartWithCell");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-14")]
    public async Task BarBackgroundsFillTheirBandAsync()
    {
        var verdict = await Reference.CompareAsync("BarChartHasBackground");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-15")]
    public async Task AComposedChartOfAreaBarLineAndScatterMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("LineBarAreaComposedChart");

        Assert.Empty(verdict.Differences);
    }
}
