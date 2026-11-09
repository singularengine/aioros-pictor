using System.Buffers;
using System.Text;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Svg;
using Aioros.Pictor.Tests.Desktop;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Interactive;

public sealed class ZoomTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "INTERACT-07")]
    public async Task AZoomedNestTreemapIsRechartsRenderAfterTheClickWithZoomButtonsAsync()
    {
        var example = await Reference.ExampleAsync("NestedTreemap");
        var zoomed = example.Spec with { Treemap = example.Spec.Treemap! with { Zoom = "0" } };
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, zoomed, example.Data, static (_, _) => null);
        var verdict = MarkupComparison.Compare(await Reference.RechartsAsync("NestedTreemap.0"), Encoding.UTF8.GetString(buffer.WrittenSpan));

        var written = await desktop.Session.Dispatch(
            () =>
            {
                var chart = (Treemap)WebChart.Create(example);
                chart.Zoom = "0";
                return WebChart.Write(chart, example.Spec.Width, example.Spec.Height);
            },
            TestContext.Current.CancellationToken);
        var zooms = written.Reserved.Where(reserved => reserved.Key == "zoom").Select(reserved => reserved.Parameter).ToList();

        Assert.Empty(verdict.Differences);
        var field = Assert.Single(written.Reserved, reserved => reserved.Parameter is null);
        Assert.Equal("zoom-path", field.Key);
        Assert.Contains($"<input type=\"hidden\" class=\"pictor-off\" value=\"0\" name=\"f{field.Id}\" data-change>", written.Html, StringComparison.Ordinal);
        Assert.Contains(string.Empty, zooms);
        Assert.Contains("0", zooms);
        Assert.DoesNotContain("1", zooms);
        Assert.All(zooms.Where(zoom => zoom is not ("" or "0")), zoom => Assert.StartsWith("0.", zoom, StringComparison.Ordinal));
        Assert.Contains(">root<button class=\"pictor-zoom\" aria-label=\"root\"", written.Html, StringComparison.Ordinal);
        Assert.Contains(">axis<button class=\"pictor-zoom\" aria-label=\"axis\"", written.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("recharts-treemap-nest-index-box pictor-off", written.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<button hidden", written.Html, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "INTERACT-08")]
    public async Task ANestTreemapZoomsOnTheDesktopAsync()
    {
        var example = await Reference.ExampleAsync("NestedTreemap");

        var (inside, back, bound) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = (Treemap)WebChart.Create(example);
                var window = new Window { Width = example.Spec.Width, Height = example.Spec.Height, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                Click(window, new Point(80, 60));
                var zoomedIn = (chart.Zoom, ((TreemapPlot)chart.Plot()).Root.Name, ((TreemapPlot)chart.Plot()).Root.Children!.Length);
                var plot = (TreemapPlot)chart.Plot();
                var root = NestBoxes.Place(plot.Trail, 14, plot.Spec.Width, name => Text.InterMetrics.Width(name, 14))[0];
                Click(window, new Point(root.Left + (root.Width / 2), plot.Spec.Height + 12));
                var zoomedOut = (chart.Zoom, ((TreemapPlot)chart.Plot()).Root.Children!.Length);
                var picks = new Picks();
                chart.ZoomCommand = picks;
                Click(window, new Point(80, 60));
                var result = (zoomedIn, zoomedOut, (picks.Ran.ToList(), chart.Zoom));
                window.Close();
                return result;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(("0", "axis"), (inside.Zoom, inside.Name));
        Assert.Equal(("", 5), back);
        Assert.Equal(["0"], bound.Item1);
        Assert.Equal(string.Empty, bound.Zoom);
        Assert.True(inside.Length > 1);
    }

    private static void Click(Window window, Point point)
    {
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }
}
