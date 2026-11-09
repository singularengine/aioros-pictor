using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Aioros.Pictor.Gallery.Tests;

/// <summary>
/// What the coverage claims of the web charts in a browser, checked in Chromium on the running Gallery with the
/// framework's script: the touch follow, the roving focus and the box following the pointer. Every step awaits the
/// browser's delivery of its input (counted by listeners the page carries) before it reads what the chart shows.
/// </summary>
public sealed partial class BrowserBehaviorTests(GalleryHost gallery)
{
    private const string Delivered = """
        (() => {
          window.pictorInputs = {};
          for (const type of ['touchstart', 'touchmove', 'touchend', 'keydown', 'mousemove', 'click'])
            window.addEventListener(type, () => { window.pictorInputs[type] = (window.pictorInputs[type] ?? 0) + 1; }, {capture: true, passive: true});
        })();
        """;

    private const string Shown = """
        (selector) => {
          const wrapper = document.querySelector(selector).closest('.recharts-wrapper');
          const tips = [...wrapper.querySelectorAll('.pictor-hover > [class^=pictor-tk]')];
          const visible = tips.map((tip, i) => (getComputedStyle(tip).display !== 'none' ? i : -1)).filter((i) => i >= 0);
          return visible.length > 1 ? -2 : visible[0] ?? -1;
        }
        """;

    private const string Escaped = """
        (s) => {
          const wrapper = document.querySelector(s).closest('.recharts-wrapper');
          const tips = [...wrapper.querySelectorAll('.pictor-hover > [class^=pictor-tk]')].filter((t) => getComputedStyle(t).display !== 'none');
          return {box: tips.some((t) => getComputedStyle(t.querySelector('foreignObject')).display !== 'none'), cursor: [...wrapper.querySelectorAll('.pictor-cursors > g')].some((c) => getComputedStyle(c).display !== 'none'), dots: tips.reduce((n, t) => n + [...t.querySelectorAll('circle')].filter((c) => getComputedStyle(c).display !== 'none').length, 0)};
        }
        """;

    private const string Centers = """
        (selector) => {
          const hits = document.querySelector(selector);
          hits.closest('.recharts-wrapper').scrollIntoView({block: 'center'});
          return [...hits.children].filter((g) => !g.matches('[class^=pictor-tk]')).map((g) => { const b = g.querySelector('.pictor-hit').getBoundingClientRect(); return [Math.round(b.x + b.width / 2), Math.round(b.y + b.height / 2)]; });
        }
        """;

    [Fact]
    [Trait("AC", "TOUCH-19")]
    public async Task AFingerMovesTheTooltipAcrossCategoriesAndSyncedChartsInChromiumAsync()
    {
        await using var browser = await ChromiumAsync(touch: true);
        var page = await browser.PageAsync(gallery.Address + "interactive");
        const string Roving = "[data-aioros-roving]";
        var followed = await DragAsync(page, Roving, [2, 3, 4]);
        await TapAsync(page, 5, 5);
        var outside = await page.EvaluateAsync<JsonElement>(Escaped, Roving);

        await page.EvaluateAsync("() => document.querySelectorAll('.pictor-y-pages .pictor-hover').forEach((l, i) => l.setAttribute('data-check', 'pages-' + i))");
        var dragged = await DragAsync(page, "[data-check=pages-0]", [1, 2, 3]);
        var other = await page.EvaluateAsync<int>(Shown, "[data-check=pages-1]");
        await TapAsync(page, 5, 5);
        var hidden = (await page.EvaluateAsync<int>(Shown, "[data-check=pages-0]"), await page.EvaluateAsync<int>(Shown, "[data-check=pages-1]"));

        var features = await browser.PageAsync(gallery.Address + "features");
        const string Capture = "[data-aioros-follow=capture]";
        var action = await features.EvaluateAsync<string>("(s) => getComputedStyle(document.querySelector(s)).touchAction", Capture);
        await features.EvaluateAsync<int[][]>(Centers, Capture);
        var before = await features.EvaluateAsync<double>("() => scrollY");
        var vertical = await DragAsync(features, Capture, [1, 3, 5]);
        var scrolled = await features.EvaluateAsync<double>("() => scrollY") - before;

        Assert.Equal([2, 3, 4, 4], followed);
        Assert.Equal((false, true, 2), Marks(outside));
        Assert.Equal([1, 2, 3, 3], dragged);
        Assert.Equal(3, other);
        Assert.Equal((-1, -1), hidden);
        Assert.Equal("none", action);
        Assert.Equal([1, 3, 5, 5], vertical);
        Assert.Equal(0, scrolled);
    }

