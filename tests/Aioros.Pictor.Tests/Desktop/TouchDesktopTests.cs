using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class TouchDesktopTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "TOUCH-01")]
    public Task ALineChartsTooltipFollowsTheFingerAsync() => AssertTouchAsync("SimpleLineChart", follows: true);

    [Fact]
    [Trait("AC", "TOUCH-02")]
    public Task AnAreaChartsTooltipFollowsTheFingerAsync() => AssertTouchAsync("SimpleAreaChart", follows: true);

    [Fact]
    [Trait("AC", "TOUCH-03")]
    public Task ABarChartsTooltipFollowsTheFingerAsync() => AssertTouchAsync("SimpleBarChart", follows: true);

    [Fact]
    [Trait("AC", "TOUCH-04")]
    public Task AComposedChartsTooltipFollowsTheFingerAsync() => AssertTouchAsync("LineBarAreaComposedChart", follows: true);

    [Fact]
    [Trait("AC", "TOUCH-05")]
    public Task AScatterPointsTooltipStaysWhenTheFingerMovesAsync() => AssertTouchAsync("SimpleScatterChart", follows: false);

    [Fact]
    [Trait("AC", "TOUCH-06")]
    public Task APieSectorsTooltipStaysWhenTheFingerMovesAsync() => AssertTouchAsync("TwoSimplePieChart", follows: false);

    [Fact]
    [Trait("AC", "TOUCH-07")]
    public Task ARadarChartsTooltipFollowsTheFingerAsync() => AssertTouchAsync("SpecifiedDomainRadarChart", follows: true);

    [Fact]
    [Trait("AC", "TOUCH-08")]
    public Task ARadialBarChartsTooltipFollowsTheFingerAsync() => AssertTouchAsync("SimpleRadialBarChart", follows: true);

    [Fact]
    [Trait("AC", "TOUCH-09")]
    public Task AFunnelTrapezoidsTooltipStaysWhenTheFingerMovesAsync() => AssertTouchAsync("FunnelChart", follows: false);

    [Fact]
    [Trait("AC", "TOUCH-10")]
    public Task ATreemapLeafsTooltipStaysWhenTheFingerMovesAsync() => AssertTouchAsync("ColorPanelTreemap", follows: false);

    [Fact]
    [Trait("AC", "TOUCH-11")]
    public Task ASankeyLinksTooltipStaysWhenTheFingerMovesAsync() => AssertTouchAsync("SimpleSankey", follows: false);

    [Fact]
    [Trait("AC", "TOUCH-12")]
    public Task ASunburstSectorsTooltipStaysWhenTheFingerMovesAsync() => AssertTouchAsync("SimpleSunburst", follows: false);

    [Fact]
    [Trait("AC", "TOUCH-13")]
    public async Task AFingerDragsTheBrushAsync()
    {
        var example = await Reference.ExampleAsync("BrushWindowBarChart");

        var (start, end, count) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var window = new Window { Width = 500, Height = 300, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var brush = ((ChartLayout)chart.Plot()).Brush!;
                var step = (brush.Width - brush.Spec.TravellerWidth) / (brush.Count - 1);
                var y = brush.Y + (brush.Height / 2);
                var finger = window.TouchBegin(new Point(brush.EndX + 2, y), RawInputModifiers.None);
                window.TouchMove(finger, new Point(brush.EndX + 2 - (2.5 * step), y), RawInputModifiers.None);
                window.TouchEnd(finger, new Point(brush.EndX + 2 - (2.5 * step), y), RawInputModifiers.None);
                var element = chart.Elements.OfType<Brush>().Single();
                var result = (element.StartIndex, element.EndIndex, ((ChartLayout)chart.Plot()).Data.Count);
                window.Close();
                return result;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(5, start);
        Assert.Equal(12, end);
        Assert.Equal(8, count);
    }

    /// <summary>
    /// A tap on a state shows it, a drag to another state moves an axis tooltip there and keeps an item's, lifting the
    /// finger keeps it, a tap elsewhere in the window hides it; the web's markup makes the hit groups clickable on a
    /// touch screen.
    /// </summary>
    private async Task AssertTouchAsync(string name, bool follows)
    {
        var example = await Reference.ExampleAsync(name);
        var plot = Plots.Compute(example.Spec, Pictor.Data.ChartData.From(example.Data, static (_, _) => null));
        var (first, second) = States(plot, example.Spec.Width, example.Spec.Height);

        var (tapped, dragged, lifted, outside) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var window = new Window { Width = example.Spec.Width + 100, Height = example.Spec.Height + 100, Content = new Canvas { Children = { chart } } };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var finger = window.TouchBegin(first.Point, RawInputModifiers.None);
                var tap = chart.ActiveIndex;
                window.TouchMove(finger, second.Point, RawInputModifiers.None);
                var drag = chart.ActiveIndex;
                window.TouchEnd(finger, second.Point, RawInputModifiers.None);
                var lift = chart.ActiveIndex;
                var elsewhere = window.TouchBegin(new Point(example.Spec.Width + 50, example.Spec.Height + 50), RawInputModifiers.None);
                window.TouchEnd(elsewhere, new Point(example.Spec.Width + 50, example.Spec.Height + 50), RawInputModifiers.None);
                var result = (tap, drag, lift, chart.ActiveIndex);
                window.Close();
                return result;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(first.State, tapped);
        Assert.Equal(follows ? second.State : first.State, dragged);
        Assert.Equal(dragged, lifted);
        Assert.True(outside < 0, $"a tap outside the chart hides its tooltip ({outside})");
        Assert.Contains("@media (hover:none){.pictor-hover>g{cursor:pointer}}", Reference.Pictor(example), StringComparison.Ordinal);
    }

    private static ((int State, Point Point) First, (int State, Point Point) Second) States(IChartPlot plot, double width, double height)
    {
        var found = new SortedDictionary<int, Point>();
        for (var y = 2.0; y < height; y += 3)
        {
            for (var x = 2.0; x < width; x += 3)
            {
                var state = Plots.Hovered(plot, x, y);
                if (state >= 0)
                {
                    found.TryAdd(state, new Point(x, y));
                }
            }
        }

        var states = found.Keys.ToList();
        var first = states[states.Count / 2];
        var second = states[Math.Min((states.Count / 2) + 1, states.Count - 1)];
        return ((first, found[first]), (second, found[second]));
    }
}
