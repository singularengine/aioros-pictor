using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Data;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;
using Aioros.Pictor.Tests.Desktop;

namespace Aioros.Pictor.Tests.Charts;

public sealed partial class RangeTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "RANGE-01")]
    public async Task ARangeAreaRunsBetweenItsLowerAndUpperEndsAsync()
    {
        var verdict = await Reference.CompareAsync("RangeAreaChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "RANGE-02")]
    public async Task AVerticalRangeAreaMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("VerticalRangeAreaChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "RANGE-03")]
    public async Task FloatingBarsSpanTheirRangesAsync()
    {
        var verdict = await Reference.CompareAsync("RangeBarChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "RANGE-04")]
    public async Task AreasFillToTheirBaseValueAsync()
    {
        var verdict = await Reference.CompareAsync("AreaBaseValueChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "RANGE-05")]
    public async Task ErrorPairsDrawAsymmetricWhiskersAsync()
    {
        var verdict = await Reference.CompareAsync("ErrorBarPairsChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "RANGE-06")]
    public async Task ARangeRadarFillsTheBandBetweenItsPolygonsAsync()
    {
        var verdict = await Reference.CompareAsync("RangeRadarChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "RANGE-07")]
    public async Task RangeTooltipsShowBothEndsAsync()
    {
        var areaExample = await Reference.ExampleAsync("RangeAreaChart");
        var area = Reference.Pictor(areaExample);
        var bar = Reference.Pictor(await Reference.ExampleAsync("RangeBarChart"));
        var radar = Reference.Pictor(Tooltipped(await Reference.ExampleAsync("RangeRadarChart")));

        var areaTip = Demo.HoverComparison.PictorTip(area, 0);
        var barTip = Demo.HoverComparison.PictorTip(bar, 8);
        var radarTip = Demo.HoverComparison.PictorTip(radar, 0);
        var dots = Regex.Matches(Demo.HoverComparison.Group(area, "pictor-tips", 0), "<circle[^>]* cy=\"([^\"]+)\"").Select(dot => double.Parse(dot.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        var layout = (Layout.ChartLayout)Aioros.Pictor.Painting.Plots.Compute(areaExample.Spec, Aioros.Pictor.Data.ChartData.From(areaExample.Data, static (_, _) => null));

        Assert.Equal([("temperature : -1 ~ 10", areaTip.Items[0].Color)], areaTip.Items);
        Assert.Equal([("temperature : -3 ~ 5", barTip.Items[0].Color)], barTip.Items);
        Assert.Equal("Mike : 60 ~ 120", radarTip.Items[0].Text);
        Assert.Equal(18, Regex.Count(Tips(area), "<circle"));
        Assert.Equal(2, dots.Count);
        Assert.Equal([layout.YAxes[0].Map(10), layout.YAxes[0].Map(-1)], dots.Order(), (a, b) => Math.Abs(a - b) < 0.001);
    }

    [Fact]
    [Trait("AC", "RANGE-08")]
    public async Task PairsAreReadFromArraysListsTuplesAndTablesAsync()
    {
        var example = await Reference.ExampleAsync("RangeAreaChart");
        var rows = Reference.Rows(example);
        var expected = Reference.Pictor(example);
        var pairs = rows.Select(row => ((List<object?>)row["temperature"]!).Select(value => (double)value!).ToArray()).ToList();
        object?[] arrays = [.. rows.Select((row, i) => new ArrayDay((string)row["day"]!, pairs[i]))];
        object?[] lists = [.. rows.Select((row, i) => new ListDay((string)row["day"]!, [.. pairs[i]]))];
        object?[] tuples = [.. rows.Select((row, i) => new TupleDay((string)row["day"]!, (pairs[i][0], pairs[i][1])))];
        var table = DataTable.Read(rows, ["day", "temperature"], static (_, _) => null);
        var own = example.Spec with { Series = [example.Spec.Series[0] with { Data = table }] };

        Assert.Equal(expected, Write(example.Spec, arrays));
        Assert.Equal(expected, Write(example.Spec, lists));
        Assert.Equal(expected, Write(example.Spec, tuples));
        Assert.Equal("-1,10", table.Columns["temperature"][0]);
        Assert.Empty(MarkupComparison.Compare(await Reference.RechartsAsync("RangeAreaChart"), Write(own, rows)).Differences);
    }

    [Fact]
    [Trait("AC", "RANGE-09")]
    public async Task BaseValuesAndRangesAreSetInXamlAsync()
    {
        Assert.Empty((await ReferenceDesktop.CompareXamlAsync(desktop, "AreaBaseValueChart")).Differences);
        Assert.Empty((await ReferenceDesktop.CompareXamlAsync(desktop, "RangeAreaChart")).Differences);
    }

    [Fact]
    [Trait("AC", "RANGE-10")]
    public async Task TheDesktopDrawsRangesAsRechartsDoesAsync()
    {
        var area = await ReferenceDesktop.MeasureAsync(desktop, "RangeAreaChart");
        var radar = await ReferenceDesktop.MeasureAsync(desktop, "RangeRadarChart");

        PixelVerdict.Within(area);
        PixelVerdict.Within(radar);
    }

    private static RechartsExample Tooltipped(RechartsExample example) => example with { Spec = example.Spec with { Tooltip = new TooltipSpec() } };

    private static string Write(ChartSpec spec, object? data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, data, static (type, member) => type.GetProperty(char.ToUpperInvariant(member[0]) + member[1..]) is { } property ? property.GetValue : null);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string Tips(string markup) => string.Concat(Reference.States(markup, "pictor-tips"));

    private sealed record ArrayDay(string Day, double[] Temperature);

    private sealed record ListDay(string Day, List<double> Temperature);

    private sealed record TupleDay(string Day, (double Low, double High) Temperature);
}

public sealed class StackTests
{
    [Fact]
    [Trait("AC", "RANGE-11")]
    public async Task ErrorBarsStandOnTheTopOfStackedBarsAsync()
    {
        var verdict = await Reference.CompareAsync("StackedErrorBarChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "RANGE-12")]
    public async Task ASeriesWithItsOwnDataInAStackMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("StackedOwnDataAreaChart");
        var example = await Reference.ExampleAsync("StackedOwnDataAreaChart");
        var rows = Enumerable.Range(0, 9).Select(i => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { ["name"] = $"Day {i}", ["pv"] = 100.0 * i }).ToList();
        var longer = example.Spec with { Series = [example.Spec.Series[0], example.Spec.Series[1] with { Data = Specs.DataTable.Read(rows, ["name", "pv"], static (_, _) => null) }] };
        var layout = Layout.ChartLayout.Compute(longer, ChartData.From(example.Data, static (_, _) => null));

        Assert.Empty(verdict.Differences);
        Assert.Equal([true, true, true, true, true, true, true, false, false], layout.Series[1].Defined);
    }
}

public sealed class RangeOutputTests
{
    [Fact]
    [Trait("AC", "RANGE-13")]
    public async Task AChangedLowerEndIsNotServedFromTheKeptMarkupAsync()
    {
        var example = await Reference.ExampleAsync("RangeAreaChart");
        var rows = Reference.Rows(example);
        var output = new ChartOutput(example.Spec, static (_, _) => null);
        List<IReadOnlyDictionary<string, object?>> lowered = [.. rows.Select((row, i) => i == 3 ? new Dictionary<string, object?>(row) { ["temperature"] = new List<object?> { 1.0, 12.0 } } : row)];

        var first = Write(output, [.. rows]);
        Write(output, [.. rows]);
        Write(output, [.. rows]);
        var kept = output.Renders;
        var changed = Write(output, lowered);

        Assert.Equal(2, kept);
        Assert.Equal(3, output.Renders);
        Assert.NotEqual(first, changed);
    }

    private static string Write(ChartOutput output, List<IReadOnlyDictionary<string, object?>> rows)
    {
        var buffer = new ArrayBufferWriter<byte>();
        output.Write(rows, buffer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
