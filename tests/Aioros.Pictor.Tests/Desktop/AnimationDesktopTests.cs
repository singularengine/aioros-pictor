using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Avalonia;
using Avalonia.Controls;

using static Aioros.Pictor.Tests.Desktop.DesktopFrames;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class AnimationDesktopTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "ANIMATION-05")]
    public async Task BarsGrowFromTheirBaseOnTheDesktopAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");

        var (start, middle, height, end, still) = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new ManualClock();
                var (chart, host) = Hosted(example, animated: true, clock);
                var bar = ((ChartLayout)chart.Plot()).Series[0].Bars[0];
                var column = (int)(bar.X + (bar.Width / 2));
                var first = Ink(Frame(host), column);
                clock.Advance(280);
                var half = Ink(Frame(host), column);
                clock.Advance(232);
                var last = Frame(host);
                return (first, half, bar.Height, last, Frame(Hosted(example, animated: false, clock).Host));
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(0, start);
        Assert.InRange(middle, (0.8024 * height) - 2, (0.8024 * height) + 2);
        Assert.Equal(still, end);
    }

    [Fact]
    [Trait("AC", "ANIMATION-06")]
    public async Task LinesAreDrawnAlongTheirLengthAndDotsWaitOnTheDesktopAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");

        var (moving, end, still, dot, last) = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new ManualClock();
                var (chart, host) = Hosted(example, animated: true, clock);
                var series = ((ChartLayout)chart.Plot()).Series[0];
                Frame(host);
                clock.Advance(830);
                var frame = Frame(host);
                clock.Advance(782);
                return (frame, Frame(host), Frame(Hosted(example, animated: false, clock).Host), ((int)series.X[0], (int)series.Y[0]), ((int)series.X[^1], (int)series.Y[^1]));
            },
            TestContext.Current.CancellationToken);

        Assert.True(IsLine(Pixel(moving, dot.Item1, dot.Item2)), "the first point's dot is not drawn yet: the line shows through");
        Assert.False(IsLine(Pixel(still, dot.Item1, dot.Item2)), "the static chart draws the dot's white fill over the line");
        Assert.DoesNotContain(Enumerable.Range(-4, 9), dy => IsLine(Pixel(moving, last.Item1, last.Item2 + dy)));
        Assert.Contains(Enumerable.Range(-4, 9), dy => IsLine(Pixel(still, last.Item1, last.Item2 + dy)));
        Assert.Equal(still, end);
    }

    [Fact]
    [Trait("AC", "ANIMATION-07")]
    public async Task ReducedMotionDrawsTheFinalChartAtOnceAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");

        var (reduced, still) = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new ManualClock();
                var (_, host) = Hosted(example, animated: true, clock);
                Chart.SetReduceMotion(host, value: true);
                return (Frame(host), Frame(Hosted(example, animated: false, clock).Host));
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(still, reduced);
    }

    [Fact]
    [Trait("AC", "ANIMATION-08")]
    public async Task NewDataWithTheSameValuesDoesNotPlayTheEnterAnimationAgainAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");

        var (played, replayed) = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new ManualClock();
                var (chart, host) = Hosted(example, animated: true, clock);
                var bar = ((ChartLayout)chart.Plot()).Series[0].Bars[0];
                var column = (int)(bar.X + (bar.Width / 2));
                Frame(host);
                clock.Advance(1000);
                var done = Ink(Frame(host), column);
                chart.Data = Reference.Rows(example).ToList();
                return (done, Ink(Frame(host), column));
            },
            TestContext.Current.CancellationToken);

        Assert.True(played > 0);
        Assert.Equal(played, replayed);
    }

    private static bool IsLine((byte B, byte G, byte R) pixel) => pixel.B > 180 && pixel.R < 200 && pixel.G < 200;
}
