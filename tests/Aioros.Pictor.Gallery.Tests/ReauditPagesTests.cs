using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Aioros.Pictor.Gallery.Tests;

public sealed partial class ReauditPagesTests(GalleryHost gallery)
{
    private const string Fit = """
        () => {
          const width = document.documentElement.clientWidth;
          const faults = [];
          const cards = [...document.querySelectorAll('.recharts-wrapper')].map((chart) => {
            let card = chart.parentElement;
            while (card && getComputedStyle(card).borderTopLeftRadius !== '12px') card = card.parentElement;
            return [chart, card];
          }).filter(([, card]) => card);
          for (const [chart, card] of cards) {
            const box = chart.getBoundingClientRect(), outer = card.getBoundingClientRect();
            if (box.right > outer.right + 1 || box.left < outer.left - 1) faults.push(`a chart spans ${Math.round(box.left)}..${Math.round(box.right)} in a card at ${Math.round(outer.left)}..${Math.round(outer.right)}`);
            for (const inner of card.querySelectorAll('*')) {
              if (inner.closest('svg') && inner.tagName.toLowerCase() !== 'svg') continue;
              if (/(auto|scroll)/.test(getComputedStyle(inner).overflowX) && inner.scrollWidth > inner.clientWidth + 1) faults.push(`a box scrolls sideways by ${inner.scrollWidth - inner.clientWidth} px`);
            }
          }
          for (const end of document.querySelectorAll('.pictor-b1')) {
            const box = end.getBoundingClientRect();
            if (box.right > width + 1) faults.push(`an end traveller reaches ${Math.round(box.right)}`);
          }
          return { count: cards.length, faults, sizes: cards.filter(([chart]) => chart.querySelector('input[type=checkbox]')).map(([chart]) => `${Math.round(chart.getBoundingClientRect().width)}x${Math.round(chart.getBoundingClientRect().height)}`) };
        }
        """;

    [Fact]
    [Trait("AC", "AUDIT-38")]
    public async Task EveryChangeOfTheValuesAdvancesFromThePostedStepAsync()
    {
        var cancel = TestContext.Current.CancellationToken;
        var html = await gallery.Client.GetStringAsync("binding", cancel);
        var seen = new List<string>();
        for (var post = 0; post < 3; post++)
        {
            var button = Button().Match(html).Groups[1].Value;
            List<KeyValuePair<string, string>> form = [.. Field().Matches(html).Select(match => KeyValuePair.Create(match.Groups[1].Value, match.Groups[2].Value)), KeyValuePair.Create("_btn", button)];
            using var answer = await gallery.Client.PostAsync("binding", new FormUrlEncodedContent(form), cancel);
            html = await answer.Content.ReadAsStringAsync(cancel);
            var bound = html[html.IndexOf("Bound brushes, corners, padding and dashes", StringComparison.Ordinal)..];
            seen.Add($"{Value().Match(html).Groups[1].Value} {Field().Match(html).Groups[2].Value} {bound.Contains("fill=\"#82ca9d\"", StringComparison.Ordinal)} {bound.Contains("pictor-gradient", StringComparison.Ordinal)}");
        }

