using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Tests.Desktop;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;

namespace Aioros.Pictor.Tests.Charts;

public sealed partial class CartesianFeatureTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "CHARTS-16")]
    public async Task AVerticalLineChartMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("VerticalLineChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-17")]
    public async Task AVerticalComposedChartMatchesRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("VerticalComposedChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-18")]
    public async Task VerticalBarsMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("VerticalBarChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-19")]
    public async Task ErrorBarsOnABarAndALineMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("ComposedChartWithErrorBars");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-20")]
    public async Task ReferenceAreasAndDotsMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("LineChartWithReferenceAreas");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "CHARTS-21")]
    public async Task TheHoveredBarIsRedrawnWithItsActiveBarPropsAsync()
    {
        var markup = Reference.Pictor(await Reference.ExampleAsync("VerticalBarChart"));
        var tips = Reference.States(markup, "pictor-tips");

        Assert.Equal(7, tips.Count);
        Assert.All(tips, tip =>
        {
            Assert.Contains("stroke=\"blue\"", tip, StringComparison.Ordinal);
            Assert.Contains("fill=\"pink\"", tip, StringComparison.Ordinal);
            Assert.Contains("stroke=\"purple\"", tip, StringComparison.Ordinal);
            Assert.Contains("fill=\"gold\"", tip, StringComparison.Ordinal);
        });
        Assert.DoesNotContain("pink", Layer(markup, "recharts-layer recharts-bar"), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "CHARTS-22")]
    public async Task ActiveDotPropsAreDrawnAndAFalseActiveDotIsNotAsync()
    {
        var styled = Reference.States(Reference.Pictor(await Reference.ExampleAsync("ComposedChartWithErrorBars")), "pictor-tips");
        var hidden = Reference.States(Reference.Pictor(await Reference.ExampleAsync("LineChartWithReferenceAreas")), "pictor-tips");

        Assert.All(styled, tip => Assert.Matches(ActiveDot(), tip));
        Assert.All(hidden, tip => Assert.Equal(1, Regex.Count(tip, "<circle")));
    }

    [Fact]
    [Trait("AC", "CHARTS-23")]
    public async Task VerticalChartsHoverByCategoryDownTheYAxisAsync()
    {
        var lines = await Reference.ExampleAsync("VerticalLineChart");
        var bars = await Reference.ExampleAsync("VerticalBarChart");
        var lineMarkup = Reference.Pictor(lines);
        var barMarkup = Reference.Pictor(bars);
        var lineHits = Reference.States(lineMarkup, "pictor-cursors");
        var barHits = Reference.States(barMarkup, "pictor-cursors");
        var lineLayout = (ChartLayout)Aioros.Pictor.Painting.Plots.Compute(lines.Spec, Aioros.Pictor.Data.ChartData.From(lines.Data, static (_, _) => null));
        var barLayout = (ChartLayout)Aioros.Pictor.Painting.Plots.Compute(bars.Spec, Aioros.Pictor.Data.ChartData.From(bars.Data, static (_, _) => null));
        var lineTip = HoverComparison.PictorTip(lineMarkup, 2);
        var barTip = HoverComparison.PictorTip(barMarkup, 2);
        (string, string)[] shared = [("pv : 9800", "#8884d8"), ("uv : 2000", "#82ca9d")];

        Assert.Equal(7, lineHits.Count);
        Assert.Equal(7, barHits.Count);
        Assert.Matches(HorizontalCursor(), lineHits[2]);
        Assert.Contains("recharts-rectangle recharts-tooltip-cursor", barHits[2], StringComparison.Ordinal);
        Assert.Equal("Page C", lineTip.Label);
        Assert.Equal(shared, lineTip.Items);
        Assert.Equal("Page C", barTip.Label);
        Assert.Equal(shared, barTip.Items);
        Assert.Equal(((lineLayout.Left + lineLayout.Spec.Width) / 2) + 10, lineTip.X, 0.001);
        Assert.Equal(lineLayout.Categories!.Category(2) + 10, lineTip.Y, 0.001);
        Assert.Equal(((barLayout.Left + barLayout.Spec.Width) / 2) + 10, barTip.X, 0.001);
        Assert.Equal(barLayout.Categories!.Category(2) + (barLayout.Categories.Bandwidth / 2) + 10, barTip.Y, 0.001);
    }

    [Fact]
    [Trait("AC", "CHARTS-24")]
    public async Task HoveringAVerticalCategoryShowsItsTooltipOnTheDesktopAsync()
    {
        var example = await Reference.ExampleAsync("VerticalBarChart");
        var markup = Reference.Pictor(example);
        var tip = HoverComparison.PictorTip(markup, 2);
        var band = MarkupComparison.Parse(Reference.States(markup, "pictor-cursors")[2]).Single(element => element.Tag == "path");

        var (active, outside, hover, same, layout) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var window = new Window { Width = 500, Height = 300, Content = chart };
                window.Show();
                var before = HoverPixels.Frame(chart);
                var plot = (ChartLayout)chart.Plot();
                var axis = plot.Categories!;
                window.MouseMove(new Point(plot.Left + 20, axis.Category(2) + (axis.Bandwidth / 2)), RawInputModifiers.None);
                var hovered = chart.ActiveIndex;
                var pixels = new HoverPixels(before, HoverPixels.Frame(chart), 500);
                window.MouseMove(new Point(2, 2), RawInputModifiers.None);
                var none = chart.ActiveIndex;
                var unchanged = HoverPixels.Frame(chart).AsSpan().SequenceEqual(before);
                window.Close();
                return (hovered, none, pixels, unchanged, plot);
            },
            TestContext.Current.CancellationToken);
        var top = (int)Math.Ceiling(layout.Categories!.Category(2));
        var bottom = (int)Math.Floor(layout.Categories.Category(2) + layout.Categories.Bandwidth);
        var right = (int)(layout.Left + (layout.PlotWidth * 0.6));

        Assert.Equal(2, active);
        Assert.Contains("recharts-rectangle recharts-tooltip-cursor", band.Classes, StringComparison.Ordinal);
        Assert.True(hover.Painted((right, top + 1), (right, bottom - 1), "#ccc") > 0.8, "the band cursor covers the category where the bars leave it");
        var x = (int)layout.Left + 30;
        Assert.True(hover.Near(x, (int)tip.Y + 20, "#ccc"), $"the box's left border 10 px right of the pointer, at the category's y ({x}, {tip.Y}): {hover.At(x, (int)tip.Y + 20)}");
        Assert.True(hover.Written(x + 2, (int)tip.Y + 2, x + 110, (int)tip.Y + 80) > 300, "the box holds its label and both bars' lines");
        Assert.True(hover.Near((int)layout.Left + 3, (int)(layout.Categories.Category(2) + (layout.Categories.Bandwidth / 2)) - 4, "pink") || hover.Near((int)layout.Left + 3, (int)(layout.Categories.Category(2) + (layout.Categories.Bandwidth / 2)) + 4, "gold"), "the active bars take their activeBar fills");
        Assert.True(outside < 0);
        Assert.True(same, "leaving the plot draws the chart as before the hover");
    }

    private static string Layer(string markup, string name)
    {
        var start = markup.IndexOf($"<g class=\"{name}\"", StringComparison.Ordinal);
        Assert.True(start >= 0, $"no {name} layer");
        var depth = 0;
        for (var i = start; i < markup.Length; i++)
        {
            if (string.CompareOrdinal(markup, i, "<g", 0, 2) == 0)
            {
                depth++;
            }
            else if (string.CompareOrdinal(markup, i, "</g>", 0, 4) == 0 && --depth == 0)
            {
                return markup[start..(i + 4)];
            }
        }

        return markup[start..];
    }

    [GeneratedRegex(@"<circle r=""6"" stroke=""#fff"" stroke-width=""3"" fill=""#ff7300""")]
    private static partial Regex ActiveDot();

    [GeneratedRegex(@"<path class=""recharts-curve recharts-tooltip-cursor"" d=""M80,([\d.]+)L470,\1""")]
    private static partial Regex HorizontalCursor();
}
