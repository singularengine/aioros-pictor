using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;

namespace Aioros.Pictor.Tests.Web;

public sealed partial class FocusWebTests
{
    private const string Gate = ".recharts-wrapper:not(:has(:focus-visible)) .pictor-hover>:hover+g{display:inline}";

    [Theory]
    [Trait("AC", "TOUCH-14")]
    [InlineData("SimpleLineChart", true)]
    [InlineData("SimpleAreaChart", true)]
    [InlineData("SimpleBarChart", true)]
    [InlineData("LineBarAreaComposedChart", true)]
    [InlineData("SpecifiedDomainRadarChart", true)]
    [InlineData("SimpleRadialBarChart", true)]
    [InlineData("SimpleScatterChart", false)]
    [InlineData("TwoSimplePieChart", false)]
    [InlineData("FunnelChart", false)]
    [InlineData("ColorPanelTreemap", false)]
    [InlineData("SimpleSankey", false)]
    [InlineData("SimpleSunburst", false)]
    [InlineData("TooltipClick", false)]
    public async Task AFingerMovesAnAxisTooltipThroughTheFrameworksTouchFollowAsync(string name, bool follows)
    {
        var markup = Reference.Pictor(await Reference.ExampleAsync(name));
        var groups = HitGroups(markup);

        if (follows)
        {
            Assert.NotEmpty(groups);
            Assert.Equal(name.Contains("Radar", StringComparison.Ordinal) || name.Contains("Radial", StringComparison.Ordinal) ? " data-aioros-follow=\"capture\"" : " data-aioros-follow", HoverLayer(markup));
            Assert.All(groups, group => Assert.Equal(" tabindex=\"-1\"", group));
            Assert.Contains(".pictor-hover>g{outline:none}", markup, StringComparison.Ordinal);
            Assert.Contains(".pictor-hover>:focus-visible+g{display:inline}", markup, StringComparison.Ordinal);
            for (var i = 0; i < groups.Count; i++)
            {
                Assert.Contains($"svg:has(>g>.pictor-hk{i}:focus-visible)>g>.pictor-c{i}", markup, StringComparison.Ordinal);
            }
        }
        else
        {
            Assert.DoesNotContain("data-aioros-follow", markup, StringComparison.Ordinal);
            Assert.DoesNotContain(":focus-visible+g", markup, StringComparison.Ordinal);
            Assert.All(groups, group => Assert.Equal(name == "TooltipClick" ? " tabindex=\"-1\"" : string.Empty, group));
        }
    }

