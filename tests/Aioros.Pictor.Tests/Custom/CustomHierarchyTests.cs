using Aioros.Pictor.Tests.Desktop;

namespace Aioros.Pictor.Tests.Custom;

public sealed class CustomHierarchyTests(DesktopSession desktop)
{
    internal static void Treemap(Chart chart, CustomTemplates templates)
    {
        var treemap = (Treemap)chart;
        treemap.ColorPanel = "#8889DD,#9597E4,#8DC77B,#A5D297,#E2CF45,#F8C12D";
        treemap.ContentTemplate = templates["CustomizedContent"];
    }

    [Fact]
    [Trait("AC", "CUSTOM-09")]
    public async Task ATreemapContentTemplateIsWrittenAsRechartsDrawsCustomizedContentAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "CustomContentTreemap", Treemap);

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
    }

    internal static void SankeyNode(Chart chart, CustomTemplates templates) => ((Sankey)chart).NodeTemplate = templates["DemoSankeyNode"];

    internal static void SankeyLink(Chart chart, CustomTemplates templates) => ((Sankey)chart).LinkTemplate = templates["BandLink"];

    [Fact]
    [Trait("AC", "CUSTOM-10")]
    public async Task SankeyNodeAndLinkTemplatesAreWrittenAsRechartsDrawsThemAsync()
    {
        var nodes = await CustomReference.CompareAsync(desktop, "CustomNodeSankey", SankeyNode);
        var links = await CustomReference.CompareAsync(desktop, "CustomLinkSankey", SankeyLink);

        Assert.True(nodes.Matches, "nodes: " + string.Join("\n", nodes.Differences));
        Assert.True(links.Matches, "links: " + string.Join("\n", links.Differences));
    }

    internal static void NestIndex(Chart chart, CustomTemplates templates) => ((Treemap)chart).NestIndexTemplate = templates["NestIndexContent"];

    [Fact]
    [Trait("AC", "CUSTOM-16")]
    public async Task ANestIndexTemplateIsWrittenInEachTrailBoxAsRechartsWritesItAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "CustomNestIndexTreemap", NestIndex);
        var markup = await CustomReference.MarkupAsync(desktop, "CustomNestIndexTreemap", NestIndex);

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
        Assert.Contains("font-weight:700;margin:0 0 0 0;\">0: root</span>", markup, StringComparison.Ordinal);
    }
}
