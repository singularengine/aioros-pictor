using System.Text.RegularExpressions;
using Aioros.Pictor.Content;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Drawing;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Tests.Desktop;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Aioros.Pictor.Tests.Custom;

public sealed class TooltipItemsTests(DesktopSession desktop)
{
    private static readonly TooltipEntry[] Lines =
    [
        new("pv", "1398", "#8884d8"), new("uv", "3000", "#82ca9d"), new("amt", "2210", "#ffc658"), new("uv again", "3000", "#ff7300"), new("pv again", "1398", "#0088fe"),
    ];

    internal static void FiveLines(Chart chart, CustomTemplates templates)
    {
        var tooltip = chart.Elements.OfType<Tooltip>().Single();
        (tooltip.ContentTemplate, tooltip.ItemTemplate) = (templates["ItemsTooltip"], templates["TooltipLine"]);
        chart.Elements.Add(new Bar { DataKey = "amt", Fill = "#ffc658", IsAnimationActive = false });
        chart.Elements.Add(new Bar { DataKey = "uv", Name = "uv again", Fill = "#ff7300", IsAnimationActive = false });
        chart.Elements.Add(new Bar { DataKey = "pv", Name = "pv again", Fill = "#0088fe", IsAnimationActive = false });
    }

    [Fact]
    [Trait("AC", "TOOLTIP-25")]
    public async Task ATooltipsItemTemplateIsWrittenOncePerLineAsync()
    {
        var spec = await CustomReference.SpecAsync(desktop, "SimpleBarChart", FiveLines);
        Assert.True(spec.Tooltip!.ItemContent!.Refused is null, spec.Tooltip.ItemContent.Refused);
        var markup = await CustomReference.MarkupAsync(desktop, "SimpleBarChart", FiveLines);
        var pageB = PageB(markup);
        var names = Regex.Matches(pageB, "class=\"name\"[^>]*>([^<]*)<").Select(static match => match.Groups[1].Value).ToList();
        var values = Regex.Matches(pageB, "class=\"value\"[^>]*>([^<]*)<").Select(static match => match.Groups[1].Value).ToList();
        static void Unlisted(Chart chart, CustomTemplates templates)
        {
            FiveLines(chart, templates);
            chart.Elements.OfType<Tooltip>().Single().ContentTemplate = templates["LinesTooltip"];
        }

        var unlisted = await CustomReference.MarkupAsync(desktop, "SimpleBarChart", Unlisted);
        static void Brought(Chart chart, CustomTemplates templates)
        {
            FiveLines(chart, templates);
            var tooltip = chart.Elements.OfType<Tooltip>().Single();
            (tooltip.ContentTemplate, tooltip.ItemTemplate) = (new LinesContent(templates["ItemsTooltip"], templates["TooltipLine"]), null);
        }

        var brought = PageB(await CustomReference.MarkupAsync(desktop, "SimpleBarChart", Brought));

        Assert.Contains("<div style=\"display:flex;flex-direction:column;gap:6px\">", pageB, StringComparison.Ordinal);
        Assert.Equal(["pv", "uv", "amt", "uv again", "pv again"], names);
        Assert.Equal(["1398", "3000", "2210", "3000", "1398"], values);
        Assert.Matches("class=\"indicator\" style=\"[^\"]*#0088fe", pageB);
        Assert.DoesNotContain("class=\"line\"", unlisted, StringComparison.Ordinal);
        Assert.Equal(5, Regex.Count(brought, "class=\"line\""));

        var tooltip = spec.Tooltip;
        var content = ContentProps.Of(tooltip.Content, ContentSlots.Tooltip, null)!;
        var item = ContentProps.Tooltip(content, 0, 0, 1, "Page B", Lines);
        var lines = TooltipLines.Of(tooltip, tooltip.Content!, Lines, 14, null)!;
        var line = InterMetricsLine();
        var (width, height) = HtmlContent.Measure(tooltip.Content!.Nodes, item, 14, lines);
        Assert.Equal((5 * line) + (4 * 6), lines.Height(0), 3);
        Assert.Equal(1 + 8 + line + 6 + lines.Height(0) + 8 + 1, height, 3);
        Assert.True(width >= lines.Width + 18, $"{width} wide for lines {lines.Width} wide");
        var top = 0.0;
        for (var i = 0; i < lines.Count; i++)
        {
            var boxes = lines.Arrange(i, 10, top, 200, out _, out var next);
            Assert.Equal(top + line + 6, next, 3);
            Assert.Equal((10.0, 200.0), (boxes[0].X, boxes[0].Width));
            Assert.Equal(210, boxes[^1].X + boxes[^1].Width, 3);
            top = next;
        }
    }

