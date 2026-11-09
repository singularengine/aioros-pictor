using System.Text.RegularExpressions;
using Aioros.Pictor.Svg;
using Aioros.Pictor.Tests.Desktop;
using Aioros.Pictor.Tests.Interactive;

namespace Aioros.Pictor.Tests.Audit;

public sealed partial class FunctionalAuditTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "AUDIT-30")]
    public async Task AToggledFunnelsTipsAreHoverStatesAsync()
    {
        var example = await Reference.ExampleAsync("FunnelChart");
        var spec = example.Spec with { Legend = new Specs.LegendSpec { Toggles = true } };

        var markup = Reference.Pictor(example with { Spec = spec });
        var hover = markup[markup.IndexOf("<g class=\"pictor-hover", StringComparison.Ordinal)..];

        Assert.Equal(5, Regex.Count(hover, "<g class=\"pictor-tk\\d\"><g class=\"pictor-s0\"><g><foreignObject"));
        Assert.Equal(5, Regex.Count(hover, "<g class=\"pictor-s0\"><g><foreignObject"));
    }

    [Fact]
    [Trait("AC", "AUDIT-31")]
    public async Task OffTrailNestBoxesAreNotDrawnAsync()
    {
        var example = await Reference.ExampleAsync("NestedTreemap");

        var written = await desktop.Session.Dispatch(
            () =>
            {
                var chart = (Treemap)WebChart.Create(example);
                chart.Zoom = "0";
                return WebChart.Write(chart, example.Spec.Width, example.Spec.Height).Html;
            },
            TestContext.Current.CancellationToken);
        var boxes = Boxes().Matches(written).Select(match => (Hidden: match.Groups[1].Success, Display: match.Groups[2].Value, Text: match.Groups[3].Value)).ToList();

        Assert.All(boxes, box => Assert.Equal((false, "inline-block"), (box.Hidden, box.Display)));
        Assert.Equal(["root", "axis"], boxes.Select(box => box.Text));
    }

    [Fact]
    [Trait("AC", "AUDIT-32")]
    public void ThePolarRadiusTicksOfTheDarkThemeAreReadable()
    {
        var dark = Theming.ChartTheme.Dark;

        Assert.Equal("#ccc", Theming.ChartTheme.Light.PolarTickText);
        Assert.Equal("#313131", dark.PolarAxisStroke);
        Assert.True(Contrast(dark.PolarTickText, "#09090b") >= 4.5, $"{Contrast(dark.PolarTickText, "#09090b"):0.00}:1");
        Assert.True(Contrast(dark.PolarTickText, "#000000") >= 4.5);
    }

    [Fact]
    [Trait("AC", "AUDIT-33")]
    public async Task ABrushsInputsComeAfterTheCategoriesAsync()
    {
        var example = await Reference.ExampleAsync("BrushWindowBarChart");
        var spec = example.Spec with { AccessibilityLayer = true, Brush = example.Spec.Brush! with { Fields = true } };
        var render = ChartRenderer.Create(Specs.SpecJson.Write(spec), static (_, _) => null!);

        var html = Slots.Render(render, example.Data, new Dictionary<string, object?>()).Html;
        var surface = html[..html.IndexOf("</svg>", StringComparison.Ordinal)];

        Assert.True(surface.IndexOf("pictor-hk0", StringComparison.Ordinal) < surface.IndexOf("class=\"pictor-b0\"", StringComparison.Ordinal));
        Assert.EndsWith("</div></foreignObject>", surface, StringComparison.Ordinal);
        Assert.Contains(".recharts-surface:has(.pictor-b0:active) .recharts-brush>.recharts-brush-slide+g", html, StringComparison.Ordinal);
    }

    private static double Contrast(string first, string second)
    {
        static double Luminance(string hex)
        {
            static double Channel(int value) => value / 255.0 is var c && c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            var full = hex.Length == 4 ? string.Concat(hex[1], hex[1], hex[2], hex[2], hex[3], hex[3]) : hex[1..];
            var rgb = Convert.FromHexString(full);
            return (0.2126 * Channel(rgb[0])) + (0.7152 * Channel(rgb[1])) + (0.0722 * Channel(rgb[2]));
        }

        var (a, b) = (Luminance(first), Luminance(second));
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    [GeneratedRegex("<div class=\"recharts-treemap-nest-index-box[^\"]*\"( hidden)? style=\"cursor:pointer;display:([a-z-]+);[^>]*>([^<]*)<")]
    private static partial Regex Boxes();
}
