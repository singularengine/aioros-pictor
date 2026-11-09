using System.Collections.Frozen;
using System.Globalization;
using Aioros.Pictor.Demo;

namespace Aioros.Pictor.Tests;

/// <summary>
/// The verdict of a desktop frame against Recharts' screenshot: the same size, and at most the example's share of the
/// inked pixels differing strongly. Each limit is the share measured at the time (what differs is the texts' antialiasing, Skia against Chromium) with a tenth and a thousandth above it, so
/// a row of tick labels or a series' dots that vanish fail.
/// </summary>
internal static class PixelVerdict
{
    private static readonly FrozenDictionary<string, double> Limits = new Dictionary<string, double>(StringComparer.Ordinal)
    {
        ["AreaChartGradient"] = 0.012,
        ["BrushPanoramaChart"] = 0.033,
        ["CustomActiveShapePieChart"] = 0.007,
        ["CustomContentLegend"] = 0.095,
        ["CustomContentTreemap"] = 0.008,
        ["CustomizedChart"] = 0.079,
        ["CustomizedDotLineChart"] = 0.078,
        ["CustomizedLabelTickLineChart"] = 0.085,
        ["CustomLinkSankey"] = 0.001,
        ["CustomNodeSankey"] = 0.003,
        ["CustomReferenceShapes"] = 0.02,
        ["CustomShapeBarChart"] = 0.059,
        ["CustomShapeFunnelChart"] = 0.006,
        ["CustomShapeRadarChart"] = 0.005,
        ["CustomShapeRadialBarChart"] = 0.001,
        ["CustomShapeScatterChart"] = 0.05,
        ["CustomTravellerBrush"] = 0.016,
        ["DataOverflowComposedChart"] = 0.025,
        ["FunnelChart"] = 0.009,
        ["FunnelLabelListContent"] = 0.012,
        ["GapsAreaLegendBrushChart"] = 0.012,
        ["GapsBarBackgroundChart"] = 0.01,
        ["GapsComposedBrushChart"] = 0.026,
        ["GapsFunnelOptionsChart"] = 0.012,
        ["GapsLabelsChart"] = 0.027,
        ["GapsOwnDataErrorBarChart"] = 0.021,
        ["GapsPieOptionsChart"] = 0.029,
        ["GapsPolarAxisScalesChart"] = 0.019,
        ["GapsRadarTwoScalesChart"] = 0.005,
        ["GapsRadialGaugeChart"] = 0.019,
        ["GapsSignRadialChart"] = 0.016,
        ["GapsStackedLabelsChart"] = 0.028,
        ["PieLabelListContent"] = 0.001,
        ["PolarRadiusLabelContent"] = 0.007,
        ["PolarTickContent"] = 0.002,
        ["RadarLabelListContent"] = 0.005,
        ["RadialBarLabelListContent"] = 0.005,
        ["RangeAreaChart"] = 0.016,
        ["RangeRadarChart"] = 0.005,
        ["ReferenceOverflowLineChart"] = 0.031,
        ["SankeyLinkWidthChart"] = 0.001,
        ["SimpleBarChart"] = 0.027,
        ["SimpleRadarChart"] = 0.005,
        ["SimpleSankey"] = 0.001,
        ["SimpleSunburst"] = 0.015,
        ["SimpleTreemap"] = 0.003,
        ["ThreeDimScatterChart"] = 0.063,
        ["TwoLevelPieChart"] = 0.011,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static void Within(DesktopResult result)
    {
        Assert.True(Limits.TryGetValue(result.Name, out var limit), $"{result.Name}: no pixel limit measured");
        TestContext.Current.TestOutputHelper?.WriteLine(string.Create(CultureInfo.InvariantCulture, $"pixels {result.Name}: {result.Share:0.0000} ({result.Strong} of {result.Inked}), limit {limit}, same size {result.SameSize}"));
        Assert.True(result.SameSize, $"{result.Name}: the frame's size differs from Recharts' screenshot");
        Assert.True(result.Share < limit, string.Create(CultureInfo.InvariantCulture, $"{result.Name}: {result.Strong} of {result.Inked} inked pixels differ strongly ({result.Share:P2}, limit {limit:P1})"));
    }
}
