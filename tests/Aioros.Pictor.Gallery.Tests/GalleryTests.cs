using System.Text.RegularExpressions;

namespace Aioros.Pictor.Gallery.Tests;

public sealed partial class GalleryTests(GalleryHost gallery)
{
    [Fact]
    [Trait("AC", "DASHBOARD-01")]
    public async Task EveryPageOfTheGalleryPrecompilesInBothThemesAsync()
    {
        var plan = Path.Combine(Path.GetTempPath(), $"gallery-precompile-{Guid.NewGuid():N}.json");
        using var process = GalleryHost.Start(GalleryHost.Application, "build", "--precompile=" + plan);
        var output = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        File.Delete(plan);

        var pages = Precompiled().Match(output);
        var identical = Identical().Match(output);
        Assert.Equal(0, process.ExitCode);
        Assert.True(pages.Success, output);
        Assert.Equal(pages.Groups[1].Value, pages.Groups[2].Value);
        Assert.True(int.Parse(pages.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) >= GalleryHost.Routes.Length + 2);
        Assert.All(GalleryHost.Routes, route => Assert.Matches("precompiled  " + Regex.Escape(route) + "(\n|  \\()", output));
        Assert.True(identical.Success, output);
        Assert.Equal(identical.Groups[1].Value, identical.Groups[2].Value);
        var cultures = Regex.Match(output, @"Cultures: ([^\n]+)") is { Success: true } listed ? listed.Groups[1].Value.Split(", ").Length : 1;
        Assert.Equal(4, cultures);
        Assert.Equal(2 * cultures * int.Parse(pages.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), int.Parse(identical.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    [Trait("AC", "DASHBOARD-07")]
    public async Task OnlyTheLiveViewsRegionsArePushedOnEachTickAsync()
    {
        var live = await EventsAsync("dashboard/live?app=api&range=live", 4);
        var week = await EventsAsync("dashboard/live?app=api&range=7d", 4);

        Assert.All(live.Concat(week), data =>
        {
            Assert.DoesNotContain("Top endpoints", data, StringComparison.Ordinal);
            Assert.DoesNotContain("Busiest hours", data, StringComparison.Ordinal);
            var known = Count(data, "Requests per second") + Count(data, "compared with yesterday", "compared with previous week") + Count(data, "events per minute") + Count(data, "Error rate");
            Assert.True(known == Regions(data), $"{Regions(data)} regions: {Summary(data)}");
        });
        Assert.True(live.Exists(data => Regions(data) == 3 && Count(data, "Requests per second") + Count(data, "compared with yesterday") + Count(data, "events per minute") == 3), string.Join(" / ", live.Select(Summary)));
        Assert.True(week.Exists(data => Regions(data) == 2 && Count(data, "Requests per second") + Count(data, "events per minute") == 2), string.Join(" / ", week.Select(Summary)));
        Assert.All(live.Concat(week).Where(data => Count(data, "Requests per second") == 0), data => Assert.Contains("Error rate", data, StringComparison.Ordinal));
        var times = live.Where(data => data.Contains("Updated ", StringComparison.Ordinal)).Select(Updated).ToList();
        var elapsed = (Seconds(times[^1]) - Seconds(times[0]) + 86_400) % 86_400;
        Assert.True(elapsed is > 0 and < 3_600, string.Join(", ", times));
    }

    private static int Seconds(string updated) => (int)TimeSpan.ParseExact(updated["Updated ".Length..], @"hh\:mm\:ss", System.Globalization.CultureInfo.InvariantCulture).TotalSeconds;

    private static string Summary(string data) => string.Join(" | ", ViewKey().Matches(data).Select(match => match.Groups[1].Value)) + " at " + Updated(data);

    [GeneratedRegex(@"<div data-aioros-view=""([^""]+)""")]
    private static partial Regex ViewKey();

    private static int Count(string data, params string[] marks) => marks.Any(mark => data.Contains(mark, StringComparison.Ordinal)) ? 1 : 0;

    [Fact]
    [Trait("AC", "DASHBOARD-09")]
    public async Task TheNavigationMarksTheCurrentPageAsync()
    {
        var cancel = TestContext.Current.CancellationToken;
        Assert.Equal(["/line"], Current(await gallery.Client.GetStringAsync("line", cancel)));
        Assert.Equal(["/dashboard"], Current(await gallery.Client.GetStringAsync("dashboard", cancel)));
        Assert.Equal(["/dashboard", "/dashboard?app=mobile&range=24h"], Current(await gallery.Client.GetStringAsync("dashboard?app=mobile&range=24h", cancel)));
        Assert.Equal(["/about"], Current(await gallery.Client.GetStringAsync("about", cancel)));
        foreach (var route in GalleryHost.Routes)
        {
            using var answer = await gallery.Client.GetAsync(route.TrimStart('/'), cancel);
            Assert.Equal(System.Net.HttpStatusCode.OK, answer.StatusCode);
        }

        Browsers.Require();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var chromium = await playwright.Chromium.LaunchAsync(new() { ExecutablePath = Browsers.Chromium, Args = ["--no-sandbox"] });
        var narrow = await chromium.NewPageAsync(new() { ViewportSize = new() { Width = 800, Height = 900 } });
        await narrow.GotoAsync(gallery.Address + "line");
        var closed = (await narrow.IsVisibleAsync("#nav a[href='/line']"), await narrow.IsVisibleAsync("button[aria-label='Menu']"));
        await narrow.ClickAsync("button[aria-label='Menu']");
        await narrow.WaitForSelectorAsync("#nav:popover-open", new() { Timeout = (float)Patience.Limit.TotalMilliseconds });
        var opened = (await narrow.IsVisibleAsync("#nav a[href='/line']"), await narrow.EvaluateAsync<string?>("() => document.querySelector(\"#nav a[aria-current='page']\")?.getAttribute('href')"));
        var wide = await chromium.NewPageAsync(new() { ViewportSize = new() { Width = 1400, Height = 900 } });
        await wide.GotoAsync(gallery.Address + "line");
        var pane = (await wide.IsVisibleAsync("#nav a[href='/line']"), await wide.IsVisibleAsync("button[aria-label='Menu']"));

        Assert.Equal((false, true), closed);
        Assert.Equal((true, "/line"), opened);
        Assert.Equal((true, false), pane);
    }

    [Fact]
    [Trait("AC", "THEME-18")]
    public async Task TheThemeSwitchOffersLightDarkAndSystemAsync()
    {
        var cancel = TestContext.Current.CancellationToken;
        var page = await gallery.Client.GetStringAsync("line", cancel);
        using var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
        using var client = new HttpClient(handler) { BaseAddress = gallery.Address };
        using var switched = await client.GetAsync("theme/system", cancel);

        foreach (var (href, name) in new[] { ("/theme/light", "Light theme"), ("/theme/dark", "Dark theme"), ("/theme/system", "System theme, following the device") })
        {
            Assert.Matches($"<a aria-label=\"{Regex.Escape(name)}\" href=\"{Regex.Escape(href)}\"", page);
        }

        Assert.Contains(switched.Headers.GetValues("Set-Cookie"), cookie => cookie.StartsWith("theme=system;", StringComparison.Ordinal));
        Assert.Contains("Sec-CH-Prefers-Color-Scheme", switched.Headers.GetValues("Accept-CH"));
        Assert.Equal(("#09090b", true), await ThemeAsync(client, "theme=system", "\"dark\""));
        Assert.Equal(("#09090b", true), await ThemeAsync(client, "theme=system; theme-system=dark", null));
        Assert.Equal(("#f7f7f8", false), await ThemeAsync(client, "theme=system; theme-system=light", null));
        Assert.Equal(("#f7f7f8", false), await ThemeAsync(client, "theme=system", "\"light\""));
    }

    private static async Task<(string Background, bool DarkCharts)> ThemeAsync(HttpClient client, string cookies, string? hint)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "bar");
        request.Headers.Add("Cookie", cookies);
        if (hint is not null)
        {
            request.Headers.Add("Sec-CH-Prefers-Color-Scheme", hint);
        }

        using var answer = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var html = await answer.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return (BodyBackground().Match(html).Groups[1].Value, html.Contains("--pictor-tip-bg:", StringComparison.Ordinal));
    }

    /// <summary>The <paramref name="count"/> pushes of a live dashboard's stream that follow as many ticks of the Gallery's clock, each a <c>views</c> event.</summary>
    private async Task<List<string>> EventsAsync(string path, int count)
    {
        var cancel = TestContext.Current.CancellationToken;
        await using var stream = await LiveStream.OpenAsync(gallery, path, cancel);
        var events = new List<string>();
        while (events.Count < count)
        {
            var (name, data) = await stream.TickedAsync(gallery, cancel);
            Assert.Equal("views", name);
            events.Add(data);
        }

        return events;
    }

    private static int Regions(string data) => ViewRegion().Count(data);

    private static string Updated(string data) => UpdatedTime().Match(data).Value;

    private static List<string> Current(string html) => [.. CurrentLink().Matches(html).Select(match => WebUtility(match.Groups[1].Value))];

    private static string WebUtility(string href) => System.Net.WebUtility.HtmlDecode(href);

    [GeneratedRegex(@"Precompiled (\d+) of (\d+) pages")]
    private static partial Regex Precompiled();

    [GeneratedRegex(@"Identical output: (\d+) of (\d+) pages")]
    private static partial Regex Identical();

    [GeneratedRegex(@"<div data-aioros-view=""")]
    private static partial Regex ViewRegion();

    [GeneratedRegex(@"Updated \d\d:\d\d:\d\d")]
    private static partial Regex UpdatedTime();

    [GeneratedRegex(@"<body style=""background:(#[0-9a-f]{6})")]
    private static partial Regex BodyBackground();

    [GeneratedRegex(@"<a href=""([^""]*)""[^>]*aria-current=""page""")]
    private static partial Regex CurrentLink();
}
