using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Web;

public sealed partial class ResponsiveTests
{
    [Fact]
    [Trait("AC", "WEB-05")]
    public async Task AResponsiveChartIsLaidOutAtEachWidthAndShownByContainerQueriesAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");
        var spec = example.Spec with { Responsive = true, ResponsiveWidths = [720, 360, 1080] };

        var markup = Write(spec, example.Data);
        var once = Write(spec with { ResponsiveWidths = [] }, example.Data);
        var fixedWidth = Write(spec with { Responsive = false }, example.Data);

        Assert.StartsWith("<div class=\"pictor-responsive\">", markup, StringComparison.Ordinal);
        Assert.Equal(["0 0 360 300", "0 0 720 300", "0 0 1080 300"], ViewBox().Matches(markup).Select(match => match.Groups[1].Value));
        Assert.Contains("@container (width >= 540px){.pictor-responsive>.recharts-wrapper.pictor-w0-540{position:absolute!important;top:0;left:0;width:0!important;height:0!important;overflow:hidden;visibility:hidden}.pictor-responsive>.recharts-wrapper.pictor-w0-540 *{visibility:hidden!important}}", markup, StringComparison.Ordinal);
        Assert.Contains("@container (width < 540px) or (width >= 900px){.pictor-responsive>.recharts-wrapper.pictor-w540-900{position:absolute!important;top:0;left:0;width:0!important;height:0!important;overflow:hidden;visibility:hidden}.pictor-responsive>.recharts-wrapper.pictor-w540-900 *{visibility:hidden!important}}", markup, StringComparison.Ordinal);
        Assert.Contains("@container (width < 900px){.pictor-responsive>.recharts-wrapper.pictor-w900-up{position:absolute!important;top:0;left:0;width:0!important;height:0!important;overflow:hidden;visibility:hidden}.pictor-responsive>.recharts-wrapper.pictor-w900-up *{visibility:hidden!important}}", markup, StringComparison.Ordinal);
        Assert.Contains("<div class=\"recharts-wrapper pictor-w540-900\"", markup, StringComparison.Ordinal);
        Assert.True(Ticks(markup, 0) < Ticks(markup, 2), "the narrow layout shows fewer x ticks than the wide one");
        Assert.Single(ViewBox().Matches(once));
        Assert.Single(ViewBox().Matches(fixedWidth));
        Assert.DoesNotContain("pictor-responsive", once, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "WEB-09")]
    public async Task ALaterResponsiveChartDoesNotHideAnEarlierOnesLayoutAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");
        var first = Write(example.Spec with { Responsive = true, ResponsiveWidths = [380, 760] }, example.Data);
        var second = Write(example.Spec with { Responsive = true, ResponsiveWidths = [380, 620] }, example.Data);

        var hides = HideRule().Matches(first + second).Select(match => match.Groups[1].Value).ToList();

        Assert.Equal([".pictor-responsive>.recharts-wrapper.pictor-w0-570", ".pictor-responsive>.recharts-wrapper.pictor-w570-up", ".pictor-responsive>.recharts-wrapper.pictor-w0-500", ".pictor-responsive>.recharts-wrapper.pictor-w500-up"], hides);
        Assert.DoesNotContain(".pictor-responsive>.recharts-wrapper{", first + second, StringComparison.Ordinal);
        Assert.Contains("<div class=\"recharts-wrapper pictor-w570-up\"", first, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "WEB-11")]
    public async Task AResponsiveSurfaceIsABlockAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var aspect = Write(example.Spec with { Responsive = true, Aspect = 16d / 9, ResponsiveWidths = [542] }, example.Data);
        var filled = Write(example.Spec with { Responsive = true }, example.Data);

        Assert.Contains("aspect-ratio:1.778", aspect, StringComparison.Ordinal);
        Assert.Contains("viewBox=\"0 0 542 304.875\" style=\"width:100%;height:100%;display:block\">", aspect, StringComparison.Ordinal);
        Assert.Contains("style=\"width:100%;height:100%;display:block\">", filled, StringComparison.Ordinal);
    }

    private static int Ticks(string markup, int variant) =>
        markup.Split("<div class=\"recharts-wrapper")[variant + 1].Split("recharts-xAxis")[1].Split("recharts-yAxis")[0].Split("recharts-cartesian-axis-tick-value").Length - 1;

    [GeneratedRegex(@"\{([^{}]+)\{position:absolute!important;")]
    private static partial Regex HideRule();

    private static string Write(Specs.ChartSpec spec, object? data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, data, static (_, _) => null);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    [GeneratedRegex("viewBox=\"(0 0 \\d+ 300)\"")]
    private static partial Regex ViewBox();
}
