using System.Text.Json;
using Microsoft.Playwright;

namespace Aioros.Pictor.Gallery.Tests;

/// <summary>
/// What a client navigation shows of the charts it brings, checked in Chromium on the running Gallery with the
/// framework's script: Playwright's clock is paused so the morph script's frames stay where they start, and the
/// document's animation timeline is paused through DevTools so the CSS enter animations stay in flight until read.
/// </summary>
public sealed class NavigationBrowserTests(GalleryHost gallery)
{
    private const string Lines = "path.recharts-line-curve.pictor-draw";

    private const string Drawn = """
        async (selector) => {
          const served = new DOMParser().parseFromString(await (await fetch('/line')).text(), 'text/html');
          const expected = [...served.querySelectorAll(selector)].map((p) => p.getAttribute('d'));
          const lines = [...document.querySelectorAll(selector)];
          return {
            count: lines.length,
            served: expected.length,
            morphing: document.querySelectorAll('.recharts-line [data-aioros-morphing], .recharts-line[data-aioros-morphing]').length,
            lines: lines.map((p, i) => {
              const draws = p.getAnimations().filter((a) => a.animationName === 'pictor-draw');
              const frames = draws.length === 1 ? draws[0].effect.getKeyframes().map((k) => k.strokeDashoffset.replace('px', '')).join(' ') : '';
              const dashes = getComputedStyle(p).strokeDasharray.replaceAll('px', '').replaceAll(',', ' ').replace(/\s+/g, ' ');
              return `${draws.map((a) => a.playState).join()}|${frames}|${dashes}|${p.getAttribute('pathLength')}|${p.getAttribute('d') === expected[i]}`;
            }),
          };
        }
        """;

    [Fact]
    [Trait("AC", "ANIMATION-25")]
    public async Task LinesDrawFromTheirStartAfterAClientNavigationInChromiumAsync()
    {
        Browsers.Require();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { ExecutablePath = Browsers.Chromium, Args = ["--no-sandbox"] });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1400, Height = 1000 }, DeviceScaleFactor = 1 });
        var bound = new PageWaitForFunctionOptions { Timeout = (float)Patience.Limit.TotalMilliseconds, PollingInterval = 20 };
        await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch });
        await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(1024));
        await page.GotoAsync(gallery.Address + "dashboard", new() { WaitUntil = WaitUntilState.Load });
        var devTools = await page.Context.NewCDPSessionAsync(page);
        await devTools.SendAsync("Animation.enable");
        await devTools.SendAsync("Animation.setPlaybackRate", new Dictionary<string, object> { ["playbackRate"] = 0 });

        await page.ClickAsync("[href='/line']");
        await page.WaitForFunctionAsync("(s) => location.pathname === '/line' && document.querySelectorAll(s).length > 0", Lines, bound);
        var shown = await page.EvaluateAsync<JsonElement>(Drawn, Lines);

        var count = shown.GetProperty("count").GetInt32();
        Assert.True(count > 0);
        Assert.Equal(shown.GetProperty("served").GetInt32(), count);
        Assert.Equal(0, shown.GetProperty("morphing").GetInt32());
        Assert.All(shown.GetProperty("lines").EnumerateArray().Select(line => line.GetString()), line => Assert.Equal("running|1 0|1 1|1|true", line));
    }
}