    [Fact]
    [Trait("AC", "KEYBOARD-13")]
    public async Task TheRovingFocusMovesTheTooltipAsRechartsKeysDoInChromiumAsync()
    {
        await using var browser = await ChromiumAsync(touch: false);
        var page = await browser.PageAsync(gallery.Address + "interactive");
        const string Roving = "[data-aioros-roving]";
        var stops = await page.EvaluateAsync<int>("(s) => document.querySelector(s).closest('.recharts-wrapper').querySelectorAll(\"[tabindex='00'],[tabindex='0']\").length", Roving);
        await page.EvaluateAsync("(s) => document.querySelector(s).closest('.recharts-wrapper').scrollIntoView({block: 'center'})", Roving);
        await page.EvaluateAsync("(s) => { const start = document.createElement('button'); start.id = 'pictor-start'; document.querySelector(s).closest('.recharts-wrapper').before(start); }", Roving);
        await page.FocusAsync("#pictor-start");
        var seen = new List<int>();
        foreach (var key in (string[])["Tab", "ArrowRight", "ArrowRight", "ArrowLeft", "ArrowLeft", "ArrowLeft", "Home", "End", "ArrowDown", "Escape"])
        {
            await PressAsync(page, key);
            seen.Add(await page.EvaluateAsync<int>(Shown, Roving));
        }

        var escaped = await page.EvaluateAsync<JsonElement>(Escaped, Roving);

        Assert.Equal(1, stops);
        Assert.Equal([0, 1, 2, 1, 0, 0, 0, 0, 0, 0], seen);
        Assert.Equal((false, true, 2), Marks(escaped));
    }

    [Fact]
    [Trait("AC", "TOOLTIP-23")]
    public async Task TheBoxFollowsThePointerWhereRechartsBoxStandsInChromiumAsync()
    {
        await using var browser = await ChromiumAsync(touch: false);
        var differences = new List<string>();
        foreach (var (path, title, name) in ((string, string, string)[])[("tips", "The box follows the pointer", "DetailsFollowLineChart"), ("tips", "Following the pointer, reversed", "DetailsFollowReversedChart"), ("radar", "SpecifiedDomainRadarChart", "DetailsFollowRadarChart"), ("radial-bar", "SimpleRadialBarChart", "DetailsFollowRadialBarChart")])
        {
            var page = await browser.PageAsync(gallery.Address + path);
            var surface = await page.EvaluateAsync<double[]>("""
                (t) => {
                  const heading = [...document.querySelectorAll('*')].find((e) => e.childElementCount === 0 && e.textContent.trim() === t);
                  const svg = heading.parentElement.querySelector('svg.recharts-surface');
                  svg.scrollIntoView({block: 'center'});
                  const r = svg.getBoundingClientRect();
                  return [r.x, r.y];
                }
                """, title);
            var points = await PointsAsync(name);
            for (var i = 0; i < points.Count; i++)
            {
                var moves = await page.EvaluateAsync<int>("() => window.pictorInputs.mousemove ?? 0");
                await page.Mouse.MoveAsync((float)(surface[0] + points[i].X), (float)(surface[1] + points[i].Y));
                await page.WaitForFunctionAsync("(n) => (window.pictorInputs.mousemove ?? 0) > n", moves, new() { Timeout = Bound });
                var shown = await page.EvaluateAsync<double[][]>("""
                    (t) => {
                      const heading = [...document.querySelectorAll('*')].find((e) => e.childElementCount === 0 && e.textContent.trim() === t);
                      const svg = heading.parentElement.querySelector('svg.recharts-surface');
                      const s = svg.getBoundingClientRect();
                      const tips = [...svg.querySelectorAll('.pictor-hover > [class^=pictor-tk]')].filter((g) => getComputedStyle(g).display !== 'none');
                      return tips.map((g) => { const r = g.querySelector('.recharts-default-tooltip').getBoundingClientRect(); return [r.x - s.x, r.y - s.y]; });
                    }
                    """, title);
                var expected = await RechartsBoxAsync(name, i);
                var same = expected is null ? shown.Length == 0 : shown.Length == 1 && Math.Abs(shown[0][0] - expected.Value.X) <= 1 && Math.Abs(shown[0][1] - expected.Value.Y) <= 1;
                if (!same)
                {
                    differences.Add($"{name} at ({points[i].X}, {points[i].Y}): Pictor [{string.Join("; ", shown.Select(box => string.Join(", ", box)))}], Recharts {expected}");
                }
            }
        }

        Assert.Empty(differences);
    }

    private static (bool Box, bool Cursor, int Dots) Marks(JsonElement shown) => (shown.GetProperty("box").GetBoolean(), shown.GetProperty("cursor").GetBoolean(), shown.GetProperty("dots").GetInt32());

    private static float Bound => (float)Patience.Limit.TotalMilliseconds;