    [Fact]
    [Trait("AC", "CUSTOM-29")]
    public async Task ADashedBorderIsWrittenAndDrawnDashedAsync()
    {
        var markup = await CustomReference.MarkupAsync(desktop, "SimpleBarChart", FiveLines);
        var spec = await CustomReference.SpecAsync(desktop, "SimpleBarChart", FiveLines);
        var dashed = await RowsAsync(spec.Tooltip!.ItemContent!, null);
        var solid = await RowsAsync(spec.Tooltip.ItemContent!, "solid");
        var dotted = await RowsAsync(spec.Tooltip.ItemContent!, "dotted");

        Assert.Matches("class=\"indicator\" style=\"[^\"]*border:1.5px dashed #8884d8", PageB(markup));
        Assert.Equal(12, solid.Count(static painted => painted));
        Assert.InRange(Longest(dashed, true), 4, 5);
        Assert.InRange(Longest(dashed, false), 4, 5);
        Assert.InRange(dotted.Count(static painted => painted), 5, 7);
    }

    private static int Longest(bool[] rows, bool painted)
    {
        var (longest, run) = (0, 0);
        foreach (var row in rows)
        {
            run = row == painted ? run + 1 : 0;
            longest = Math.Max(longest, run);
        }

        return longest;
    }

    private static string PageB(string markup)
    {
        var pageB = markup[markup.IndexOf(">Page B</p>", StringComparison.Ordinal)..];
        return pageB[..pageB.IndexOf("</foreignObject>", StringComparison.Ordinal)];
    }

    private static double InterMetricsLine() => Text.InterMetrics.LineHeight(14);

    /// <summary>Whether the desktop paints each row of the first line's indicator's left border, its style replaced by <paramref name="style"/> when given.</summary>
    private async Task<bool[]> RowsAsync(Specs.ContentSpec line, string? style)
    {
        var nodes = style is null ? line.Nodes : [.. line.Nodes.Select(node => node with { Children = [.. node.Children.Select(child => child.Class == "indicator" ? child with { Attributes = [.. child.Attributes.Select(prop => prop.Name == "border-style" ? prop with { Text = style } : prop)] } : child)] })];
        var spec = line with { Nodes = nodes };
        return await desktop.Session.Dispatch(
            () =>
            {
                var values = new ContentValues(ContentSlots.TooltipItem, spec, null);
                var item = ContentProps.TooltipItem(values, 0, Lines[0]);
                var boxes = HtmlContent.ArrangeAcross(spec.Nodes, item, 14, 0, 0, 100);
                var indicator = boxes.Single(box => box.Node.Class == "indicator");
                using var target = new RenderTargetBitmap(new PixelSize(100, 20));
                using (var context = target.CreateDrawingContext())
                {
                    context.FillRectangle(Brushes.White, new Rect(0, 0, 100, 20));
                    ContentDrawing.Html(context, new ChartPaints(Typeface.Default, 14), boxes, item);
                }

                var pixels = DesktopRender.Pixels(target);
                var x = (int)Math.Floor(indicator.X + 0.75);
                var rows = new bool[12];
                for (var y = 0; y < 12; y++)
                {
                    var i = ((((int)indicator.Y + y) * pixels.Width) + x) * 4;
                    rows[y] = pixels.Bgra[i + 2] < 220;
                }

                return rows;
            },
            TestContext.Current.CancellationToken);
    }

    /// <summary>A content template bringing its lines' template, as a design system's tooltip content does.</summary>
    private sealed class LinesContent(Avalonia.Controls.Templates.IDataTemplate content, Avalonia.Controls.Templates.IDataTemplate line) : Avalonia.Controls.Templates.IDataTemplate, ITooltipLinesTemplate
    {
        public Avalonia.Controls.Templates.IDataTemplate ItemTemplate => line;

        public Avalonia.Controls.Control? Build(object? param) => content.Build(param);

        public bool Match(object? data) => content.Match(data);
    }
}
