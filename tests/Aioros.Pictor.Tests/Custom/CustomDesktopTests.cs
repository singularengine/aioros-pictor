using Aioros.Pictor.Tests.Desktop;

namespace Aioros.Pictor.Tests.Custom;

public sealed class CustomDesktopTests(DesktopSession desktop)
{
    /// <summary>Each custom content example with its templates, as the Gallery's /custom page declares them.</summary>
    internal static readonly (string Name, Action<Chart, CustomTemplates> Templates)[] Examples =
    [
        ("CustomShapeBarChart", static (chart, templates) => chart.Elements.OfType<Bar>().Single().ShapeTemplate = templates["TriangleBar"]),
        ("CustomizedDotLineChart", static (chart, templates) => chart.Elements.OfType<Line>().First().DotTemplate = templates["CustomizedDot"]),
        ("CustomizedLabelTickLineChart", CustomLabelTests.Templates),
        ("CustomActiveShapePieChart", CustomSectorTests.Templates),
        ("CustomShapeScatterChart", CustomShapeTests.Scatter),
        ("CustomShapeFunnelChart", CustomShapeTests.Funnel),
        ("CustomShapeRadarChart", CustomShapeTests.Radar),
        ("CustomShapeRadialBarChart", CustomShapeTests.RadialBar),
        ("CustomContentTreemap", CustomHierarchyTests.Treemap),
        ("CustomNodeSankey", CustomHierarchyTests.SankeyNode),
        ("CustomLinkSankey", CustomHierarchyTests.SankeyLink),
        ("CustomTravellerBrush", CustomExtraTests.Traveller),
        ("CustomReferenceShapes", CustomExtraTests.References),
        ("CustomizedChart", CustomExtraTests.Customized),
        ("CustomContentLegend", CustomExtraTests.Legend),
        .. CustomLabelTests.PolarLists.Select(list => (list.Name, CustomLabelTests.PolarList(list.Template))),
        ("PolarTickContent", CustomLabelTests.PolarTicks),
        ("PolarRadiusLabelContent", CustomLabelTests.RadiusTitle),
    ];

    [Fact]
    [Trait("AC", "CUSTOM-04")]
    public async Task TheDesktopDrawsTheCustomContentExamplesAsRechartsDoesAsync()
    {
        foreach (var (name, templates) in Examples)
        {
            var result = await CustomReference.MeasureAsync(desktop, name, templates);
            PixelVerdict.Within(result);
        }
    }
}
