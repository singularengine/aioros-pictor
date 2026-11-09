using System.Buffers;
using System.Text;
using Aioros.Pictor.Svg;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using static Aioros.Pictor.Tests.Desktop.DesktopFrames;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class ReplayDesktopTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "ANIMATION-15")]
    public async Task AReplayButtonPlaysTheChartsEnterAnimationsAgainAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");

        var (played, restarted, replayed) = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new ManualClock();
                var (chart, host) = Hosted(example, animated: true, clock);
                host.Child = null;
                var button = new ReplayButton { Content = "Replay" };
                var window = new Window { Width = 520, Height = 360, Content = new StackPanel { Children = { button, chart } } };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var bar = ((Layout.ChartLayout)chart.Plot()).Series[0].Bars[0];
                var middle = ((int)(bar.X + (bar.Width / 2)), (int)(bar.Y + (bar.Height / 2)));
                window.CaptureRenderedFrame();
                clock.Advance(1000);
                window.CaptureRenderedFrame();
                var done = Color(chart, middle);
                var center = new Avalonia.Point(button.Bounds.Width / 2, button.Bounds.Height / 2);
                window.MouseDown(center, Avalonia.Input.MouseButton.Left);
                window.MouseUp(center, Avalonia.Input.MouseButton.Left);
                window.CaptureRenderedFrame();
                var again = Color(chart, middle);
                clock.Advance(1000);
                window.CaptureRenderedFrame();
                var after = Color(chart, middle);
                window.Close();
                return (done, again, after);
            },
            TestContext.Current.CancellationToken);
        var buffer = new ArrayBufferWriter<byte>();
        ReplaySlotWriter.Create("Replay", static (_, _) => static _ => null)(null, buffer);
        var markup = Encoding.UTF8.GetString(buffer.WrittenSpan);

        Assert.Equal("#8884d8", played);
        Assert.Equal("#ffffff", restarted);
        Assert.Equal("#8884d8", replayed);
        Assert.StartsWith("<label class=\"pictor-replay\"", markup, StringComparison.Ordinal);
        Assert.Contains("<input type=\"checkbox\"", markup, StringComparison.Ordinal);
        foreach (var movement in (string[])["pictor-draw", "pictor-reveal", "pictor-grow-b", "pictor-scale-area", "pictor-center", "pictor-slide", "pictor-arc", "pictor-edge", "pictor-converge", "pictor-shown", "pictor-late"])
        {
            Assert.Contains(":root:has(.pictor-replay :checked) ." + movement + "{animation-name:", markup, StringComparison.Ordinal);
        }

        Assert.Contains("@keyframes pictor-draw-r{", markup, StringComparison.Ordinal);
        Assert.StartsWith("<style>@media (prefers-reduced-motion:no-preference)", markup[markup.IndexOf("<style>", StringComparison.Ordinal)..], StringComparison.Ordinal);
    }

    private static string Color(Avalonia.Controls.Control chart, (int X, int Y) at)
    {
        var frame = HoverPixels.Frame(chart);
        var i = ((at.Y * (int)chart.Bounds.Width) + at.X) * 4;
        return $"#{frame[i + 2]:x2}{frame[i + 1]:x2}{frame[i]:x2}";
    }
}
