using Aioros.Pictor.Demo;
using Aioros.Pictor.Tests.Desktop;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Details;

public sealed class PresentationTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "DETAIL-06")]
    public async Task SvgPresentationAttributesStandWhereRechartsSpreadsThemAsync()
    {
        var example = await Reference.ExampleAsync("DetailsPresentationChart");
        var verdict = await Reference.CompareAsync("DetailsPresentationChart");
        var markup = Reference.Pictor(example);

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
        Assert.Contains("<g class=\"recharts-layer recharts-errorBar\" opacity=\"0.7\">", markup, StringComparison.Ordinal);
        Assert.Contains("stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-miterlimit=\"2\"", markup, StringComparison.Ordinal);

        var (specs, plain, presented) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var spec = chart.Spec(example.Spec.Width, example.Spec.Height);
                var bare = SpecControls.Create(example with { Spec = example.Spec with { Series = [.. example.Spec.Series.Select(series => series with { Presentation = null })] } });
                return ((spec.XAxes[0].Presentation, spec.YAxes[0].Presentation, spec.Grid!.Presentation, spec.Series[1].Presentation, spec.Series[1].ErrorBars[0].Presentation), Ink(bare, example), Ink(chart, example));
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(example.Spec.XAxes[0].Presentation, specs.Item1);
        Assert.Equal(example.Spec.YAxes[0].Presentation, specs.Item2);
        Assert.Equal(example.Spec.Grid!.Presentation, specs.Item3);
        Assert.Equal(example.Spec.Series[1].Presentation, specs.Item4);
        Assert.Equal(example.Spec.Series[1].ErrorBars[0].Presentation, specs.Item5);
        Assert.True(presented < plain - 100, $"{plain} strongly orange pixels without the line's attributes, {presented} with them");
    }

    /// <summary>How many pixels of the chart's frame are close to the line's full orange (#ff7300, in either channel order): fewer when the line is drawn at an opacity.</summary>
    private static int Ink(Chart chart, RechartsExample example)
    {
        var window = new Window { Width = example.Spec.Width, Height = example.Spec.Height, Content = chart };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        using var frame = window.CaptureRenderedFrame()!;
        var (width, height, bgra) = DesktopRender.Pixels(frame);
        window.Close();
        var count = 0;
        for (var i = 0; i < width * height * 4; i += 4)
        {
            var (low, high) = (Math.Min(bgra[i], bgra[i + 2]), Math.Max(bgra[i], bgra[i + 2]));
            count += high > 0xf0 && bgra[i + 1] is > 0x60 and < 0x80 && low < 0x20 ? 1 : 0;
        }

        return count;
    }
}
