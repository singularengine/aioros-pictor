using Gallery.Analytics;
using Microsoft.Playwright;

namespace Aioros.Pictor.Gallery.Tests;

public sealed class DashboardBrowserTests(GalleryHost gallery)
{
    private const string ViewText = """
        (text) => [...document.querySelectorAll('[data-aioros-view]')].find(v => v.innerText.includes(text))?.innerText ?? ''
        """;

    [Fact(Timeout = 90_000)]
    [Trait("AC", "DASHBOARD-10")]
    public async Task SwitchingTheProductTheFocusAndTheRangeKeepsTheLiveViewsOnTheNewChoiceAsync()
    {
        var cancel = TestContext.Current.CancellationToken;
        Browsers.Require();
        await using var browser = await BrowserAsync();
        var page = await browser.NewPageAsync();
        var opened = page.WaitForRequestAsync(request => request.Url.Contains("/live?app=web", StringComparison.Ordinal));
        await page.GotoAsync(gallery.Address + "dashboard?app=web&range=live");
        await opened.WaitAsync(cancel);

        var relay = page.WaitForResponseAsync(response => response.Url.Contains("/live?app=api&range=live", StringComparison.Ordinal));
        await page.ClickAsync("a[aria-label='Relay, the payments API']");
        await relay.WaitAsync(cancel);
        var product = await TwoPushesAsync(page, "Requests per second");

        await page.ClickAsync("button[aria-label='Views, GET /v1/customers']");
        await page.WaitForFunctionAsync("() => document.body.innerText.includes('Requests · GET /v1/customers')");
        var focused = await TwoPushesAsync(page, "Per 2 minutes");

        var day = page.WaitForResponseAsync(response => response.Url.Contains("range=24h", StringComparison.Ordinal) && response.Url.Contains("/live?", StringComparison.Ordinal));
        await page.ClickAsync("a[href='/dashboard?app=api&range=24h']");
        await day.WaitAsync(cancel);
        await page.WaitForFunctionAsync("() => document.body.innerText.includes('Per 30 minutes, last 24 hours')");
        await TwoPushesAsync(page, "Requests per second");
        var body = await page.InnerTextAsync("body");

        Assert.All(product, text => Assert.DoesNotContain("Active users now", text, StringComparison.Ordinal));
        Assert.Contains("Requests per second", await page.InnerTextAsync("body"), StringComparison.Ordinal);
        Assert.All(focused, text => Assert.Contains("Requests · GET /v1/customers", text, StringComparison.Ordinal));
        Assert.Contains("Per 30 minutes, last 24 hours", body, StringComparison.Ordinal);
        Assert.DoesNotContain("the last bucket live", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Active users now", body, StringComparison.Ordinal);
    }

    [Fact(Timeout = 90_000)]
    [Trait("AC", "DASHBOARD-11")]
    public async Task TheMainChartsBrushIsKeptAcrossLiveTicksAsync()
    {
        Browsers.Require();
        await using var browser = await BrowserAsync();
        var page = await browser.NewPageAsync();
        var opened = page.WaitForRequestAsync(request => request.Url.Contains("/live?app=api", StringComparison.Ordinal));
        await page.GotoAsync(gallery.Address + "dashboard?app=api&range=live");
        await opened;

        var reopened = page.WaitForResponseAsync(response => response.Url.Contains("/live?app=api&range=live&from=5&to=14", StringComparison.Ordinal));
        await page.EvalOnSelectorAsync(
            "input[aria-label='Start of the window']",
            "input => { input.value = '5'; input.dispatchEvent(new Event('input', {bubbles: true})); input.dispatchEvent(new Event('change', {bubbles: true})); }");
        await reopened;
        var address = page.Url;
        var pushes = await TwoPushesAsync(page, "Per 2 minutes");
        var window = await page.EvalOnSelectorAllAsync<string[]>("input[type=range]", "inputs => inputs.filter(i => i.checkVisibility({ visibilityProperty: true }) && !i.classList.contains('pictor-bs')).map(i => i.value)");
        var clamped = await gallery.Client.GetStringAsync("dashboard?app=api&range=live&from=40&to=2", TestContext.Current.CancellationToken);
        var model = new TrafficView(new TrafficState(Data(15), "api", "live", -1, null, null), new Navigation());
        model.BrushStart = 3;
        model.BrushEnd = 9;
        model.Receive(new TrafficState(Data(15), "api", "live", -1, null, null));

        Assert.EndsWith("/dashboard?app=api&range=live&from=5&to=14", address, StringComparison.Ordinal);
        Assert.NotEqual(pushes[0], pushes[1]);
        Assert.Equal(["5", "14"], window);
        Assert.Contains("min=\"0\" max=\"14\" value=\"14\" aria-valuetext", clamped, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"40\"", clamped, StringComparison.Ordinal);
        Assert.Equal((3, 9), (model.BrushStart, model.BrushEnd));
    }

    private static TrafficData Data(int count) =>
        new("Requests", "Per 2 minutes", "Requests", "Yesterday", "0", true, "Updated 00:00:00", [.. Enumerable.Range(0, count).Select(i => new TrafficPoint($"{i:00}", i, i))]);

    /// <summary>
    /// Ticks the Gallery's clock until the view holding <paramref name="text"/> changes, twice, and returns its text after
    /// each change: a tick whose push leaves the view's text as it was (a value that did not move that second) is followed
    /// by another, each push awaited.
    /// </summary>
    private async Task<List<string>> TwoPushesAsync(IPage page, string text)
    {
        var seen = new List<string>();
        var before = await page.EvaluateAsync<string>(ViewText, text);
        for (var tick = 0; seen.Count < 2 && tick < 30; tick++)
        {
            await LivePage.PushAsync(gallery, page);
            var after = await page.EvaluateAsync<string>(ViewText, text);
            if (after != before && after.Length > 0)
            {
                seen.Add(after);
                before = after;
            }
        }

        Assert.Equal(2, seen.Count);
        return seen;
    }

    private static async Task<Browser> BrowserAsync()
    {
        var playwright = await Playwright.CreateAsync();
        var chromium = await playwright.Chromium.LaunchAsync(new() { ExecutablePath = Browsers.Chromium, Args = ["--no-sandbox"] });
        return new Browser(playwright, chromium);
    }

    private sealed class Browser(IPlaywright playwright, IBrowser browser) : IAsyncDisposable
    {
        public async Task<IPage> NewPageAsync()
        {
            var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1400, Height = 1000 } });
            await LivePage.CountPushesAsync(page);
            return page;
        }

        public async ValueTask DisposeAsync()
        {
            await browser.DisposeAsync();
            playwright.Dispose();
        }
    }

    private sealed class Navigation : Aioros.Navigation.INavigator
    {
        public ValueTask NavigateAsync(Aioros.Navigation.NavigationRequest request, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }
}