    [Fact]
    [Trait("AC", "TOUCH-15")]
    public async Task AVerticalChartCapturesTheFingerAsync()
    {
        var vertical = Reference.Pictor(await Reference.ExampleAsync("VerticalBarChart"));
        var horizontal = Reference.Pictor(await Reference.ExampleAsync("SimpleBarChart"));

        Assert.Equal(" data-aioros-follow=\"capture\"", HoverLayer(vertical));
        Assert.All(HitGroups(vertical), group => Assert.Equal(" tabindex=\"-1\"", group));
        Assert.Equal(" data-aioros-follow", HoverLayer(horizontal));
        Assert.DoesNotContain("capture", horizontal, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "TOUCH-16")]
    public async Task HoverRulesYieldToAVisibleFocusAsync()
    {
        var line = Reference.Pictor(await Reference.ExampleAsync("SimpleLineChart"));
        var initial = Reference.Pictor(await Reference.ExampleAsync("TooltipDefaultIndex"));
        var pie = Reference.Pictor(await Reference.ExampleAsync("TwoSimplePieChart"));

        foreach (var markup in (string[])[line, initial, pie])
        {
            Assert.Contains(Gate, markup, StringComparison.Ordinal);
            Assert.DoesNotContain("}.pictor-hover>:hover", markup, StringComparison.Ordinal);
            var gated = GatedCursors().Match(markup).Value;
            Assert.Equal(Regex.Count(markup, ":hover\\)>\\.pictor-cursors"), Regex.Count(gated, ":hover\\)>\\.pictor-cursors"));
        }

        Assert.Contains(".recharts-wrapper:not(:has(:focus-visible)){svg:has(>g>.pictor-hk0:hover)>g>.pictor-c0,", line, StringComparison.Ordinal);
        Assert.DoesNotContain(".pictor-cursors>.pictor-c", pie, StringComparison.Ordinal);
        Assert.Contains(".pictor-d3:not(:has(.pictor-hover>:is(:hover,[tabindex=\"0\"]),:focus-visible)) .pictor-tk3{display:inline}.pictor-d3:not(:has(.pictor-hover>:is(:hover,[tabindex=\"0\"]),:focus-visible)) .pictor-c3{display:inline}", initial, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "TOUCH-17")]
    public async Task SyncedChartsFollowAFocusedStateAsync()
    {
        var line = await Reference.ExampleAsync("SimpleLineChart");
        var bar = await Reference.ExampleAsync("SimpleBarChart");
        var byIndex = Reference.Pictor(line with { Spec = line.Spec with { SyncId = "a" } });
        var byValue = Reference.Pictor(bar with { Spec = bar.Spec with { SyncId = "b", SyncMethod = "value" } });
        var pageC = Svg.SyncMarkup.ValueClass("Page C");

        Assert.Contains(":root:not(:has(.recharts-wrapper :focus-visible)){&:has(.pictor-y-a .pictor-hk0:hover) .pictor-y-a .pictor-tk0", byIndex, StringComparison.Ordinal);
        Assert.Contains(":root{&:has(.pictor-y-a .pictor-hk0:focus-visible) .pictor-y-a .pictor-tk0", byIndex, StringComparison.Ordinal);
        Assert.Contains("&:has(.pictor-y-a .pictor-hk2:focus-visible) .pictor-y-a .pictor-c2", byIndex, StringComparison.Ordinal);
        Assert.Contains($"&:has(.pictor-y-b [class^=pictor-hk].{pageC}:focus-visible) .pictor-y-b [class^=pictor-tk].{pageC}", byValue, StringComparison.Ordinal);
        Assert.Contains($"&:has(.pictor-y-b [class^=pictor-hk].{pageC}:focus-visible) .pictor-y-b .pictor-cursors>.{pageC}", byValue, StringComparison.Ordinal);
        Assert.Contains($"<g tabindex=\"-1\" class=\"pictor-hk2 {pageC}\">", byValue, StringComparison.Ordinal);
        Assert.DoesNotContain(":root:has(", byIndex + byValue, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "TOUCH-18")]
    public async Task AFingerMovesTheTooltipOverCommandButtonsAsync()
    {
        var bar = await Reference.ExampleAsync("SimpleBarChart");
        var pie = await Reference.ExampleAsync("TwoSimplePieChart");
        var commanded = Commands(bar.Spec with { ClickCommand = "pick" }, "pick");
        var markup = Reference.Pictor(bar with { Spec = commanded });
        var pieMarkup = Reference.Pictor(pie with { Spec = pie.Spec with { Polar = pie.Spec.Polar! with { Pies = [.. pie.Spec.Polar.Pies.Select(one => one with { ClickCommand = "pick" })] } } });

        Assert.Contains("data-aioros-follow", Surface().Match(markup).Value, StringComparison.Ordinal);
        Assert.Equal(string.Empty, HoverLayer(markup));
        Assert.Contains("<button tabindex=\"0\" class=\"pictor-h0\"", markup, StringComparison.Ordinal);
        Assert.Contains(".recharts-wrapper:not(:has(:focus-visible)){&:has(.pictor-h0:hover) .pictor-tk0", markup, StringComparison.Ordinal);
        Assert.Contains(".recharts-wrapper{&:has(.pictor-h0:focus-visible) .pictor-tk0", markup, StringComparison.Ordinal);
        Assert.Contains("&:has(.pictor-h0:focus-visible) .pictor-c0", markup, StringComparison.Ordinal);
        Assert.DoesNotContain(":is(:hover,:focus-visible)", markup, StringComparison.Ordinal);
        Assert.Contains("<button", pieMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("data-aioros-follow", pieMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("<button tabindex", pieMarkup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "KEYBOARD-11")]
    public async Task EscapeKeepsTheCursorAndDotsWithoutTheBoxAsync()
    {
        var line = await Reference.ExampleAsync("SimpleLineChart");
        var initial = await Reference.ExampleAsync("TooltipDefaultIndex");
        var markup = Reference.Pictor(line with { Spec = line.Spec with { AccessibilityLayer = true } });
        var defaulted = Reference.Pictor(initial with { Spec = initial.Spec with { AccessibilityLayer = true } });
        var plain = Reference.Pictor(line);

        const string Left = ".recharts-surface:has(>.pictor-hover>[tabindex=\"0\"]):not(:has(>.pictor-hover>:is(:hover,:focus))){&>.pictor-hover foreignObject{display:none}&>.pictor-hover>[tabindex=\"0\"]+g{display:inline}&:has(>g>.pictor-hk0[tabindex=\"0\"])>g>.pictor-c0,";
        Assert.Contains(Left, markup, StringComparison.Ordinal);
        Assert.Contains("&:has(>g>.pictor-hk6[tabindex=\"0\"])>g>.pictor-c6{display:inline}}", markup, StringComparison.Ordinal);
        Assert.Contains(Left, defaulted, StringComparison.Ordinal);
        Assert.Contains(".pictor-d3:not(:has(.pictor-hover>:is(:hover,[tabindex=\"0\"]),:focus-visible))", defaulted, StringComparison.Ordinal);
        Assert.DoesNotContain("foreignObject{display:none}", plain, StringComparison.Ordinal);
    }

    [Theory]
    [Trait("AC", "KEYBOARD-08")]
    [InlineData("SimpleLineChart")]
    [InlineData("SimpleAreaChart")]
    [InlineData("SimpleBarChart")]
    [InlineData("LineBarAreaComposedChart")]
    public async Task TheArrowKeysMoveTheFocusAmongTheCategoriesOnTheWebAsync(string name)
    {
        var example = await Reference.ExampleAsync(name);
        var accessible = Reference.Pictor(example with { Spec = example.Spec with { AccessibilityLayer = true } });
        var plain = Reference.Pictor(example);
        var groups = HitGroups(accessible);

        Assert.Contains(" role=\"application\"", accessible, StringComparison.Ordinal);
        Assert.DoesNotContain("tabindex", Surface().Match(accessible).Value, StringComparison.Ordinal);
        Assert.Equal(" data-aioros-follow data-aioros-roving=\"horizontal\" data-aioros-roving-escape data-aioros-roving-ends=\"off\"", HoverLayer(accessible));
        Assert.Equal(" tabindex=\"00\"", groups[0]);
        Assert.All(groups.Skip(1), group => Assert.Equal(" tabindex=\"-1\"", group));
        Assert.Equal(1, TabStops().Count(accessible));
        Assert.DoesNotMatch("(?<!\\[)tabindex=\"0\"", accessible);
        Assert.DoesNotContain("surface:focus", accessible, StringComparison.Ordinal);
        Assert.DoesNotContain("data-aioros-roving", plain, StringComparison.Ordinal);
        Assert.All(HitGroups(plain), group => Assert.Equal(" tabindex=\"-1\"", group));
    }

    [Fact]
    [Trait("AC", "KEYBOARD-09")]
    public async Task TheTabStopIsTheDefaultIndexAsync()
    {
        var example = await Reference.ExampleAsync("TooltipDefaultIndex");
        var groups = HitGroups(Reference.Pictor(example with { Spec = example.Spec with { AccessibilityLayer = true } }));

        Assert.Equal([3], groups.Select((group, index) => (group, index)).Where(pair => pair.group == " tabindex=\"00\"").Select(pair => pair.index));
        Assert.Equal(groups.Count - 1, groups.Count(group => group == " tabindex=\"-1\""));
    }

    [Theory]
    [Trait("AC", "KEYBOARD-10")]
    [InlineData("VerticalBarChart")]
    [InlineData("TwoSimplePieChart")]
    [InlineData("SpecifiedDomainRadarChart")]
    [InlineData("SimpleRadialBarChart")]
    [InlineData("FunnelChart")]
    public async Task OtherChartsKeepAFocusableSurfaceWithoutRovingAsync(string name)
    {
        var example = await Reference.ExampleAsync(name);
        var markup = Reference.Pictor(example with { Spec = example.Spec with { AccessibilityLayer = true } });

        Assert.Contains(" tabindex=\"0\" role=\"application\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("data-aioros-roving", markup, StringComparison.Ordinal);
        Assert.Equal(1 + Regex.Count(markup, "recharts-pie\" tabindex=\"0\""), TabStops().Count(markup));
    }

    [Fact]
    [Trait("AC", "KEYBOARD-12")]
    public async Task HomeAndEndAreLeftToThePageAsRechartsLeavesThemAsync()
    {
        var line = await Reference.ExampleAsync("SimpleLineChart");
        var scatter = await Reference.ExampleAsync("DetailsKeyboardScatterChart");

        foreach (var markup in (string[])[Reference.Pictor(line with { Spec = line.Spec with { AccessibilityLayer = true } }), Reference.Pictor(scatter)])
        {
            var roving = Regex.Matches(markup, "<g[^>]*data-aioros-roving=[^>]*>");
            Assert.NotEmpty(roving);
            Assert.All(roving, element => Assert.Contains("data-aioros-roving-ends=\"off\"", element.Value, StringComparison.Ordinal));
        }
    }

    private static Specs.ChartSpec Commands(Specs.ChartSpec spec, string command) =>
        spec with { Series = [.. spec.Series.Select(series => series with { ClickCommand = command })] };

    /// <summary>The hover layer's attributes after its class and pointer-follow properties (the framework's behaviors).</summary>
    private static string HoverLayer(string markup) => HoverTag().Match(markup).Groups[1].Value;

    /// <summary>The tab index attribute of each hit group (or hit shape) of the hover layer, in order.</summary>
    private static List<string> HitGroups(string markup)
    {
        var groups = new List<string>();
        for (var i = 0; HoverComparison.Group(markup, markup.Contains("class=\"pictor-ck", StringComparison.Ordinal) ? "pictor-clicks" : "pictor-hits", i) is { Length: > 0 } group; i++)
        {
            groups.Add(TabIndex().Match(group[..group.IndexOf('>', StringComparison.Ordinal)]).Value);
        }

        return groups;
    }

    [GeneratedRegex("<g class=\"pictor-hover[^\"]*\"(?: style=\"[^\"]*\")?([^>]*)>")]
    private static partial Regex HoverTag();

    [GeneratedRegex(" tabindex=\"-?\\d+\"")]
    private static partial Regex TabIndex();

    [GeneratedRegex("<svg class=\"recharts-surface\"[^>]*>")]
    private static partial Regex Surface();

    [GeneratedRegex("(?<!\\[)tabindex=\"00?\"")]
    private static partial Regex TabStops();

    [GeneratedRegex("\\.recharts-wrapper:not\\(:has\\(:focus-visible\\)\\)\\{[^}]*\\}")]
    private static partial Regex GatedCursors();
}
