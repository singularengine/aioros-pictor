using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Charts;

public sealed class LegendToggleTests
{
    [Fact]
    [Trait("AC", "LEGEND-01")]
    public async Task LegendItemsToggleTheirSeriesWithCheckboxesAndCssAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");
        var spec = example.Spec with { Legend = example.Spec.Legend! with { Toggles = true } };

        var markup = Write(spec, example.Data);
        var plain = Reference.Pictor(example);

        Assert.Equal(2, Regex.Count(markup, "<label><input type=\"checkbox\" hidden class=\"pictor-t[01]\">"));
        Assert.Contains("<li class=\"recharts-legend-item legend-item-1\" style=\"display:inline-block;margin-right:10px\"><label><input type=\"checkbox\" hidden class=\"pictor-t1\"><svg", markup, StringComparison.Ordinal);
        Assert.Contains("<g class=\"pictor-s0\"><g class=\"recharts-layer recharts-line\">", markup, StringComparison.Ordinal);
        Assert.Contains("<g class=\"pictor-s1\"><g class=\"recharts-layer recharts-line\">", markup, StringComparison.Ordinal);
        Assert.Equal(7, Regex.Count(markup, "<li class=\"pictor-s1\" style=\"color:#82ca9d\">uv : "));
        Assert.Equal(7, Regex.Count(markup, "<g class=\"pictor-s0\"><circle class=\"pictor-dot\" r=\"8\""));
        Assert.Contains(".recharts-wrapper:has(.pictor-t0:checked) .pictor-s0,.recharts-wrapper:has(.pictor-t1:checked) .pictor-s1{visibility:hidden}.recharts-wrapper:has(.pictor-t0:checked) li.pictor-s0,.recharts-wrapper:has(.pictor-t1:checked) li.pictor-s1{display:none}", markup, StringComparison.Ordinal);
        Assert.Contains(".recharts-legend-item:has(:checked) .recharts-legend-item-text{color:var(--pictor-inactive,#ccc)!important}", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("checkbox", plain, StringComparison.Ordinal);
        Assert.DoesNotContain("pictor-s", plain, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "LEGEND-03")]
    public async Task PieSectorsAndRadarsAreToggledByTheirLegendEntriesAsync()
    {
        var pie = await Reference.ExampleAsync("TwoLevelPieChart");
        var radar = await Reference.ExampleAsync("SpecifiedDomainRadarChart");

        var pies = Write(pie.Spec with { Legend = new LegendSpec { Toggles = true }, Tooltip = new TooltipSpec() }, pie.Data);
        var radars = Write(radar.Spec with { Legend = radar.Spec.Legend! with { Toggles = true } }, radar.Data);
        var sectors = Regex.Matches(pies, "<g class=\"(pictor-s\\d+)\"><g class=\"recharts-layer recharts-pie-sector\" tabindex=\"-1\">").Select(match => match.Groups[1].Value).ToList();
        var boxes = Regex.Count(pies, "<input type=\"checkbox\" hidden class=\"pictor-t");

        Assert.Equal(sectors.Count, boxes);
        Assert.Equal(sectors.Count, sectors.Distinct().Count());
        Assert.Contains("pictor-s0", sectors);
        Assert.Equal(2, Regex.Count(radars, "<g class=\"pictor-s[01]\"><g class=\"recharts-layer recharts-radar\">"));
        Assert.Equal(2, Regex.Count(radars, "<input type=\"checkbox\" hidden class=\"pictor-t[01]\">"));
        Assert.Contains("<li class=\"pictor-s1\" style=\"color:", radars, StringComparison.Ordinal);
    }

    private static string Write(ChartSpec spec, object? data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, data, static (_, _) => null);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
