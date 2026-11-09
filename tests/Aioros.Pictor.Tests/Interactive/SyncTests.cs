using Aioros.Pictor.Layout;
using Aioros.Pictor.Svg;
using Aioros.Pictor.Tests.Desktop;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Interactive;

public sealed class SyncTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "SYNC-01")]
    public async Task SyncedChartsShareTheHoveredIndexOnTheWebAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");
        var synced = example.Spec with { SyncId = "a" };

        var html = Write(synced, example.Data);

        Assert.Contains("class=\"recharts-wrapper pictor-y-a\"", html, StringComparison.Ordinal);
        Assert.Contains("&:has(.pictor-y-a .pictor-hk2:hover) .pictor-y-a .pictor-tk2", html, StringComparison.Ordinal);
        Assert.Contains("&:has(.pictor-y-a .pictor-hk6:hover) .pictor-y-a .pictor-c6", html, StringComparison.Ordinal);
        Assert.DoesNotContain("pictor-y-", Reference.Pictor(example), StringComparison.Ordinal);
        foreach (var name in (string[])["SpecifiedDomainRadarChart", "SimpleRadialBarChart"])
        {
            var polar = await Reference.ExampleAsync(name);
            var markup = Write(polar.Spec with { SyncId = "p" }, polar.Data);
            var states = Reference.States(markup, "pictor-hits").Count;

            Assert.Contains("class=\"recharts-wrapper pictor-y-p\"", markup, StringComparison.Ordinal);
            Assert.True(states > 2, $"{name}: {states} hover states");
            Assert.All(Enumerable.Range(0, states), state => Assert.Contains($"&:has(.pictor-y-p .pictor-hk{state}:hover) .pictor-y-p .pictor-tk{state}", markup, StringComparison.Ordinal));
            Assert.DoesNotContain("pictor-y-", Reference.Pictor(polar), StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait("AC", "SYNC-02")]
    public async Task SyncedChartsByValueShareTheHoveredCategoryOnTheWebAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var synced = example.Spec with { SyncId = "b", SyncMethod = "value" };
        var reversed = Reference.Rows(example).Reverse().ToList();

        var first = Write(synced, example.Data);
        var second = Write(synced, reversed);
        var pageC = SyncMarkup.ValueClass("Page C");

        Assert.Contains($" {pageC}\">", first, StringComparison.Ordinal);
        Assert.Contains($" {pageC}\">", second, StringComparison.Ordinal);
        Assert.Contains($"&:has(.pictor-y-b [class^=pictor-hk].{pageC}:hover) .pictor-y-b [class^=pictor-tk].{pageC}", first, StringComparison.Ordinal);
        Assert.True(first.IndexOf($" {pageC}\">", StringComparison.Ordinal) < first.IndexOf($" {SyncMarkup.ValueClass("Page E")}\">", StringComparison.Ordinal));
        Assert.True(second.IndexOf($" {pageC}\">", StringComparison.Ordinal) > second.IndexOf($" {SyncMarkup.ValueClass("Page E")}\">", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("AC", "SYNC-05")]
    public async Task ASyncIdsRulesAreWrittenOncePerPageAsync()
    {
        var line = await Reference.ExampleAsync("SimpleLineChart");
        var bar = await Reference.ExampleAsync("SimpleBarChart");
        (Specs.ChartSpec Spec, object? Data)[] charts =
        [
            (line.Spec with { SyncId = "a" }, line.Data), (line.Spec with { SyncId = "a" }, line.Data), (line.Spec with { SyncId = "a" }, line.Data),
            (line.Spec with { SyncId = "b" }, line.Data), (bar.Spec with { SyncId = "b", SyncMethod = "value" }, bar.Data),
            (bar.Spec with { SyncId = "b", SyncMethod = "value" }, Reference.Rows(bar).Reverse().ToList()),
        ];
        static List<string> Page(IEnumerable<(Specs.ChartSpec Spec, object? Data)> charts, Aioros.Runtime.Rendering.PageStyles styles) =>
            [.. charts.Select(chart => Slots.Render(ChartRenderer.Create(Specs.SpecJson.Write(chart.Spec), static (_, _) => null!), chart.Data, new Dictionary<string, object?>(), styles).Html)];

        var styles = new Aioros.Runtime.Rendering.PageStyles();
        var page = Page(charts, styles);
        var held = Page(charts, styles);
        var alone = Write(line.Spec with { SyncId = "a" }, line.Data);
        var rules = styles.All[0].Value;

        Assert.All(page, html => Assert.DoesNotContain(":has(.pictor-y-", html, StringComparison.Ordinal));
        Assert.Equal(page, held);
        Assert.Equal(["pictor-sync-a.i7", "pictor-sync-b.i7"], styles.All.Take(2).Select(style => style.Key));
        Assert.Equal(3, styles.All.Count);
        Assert.StartsWith("pictor-sync-b.v", styles.All[2].Key, StringComparison.Ordinal);
        Assert.Equal(1, WebChart.Count(rules, "&:has(.pictor-y-a .pictor-hk2:hover) .pictor-y-a .pictor-tk2"));
        Assert.Contains("<style>" + rules + "</style>", alone, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "SYNC-03")]
    public async Task SyncedChartsShareTheHoveredCategoryOnTheDesktopAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");

        var result = await desktop.Session.Dispatch(
            () =>
            {
                var (window, first, second) = Pair(example, "a");
                using var closing = Closing(window);
                var layout = (ChartLayout)first.Plot();
                var x = layout.Categories!.Category(2);
                window.MouseMove(new Point(x, 100), RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
                var byIndex = second.ActiveIndex;
                second.SyncMethod = "value";
                first.SyncMethod = "value";
                second.Data = Reference.Rows(example).Reverse().ToList();
                Dispatcher.UIThread.RunJobs();
                window.MouseMove(new Point(layout.Categories.Category(1), 100), RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
                var byValue = second.ActiveIndex;
                window.MouseMove(new Point(499, 299), RawInputModifiers.None);
                window.MouseMove(new Point(499, 450), RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
                var left = (first.ActiveIndex, second.ActiveIndex);
                return (byIndex, byValue, left);
            },
            TestContext.Current.CancellationToken);
        var radar = await Reference.ExampleAsync("SpecifiedDomainRadarChart");
        var radial = await Reference.ExampleAsync("SimpleRadialBarChart");
        var polar = await desktop.Session.Dispatch(
            () =>
            {
                var (window, first, second) = Pair(radar, "r");
                using var closing = Closing(window);
                var layout = (Layout.PolarLayout)first.Plot();
                var angle = (90 - (2 * 360.0 / layout.Categories.Length)) * Math.PI / 180;
                window.MouseMove(new Point(layout.Cx + (layout.OuterRadius / 2 * Math.Cos(angle)), layout.Cy - (layout.OuterRadius / 2 * Math.Sin(angle))), RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
                var radarStates = (first.ActiveIndex, second.ActiveIndex);
                var (barWindow, firstBars, secondBars) = Pair(radial, "s");
                using var barClosing = Closing(barWindow);
                var bars = (Layout.PolarLayout)firstBars.Plot();
                barWindow.MouseMove(new Point(bars.Cx, bars.Cy - (bars.OuterRadius * 0.9)), RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
                var barStates = (firstBars.ActiveIndex, secondBars.ActiveIndex);
                return (radarStates, barStates);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(2, result.byIndex);
        Assert.Equal(5, result.byValue);
        Assert.True(result.left.Item1 < 0 && result.left.Item2 < 0, $"{result.left}");
        Assert.Equal((2, 2), polar.radarStates);
        Assert.True(polar.barStates.Item1 >= 0, $"{polar.barStates}");
        Assert.Equal(polar.barStates.Item1, polar.barStates.Item2);
    }

    [Fact]
    [Trait("AC", "SYNC-04")]
    public async Task ASyncedBrushMovesTheOtherBrushOnTheDesktopAsync()
    {
        var example = await Reference.ExampleAsync("BrushWindowBarChart");

        var (moved, follower) = await desktop.Session.Dispatch(
            () =>
            {
                var (window, first, second) = Pair(example, "c");
                using var closing = Closing(window);
                var brush = ((ChartLayout)first.Plot()).Brush!;
                var step = (brush.Width - brush.Spec.TravellerWidth) / (brush.Count - 1);
                var y = brush.Y + (brush.Height / 2);
                window.MouseDown(new Point(brush.EndX + 2, y), MouseButton.Left, RawInputModifiers.None);
                window.MouseMove(new Point(brush.EndX + 2 - (2.5 * step), y), RawInputModifiers.LeftMouseButton);
                window.MouseUp(new Point(brush.EndX + 2 - (2.5 * step), y), MouseButton.Left, RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
                var a = first.Elements.OfType<Brush>().Single();
                var b = second.Elements.OfType<Brush>().Single();
                return ((a.StartIndex, a.EndIndex), (b.StartIndex, b.EndIndex));
            },
            TestContext.Current.CancellationToken);

        Assert.Equal((5, 12), moved);
        Assert.Equal(moved, follower);
    }

    /// <summary>Closes a window when the dispatch ends, failed or not, so the static sync groups and the session keep no chart of a failed test.</summary>
    private static Closer Closing(Window window) => new(window);

    private readonly struct Closer(Window window) : IDisposable
    {
        public void Dispose() => window.Close();
    }

    private static (Window Window, Chart First, Chart Second) Pair(Demo.RechartsExample example, string sync)
    {
        var first = WebChart.Create(example);
        var second = WebChart.Create(example);
        first.SyncId = sync;
        second.SyncId = sync;
        var panel = new StackPanel();
        panel.Children.Add(first);
        panel.Children.Add(second);
        first.Height = example.Spec.Height;
        second.Height = example.Spec.Height;
        var window = new Window { Width = example.Spec.Width, Height = 2 * example.Spec.Height, Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, first, second);
    }

    private static string Write(Specs.ChartSpec spec, object? data)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, data, static (_, _) => null);
        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
