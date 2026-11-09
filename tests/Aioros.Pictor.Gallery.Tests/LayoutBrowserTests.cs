using Microsoft.Playwright;

namespace Aioros.Pictor.Gallery.Tests;

public sealed class LayoutBrowserTests(GalleryHost gallery)
{
    private static readonly string[] Routes =
    [
        "/dashboard", "/about", "/line", "/area", "/bar", "/composed", "/scatter", "/pie", "/radar", "/radial-bar", "/funnel", "/treemap", "/sankey", "/sunburst",
        "/compare", "/axes", "/tips", "/ranges", "/custom", "/interactions", "/interactive", "/families", "/live-charts", "/updates", "/binding", "/themes",
        "/features", "/more", "/extras", "/compare/line", "/compare/area", "/compare/bar", "/compare/composed", "/compare/scatter", "/compare/pie",
        "/compare/radar", "/compare/radial-bar", "/compare/funnel", "/compare/treemap", "/compare/sankey", "/compare/sunburst",
    ];

    private const string Faults = """
        () => {
          const width = document.documentElement.clientWidth;
          const faults = [];
          if (document.scrollingElement.scrollWidth > width) faults.push(`scrolls sideways by ${document.scrollingElement.scrollWidth - width} px`);
          const scroller = (element) => {
            for (let parent = element.parentElement; parent && parent !== document.body; parent = parent.parentElement) {
              if (/(auto|scroll|hidden|clip)/.test(getComputedStyle(parent).overflowX)) {
                const box = parent.getBoundingClientRect();
                if (box.left >= -1 && box.right <= width + 1) return parent;
              }
            }
            return null;
          };
          for (const element of document.querySelectorAll('body *')) {
            if (element.closest('svg') && element.tagName.toLowerCase() !== 'svg') continue;
            const box = element.getBoundingClientRect();
            if (box.width === 0 || box.height === 0) continue;
            const style = getComputedStyle(element);
            if (style.display === 'none' || style.visibility === 'hidden') continue;
            const held = scroller(element);
            if ((box.left < -1 || box.right > width + 1) && !held) faults.push(`${element.tagName.toLowerCase()}.${element.getAttribute('class') ?? ''} spans ${Math.round(box.left)}..${Math.round(box.right)}`);
            if (held && held.scrollLeft === 0 && box.left < held.getBoundingClientRect().left - 1) faults.push(`${element.tagName.toLowerCase()}.${element.getAttribute('class') ?? ''} starts at ${Math.round(box.left)}, left of its scroll box at ${Math.round(held.getBoundingClientRect().left)}`);
          }
          const links = [...document.querySelectorAll('a')].map((link) => {
            const range = document.createRange();
            range.selectNodeContents(link);
            return [link, range.getBoundingClientRect()];
          }).filter(([, box]) => box.width > 0 && box.height > 0);
          for (let i = 0; i < links.length; i++) {
            for (let j = i + 1; j < links.length; j++) {
              const [a, r] = links[i], [b, s] = links[j];
              if (a.contains(b) || b.contains(a)) continue;
              if (Math.min(r.right, s.right) - Math.max(r.left, s.left) > 2 && Math.min(r.bottom, s.bottom) - Math.max(r.top, s.top) > 2) faults.push(`"${a.innerText}" overprints "${b.innerText}"`);
            }
          }
          return faults.slice(0, 5);
        }
        """;

    private const string Tiles = """
        () => {
          const card = (element) => { for (let e = element; e; e = e.parentElement) { if (getComputedStyle(e).borderTopLeftRadius === '12px') return e; } return element; };
          const caption = [...document.querySelectorAll('div')].find((element) => element.childElementCount === 0 && element.textContent.startsWith('vs 30 minutes'));
          const boxes = ['Active users now', 'Page views', 'Conversion rate', 'Revenue'].map((text) => {
            const label = [...document.querySelectorAll('div')].find((element) => element.childElementCount === 0 && element.textContent.trim() === text);
            const box = card(label).getBoundingClientRect();
            return { text, left: Math.round(box.left), top: Math.round(box.top), width: Math.round(box.width), clipped: label.scrollWidth > label.clientWidth };
          });
          const traffic = card([...document.querySelectorAll('div')].find((element) => element.childElementCount === 0 && element.textContent.startsWith('Visitors'))).getBoundingClientRect();
          return { boxes, main: Math.round(traffic.width), captionClipped: caption.scrollWidth > caption.clientWidth };
        }
        """;

