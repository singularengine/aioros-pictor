using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Motion;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Charts;

public sealed partial class WebRestTests
{
    [Fact]
    [Trait("AC", "ANIMATION-16")]
    public void MovementsKeepTheirEffectOnlyWhileTheyRun()
    {
        var css = new StringBuilder(Encoding.UTF8.GetString(SvgMotion.Base));
        foreach (var kind in Enum.GetValues<StepKind>())
        {
            css.Append(Encoding.UTF8.GetString(SvgMotion.Rules(kind)));
        }

        var buffer = new ArrayBufferWriter<byte>();
        ReplaySlotWriter.Create("Replay", static (_, _) => static _ => null)(null, buffer);
        css.Append(Encoding.UTF8.GetString(buffer.WrittenSpan));
        var rules = css.ToString();
        var shorthands = Shorthand().Matches(rules).Select(match => match.Groups[1].Value).ToList();

        Assert.True(shorthands.Count >= 14);
        Assert.All(shorthands, shorthand => Assert.DoesNotMatch(@"\b(both|forwards|infinite)\b", shorthand));
        Assert.Contains("animation:pictor-arc var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}", rules, StringComparison.Ordinal);
        Assert.Contains("animation:pictor-edge var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}", rules, StringComparison.Ordinal);
        Assert.Contains("<style>.pictor-shown{visibility:hidden}@media (prefers-reduced-motion:no-preference)", rules, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "ANIMATION-17")]
    public async Task APieGrowsByAsFewArcsAsItsColorsNeedWithItsEdgesAboveAsync()
    {
        var rings = await AnimatedAsync("TwoLevelPieChart", toggles: false);
        var padded = await AnimatedAsync("PieChartWithPaddingAngle", toggles: false);
        var toggled = await AnimatedAsync("TwoLevelPieChart", toggles: true);

        Assert.Equal([1, 1], Groups(rings).Select(group => Count(group, "<path class=\"pictor-arc\"")));
        Assert.Equal([5, 12], Groups(rings).Select(group => Count(group, "<line class=\"pictor-edge\"")));
        Assert.All(Groups(rings), group => Assert.Equal(Count(group, "<line class=\"pictor-edge\""), Distinct(group)));
        Assert.All(Groups(rings).Concat(Groups(padded)).Concat(Groups(toggled)), group => Assert.DoesNotMatch("pictor-edge.*pictor-arc", group));
        Assert.All(Groups(padded), group => Assert.Equal(2 * Count(group, "<path class=\"pictor-arc\""), Count(group, "<line class=\"pictor-edge\"")));
        Assert.Equal(15, Count(toggled, "\"><path class=\"pictor-arc\""));
        Assert.Equal(15, Toggled().Count(toggled));
    }

    [Fact]
    [Trait("AC", "ANIMATION-18")]
    public async Task APiesMovementsLiveInsideItsOwnMarkupAsync()
    {
        var markup = await AnimatedAsync("TwoLevelPieChart", toggles: false);
        var start = markup.IndexOf("<svg", StringComparison.Ordinal);
        var end = markup.LastIndexOf("</svg>", StringComparison.Ordinal);
        var styles = Style().Matches(markup).Select(match => match.Index).ToList();

        Assert.NotEmpty(styles);
        Assert.All(styles, at => Assert.InRange(at, start, end));
        Assert.Contains("@keyframes pictor-arc", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("@property", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("will-change", markup, StringComparison.Ordinal);
    }

    private static async Task<string> AnimatedAsync(string name, bool toggles)
    {
        var example = await Reference.ExampleAsync(name);
        var spec = Demo.ExampleAnimations.Animated(example.Spec);
        spec = toggles ? spec with { Legend = new LegendSpec { Toggles = true } } : spec;
        return Reference.Pictor(example with { Spec = spec });
    }

    private static IEnumerable<string> Groups(string markup) => Shown().Matches(markup).Select(match => match.Groups[1].Value);

    private static int Distinct(string group) => Edge().Matches(group).Select(match => match.Groups[1].Value).Distinct().Count();

    private static int Count(string markup, string text) => markup.Split(text).Length - 1;

    [GeneratedRegex(@"animation:([^;}""]*)")]
    private static partial Regex Shorthand();

    [GeneratedRegex(@"<g class=""pictor-shown"">((?:(?!recharts-pie-sector).)*)", RegexOptions.Singleline)]
    private static partial Regex Shown();

    [GeneratedRegex(@"<line class=""pictor-edge""[^>]*--r:([-\d.]+)deg")]
    private static partial Regex Edge();

    [GeneratedRegex(@"<g class=""pictor-s\d+""><line class=""pictor-edge""[^>]*/><line class=""pictor-edge""")]
    private static partial Regex Toggled();

    [GeneratedRegex("<style")]
    private static partial Regex Style();
}
