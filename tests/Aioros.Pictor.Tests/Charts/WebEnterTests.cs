using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Data;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Motion;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Charts;

public sealed partial class WebEnterTests
{
    [Fact]
    [Trait("AC", "ANIMATION-12")]
    public async Task WebArcsGrowEachSectorAsRechartsGrowsItAsync()
    {
        foreach (var name in (string[])["TwoLevelPieChart", "PieChartWithPaddingAngle"])
        {
            await ArcsGrowAsRechartsSectorsAsync(name);
        }
    }

    private static async Task ArcsGrowAsRechartsSectorsAsync(string name)
    {
        var example = await Reference.ExampleAsync(name);
        var spec = Demo.ExampleAnimations.Animated(example.Spec);
        var plot = (PolarLayout)Plots.Compute(spec, ChartData.From(example.Data, static (_, _) => null));
        var markup = Write(plot);
        var arcs = Arc().Matches(markup).Select(match => (A: Number(match, 1), B: Number(match, 2), L: Number(match, 3), X: Number(match, 4), Y: Number(match, 5))).ToList();
        var enter = PolarMorphs.Enter(plot)!;
        var timing = Animations.Of(plot.Pies[0])!.Value;

        foreach (var elapsed in new[] { 700.0, 1100, 1600 })
        {
            var t = timing.Progress(elapsed);
            var frame = (PolarLayout)enter.Frame(elapsed);
            var sectors = frame.Pies.SelectMany(pie => pie.Sectors.Select(sector => (Pie: pie, sector.Shape))).ToList();
            var next = 0;
            foreach (var arc in arcs)
            {
                var (pie, shape) = sectors[next];
                var origin = Math.Atan2(-(arc.Y - pie.Cy), arc.X - pie.Cx) * 180 / Math.PI;
                var sign = pie.Spec.EndAngle >= pie.Spec.StartAngle ? 1 : -1;
                Assert.Equal(Normal(shape.StartAngle), Normal(origin + (sign * (arc.A + (t * (arc.B - arc.A))))), 2);
                var spanned = 0.0;
                while (next < sectors.Count && sectors[next].Pie == pie && spanned < (t * arc.L) - 0.01)
                {
                    spanned += Math.Abs(sectors[next].Shape.EndAngle - sectors[next].Shape.StartAngle);
                    next++;
                }

                Assert.Equal(t * arc.L, spanned, 2);
            }

            Assert.Equal(sectors.Count, next);
        }

        Assert.DoesNotContain("mask-image", markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "ANIMATION-13")]
    public async Task RadarDotsComeFromTheCenterWithTheirPointsAsync()
    {
        var example = await Reference.ExampleAsync("RadarOptions");
        var spec = Demo.ExampleAnimations.Animated(example.Spec);
        var plot = (PolarLayout)Plots.Compute(spec, ChartData.From(example.Data, static (_, _) => null));
        var markup = Write(plot);
        var dots = Dot().Matches(markup).ToList();
        var radar = plot.Radars.First(radar => radar.Spec.Dot);

        Assert.NotEmpty(dots);
        Assert.Equal(plot.Cx - radar.X[0], Number(dots[0], 3), 2);
        Assert.Equal(plot.Cy - radar.Y[0], Number(dots[0], 4), 2);
        Assert.DoesNotContain("recharts-radar-dots pictor-late", markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "ANIMATION-14")]
    public async Task TheWebStartsAndEndsAsRechartsFramesDoAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var plot = Plots.Compute(Demo.ExampleAnimations.Animated(example.Spec), ChartData.From(example.Data, static (_, _) => null));
        var markup = Write(plot);

        Assert.Contains("animation:pictor-grow-y var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards", markup, StringComparison.Ordinal);
        Assert.Contains(".pictor-late{animation:pictor-late 0s calc(var(--pb) + var(--pd) + 112ms) backwards}", markup, StringComparison.Ordinal);
        Assert.Equal(80 + 400 + 32, Animations.End(plot.Spec));
    }

    [Fact]
    [Trait("AC", "UPDATE-16")]
    public async Task OnlyShapesWithoutMorphNumbersTransitionOnTheWebAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var animated = Write(Plots.Compute(Demo.ExampleAnimations.Animated(example.Spec), ChartData.From(example.Data, static (_, _) => null)));
        var still = Write(Plots.Compute(example.Spec, ChartData.From(example.Data, static (_, _) => null)));
        var media = animated[animated.IndexOf("@media (prefers-reduced-motion:no-preference)", StringComparison.Ordinal)..];

        Assert.Contains(".pictor-a :is(.recharts-curve,.recharts-rectangle,.recharts-trapezoid,.recharts-polygon,.recharts-symbols):not([data-aioros-morph]){transition:d var(--pd) var(--pe) calc(var(--pb) + 80ms)}", media, StringComparison.Ordinal);
        Assert.Contains(".pictor-a circle:not([data-aioros-morph]){transition:cx var(--pd) var(--pe) calc(var(--pb) + 80ms),cy var(--pd) var(--pe) calc(var(--pb) + 80ms)}", media, StringComparison.Ordinal);
        Assert.Contains(".pictor-late{transition:visibility 0s 32ms}.pictor-a:has([data-aioros-morphing]) .pictor-late:not(:has([data-aioros-morph])){visibility:hidden;transition:none}[data-aioros-morphing]{transform-box:view-box;transform-origin:0 0}", media, StringComparison.Ordinal);
        Assert.DoesNotContain("transition", still, StringComparison.Ordinal);
    }

    private static string Write(IChartPlot plot)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var canvas = new SvgCanvas(buffer);
        Plots.Paint(ref canvas, plot, ChartPainter.AllTips);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static double Normal(double angle) => ((angle % 360) + 360) % 360;

    private static double Number(Match match, int group) => double.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture);

    [GeneratedRegex("<path class=\"pictor-arc\"[^>]*--a:([-\\d.]+);--b:([-\\d.]+);--l:([-\\d.]+)\" d=\"M([-\\d.]+),([-\\d.]+)")]
    private static partial Regex Arc();

    [GeneratedRegex("<circle r=\"[\\d.]+\"[^>]* cx=\"([-\\d.]+)\" cy=\"([-\\d.]+)\" class=\"recharts-dot recharts-radar-dot pictor-converge\" style=\"--dx:([-\\d.]+)px;--dy:([-\\d.]+)px\"")]
    private static partial Regex Dot();
}
