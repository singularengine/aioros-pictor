using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aioros.Pictor.Tests.Desktop;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Interactive;

public sealed partial class TickClickTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "INTERACT-09")]
    public async Task ATickIsAButtonRunningItsAxisCommandWithItsValueAsync()
    {
        var example = await Reference.ExampleAsync("TickClickChart");
        var (written, xs, ys) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = WebChart.Create(example);
                var (x, y) = (new Picks(), new Picks());
                chart.Elements.OfType<XAxis>().Single().ClickCommand = x;
                chart.Elements.OfType<YAxis>().Single().ClickCommand = y;
                var web = WebChart.Write(chart);
                var window = new Window { Width = 500, Height = 300, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                Click(window, new Point(210, 285));
                Click(window, new Point(50, 140));
                window.Close();
                return (web, x.Ran.ToList(), y.Ran.ToList());
            },
            TestContext.Current.CancellationToken);
        var expected = await BoxesAsync();
        var boxes = Clip().Matches(written.Html).Select(match => Text(match.Groups[1].Value)).ToList();
        var recharts = (await HandlerAsync(0), await HandlerAsync(1));

        Assert.Equal(["Page A", "Page B", "Page C", "Page D", "Page E", "Page F", "Page G", "0", "2500", "5000", "7500", "10000"], written.Reserved.Select(reserved => reserved.Parameter));
        Assert.Equal(expected.Count, boxes.Count);
        Assert.All(expected.Zip(boxes), pair => Assert.True(Math.Abs(pair.First.X - pair.Second.X) <= 0.5 && Math.Abs(pair.First.Y - pair.Second.Y) <= 0.5 && Math.Abs(pair.First.Width - pair.Second.Width) <= 0.5 && Math.Abs(pair.First.Height - pair.Second.Height) <= 0.5, $"{pair.First} in Recharts, {pair.Second} in Pictor"));
        Assert.Equal(("Page C", "5000"), recharts);
        Assert.Equal(["Page C"], xs);
        Assert.Equal(["5000"], ys);
    }

    /// <summary>The boxes Chromium's <c>getBBox</c> gives Recharts' tick texts, X axis first.</summary>
    private static async Task<List<(double X, double Y, double Width, double Height)>> BoxesAsync()
    {
        await using var stream = File.OpenRead(Path.Combine(Reference.Root, "tools", "recharts", "out", "TickClickChart.ticks.json"));
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        return [.. document.RootElement.EnumerateArray().SelectMany(axis => axis.GetProperty("ticks").EnumerateArray())
            .Select(tick => (tick.GetProperty("x").GetDouble(), tick.GetProperty("y").GetDouble(), tick.GetProperty("width").GetDouble(), tick.GetProperty("height").GetDouble()))];
    }

    /// <summary>The value Recharts' handler received after an interaction of the example (kept on its wrapper).</summary>
    private static async Task<string> HandlerAsync(int interaction)
    {
        var markup = await Reference.RechartsAsync("TickClickChart." + interaction.ToString(CultureInfo.InvariantCulture));
        using var tick = JsonDocument.Parse(System.Net.WebUtility.HtmlDecode(Handler().Match(markup).Groups[1].Value));
        var value = tick.RootElement.GetProperty("value");
        return value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText();
    }

    /// <summary>The box of a button's clip path's text: its subpaths after the tick line's.</summary>
    private static (double X, double Y, double Width, double Height) Text(string path)
    {
        var points = path.Split('Z', StringSplitOptions.RemoveEmptyEntries).Skip(1)
            .SelectMany(part => Point().Matches(part).Select(match => (X: double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), Y: double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture))))
            .ToList();
        var (left, top) = (points.Min(point => point.X), points.Min(point => point.Y));
        return (left, top, points.Max(point => point.X) - left, points.Max(point => point.Y) - top);
    }

    private static void Click(Window window, Point point)
    {
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    [GeneratedRegex(@"clip-path:path\('([^']*)'\)")]
    private static partial Regex Clip();

    [GeneratedRegex(@"[ML]([-\d.e]+),([-\d.e]+)")]
    private static partial Regex Point();

    [GeneratedRegex(@"data-tick=""([^""]*)""")]
    private static partial Regex Handler();
}
