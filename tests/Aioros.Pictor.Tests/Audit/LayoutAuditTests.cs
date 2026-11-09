using Aioros.Pictor.Data;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Ticks;

namespace Aioros.Pictor.Tests.Audit;

public sealed class LayoutAuditTests
{
    [Fact]
    [Trait("AC", "AUDIT-09")]
    public async Task AGapPaddingOverEqualValuesIsNoneAsync()
    {
        var example = await Reference.ExampleAsync("GapPaddingChart");
        var rows = Reference.Rows(example).Select(row => (object?)new Dictionary<string, object?>(row) { ["x"] = 0 }).ToList();

        var markup = Reference.Pictor(example with { Data = rows });
        var layout = ChartLayout.Compute(example.Spec, ChartData.From(rows, static (_, _) => null));

        Assert.DoesNotContain("NaN", markup, StringComparison.Ordinal);
        Assert.All(layout.XAxes, axis => Assert.True(double.IsFinite(axis.RangeStart) && double.IsFinite(axis.RangeEnd)));
    }

    [Fact]
    [Trait("AC", "AUDIT-10")]
    public async Task ASankeysNodesWithoutLinksAndLinksOfNoValueWriteNoNaNAsync()
    {
        var example = await Reference.ExampleAsync("BudgetSankey");
        var data = new Dictionary<string, object?>
        {
            ["nodes"] = new List<object?> { Node("A"), Node("B"), Node("C"), Node("Alone") },
            ["links"] = new List<object?> { Link(0, 1, 0), Link(0, 2, 0) },
        };
        var spec = example.Spec with { Tooltip = example.Spec.Tooltip ?? new TooltipSpec() };

        var markup = Reference.Pictor(example with { Spec = spec, Data = data });

        Assert.DoesNotContain("NaN", markup, StringComparison.Ordinal);
        Assert.Equal(6, Reference.States(markup, "pictor-hover").Count);
    }

    [Fact]
    [Trait("AC", "AUDIT-11")]
    public async Task AFunnelOfZerosAndAnInvalidWidthWriteNoNaNAsync()
    {
        var example = await Reference.ExampleAsync("FunnelChart");
        var funnel = example.Spec.Funnels[0];
        var zeros = funnel.Data! with { Columns = funnel.Data.Columns.SetItem("value", [.. Enumerable.Repeat<string?>("0", funnel.Data.Count)]) };
        var spec = example.Spec with { Funnels = [funnel with { Data = zeros, Width = "wide" }] };

        var markup = Reference.Pictor(example with { Spec = spec });
        var plot = FunnelPlot.Compute(spec, ChartData.From(null, static (_, _) => null));

        Assert.DoesNotContain("NaN", markup, StringComparison.Ordinal);
        Assert.All(plot.Funnels[0].Items, item => Assert.Equal((0.0, 0.0), (item.UpperWidth, item.LowerWidth)));
        Assert.Equal(Reference.Pictor(example), Reference.Pictor(example with { Spec = example.Spec with { Funnels = [funnel with { Width = "garbage%" }] } }));
    }

