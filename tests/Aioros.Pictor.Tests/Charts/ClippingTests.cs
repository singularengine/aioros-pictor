using Aioros.Pictor.Data;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Tests.Desktop;

namespace Aioros.Pictor.Tests.Charts;

public sealed class ClippingTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "CLIP-01")]
    public async Task SeriesOnAnAxisThatAllowsDataOverflowAreClippedAsRechartsClipsThemAsync()
    {
        var verdict = await Reference.CompareAsync("DataOverflowComposedChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CLIP-02")]
    public async Task AScatterCutOnItsXAxisIsClippedToThePlotWidthAsync()
    {
        var verdict = await Reference.CompareAsync("DataOverflowScatterChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CLIP-03")]
    public async Task OnlyAClipThatIsUsedIsWrittenAndItsIdNamesItsRectangleAsync()
    {
        var simple = Reference.Pictor(await Reference.ExampleAsync("SimpleLineChart"));
        var overflow = Reference.Pictor(await Reference.ExampleAsync("DataOverflowComposedChart"));
        var scatter = Reference.Pictor(await Reference.ExampleAsync("DataOverflowScatterChart"));

        Assert.DoesNotContain("clipPath", simple, StringComparison.Ordinal);
        Assert.Contains("<clipPath id=\"pictor-clip-", overflow, StringComparison.Ordinal);
        Assert.Empty(Ids(overflow).Intersect(Ids(scatter)));
    }

    [Fact]
    [Trait("AC", "CLIP-04")]
    public async Task TheDesktopClipsASeriesToThePlotAsync()
    {
        var example = await Reference.ExampleAsync("DataOverflowComposedChart");
        var layout = ChartLayout.Compute(example.Spec, ChartData.From(example.Data, static (_, _) => null));
        var pixels = await ReferenceDesktop.PixelsAsync(desktop, "DataOverflowComposedChart");
        var result = await ReferenceDesktop.MeasureAsync(desktop, "DataOverflowComposedChart");
        var inked = Enumerable.Range((int)layout.Left + 1, (int)layout.PlotWidth - 2)
            .SelectMany(x => Enumerable.Range(0, (int)layout.Top - 1).Select(y => (x, y)))
            .Count(point => !ReferenceDesktop.Blank(pixels, point.x, point.y));

        Assert.Equal(0, inked);
        PixelVerdict.Within(result);
    }

    private static HashSet<string> Ids(string markup) =>
        [.. System.Text.RegularExpressions.Regex.Matches(markup, "clipPath id=\"([^\"]+)\"").Select(match => match.Groups[1].Value)];
}

public sealed class ReferenceTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "REFERENCE-01")]
    public async Task ReferencesBeyondTheDomainAreVisibleHiddenOrDiscardedAsync()
    {
        var verdict = await Reference.CompareAsync("ReferenceOverflowLineChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "REFERENCE-02")]
    public async Task SegmentsPositionsAndEndPointOrderMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("ReferenceSegmentBarChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "REFERENCE-03")]
    public async Task TheDesktopClipsHiddenReferencesToThePlotAsync()
    {
        var example = await Reference.ExampleAsync("ReferenceOverflowLineChart");
        var layout = ChartLayout.Compute(example.Spec, ChartData.From(example.Data, static (_, _) => null));
        var above = (int)Math.Round(layout.YAxes[0].Map(10500));
        var pixels = await ReferenceDesktop.PixelsAsync(desktop, "ReferenceOverflowLineChart");
        var result = await ReferenceDesktop.MeasureAsync(desktop, "ReferenceOverflowLineChart");

        Assert.True(ReferenceDesktop.Blank(pixels, (int)layout.Left + 20, above), "the hidden line or area is drawn above the plot");
        Assert.False(ReferenceDesktop.Blank(pixels, (int)layout.XAxes[0].Ticks[5].Coordinate, above), "the visible area is not drawn above the plot");
        PixelVerdict.Within(result);
    }

    [Fact]
    [Trait("AC", "REFERENCE-04")]
    public async Task ReferenceLinePropsAreSetInXamlAsync()
    {
        var verdict = await ReferenceDesktop.CompareXamlAsync(desktop, "ReferenceSegmentBarChart");

        Assert.Empty(verdict.Differences);
    }
}
