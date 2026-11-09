using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Svg;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Tips;

public sealed class LegendTests(Desktop.DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "LEGEND-04")]
    public async Task EveryLegendTypeMatchesRechartsOnBothTargetsAsync()
    {
        foreach (var name in new[] { "LegendPayload", "LegendTypes", "LegendIconType", "LegendStarSquare" })
        {
            Assert.Empty(await DifferencesAsync(name));
        }

        var example = await Reference.ExampleAsync("LegendPayload");
        var colors = await desktop.Session.Dispatch(
            () => Desktop.TooltipDesktopTests.Run(example, (_, chart) =>
            {
                var legend = ((ChartLayout)chart.Plot()).Legend!;
                return legend.Items.Where(item => item.Icon is not (LegendIcon.Line or LegendIcon.PlainLine or LegendIcon.Rect))
                    .Select(item => (item.Color, Pixel: Desktop.TooltipDesktopTests.Pixel(chart, (int)(item.X + 7), (int)(item.Y + (legend.RowHeight / 2)))))
                    .ToList();
            }),
            TestContext.Current.CancellationToken);

        Assert.Equal(7, colors.Count);
        Assert.All(colors, color => Assert.Equal(Rgb(color.Color), color.Pixel));
    }

    [Fact]
    [Trait("AC", "LEGEND-05")]
    public async Task AHiddenSeriesItemIsInactiveInTheInactiveColorAsync()
    {
        var example = await Reference.ExampleAsync("LegendInactive");
        var toggled = Write(example.Spec with { Legend = example.Spec.Legend! with { Toggles = true } }, example.Data);

        Assert.Empty(await DifferencesAsync("LegendInactive"));
        Assert.Contains("<li class=\"recharts-legend-item legend-item-0 inactive\"", Reference.Pictor(example), StringComparison.Ordinal);
        Assert.Contains("px;--pictor-inactive:#999\"><ul", toggled, StringComparison.Ordinal);
        Assert.Contains("fill:var(--pictor-inactive,#ccc)", toggled, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "LEGEND-06")]
    public async Task ALegendsOwnPayloadIsListedInItsOrderAsync()
    {
        var example = await Reference.ExampleAsync("LegendPayload");
        var markup = Reference.Pictor(example);
        var payload = await desktop.Session.Dispatch(
            () =>
            {
                var chart = new BarChart();
                chart.Elements.Add(SpecControls.LegendOf(example.Spec.Legend!));
                return chart.Spec(500, 300).Legend!.Payload;
            },
            TestContext.Current.CancellationToken);

        Assert.Empty(await DifferencesAsync("LegendPayload"));
        Assert.Equal(10, Regex.Count(markup, "<li class=\"recharts-legend-item legend-item-"));
        Assert.Contains("legend-item-9\"", markup, StringComparison.Ordinal);
        Assert.Equal(example.Spec.Legend!.Payload, payload);
    }

    [Fact]
    [Trait("AC", "LEGEND-07")]
    public async Task ItemTextsAreFormattedAsync()
    {
        Assert.Empty(await DifferencesAsync("LegendFormatter"));
        Assert.Contains(">pv (k)</span>", Reference.Pictor(await Reference.ExampleAsync("LegendFormatter")), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "LEGEND-08")]
    public async Task LegendLayoutsAndAlignmentsMatchRechartsAsync()
    {
        foreach (var name in new[] { "LegendFormatter", "LegendVerticalLeft", "LegendMiddle", "LegendVerticalCenter", "LegendWrapperStyle" })
        {
            Assert.Empty(await DifferencesAsync(name));
        }

        Assert.Contains("style=\"position:absolute;width:450px;height:auto;line-height:30px;top:0px;left:100px;background-color:#f5f5f5\"", Reference.Pictor(await Reference.ExampleAsync("LegendWrapperStyle")), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "LEGEND-09")]
    public async Task TheIconSizeAndTheLegendWidthShapeTheRowsAsync()
    {
        Assert.Empty(await DifferencesAsync("LegendIconType"));
        Assert.Empty(await DifferencesAsync("LegendWidth"));
    }

    [Fact]
    [Trait("AC", "LEGEND-10")]
    public async Task APlainLineTakesItsSeriesDashesAndRepeatedNamesAreListedOnceAsync()
    {
        var markup = Reference.Pictor(await Reference.ExampleAsync("LegendDashed"));

        Assert.Empty(await DifferencesAsync("LegendDashed"));
        Assert.Contains("stroke-dasharray=\"5 5\" x1=\"0\" y1=\"16\"", markup, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Count(markup, "<li class=\"recharts-legend-item"));
    }

    [Fact]
    [Trait("AC", "LEGEND-11")]
    public async Task RadialBarItemsAndFunnelsAreToggledByTheirLegendAsync()
    {
        var radial = await Reference.ExampleAsync("SimpleRadialBarChart");
        var funnel = await Reference.ExampleAsync("FunnelChart");
        var bars = Write(radial.Spec with { Legend = radial.Spec.Legend! with { Toggles = true } }, radial.Data);
        var funnelSpec = funnel.Spec with { Legend = new LegendSpec { Toggles = true } };
        var funnels = Write(funnelSpec, funnel.Data);

        Assert.Equal(7, Regex.Count(bars, "<input type=\"checkbox\" hidden class=\"pictor-t"));
        Assert.Equal(7, Regex.Count(bars, "<g class=\"pictor-s\\d\"><path fill=\"[^\"]*\" class=\"recharts-sector recharts-radial-bar-sector undefined\""));
        Assert.Equal(7, Regex.Count(bars, "<g class=\"pictor-s\\d\"><text"));
        Assert.Equal(7, Regex.Count(bars, "<g class=\"pictor-s\\d\"><path class=\"pictor-hit\""));
        Assert.Contains("<g class=\"pictor-s0\"><g class=\"recharts-layer recharts-trapezoids\">", funnels, StringComparison.Ordinal);
        Assert.Equal(5, Regex.Count(funnels, "<g class=\"pictor-s0 pictor-hk\\d+\"><path class=\"pictor-hit\""));

        var example = funnel with { Spec = funnelSpec };
        var result = await desktop.Session.Dispatch(
            () => Desktop.TooltipDesktopTests.Run(example, (window, chart) =>
            {
                var plot = chart.Plot();
                var item = Plots.LegendOf(plot)!.Items[0];
                var inside = (window.Bounds.Width, Hovered: Plots.Hovered(plot, 350, 100));
                var point = new Point(item.X + 5, item.Y + 5);
                window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
                window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
                return (inside.Hovered, chart.Concealed.ToArray(), Plots.Hovered(chart.Plot(), 350, 100));
            }),
            TestContext.Current.CancellationToken);

        Assert.True(result.Item1 >= 0);
        Assert.Equal([0], result.Item2);
        Assert.True(result.Item3 < 0);
    }

    private static async Task<List<string>> DifferencesAsync(string name) =>
        [.. (await Reference.CompareAsync(name)).Differences, .. await Reference.LegendDifferencesAsync(name)];

    private static (int, int, int) Rgb(string color)
    {
        var parsed = Avalonia.Media.Color.Parse(color);
        return (parsed.R, parsed.G, parsed.B);
    }

    private static string Write(ChartSpec spec, object? data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, data, static (_, _) => null);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