    [Fact]
    [Trait("AC", "AUDIT-12")]
    public async Task ARadialBarChartWithEveryBarHiddenHasNoHoverStateAsync()
    {
        var example = await Reference.ExampleAsync("SimpleRadialBarChart");
        var spec = example.Spec with { Polar = example.Spec.Polar! with { RadialBars = [.. example.Spec.Polar.RadialBars.Select(bar => bar with { Hide = true })] } };

        var markup = Reference.Pictor(example with { Spec = spec });
        var layout = PolarLayout.Compute(spec, ChartData.From(example.Data, static (_, _) => null));

        Assert.Equal(0, layout.HoverCount);
        Assert.DoesNotContain("pictor-hover", markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AUDIT-13")]
    public async Task ADeduplicatedCategorysTooltipReadsItsFirstItemAsync()
    {
        var radial = await Reference.ExampleAsync("SimpleRadialBarChart");
        List<object?> rows = [Row("a", 1), Row("a", 2), Row("b", 3)];
        var radialSpec = radial.Spec with { Legend = null, Polar = radial.Spec.Polar! with { RadiusAxis = new PolarRadiusAxisSpec { DataKey = "name", AllowDuplicatedCategory = false, Category = true } } };
        var radialTips = Tips(Reference.Pictor(radial with { Spec = radialSpec, Data = rows }));

        var radar = await Reference.ExampleAsync("SimpleRadarChart");
        List<object?> subjects = [Subject("a", 1), Subject("a", 2), Subject("b", 3)];
        var radarSpec = radar.Spec with { Tooltip = radar.Spec.Tooltip ?? new TooltipSpec(), Polar = radar.Spec.Polar! with { AngleAxis = (radar.Spec.Polar.AngleAxis ?? new PolarAngleAxisSpec()) with { DataKey = "subject", AllowDuplicatedCategory = false } } };
        var radarTips = Tips(Reference.Pictor(radar with { Spec = radarSpec, Data = subjects }));

        Assert.Equal(2, radialTips.Count);
        Assert.Contains(">uv : 1<", radialTips[0], StringComparison.Ordinal);
        Assert.Contains(">uv : 3<", radialTips[1], StringComparison.Ordinal);
        Assert.Contains(" : 1<", radarTips[0], StringComparison.Ordinal);
        Assert.Contains(" : 3<", radarTips[1], StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AUDIT-14")]
    public async Task TwoSeriesWrittenAlikeKeepTheirOwnPlacesAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var bar = example.Spec.Series[1] with { StackId = "s", Labels = new LabelSpec() };
        var spec = example.Spec with { Series = [bar, bar with { }], Legend = example.Spec.Legend! with { Toggles = true } };

        var layout = ChartLayout.Compute(spec, ChartData.From(example.Data, static (_, _) => null));
        var markup = Reference.Pictor(example with { Spec = spec });

        Assert.NotEmpty(layout.Series[0].StackTops);
        Assert.Equal(layout.Series[0].StackTops.Select(top => top * 2), layout.Series[1].StackTops);
        Assert.Contains("pictor-s1", markup, StringComparison.Ordinal);
        Assert.Contains("pictor-t1", markup, StringComparison.Ordinal);

        var apart = example.Spec with { Series = [example.Spec.Series[1], example.Spec.Series[1] with { }] };
        var bars = ChartLayout.Compute(apart, ChartData.From(example.Data, static (_, _) => null)).Series;
        Assert.True(bars[1].Bars[0].X > bars[0].Bars[0].X + bars[0].Bars[0].Width);
    }

    [Fact]
    [Trait("AC", "AUDIT-15")]
    public async Task ATickCountOfZeroAsksForNoNiceTicksAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var spec = example.Spec with { YAxes = [example.Spec.YAxes[0] with { TickCount = 0 }] };

        var layout = ChartLayout.Compute(spec, ChartData.From(example.Data, static (_, _) => null));

        Assert.Null(layout.YAxes[0].NiceTicks);
        Assert.Equal(9800, layout.YAxes[0].DomainMax);
        Assert.Empty(layout.YAxes[0].Visible);
        Assert.Equal([0.0], NiceTicks.Compute(0, double.PositiveInfinity, 0));
        Assert.Equal([3.0], NiceTicks.Compute(double.NegativeInfinity, 3, 0));
    }

    [Fact]
    [Trait("AC", "AUDIT-16")]
    public void ADomainShiftIsAnUnsignedDecimal()
    {
        Assert.Equal(5, AxisDomains.Bound("dataMin - NaN", 5, 9, overflow: false, lower: true));
        Assert.Equal(5, AxisDomains.Bound("dataMin - -5", 5, 9, overflow: false, lower: true));
        Assert.Equal(5, AxisDomains.Bound("dataMin - 1e3", 5, 9, overflow: false, lower: true));
        Assert.Equal(9, AxisDomains.Bound("dataMax + 2.", 5, 9, overflow: false, lower: false));
        Assert.Equal(0, AxisDomains.Bound("dataMin - 5", 5, 9, overflow: false, lower: true));
        Assert.Equal(10.5, AxisDomains.Bound("dataMax + 1.5", 5, 9, overflow: false, lower: false));
    }

    [Fact]
    [Trait("AC", "AUDIT-17")]
    public async Task StacksAreTakenOverTheWholeDataUnderABrushAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var spec = example.Spec with { StackOffset = StackOffsetKind.Wiggle, Series = [.. example.Spec.Series.Select(series => series with { StackId = "s", Labels = new LabelSpec() })] };
        var data = ChartData.From(example.Data, static (_, _) => null);

        var whole = ChartLayout.Compute(spec, data);
        var windowed = ChartLayout.Compute(spec with { Brush = new BrushSpec { StartIndex = 2, EndIndex = 5 } }, data);

        Assert.Equal(whole.Series[1].StackTops[2..6], windowed.Series[1].StackTops);
    }

    [Theory]
    [InlineData("{1}")]
    [InlineData("{")]
    [InlineData("}")]
    [InlineData("{0:X}")]
    [Trait("AC", "AUDIT-18")]
    public async Task AFormatThatFormatsNoValueWritesTheValueAsItIsAsync(string format)
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var spec = example.Spec with
        {
            XAxes = [example.Spec.XAxes[0] with { TickFormat = format }],
            YAxes = [example.Spec.YAxes[0] with { TickFormat = format }],
            Tooltip = example.Spec.Tooltip! with { LabelFormat = format },
            Legend = example.Spec.Legend! with { Format = format },
        };

        var markup = Reference.Pictor(example with { Spec = spec });

        Assert.Equal(Reference.Pictor(example), markup);
        Assert.Equal("1,235", Text.Formats.Apply("{0:N0}", 1234.6, "plain"));
        Assert.Equal("    12.5", Text.Formats.Apply("{0,8}", 12.5, "plain"));
        Assert.Equal("{12.5}", Text.Formats.Apply("{{{0}}}", 12.5, "plain"));
    }

