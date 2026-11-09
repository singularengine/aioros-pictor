using System.Buffers;
using System.Text;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Svg;
using Avalonia;

namespace Aioros.Pictor.Tests.Charts;

public sealed class PolarOptionTests(Desktop.DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "POLAR-20")]
    public async Task RadialBarChartsMatchRecharts3Async()
    {
        var names = Directory.GetFiles(Path.Combine(Reference.Root, "tools", "recharts", "out", "recharts3"), "*.client.html")
            .Select(static file => Path.GetFileName(file)[..^".client.html".Length])
            .Where(static name => !name.Contains('.', StringComparison.Ordinal) && !name.StartsWith("Custom", StringComparison.Ordinal) && name != "RadialBarLabelListContent")
            .Order(StringComparer.Ordinal)
            .ToList();
        var differences = new List<string>();
        foreach (var name in names)
        {
            differences.AddRange((await Reference.CompareAsync(name)).Differences.Select(difference => $"{name}: {difference}"));
        }

        Assert.Equal(19, names.Count);
        Assert.True(differences.Count == 0, string.Join("\n", differences));
    }

    [Fact]
    [Trait("AC", "POLAR-13")]
    public async Task RadialBarSizesCornersAndBackgroundsMatchRechartsAsync() => Assert.Empty((await Reference.CompareAsync("RadialBarOptions")).Differences);

    [Fact]
    [Trait("AC", "POLAR-14")]
    public async Task StackedRadialBarsMatchRechartsAsync() => Assert.Empty((await Reference.CompareAsync("StackedRadialBarChart")).Differences);

    [Fact]
    [Trait("AC", "POLAR-15")]
    public async Task ARadialBarChartWithoutASharedTooltipShowsOneTooltipPerBarAsync()
    {
        Assert.Empty((await Reference.CompareAsync("RadialBarItemTooltip")).Differences);
        Assert.Empty(Reference.Differences(await Reference.HoverAsync("RadialBarItemTooltip", 0), 0.5));
    }

    [Fact]
    [Trait("AC", "POLAR-16")]
    public async Task SeveralActiveSectorsInactiveShapesAndBlendedStrokesMatchRechartsAsync() => Assert.Empty((await Reference.CompareAsync("PieActiveShapes")).Differences);

    [Fact]
    [Trait("AC", "POLAR-17")]
    public async Task PieLabelAndLabelLinePropsMatchRechartsAsync() => Assert.Empty((await Reference.CompareAsync("PieLabelProps")).Differences);

    [Fact]
    [Trait("AC", "POLAR-18")]
    public async Task AClockwisePieWithPaddingAndMinimumAnglesMatchesRechartsAsync() => Assert.Empty((await Reference.CompareAsync("PieAngles")).Differences);

    [Fact]
    [Trait("AC", "POLAR-19")]
    public async Task PolarOptionsOfXamlElementsBuildTheSpecRechartsPropsGiveAsync()
    {
        foreach (var name in new[] { "RadialBarOptions", "StackedRadialBarChart", "PieActiveShapes", "PieLabelProps" })
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
