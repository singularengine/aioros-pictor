using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Tests.Desktop;

namespace Aioros.Pictor.Tests.Charts;

public sealed class GapsTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "GAPS-01")]
    public async Task ATitleADescriptionBridgedNullsAndALegendHeightMatchRechartsAsync()
    {
        await MatchesAsync("GapsTitledComposedChart");
        var markup = Reference.Pictor(await Reference.ExampleAsync("GapsTitledComposedChart"));

        Assert.Contains("<title>Visits per page</title><desc>Views, visitors and amounts with gaps</desc>", markup, StringComparison.Ordinal);
        Assert.Empty(await Reference.LegendDifferencesAsync("GapsTitledComposedChart"));
    }

    [Fact]
    [Trait("AC", "GAPS-02")]
    public async Task LineAndAreaUnitsFollowTheirValuesInTheTooltipAsync()
    {
        None(Reference.Differences(await Reference.HoverAsync("GapsTitledComposedChart", 0), 0.5));
        None(Reference.Differences(await Reference.HoverAsync("GapsTitledComposedChart", 1), 0.5));
    }

    [Fact]
    [Trait("AC", "GAPS-03")]
    public async Task BarPropsReferencesOnNamedAxesAndALabelFormatMatchRechartsAsync() => await MatchesAsync("GapsBarOptionsChart");

    [Fact]
    [Trait("AC", "GAPS-04")]
    public async Task ABarsNameAndUnitShowInItsTooltipAsync() =>
        None(Reference.Differences(await Reference.HoverAsync("GapsBarOptionsChart", 0)));

    [Fact]
    [Trait("AC", "GAPS-05")]
    public async Task AxisStrokesHiddenAxesAndAnAxisWithoutTicksMatchRechartsAsync() => await MatchesAsync("GapsAxisHideStrokeChart");

    [Fact]
    [Trait("AC", "GAPS-06")]
    public async Task ScattersOnTheirOwnAxesWithLegendTypesAndAHiddenScatterMatchRechartsAsync() => await MatchesAsync("GapsScatterAxesChart");

    [Fact]
    [Trait("AC", "GAPS-07")]
    public async Task AHalfCircleRadialBarChartWithItsGapsAndRadialBarPropsMatchesRechartsAsync() => await MatchesAsync("GapsRadialGaugeChart");

    [Fact]
    [Trait("AC", "GAPS-08")]
    public async Task ARadialBarsNameShowsInItsTooltipWithoutItsUnitAsync()
    {
        var differences = Reference.Differences(await Reference.HoverAsync("GapsRadialGaugeChart", 0), 0.5);

        None([.. differences.Where(difference => !difference.StartsWith("tooltip at ", StringComparison.Ordinal))]);
        Assert.True(differences.Count <= 1, string.Join("\n", differences));
    }

    [Fact]
    [Trait("AC", "GAPS-09")]
    public async Task PieNameKeyCornerRadiusStrokeLegendTypeAndAHiddenPieMatchRechartsAsync()
    {
        await MatchesAsync("GapsPieOptionsChart");

        Assert.Contains("<title>Devices</title><desc>Visits by device</desc>", Reference.Pictor(await Reference.ExampleAsync("GapsPieOptionsChart")), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "GAPS-10")]
    public async Task APiesNameKeyNamesItsTooltipAsync() =>
        None(Reference.Differences(await Reference.HoverAsync("GapsPieOptionsChart", 0), 0.5));

    [Fact]
    [Trait("AC", "GAPS-11")]
    public async Task RadarAndPolarAxisPropsMatchRechartsAsync() => await MatchesAsync("GapsRadarOptionsChart");

    [Fact]
    [Trait("AC", "GAPS-12")]
    public async Task ARadarsUnitShowsInItsTooltipAsync() =>
        None(Reference.Differences(await Reference.HoverAsync("GapsRadarOptionsChart", 0), 0.5));

    [Fact]
    [Trait("AC", "GAPS-13")]
    public async Task HiddenPolarAxesDrawNothingAsync() => await MatchesAsync("GapsHiddenPolarAxesChart");

    [Fact]
    [Trait("AC", "GAPS-14")]
    public async Task ANumericAngleAxisWithAnOverflowingDomainAndAFormattedCategoryRadiusAxisMatchRechartsAsync() => await MatchesAsync("GapsPolarAxisScalesChart");

    [Fact]
    [Trait("AC", "GAPS-15")]
    public async Task FunnelPropsCellsAndAHiddenFunnelMatchRechartsAsync() => await MatchesAsync("GapsFunnelOptionsChart");

    [Fact]
    [Trait("AC", "GAPS-16")]
    public async Task AFunnelsNameKeyNamesItsTooltipAsync() =>
        None(Reference.Differences(await Reference.HoverAsync("GapsFunnelOptionsChart", 0), 0.5));

    [Fact]
    [Trait("AC", "GAPS-18")]
    public async Task StackedLabelListsShowTheStacksTopAsync()
    {
        await MatchesAsync("GapsStackedLabelsChart");
        var markup = Reference.Pictor(await Reference.ExampleAsync("GapsStackedLabelsChart"));

        Assert.Contains(">6400</tspan>", markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "GAPS-19")]
    public async Task GridValuesPlaceTheLinesAsTheirAxesKeepTicksAsync() => await MatchesAsync("GapsGridValuesChart");

    [Fact]
    [Trait("AC", "GAPS-20")]
    public async Task TooltipTypeNoneLeavesPolarAndFunnelEntriesOutAsync()
    {
        foreach (var (name, interaction) in new[] { ("GapsTooltipTypePieChart", 0), ("GapsTooltipTypePieChart", 1), ("GapsTooltipTypeRadarChart", 0), ("GapsTooltipTypeRadialChart", 0), ("GapsTooltipTypeFunnelChart", 0) })
        {
            await MatchesAsync(name);
            None([.. Reference.Differences(await Reference.HoverAsync(name, interaction), 0.5).Select(difference => $"{name}.{interaction}: {difference}")]);
        }
    }

    [Fact]
    [Trait("AC", "GAPS-21")]
    public async Task TooltipTypeNoneLeavesAScattersEntriesOutAsync()
    {
        await MatchesAsync("GapsTooltipTypeScatterChart");
        None(Reference.Differences(await Reference.HoverAsync("GapsTooltipTypeScatterChart", 0), 0.5));
    }

    [Fact]
    [Trait("AC", "GAPS-22")]
    public async Task ABrushSlicesTheChartDataOfAScatterChartAsync() => await MatchesAsync("GapsBrushScatterChart");

    [Fact]
    [Trait("AC", "GAPS-23")]
    public async Task QuantileQuantizeAndThresholdScalesMapToTheirRangeEndsAsync() => await MatchesAsync("GapsDiscreteScalesChart");

    [Fact]
    [Trait("AC", "GAPS-24")]
    public async Task IdentityAndSequentialScalesMatchRechartsAsync() => await MatchesAsync("GapsOtherScalesChart");

    [Fact]
    [Trait("AC", "GAPS-25")]
    public async Task ACategoryZValueShowsInTheTooltipAndAZeroOneIsLeftOutAsync()
    {
        await MatchesAsync("GapsZTooltipChart");
        None(Reference.Differences(await Reference.HoverAsync("GapsZTooltipChart", 0), 0.5));
        None(Reference.Differences(await Reference.HoverAsync("GapsZTooltipChart", 1), 0.5));
    }

    [Fact]
    [Trait("AC", "GAPS-26")]
    public async Task ACentricRadialBarChartLaysCategoriesAroundAndValuesAlongTheRadiusAsync()
    {
        await MatchesAsync("GapsCentricRadialChart");
        None(Reference.Differences(await Reference.HoverAsync("GapsCentricRadialChart", 0), 0.5));
    }

    [Theory]
    [Trait("AC", "GAPS-27")]
    [InlineData("GapsExpandRadialChart")]
    [InlineData("GapsSignRadialChart")]
    public async Task StackOffsetsApplyToStackedRadialBarsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "GAPS-28")]
    [InlineData("GapsLabelsChart")]
    [InlineData("GapsRadiusLabelsChart")]
    public async Task LabelChildrenFollowTheLabelPropWithPositionsAndFormatsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "GAPS-29")]
    [InlineData("BrushWindowBarChart")]
    [InlineData("GapsBrushScatterChart")]
    public async Task BrushTravellersAreNamedSlidersAsync(string name)
    {
        var expected = Sliders(await Reference.RechartsAsync(name));
        var actual = Sliders(Reference.Pictor(await Reference.ExampleAsync(name)));

        Assert.Equal(2, expected.Count);
        Assert.Equal(expected.Select(slider => slider.Label), actual.Select(slider => slider.Label));
        Assert.All(expected.Zip(actual), pair => Assert.Equal(pair.First.Value, pair.Second.Value, 0.01));
    }

    [Fact]
    [Trait("AC", "GAPS-30")]
    public async Task AResponsiveChartsContainerTakesTheIdClassAndStyleAsync()
    {
        var example = await Reference.ExampleAsync("GapsResponsiveContainerChart");
        var argument = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                (chart.Width, chart.Height, chart.Aspect) = (double.NaN, double.NaN, 2);
                (chart.ContainerId, chart.ContainerClass, chart.ContainerStyle) = ("sales", "chart-box", "background:#fafafa;padding:4px");
                chart.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
                var window = new Avalonia.Controls.Window { Width = 500, Height = 600, Content = chart };
                window.Show();
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                var written = chart.WrittenSlot!.Argument;
                window.Close();
                return written;
            },
            TestContext.Current.CancellationToken);
        var markup = Write(SpecJson.Read(argument), example.Data);
        var widths = Write(SpecJson.Read(argument) with { ResponsiveWidths = [300, 500] }, example.Data);

        Assert.Empty(MarkupComparison.Compare(await Reference.RechartsAsync("GapsResponsiveContainerChart"), markup.Replace("recharts-wrapper chart-box", "recharts-wrapper", StringComparison.Ordinal), MarkupDivergence.ResponsiveSurface).Differences);
        Assert.StartsWith("<div class=\"recharts-wrapper chart-box\" id=\"sales\" style=\"position:relative;cursor:default;background:#fafafa;padding:4px;width:100%;aspect-ratio:2;", markup, StringComparison.Ordinal);
        Assert.StartsWith("<div class=\"pictor-responsive chart-box\" id=\"sales\" style=\"background:#fafafa;padding:4px\">", widths, StringComparison.Ordinal);
        Assert.Equal(2, Count(widths, "<div class=\"recharts-wrapper pictor-w"));
    }

    [Fact]
    [Trait("AC", "GAPS-31")]
    public async Task BarBackgroundsTakeTheirOwnPropsAndNoneOfTheBarsAsync() => await MatchesAsync("GapsBarBackgroundChart");

    [Fact]
    [Trait("AC", "GAPS-32")]
    public async Task ErrorBarsOfBarsWithTheirOwnDataStandOnTheirBandsAsync() => await MatchesAsync("GapsOwnDataErrorBarChart");

    [Theory]
    [Trait("AC", "POLARAXIS-04")]
    [InlineData("GapsRadarTwoScalesChart")]
    [InlineData("GapsRadialTwoAnglesChart")]
    [InlineData("GapsImplicitPolarAxesChart")]
    public async Task SeveralPolarAxesByIdEachTakeTheirSeriesDomainAsync(string name) => await MatchesAsync(name);

    [Fact]
    [Trait("AC", "GAPS-33")]
    public async Task ACompactChartIsItsSurfaceAloneAsync()
    {
        await MatchesAsync("GapsCompactChart");
        var markup = Reference.Pictor(await Reference.ExampleAsync("GapsCompactChart"));

        Assert.StartsWith("<svg class=\"recharts-surface\" width=\"600\" height=\"160\"", markup, StringComparison.Ordinal);
        Assert.EndsWith("</svg>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("recharts-legend", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("recharts-tooltip", markup, StringComparison.Ordinal);
    }

    [Theory]
    [Trait("AC", "GAPS-34")]
    [InlineData("GapsAreaLegendBrushChart")]
    [InlineData("GapsComposedBrushChart")]
    public async Task AreaAndComposedChartsWithALegendAndABrushMatchRechartsAsync(string name)
    {
        await MatchesAsync(name);
        Assert.Empty(await Reference.LegendDifferencesAsync(name));
    }

    [Theory]
    [Trait("AC", "GAPS-35")]
    [InlineData("GapsLabelsChart")]
    [InlineData("GapsBarBackgroundChart")]
    [InlineData("GapsRadarTwoScalesChart")]
    [InlineData("GapsRadialGaugeChart")]
    [InlineData("GapsPolarAxisScalesChart")]
    [InlineData("GapsFunnelOptionsChart")]
    [InlineData("GapsOwnDataErrorBarChart")]
    [InlineData("GapsSignRadialChart")]
    [InlineData("GapsAreaLegendBrushChart")]
    [InlineData("GapsComposedBrushChart")]
    [InlineData("GapsStackedLabelsChart")]
    [InlineData("GapsPieOptionsChart")]
    public async Task TheDesktopControlsOfTheGapsExamplesLookLikeRechartsAsync(string name)
    {
        var result = await ReferenceDesktop.MeasureAsync(desktop, name);

        PixelVerdict.Within(result);
    }

    [Theory]
    [Trait("AC", "GAPS-17")]
    [InlineData("GapsTitledComposedChart")]
    [InlineData("GapsBarOptionsChart")]
    [InlineData("GapsAxisHideStrokeChart")]
    [InlineData("GapsScatterAxesChart")]
    [InlineData("GapsRadialGaugeChart")]
    [InlineData("GapsPieOptionsChart")]
    [InlineData("GapsRadarOptionsChart")]
    [InlineData("GapsHiddenPolarAxesChart")]
    [InlineData("GapsPolarAxisScalesChart")]
    [InlineData("GapsFunnelOptionsChart")]
    [InlineData("GapsStackedLabelsChart")]
    [InlineData("GapsGridValuesChart")]
    [InlineData("GapsTooltipTypePieChart")]
    [InlineData("GapsTooltipTypeRadarChart")]
    [InlineData("GapsTooltipTypeRadialChart")]
    [InlineData("GapsTooltipTypeFunnelChart")]
    [InlineData("GapsTooltipTypeScatterChart")]
    [InlineData("GapsBrushScatterChart")]
    [InlineData("GapsDiscreteScalesChart")]
    [InlineData("GapsOtherScalesChart")]
    [InlineData("GapsZTooltipChart")]
    [InlineData("GapsCentricRadialChart")]
    [InlineData("GapsExpandRadialChart")]
    [InlineData("GapsSignRadialChart")]
    [InlineData("GapsLabelsChart")]
    [InlineData("GapsRadiusLabelsChart")]
    [InlineData("GapsBarBackgroundChart")]
    [InlineData("GapsOwnDataErrorBarChart")]
    [InlineData("GapsRadarTwoScalesChart")]
    [InlineData("GapsRadialTwoAnglesChart")]
    [InlineData("GapsImplicitPolarAxesChart")]
    [InlineData("GapsCompactChart")]
    [InlineData("GapsAreaLegendBrushChart")]
    [InlineData("GapsComposedBrushChart")]
    public async Task TheXamlPropsOfTheGapsExamplesWriteRechartsMarkupAsync(string name)
    {
        var verdict = await ReferenceDesktop.CompareXamlAsync(desktop, name);

        Assert.True(verdict.Differences.Count == 0, string.Join("\n", verdict.Differences));
    }

    private static List<(string Label, double Value)> Sliders(string markup) =>
        [.. System.Text.RegularExpressions.Regex.Matches(markup, "<g[^>]*recharts-brush-traveller[^>]*>").Select(match => match.Value)
            .Where(group => group.Contains("role=\"slider\"", StringComparison.Ordinal))
            .Select(group => (System.Text.RegularExpressions.Regex.Match(group, "aria-label=\"([^\"]*)\"").Groups[1].Value, double.Parse(System.Text.RegularExpressions.Regex.Match(group, "aria-valuenow=\"([^\"]*)\"").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)))];

    private static string Write(Specs.ChartSpec spec, object? data)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        Svg.ChartSvg.Write(buffer, spec, data, static (_, _) => null);
        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static int Count(string text, string part) => (text.Length - text.Replace(part, string.Empty, StringComparison.Ordinal).Length) / part.Length;

    private static void None(List<string> differences) => Assert.True(differences.Count == 0, string.Join("\n", differences));

    private static async Task MatchesAsync(string name)
    {
        var verdict = await Reference.CompareAsync(name);

        Assert.True(verdict.Differences.Count == 0, string.Join("\n", verdict.Differences));
    }
}