    [Fact]
    [Trait("AC", "TICKS-10")]
    public async Task ACategoryThatReadsAsADateIsFormattedAsThatDateAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var spec = example.Spec with
        {
            XAxes = [example.Spec.XAxes[0] with { TickFormat = "{0:MMM d}" }],
            Tooltip = example.Spec.Tooltip! with { LabelFormat = "{0:MMMM}" },
        };
        string Dated(Func<int, object> day) => Reference.Pictor(example with
        {
            Spec = spec,
            Data = Reference.Rows(example).Select((row, index) => (object?)new Dictionary<string, object?>(row) { ["name"] = day(index) }).ToList(),
        });

        var texts = Dated(index => $"2024-04-{index + 1:00}");
        var dates = Dated(index => new DateTime(2024, 4, index + 1, 0, 0, 0, DateTimeKind.Unspecified));
        var plain = Reference.Pictor(example with { Spec = spec });

        Assert.Contains(">Apr 1<", texts, StringComparison.Ordinal);
        Assert.Contains(">Apr 2<", texts, StringComparison.Ordinal);
        Assert.Contains(">April<", texts, StringComparison.Ordinal);
        Assert.Contains(">Apr 1<", dates, StringComparison.Ordinal);
        Assert.Contains(">April<", dates, StringComparison.Ordinal);
        Assert.Contains(">Page A<", plain, StringComparison.Ordinal);
        Assert.DoesNotContain(">Apr", plain, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AXES-11")]
    public async Task AValueAxisWithADataKeyKeepsItsBarsSlotsAsync()
    {
        var example = await Reference.ExampleAsync("VerticalBarChart");
        var plain = Bars(Reference.Pictor(example with { Spec = example.Spec with { XAxes = [example.Spec.XAxes[0] with { Hide = true }] } }));
        var keyed = Bars(Reference.Pictor(example with { Spec = example.Spec with { XAxes = [example.Spec.XAxes[0] with { DataKey = "pv", Hide = true }] } }));

        Assert.Equal(14, keyed.Count);
        Assert.Equal(plain, keyed);
    }

    private static List<(string Y, string Width, string Height)> Bars(string markup) =>
        [.. System.Text.RegularExpressions.Regex.Matches(markup, "recharts-bar-rectangle\"><path [^>]*d=\"M[-\\d.]+,([-\\d.]+)h([-\\d.]+)v([-\\d.]+)")
            .Select(static match => (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value))];

    private static List<string> Tips(string markup) =>
        [.. markup.Split("class=\"pictor-tk", StringSplitOptions.None).Skip(1).Select(tip => tip.Split("class=\"pictor-hk", 2)[0])];

    private static Dictionary<string, object?> Node(string name) => new() { ["name"] = name };

    private static Dictionary<string, object?> Link(int source, int target, double value) => new() { ["source"] = source, ["target"] = target, ["value"] = value };

    private static Dictionary<string, object?> Row(string name, double uv) => new() { ["name"] = name, ["uv"] = uv, ["fill"] = "#8884d8" };

    private static Dictionary<string, object?> Subject(string subject, double a) => new() { ["subject"] = subject, ["A"] = a, ["B"] = a, ["fullMark"] = 150 };
}
