using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;

namespace Aioros.Pictor.Tests.Oracles;

public sealed partial class MarkupOracleTests
{
    [Fact]
    [Trait("AC", "ORACLE-01")]
    public async Task TheMarkupComparisonFailsOnEveryAttributeItComparesAsync()
    {
        var line = Reference.Pictor(await Reference.ExampleAsync("SimpleLineChart"));
        var bars = Reference.Pictor(await Reference.ExampleAsync("SimpleBarChart"));
        var ticks = await Reference.RechartsAsync("CustomizedLabelTickLineChart");
        var radar = await Reference.RechartsAsync("CustomShapeRadarChart");
        var labels = await Reference.RechartsAsync("RadialBarLabelLists");

        Assert.Empty(await DifferencesAsync("SimpleLineChart", line));
        Assert.Empty(await DifferencesAsync("SimpleBarChart", bars));
        Assert.Empty(MarkupComparison.Compare(ticks, ticks).Differences);
        Assert.Empty(await DifferencesAsync("SimpleLineChart", line.Replace(" stroke=\"#8884d8\" stroke-width=\"1\" fill=\"none\"", " stroke=\"#8884d8\" fill=\"transparent\"", StringComparison.Ordinal)));
        Assert.Contains(await DifferencesAsync("SimpleLineChart", line.Replace(" stroke-dasharray=\"3 3\"", string.Empty, StringComparison.Ordinal)), Naming("stroke-dasharray: 3 3 in Recharts, none in Pictor"));
        Assert.Contains(await DifferencesAsync("SimpleLineChart", line.Replace("stroke-dasharray=\"3 3\"", "stroke-dasharray=\"5 5\"", StringComparison.Ordinal)), Naming("stroke-dasharray: 3 3 in Recharts, 5 5 in Pictor"));
        Assert.Contains(await DifferencesAsync("SimpleLineChart", line.Replace(" fill=\"none\" class=\"recharts-curve recharts-line-curve\"", " class=\"recharts-curve recharts-line-curve\"", StringComparison.Ordinal)), Naming("fill: none in Recharts, black in Pictor"));
        Assert.Contains(await DifferencesAsync("SimpleLineChart", line.Replace("<path stroke=\"#8884d8\" ", "<path ", StringComparison.Ordinal)), Naming("stroke: #8884d8 in Recharts, none in Pictor"));
        Assert.Contains(await DifferencesAsync("SimpleLineChart", line.Replace(" text-anchor=\"middle\"", string.Empty, StringComparison.Ordinal)), Naming("text-anchor: middle in Recharts, start in Pictor"));
        Assert.Contains(await DifferencesAsync("SimpleBarChart", bars.Replace("<path fill=\"#8884d8\" class=\"recharts-rectangle\"", "<path class=\"recharts-rectangle\"", StringComparison.Ordinal)), Naming("fill: #8884d8 in Recharts, black in Pictor"));
        Assert.Contains(MarkupComparison.Compare(ticks, ticks.Replace("<g transform=\"translate(80,221)\"", "<g transform=\"translate(80,231)\"", StringComparison.Ordinal)).Differences, Naming("transform: [80, 221] in Recharts, [80, 231] in Pictor"));
        Assert.Contains(MarkupComparison.Compare(ticks, ticks.Replace("<g transform=\"translate(80,221)\"", "<g", StringComparison.Ordinal)).Differences, Naming("transform: [80, 221] in Recharts, [] in Pictor"));
        Assert.Contains(MarkupComparison.Compare(ticks, Surface().Replace(ticks, "width=\"510\"", 1)).Differences, Naming("width: [500] in Recharts, [510] in Pictor"));
        Assert.Contains(MarkupComparison.Compare(radar, radar.Replace("points=\"250,44 ", "points=\"250,45 ", StringComparison.Ordinal)).Differences, Naming("points: [250, 44"));
        Assert.Contains(MarkupComparison.Compare(labels, labels.Replace("M290.8439826217616,153.57338545265398", "M290.8439826217616,154.57338545265398", StringComparison.Ordinal)).Differences, Naming("<textpath class=\"\"> path: "));
    }

    [Fact]
    [Trait("AC", "ORACLE-02")]
    public async Task TheHoverComparisonFailsOnTheBoxStyleTheLinesAndTheCursorAsync()
    {
        var formats = await Reference.HoverAsync("TooltipFormats", 0);
        var line = await Reference.HoverAsync("DetailsFollowLineChart", 0);

        Assert.Empty(Reference.Differences(formats));
        Assert.Empty(Reference.Differences(line));
        Assert.Contains(Reference.Differences(formats with { Pictor = formats.Pictor.Replace("border-radius:6px;padding:6px", "border-radius:6px;padding:8px", StringComparison.Ordinal) }), Naming("box padding: 6px in Recharts, 8px in Pictor"));
        Assert.Contains(Reference.Differences(formats with { Pictor = formats.Pictor.Replace("style=\"background-color:#f8f8f8;", "style=\"", StringComparison.Ordinal) }), Naming("box background-color: #f8f8f8 in Recharts, #ffffff in Pictor"));
        Assert.Contains(Reference.Differences(line with { Pictor = line.Pictor.Replace("border:1px solid var(--pictor-tip-border,#ccc)", "border:1px solid var(--pictor-tip-border,#ddd)", StringComparison.Ordinal) }), Naming("box border: 1px solid #cccccc in Recharts, 1px solid #dddddd in Pictor"));
        Assert.Contains(Reference.Differences(formats with { Pictor = formats.Pictor.Replace("color:#82ca9d;color:#333", "color:#82ca9d;color:#334", StringComparison.Ordinal) }), Naming("items ["));
        Assert.Contains(Reference.Differences(line with { Pictor = line.Pictor.Replace("class=\"recharts-curve recharts-tooltip-cursor\" d=\"M", "class=\"recharts-curve recharts-tooltip-cursor\" d=\"M1", StringComparison.Ordinal) }), Naming("<path class=\"cursor\"> d: "));
    }

    private static async Task<IReadOnlyList<string>> DifferencesAsync(string name, string pictor) =>
        MarkupComparison.Compare(await Reference.RechartsAsync(name), pictor).Differences;

    private static Predicate<string> Naming(string text) => difference => difference.Contains(text, StringComparison.Ordinal);

    [GeneratedRegex("(?<=<svg class=\"recharts-surface\" )width=\"500\"")]
    private static partial Regex Surface();
}
