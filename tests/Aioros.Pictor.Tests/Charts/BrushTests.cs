using Aioros.Pictor.Data;
using Aioros.Pictor.Layout;

namespace Aioros.Pictor.Tests.Charts;

public sealed class BrushTests
{
    [Fact]
    [Trait("AC", "BRUSH-01")]
    public async Task ABrushUnderTheXAxisMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("BrushBarChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "BRUSH-02")]
    public async Task TheChartShowsTheBrushWindowAsRechartsDoesAsync()
    {
        var example = await Reference.ExampleAsync("BrushWindowBarChart");
        var verdict = await Reference.CompareAsync("BrushWindowBarChart");
        var layout = ChartLayout.Compute(example.Spec, ChartData.From(example.Data, static (_, _) => null));

        Assert.Empty(verdict.Differences);
        Assert.Equal(11, layout.Data.Count);
        Assert.Equal(39, layout.Brush!.Count);
        Assert.Equal((5, 15), (layout.Brush.StartIndex, layout.Brush.EndIndex));
    }

    [Fact]
    [Trait("AC", "BRUSH-03")]
    public async Task AWindowOutsideTheDataIsClampedAndAReversedOneOrderedAsync()
    {
        var example = await Reference.ExampleAsync("BrushWindowBarChart");
        var data = ChartData.From(example.Data, static (_, _) => null);

        var outside = ChartLayout.Compute(example.Spec with { Brush = example.Spec.Brush! with { StartIndex = -3, EndIndex = 100 } }, data);
        var reversed = ChartLayout.Compute(example.Spec with { Brush = example.Spec.Brush with { StartIndex = 15, EndIndex = 5 } }, data);
        var single = ChartLayout.Compute(example.Spec, ChartData.From(Reference.Rows(example).Take(1).ToList(), static (_, _) => null));

        Assert.Equal((0, 38, 39), (outside.Brush!.StartIndex, outside.Brush.EndIndex, outside.Data.Count));
        Assert.Equal((5, 15, 11), (reversed.Brush!.StartIndex, reversed.Brush.EndIndex, reversed.Data.Count));
        Assert.Equal((0, 0, 1), (single.Brush!.StartIndex, single.Brush.EndIndex, single.Data.Count));
    }
}

public sealed class BrushOptionTests(Desktop.DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "BRUSH-06")]
    public async Task APanoramaAndAlwaysShownTextsMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("BrushPanoramaChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "BRUSH-07")]
    public async Task TheBrushPlaceSizeAndColorsMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("BrushOptionsBarChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "BRUSH-08")]
    public async Task TheDesktopDrawsThePanoramaInsideTheStripAsync()
    {
        var result = await ReferenceDesktop.MeasureAsync(desktop, "BrushPanoramaChart");

        PixelVerdict.Within(result);
    }

    [Fact]
    [Trait("AC", "BRUSH-09")]
    public async Task BrushOptionsAndThePanoramaAreSetInXamlAsync()
    {
        Assert.Empty((await ReferenceDesktop.CompareXamlAsync(desktop, "BrushPanoramaChart")).Differences);
        Assert.Empty((await ReferenceDesktop.CompareXamlAsync(desktop, "BrushOptionsBarChart")).Differences);
    }

    [Fact]
    [Trait("AC", "BRUSH-10")]
    public void TheTextAtAnItemIsItsValueThroughTheFormatOrItsIndex()
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["name"] = "Mon", ["uv"] = 1.0 },
            new Dictionary<string, object?> { ["uv"] = 2.0 },
            new Dictionary<string, object?> { ["name"] = "Wed", ["uv"] = 3.0 },
        };
        var spec = new Specs.ChartSpec
        {
            Kind = Specs.ChartKind.Bar,
            Series = [new Specs.SeriesSpec { Kind = Specs.SeriesKind.Bar, DataKey = "uv" }],
            Brush = new Specs.BrushSpec { DataKey = "name", TickFormat = "<{0}>", AlwaysShowText = true, StartIndex = 1 },
        };
        var layout = ChartLayout.Compute(spec, ChartData.From(rows, static (_, _) => null));

        Assert.Equal(("<1>", "<Wed>"), layout.Brush!.Texts);
        Assert.Equal("<Mon>", layout.Brush.TextAt(layout.WholeData, 0));
    }
}

