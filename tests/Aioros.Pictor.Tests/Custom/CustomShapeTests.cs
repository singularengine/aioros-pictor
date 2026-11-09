using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Tests.Desktop;
using Avalonia.Headless;

namespace Aioros.Pictor.Tests.Custom;

public sealed class CustomShapeTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "CUSTOM-01")]
    public async Task ABarShapeTemplateIsWrittenAsRechartsDrawsTheTriangleBarsAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "CustomShapeBarChart", static (chart, templates) => chart.Elements.OfType<Bar>().Single().ShapeTemplate = templates["TriangleBar"]);

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
    }

    [Fact]
    [Trait("AC", "CUSTOM-02")]
    public async Task ATemplateWithAConverterWithLogicIsRefusedWithItsReasonAndDrawnAsAvaloniaLaysItOutAsync()
    {
        var example = await Reference.ExampleAsync("CustomShapeBarChart");
        var (slot, reason, root, desktopBars) = await desktop.Session.Dispatch(() =>
        {
            var chart = CustomReference.Chart(example, static (chart, templates) => chart.Elements.OfType<Bar>().Single().ShapeTemplate = templates["RootTriangleBar"]);
            var slot = chart.WrittenSlot!;
            var spec = SpecJson.Read(slot.Argument);
            var registered = Avalonia.AvaloniaPropertyRegistry.Instance.FindRegistered(chart, ChartSlot.RefusedPart);
            using var bitmap = DesktopRender.Draw(chart);
            var pixels = DesktopRender.Pixels(bitmap);
            return (slot, spec.Series[0].ShapeContent!.Refused!, registered, pixels);
        }, TestContext.Current.CancellationToken);
        var markup = await CustomReference.MarkupAsync(desktop, "CustomShapeBarChart", static (chart, templates) => chart.Elements.OfType<Bar>().Single().ShapeTemplate = templates["RootTriangleBar"]);
        var tips = System.Text.RegularExpressions.Regex.Matches(markup, @"recharts-bar-rectangle""><path d=""M([\d.]+),([\d.]+)C[\d., ]+ ([\d.]+),([\d.]+)C").Select(match => (Bottom: double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), Top: double.Parse(match.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture))).ToList();

        Assert.StartsWith("Bar.ShapeTemplate cannot be written as SVG: the d of the template's AvaloniaPath is not a sum of the context's members", reason, StringComparison.Ordinal);
        Assert.Contains(slot.Parts, part => part.Kind == Aioros.Runtime.Rendering.SlotPartKind.Read && part.Key == reason && part.Property == ChartSlot.RefusedPart);
        Assert.Null(root);
        Assert.Equal(7, tips.Count);
        Assert.All(tips, tip => Assert.True(tip.Bottom - tip.Top > 5 && tip.Bottom - tip.Top < 16, $"{tip}"));
        Assert.False(ReferenceDesktop.Blank(desktopBars, 107, 33), "the first triangle is drawn on the desktop, where its converter puts it");
        Assert.True(ReferenceDesktop.Blank(desktopBars, 110, 255), "the first triangle is short, as the converter makes it, where the full triangle would be drawn");
    }

    internal static void Scatter(Chart chart, CustomTemplates templates)
    {
        var scatter = chart.Elements.OfType<Scatter>().Single();
        scatter.ShapeTemplate = templates["DiamondShape"];
        scatter.ActiveShapeTemplate = templates["ActiveDiamond"];
    }

    internal static void Funnel(Chart chart, CustomTemplates templates) => chart.Elements.OfType<Funnel>().Single().ShapeTemplate = templates["RoundedTrapezoid"];

    internal static void Radar(Chart chart, CustomTemplates templates) => chart.Elements.OfType<Radar>().Single().ShapeTemplate = templates["RadarPolygon"];

    internal static void RadialBar(Chart chart, CustomTemplates templates) => chart.Elements.OfType<RadialBar>().Single().ShapeTemplate = templates["RoundedSector"];

    [Fact]
    [Trait("AC", "CUSTOM-07")]
    public async Task ScatterShapeAndActiveShapeTemplatesAreWrittenAsRechartsDrawsThemAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "CustomShapeScatterChart", Scatter);
        var active = await CustomReference.ActiveDifferencesAsync(desktop, "CustomShapeScatterChart", Scatter, 0);

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
        Assert.Empty(active);
    }

    [Fact]
    [Trait("AC", "CUSTOM-08")]
    public async Task FunnelRadarAndRadialBarShapeTemplatesAreWrittenAsRechartsDrawsThemAsync()
    {
        var funnel = await CustomReference.CompareAsync(desktop, "CustomShapeFunnelChart", Funnel);
        var radar = await CustomReference.CompareAsync(desktop, "CustomShapeRadarChart", Radar);
        var radial = await CustomReference.CompareAsync(desktop, "CustomShapeRadialBarChart", RadialBar);

        Assert.True(funnel.Matches, "funnel: " + string.Join("\n", funnel.Differences));
        Assert.True(radar.Matches, "radar: " + string.Join("\n", radar.Differences));
        Assert.True(radial.Matches, "radial bar: " + string.Join("\n", radial.Differences));
    }

    internal static void ActiveBar(Chart chart, CustomTemplates templates)
    {
        var bar = chart.Elements.OfType<Bar>().Single();
        bar.ShapeTemplate = templates["TriangleBar"];
        bar.ActiveShapeTemplate = templates["OutlinedTriangleBar"];
    }

    [Fact]
    [Trait("AC", "CUSTOM-15")]
    public async Task ABarActiveShapeTemplateIsDrawnOnTheHoveredBarAsRechartsDrawsItAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "CustomActiveBarChart", ActiveBar);
        var active = await CustomReference.ActiveDifferencesAsync(desktop, "CustomActiveBarChart", ActiveBar, 0, "recharts-active-bar");

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
        Assert.Empty(active);
    }

    internal static void ActiveFunnel(Chart chart, CustomTemplates templates)
    {
        var funnel = chart.Elements.OfType<Funnel>().Single();
        funnel.ShapeTemplate = templates["RoundedTrapezoid"];
        funnel.ActiveShapeTemplate = templates["OutlinedTrapezoid"];
    }

    internal static void ActiveRadialBar(Chart chart, CustomTemplates templates) => chart.Elements.OfType<RadialBar>().Single().ActiveShapeTemplate = templates["GrownSector"];

    [Fact]
    [Trait("AC", "CUSTOM-17")]
    public async Task FunnelAndRadialBarActiveShapeTemplatesAreDrawnOnTheHoveredItemAsRechartsDrawsThemAsync()
    {
        var funnel = await CustomReference.OutlinedDifferencesAsync(desktop, "CustomActiveFunnelChart", ActiveFunnel, 0, "#333");
        var radial = await CustomReference.OutlinedDifferencesAsync(desktop, "CustomActiveRadialBarChart", ActiveRadialBar, 0, "#333");

        Assert.True(funnel.Count == 0, "funnel: " + string.Join("\n", funnel));
        Assert.True(radial.Count == 0, "radial bar: " + string.Join("\n", radial));
    }

    [Fact]
    [Trait("AC", "CUSTOM-24")]
    public async Task AFunnelsActiveShapeIsDrawnInPlaceUnderItsLabelAsync()
    {
        var hover = await Reference.HoverAsync("CustomActiveFunnelLabels", 0);
        var markup = await CustomReference.MarkupAsync(desktop, "CustomActiveFunnelLabels", ActiveFunnel);
        var shown = MarkupComparison.Compare(hover.Recharts, CustomReference.Applied(markup, hover.State));
        var tip = HoverComparison.Group(markup, "pictor-tips", hover.State);
        var rules = markup.Contains("svg:has(>g>.pictor-hk" + hover.State + ":hover) .pictor-ak" + hover.State, StringComparison.Ordinal);
        var example = await Reference.ExampleAsync("CustomActiveFunnelLabels");
        var white = await desktop.Session.Dispatch(() => Aioros.Pictor.Tests.Desktop.TooltipDesktopTests.Run(example, (window, chart) =>
        {
            ActiveFunnel(chart, new CustomTemplates());
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            window.MouseMove(new Avalonia.Point(150, 120), Avalonia.Input.RawInputModifiers.None);
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            using var target = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize(700, 432));
            target.Render(chart);
            var pixels = Aioros.Pictor.Demo.DesktopRender.Pixels(target);
            var count = 0;
            for (var y = 120; y < 140; y++)
            {
                for (var x = 290; x < 350; x++)
                {
                    var i = ((y * pixels.Width) + x) * 4;
                    count += pixels.Bgra[i] > 230 && pixels.Bgra[i + 1] > 230 && pixels.Bgra[i + 2] > 230 ? 1 : 0;
                }
            }

            return count;
        }), TestContext.Current.CancellationToken);

        Assert.True(shown.Matches, string.Join("\n", shown.Differences));
        Assert.True(rules, "no rule shows the in-place group of the hovered state");
        Assert.DoesNotContain("recharts-active-shape", tip, StringComparison.Ordinal);
        Assert.True(white > 60, $"{white} white pixels of the hovered item's name on the desktop");
    }
}
