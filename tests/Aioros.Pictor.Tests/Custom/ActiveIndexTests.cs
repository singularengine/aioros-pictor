using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Tests.Desktop;

namespace Aioros.Pictor.Tests.Custom;

public sealed partial class ActiveIndexTests(DesktopSession desktop)
{
    private static readonly Action<Chart, CustomTemplates> None = static (_, _) => { };

    internal static void Scatter(Chart chart, CustomTemplates templates)
    {
        var scatter = chart.Elements.OfType<Scatter>().Single();
        scatter.ShapeTemplate = templates["DiamondShape"];
        scatter.ActiveShapeTemplate = templates["ActiveDiamond"];
    }

    internal static void Funnel(Chart chart, CustomTemplates templates)
    {
        var funnel = chart.Elements.OfType<Funnel>().Single();
        funnel.ShapeTemplate = templates["RoundedTrapezoid"];
        funnel.ActiveShapeTemplate = templates["OutlinedTrapezoid"];
    }

    internal static void RadialBar(Chart chart, CustomTemplates templates) => chart.Elements.OfType<RadialBar>().Single().ActiveShapeTemplate = templates["GrownSector"];

    [Fact]
    [Trait("AC", "SHAPES-09")]
    public async Task ABarsActiveIndexIsDrawnActiveAndKeptUnderThePointerAsync()
    {
        var differences = await DifferencesAsync("ActiveIndexBarChart", None, "recharts-active-bar");

        Assert.Empty(differences);
    }

    [Fact]
    [Trait("AC", "SHAPES-10")]
    public async Task AScattersActiveIndexIsDrawnActiveAndKeptUnderThePointerAsync()
    {
        var differences = await DifferencesAsync("ActiveIndexScatterChart", None, "recharts-active-shape");
        differences.AddRange(await DifferencesAsync("CustomActiveIndexScatterChart", Scatter, "recharts-active-shape"));

        Assert.Empty(differences);
    }

    [Fact]
    [Trait("AC", "SHAPES-11")]
    public async Task AFunnelsActiveIndexIsDrawnActiveAndKeptUnderThePointerAsync()
    {
        var differences = await DifferencesAsync("ActiveIndexFunnelChart", None, "recharts-active-shape");
        differences.AddRange(await DifferencesAsync("CustomActiveIndexFunnelChart", Funnel, "recharts-active-shape"));

        Assert.Empty(differences);
    }

    [Fact]
    [Trait("AC", "SHAPES-12")]
    public async Task ARadialBarsActiveIndexIsIgnoredAndTheHoveredRingIsActiveAsync()
    {
        var grown = await CustomReference.CompareAsync(desktop, "ActiveIndexRadialBarChart", RadialBar);
        var plain = await CustomReference.CompareAsync(desktop, "DefaultActiveIndexRadialBarChart", None);
        var hover = await Reference.HoverAsync("ActiveIndexRadialBarChart", 0);
        var markup = await CustomReference.MarkupAsync(desktop, "ActiveIndexRadialBarChart", RadialBar);

        Assert.True(grown.Matches, string.Join("\n", grown.Differences));
        Assert.True(plain.Matches, string.Join("\n", plain.Differences));
        Assert.DoesNotContain("recharts-active-shape", await Reference.RechartsAsync("ActiveIndexRadialBarChart"), StringComparison.Ordinal);
        Assert.Contains("recharts-active-shape", hover.Recharts, StringComparison.Ordinal);
        Assert.Contains("recharts-active-shape", HoverComparison.Group(markup, "pictor-tips", hover.State), StringComparison.Ordinal);
        Assert.Empty(Reference.Differences(hover));
    }

    [Fact]
    [Trait("AC", "SHAPES-13")]
    public async Task APiesActiveIndexIsKeptUnderThePointerAsync()
    {
        var differences = await DifferencesAsync("ActiveIndexPieChart", None, "recharts-active-shape");

        Assert.Empty(differences);
    }

    /// <summary>
    /// The differences between Recharts and Pictor for a set <c>activeIndex</c>: the static markup element for element,
    /// Recharts' active layers unchanged by its interaction (a set index wins over the pointer), and no active layer in
    /// Pictor's hover state under the same pointer.
    /// </summary>
    private async Task<List<string>> DifferencesAsync(string name, Action<Chart, CustomTemplates> templates, string layer)
    {
        var verdict = await CustomReference.CompareAsync(desktop, name, templates);
        var differences = verdict.Matches ? [] : verdict.Differences.Select(difference => $"{name}: {difference}").ToList();
        var hover = await Reference.HoverAsync(name, 0);
        var still = Layers(await Reference.RechartsAsync(name), layer);
        if (still.Count == 0 || !still.SequenceEqual(Layers(hover.Recharts, layer)))
        {
            differences.Add($"{name}: Recharts' active layers changed under its interaction");
        }

        var markup = await CustomReference.MarkupAsync(desktop, name, templates);
        var state = HoverComparison.Group(markup, "pictor-tips", hover.State);
        if (state.Length == 0 || state.Contains("recharts-active-", StringComparison.Ordinal))
        {
            differences.Add($"{name}: Pictor's hover state {hover.State} draws an active shape or is missing");
        }

        return differences;
    }

    private static List<string> Layers(string markup, string layer) =>
        [.. Regex.Matches(markup[..(markup.IndexOf("</svg>", StringComparison.Ordinal) + 6)], "<g class=\"recharts-layer " + layer + "\">(?:<g>)?<[a-z]+ [^>]*>").Select(match => match.Value)];
}
