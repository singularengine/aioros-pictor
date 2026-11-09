using Aioros.Pictor.Tests.Desktop;

namespace Aioros.Pictor.Tests.Custom;

public sealed class CustomSectorTests(DesktopSession desktop)
{
    internal static void Templates(Chart chart, CustomTemplates templates) => chart.Elements.OfType<Pie>().Single().ActiveShapeTemplate = templates["RenderActiveShape"];

    [Fact]
    [Trait("AC", "CUSTOM-06")]
    public async Task APieActiveShapeTemplateIsWrittenAsRechartsDrawsRenderActiveShapeAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "CustomActiveShapePieChart", Templates);

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
    }
}
