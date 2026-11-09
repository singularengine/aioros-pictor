using System.Text.Json;
using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Tests.Desktop;
using static Aioros.Pictor.Tests.Desktop.DesktopFrames;

namespace Aioros.Pictor.Tests.Charts;

public sealed partial class TreemapStrokeTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "ANIMATION-19")]
    public async Task TreemapRectanglesDrawTheirStrokesInWhenTheUpdateAnimationIsOffAsync()
    {
        var example = await Reference.ExampleAsync("SimpleTreemap");
        var animated = ExampleAnimations.Animated(example.Spec);
        var off = Reference.Pictor(example with { Spec = animated with { Treemap = animated.Treemap! with { UpdateAnimation = false } } });
        var on = Reference.Pictor(example with { Spec = animated });
        var recorded = await RecordedAsync();
        var (partly, whole) = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new ManualClock();
                var (drawn, drawnHost) = Hosted(example, animated: true, clock);
                var (plain, plainHost) = Hosted(example, animated: true, clock);
                ((Treemap)drawn).IsUpdateAnimationActive = false;
                ((Treemap)drawn).IsAnimationActive = true;
                ((Treemap)plain).IsAnimationActive = true;
                drawnHost.Measure(new Avalonia.Size(drawn.Width, drawn.Height));
                drawnHost.Arrange(new Avalonia.Rect(0, 0, drawn.Width, drawn.Height));
                Frame(drawnHost);
                Frame(plainHost);
                clock.Advance(400);
                var early = (White(Frame(drawnHost)), White(Frame(plainHost)));
                clock.Advance(5000);
                var late = (White(Frame(drawnHost)), White(Frame(plainHost)));
                return (early, late);
            },
            TestContext.Current.CancellationToken);

        Assert.Contains(".pictor-stroke{animation:pictor-stroke 1500ms ease 64ms backwards}.pictor-stroke-late{animation:pictor-stroke 1500ms ease calc(var(--pb) + var(--pd) + 176ms) backwards}@keyframes pictor-stroke{from{stroke-dasharray:0 1}to{stroke-dasharray:1 0}}", off, StringComparison.Ordinal);
        Assert.Equal(50, Stroked().Count(off));
        Assert.True(Late().Count(off) > 0, "no deeper node waits for the end");
        Assert.DoesNotContain("pictor-stroke", on, StringComparison.Ordinal);
        Assert.Equal((Motion.Timing.StrokeLag, 50, "1500ms"), recorded);
        Assert.True(partly.Item1 < partly.Item2 * 0.8, $"{partly.Item1} white pixels drawn in at 400 ms, {partly.Item2} with the default");
        Assert.Equal(whole.Item2, whole.Item1);
    }

    /// <summary>When Recharts sets the final dash (ms after the mount), on how many rectangles, and the transition's duration.</summary>
    private static async Task<(double Lag, int Count, string Duration)> RecordedAsync()
    {
        await using var stream = File.OpenRead(Path.Combine(Reference.Root, "tools", "recharts", "out", "motion", "TreemapRectanglesEnter.styles.json"));
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var frames = document.RootElement.GetProperty("frames").EnumerateArray().ToList();
        var set = frames.First(frame => frame.GetProperty("styles")[0].GetString()!.Contains("transition", StringComparison.Ordinal));
        var styles = set.GetProperty("styles").EnumerateArray().Select(style => style.GetString()!).ToList();
        var first = frames[0].GetProperty("styles").EnumerateArray().All(style => style.GetString()!.StartsWith("stroke-dasharray: 0px", StringComparison.Ordinal));
        return (first ? set.GetProperty("time").GetDouble() : -1, styles.Count(style => style.Contains("transition: stroke-dasharray 1500ms", StringComparison.Ordinal)), Duration().Match(styles[0]).Groups[1].Value);
    }

    /// <summary>How many of the treemap's pixels are white (its nodes' strokes) where its root covers the surface while it slides in (from 100 px on both axes; ANIMATION-23).</summary>
    private static int White(byte[] frame)
    {
        var count = 0;
        for (var i = 0; i < frame.Length; i += 4)
        {
            var (x, y) = (i / 4 % 500, i / 4 / 500);
            count += x >= 100 && y >= 100 && frame[i] > 245 && frame[i + 1] > 245 && frame[i + 2] > 245 ? 1 : 0;
        }

        return count;
    }

    [GeneratedRegex(@"class=""recharts-rectangle pictor-stroke"" pathLength=""1""")]
    private static partial Regex Stroked();

    [GeneratedRegex(@"class=""recharts-rectangle pictor-stroke-late"" pathLength=""1""")]
    private static partial Regex Late();

    [GeneratedRegex(@"transition: stroke-dasharray (\d+ms)")]
    private static partial Regex Duration();
}