public sealed class BrushFieldTests
{
    [Fact]
    [Trait("AC", "BRUSH-13")]
    public async Task TheWebBrushWritesItsTravellersAsTheFrameworksRangeFieldsAsync()
    {
        var example = await Reference.ExampleAsync("BrushWindowBarChart");
        var spec = example.Spec with { Brush = example.Spec.Brush! with { Fields = true } };
        var render = Svg.ChartRenderer.Create(Specs.SpecJson.Write(spec), static (_, _) => null!);

        var rendered = Slots.Render(render, example.Data, new Dictionary<string, object?>());
        var start = rendered.Html.IndexOf("<style>.pictor-brush-fields", StringComparison.Ordinal);
        var end = rendered.Html.IndexOf("</foreignObject>", start, StringComparison.Ordinal) + "</foreignObject>".Length;
        var fields = rendered.Html[start..end];

        var drawn = System.Text.RegularExpressions.Regex.Replace(Reference.Pictor(example), "<g role=\"slider\" aria-label=\"[^\"]*\" aria-valuenow=\"[^\"]*\"", "<g");
        Assert.Equal(drawn, rendered.Html[..start] + rendered.Html[end..]);
        Assert.DoesNotContain("role=\"slider\"", rendered.Html, StringComparison.Ordinal);
        Assert.Equal([(0, "brush-start", (string?)null), (1, "brush-end", null), (2, "brush-slide", null)], rendered.Reserved);
        Assert.Matches("<input type=\"range\" class=\"pictor-b0\" style=\"--pictor-c:inset\\(0 [\\d.]+px 0 0\\)\" min=\"0\" max=\"38\" value=\"5\" aria-valuetext=\"6\" aria-label=\"Start of the window\" name=\"f0\" data-change>", fields);
        Assert.Matches("<input type=\"range\" class=\"pictor-b1\" style=\"--pictor-c:inset\\(0 0 0 [\\d.]+px\\)\" min=\"0\" max=\"38\" value=\"15\" aria-valuetext=\"17\" aria-label=\"End of the window\" name=\"f1\" data-change>", fields);
        Assert.Contains("--pictor-tw:5px;--pictor-th:30px", fields, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "BRUSH-14")]
    public async Task ARangeThumbSitsOnTheTravellerAtEveryIndexAsync()
    {
        var example = await Reference.ExampleAsync("BrushBarChart");
        var brush = ChartLayout.Compute(example.Spec, ChartData.From(example.Data, static (_, _) => null)).Brush!;
        var thumb = brush.Spec.TravellerWidth;

        var offsets = Enumerable.Range(0, brush.Count).Select(index => brush.X + (index * (brush.Width - thumb) / (brush.Count - 1)) - brush.Position(index));

        Assert.All(offsets, offset => Assert.Equal(0, offset, 9));
    }

    [Fact]
    [Trait("AC", "BRUSH-15")]
    public async Task TheWebBrushsSlideIsOneCompositeFieldMovingBothIndexesAsync()
    {
        var example = await Reference.ExampleAsync("BrushWindowBarChart");
        var spec = example.Spec with { Brush = example.Spec.Brush! with { Fields = true } };
        var render = Svg.ChartRenderer.Create(Specs.SpecJson.Write(spec), static (_, _) => null!);
        var rendered = Slots.Render(render, example.Data, new Dictionary<string, object?>());
        var brush = ChartLayout.Compute(spec, ChartData.From(example.Data, static (_, _) => null)).Brush!;
        var slide = System.Text.RegularExpressions.Regex.Match(await Reference.RechartsAsync("BrushWindowBarChart"), "recharts-brush-slide\"[^>]*x=\"([\\d.]+)\"[^>]*width=\"([\\d.]+)\"");
        var width = double.Parse(System.Text.RegularExpressions.Regex.Match(rendered.Html, "--pictor-sw:([\\d.]+)px").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        var tw = brush.Spec.TravellerWidth;
        var max = brush.Count - 1 - 10;
        var thumb = brush.X + (5 * (brush.Width - width) / max);
        var decoded = new string?[2];
        var refused = new string?[2];

        var decodes = Svg.ChartRenderer.Decode("brush-slide", "1560001000012", [5, 15], decoded);
        var refuses = !Svg.ChartRenderer.Decode("brush-slide", "x", [5, 15], refused) && !Svg.ChartRenderer.Decode("brush-start", "12", [5, 15], refused);

        Assert.Contains("<input type=\"range\" class=\"pictor-bs\" min=\"1560001000000\" max=\"1560001000028\" value=\"1560001000005\"", rendered.Html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Window\" name=\"f2\" data-change>", rendered.Html, StringComparison.Ordinal);
        Assert.Equal((2, "brush-slide", (string?)null), rendered.Reserved[2]);
        Assert.Equal(double.Parse(slide.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), thumb + tw, 2);
        Assert.Equal(double.Parse(slide.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), width - (2 * tw), 2);
        Assert.All(Enumerable.Range(0, max + 1), start => Assert.Equal(brush.Position(start), brush.X + (start * (brush.Width - width) / max), 2));
        Assert.True(decodes);
        Assert.Equal(["12", "22"], decoded);
        Assert.True(refuses);
    }
}
