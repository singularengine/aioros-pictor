using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using static Aioros.Pictor.Tests.Desktop.DesktopFrames;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class UpdateDesktopTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "UPDATE-10")]
    public async Task NewDataMovesTheBarsFromTheirPreviousHeightsOnTheDesktopAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var rows = Changed(Reference.Rows(example), "pv", 0, 1200);

        var result = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new ManualClock();
                var (chart, host) = Hosted(example, animated: true, clock);
                Frame(host);
                clock.Advance(1000);
                Frame(host);
                var before = ((ChartLayout)chart.Plot()).Series[0].Bars[0];
                var column = (int)(before.X + (before.Width / 2));
                chart.Data = rows;
                var after = ((ChartLayout)chart.Plot()).Series[0].Bars[0];
                var start = Ink(Frame(host), column);
                clock.Advance(280);
                var middle = Ink(Frame(host), column);
                var updating = chart.Updating;
                clock.Advance(232);
                var end = Frame(host);
                var (still, stillHost) = Hosted(example, animated: false, clock);
                still.Data = rows;
                return (before.Height, after.Height, start, middle, updating, end, Frame(stillHost), chart.Updating);
            },
            TestContext.Current.CancellationToken);

        Assert.InRange(result.start, result.Item1 - 2, result.Item1 + 2);
        var expected = result.Item1 + ((result.Item2 - result.Item1) * 0.8024);
        Assert.InRange(result.middle, expected - 2, expected + 2);
        Assert.True(result.updating);
        Assert.Equal(result.Item7, result.end);
        Assert.False(result.Item8);
    }

    [Fact]
    [Trait("AC", "UPDATE-11")]
    public async Task WhatWaitsForTheEndWaitsForEveryPartWhoseItemsChangedAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");
        var rows = Changed(Reference.Rows(example), "pv", 0, 3400);
        var pies = await Reference.ExampleAsync("TwoSimplePieChart");

        var (moving, still, final, pvDot, uvDot) = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new ManualClock();
                var (chart, host) = Hosted(example, animated: true, clock);
                Frame(host);
                clock.Advance(2000);
                Frame(host);
                chart.Data = rows;
                var layout = (ChartLayout)chart.Plot();
                var pv = layout.Series[0];
                var uv = layout.Series[1];
                clock.Advance(830);
                var frame = Frame(host);
                clock.Advance(782);
                var (stillChart, stillHost) = Hosted(example, animated: false, clock);
                stillChart.Data = rows;
                return (frame, Frame(stillHost), Frame(host), ((int)pv.X[3], (int)pv.Y[3]), ((int)uv.X[3], (int)uv.Y[3]));
            },
            TestContext.Current.CancellationToken);
        var spec = Demo.ExampleAnimations.Animated(pies.Spec);
        var first = spec.Polar!.Pies[0];
        var changed = spec with { Polar = spec.Polar with { Pies = [first with { Data = first.Data! with { Columns = first.Data.Columns.SetItem("value", [.. first.Data.Columns["value"].Select((value, i) => i == 0 ? "900" : value)]) } }, spec.Polar.Pies[1]] } };
        var morph = Pictor.Motion.Morph.Plan(Plots.Compute(spec, Pictor.Data.ChartData.From(null, static (_, _) => null)), Plots.Compute(changed, Pictor.Data.ChartData.From(null, static (_, _) => null)));
        var frame = (PolarLayout)morph!.Frame(0);

        Assert.True(IsWhite(Pixel(still, pvDot.Item1, pvDot.Item2)), "the static chart draws the pv dot's white fill");
        Assert.False(IsWhite(Pixel(moving, pvDot.Item1, pvDot.Item2)), "the moving pv line's dots are not drawn: its curve shows");
        Assert.False(IsWhite(Pixel(moving, uvDot.Item1, uvDot.Item2)), "the uv line's points did not move, but its items changed: its dots wait too");
        Assert.Equal(still, final);
        Assert.False(frame.Pies[0].Still);
        Assert.True(frame.Pies[1].Still);
    }

    [Fact]
    [Trait("AC", "UPDATE-12")]
    public async Task ReducedMotionAndInactiveAnimationsDrawNewDataAtOnceAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var rows = Changed(Reference.Rows(example), "pv", 0, 1200);

        var (reduced, inactive, still, updating) = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new ManualClock();
                var (chart, host) = Hosted(example, animated: true, clock);
                Chart.SetReduceMotion(host, value: true);
                Frame(host);
                chart.Data = rows;
                var first = Frame(host);
                var (off, offHost) = Hosted(example, animated: false, clock);
                Frame(offHost);
                off.Data = rows;
                var second = Frame(offHost);
                var (stillChart, stillHost) = Hosted(example, animated: false, clock);
                stillChart.Data = rows;
                return (first, second, Frame(stillHost), chart.Updating || off.Updating);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(still, reduced);
        Assert.Equal(still, inactive);
        Assert.False(updating);
    }

    [Fact]
    [Trait("AC", "UPDATE-13")]
    public async Task AChildsChangeDrawsAtOnceUnlessTheChartHasNoDataOfItsOwnAsync()
    {
        var bars = await Reference.ExampleAsync("SimpleBarChart");
        var pies = await Reference.ExampleAsync("TwoSimplePieChart");

        var (barUpdating, pieUpdating) = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new ManualClock();
                var (chart, host) = Hosted(bars, animated: true, clock);
                Frame(host);
                clock.Advance(1000);
                Frame(host);
                chart.Elements.OfType<Bar>().First().Fill = "#ff0000";
                Frame(host);
                var bar = chart.Updating;
                var (pieChart, pieHost) = Hosted(pies, animated: true, clock);
                pieChart.Data = null;
                Frame(pieHost);
                clock.Advance(3000);
                Frame(pieHost);
                var pie = pieChart.Elements.OfType<Pie>().First();
                pie.Data = Changed(((IEnumerable<IReadOnlyDictionary<string, object?>>)pie.Data!).ToList(), "value", 0, 900);
                Frame(pieHost);
                return (bar, pieChart.Updating);
            },
            TestContext.Current.CancellationToken);

        Assert.False(barUpdating);
        Assert.True(pieUpdating);
    }

    [Fact]
    [Trait("AC", "UPDATE-14")]
    public async Task ANewSizeMovesTheSeriesFromTheirPreviousGeometryAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");

        var (updating, end, still) = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new ManualClock();
                var (chart, host) = Hosted(example, animated: true, clock);
                Frame(host);
                clock.Advance(1000);
                Frame(host);
                chart.Width = 400;
                host.Width = 400;
                host.Measure(new Size(400, 300));
                host.Arrange(new Rect(0, 0, 400, 300));
                Frame(host);
                var moving = chart.Updating;
                clock.Advance(512);
                var last = Frame(host);
                var (stillChart, stillHost) = Hosted(example, animated: false, clock);
                stillChart.Width = 400;
                stillHost.Width = 400;
                stillHost.Measure(new Size(400, 300));
                stillHost.Arrange(new Rect(0, 0, 400, 300));
                return (moving, last, Frame(stillHost));
            },
            TestContext.Current.CancellationToken);

        Assert.True(updating);
        Assert.Equal(still, end);
    }

    [Fact]
    [Trait("AC", "UPDATE-15")]
    public async Task AnItemsPropertyChangeAndACollectionChangeAnimateAsync()
    {
        var (itemUpdating, listUpdating) = await desktop.Session.Dispatch(
            () =>
            {
                var items = new System.Collections.ObjectModel.ObservableCollection<NotifyingDictionary>(
                    Enumerable.Range(0, 5).Select(i => new NotifyingDictionary { Name = "Item " + i, Value = 10 + (i * 3) }));
                var chart = new BarChart { Width = 400, Height = 300, Data = items };
                chart.Elements.Add(new XAxis { DataKey = "Name" });
                chart.Elements.Add(new YAxis());
                chart.Elements.Add(new Bar { DataKey = "Value" });
                var clock = new ManualClock();
                chart.Time = clock;
                var window = new Window { Width = 500, Height = 500, Content = chart };
                window.Show();
                window.CaptureRenderedFrame();
                clock.Advance(1000);
                window.CaptureRenderedFrame();
                items[1].Value = 30;
                Dispatcher.UIThread.RunJobs();
                window.CaptureRenderedFrame();
                var item = chart.Updating;
                clock.Advance(1000);
                window.CaptureRenderedFrame();
                items.Add(new NotifyingDictionary { Name = "Item 5", Value = 20 });
                window.CaptureRenderedFrame();
                var list = chart.Updating;
                window.Close();
                return (item, list);
            },
            TestContext.Current.CancellationToken);

        Assert.True(itemUpdating);
        Assert.True(listUpdating);
    }

    private static bool IsWhite((byte B, byte G, byte R) pixel) => pixel.B > 240 && pixel.G > 240 && pixel.R > 240;

    private static List<IReadOnlyDictionary<string, object?>> Changed(IEnumerable<IReadOnlyDictionary<string, object?>> rows, string key, int index, double value) =>
        [.. rows.Select((row, i) => i == index ? (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>(row) { [key] = value } : row)];
}
