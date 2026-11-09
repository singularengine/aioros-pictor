using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Svg;
using Avalonia;

namespace Aioros.Pictor.Tests.Tips;

public sealed class LabelTests(Desktop.DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "LABELS-01")]
    public async Task EveryLabelListOfASeriesIsDrawnWithItsTextPropsAsync()
    {
        var bars = Reference.Pictor(await Reference.ExampleAsync("LabelListsBarChart"));

        Assert.Empty((await Reference.CompareAsync("LabelListsBarChart")).Differences);
        Assert.Empty((await Reference.CompareAsync("LabelListsLineChart")).Differences);
        Assert.Equal(4, Regex.Count(bars, "<g class=\"recharts-layer recharts-label-list\">"));
        Assert.Contains("font-size=\"10\" font-weight=\"bold\" transform=\"rotate(-90, 107.571, 235)\"", bars, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "LABELS-02")]
    public async Task OffsetCenterAndPolarPositionsPlaceLabelsAsRechartsAsync()
    {
        Assert.Empty((await Reference.CompareAsync("LabelListsBarChart")).Differences);
        Assert.Empty((await Reference.CompareAsync("PieLabelLists")).Differences);
    }

    [Fact]
    [Trait("AC", "LABELS-03")]
    public async Task APiesLabelListsTakeTheirSectorsPaintsAsync()
    {
        var pie = Reference.Pictor(await Reference.ExampleAsync("PieLabelLists"));

        Assert.Empty((await Reference.CompareAsync("PieLabelLists")).Differences);
        Assert.Contains("text-anchor=\"start\" fill=\"#0088FE\" stroke=\"#fff\"><tspan", pie, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "LABELS-04")]
    public async Task RadialBarLabelListsRunAlongTheirBarsAsync() => Assert.Empty((await Reference.CompareAsync("RadialBarLabelLists")).Differences);

    [Fact]
    [Trait("AC", "LABELS-05")]
    public async Task LabelsWiderThanTheirBarAreWrappedAsRechartsTextWrapsThemAsync()
    {
        var bars = Reference.Pictor(await Reference.ExampleAsync("LabelListsBarChart"));

        Assert.Empty((await Reference.CompareAsync("LabelListsBreakAll")).Differences);
        Assert.Contains("<tspan x=\"107.571\" dy=\"-1em\">Page</tspan><tspan x=\"107.571\" dy=\"1em\">A</tspan>", bars, StringComparison.Ordinal);
        Assert.Equal("Page \nA", Text.TextLines.Wrap("Page A", 44, 14, breakAll: true));
        Assert.Equal("4000", Text.TextLines.Wrap("4000", 10, 14, breakAll: false));
    }

    [Fact]
    [Trait("AC", "LABELS-06")]
    public async Task EveryLabelListOfAFunnelIsDrawnAsync() => Assert.Empty((await Reference.CompareAsync("FunnelLabelLists")).Differences);

    [Fact]
    [Trait("AC", "LABELS-07")]
    public async Task LabelListElementsBuildTheSpecRechartsPropsGiveAsync()
    {
        foreach (var name in new[] { "LabelListsBarChart", "PieLabelLists", "RadialBarLabelLists" })
        {
            var example = await Reference.ExampleAsync(name);
            var argument = await desktop.Session.Dispatch(
                () =>
                {
                    var chart = SpecControls.Create(example);
                    chart.Measure(new Size(example.Spec.Width, example.Spec.Height));
                    chart.Arrange(new Rect(0, 0, example.Spec.Width, example.Spec.Height));
                    return chart.WrittenSlot!.Argument;
                },
                TestContext.Current.CancellationToken);
            var buffer = new ArrayBufferWriter<byte>();
            Slots.Writer(argument, static (_, _) => null!)(example.Data, buffer);

            Assert.Equal(Reference.Pictor(example), Encoding.UTF8.GetString(buffer.WrittenSpan));
        }
    }
}
