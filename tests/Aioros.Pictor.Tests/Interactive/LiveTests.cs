using System.ComponentModel;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Tests.Desktop;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Interactive;

public sealed class LiveTests(DesktopSession desktop)
{
    public sealed class Feed : INotifyPropertyChanged
    {
        private int _next = 5;

        public event PropertyChangedEventHandler? PropertyChanged;

        public IReadOnlyList<IReadOnlyDictionary<string, object?>> Readings { get; private set; } = [.. Enumerable.Range(0, 5).Select(Tick)];

        public void Advance()
        {
            Readings = [.. Readings.Skip(1), Tick(_next)];
            _next++;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Readings)));
        }

        private static Dictionary<string, object?> Tick(int index) => new(StringComparer.Ordinal) { ["Time"] = $"t{index}", ["Value"] = index * 10.0 };
    }

    [Fact]
    [Trait("AC", "LIVE-01")]
    public void EachTickWritesTheNewReadings()
    {
        var spec = new ChartSpec
        {
            Kind = ChartKind.Line,
            XAxes = [new AxisSpec { DataKey = "Time", Type = AxisType.Category }],
            YAxes = [new AxisSpec { Type = AxisType.Number }],
            Series = [new SeriesSpec { Kind = SeriesKind.Line, DataKey = "Value" }],
        };
        var write = Slots.Writer(SpecJson.Write(spec), WebChart.Members);
        var feed = new Feed();
        var first = Write(write, feed.Readings);
        var again = Write(write, feed.Readings);
        feed.Advance();
        var second = Write(write, feed.Readings);
        feed.Advance();
        var third = Write(write, feed.Readings);

        Assert.Equal(first, again);
        Assert.Contains(">t4<", first, StringComparison.Ordinal);
        Assert.DoesNotContain(">t5<", first, StringComparison.Ordinal);
        Assert.Contains(">t5<", second, StringComparison.Ordinal);
        Assert.DoesNotContain(">t0<", second, StringComparison.Ordinal);
        Assert.Contains(">t6<", third, StringComparison.Ordinal);
        Assert.NotEqual(Curve(second), Curve(third));
    }

    [Fact]
    [Trait("AC", "LIVE-02")]
    public async Task TheDesktopChartFollowsEachSnapshotAsync()
    {
        var (before, after) = await desktop.Session.Dispatch(
            () =>
            {
                var feed = new Feed();
                var chart = new LineChart { Width = 500, Height = 300, DataContext = feed };
                chart.Bind(Chart.DataProperty, new Binding(nameof(Feed.Readings)));
                chart.Elements.Add(new XAxis { DataKey = "Time" });
                chart.Elements.Add(new YAxis());
                chart.Elements.Add(new Line { DataKey = "Value" });
                var window = new Window { Width = 500, Height = 300, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var first = ((ChartLayout)chart.Plot()).Categories!.Categories[^1];
                feed.Advance();
                Dispatcher.UIThread.RunJobs();
                var next = ((ChartLayout)chart.Plot()).Categories!.Categories[^1];
                window.Close();
                return (first, next);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("t4", before);
        Assert.Equal("t5", after);
    }

    private static string Write(Aioros.Runtime.Rendering.SlotWrite write, object data)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        write(data, buffer);
        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string Curve(string html)
    {
        var start = html.IndexOf("recharts-line-curve", StringComparison.Ordinal);
        return html[start..html.IndexOf("/>", start, StringComparison.Ordinal)];
    }
}