    [Fact(Timeout = 300_000)]
    [Trait("AC", "GALLERY-03")]
    public async Task NoRouteScrollsSidewaysOrOverprintsOnAPhoneOrATabletAsync()
    {
        var cancel = TestContext.Current.CancellationToken;
        await using var browser = await BrowserAsync();
        var faults = new List<string>();
        foreach (var width in new[] { 390, 1024 })
        {
            var page = await browser.NewPageAsync(width, 844);
            foreach (var route in Routes)
            {
                cancel.ThrowIfCancellationRequested();
                await page.GotoAsync(gallery.Address + route.TrimStart('/'), new() { WaitUntil = WaitUntilState.Load });
                faults.AddRange((await page.EvaluateAsync<string[]>(Faults)).Select(fault => $"{route} at {width} px: {fault}"));
            }

            await page.CloseAsync();
        }

        Assert.True(faults.Count == 0, string.Join('\n', faults));
    }

    [Fact(Timeout = 90_000)]
    [Trait("AC", "DASHBOARD-12")]
    public async Task TheKpiTilesStackOnAPhoneAndShareARowOnADesktopAsync()
    {
        await using var browser = await BrowserAsync();
        var phone = await browser.NewPageAsync(390, 844);
        await phone.GotoAsync(gallery.Address + "dashboard?app=web&range=live", new() { WaitUntil = WaitUntilState.Load }).WaitAsync(TestContext.Current.CancellationToken);
        var stacked = Read(await phone.EvaluateAsync<System.Text.Json.JsonElement>(Tiles));
        var desktop = await browser.NewPageAsync(1440, 900);
        await desktop.GotoAsync(gallery.Address + "dashboard?app=web&range=live", new() { WaitUntil = WaitUntilState.Load });
        var row = Read(await desktop.EvaluateAsync<System.Text.Json.JsonElement>(Tiles));

        Assert.All(stacked.Boxes, box => Assert.InRange(box.Width, stacked.Main - 1, stacked.Main + 1));
        Assert.All(stacked.Boxes, box => Assert.Equal(stacked.Boxes[0].Left, box.Left));
        Assert.All(stacked.Boxes.Zip(stacked.Boxes.Skip(1)), pair => Assert.True(pair.Second.Top > pair.First.Top, $"{pair.Second.Text} above {pair.First.Text}"));
        Assert.All(stacked.Boxes, box => Assert.False(box.Clipped, box.Text));
        Assert.False(stacked.CaptionClipped);
        Assert.All(row.Boxes, box => Assert.Equal(row.Boxes[0].Top, box.Top));
    }

    private static Layout Read(System.Text.Json.JsonElement layout) => new(
        [.. layout.GetProperty("boxes").EnumerateArray().Select(box => new Box(box.GetProperty("text").GetString()!, box.GetProperty("left").GetInt32(), box.GetProperty("top").GetInt32(), box.GetProperty("width").GetInt32(), box.GetProperty("clipped").GetBoolean()))],
        layout.GetProperty("main").GetInt32(),
        layout.GetProperty("captionClipped").GetBoolean());

    private sealed record Box(string Text, int Left, int Top, int Width, bool Clipped);

    private sealed record Layout(Box[] Boxes, int Main, bool CaptionClipped);

    private static async Task<Browser> BrowserAsync()
    {
        Browsers.Require();
        var playwright = await Playwright.CreateAsync();
        var chromium = await playwright.Chromium.LaunchAsync(new() { ExecutablePath = Browsers.Chromium, Args = ["--no-sandbox"] });
        return new Browser(playwright, chromium);
    }

    private sealed class Browser(IPlaywright playwright, IBrowser browser) : IAsyncDisposable
    {
        public Task<IPage> NewPageAsync(int width, int height) => browser.NewPageAsync(new() { ViewportSize = new() { Width = width, Height = height } });

        public async ValueTask DisposeAsync()
        {
            await browser.DisposeAsync();
            playwright.Dispose();
        }
    }
}
