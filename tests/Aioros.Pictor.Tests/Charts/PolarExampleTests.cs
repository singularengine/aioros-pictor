using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Charts;

public sealed partial class PolarExampleTests
{
    [Fact]
    [Trait("AC", "POLAR-01")]
    public async Task APieAndALabelledRingMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("TwoLevelPieChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "POLAR-02")]
    public async Task AHalfPieFrom180To0DegreesMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("StraightAnglePieChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "POLAR-04")]
    public async Task PaddingAnglesAndCellsMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("PieChartWithPaddingAngle");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "POLAR-06")]
    public async Task ARadarWithItsGridAndAxesMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("SimpleRadarChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "POLAR-07")]
    public async Task TwoRadarsOverAFixedDomainWithALegendMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("SpecifiedDomainRadarChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "POLAR-08")]
    public async Task RadialBarsWithArcLabelsAndAVerticalLegendMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("SimpleRadialBarChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "POLAR-03")]
    public async Task EverySectorOfBothPiesHasItsHoverStateAsync()
    {
        var example = await Reference.ExampleAsync("TwoSimplePieChart");
        var verdict = await Reference.CompareAsync("TwoSimplePieChart");
        var markup = Reference.Pictor(example);

        Assert.Empty(verdict.Differences);
        Assert.Equal(12, Count(markup, "pictor-hit\""));
        Assert.Equal(12, Count(markup, "<foreignObject"));
        Assert.Contains(">Group D : 9800</li>", markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "POLAR-05")]
    public async Task AnActiveShapeIsDrawnAtItsIndexOrOnHoverAsync()
    {
        var example = await Reference.ExampleAsync("ActiveShapePieChart");
        var verdict = await Reference.CompareAsync("ActiveShapePieChart");
        var polar = example.Spec.Polar!;
        var hovered = example with { Spec = example.Spec with { Polar = polar with { Pies = [polar.Pies[0] with { ActiveIndex = null }] } } };
        var markup = Reference.Pictor(hovered);
        var tips = markup.IndexOf("class=\"pictor-tk", StringComparison.Ordinal);

        Assert.Empty(verdict.Differences);
        Assert.DoesNotContain("recharts-active-shape", markup[..tips], StringComparison.Ordinal);
        Assert.Equal(4, Count(markup[tips..], "recharts-active-shape"));
    }

    [Theory]
    [Trait("AC", "POLAR-09")]
    [InlineData("TwoLevelPieChart")]
    [InlineData("SpecifiedDomainRadarChart")]
    [InlineData("SimpleRadialBarChart")]
    public async Task APolarSpecReadBackFromJsonWritesTheSameChartAsync(string name)
    {
        var example = await Reference.ExampleAsync(name);
        var direct = Reference.Pictor(example);
        var buffer = new ArrayBufferWriter<byte>();
        Slots.Writer(SpecJson.Write(example.Spec), static (_, _) => null!)(example.Data, buffer);
        var polar = example.Spec.Polar!;
        var shared = polar.Pies.IsEmpty ? null : example with
        {
            Spec = example.Spec with { Polar = polar with { Pies = [polar.Pies[0] with { Data = null }] } },
            Data = PolarControls.Rows(polar.Pies[0].Data!),
        };

        Assert.Equal(direct, Encoding.UTF8.GetString(buffer.WrittenSpan));
        if (shared is not null)
        {
            Assert.Equal(Inner(Reference.Pictor(example with { Spec = example.Spec with { Polar = polar with { Pies = [polar.Pies[0]] } } })), Inner(Reference.Pictor(shared)));
        }
    }

    [Fact]
    [Trait("AC", "POLAR-10")]
    public async Task RadarsAndRadialBarsHaveAHoverStatePerCategoryAsync()
    {
        var radar = Reference.Pictor(await Reference.ExampleAsync("SpecifiedDomainRadarChart"));
        var radial = Reference.Pictor(await Reference.ExampleAsync("SimpleRadialBarChart"));
        var plain = Reference.Pictor(await Reference.ExampleAsync("SimpleRadarChart"));

        Assert.Equal(6, Count(radar, "pictor-hit\""));
        Assert.Equal(6, Count(radar, "recharts-curve recharts-tooltip-cursor"));
        Assert.Equal(12, Count(radar, "<circle class=\"pictor-dot\""));
        Assert.Contains("<p>Chinese</p>", radar, StringComparison.Ordinal);
        Assert.Equal(7, Count(radial, "pictor-hit\""));
        Assert.Equal(7, Count(radial, "recharts-sector recharts-tooltip-cursor"));
        Assert.DoesNotContain("pictor-hover", plain, StringComparison.Ordinal);
    }

    private static int Count(string text, string part) => Regex.Count(text, Regex.Escape(part));

    private static string Inner(string markup) => Sectors().Match(markup).Value;

    [GeneratedRegex("<g class=\"recharts-layer recharts-pie\">.*?</g></g>(?=</svg>|<g class=\"recharts-layer recharts-pie\">)")]
    private static partial Regex Sectors();
}