        Assert.Equal(["3000 1 True False", "2000 2 False False", "2780 3 False True"], seen);
    }

    [Fact]
    [Trait("AC", "AUDIT-39")]
    public async Task TheCustomizedLabelsTakeTheLinesStrokeInBothThemesAsync()
    {
        foreach (var theme in new[] { "light", "dark" })
        {
            var html = await ThemedAsync("custom", theme);
            var chart = html[html.IndexOf("CustomizedLabelLineChart</h2>", StringComparison.Ordinal)..];
            var labels = SmallLabel().Matches(chart[..chart.IndexOf("</svg>", StringComparison.Ordinal)]).ToList();

            Assert.Equal(7, labels.Count);
            Assert.Equal("2400", labels[0].Groups[1].Value);
            Assert.All(labels, label => Assert.Equal("#8884d8", FillOf().Match(label.Value).Groups[1].Value));
        }
    }

    [Fact]
    [Trait("AC", "AUDIT-41")]
    public async Task TheFunnelsBadgesStandRightOfTheirTrapezoidsAsync()
    {
        var html = await gallery.Client.GetStringAsync("extras", TestContext.Current.CancellationToken);
        var chart = html[html.IndexOf("A funnel's active item under its labels", StringComparison.Ordinal)..];
        chart = chart[..chart.IndexOf("</svg>", StringComparison.Ordinal)];
        var width = double.Parse(ViewBoxWidth().Match(chart).Groups[1].Value, CultureInfo.InvariantCulture);
        var boxes = Trapezoid().Matches(chart).Select(Numbers).ToList();
        var pills = Pill().Matches(chart).Select(Numbers).ToList();

        Assert.Equal(5, pills.Count);
        Assert.Equal(5, boxes.Count);
        Assert.All(boxes.Zip(pills).Select((pair, i) => (Box: pair.First, Pill: pair.Second, Lower: i + 1 < boxes.Count ? boxes[i + 1][2] : 0)), entry =>
        {
            var (box, pill, lower) = entry;
            Assert.Equal(box[0] + ((box[2] - lower) / 4) + (Math.Abs(box[2] - lower) / 2) + Math.Min(box[2], lower) + 25, pill[0], 3);
            Assert.Equal(box[1] + (box[3] / 2), pill[1] + (pill[3] / 2), 3);
            Assert.True(pill[0] + pill[2] <= width, $"a badge ends at {pill[0] + pill[2]} in a view box of {width}");
        });
    }

    [Fact(Timeout = 120_000)]
    [Trait("AC", "AUDIT-44")]
    public async Task TheFamiliesChartsFitTheirCardsOnAPhoneAsync()
    {
        Browsers.Require();
        using var playwright = await Playwright.CreateAsync();
        await using var chromium = await playwright.Chromium.LaunchAsync(new() { ExecutablePath = Browsers.Chromium, Args = ["--no-sandbox"] });
        var phone = await chromium.NewPageAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
        await phone.GotoAsync(gallery.Address + "families", new() { WaitUntil = WaitUntilState.Load }).WaitAsync(TestContext.Current.CancellationToken);
        var narrow = await phone.EvaluateAsync<System.Text.Json.JsonElement>(Fit);
        var wide = await chromium.NewPageAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
        await wide.GotoAsync(gallery.Address + "families", new() { WaitUntil = WaitUntilState.Load });
        var large = await wide.EvaluateAsync<System.Text.Json.JsonElement>(Fit);

        Assert.Equal(12, narrow.GetProperty("count").GetInt32());
        Assert.Empty(narrow.GetProperty("faults").EnumerateArray().Select(fault => fault.GetString()));
        Assert.All(large.GetProperty("sizes").EnumerateArray(), size => Assert.StartsWith("420x", size.GetString(), StringComparison.Ordinal));
        Assert.Equal(9, large.GetProperty("sizes").GetArrayLength());
    }

    private async Task<string> ThemedAsync(string route, string theme)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Add("Cookie", "theme=" + theme);
        using var answer = await gallery.Client.SendAsync(request, TestContext.Current.CancellationToken);
        return await answer.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private static double[] Numbers(Match match) => [.. match.Groups.Cast<Group>().Skip(1).Select(group => double.Parse(group.Value, CultureInfo.InvariantCulture))];

    [GeneratedRegex(@"name=""_btn"" value=""([^""]+)""")]
    private static partial Regex Button();

    [GeneratedRegex(@"<input type=""number"" name=""(f\d+)""[^>]* value=""([^""]*)""")]
    private static partial Regex Field();

    [GeneratedRegex(@"value : (\d+)")]
    private static partial Regex Value();

    [GeneratedRegex(@"<text [^>]*dy=""-4""[^>]*font-size=""10""[^>]*>(\d+)<")]
    private static partial Regex SmallLabel();

    [GeneratedRegex(@" fill=""([^""]*)""")]
    private static partial Regex FillOf();

    [GeneratedRegex(@"viewBox=""0 0 ([\d.]+) [\d.]+""")]
    private static partial Regex ViewBoxWidth();

    [GeneratedRegex(@"<rect x=""([\d.]+)"" y=""([\d.]+)"" width=""([\d.]+)"" height=""([\d.]+)"" rx=""4"" ry=""4"" fill=""#[0-9a-f]{6}"" stroke=""none""/>")]
    private static partial Regex Trapezoid();

    [GeneratedRegex(@"<rect x=""([\d.]+)"" y=""([\d.]+)"" width=""(60)"" height=""(20)"" rx=""10""")]
    private static partial Regex Pill();
}
