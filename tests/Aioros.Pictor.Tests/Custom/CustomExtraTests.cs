using Aioros.Pictor.Demo;
using Aioros.Pictor.Tests.Desktop;
using Avalonia.Headless;

namespace Aioros.Pictor.Tests.Custom;

public sealed class CustomExtraTests(DesktopSession desktop)
{
    internal static void Traveller(Chart chart, CustomTemplates templates) => chart.Elements.OfType<Brush>().Single().TravellerTemplate = templates["CustomTraveller"];

    internal static void References(Chart chart, CustomTemplates templates)
    {
        chart.Elements.OfType<ReferenceLine>().Single().ShapeTemplate = templates["ArrowLine"];
        chart.Elements.OfType<ReferenceArea>().Single().ShapeTemplate = templates["HatchedArea"];
        chart.Elements.OfType<ReferenceDot>().Single().ShapeTemplate = templates["StarDot"];
    }

    [Fact]
    [Trait("AC", "CUSTOM-11")]
    public async Task BrushTravellerAndReferenceShapeTemplatesAreWrittenAsRechartsDrawsThemAsync()
    {
        var traveller = await CustomReference.CompareAsync(desktop, "CustomTravellerBrush", Traveller);
        var references = await CustomReference.CompareAsync(desktop, "CustomReferenceShapes", References);

        Assert.True(traveller.Matches, "traveller: " + string.Join("\n", traveller.Differences));
        Assert.True(references.Matches, "references: " + string.Join("\n", references.Differences));
    }

    internal static void Customized(Chart chart, CustomTemplates templates) => chart.Elements.Add(new Customized { Template = templates["PlotFrame"] });

