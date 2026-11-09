using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Aioros.Pictor.Gallery.Tests;

public sealed partial class ReplayBrowserTests(GalleryHost gallery)
{
    [Fact(Timeout = 90_000)]
    [Trait("AC", "ANIMATION-26")]
    public async Task AReplayButtonInAScrollingPanelKeepsThePageInViewAsync()
    {
        Browsers.Require();
        using var client = new HttpClient();
        var html = await client.GetStringAsync(new Uri(gallery.Address, "sankey"), TestContext.Current.CancellationToken);
        var button = Label().Match(html).Value;
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { ExecutablePath = Browsers.Chromium, Args = ["--no-sandbox"] });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 800, Height = 600 } });
        await page.SetContentAsync($"<body style=\"margin:0;overflow:hidden\"><div id=\"panel\" style=\"height:600px;overflow:auto\"><div style=\"height:3000px\"></div>{button}</div></body>");

        await page.EvaluateAsync("() => { const panel = document.getElementById('panel'); panel.scrollTop = panel.scrollHeight; }");
        var before = await page.EvaluateAsync<double>("() => document.getElementById('panel').scrollTop");
        await page.ClickAsync(".pictor-replay");
        var inside = await page.EvaluateAsync<bool>("() => { const l = document.querySelector('.pictor-replay').getBoundingClientRect(), c = document.querySelector('.pictor-replay input').getBoundingClientRect(); return c.top >= l.top && c.bottom <= l.bottom && c.left >= l.left && c.right <= l.right; }");

        Assert.NotEmpty(button);
        Assert.True(await page.IsCheckedAsync(".pictor-replay input"));
        Assert.True(inside);
        Assert.Equal(0, await page.EvaluateAsync<double>("() => document.scrollingElement.scrollTop"));
        Assert.Equal(before, await page.EvaluateAsync<double>("() => document.getElementById('panel').scrollTop"));
    }

    [GeneratedRegex("<label class=\"pictor-replay\".*?</label>", RegexOptions.Singleline)]
    private static partial Regex Label();
}
