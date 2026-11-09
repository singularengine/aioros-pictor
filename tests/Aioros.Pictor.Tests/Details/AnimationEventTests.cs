using Aioros.Pictor.Demo;
using Aioros.Pictor.Tests.Desktop;
using Avalonia.Threading;
using static Aioros.Pictor.Tests.Desktop.DesktopFrames;

namespace Aioros.Pictor.Tests.Details;

public sealed class AnimationEventTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "DETAIL-08")]
    public async Task SeriesRaiseTheirAnimationEventsOnTheDesktopAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");

        var seen = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new ManualClock();
                var (chart, host) = Hosted(example, animated: true, clock);
                var log = new List<string>();
                foreach (var (bar, i) in chart.Elements.OfType<Bar>().Select((bar, i) => (bar, i)))
                {
                    bar.AnimationStarted += (_, _) => log.Add($"start {i}");
                    bar.AnimationEnded += (_, _) => log.Add($"end {i}");
                }

                var steps = new List<string>();
                void Step(double advance)
                {
                    clock.Advance(advance);
                    Frame(host);
                    Dispatcher.UIThread.RunJobs();
                    steps.Add(string.Join(",", log.Order()));
                    log.Clear();
                }

                Step(0);
                Step(300);
                Step(200);
                Step(100);
                chart.Data = Reference.Rows(example).Select(row => row.ToDictionary(pair => pair.Key, pair => pair.Value is double value ? (object?)(value * 2) : pair.Value)).ToList();
                Step(0);
                Step(600);

                var (still, stillHost) = Hosted(example, animated: false, clock);
                var reduced = SpecControls.Create(example);
                Chart.SetReduceMotion(reduced, true);
                var quiet = new List<string>();
                foreach (var bar in still.Elements.OfType<Bar>().Concat(reduced.Elements.OfType<Bar>()))
                {
                    bar.AnimationStarted += (_, _) => quiet.Add("start");
                    bar.AnimationEnded += (_, _) => quiet.Add("end");
                }

                Frame(stillHost);
                reduced.Time = clock;
                var reducedHost = new Avalonia.Controls.Border { Child = reduced, Width = 500, Height = 300 };
                reducedHost.Measure(new Avalonia.Size(500, 300));
                reducedHost.Arrange(new Avalonia.Rect(0, 0, 500, 300));
                Frame(reducedHost);
                clock.Advance(1000);
                Frame(stillHost);
                Frame(reducedHost);
                Dispatcher.UIThread.RunJobs();
                return (steps, quiet);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(["start 0,start 1", "", "end 0,end 1", "", "start 0,start 1", "end 0,end 1"], seen.steps);
        Assert.Empty(seen.quiet);
        Assert.DoesNotContain("onanimation", Reference.Pictor(example with { Spec = ExampleAnimations.Animated(example.Spec) }), StringComparison.OrdinalIgnoreCase);
    }
}
