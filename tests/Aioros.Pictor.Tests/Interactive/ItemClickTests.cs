using System.Text.RegularExpressions;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Tests.Desktop;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Interactive;

public sealed class ItemClickTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "INTERACT-04")]
    public async Task EveryFamilysItemsAreButtonsOverTheirHoverAreasAsync()
    {
        var pie = await Reference.ExampleAsync("TwoLevelPieChart");
        var funnel = await Reference.ExampleAsync("FunnelChart");
        var sankey = await Reference.ExampleAsync("SimpleSankey");
        var sunburst = await Reference.ExampleAsync("SimpleSunburst");
        var treemap = await Reference.ExampleAsync("SimpleTreemap");

        var written = await desktop.Session.Dispatch(
            () =>
            {
                var pies = WebChart.Create(pie);
                pies.Elements.OfType<Pie>().First().ClickCommand = new Picks();
                var funnels = WebChart.Create(funnel);
                funnels.Elements.OfType<Funnel>().Single().ClickCommand = new Picks();
                var sankeys = WebChart.Create(sankey);
                sankeys.ItemClickCommand = new Picks();
                var sunbursts = WebChart.Create(sunburst);
                sunbursts.ItemClickCommand = new Picks();
                var treemaps = WebChart.Create(treemap);
                treemaps.ClickCommand = new Picks();
                return (Pie: WebChart.Write(pies, pie.Spec.Width, pie.Spec.Height), Funnel: WebChart.Write(funnels, funnel.Spec.Width, funnel.Spec.Height),
                    Sankey: WebChart.Write(sankeys, sankey.Spec.Width, sankey.Spec.Height), Sunburst: WebChart.Write(sunbursts, sunburst.Spec.Width, sunburst.Spec.Height),
                    Treemap: WebChart.Write(treemaps, treemap.Spec.Width, treemap.Spec.Height));
            },
            TestContext.Current.CancellationToken);
        static List<string?> Parameters(Slots.Rendered rendered) => [.. rendered.Reserved.Select(reserved => reserved.Parameter)];

        var inner = pie.Spec.Polar!.Pies[0].Data?.Count ?? Reference.Rows(pie).Count;
        Assert.Equal(Enumerable.Range(0, inner).Select(i => (string?)i.ToString(System.Globalization.CultureInfo.InvariantCulture)), Parameters(written.Pie));
        Assert.Contains("style=\"clip-path:path('M", written.Pie.Html, StringComparison.Ordinal);
        Assert.Equal(5, Parameters(written.Funnel).Count);
        Assert.Contains("link:0", Parameters(written.Sankey));
        Assert.Contains("node:0", Parameters(written.Sankey));
        Assert.All(Parameters(written.Sunburst), parameter => Assert.True(int.TryParse(parameter, out _)));
        Assert.Contains("0.0", Parameters(written.Treemap));
        Assert.DoesNotContain("0", Parameters(written.Treemap));
        foreach (var (name, html) in (ReadOnlySpan<(string, string)>)[("pie", written.Pie.Html), ("funnel", written.Funnel.Html), ("sankey", written.Sankey.Html), ("sunburst", written.Sunburst.Html), ("treemap", written.Treemap.Html)])
        {
            Assert.Empty(Uncovered(name, html));
        }
    }

    /// <summary>
    /// The buttons of a chart whose area is not exactly their hover state's hit shape, or, where the button is the state's
    /// hit area itself or the chart has no hover states, a drawn shape of their own (each shape taken once): a button clipped to a path whose numbers differ from the shape's path, or a box
    /// whose rectangle differs from the shape's rectangle (0.01 px).
    /// </summary>
    private static List<string> Uncovered(string name, string html)
    {
        var differences = new List<string>();
        var buttons = Regex.Matches(html, "<foreignObject((?: [a-z-]+=\"[^\"]*\")*)><button((?: [a-z-]+=\"[^\"]*\")*)", RegexOptions.None, TimeSpan.FromSeconds(1));
        var drawn = Demo.MarkupComparison.Parse(html).Where(element => element.Tag is "path" or "rect" && element.Classes.StartsWith("recharts-", StringComparison.Ordinal)).Select(Band).ToList();
        var states = 0;
        foreach (Match button in buttons)
        {
            var box = Demo.MarkupComparison.Parse($"<svg><rect{button.Groups[1].Value}/></svg>")[1].Attributes;
            var own = Demo.MarkupComparison.Parse($"<svg><g{button.Groups[2].Value}/></svg>")[1].Attributes;
            if (own.ContainsKey("hidden"))
            {
                continue;
            }

            states++;
            var hit = own.GetValueOrDefault("class") is ['p', 'i', 'c', 't', 'o', 'r', '-', 'h', .. var digits] && int.TryParse(digits, out var state)
                ? Demo.MarkupComparison.Parse(Demo.HoverComparison.Group(html, "pictor-hits", state)).FirstOrDefault(element => element.Classes.Contains("pictor-hit", StringComparison.Ordinal))
                : null;
            if (hit is not null)
            {
                differences.AddRange(Covered(hit, own, box).Select(difference => $"{name} button {own.GetValueOrDefault("aria-label")}: {difference}"));
                continue;
            }

            var shape = drawn.FindIndex(path => Covered(path, own, box).Count == 0);
            differences.AddRange(shape < 0 ? [$"{name} button {own.GetValueOrDefault("aria-label")}: no drawn shape has its area"] : []);
            drawn.RemoveAt(Math.Max(shape, 0));
        }

        if (states == 0)
        {
            differences.Add($"{name}: no item button");
        }

        return differences;
    }

    /// <summary>The differences between a button's area (its clip path, or its box) and a shape's area (its path, or its rectangle), both read as paths.</summary>
    private static List<string> Covered(Demo.MarkupElement shape, Dictionary<string, string> own, Dictionary<string, string> box)
    {
        var area = own.GetValueOrDefault("style") is { } style && Regex.Match(style, @"clip-path:path\('([^']*)'\)", RegexOptions.None, TimeSpan.FromSeconds(1)) is { Success: true } clip
            ? clip.Groups[1].Value
            : Rectangle(box);
        var outline = shape.Tag == "rect" ? Rectangle(shape.Attributes) : shape.Attributes.GetValueOrDefault("d") ?? string.Empty;
        var found = new List<string>();
        Demo.MarkupComparison.CompareElements(Path(outline), Path(area), found);
        return found;

        static Demo.MarkupElement Path(string d) => new("path", new(StringComparer.Ordinal) { ["class"] = "area", ["d"] = d, ["fill"] = "none" }, string.Empty);
    }

    /// <summary>A Sankey link's hover area: its curve shifted up and down by half its width (decisions, "Hover"); any other shape as drawn.</summary>
    private static Demo.MarkupElement Band(Demo.MarkupElement shape)
    {
        if (shape.Classes != "recharts-sankey-link")
        {
            return shape;
        }

        var n = Regex.Matches(shape.Attributes["d"], @"-?[\d.]+", RegexOptions.None, TimeSpan.FromSeconds(1)).Select(match => double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var h = double.Parse(shape.Attributes["stroke-width"], System.Globalization.CultureInfo.InvariantCulture) / 2;
        return shape with { Attributes = new(shape.Attributes) { ["d"] = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"M{n[0]},{n[1] - h}C{n[2]},{n[3] - h},{n[4]},{n[5] - h},{n[6]},{n[7] - h}L{n[6]},{n[7] + h}C{n[4]},{n[5] + h},{n[2]},{n[3] + h},{n[0]},{n[1] + h}Z") } };
    }

    private static string Rectangle(Dictionary<string, string> rect) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"M{rect.GetValueOrDefault("x") ?? "0"},{rect.GetValueOrDefault("y") ?? "0"}h{rect["width"]}v{rect["height"]}h-{rect["width"]}Z");

    [Fact]
    [Trait("AC", "INTERACT-05")]
    public async Task ALegendsCommandIsAButtonOverEachItemAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");

        var (written, ran, concealed) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = WebChart.Create(example);
                var picks = new Picks();
                var legend = chart.Elements.OfType<Legend>().Single();
                legend.ClickCommand = picks;
                legend.Toggles = true;
                var web = WebChart.Write(chart);
                var window = new Window { Width = 500, Height = 300, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var layout = (ChartLayout)chart.Plot();
                var item = layout.Legend!.Items[1];
                Click(window, new Point(item.X + 5, item.Y + (layout.Legend.RowHeight / 2)));
                var result = (web, picks.Ran.ToList(), chart.Concealed);
                window.Close();
                return result;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal([(string?)"0", "1"], written.Reserved.Select(reserved => reserved.Parameter));
        Assert.Contains("<button class=\"pictor-lc\" aria-label=\"uv\" type=\"submit\" name=\"_btn\" value=\"1|1\"></button></li>", written.Html, StringComparison.Ordinal);
        Assert.Equal(["1"], ran);
        Assert.Empty(concealed);
    }

    [Fact]
    [Trait("AC", "INTERACT-06")]
    public async Task ClicksRunTheSameCommandsOnTheDesktopAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");

        var (bar, chartRan, crossed) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = WebChart.Create(example);
                var bars = new Picks();
                var categories = new Picks();
                chart.Elements.OfType<Bar>().First(series => series.DataKey == "pv").ClickCommand = bars;
                chart.ClickCommand = categories;
                var window = new Window { Width = 500, Height = 300, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var layout = (ChartLayout)chart.Plot();
                var pv = layout.Series.First(series => series.Spec.DataKey == "pv");
                var third = pv.Bars[2];
                Click(window, new Point(third.X + (third.Width / 2), third.Y + (third.Height / 2)));
                var onBar = (bars.Ran.ToList(), categories.Ran.ToList());
                Click(window, new Point(third.X + (third.Width / 2), layout.Top + 2));
                var onCategory = categories.Ran.ToList();
                var fourth = pv.Bars[3];
                window.MouseDown(new Point(third.X + (third.Width / 2), third.Y + (third.Height / 2)), MouseButton.Left, RawInputModifiers.None);
                window.MouseUp(new Point(fourth.X + (fourth.Width / 2), fourth.Y + (fourth.Height / 2)), MouseButton.Left, RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
                var across = (bars.Ran.Count, categories.Ran.Count);
                window.Close();
                return (onBar, onCategory, across);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(["2"], bar.Item1);
        Assert.Empty(bar.Item2);
        Assert.Equal(["2"], chartRan);
        Assert.Equal((1, 1), crossed);
    }

    private static void Click(Window window, Point point)
    {
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }
}
