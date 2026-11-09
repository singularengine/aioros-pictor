using System.Buffers;
using System.Text;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Svg;
using Avalonia.Controls;
using Avalonia.Headless;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class BindingTests(DesktopSession desktop)
{
    [Theory]
    [InlineData("line")]
    [InlineData("pie")]
    [InlineData("funnel")]
    [InlineData("treemap")]
    [InlineData("sankey")]
    [InlineData("sunburst")]
    [Trait("AC", "BINDING-01")]
    public async Task AnItemsPropertyChangeLaysEveryChartOutAgainAsync(string family)
    {
        var (layouts, changed, held) = await desktop.Session.Dispatch(
            () =>
            {
                var (chart, item) = Family(family);
                var window = Shown(chart);
                var before = Markup(chart.Plot());
                var count = chart.Layouts;
                item.Value += 7;
                var waited = chart.Layouts == count;
                window.CaptureRenderedFrame();
                var after = Markup(chart.Plot());
                var current = Markup(Plots.Compute(chart.Plot().Spec, Pictor.Data.ChartData.From(chart.Data, static (_, _) => null)));
                window.Close();
                return (waited ? chart.Layouts - count : -1, before != after, after == current);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(1, layouts);
        Assert.True(changed);
        Assert.True(held);
    }

    [Fact]
    [Trait("AC", "BINDING-02")]
    public async Task ManyChangesInAFrameLayTheChartOutOnceAsync()
    {
        var (between, after, played) = await desktop.Session.Dispatch(
            () =>
            {
                var items = Items(100);
                var chart = Line(items);
                var clock = new ManualClock();
                chart.Time = clock;
                var window = Shown(chart, reduceMotion: false);
                clock.Advance(2000);
                window.CaptureRenderedFrame();
                var count = chart.Layouts;
                foreach (var item in items)
                {
                    item.Value += 1;
                    item.Value += 1;
                }

                var waiting = chart.Layouts - count;
                window.CaptureRenderedFrame();
                var result = (waiting, chart.Layouts - count, chart.EnterPlayed);
                window.Close();
                return result;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(0, between);
        Assert.Equal(1, after);
        Assert.True(played);
    }

    [Fact]
    [Trait("AC", "BINDING-03")]
    public async Task ARemovedItemIsNoLongerFollowedAsync()
    {
        var (removal, removedChange, removedHandlers, replacedChange, replacedHandlers) = await desktop.Session.Dispatch(
            () =>
            {
                var items = new WatchedCollection<NotifyingDictionary>(Items(5));
                var chart = Line(items);
                var window = Shown(chart);
                var removed = items[2];
                items.RemoveAt(2);
                var count = chart.Layouts;
                window.CaptureRenderedFrame();
                var layouts = chart.Layouts - count;
                removed.Value = 99;
                count = chart.Layouts;
                window.CaptureRenderedFrame();
                var afterRemoved = chart.Layouts - count;
                var replaced = items[1];
                items[1] = new NotifyingDictionary { Name = "new", Value = 3 };
                window.CaptureRenderedFrame();
                replaced.Value = 42;
                count = chart.Layouts;
                window.CaptureRenderedFrame();
                var result = (layouts, afterRemoved, removed.Handlers, chart.Layouts - count, replaced.Handlers);
                window.Close();
                return result;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(1, removal);
        Assert.Equal(0, removedChange);
        Assert.Equal(0, removedHandlers);
        Assert.Equal(0, replacedChange);
        Assert.Equal(0, replacedHandlers);
    }

    [Fact]
    [Trait("AC", "BINDING-04")]
    public async Task ReplacedDataIsNoLongerFollowedAsync()
    {
        var (oldChange, oldHandlers, newChange) = await desktop.Session.Dispatch(
            () =>
            {
                var old = Items(4);
                var chart = Line(old);
                var window = Shown(chart);
                var fresh = Items(4);
                chart.Data = fresh;
                window.CaptureRenderedFrame();
                old[0].Value = 50;
                var count = chart.Layouts;
                window.CaptureRenderedFrame();
                var afterOld = chart.Layouts - count;
                fresh[0].Value = 50;
                count = chart.Layouts;
                window.CaptureRenderedFrame();
                var result = (afterOld, old.Sum(item => item.Handlers), chart.Layouts - count);
                window.Close();
                return result;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(0, oldChange);
        Assert.Equal(0, oldHandlers);
        Assert.Equal(1, newChange);
    }

    [Fact]
    [Trait("AC", "BINDING-05")]
    public async Task ADetachedChartHoldsNoSubscriptionAsync()
    {
        var (attached, detached, back, value) = await desktop.Session.Dispatch(
            () =>
            {
                var leaves = new WatchedCollection<NotifyingDictionary>(Items(3));
                var tree = new WatchedCollection<NotifyingDictionary>([new NotifyingDictionary { Name = "parent", Children = leaves }, new NotifyingDictionary { Name = "single", Value = 4 }]);
                var treemap = new Treemap { Width = 300, Height = 200, Data = tree };
                var own = new WatchedCollection<NotifyingDictionary>(Items(3));
                var line = new LineChart { Width = 300, Height = 200 };
                line.Elements.Add(new XAxis { DataKey = "Name" });
                line.Elements.Add(new YAxis());
                line.Elements.Add(new Line { DataKey = "Value", Data = own });
                var panel = new StackPanel();
                panel.Children.Add(treemap);
                panel.Children.Add(line);
                var window = Shown(panel);
                int Handlers() => tree.Handlers + leaves.Handlers + own.Handlers + tree.Sum(item => item.Handlers) + leaves.Sum(item => item.Handlers) + own.Sum(item => item.Handlers);
                var whileShown = Handlers();
                window.Content = null;
                var whileOut = Handlers();
                own[1].Value = 1234;
                window.Content = panel;
                window.CaptureRenderedFrame();
                var result = (whileShown, whileOut, Handlers(), ((ChartLayout)line.Plot()).Series[0].Values[1]);
                window.Close();
                return result;
            },
            TestContext.Current.CancellationToken);

        Assert.True(attached >= 3 + 2 + 3 + 3);
        Assert.Equal(0, detached);
        Assert.Equal(attached, back);
        Assert.Equal(1234, value);
    }

    [Fact]
    [Trait("AC", "BINDING-06")]
    public async Task AChangeOnAnotherThreadIsPostedToTheUiThreadAsync()
    {
        var (posts, layouts, value) = await desktop.Session.Dispatch(
            async () =>
            {
                var items = Items(3);
                var chart = Line(items);
                var window = Shown(chart);
                var count = chart.Layouts;
                await Task.Run(
                    () =>
                    {
                        for (var i = 0; i < 50; i++)
                        {
                            items[1].Value = 500 + i;
                        }
                    },
                    TestContext.Current.CancellationToken);
                var before = chart.Layouts - count;
                chart.Dispatcher.RunJobs();
                window.CaptureRenderedFrame();
                var result = (before == 0 ? chart.Posts : -1, chart.Layouts - count, ((ChartLayout)chart.Plot()).Series[0].Values[1]);
                window.Close();
                return result;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal((1, 1, 549.0), (posts, layouts, value));
    }

    [Fact]
    [Trait("AC", "BINDING-07")]
    public async Task TheWebWriterSubscribesToNothingAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");
        var items = Items(4);
        var spec = example.Spec with { XAxes = [example.Spec.XAxes[0] with { DataKey = "Name" }], Series = [example.Spec.Series[0] with { DataKey = "Value" }] };
        var output = Slots.Writer(SpecJson(spec), static (owner, member) => owner == typeof(NotifyingDictionary) ? member switch
        {
            "Name" => static item => ((NotifyingDictionary)item).Name,
            "Value" => static item => ((NotifyingDictionary)item).Value,
            _ => null!,
        } : null!);

        var first = Write(output, items);
        items[2].Value = 777;
        var second = Write(output, items);

        Assert.Contains(">Item 0<", first, StringComparison.Ordinal);
        Assert.All(items, item => Assert.Equal(0, item.Handlers));
        Assert.DoesNotContain(">777<", first, StringComparison.Ordinal);
        Assert.NotEqual(first, second);
    }

    private static string SpecJson(Specs.ChartSpec spec) => Specs.SpecJson.Write(spec);

    private static string Write(Aioros.Runtime.Rendering.SlotWrite write, object data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        write(data, buffer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static List<NotifyingDictionary> Items(int count) => [.. Enumerable.Range(0, count).Select(i => new NotifyingDictionary { Name = "Item " + i, Value = 10 + (i * 3) })];

    private static LineChart Line(System.Collections.IEnumerable items)
    {
        var chart = new LineChart { Width = 400, Height = 300, Data = items };
        chart.Elements.Add(new XAxis { DataKey = "Name" });
        chart.Elements.Add(new YAxis());
        chart.Elements.Add(new Line { DataKey = "Value" });
        return chart;
    }

    private static (Chart Chart, NotifyingDictionary Item) Family(string family)
    {
        var items = Items(4);
        switch (family)
        {
            case "line":
                return (Line(items), items[1]);
            case "pie":
                var pie = new PieChart { Width = 400, Height = 300 };
                pie.Elements.Add(new Pie { DataKey = "Value", NameKey = "Name", Data = items });
                return (pie, items[1]);
            case "funnel":
                var funnel = new FunnelChart { Width = 400, Height = 300 };
                funnel.Elements.Add(new Funnel { DataKey = "Value", NameKey = "Name", Data = items });
                return (funnel, items[1]);
            case "treemap":
                var leaves = Items(3);
                return (new Treemap { Width = 400, Height = 300, DataKey = "Value", Data = new List<NotifyingDictionary> { new() { Name = "parent", Children = leaves }, items[0] } }, leaves[1]);
            case "sankey":
                var links = new List<NotifyingDictionary> { new() { Source = 0, Target = 1, Value = 5 }, new() { Source = 0, Target = 2, Value = 3 } };
                return (new Sankey { Width = 400, Height = 300, Data = new Dictionary<string, object?> { ["nodes"] = items.Take(3).ToList(), ["links"] = links } }, links[1]);
            default:
                var deepest = new NotifyingDictionary { Name = "deepest", Value = 2 };
                var root = new NotifyingDictionary { Name = "root", Value = 30, Children = [new() { Name = "a", Value = 10 }, new() { Name = "b", Value = 12, Children = [deepest] }] };
                return (new SunburstChart { Width = 400, Height = 300, Data = root }, deepest);
        }
    }

    private static Window Shown(Control content, bool reduceMotion = true)
    {
        var window = new Window { Width = 500, Height = 500, Content = content };
        Chart.SetReduceMotion(window, reduceMotion);
        window.Show();
        window.CaptureRenderedFrame();
        return window;
    }

    private static string Markup(IChartPlot plot)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var canvas = new SvgCanvas(buffer);
        Plots.Paint(ref canvas, plot, ChartPainter.NoTips);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private sealed class ManualClock : TimeProvider
    {
        private long _ticks = 1;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks;

        public void Advance(double milliseconds) => _ticks += (long)(milliseconds * TimeSpan.TicksPerMillisecond);
    }
}
