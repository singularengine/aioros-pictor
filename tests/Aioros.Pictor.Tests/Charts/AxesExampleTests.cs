using System.Buffers;
using System.Text;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;
using Aioros.Pictor.Tests.Desktop;

namespace Aioros.Pictor.Tests.Charts;

public sealed class AxesExampleTests(DesktopSession desktop)
{
    [Theory]
    [Trait("AC", "AXES-01")]
    [InlineData("AxisLabelLineChart")]
    public async Task AxisAndReferenceLineLabelsAsObjectsMatchRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "AXES-02")]
    [InlineData("ReferenceLabelsChart")]
    public async Task ReferenceAreaAndDotLabelsAsObjectsMatchRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "TICKS-04")]
    [InlineData("RotatedTickBarChart")]
    [InlineData("SteepTickLineChart")]
    public async Task RotatedTickLabelsMatchRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "TICKS-05")]
    [InlineData("StyledAxesChart")]
    public async Task TickAxisLineAndTickLinePropsMatchRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "TICKS-06")]
    [InlineData("AxisOptionsChart")]
    public async Task OrientationMirrorReversedPaddingAndTickOptionsMatchRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "TICKS-07")]
    [InlineData("ExplicitTicksChart")]
    public async Task ExplicitTicksReplaceTheComputedOnesAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "TICKS-08")]
    [InlineData("EquidistantTicksChart")]
    public async Task EquidistantIntervalKeepsEveryNthTickAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "AXES-04")]
    [InlineData("MultiSeriesLineChart")]
    public async Task DroppingDuplicatedCategoriesPlacesEachPointAtItsCategoryAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "AXES-05")]
    [InlineData("DomainOptionsChart")]
    public async Task CategoryDomainIncludeHiddenAndShiftedBoundsMatchRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "AXES-06")]
    [InlineData("GapPaddingChart")]
    public async Task GapAndNoGapPaddingMatchRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "AXES-07")]
    [InlineData("CurveTypesChart")]
    [InlineData("VerticalBumpChart")]
    public async Task EveryNamedCurveMatchesRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "AXES-08")]
    [InlineData("LogScaleChart")]
    [InlineData("PowerScalesChart")]
    public async Task LogSqrtPowAndExplicitLinearScalesMatchRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "AXES-09")]
    [InlineData("TimeScaleChart")]
    [InlineData("VerticalTimeChart")]
    public async Task TimeAndUtcScalesMatchRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "TICKS-09")]
    [InlineData("WrappedTicksChart")]
    public async Task TickLabelsWiderThanTheirAxisWrapAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "AXES-10")]
    [InlineData("ZAxisOptionsChart")]
    public async Task ZAxisTypeScaleAndDomainMatchRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "STACK-01")]
    [InlineData("PercentAreaChart")]
    public async Task ExpandStacksToOneAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "STACK-02")]
    [InlineData("SilhouetteAreaChart")]
    public async Task SilhouetteCentersTheStackOnZeroAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "STACK-03")]
    [InlineData("WiggleAreaChart")]
    public async Task WiggleMinimizesTheStreamsMovementAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "STACK-04")]
    [InlineData("SignStackedBarChart")]
    public async Task SignStacksNegativeValuesBelowZeroAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "STACK-05")]
    [InlineData("PositiveStackedBarChart")]
    public async Task PositiveStacksNegativeValuesAsZeroAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "STACK-06")]
    [InlineData("ReverseStackBarChart")]
    public async Task ReverseStackOrderReversesStacksGroupsAndLegendAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "STACK-07")]
    [InlineData("BarGapsChart")]
    [InlineData("OwnBarSizesChart")]
    public async Task BarSizesAndGapsInEveryFormMatchRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "POLARAXIS-01")]
    [InlineData("RadarAxesChart")]
    public async Task PolarAxisPropsAndAnOutsideLabelMatchRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "POLARAXIS-02")]
    [InlineData("RadarCategoryAngleChart")]
    public async Task ACategoryAngleAxisAndAScaledRadiusAxisMatchRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "POLARAXIS-03")]
    [InlineData("RadialAngleAxisChart")]
    public async Task ANumericAngleAxisAndAReversedCategoryRadiusAxisMatchRechartsAsync(string name) => await MatchesAsync(name);

    [Theory]
    [Trait("AC", "AXES-03")]
    [InlineData("AxisLabelLineChart")]
    [InlineData("ReferenceLabelsChart")]
    [InlineData("RotatedTickBarChart")]
    [InlineData("SteepTickLineChart")]
    [InlineData("StyledAxesChart")]
    [InlineData("AxisOptionsChart")]
    [InlineData("ExplicitTicksChart")]
    [InlineData("EquidistantTicksChart")]
    [InlineData("MultiSeriesLineChart")]
    [InlineData("DomainOptionsChart")]
    [InlineData("GapPaddingChart")]
    [InlineData("CurveTypesChart")]
    [InlineData("VerticalBumpChart")]
    [InlineData("LogScaleChart")]
    [InlineData("PowerScalesChart")]
    [InlineData("TimeScaleChart")]
    [InlineData("VerticalTimeChart")]
    [InlineData("WrappedTicksChart")]
    [InlineData("ZAxisOptionsChart")]
    [InlineData("PercentAreaChart")]
    [InlineData("SilhouetteAreaChart")]
    [InlineData("WiggleAreaChart")]
    [InlineData("SignStackedBarChart")]
    [InlineData("PositiveStackedBarChart")]
    [InlineData("ReverseStackBarChart")]
    [InlineData("BarGapsChart")]
    [InlineData("OwnBarSizesChart")]
    [InlineData("RadarAxesChart")]
    [InlineData("RadarCategoryAngleChart")]
    [InlineData("RadialAngleAxisChart")]
    public async Task TheXamlPropsOfAxesAndLabelsWriteRechartsMarkupAsync(string name)
    {
        var example = await Reference.ExampleAsync(name);

        var argument = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                chart.Measure(new Avalonia.Size(example.Spec.Width, example.Spec.Height));
                chart.Arrange(new Avalonia.Rect(0, 0, example.Spec.Width, example.Spec.Height));
                return chart.WrittenSlot!.Argument;
            },
            TestContext.Current.CancellationToken);
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, SpecJson.Read(argument), example.Data, static (_, _) => null);

        Assert.Empty(MarkupComparison.Compare(await Reference.RechartsAsync(name), Encoding.UTF8.GetString(buffer.WrittenSpan)).Differences);
    }

    internal static async Task MatchesAsync(string name)
    {
        var verdict = await Reference.CompareAsync(name);

        Assert.True(verdict.Differences.Count == 0, string.Join("\n", verdict.Differences));
    }
}
