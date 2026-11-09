using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using static Aioros.Pictor.Tests.Desktop.DesktopFrames;

namespace Aioros.Pictor.Tests.Desktop;

public sealed partial class EnterExactTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "ANIMATION-20")]
    public async Task DashedLinesAreDrawnAlongTheirLengthWithTheirDashesAsync()
    {
        var example = await Reference.ExampleAsync("DashedLineChart");
        var markup = Reference.Pictor(example with { Spec = ExampleAnimations.Animated(example.Spec) });
        var masks = Mask().Matches(markup).ToList();
        var (rightmost, limit) = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new ManualClock();
                var (chart, host) = Hosted(example, animated: true, clock);
                var layout = (ChartLayout)chart.Plot();
                Frame(host);
                clock.Advance(80 + 750);
                var frame = Frame(host);
                var right = Enumerable.Range(0, 500).Where(x => Ink(frame, x) > 0).DefaultIfEmpty(-1).Max();
                return (right, layout.Left + (layout.PlotWidth * Motion.Easings.Ease(Specs.AnimationEasing.Ease, 0.5)));
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(2, masks.Count);
        Assert.All(masks, match => Assert.Equal(match.Groups[2].Value, match.Groups[4].Value));
        Assert.All(masks, match => Assert.Equal(match.Groups[1].Value, match.Groups[3].Value));
        Assert.Contains(".pictor-dash-mask{stroke-dasharray:1 1;animation:pictor-dash var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("pictor-reveal", markup, StringComparison.Ordinal);
        Assert.InRange(rightmost, 1, limit - 10);
    }

    [Fact]
    [Trait("AC", "ANIMATION-21")]
    public async Task ScatterSymbolsGrowByAreaThroughARegisteredPropertyAsync()
    {
        var example = await Reference.ExampleAsync("SimpleScatterChart");
        var markup = Reference.Pictor(example with { Spec = ExampleAnimations.Animated(example.Spec) });

        Assert.Contains("@property --pictor-s{syntax:'&lt;number&gt;';inherits:false;initial-value:1}", markup, StringComparison.Ordinal);
        Assert.Contains(".pictor-scale-area{transform-box:fill-box;transform-origin:50% 50%;transform:scale(sqrt(var(--pictor-s)));animation:pictor-scale-area var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-scale-area{from{--pictor-s:0}to{--pictor-s:1}}", markup, StringComparison.Ordinal);
        Assert.Contains("pictor-scale-area", markup[markup.IndexOf("recharts-scatter-symbol", StringComparison.Ordinal)..], StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "ANIMATION-22")]
    public async Task RoundedBarsAreDrawnAgainAtEachHeightAsync()
    {
        var example = await Reference.ExampleAsync("GapsBarBackgroundChart");
        var (corner, inside) = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new ManualClock();
                var (chart, host) = Hosted(example, animated: true, clock);
                var bar = ((ChartLayout)chart.Plot()).Series[0].Bars[2];
                Frame(host);
                var elapsed = 80 + Enumerable.Range(0, 400).First(t => Motion.Easings.Ease(Specs.AnimationEasing.Ease, t / 400.0) * bar.Height >= 40);
                clock.Advance(elapsed);
                var frame = Frame(host);
                var p = Motion.Easings.Ease(Specs.AnimationEasing.Ease, (elapsed - 80) / 400.0);
                var top = bar.Y + bar.Height - (bar.Height * p);
                return (Pixel(frame, (int)Math.Floor(bar.X + 2.5), (int)Math.Floor(top + 1.5)), Pixel(frame, (int)(bar.X + (bar.Width / 2)), (int)(top + 5)));
            },
            TestContext.Current.CancellationToken);

        Assert.True(IsBar(inside), $"the bar's middle {inside} is the bar's purple");
        Assert.False(IsBar(corner), $"{corner} at the bar's top corner, inside a squashed corner, outside the true one");
    }

    private static bool IsBar((byte B, byte G, byte R) pixel) => Math.Abs(pixel.B - 216) < 40 && Math.Abs(pixel.G - 132) < 40 && Math.Abs(pixel.R - 136) < 40;

    [GeneratedRegex(@"<mask id=""(pictor-dm\d+)"" maskUnits=""userSpaceOnUse"" x=""0"" y=""0"" width=""100%"" height=""100%""><path class=""pictor-dash-mask"" pathLength=""1"" fill=""none"" stroke=""#fff"" stroke-width=""[\d.]+"" d=""([^""]+)""/></mask><path [^>]*stroke-dasharray=""[^""]+""[^>]* mask=""url\(#(pictor-dm\d+)\)""[^>]* d=""([^""]+)""")]
    private static partial Regex Mask();
}
