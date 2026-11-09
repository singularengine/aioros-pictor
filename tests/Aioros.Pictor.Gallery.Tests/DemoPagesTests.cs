using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Aioros.Pictor.Gallery.Tests;

public sealed partial class DemoPagesTests(GalleryHost gallery)
{
    private static readonly (string Route, string Kind, string Title)[] Families =
    [
        ("line", "LineChart", "Line chart"), ("area", "AreaChart", "Area chart"), ("bar", "BarChart", "Bar chart"), ("composed", "ComposedChart", "Composed chart"),
        ("scatter", "ScatterChart", "Scatter chart"), ("pie", "PieChart", "Pie chart"), ("radar", "RadarChart", "Radar chart"), ("radial-bar", "RadialBarChart", "Radial bar chart"),
        ("funnel", "FunnelChart", "Funnel chart"), ("treemap", "Treemap", "Treemap"), ("sankey", "Sankey", "Sankey"), ("sunburst", "SunburstChart", "Sunburst chart"),
    ];

    [Fact]
    [Trait("AC", "GALLERY-01")]
    public async Task EachFamilysReferenceExamplesStandBesideRechartsRenderWithTheirVerdictAsync()
    {
        var cancel = TestContext.Current.CancellationToken;
        var root = Path.GetFullPath(Path.Combine(GalleryHost.ProjectFolder, "..", "..", ".."));
        var files = await Task.WhenAll(Directory.EnumerateFiles(Path.Combine(root, "tools", "recharts", "examples"), "*.json").Select(file => File.ReadAllTextAsync(file, cancel)));
        var examples = files
            .Select(text => JsonDocument.Parse(text).RootElement)
            .Where(example => !example.GetRawText().Contains("\"$content\"", StringComparison.Ordinal))
            .ToLookup(example => example.GetProperty("kind").GetString()!, example => example.GetProperty("name").GetString()!);
        using var pixels = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(GalleryHost.ProjectFolder, "Reference", "pixels.json"), cancel));
        var index = await gallery.Client.GetStringAsync("compare", cancel);
        using var unknown = await gallery.Client.GetAsync("compare/nothing", cancel);

        foreach (var (route, kind, _) in Families)
        {
            var html = await gallery.Client.GetStringAsync("compare/" + route, cancel);
            var family = await gallery.Client.GetStringAsync(route, cancel);
            var names = Card().Matches(html).Select(match => match.Groups[1].Value).ToList();
            Assert.Equal(examples[kind].Order(StringComparer.Ordinal), names);
            Assert.Equal(names.Count, Image().Count(html));
            Assert.Equal(names.Count, html.Split(">PICTOR<").Skip(1).Count(part => part.Contains("class=\"recharts-surface\"", StringComparison.Ordinal)));
            Assert.Equal(names.Count, Verdict().Count(html));
            foreach (Match match in Pixels().Matches(html))
            {
                var recorded = pixels.RootElement.GetProperty("results").GetProperty(match.Groups[1].Value).GetProperty("share").GetDouble();
                Assert.Equal((recorded * 100).ToString("0.00", CultureInfo.InvariantCulture), match.Groups[2].Value);
            }

            Assert.Contains($"href=\"/compare/{route}\"", family, StringComparison.Ordinal);
            Assert.Contains($"href=\"/compare/{route}\"", index, StringComparison.Ordinal);
            using var image = await gallery.Client.GetAsync($"reference/{names[0]}.png", cancel);
            Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        }

        Assert.Contains("href=\"/compare\"", index, StringComparison.Ordinal);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    [Trait("AC", "GALLERY-02")]
    public async Task EveryFamilysInteractionsAreShownOnOnePageAsync()
    {
        var cancel = TestContext.Current.CancellationToken;
        var html = await gallery.Client.GetStringAsync("families", cancel);
        var cards = Families.ToDictionary(family => family.Title, family => Section(html, family.Title), StringComparer.Ordinal);

        foreach (var (_, kind, title) in Families)
        {
            var card = cards[title];
            Assert.Contains("class=\"recharts-wrapper", card, StringComparison.Ordinal);
            Assert.Contains("class=\"pictor-hover", card, StringComparison.Ordinal);
            Assert.Equal(kind is not ("Treemap" or "Sankey" or "SunburstChart"), card.Contains("type=\"checkbox\"", StringComparison.Ordinal));
            Assert.Equal(kind is "LineChart" or "AreaChart" or "BarChart" or "ComposedChart", Count(card, "type=\"range\"") == 3);
            Assert.Equal(kind is "LineChart" or "AreaChart" or "BarChart" or "ComposedChart" or "ScatterChart", Focusable().IsMatch(card));
            var button = Button().Match(card).Groups[1].Value;
            var form = new FormUrlEncodedContent([KeyValuePair.Create("_btn", button)]);
            using var posted = await gallery.Client.PostAsync("families", form, cancel);
            var answer = await posted.Content.ReadAsStringAsync(cancel);
            Assert.Matches($"{Regex.Escape(title)}: the command ran with &quot;[^&]+&quot;", answer);
        }

        Assert.Contains("href=\"/live-charts\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/theme/system\"", html, StringComparison.Ordinal);
        foreach (var (_, kind, title) in Families)
        {
            var card = cards[title];
            Assert.Equal(kind is not ("Treemap" or "Sankey" or "SunburstChart") ? Count(card, "class=\"recharts-legend-item ") : 0, Count(card, "type=\"checkbox\""));
        }

        Browsers.Require();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var chromium = await playwright.Chromium.LaunchAsync(new() { ExecutablePath = Browsers.Chromium, Args = ["--no-sandbox"] });
        var page = await chromium.NewPageAsync(new() { ViewportSize = new() { Width = 1400, Height = 1000 } });
        await page.GotoAsync(gallery.Address + "families");
        var shown = new List<string>();
        foreach (var (_, _, title) in Families)
        {
            await page.Mouse.MoveAsync(2, 2);
            var at = await page.EvaluateAsync<double[]>(Target, title);
            if (at[0] < 0)
            {
                shown.Add($"{title}: no hover target");
                continue;
            }

            var idle = await page.EvaluateAsync<int>(Tips, title);
            await page.Mouse.MoveAsync((float)at[0], (float)at[1]);
            var boxed = true;
            try
            {
                await page.WaitForFunctionAsync(Hovered, title, new() { Timeout = (float)Patience.Limit.TotalMilliseconds });
            }
            catch (TimeoutException)
            {
                boxed = false;
            }

            shown.Add(boxed ? $"{title}: {idle} then {await page.EvaluateAsync<int>(Tips, title)}" : $"{title}: no box after the hover");
        }

        Assert.Equal(Families.Select(family => $"{family.Title}: 0 then 1"), shown);
    }

    [Fact]
    [Trait("AC", "GALLERY-05")]
    public async Task OnlyPixelsMeasuredOnTheCurrentMarkupCountAsMatchingAsync()
    {
        var cancel = TestContext.Current.CancellationToken;
        using var pixels = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(GalleryHost.ProjectFolder, "Reference", "pixels.json"), cancel));
        var recorded = pixels.RootElement.GetProperty("results");
        var mixed = global::Gallery.Compare.ComparedExample.Headline(
            [new("A", "BarChart", string.Empty, 500, 300, "Elements: all 9 match", "Pixels: 0.00%", true, 0), new("B", "BarChart", string.Empty, 500, 300, "Elements: all 9 match", "Pixels: not measured", true, null)],
            "reference examples match");

        foreach (var (route, _, _) in Families)
        {
            var html = await gallery.Client.GetStringAsync("compare/" + route, cancel);
            var cards = Verdicts().Matches(html).ToList();
            var current = cards.ToDictionary(card => card.Groups[1].Value, card => recorded.TryGetProperty(card.Groups[1].Value, out var entry)
                && entry.GetProperty("markup").GetString() == Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(global::Gallery.Compare.ReferenceExamples.MarkupOf(card.Groups[1].Value)!)))[..16]);
            var headline = Headline().Match(html);
            var matching = cards.Count(card => current[card.Groups[1].Value] && card.Groups[2].Value == "match");

            Assert.All(cards, card => Assert.Equal(current[card.Groups[1].Value], card.Groups[4].Value.EndsWith("% of inked pixels differ strongly", StringComparison.Ordinal)));
            Assert.All(cards.Where(card => !current[card.Groups[1].Value]), card => Assert.True(card.Groups[2].Value is "not measured" or "differs", card.Groups[1].Value));
            Assert.True(headline.Success, route);
            Assert.Equal((matching, current.Count(entry => entry.Value)), (int.Parse(headline.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(headline.Groups[2].Value, CultureInfo.InvariantCulture)));
            Assert.Equal(current.Count(entry => !entry.Value), headline.Groups[3].Success ? int.Parse(headline.Groups[3].Value, CultureInfo.InvariantCulture) : 0);
            Assert.Equal(pixels.RootElement.GetProperty("commit").GetString(), Measurement().Match(html).Groups[1].Value);
            Assert.Equal(pixels.RootElement.GetProperty("date").GetString(), Measurement().Match(html).Groups[2].Value);
        }

        Assert.StartsWith("1 of 1 measured reference examples match; 1 not measured on Pictor's current markup", mixed, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "GALLERY-04")]
    public async Task TheSamplesNeutralColorsFollowTheThemeAsync()
    {
        (string Route, string Light, string Dark)[] marks =
        [
            ("funnel", "fill=\"#000\"", "fill=\"#ededf0\""),
            ("radial-bar", "fill=\"#eee\"", "fill=\"#161616\""),
            ("bar", "fill=\"#eee\"", "fill=\"#161616\""),
            ("tips", "stroke:#8884d8;fill:#f5f5f5", "stroke:#8884d8;fill:#1b1b1f"),
            ("tips", "fill=\"#f4f4f4\"", "fill=\"#1b1b1f\""),
            ("ranges", "fill=\"#eee\"", "fill=\"#161616\""),
        ];

        foreach (var (route, light, dark) in marks)
        {
            var lightPage = await ThemedAsync(route, "light");
            var darkPage = await ThemedAsync(route, "dark");
            Assert.True(lightPage.Contains(light, StringComparison.Ordinal), $"{route} light lacks {light}");
            Assert.True(darkPage.Contains(dark, StringComparison.Ordinal), $"{route} dark lacks {dark}");
            Assert.False(darkPage.Contains(light, StringComparison.Ordinal), $"{route} dark draws {light}");
            Assert.False(lightPage.Contains(dark, StringComparison.Ordinal), $"{route} light draws {dark}");
        }
    }

    private async Task<string> ThemedAsync(string route, string theme)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Add("Cookie", "theme=" + theme);
        using var answer = await gallery.Client.SendAsync(request, TestContext.Current.CancellationToken);
        return await answer.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private const string CardScript = """
        (t) => {
          const heading = [...document.querySelectorAll('*')].find((e) => e.childElementCount === 0 && e.textContent.trim() === t && e.closest('main, body'));
          let card = heading;
          while (card && !card.querySelector('.recharts-wrapper')) card = card.parentElement;
          return card.querySelector('.recharts-wrapper');
        }
        """;

    private static readonly string Target = "(t) => { const wrapper = (" + CardScript + ")(t); wrapper.scrollIntoView({block: 'center'}); const targets = '.pictor-hover > [class^=pictor-hk], .pictor-hover button[class^=pictor-h], .pictor-cmds button[class^=pictor-h], .pictor-hover > g:not([class^=pictor-tk])'; const on = (x, y) => { const hit = document.elementFromPoint(x, y); return hit && wrapper.contains(hit) && hit.closest(targets); }; for (const shape of wrapper.querySelectorAll('.pictor-hover .pictor-hit, .pictor-hover button[class^=pictor-h], .pictor-cmds button[class^=pictor-h], .pictor-hover > [class^=pictor-hk] > *')) { const b = shape.getBoundingClientRect(); if (on(b.x + b.width / 2, b.y + b.height / 2)) return [b.x + b.width / 2, b.y + b.height / 2]; } const box = wrapper.getBoundingClientRect(); for (let y = box.top + 2; y < box.bottom; y += 3) for (let x = box.left + 2; x < box.right; x += 3) if (on(x, y)) return [x, y]; return [-1, -1]; }";

    private static readonly string Tips = "(t) => [...(" + CardScript + ")(t).querySelectorAll('.pictor-hover foreignObject')].filter((box) => box.checkVisibility()).length";

    private static readonly string Hovered = "(t) => (" + Tips + ")(t) > 0";

    private static string Section(string html, string title)
    {
        var start = html.IndexOf($">{title}</div>", html.IndexOf("Every interaction, every family", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.True(start >= 0, title);
        var next = Families.Select(family => html.IndexOf($">{family.Title}</div>", start + 1, StringComparison.Ordinal)).Where(at => at > start).DefaultIfEmpty(html.IndexOf("Pictor is an independent port", start, StringComparison.Ordinal)).Min();
        return html[start..next];
    }

    private static int Count(string html, string text) => (html.Length - html.Replace(text, string.Empty, StringComparison.Ordinal).Length) / text.Length;

    [GeneratedRegex(@"<div class=""[^""]+"">([A-Za-z0-9]+)</div><div class=""c [^""]+""><div class=""[^""]+"">(?:match|differs|not measured)</div>")]
    private static partial Regex Card();

    [GeneratedRegex(@"<img src=""/reference/[A-Za-z0-9]+\.png""")]
    private static partial Regex Image();

    [GeneratedRegex(@">Elements: (all \d+ match|\d+ of \d+ differ)<")]
    private static partial Regex Verdict();

    [GeneratedRegex(@">([A-Za-z0-9]+)</div><div class=""c [^""]+""><div class=""[^""]+"">(?:match|differs|not measured)</div></div></div><div class=""[^""]+"">[^<]*</div><div class=""[^""]+"">Elements: [^<]*</div><div class=""[^""]+"">Pixels: (\d+\.\d\d)% ")]
    private static partial Regex Pixels();

    [GeneratedRegex(@">([A-Za-z0-9]+)</div><div class=""c [^""]+""><div class=""[^""]+"">(match|differs|not measured)</div></div></div><div class=""[^""]+"">[^<]*</div><div class=""[^""]+"">Elements: ([^<]*)</div><div class=""[^""]+"">Pixels: ([^<]*)</div>")]
    private static partial Regex Verdicts();

    [GeneratedRegex(@"(\d+) of (\d+) measured reference examples match Recharts' render element for element, without a pixel that differs strongly(?:; (\d+) not measured on Pictor's current markup)?")]
    private static partial Regex Headline();

    [GeneratedRegex(@"Pixels measured with Chromium [\d.]+, Pictor ([0-9a-f]{7,}), on (\d{4}-\d\d-\d\d)")]
    private static partial Regex Measurement();

    [GeneratedRegex(@"data-aioros-roving|<svg class=""recharts-surface""[^>]*tabindex=""0""")]
    private static partial Regex Focusable();

    [GeneratedRegex(@"name=""_btn"" value=""([^""]+)""")]
    private static partial Regex Button();
}