    private static async Task<List<int>> DragAsync(IPage page, string layer, int[] indices)
    {
        var at = await page.EvaluateAsync<int[][]>(Centers, layer);
        var seen = new List<int>();
        var cdp = await page.Context.NewCDPSessionAsync(page);
        await TouchAsync(page, cdp, "touchStart", "touchstart", at[indices[0]]);
        seen.Add(await page.EvaluateAsync<int>(Shown, layer));
        foreach (var index in indices.Skip(1))
        {
            await TouchAsync(page, cdp, "touchMove", "touchmove", at[index]);
            seen.Add(await page.EvaluateAsync<int>(Shown, layer));
        }

        await TouchAsync(page, cdp, "touchEnd", "touchend", null);
        seen.Add(await page.EvaluateAsync<int>(Shown, layer));
        await cdp.DetachAsync();
        return seen;
    }

    /// <summary>Dispatches a touch event through CDP and returns once the page has received it.</summary>
    private static async Task TouchAsync(IPage page, ICDPSession cdp, string type, string received, int[]? at)
    {
        var count = await page.EvaluateAsync<int>("(t) => window.pictorInputs[t] ?? 0", received);
        object[] points = at is null ? [] : [new Dictionary<string, object> { ["x"] = at[0], ["y"] = at[1] }];
        await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object> { ["type"] = type, ["touchPoints"] = points });
        await page.WaitForFunctionAsync("([t, n]) => (window.pictorInputs[t] ?? 0) > n", new object[] { received, count }, new() { Timeout = Bound });
    }

    /// <summary>Taps the page and returns once the click the tap synthesizes (after its touch and mouse events) has been received.</summary>
    private static async Task TapAsync(IPage page, float x, float y)
    {
        var count = await page.EvaluateAsync<int>("() => window.pictorInputs.click ?? 0");
        await page.Touchscreen.TapAsync(x, y);
        await page.WaitForFunctionAsync("(n) => (window.pictorInputs.click ?? 0) > n", count, new() { Timeout = Bound });
    }

    private static async Task PressAsync(IPage page, string key)
    {
        var count = await page.EvaluateAsync<int>("() => window.pictorInputs.keydown ?? 0");
        await page.Keyboard.PressAsync(key);
        await page.WaitForFunctionAsync("(n) => (window.pictorInputs.keydown ?? 0) > n", count, new() { Timeout = Bound });
    }

    /// <summary>The example's mouse positions: the last hover of each of its interactions.</summary>
    private static async Task<List<(double X, double Y)>> PointsAsync(string name)
    {
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(GalleryHost.ProjectFolder, "..", "..", "..", "tools", "recharts", "examples", name + ".json"), TestContext.Current.CancellationToken));
        return [.. document.RootElement.GetProperty("interactions").EnumerateArray().Select(steps => steps[steps.GetArrayLength() - 1].GetProperty("hover")).Select(hover => (hover[0].GetDouble(), hover[1].GetDouble()))];
    }

    /// <summary>Where Recharts' box stands after the example's interaction in Chromium (its wrapper's translation), null when hidden.</summary>
    private static async Task<(double X, double Y)?> RechartsBoxAsync(string name, int interaction)
    {
        var folder = Path.Combine(GalleryHost.ProjectFolder, "..", "..", "..", "tools", "recharts", "out");
        var followed = Path.Combine(folder, "recharts3", $"{name}.{interaction}.client.html");
        var html = await File.ReadAllTextAsync(File.Exists(followed) ? followed : Path.Combine(folder, $"{name}.{interaction}.client.html"), TestContext.Current.CancellationToken);
        var match = Wrapper().Match(html);
        return !match.Success || match.Groups[1].Value.Contains("visibility: hidden", StringComparison.Ordinal) ? null : (double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), double.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static async Task<Chromium> ChromiumAsync(bool touch)
    {
        Browsers.Require();
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { ExecutablePath = Browsers.Chromium, Args = ["--no-sandbox"] });
        var context = await browser.NewContextAsync(new() { HasTouch = touch, ViewportSize = touch ? new() { Width = 900, Height = 700 } : new() { Width = 1400, Height = 1000 }, DeviceScaleFactor = 1 });
        await context.AddInitScriptAsync(Delivered);
        return new Chromium(playwright, browser, context);
    }

    [GeneratedRegex(@"recharts-tooltip-wrapper[^""]*"" style=""([^""]*)translate\(([-\d.]+)px, ([-\d.]+)px\)")]
    private static partial Regex Wrapper();

    private sealed class Chromium(IPlaywright playwright, IBrowser browser, IBrowserContext context) : IAsyncDisposable
    {
        /// <summary>A page of the Gallery once its framework script and its styles have loaded.</summary>
        public async Task<IPage> PageAsync(string address)
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(address, new() { WaitUntil = WaitUntilState.Load });
            return page;
        }

        public async ValueTask DisposeAsync()
        {
            await browser.DisposeAsync();
            playwright.Dispose();
        }
    }
}
