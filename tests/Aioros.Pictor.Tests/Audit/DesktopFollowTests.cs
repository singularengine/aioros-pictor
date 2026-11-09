using System.Collections.ObjectModel;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Tests.Desktop;
using Avalonia.Controls;
using Avalonia.Headless;

namespace Aioros.Pictor.Tests.Audit;

public sealed class DesktopFollowTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "AUDIT-03")]
    public async Task ACollectionChangeOnAnotherThreadIsPostedToTheUiThreadAsync()
    {
        var (posts, before, after, count) = await desktop.Session.Dispatch(
            async () =>
            {
                var items = new ObservableCollection<NotifyingDictionary>(Items(3));
                var chart = Line(items);
                var window = Shown(chart);
                var layouts = chart.Layouts;
                await Task.Run(() => items.Add(new NotifyingDictionary { Name = "Item 3", Value = 99 }), TestContext.Current.CancellationToken);
                var waited = chart.Layouts - layouts;
                var posted = chart.Posts;
                chart.Dispatcher.RunJobs();
                window.CaptureRenderedFrame();
                var result = (posted, waited, chart.Layouts - layouts, ((ChartLayout)chart.Plot()).Series[0].Values.Length);
                window.Close();
                return result;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal((1, 0, 1, 4), (posts, before, after, count));
    }

    [Theory]
    [InlineData("replace")]
    [InlineData("move")]
    [InlineData("reinsert")]
    [Trait("AC", "AUDIT-04")]
    public async Task AnItemRemovedAndPutBackIsFollowedAgainAsync(string change)
    {
        var (value, handlers) = await desktop.Session.Dispatch(
            () =>
            {
                var items = new ObservableCollection<NotifyingDictionary>(Items(3));
                var chart = Line(items);
                var window = Shown(chart);
                var item = items[1];
                switch (change)
                {
                    case "replace":
                        items[1] = item;
                        break;
                    case "move":
                        items.Move(1, 1);
                        break;
                    default:
                        items.RemoveAt(1);
                        items.Insert(1, item);
                        break;
                }

                window.CaptureRenderedFrame();
                item.Value = 900;
                window.CaptureRenderedFrame();
                var result = (((ChartLayout)chart.Plot()).Series[0].Values[1], item.Handlers);
                window.Close();
                return result;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal((900.0, 1), (value, handlers));
    }

    [Fact]
    [Trait("AC", "AUDIT-05")]
    public async Task ChildrenLetGoAreNoLongerFollowedAsync()
    {
        var layouts = await desktop.Session.Dispatch(
            () =>
            {
                var chart = Line(Items(3));
                var line = chart.Elements.OfType<Line>().Single();
                var bar = new Bar { DataKey = "Value" };
                var cell = new Cell { Fill = "#f00" };
                bar.Parts.Add(cell);
                var panorama = new LineChart();
                var gone = new Line { DataKey = "Value" };
                panorama.Elements.Add(gone);
                var brush = new Brush { Panorama = panorama };
                chart.Elements.Add(bar);
                chart.Elements.Add(brush);
                var window = Shown(chart);
                var counts = new List<int>();
                void Changed(Action change)
                {
                    var count = chart.Layouts;
                    change();
                    window.CaptureRenderedFrame();
                    counts.Add(chart.Layouts - count);
                }

                Changed(() => chart.Elements.Remove(line));
                Changed(() => line.Stroke = "#123456");
                Changed(() => chart.Elements.Add(line));
                Changed(() => chart.Elements.Remove(line));
                Changed(() => line.Stroke = "#654321");
                Changed(() => bar.Parts.Remove(cell));
                Changed(() => cell.Fill = "#0f0");
                Changed(() => brush.Panorama = new LineChart());
                Changed(() => gone.Stroke = "#abcdef");
                Changed(() => panorama.Elements.Add(new Line { DataKey = "Value" }));
                window.Close();
                return counts;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal([1, 0, 1, 1, 0, 1, 0, 1, 0, 0], layouts);
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

    private static Window Shown(Control content)
    {
        var window = new Window { Width = 500, Height = 500, Content = content };
        Chart.SetReduceMotion(window, true);
        window.Show();
        window.CaptureRenderedFrame();
        return window;
    }
}
