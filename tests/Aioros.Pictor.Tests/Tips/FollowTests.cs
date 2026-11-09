using Aioros.Pictor.Demo;
using Aioros.Pictor.Tests.Desktop;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Tips;

public sealed class FollowTests(DesktopSession desktop)
{
    private const string FollowY = ".pictor-fy .recharts-default-tooltip{--pictor-p:var(--aioros-pointer-y,var(--pictor-fr));translate:0 max(var(--pictor-fs),calc(var(--pictor-p) + var(--pictor-fo) - clamp(0px,(var(--pictor-p) + var(--pictor-fo) + 100% - var(--pictor-fe))*1e5,2*var(--pictor-fo) + 100%)))}";

    [Fact]
    [Trait("AC", "TOOLTIP-18")]
    public async Task AnAxisTooltipFollowsThePointerAcrossItsCategoryOnTheWebAsync()
    {
        var markup = Reference.Pictor(await Reference.ExampleAsync("DetailsFollowLineChart"));

        Assert.Contains("<svg class=\"recharts-surface\" width=\"500\" height=\"300\" data-aioros-pointer=\"viewbox\"", markup, StringComparison.Ordinal);
        Assert.Contains("<g class=\"pictor-hover pictor-fy\" style=\"--pictor-fs:5px;--pictor-fe:248px;--pictor-fo:10px;--pictor-fr:152.5px\"", markup, StringComparison.Ordinal);
        Assert.Contains(FollowY, markup, StringComparison.Ordinal);
        Assert.All(Reference.States(markup, "pictor-tips"), tip => Assert.Matches("<foreignObject x=\"[\\d.]+\" y=\"0\" width=\"1\" height=\"[\\d.]+\"", tip));
        await MatchesAsync("DetailsFollowLineChart", 5);
    }

    [Fact]
    [Trait("AC", "TOOLTIP-19")]
    public async Task ReversedEscapingAndFixedBoxesFollowAsRechartsAsync()
    {
        var reversed = Reference.Pictor(await Reference.ExampleAsync("DetailsFollowReversedChart"));
        var escaping = Reference.Pictor(await Reference.ExampleAsync("DetailsFollowEscapeChart"));
        var fixedY = Reference.Pictor(await Reference.ExampleAsync("DetailsFollowFixedChart"));

        Assert.Contains("<g class=\"pictor-hover pictor-fyr\"", reversed, StringComparison.Ordinal);
        Assert.Contains("<g class=\"pictor-hover pictor-fye\"", escaping, StringComparison.Ordinal);
        Assert.DoesNotContain("data-aioros-pointer", fixedY, StringComparison.Ordinal);
        Assert.DoesNotContain("pictor-fy", fixedY, StringComparison.Ordinal);
        await MatchesAsync("DetailsFollowReversedChart", 4);
        await MatchesAsync("DetailsFollowEscapeChart", 3);
        await MatchesAsync("DetailsFollowFixedChart", 2);
    }

