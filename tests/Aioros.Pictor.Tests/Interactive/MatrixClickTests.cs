using System.Text.Json;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Tests.Desktop;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Interactive;

public sealed class MatrixClickTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "MATRIX-01")]
    public async Task AreaClicksRunTheIndexesRechartsReceivedAsync()
    {
        var runs = await ClickAsync("MatrixClickAreaChart", chart => new() { ["chart"] = Picks(chart), ["dot"] = Picks(chart.Elements.OfType<Area>().Single()) });

        Assert.Equal([("dot", "2"), ("chart", "4")], runs.Pictor);
        Assert.Equal(runs.Recharts, runs.Pictor);
        Assert.All(runs.Pictor, run => Assert.Contains(run.Parameter, runs.Reserved));
    }

    [Fact]
    [Trait("AC", "MATRIX-02")]
    public async Task ComposedClicksRunTheIndexesRechartsReceivedAsync()
    {
        var runs = await ClickAsync("MatrixClickComposedChart", chart => new()
        {
            ["chart"] = Picks(chart),
            ["bar"] = Picks(chart.Elements.OfType<Bar>().Single()),
            ["scatter"] = Picks(chart.Elements.OfType<Scatter>().Single()),
            ["line"] = Picks(chart.Elements.OfType<Line>().Single()),
            ["area"] = Picks(chart.Elements.OfType<Area>().Single()),
        });

        Assert.Equal([("bar", "2"), ("line", "1"), ("scatter", "0"), ("chart", "5")], runs.Pictor);
        Assert.Equal(runs.Recharts, runs.Pictor);
        Assert.All(runs.Pictor, run => Assert.Contains(run.Parameter, runs.Reserved));
    }

    [Fact]
    [Trait("AC", "MATRIX-03")]
    public async Task ScatterClicksRunTheIndexesRechartsReceivedAsync()
    {
        var runs = await ClickAsync("MatrixClickScatterChart", chart => new() { ["scatter"] = Picks(chart.Elements.OfType<Scatter>().Single()) });

        Assert.Equal([("scatter", "0"), ("scatter", "2")], runs.Pictor);
        Assert.Equal(runs.Recharts, runs.Pictor);
        Assert.Equal(["0", "1", "2", "3", "4", "5"], runs.Reserved);
    }

    [Fact]
    [Trait("AC", "MATRIX-04")]
    public async Task RadarClicksRunTheIndexesRechartsReceivedAsync()
    {
        var runs = await ClickAsync("MatrixClickRadarChart", chart => new()
        {
            ["chart"] = Picks(chart),
            ["mike"] = Picks(chart.Elements.OfType<Radar>().Single(radar => radar.DataKey == "A")),
            ["lily"] = Picks(chart.Elements.OfType<Radar>().Single(radar => radar.DataKey == "B")),
        });

        Assert.Equal([("mike", "0"), ("lily", "1"), ("chart", "2")], runs.Pictor);
        Assert.Equal(runs.Recharts, runs.Pictor);
        Assert.All(runs.Pictor, run => Assert.Contains(run.Parameter, runs.Reserved));
    }

    [Fact]
    [Trait("AC", "MATRIX-05")]
    public async Task RadialBarClicksRunTheIndexesRechartsReceivedAsync()
    {
        var runs = await ClickAsync("MatrixClickRadialBarChart", chart => new() { ["chart"] = Picks(chart), ["bar"] = Picks(chart.Elements.OfType<RadialBar>().Single()) });
        var example = await Reference.ExampleAsync("BarChartHasBackground");
        var band = await desktop.Session.Dispatch(
            () =>
            {
                var chart = WebChart.Create(example);
                var picks = Picks(chart.Elements.OfType<Bar>().First(bar => bar.DataKey == "pv"));
                var window = new Window { Width = 500, Height = 300, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var layout = (Aioros.Pictor.Layout.ChartLayout)chart.Plot();
                var third = layout.Series[0].Bars[2];
                var point = new Point(third.X + (third.Width / 2), layout.Top + 3);
                window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
                window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
                window.Close();
                return picks.Ran.ToList();
            },
            TestContext.Current.CancellationToken);

        Assert.Equal([("bar", "3"), ("bar", "5")], runs.Pictor);
        Assert.Equal(runs.Recharts, runs.Pictor);
        Assert.All(runs.Pictor, run => Assert.Contains(run.Parameter, runs.Reserved));
        Assert.Equal(["2"], band);
    }

    private sealed record Runs(List<(string Handler, string? Parameter)> Pictor, List<(string Handler, string? Parameter)> Recharts, List<string?> Reserved);

    private static Picks Picks(AvaloniaObject target)
    {
        var picks = new Picks();
        target.SetValue(target is Chart ? Chart.ClickCommandProperty : Clickable.CommandProperty, picks);
        return picks;
    }

    /// <summary>
    /// Clicks the example's chart on the desktop at each point Recharts was clicked, returning the command each click ran
    /// (a command's name and parameter), what Recharts' handlers received (the first handler, an item's before the
    /// chart's, with the chart's index for a dot handler that received none), and the parameters of the web chart's buttons.
    /// </summary>
    private async Task<Runs> ClickAsync(string name, Func<Chart, Dictionary<string, Picks>> commands)
    {
        var example = await Reference.ExampleAsync(name);
        var points = await PointsAsync(name);
        var recharts = new List<(string, string?)>();
        for (var i = 0; i < points.Count; i++)
        {
            recharts.AddRange(await RechartsAsync(name, i));
        }

        var (pictor, reserved) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = WebChart.Create(example);
                var picks = commands(chart);
                var web = WebChart.Write(chart, example.Spec.Width, example.Spec.Height);
                var window = new Window { Width = example.Spec.Width, Height = example.Spec.Height, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var ran = new List<(string, string?)>();
                foreach (var point in points)
                {
                    var before = picks.ToDictionary(pick => pick.Key, pick => pick.Value.Ran.Count, StringComparer.Ordinal);
                    window.MouseMove(point, RawInputModifiers.None);
                    window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
                    window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
                    Dispatcher.UIThread.RunJobs();
                    ran.AddRange(picks.SelectMany(pick => pick.Value.Ran.Skip(before[pick.Key]).Select(parameter => (pick.Key, parameter))));
                }

                window.Close();
                return (ran, web.Reserved.Select(button => button.Parameter).ToList());
            },
            TestContext.Current.CancellationToken);
        return new Runs(pictor, recharts, reserved);
    }

    private static async Task<List<Point>> PointsAsync(string name)
    {
        await using var stream = File.OpenRead(Path.Combine(Reference.Root, "tools", "recharts", "examples", name + ".json"));
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        return [.. document.RootElement.GetProperty("interactions").EnumerateArray()
            .Select(steps => steps.EnumerateArray().Last(step => step.TryGetProperty("click", out _)).GetProperty("click"))
            .Select(click => new Point(click[0].GetDouble(), click[1].GetDouble()))];
    }

    private static async Task<List<(string, string?)>> RechartsAsync(string name, int interaction)
    {
        await using var stream = File.OpenRead(ReferenceFiles.Of(Reference.Root, $"{name}.{interaction}.clicks.json"));
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var calls = document.RootElement.EnumerateArray().ToList();
        if (calls.Count == 0)
        {
            return [];
        }

        var chart = calls.FirstOrDefault(call => call.GetProperty("handler").GetString() == "chart");
        var first = calls[0].GetProperty("index");
        var index = first.ValueKind == JsonValueKind.Number ? first.GetInt32() : chart.GetProperty("index").GetInt32();
        return [(calls[0].GetProperty("handler").GetString()!, index.ToString(System.Globalization.CultureInfo.InvariantCulture))];
    }
}
