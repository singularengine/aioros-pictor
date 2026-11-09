using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Tests.Audit;

public sealed partial class MarkupSafetyTests
{
    private const string Injected = "ok x\" onmouseover=\"alert(1) pictor-hits";

    [Fact]
    [Trait("AC", "AUDIT-01")]
    public async Task ClassNamesAreWrittenAsClassTokensOnlyAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var plain = Reference.Pictor(example);
        var spec = Classed(example.Spec, Injected);

        var markup = Reference.Pictor(example with { Spec = spec });

        Assert.DoesNotContain("onmouseover", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("alert", markup, StringComparison.Ordinal);
        Assert.Equal(Count(plain, "pictor-hits"), Count(markup, "pictor-hits"));
        Assert.Contains("class=\"recharts-wrapper ok\"", markup, StringComparison.Ordinal);
        Assert.Contains("recharts-rectangle ok\"", markup, StringComparison.Ordinal);
        Assert.Contains("recharts-xAxis xAxis ok\"", markup, StringComparison.Ordinal);
        Assert.Contains("recharts-default-tooltip ok\"", markup, StringComparison.Ordinal);
        Assert.Contains("<p class=\"ok\"", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"ok\"/>", markup, StringComparison.Ordinal);
        Assert.All(Attributes().Matches(markup), attribute => Assert.DoesNotContain('"', attribute.Groups[1].Value));

        var safe = Reference.Pictor(example with { Spec = Classed(example.Spec, "sales  q-1") });
        Assert.Contains("recharts-rectangle sales  q-1\"", safe, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AUDIT-02")]
    public async Task OnlyColorsOfTheGrammarReachTheMarkupAsync()
    {
        string?[] colors = ["red;position:fixed;inset:0", "url(https://evil.example/x)", "#82ca9d", "expression(alert(1))", "rgba(1, 2, 3, 0.5)", "hsl(120deg 50% 50% / 0.5)", "#ffc658"];
        var radial = await Reference.ExampleAsync("SimpleRadialBarChart");
        var rows = Reference.Rows(radial).Select((row, i) => (object?)new Dictionary<string, object?>(row) { ["fill"] = colors[i] }).ToList();
        var radialMarkup = Reference.Pictor(radial with
        {
            Data = rows,
            Spec = radial.Spec with { Tooltip = radial.Spec.Tooltip! with { CursorStroke = "red;background:url(https://evil.example/c)", CursorStrokeDashArray = "1;position:fixed" } },
        });

        var pie = await Reference.ExampleAsync("TwoSimplePieChart");
        var pieSpec = pie.Spec with { Polar = pie.Spec.Polar! with { Pies = [.. pie.Spec.Polar.Pies.Select(p => p with { FillKey = "fill", Data = Colored(p.Data!, colors) })] } };
        var pieMarkup = Reference.Pictor(pie with { Spec = pieSpec });

        var funnel = await Reference.ExampleAsync("FunnelChart");
        var funnelSpec = funnel.Spec with { Funnels = [.. funnel.Spec.Funnels.Select(f => f with { Data = Colored(f.Data!, colors) })] };
        var funnelMarkup = Reference.Pictor(funnel with { Spec = funnelSpec });

        foreach (var markup in (string[])[radialMarkup, pieMarkup, funnelMarkup])
        {
            Assert.DoesNotContain("position:fixed", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("evil.example", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("expression", markup, StringComparison.Ordinal);
            Assert.Contains("fill=\"#82ca9d\"", markup, StringComparison.Ordinal);
            Assert.Contains("fill=\"rgba(1, 2, 3, 0.5)\"", markup, StringComparison.Ordinal);
            foreach (Match style in Styles().Matches(markup))
            {
                foreach (Match color in StyleColors().Matches(style.Groups[1].Value))
                {
                    Assert.Matches(Grammar(), color.Groups[1].Value);
                }
            }
        }

        Assert.Contains("color:#82ca9d", radialMarkup, StringComparison.Ordinal);
        Assert.Contains("fill=\"#8884d8\"", pieMarkup, StringComparison.Ordinal);
    }

    private static ChartSpec Classed(ChartSpec spec, string className) => spec with
    {
        ClassName = className,
        Grid = (spec.Grid ?? new GridSpec()) with { ClassName = className },
        XAxes = [.. spec.XAxes.Select(axis => axis with { ClassName = className })],
        Series = [.. spec.Series.Select(series => series with { ClassName = className })],
        Tooltip = spec.Tooltip! with { WrapperClassName = className, LabelClassName = className },
    };

    private static DataTable Colored(DataTable table, string?[] colors) =>
        table with { Columns = table.Columns.SetItem("fill", [.. Enumerable.Range(0, table.Count).Select(i => colors[i % colors.Length])]) };

    private static int Count(string markup, string text) => Regex.Count(markup, Regex.Escape(text));

    [GeneratedRegex("class=\"([^\"]*)\"")]
    private static partial Regex Attributes();

    [GeneratedRegex("style=\"([^\"]*)\"")]
    private static partial Regex Styles();

    [GeneratedRegex(@"(?:^|;)(?:color|stroke|fill|background):([^;]*)")]
    private static partial Regex StyleColors();

    [GeneratedRegex(@"^(#[0-9a-fA-F]{3,8}|[a-zA-Z]+|(rgba?|hsla?)\([0-9., /%a-z+-]*\)|url\(#[\w-]+\)|var\(--[\w-]+\))$")]
    private static partial Regex Grammar();
}
