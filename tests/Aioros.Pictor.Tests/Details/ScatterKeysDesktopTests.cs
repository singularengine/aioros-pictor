using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Tests.Desktop;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Details;

public sealed class ScatterKeysDesktopTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "DETAIL-05")]
    public async Task AScatterChartsKeysDrawRechartsCrossOnTheDesktopAsync()
    {
        var example = await Reference.ExampleAsync("DetailsKeyboardScatterChart");
        var expected = await ScatterKeysTests.StepsAsync("DetailsKeyboardScatterChart", 6);
        Key[][] sequences = [[], [Key.Right, Key.Right], [Key.Right, Key.Right, Key.Left], [.. Enumerable.Repeat(Key.Right, 7)], [Key.Left, Key.Down, Key.Escape]];

        var seen = await desktop.Session.Dispatch(
            () =>
            {
                var results = new List<(int Step, int Ink, int Active, int Elsewhere)>();
                foreach (var keys in sequences)
                {
                    var (chart, window) = Shown(example);
                    foreach (var key in keys)
                    {
                        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
                    }

                    Dispatcher.UIThread.RunJobs();
                    var layout = (ChartLayout)chart.Plot();
                    var (xs, _) = ScatterKeys.Points(layout);
                    var step = chart.KeyCross;
                    results.Add((step, Math.Max(Ink(window, (int)Math.Floor(xs[Math.Max(step, 0)])), Ink(window, (int)Math.Ceiling(xs[Math.Max(step, 0)]))), chart.ActiveIndex, Ink(window, 200)));
                    window.Close();
                }

                var (pointed, pointedWindow) = Shown(example);
                pointedWindow.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.None, null);
                var plot = (ChartLayout)pointed.Plot();
                var point = plot.Series[0];
                pointedWindow.MouseMove(new Point(point.X[0], point.Y[0]), RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
                var entered = (pointed.KeyCross, pointed.ActiveIndex);
                pointedWindow.MouseMove(new Point(2, 2), RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
                results.Add((entered.KeyCross, entered.ActiveIndex, pointed.KeyCross, pointed.ActiveIndex));
                pointedWindow.Close();
                return results;
            },
            TestContext.Current.CancellationToken);

        for (var i = 0; i < sequences.Length; i++)
        {
            Assert.Equal(expected[i], seen[i].Step);
            Assert.True(seen[i].Ink > 40, $"sequence {i}: {seen[i].Ink} gray pixels along the cross");
            Assert.True(seen[i].Active < 0, "no tooltip from the keyboard");
            Assert.True(seen[i].Elsewhere < 20, $"sequence {i}: {seen[i].Elsewhere} gray pixels away from the cross");
        }

        Assert.True(seen[^1].Step < 0 && seen[^1].Ink == 0, "the pointer on a point replaces the cross with the point's state");
        Assert.True(seen[^1].Active < 0 && seen[^1].Elsewhere < 0, "leaving the chart shows nothing");
    }

    private static (Chart Chart, Window Window) Shown(RechartsExample example)
    {
        var chart = SpecControls.Create(example);
        chart.AccessibilityLayer = true;
        var window = new Window { Width = example.Spec.Width, Height = example.Spec.Height, Content = chart };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        chart.Focus();
        Dispatcher.UIThread.RunJobs();
        return (chart, window);
    }

    /// <summary>How many pixels of a column of the frame are gray (the cross's vertical line, or only the grid's crossings).</summary>
    private static int Ink(Window window, int x)
    {
        using var frame = window.CaptureRenderedFrame()!;
        var (width, height, bgra) = DesktopRender.Pixels(frame);
        var count = 0;
        for (var row = 0; row < height; row++)
        {
            var i = ((row * width) + x) * 4;
            count += bgra[i] is > 0xa0 and < 0xf0 && bgra[i] == bgra[i + 1] && bgra[i] == bgra[i + 2] ? 1 : 0;
        }

        return count;
    }
}
