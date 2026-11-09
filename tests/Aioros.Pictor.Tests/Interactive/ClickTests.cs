using Avalonia.Headless;
using Aioros.Pictor.Tests.Desktop;

namespace Aioros.Pictor.Tests.Interactive;

public sealed class ClickTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "INTERACT-01")]
    public async Task ABarsCommandIsWrittenAsAButtonOverEachBarAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");

        var (with, without) = await desktop.Session.Dispatch(
            () =>
            {
                var plain = WebChart.Write(WebChart.Create(example));
                var chart = WebChart.Create(example);
                chart.Elements.OfType<Bar>().First(bar => bar.DataKey == "pv").ClickCommand = new Picks();
                return (WebChart.Write(chart), plain);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(Reference.Pictor(example), without.Html);
        Assert.Empty(without.Reserved);
        Assert.Equal(7, with.Reserved.Count);
        Assert.Equal(Enumerable.Range(0, 7).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)), with.Reserved.Select(reserved => reserved.Parameter));
        Assert.All(with.Reserved, reserved => Assert.StartsWith("c", reserved.Key, StringComparison.Ordinal));
        Assert.Contains("<g class=\"pictor-cmds\">", with.Html, StringComparison.Ordinal);
        Assert.Contains("<button tabindex=\"0\" class=\"pictor-h0\" aria-label=\"pv, Page A\" type=\"submit\" name=\"_btn\" value=\"0|0\"></button>", with.Html, StringComparison.Ordinal);
        Assert.Contains(".recharts-wrapper:not(:has(:focus-visible)){&:has(.pictor-h0:hover) .pictor-tk0", with.Html, StringComparison.Ordinal);
        Assert.Equal(7, WebChart.Count(with.Html, "></button></foreignObject>"));
    }

    [Fact]
    [Trait("AC", "INTERACT-02")]
    public async Task OnlyTheButtonsShownAreWrittenAndTheFieldsKeepTheirIdsAsync()
    {
        var example = await Reference.ExampleAsync("BrushWindowBarChart");
        var spec = example.Spec with { Series = [.. example.Spec.Series.Select(series => series with { ClickCommand = "pick" })], Brush = example.Spec.Brush! with { Fields = true } };
        Slots.Rendered Write(Specs.ChartSpec written) => Slots.Render(Svg.ChartRenderer.Create(Specs.SpecJson.Write(written), static (_, _) => null!), example.Data, new Dictionary<string, object?>());

        var window = Write(spec);
        var whole = Write(spec with { Brush = spec.Brush with { StartIndex = null, EndIndex = null } });
        static IEnumerable<string?> Buttons(Slots.Rendered rendered) => rendered.Reserved.Where(reserved => reserved.Key == "pick").Select(reserved => reserved.Parameter);
        static IEnumerable<(int Id, string Key)> Fields(Slots.Rendered rendered) => rendered.Reserved.Where(reserved => reserved.Parameter is null).Select(reserved => (reserved.Id, reserved.Key));
        var shown = Enumerable.Range(5, 11).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToList();

        Assert.Equal([.. shown, .. shown], Buttons(window));
        Assert.Equal(78, Buttons(whole).Count());
        Assert.Equal(22, WebChart.Count(window.Html, "></button></foreignObject>"));
        Assert.Equal(0, WebChart.Count(window.Html, "<button hidden"));
        Assert.NotEmpty(Fields(window));
        Assert.Equal(Fields(whole), Fields(window));
    }

    [Fact]
    [Trait("AC", "INTERACT-03")]
    public async Task TheChartsCommandCoversEachCategoryAndALinesItsDotsAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");

        var written = await desktop.Session.Dispatch(
            () =>
            {
                var chart = WebChart.Create(example);
                chart.ClickCommand = new Picks();
                chart.Elements.OfType<Line>().First(line => line.DataKey == "uv").ClickCommand = new Picks();
                return WebChart.Write(chart);
            },
            TestContext.Current.CancellationToken);
        var recharts = Demo.MarkupComparison.Parse(await Reference.RechartsAsync("SimpleLineChart"));
        var dot = recharts.Where(element => element.Classes == "recharts-dot recharts-line-dot" && element.Attributes["stroke"] == "#82ca9d").ElementAt(2).Attributes;
        var plot = System.Text.RegularExpressions.Regex.Match(await Reference.RechartsAsync("SimpleLineChart"), "<clipPath[^>]*><rect x=\"([^\"]+)\" y=\"([^\"]+)\" height=\"([^\"]+)\" width=\"([^\"]+)\"");
        double Read(string text) => double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(["click", "click", "click", "click", "click", "click", "click"], written.Reserved.Take(7).Select(reserved => reserved.Key));
        Assert.Equal(14, written.Reserved.Count);
        Assert.Contains($"<foreignObject x=\"{Num(Read(dot["cx"]) - 6)}\" y=\"{Num(Read(dot["cy"]) - 6)}\" width=\"12\" height=\"12\"><button style=\"border-radius:50%\" tabindex=\"0\" class=\"pictor-h2\"", written.Html, StringComparison.Ordinal);
        Assert.Contains($"y=\"{Num(Read(plot.Groups[2].Value))}\" width=\"", written.Html, StringComparison.Ordinal);
        Assert.Contains($"height=\"{Num(Read(plot.Groups[3].Value))}\"><button tabindex=\"0\" class=\"pictor-h0\" aria-label=\"Page A\"", written.Html, StringComparison.Ordinal);
    }

    private static string Num(double value) => Aioros.Graphs.Formatting.JsNumber.ToShortestString(Math.Round(value, 3));

    [Fact]
    [Trait("AC", "PAINT-06")]
    public async Task ABarTakesItsFillAndOpacityFromItsItemAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        IReadOnlyDictionary<string, object?> Row(IReadOnlyDictionary<string, object?> row, int index) =>
            index == 1 ? new Dictionary<string, object?>(row) { ["color"] = "#ff0000", ["dim"] = 0.4 } : row;
        var html = await desktop.Session.Dispatch(
            () =>
            {
                var chart = WebChart.Create(example);
                chart.Data = Reference.Rows(example).Select(Row).ToList();
                var bar = chart.Elements.OfType<Bar>().First();
                (bar.FillKey, bar.OpacityKey) = ("color", "dim");
                bar.Parts.Add(new Cell());
                bar.Parts.Add(new Cell());
                bar.Parts.Add(new Cell { Fill = "#00ff00" });
                return WebChart.Write(chart).Html;
            },
            TestContext.Current.CancellationToken);
        var bars = System.Text.RegularExpressions.Regex.Matches(html, "<g class=\"recharts-layer recharts-bar-rectangle\"[^>]*>.*?</g>", System.Text.RegularExpressions.RegexOptions.Singleline).Select(static match => match.Value).Take(7).ToList();

        Assert.Contains("fill=\"#ff0000\"", bars[1], StringComparison.Ordinal);
        Assert.Contains("opacity=\"0.4\"", bars[1], StringComparison.Ordinal);
        Assert.Contains("fill=\"#00ff00\"", bars[2], StringComparison.Ordinal);
        Assert.DoesNotContain("opacity=", bars[0], StringComparison.Ordinal);
        Assert.DoesNotContain("#ff0000", bars[0], StringComparison.Ordinal);
    }

    private static async Task<Aioros.Pictor.Demo.RechartsExample> LinkedAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        string?[] links = ["/m/0", "https://example.com/1?a=1&b=2", "javascript:alert(1)", null];
        return example with { Data = Reference.Rows(example).Select((row, index) => (object?)new Dictionary<string, object?>(row) { ["link"] = index < links.Length ? links[index] : null }).ToList() };
    }

    [Fact]
    [Trait("AC", "INTERACT-10")]
    public async Task ABarWithALinkIsAnAnchorOverItsBoxAsync()
    {
        var example = await LinkedAsync();
        var written = await desktop.Session.Dispatch(
            () =>
            {
                var chart = WebChart.Create(example);
                chart.Data = example.Data;
                chart.Elements.OfType<Bar>().First(bar => bar.DataKey == "pv").HrefKey = "link";
                return WebChart.Write(chart);
            },
            TestContext.Current.CancellationToken);

        Assert.Empty(written.Reserved);
        Assert.Contains("<a href=\"/m/0\" class=\"pictor-h0\" aria-label=\"pv, Page A\"></a></foreignObject>", written.Html, StringComparison.Ordinal);
        Assert.Contains("<a href=\"https://example.com/1?a=1&amp;b=2\"", written.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("javascript:", written.Html, StringComparison.Ordinal);
        Assert.Equal(2, WebChart.Count(written.Html, "></a></foreignObject>"));
        Assert.Contains(".pictor-cmds a{display:block", written.Html, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "INTERACT-11")]
    public async Task ClickingALinkedBarGivesTheChartItsAddressAsync()
    {
        var example = await LinkedAsync();
        var (linked, missed, ran) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = WebChart.Create(example);
                chart.Data = example.Data;
                var picks = new Picks();
                var bar = chart.Elements.OfType<Bar>().First(item => item.DataKey == "pv");
                (bar.HrefKey, bar.ClickCommand) = ("link", picks);
                var window = new Avalonia.Controls.Window { Width = 500, Height = 300, Content = chart };
                window.Show();
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                var target = chart.Clicks().First(click => click.Href is not null);
                var point = new Avalonia.Point(target.X + (target.Width / 2), target.Y + (target.Height / 2));
                void Click(Avalonia.Point at)
                {
                    window.MouseDown(at, Avalonia.Input.MouseButton.Left, Avalonia.Input.RawInputModifiers.None);
                    window.MouseUp(at, Avalonia.Input.MouseButton.Left, Avalonia.Input.RawInputModifiers.None);
                }

                Click(point);
                var address = Aioros.Markup.Web.GetHref(chart);
                Click(new Avalonia.Point(2, 2));
                var none = Aioros.Markup.Web.GetHref(chart);
                window.Close();
                return (address, none, picks.Ran.Count);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("/m/0", linked);
        Assert.Null(missed);
        Assert.Equal(0, ran);
    }

    [Theory]
    [Trait("AC", "PAINT-07")]
    [InlineData("SimpleScatterChart", "recharts-symbols")]
    [InlineData("TwoLevelPieChart", "recharts-sector")]
    [InlineData("SimpleRadialBarChart", "recharts-radial-bar-sector")]
    [InlineData("FunnelChart", "recharts-trapezoid")]
    public async Task EveryShapeRechartsStylesFromItsItemTakesItsKeysAsync(string name, string shape)
    {
        var example = await Reference.ExampleAsync(name);
        static List<object?> Styled(System.Collections.IEnumerable items) => items.Cast<IReadOnlyDictionary<string, object?>>()
            .Select((row, index) => (object?)(index == 0 ? new Dictionary<string, object?>(row) { ["color"] = "#ff0000", ["dim"] = 0.4 } : new Dictionary<string, object?>(row) { ["color"] = null, ["dim"] = null })).ToList();
        var html = await desktop.Session.Dispatch(
            () =>
            {
                var chart = WebChart.Create(example);
                switch (chart.Elements.OfType<object>().First(element => element is Scatter or Pie or RadialBar or Funnel))
                {
                    case Scatter scatter:
                        (scatter.FillKey, scatter.OpacityKey, scatter.Data) = ("color", "dim", Styled(scatter.Data!));
                        break;
                    case Pie pie:
                        (pie.FillKey, pie.OpacityKey, pie.Data) = ("color", "dim", Styled(pie.Data!));
                        break;
                    case RadialBar bar:
                        (bar.FillKey, bar.OpacityKey, chart.Data) = ("color", "dim", Styled((System.Collections.IEnumerable)chart.Data!));
                        break;
                    case Funnel funnel:
                        (funnel.FillKey, funnel.OpacityKey, funnel.Data) = ("color", "dim", Styled(funnel.Data!));
                        break;
                }

                return WebChart.Write(chart).Html;
            },
            TestContext.Current.CancellationToken);
        var shapes = System.Text.RegularExpressions.Regex.Matches(html, $"<path[^>]*{shape}[^>]*>|<path[^>]*class=\"[^\"]*{shape}[^>]*>").Select(static match => match.Value).ToList();

        Assert.Contains("fill=\"#ff0000\"", shapes[0], StringComparison.Ordinal);
        Assert.Contains("opacity=\"0.4\"", shapes[0], StringComparison.Ordinal);
        Assert.DoesNotContain("#ff0000", shapes[1], StringComparison.Ordinal);
        Assert.DoesNotContain(" opacity=", shapes[1], StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "INTERACT-12")]
    public async Task ALinkedBarShowsTheHandCursorAsync()
    {
        var example = await LinkedAsync();
        var (over, away) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = WebChart.Create(example);
                chart.Data = example.Data;
                chart.Elements.OfType<Bar>().First(item => item.DataKey == "pv").HrefKey = "link";
                var window = new Avalonia.Controls.Window { Width = 500, Height = 300, Content = chart };
                window.Show();
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                var target = chart.Clicks().First(click => click.Href is not null);
                window.MouseMove(new Avalonia.Point(target.X + (target.Width / 2), target.Y + (target.Height / 2)), Avalonia.Input.RawInputModifiers.None);
                var hand = chart.Cursor;
                window.MouseMove(new Avalonia.Point(2, 2), Avalonia.Input.RawInputModifiers.None);
                var plain = chart.Cursor;
                window.Close();
                return (hand, plain);
            },
            TestContext.Current.CancellationToken);

        Assert.NotNull(over);
        Assert.Null(away);
    }
}
