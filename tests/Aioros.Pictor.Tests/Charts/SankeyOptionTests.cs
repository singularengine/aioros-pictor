using Aioros.Pictor.Tests.Desktop;

namespace Aioros.Pictor.Tests.Charts;

public sealed class SankeyOptionTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "SANKEY-07")]
    public async Task NodeAndLinkPropsAndLayoutOptionsMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("SankeyOptionsChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "SANKEY-08")]
    public async Task ALinkStrokeWidthOverridesEveryLinksWidthAsync()
    {
        var verdict = await Reference.CompareAsync("SankeyLinkWidthChart");
        var markup = Reference.Pictor(await Reference.ExampleAsync("SankeyLinkWidthChart"));

        Assert.Empty(verdict.Differences);
        Assert.Equal(4, System.Text.RegularExpressions.Regex.Count(markup, "stroke-width=\"4\"[^>]*recharts-sankey-link"));
    }

    [Fact]
    [Trait("AC", "SANKEY-09")]
    public async Task SankeyPropsAreSetInXamlAndDrawnOnTheDesktopAsync()
    {
        var verdict = await ReferenceDesktop.CompareXamlAsync(desktop, "SankeyOptionsChart");
        var result = await ReferenceDesktop.MeasureAsync(desktop, "SankeyLinkWidthChart");

        Assert.Empty(verdict.Differences);
        PixelVerdict.Within(result);
    }
}
