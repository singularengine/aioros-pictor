using System.Globalization;
using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Tests.Updates;

public sealed partial class WebMorphTests
{
    [Fact]
    [Trait("AC", "UPDATE-18")]
    public async Task LinesDeclareTheirPointsMappedByProportionalIndexAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");
        var markup = Animated(example);
        var linear = Animated(example with { Spec = example.Spec with { Series = [example.Spec.Series[0] with { Curve = CurveType.Linear }, example.Spec.Series[1] with { Curve = CurveType.Natural }] } });

        var layers = Layers().Matches(markup).Select(match => match.Value).ToList();
        var curves = Elements(markup, "recharts-line-curve");
        var dots = Dots().Matches(markup).Select(match => (X: Number(match.Groups[1].Value), Y: Number(match.Groups[2].Value))).ToList();

        Assert.Equal(2, layers.Count);
        Assert.All(layers, layer => Assert.Contains("data-aioros-morph-path=\"line monotone-x\" data-aioros-morph-map=\"ratio\" data-aioros-morph-duration=\"1500\" data-aioros-morph-delay=\"80\"", layer, StringComparison.Ordinal));
        Assert.Equal(dots.Take(7).SelectMany(dot => new[] { dot.X, dot.Y }), Values(curves[0]));
        Assert.Equal("780 121.5", Attribute(curves[0], "data-aioros-morph-from"));
        Assert.Matches("^[0-9a-f]{8}$", Attribute(curves[0], "data-aioros-morph-key"));
        Assert.Null(Attribute(curves[0], "data-aioros-morph-path"));
        Assert.Contains("recharts-line pictor-a\" data-aioros-morph-path=\"line\" data-aioros-morph-map=\"ratio\"", linear, StringComparison.Ordinal);
        Assert.Single(Elements(linear, "recharts-line-curve"), curve => Attribute(curve, "data-aioros-morph") is null);
    }

    [Fact]
    [Trait("AC", "UPDATE-19")]
    public async Task AreasDeclareTheirPointsAndBasePointsAsABandAsync()
    {
        var simple = Animated(await Reference.ExampleAsync("SimpleAreaChart"));
        var stacked = Animated(await Reference.ExampleAsync("StackedAreaChart"));

        var band = Values(Elements(simple, "recharts-area-area")[0]);
        var stroke = Elements(simple, "recharts-area-curve")[0];
        var bands = Elements(stacked, "recharts-area-area").Select(Values).ToList();

        Assert.Contains("recharts-area pictor-a\" data-aioros-morph-path=\"band monotone-x\" data-aioros-morph-map=\"ratio\"", simple, StringComparison.Ordinal);
        Assert.Equal(7 * 4, band.Length);
        Assert.All(Enumerable.Range(0, 7), i => Assert.Equal(band[i * 4], band[(i * 4) + 2]));
        Assert.Equal("line monotone-x", Attribute(stroke, "data-aioros-morph-path"));
        Assert.Equal(Enumerable.Range(0, 7).SelectMany(i => new[] { band[i * 4], band[(i * 4) + 1] }), Values(stroke));
        Assert.Equal(Enumerable.Range(0, 7).Select(i => bands[0][(i * 4) + 1]), Enumerable.Range(0, 7).Select(i => bands[1][(i * 4) + 3]));
    }

    [Fact]
    [Trait("AC", "UPDATE-20")]
    public async Task BarsDeclareTheirRectanglesAndTheEdgeTheyGrowFromAsync()
    {
        var simple = await Reference.ExampleAsync("SimpleBarChart");
        var markup = Animated(simple);
        var vertical = Animated(await Reference.ExampleAsync("VerticalBarChart"));
        var rounded = Animated(simple with { Spec = simple.Spec with { Series = [.. simple.Spec.Series.Select(series => series with { Radius = [4] })] } });

        var bars = Moving(markup, "recharts-rectangle");
        var verticalBars = Moving(vertical, "recharts-rectangle");

        Assert.Contains("recharts-bar pictor-a\" style=\"--pb:0ms;--pd:400ms;--pe:ease;\" data-aioros-morph-path=\"rect\" data-aioros-morph-duration=\"400\" data-aioros-morph-delay=\"80\"", markup, StringComparison.Ordinal);
        Assert.Equal(14, bars.Count);
        Assert.All(bars, bar =>
        {
            var match = Rect().Match(Attribute(bar, "d")!);
            var (x, y, width, height) = (Number(match.Groups[1].Value), Number(match.Groups[2].Value), Number(match.Groups[3].Value), Number(match.Groups[4].Value));
            Assert.Equal([x, y, width, height], Values(bar));
            Assert.Equal("_ " + (y + height).ToString(CultureInfo.InvariantCulture) + " _ 0", Attribute(bar, "data-aioros-morph-from"));
        });
        Assert.All(verticalBars, bar => Assert.Equal("_ _ 0 _", Attribute(bar, "data-aioros-morph-from")));
        Assert.DoesNotContain("data-aioros-morph=\"", rounded, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "UPDATE-21")]
    public async Task ScatterSymbolsDeclareTheirCentersAndAreasAsync()
    {
        var markup = Animated(await Reference.ExampleAsync("SimpleScatterChart"));

        var symbols = Moving(markup, "recharts-symbols");

        Assert.Contains("recharts-scatter pictor-a\" style=\"--pb:0ms;--pd:400ms;--pe:linear;\" data-aioros-morph-path=\"glyph\" data-aioros-morph-duration=\"400\" data-aioros-morph-delay=\"80\" data-aioros-morph-easing=\"linear\"", markup, StringComparison.Ordinal);
        Assert.Equal(6, symbols.Count);
        Assert.All(symbols, symbol =>
        {
            Assert.Equal(3, Values(symbol).Length);
            Assert.Equal(64, Values(symbol)[2]);
            Assert.Equal("_ _ 0", Attribute(symbol, "data-aioros-morph-from"));
        });
    }

    [Fact]
    [Trait("AC", "UPDATE-22")]
    public async Task SectorsDeclareTheirAnglesAndWhereNewOnesSweepFromAsync()
    {
        var pies = Animated(await Reference.ExampleAsync("TwoSimplePieChart"));
        var radial = Animated(await Reference.ExampleAsync("SimpleRadialBarChart"));

        var sectors = Moving(pies, "recharts-sector");
        var bars = Moving(radial, "recharts-radial-bar-sector");

        Assert.Contains("recharts-pie pictor-a\" style=\"--pb:400ms;--pd:1500ms;--pe:ease;\" data-aioros-morph-path=\"sector\" data-aioros-morph-duration=\"1500\" data-aioros-morph-delay=\"480\"", pies, StringComparison.Ordinal);
        Assert.Equal(12, sectors.Count);
        Assert.Equal([205, 200, 0, 80, 0, 86.383], Values(sectors[0]));
        Assert.All(sectors, sector => Assert.Equal("_ _ _ _ 360 360", Attribute(sector, "data-aioros-morph-from")));
        Assert.Equal(40, Values(sectors[6])[2]);
        Assert.Equal(7, bars.Count);
        Assert.All(bars, bar =>
        {
            var values = Values(bar);
            Assert.Equal("_ _ _ _ " + values[4].ToString(CultureInfo.InvariantCulture) + " " + values[4].ToString(CultureInfo.InvariantCulture), Attribute(bar, "data-aioros-morph-from"));
        });
        Assert.DoesNotContain("data-aioros-morph", string.Concat(Elements(radial, "recharts-radial-bar-background-sector")), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "UPDATE-23")]
    public async Task RadarsAndFunnelsDeclareTheirCornersAsync()
    {
        var radar = Animated(await Reference.ExampleAsync("SimpleRadarChart"));
        var range = Animated(await Reference.ExampleAsync("RangeRadarChart"));
        var funnel = Animated(await Reference.ExampleAsync("FunnelChart"));

        var polygon = Moving(radar, "recharts-polygon")[0];
        var trapezoids = Moving(funnel, "recharts-trapezoid");

        Assert.Contains("recharts-radar pictor-a\" data-aioros-morph-path=\"polygon\" data-aioros-morph-map=\"ratio\" data-aioros-morph-duration=\"1500\" data-aioros-morph-delay=\"80\"", radar, StringComparison.Ordinal);
        Assert.Equal(12, Values(polygon).Length);
        Assert.Equal("250 200", Attribute(polygon, "data-aioros-morph-from"));
        Assert.DoesNotContain("data-aioros-morph=\"", range, StringComparison.Ordinal);
        Assert.Equal([25, 0, 615, 0, 556, 86.4, 84, 86.4], Values(trapezoids[0]));
        Assert.Equal("320 43.2", Attribute(trapezoids[0], "data-aioros-morph-from"));
        Assert.Contains("recharts-trapezoids pictor-a\" style=\"--pb:400ms;--pd:1500ms;--pe:ease;\" data-aioros-morph-path=\"polygon\" data-aioros-morph-duration=\"1500\" data-aioros-morph-delay=\"480\"", funnel, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "UPDATE-24")]
    public async Task ASeriesKeyFollowsItsItemsAndLatePartsWaitForTheMorphAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");
        var markup = Animated(example);
        var rows = ((List<object?>)example.Data!).Cast<IReadOnlyDictionary<string, object?>>().Select(row => new Dictionary<string, object?>(row, StringComparer.Ordinal)).ToList();
        rows[1]["pv"] = 1399.0;
        var changed = Animated(example with { Data = rows.Select(object? (row) => (IReadOnlyDictionary<string, object?>)row).ToList() });
        var still = Reference.Pictor(example);

        var keys = Elements(markup, "recharts-line-curve").Select(curve => Attribute(curve, "data-aioros-morph-key")).ToList();
        var uv = (Elements(markup, "recharts-line-curve")[1], Elements(changed, "recharts-line-curve")[1]);

        Assert.Equal(keys[0], Attribute(Elements(Animated(example), "recharts-line-curve")[0], "data-aioros-morph-key"));
        Assert.NotEqual(keys[0], Attribute(Elements(changed, "recharts-line-curve")[0], "data-aioros-morph-key"));
        Assert.Equal(Attribute(uv.Item1, "data-aioros-morph"), Attribute(uv.Item2, "data-aioros-morph"));
        Assert.Contains(".pictor-late{transition:visibility 0s 32ms}.pictor-a:has([data-aioros-morphing]) .pictor-late:not(:has([data-aioros-morph])){visibility:hidden;transition:none}", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("data-aioros-morph", still, StringComparison.Ordinal);
    }

    private static string Animated(RechartsExample example) => Reference.Pictor(example with { Spec = ExampleAnimations.Animated(example.Spec) });

    private static List<string> Elements(string markup, string type) =>
        [.. Element().Matches(markup).Select(match => match.Value).Where(element => Attribute(element, "class") is { } classes && classes.Split(' ').Contains(type))];

    private static List<string> Moving(string markup, string type) => [.. Elements(markup, type).Where(element => Attribute(element, "data-aioros-morph") is not null)];

    private static string? Attribute(string element, string name)
    {
        var match = Regex.Match(element, " " + Regex.Escape(name) + "=\"([^\"]*)\"");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static double[] Values(string element) => [.. (Attribute(element, "data-aioros-morph") ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Number)];

    private static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    [GeneratedRegex("<(path|circle)\\b[^>]*>")]
    private static partial Regex Element();

    [GeneratedRegex("<g class=\"recharts-layer recharts-line pictor-a\"[^>]*>")]
    private static partial Regex Layers();

    [GeneratedRegex("<circle r=\"3\"[^>]* cx=\"([-\\d.]+)\" cy=\"([-\\d.]+)\" class=\"recharts-dot recharts-line-dot\"")]
    private static partial Regex Dots();

    [GeneratedRegex("^M([-\\d.]+),([-\\d.]+)h([-\\d.]+)v([-\\d.]+)")]
    private static partial Regex Rect();
}
