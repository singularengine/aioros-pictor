using Aioros.Pictor.Tests.Desktop;

namespace Aioros.Pictor.Tests.Custom;

public sealed class CustomLabelTests(DesktopSession desktop)
{
    internal static void Templates(Chart chart, CustomTemplates templates)
    {
        chart.Elements.OfType<Line>().First().Parts.OfType<LabelList>().Single().ContentTemplate = templates["CustomizedLabel"];
        chart.Elements.OfType<XAxis>().Single().TickTemplate = templates["CustomizedAxisTick"];
    }

    [Fact]
    [Trait("AC", "CUSTOM-05")]
    public async Task LabelAndTickTemplatesAreWrittenAsRechartsDrawsThemAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "CustomizedLabelTickLineChart", Templates);

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
    }

    internal static void BarLabel(Chart chart, CustomTemplates templates)
    {
        var bar = chart.Elements.OfType<Bar>().Single();
        foreach (var list in bar.Parts.OfType<LabelList>())
        {
            list.ContentTemplate = templates["CustomizedBarLabel"];
        }

        foreach (var label in bar.Parts.OfType<Label>())
        {
            label.ContentTemplate = templates["CustomizedBarLabel"];
        }
    }

    [Fact]
    [Trait("AC", "CUSTOM-18")]
    public async Task ABarLabelTemplateIsWrittenAsRechartsDrawsTheCustomizedLabelAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "CustomizedBarLabel", BarLabel);

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
    }

    internal static void ElementLabels(Chart chart, CustomTemplates templates)
    {
        chart.Elements.OfType<YAxis>().Single().Labels.Single().ContentTemplate = templates["AxisTitle"];
        chart.Elements.OfType<ReferenceLine>().Single().Labels.Single().ContentTemplate = templates["TargetLabel"];
    }

    [Fact]
    [Trait("AC", "CUSTOM-19")]
    public async Task LabelTemplatesOfAnAxisAndAReferenceLineAreWrittenAsRechartsDrawsThemAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "CustomElementLabels", ElementLabels);

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
    }

    internal static Action<Chart, CustomTemplates> PolarList(string template) => (chart, templates) =>
    {
        IEnumerable<ISeriesPart> parts = chart.Elements switch
        {
            var elements when elements.OfType<Pie>().SingleOrDefault() is { } pie => pie.Cells,
            var elements when elements.OfType<RadialBar>().SingleOrDefault() is { } bar => bar.Cells,
            var elements when elements.OfType<Radar>().SingleOrDefault() is { } radar => radar.Parts,
            var elements => elements.OfType<Funnel>().Single().Parts,
        };
        parts.OfType<LabelList>().Last().ContentTemplate = templates[template];
    };

    internal static readonly (string Name, string Template)[] PolarLists =
    [
        ("PieLabelListContent", "SectorValueLabel"),
        ("RadialBarLabelListContent", "BarValueLabel"),
        ("RadarLabelListContent", "RadarPointLabel"),
        ("FunnelLabelListContent", "FunnelBadge"),
    ];

    [Fact]
    [Trait("AC", "CUSTOM-20")]
    public async Task LabelListTemplatesOfPolarChartsAndFunnelsAreWrittenAsRechartsDrawsThemAsync()
    {
        var differences = new List<string>();
        foreach (var (name, template) in PolarLists)
        {
            var verdict = await CustomReference.CompareAsync(desktop, name, PolarList(template));
            differences.AddRange(verdict.Differences.Select(difference => $"{name}: {difference}"));
        }

        Assert.Empty(differences);
    }

    internal static void PolarTicks(Chart chart, CustomTemplates templates)
    {
        chart.Elements.OfType<PolarAngleAxis>().Single().TickTemplate = templates["AngleTick"];
        chart.Elements.OfType<PolarRadiusAxis>().Single().TickTemplate = templates["RadiusTick"];
    }

    [Fact]
    [Trait("AC", "CUSTOM-21")]
    public async Task PolarAxisTickTemplatesAreWrittenAsRechartsDrawsThemAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "PolarTickContent", PolarTicks);

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
    }

    internal static void RadiusTitle(Chart chart, CustomTemplates templates) => chart.Elements.OfType<PolarRadiusAxis>().Single().Labels.Single().ContentTemplate = templates["RadiusTitle"];

    [Fact]
    [Trait("AC", "CUSTOM-22")]
    public async Task APolarRadiusAxisLabelTemplateIsWrittenAsRechartsDrawsItAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "PolarRadiusLabelContent", RadiusTitle);

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
    }
}
