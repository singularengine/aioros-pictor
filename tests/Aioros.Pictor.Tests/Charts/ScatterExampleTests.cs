using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;
using Aioros.Pictor.Tests.Desktop;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;

namespace Aioros.Pictor.Tests.Charts;

public sealed partial class ScatterExampleTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "SCATTER-01")]
    public async Task AScatterOnNumberAxesMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("SimpleScatterChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "SCATTER-02")]
    public async Task ZAxisSizesAndSymbolsMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("ThreeDimScatterChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "SCATTER-03")]
    public async Task JointLinesMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("JointLineScatterChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "SCATTER-04")]
    public async Task ScatterLabelsMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("ScatterChartWithLabels");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "SCATTER-05")]
    public async Task ErrorBarsAndAReferenceAreaOnAScatterMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("ScatterChartWithErrorBars");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "SCATTER-06")]
    public async Task EveryPointHasItsHoverStateOnTheWebAsync()
    {
        var example = await Reference.ExampleAsync("ThreeDimScatterChart");
        var first = example.Spec.Series[0];
        var columns = first.Data!.Columns.SetItem("x", first.Data.Columns["x"].SetItem(5, null));
        var gapped = example with { Spec = example.Spec with { Series = example.Spec.Series.SetItem(0, first with { Data = first.Data with { Columns = columns } }) } };

        var markup = Reference.Pictor(example);
        var withGap = Reference.Pictor(gapped);

        var tips = Tips(markup);
        Assert.Equal(12, tips.Count);
        Assert.Equal(12, Count(markup, " pictor-hit\" d="));
        Assert.Contains("stroke-dasharray=\"3 3\"", string.Concat(tips), StringComparison.Ordinal);
        Assert.Equal(12, Count(string.Concat(tips), "recharts-tooltip-cursor"));
        Assert.DoesNotContain("class=\"pictor-c0\"", markup, StringComparison.Ordinal);
        Assert.Equal(["stature : 100cm", "weight : 200kg", "score : 200km"], Entries(tips[0]));
        Assert.Equal(11, Tips(withGap).Count);
    }

    [Fact]
    [Trait("AC", "SCATTER-07")]
    public async Task HoveringAPointShowsItsTooltipOnTheDesktopAsync()
    {
        var example = await Reference.ExampleAsync("ThreeDimScatterChart");

        var (active, empty) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var window = new Window { Width = 500, Height = 400, Content = chart };
                window.Show();
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                window.MouseMove(new Point(363.3, 98.3), RawInputModifiers.None);
                var hovered = chart.ActiveIndex;
                window.MouseMove(new Point(120, 300), RawInputModifiers.None);
                var nothing = chart.ActiveIndex;
                window.Close();
                return (hovered, nothing);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(2, active);
        Assert.True(empty < 0);
    }

    [Fact]
    [Trait("AC", "SCATTER-08")]
    public async Task TheDesktopScatterLooksLikeRechartsAsync()
    {
        var example = await Reference.ExampleAsync("ThreeDimScatterChart");
        var output = Path.Combine(Path.GetTempPath(), "pictor-desktop-" + Guid.NewGuid().ToString("N") + ".png");

        var result = await desktop.Session.Dispatch(
            () => DesktopRender.Compare(example.Name, SpecControls.Create(example), Path.Combine(Reference.Root, "tools", "recharts", "out", "ThreeDimScatterChart.png"), output),
            TestContext.Current.CancellationToken);
        File.Delete(output);

        PixelVerdict.Within(result);
    }

    [Fact]
    [Trait("AC", "SCATTER-09")]
    public async Task ScattersCarryTheirOwnItemsInTheSpecAsync()
    {
        var example = await Reference.ExampleAsync("ThreeDimScatterChart");

        var slot = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                chart.Measure(new Size(500, 400));
                chart.Arrange(new Rect(0, 0, 500, 400));
                return chart.WrittenSlot!;
            },
            TestContext.Current.CancellationToken);
        var spec = SpecJson.Read(slot.Argument);
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, null, static (_, _) => null);

        Assert.All(spec.Series, series => Assert.Equal(["x", "y", "z"], series.Data!.Columns.Keys.Order(StringComparer.Ordinal)));
        Assert.Empty(MarkupComparison.Compare(await Reference.RechartsAsync("ThreeDimScatterChart"), Encoding.UTF8.GetString(buffer.WrittenSpan)).Differences);
    }

    private static List<string> Tips(string markup) =>
        Reference.States(markup, "pictor-tips");

    private static int Count(string text, string part) => Regex.Count(text, Regex.Escape(part));

    private static List<string> Entries(string tip) =>
        [.. Entry().Matches(tip).Select(match => System.Net.WebUtility.HtmlDecode(match.Groups[1].Value))];

    [GeneratedRegex(@"<li style=""color:[^""]*"">([^<]*)</li>")]
    private static partial Regex Entry();
}