    [Fact]
    [Trait("AC", "CUSTOM-12")]
    public async Task ACustomizedChildIsDrawnWithTheChartsLayoutAsRechartsDrawsItAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "CustomizedChart", Customized);

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
    }

    internal static void Tooltip(Chart chart, CustomTemplates templates) => chart.Elements.OfType<Tooltip>().Single().ContentTemplate = templates["CustomTooltip"];

    [Fact]
    [Trait("AC", "CUSTOM-13")]
    public async Task ATooltipContentTemplateIsWrittenAsTheHtmlRechartsShowsAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "CustomContentOfTooltip", Tooltip);
        var left = await CustomReference.TooltipDifferencesAsync(desktop, "CustomContentOfTooltip", Tooltip, 0);
        var flipped = await CustomReference.TooltipDifferencesAsync(desktop, "CustomContentOfTooltip", Tooltip, 1);

        var desktopInk = await DesktopInkAsync();

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
        Assert.Empty(left);
        Assert.Empty(flipped);
        Assert.True(desktopInk > 400, $"{desktopInk} dark pixels where the desktop draws the hovered tooltip's texts");
    }

    [Fact]
    [Trait("AC", "CUSTOM-27")]
    public async Task ATooltipsBoxesTakeTheirSizesAlignmentsAndFontsAsync()
    {
        static void Configure(Chart chart, CustomTemplates templates) => chart.Elements.OfType<Tooltip>().Single().ContentTemplate = templates["ChartTooltip"];
        var markup = await CustomReference.MarkupAsync(desktop, "CustomContentOfTooltip", Configure);
        var spec = await CustomReference.SpecAsync(desktop, "CustomContentOfTooltip", Configure);
        Assert.True(spec.Tooltip!.Content!.Refused is null, spec.Tooltip.Content.Refused);
        var example = await Reference.ExampleAsync("CustomContentOfTooltip");
        var values = Painting.ContentProps.Of(spec.Tooltip.Content, Content.ContentSlots.Tooltip, Aioros.Pictor.Data.ChartData.From(example.Data, static (_, _) => null));
        var item = Painting.ContentProps.Tooltip(values!, 0, 0, 1, "Page B", [new Painting.TooltipEntry("pv", "1398", "#8884d8")]);
        var boxes = Content.HtmlContent.Arrange(values!.Spec.Nodes, item, 12, 0, 0);
        var (box, column, row, square, value) = (boxes[0], boxes[1], boxes[3], boxes[4], boxes[6]);

        Assert.Contains("min-width:128px", markup, StringComparison.Ordinal);
        Assert.Contains("box-shadow:0 20px 25px -5px rgba(0,0,0,0.102), 0 8px 10px -6px rgba(0,0,0,0.102)", markup, StringComparison.Ordinal);
        Assert.Matches("width:10px;height:10px;[^\"]*background:#8884d8;[^\"]*align-self:center", markup);
        Assert.Contains("line-height:16px", markup, StringComparison.Ordinal);
        Assert.Matches("line-height:12px;font-family:Geist Mono;[^\"]*margin-left:auto", markup);
        Assert.True(box.Width >= 128, $"box {box.Width} wide");
        Assert.Equal((10.0, 10.0), (square.Width, square.Height));
        Assert.Equal((box.Width - 22, box.Width - 22), (column.Width, row.Width));
        Assert.Equal(row.X + row.Width, value.X + value.Width, 3);
        Assert.Equal(row.Y + (row.Height / 2), square.Y + (square.Height / 2), 3);
    }

    [Fact]
    [Trait("AC", "TOOLTIP-24")]
    public async Task ATooltipContentShowsEveryLineOfThePayloadAsync()
    {
        static void Configure(Chart chart, CustomTemplates templates) => chart.Elements.OfType<Tooltip>().Single().ContentTemplate = templates["LinesTooltip"];
        var refused = (await CustomReference.SpecAsync(desktop, "SimpleBarChart", Configure)).Tooltip!.Content!.Refused;
        Assert.True(refused is null, refused);
        var markup = await CustomReference.MarkupAsync(desktop, "SimpleBarChart", Configure);
        var pageB = markup[markup.IndexOf(">pv 1398<", StringComparison.Ordinal)..];
        pageB = pageB[..pageB.IndexOf("</foreignObject>", StringComparison.Ordinal)];

        Assert.Contains(">uv 3000<", pageB, StringComparison.Ordinal);
        Assert.Matches("class=\"second\" style=\"[^\"]*color:#82ca9d", pageB);
        Assert.DoesNotContain("class=\"third\"", pageB, StringComparison.Ordinal);
    }

    internal static void Legend(Chart chart, CustomTemplates templates) => chart.Elements.OfType<Legend>().Single().ContentTemplate = templates["RenderLegend"];

    [Fact]
    [Trait("AC", "CUSTOM-14")]
    public async Task ALegendContentTemplateIsWrittenAsTheListRechartsShowsAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "CustomContentLegend", Legend);
        var box = await LegendBoxAsync();

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
        Assert.Empty(box);
    }

    private async Task<List<string>> LegendBoxAsync()
    {
        var expected = await Reference.RechartsAsync("CustomContentLegend");
        var actual = await CustomReference.MarkupAsync(desktop, "CustomContentLegend", Legend);
        var differences = new List<string>();
        foreach (var name in (string[])["left", "bottom", "width"])
        {
            var left = System.Text.RegularExpressions.Regex.Match(expected, "recharts-legend-wrapper\"[^>]*[ ;\"]" + name + ": ?([\\d.]+)px").Groups[1].Value;
            var right = System.Text.RegularExpressions.Regex.Match(actual, "recharts-legend-wrapper\"[^>]*[ ;\"]" + name + ": ?([\\d.]+)px").Groups[1].Value;
            if (left.Length == 0 || left != right)
            {
                differences.Add($"legend {name}: {left} in Recharts, {right} in Pictor");
            }
        }

        return differences;
    }

    /// <summary>How many dark pixels the desktop draws where the tooltip's texts are while Page B is hovered.</summary>
    private async Task<int> DesktopInkAsync()
    {
        var example = await Reference.ExampleAsync("CustomContentOfTooltip");
        return await desktop.Session.Dispatch(() => TooltipDesktopTests.Run(example, (window, chart) =>
        {
            Tooltip(chart, new CustomTemplates());
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            window.MouseMove(new Avalonia.Point(150, 150), Avalonia.Input.RawInputModifiers.None);
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            using var target = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize(500, 300));
            target.Render(chart);
            var pixels = DesktopRender.Pixels(target);
            var dark = 0;
            for (var y = 30; y < 110; y++)
            {
                for (var x = 186; x < 460; x++)
                {
                    var i = ((y * pixels.Width) + x) * 4;
                    dark += pixels.Bgra[i] < 120 && pixels.Bgra[i + 1] < 120 && pixels.Bgra[i + 2] < 120 ? 1 : 0;
                }
            }

            return dark;
        }), TestContext.Current.CancellationToken);
    }

    internal static Action<Chart, CustomTemplates> TooltipOf(string template) => (chart, templates) => chart.Elements.OfType<Tooltip>().Single().ContentTemplate = templates[template];

    [Fact]
    [Trait("AC", "CUSTOM-25")]
    public async Task TooltipTemplatesOfPolarFunnelTreemapAndSankeyTooltipsAreWrittenAsTheHtmlRechartsShowsAsync()
    {
        var differences = new List<string>();
        foreach (var (name, template) in ((string, string)[])[("PieTooltipContent", "ItemTooltip"), ("RadarTooltipContent", "AxisTooltip"), ("RadialBarTooltipContent", "AxisTooltip"), ("FunnelTooltipContent", "ItemTooltip"), ("TreemapTooltipContent", "ItemTooltip"), ("SankeyTooltipContent", "ItemTooltip")])
        {
            differences.AddRange((await CustomReference.TooltipDifferencesAsync(desktop, name, TooltipOf(template), 0)).Select(difference => $"{name}: {difference}"));
        }

        Assert.Empty(differences);
    }

    [Theory]
    [Trait("AC", "CUSTOM-28")]
    [InlineData("RowLegend", 16, 0)]
    [InlineData("QuarterLegend", 8, 0.25)]
    public async Task AFramedLegendLaysItsEntriesTemplatesInARowAsync(string frame, double gap, double basis)
    {
        void Configure(Chart chart, CustomTemplates templates)
        {
            var legend = chart.Elements.OfType<Legend>().Single();
            (legend.Template, legend.ContentTemplate) = (templates[frame], templates["SquareEntry"]);
        }

        var markup = await CustomReference.MarkupAsync(desktop, "CustomFramedLegend", Configure);
        var spec = await CustomReference.SpecAsync(desktop, "CustomFramedLegend", Configure);
        var example = await Reference.ExampleAsync("CustomFramedLegend");
        var legend = Aioros.Pictor.Painting.Plots.LegendOf(Aioros.Pictor.Painting.Plots.Compute(spec, Aioros.Pictor.Data.ChartData.From(example.Data, static (_, _) => null)))!;
        var values = new Content.ContentValues(Content.ContentSlots.LegendEntry, spec.Legend!.Content!, null);
        (double Width, double Height) Size(Layout.LegendItem entry) => Content.HtmlContent.Measure(values.Spec.Nodes, Painting.ContentProps.LegendEntry(values, entry), 12);
        var placed = Layout.LegendLayout.Place(legend, 0, 0, legend.Width, Size);
        var (first, second) = (Size(placed[0]).Width, Size(placed[1]).Width);
        var boxes = basis > 0 ? (basis * legend.Width, basis * legend.Width) : (first, second);
        var start = (legend.Width - boxes.Item1 - gap - boxes.Item2) / 2;
        var wrapper = markup[markup.IndexOf("recharts-legend-wrapper", StringComparison.Ordinal)..];

        Assert.Contains($"<div style=\"display:flex;flex-wrap:wrap;justify-content:center;gap:{gap}px\"><div style=\"display:flex;justify-content:center{(basis > 0 ? ";flex-basis:25%" : string.Empty)}\"><div", wrapper, StringComparison.Ordinal);
        Assert.Contains(">pv</p>", wrapper, StringComparison.Ordinal);
        Assert.Contains(">uv</p>", wrapper, StringComparison.Ordinal);
        Assert.DoesNotContain("<ul", wrapper, StringComparison.Ordinal);
        Assert.Equal(12 + Size(placed[0]).Height, legend.Height, 3);
        Assert.Equal((placed[0].Y, placed[1].Y), (0.0, 0.0));
        Assert.Equal(start + ((boxes.Item1 - first) / 2), placed[0].X, 3);
        Assert.Equal(start + boxes.Item1 + gap + ((boxes.Item2 - second) / 2), placed[1].X, 3);
    }

    internal static void FramedLegend(Chart chart, CustomTemplates templates) => chart.Elements.OfType<Legend>().Single().Template = templates["FramedLegend"];

    [Fact]
    [Trait("AC", "CUSTOM-26")]
    public async Task AWholeLegendTemplateIsWrittenAroundRechartsDefaultListAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "CustomFramedLegend", FramedLegend);
        var markup = await CustomReference.MarkupAsync(desktop, "CustomFramedLegend", FramedLegend);
        var spec = await CustomReference.SpecAsync(desktop, "CustomFramedLegend", FramedLegend);
        var example = await Reference.ExampleAsync("CustomFramedLegend");
        var plot = Aioros.Pictor.Painting.Plots.Compute(spec, Aioros.Pictor.Data.ChartData.From(example.Data, static (_, _) => null));
        var height = Aioros.Pictor.Painting.Plots.LegendOf(plot)!.Height;
        await using var client = File.OpenRead(Path.Combine(Reference.Root, "tools", "recharts", "out", "CustomFramedLegend.client.json"));
        using var measured = await System.Text.Json.JsonDocument.ParseAsync(client, cancellationToken: TestContext.Current.CancellationToken);
        var expected = measured.RootElement.GetProperty("legend").GetProperty("height").GetDouble();
        var legend = markup[markup.IndexOf("recharts-legend-wrapper", StringComparison.Ordinal)..];
        var desktopInk = await desktop.Session.Dispatch(() =>
        {
            var chart = CustomReference.Chart(example, FramedLegend);
            using var target = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize(500, 300));
            target.Render(chart);
            var pixels = Aioros.Pictor.Demo.DesktopRender.Pixels(target);
            var dark = 0;
            for (var y = 255; y < 300; y++)
            {
                for (var x = 20; x < 470; x++)
                {
                    var i = ((y * pixels.Width) + x) * 4;
                    dark += pixels.Bgra[i] < 230 ? 1 : 0;
                }
            }

            return dark;
        }, TestContext.Current.CancellationToken);

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
        Assert.Equal(expected, height);
        Assert.Contains("class=\"legend-frame\"", legend, StringComparison.Ordinal);
        Assert.Contains(">2 series</p><ul class=\"recharts-default-legend\"", legend, StringComparison.Ordinal);
        Assert.Contains("recharts-legend-item legend-item-1", legend, StringComparison.Ordinal);
        Assert.True(desktopInk > 600, $"{desktopInk} inked pixels where the desktop draws the framed legend");
    }

    [Theory]
    [Trait("AC", "LEGEND-12")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AResponsiveChartsTemplatedLegendIsWrittenInsideItsSurfaceAsync(bool framed)
    {
        void Configure(Chart chart, CustomTemplates templates)
        {
            var legend = chart.Elements.OfType<Legend>().Single();
            legend.Template = framed ? templates["RowLegend"] : null;
            legend.ContentTemplate = templates["SquareEntry"];
        }

        void Fill(Chart chart, CustomTemplates templates)
        {
            Configure(chart, templates);
            chart.Width = double.NaN;
        }

        var fixedSize = await CustomReference.MarkupAsync(desktop, "CustomFramedLegend", Configure);
        var filling = await CustomReference.MarkupAsync(desktop, "CustomFramedLegend", Fill);
        var surfaceEnd = filling.LastIndexOf("</svg>", StringComparison.Ordinal);
        var foreign = filling.IndexOf("<foreignObject x=\"0\" y=\"0\"", StringComparison.Ordinal);

        Assert.InRange(foreign, 0, surfaceEnd);
        Assert.Contains("style=\"pointer-events:none\"><div class=\"recharts-legend-wrapper\"", filling, StringComparison.Ordinal);
        Assert.Contains(";pointer-events:auto", filling[foreign..surfaceEnd], StringComparison.Ordinal);
        Assert.InRange(filling.IndexOf("recharts-legend-wrapper", StringComparison.Ordinal), foreign, surfaceEnd);
        Assert.DoesNotContain("pointer-events:auto", fixedSize, StringComparison.Ordinal);
        Assert.True(fixedSize.IndexOf("recharts-legend-wrapper", StringComparison.Ordinal) > fixedSize.LastIndexOf("</svg>", StringComparison.Ordinal));
    }
}