    [Fact]
    [Trait("AC", "TOOLTIP-20")]
    public async Task AVerticalChartsBoxFollowsThePointersXAndItemBoxesStayAsync()
    {
        var vertical = Reference.Pictor(await Reference.ExampleAsync("DetailsFollowVerticalChart"));

        Assert.Contains("<g class=\"pictor-hover pictor-fx\"", vertical, StringComparison.Ordinal);
        Assert.All(Reference.States(vertical, "pictor-tips"), tip => Assert.Matches("<foreignObject x=\"0\" y=\"[\\d.]+\" width=\"[\\d.]+\" height=\"1\"", tip));
        await MatchesAsync("DetailsFollowVerticalChart", 4);
        foreach (var name in (string[])["SimpleScatterChart", "TwoSimplePieChart", "TooltipClick"])
        {
            var markup = Reference.Pictor(await Reference.ExampleAsync(name));
            Assert.DoesNotContain("data-aioros-pointer", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("pictor-f", markup, StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait("AC", "TOOLTIP-21")]
    public async Task TheDesktopBoxFollowsThePointerInsideACategoryAsync()
    {
        var example = await Reference.ExampleAsync("DetailsFollowLineChart");
        var expected = new List<TipState>();
        for (var i = 0; i < 2; i++)
        {
            expected.Add(HoverComparison.RechartsTip(await Reference.RechartsAsync($"DetailsFollowLineChart.{i}")));
        }

        var column = (int)expected[0].X;
        var radar = await Reference.ExampleAsync("DetailsFollowRadarChart");
        var radarExpected = new List<TipState>();
        for (var i = 0; i < 2; i++)
        {
            radarExpected.Add(HoverComparison.RechartsTip(await Reference.RechartsAsync($"DetailsFollowRadarChart.{i}")));
        }

        var seen = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var window = new Window { Width = example.Spec.Width, Height = example.Spec.Height, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var result = new List<(int State, int Top)>();
                foreach (var point in (Point[])[new(150, 40), new(160, 120)])
                {
                    window.MouseMove(point, RawInputModifiers.None);
                    Dispatcher.UIThread.RunJobs();
                    result.Add((chart.ActiveIndex, BorderTop(window, column)));
                }

                window.Close();
                var polarChart = SpecControls.Create(radar);
                var polarWindow = new Window { Width = radar.Spec.Width, Height = radar.Spec.Height, Content = polarChart };
                polarWindow.Show();
                Dispatcher.UIThread.RunJobs();
                foreach (var point in (Point[])[new(250, 60), new(250, 150)])
                {
                    polarWindow.MouseMove(point, RawInputModifiers.None);
                    Dispatcher.UIThread.RunJobs();
                    result.Add((polarChart.ActiveIndex, BorderTop(polarWindow, (int)radarExpected[0].X)));
                }

                polarWindow.Close();
                return result;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(seen[0].State, seen[1].State);
        Assert.Equal(seen[2].State, seen[3].State);
        for (var i = 0; i < 2; i++)
        {
            Assert.InRange(seen[i].Top, expected[i].Y - 1, expected[i].Y + 1);
            Assert.InRange(seen[i + 2].Top, radarExpected[i].Y - 1, radarExpected[i].Y + 1);
        }
    }

    [Fact]
    [Trait("AC", "TOOLTIP-22")]
    public async Task PolarAxisTooltipsFollowThePointerAsRechartsAsync()
    {
        var radar = Reference.Pictor(await Reference.ExampleAsync("DetailsFollowRadarChart"));
        var radial = Reference.Pictor(await Reference.ExampleAsync("DetailsFollowRadialBarChart"));

        Assert.Contains("data-aioros-pointer=\"viewbox\"", radar, StringComparison.Ordinal);
        Assert.Contains("<g class=\"pictor-hover pictor-fp\" style=\"--pictor-cx:", radar, StringComparison.Ordinal);
        Assert.Contains("<g class=\"pictor-hover pictor-fa\" style=\"--pictor-cx:", radial, StringComparison.Ordinal);
        Assert.All(Reference.States(radar, "pictor-tips"), tip => Assert.Matches("<foreignObject x=\"0\" y=\"0\" width=\"[\\d.]+\" height=\"[\\d.]+\" style=\"--pictor-u:[-\\d.e]+;--pictor-v:[-\\d.e]+\"", tip));
        Assert.All(Reference.States(radial, "pictor-tips"), tip => Assert.Matches("style=\"--pictor-r:[\\d.]+px\"", tip));
        await MatchesAsync("DetailsFollowRadarChart", 5);
        await MatchesAsync("DetailsFollowRadialBarChart", 4);
    }

    private static async Task MatchesAsync(string name, int interactions)
    {
        for (var i = 0; i < interactions; i++)
        {
            var differences = Reference.Differences(await Reference.HoverAsync(name, i), 0.5);
            Assert.True(differences.Count == 0, $"{name}.{i}: " + string.Join("\n", differences));
        }
    }

    /// <summary>The first row of the longest run of the box border's gray in a column of the frame: the box's top.</summary>
    private static int BorderTop(Window window, int x)
    {
        using var frame = window.CaptureRenderedFrame()!;
        var (width, height, bgra) = DesktopRender.Pixels(frame);
        var (best, start, run, top) = (0, 0, 0, -1);
        for (var y = 0; y < height; y++)
        {
            var i = ((y * width) + x) * 4;
            var gray = bgra[i] == 0xcc && bgra[i + 1] == 0xcc && bgra[i + 2] == 0xcc;
            (start, run) = gray ? (run == 0 ? y : start, run + 1) : (start, 0);
            if (run > best)
            {
                (best, top) = (run, start);
            }
        }

        return top;
    }
}
