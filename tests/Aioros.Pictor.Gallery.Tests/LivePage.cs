using Microsoft.Playwright;

namespace Aioros.Pictor.Gallery.Tests;

/// <summary>
/// A Gallery page in the browser whose live pushes the test can await: an init script counts each event the framework's
/// live stream has applied (<c>window.pictorPushes</c>, after the framework's own handler ran) and keeps the latest stream,
/// so a test waits for that stream to be open (the server subscribed it), ticks the Gallery's clock and awaits the push.
/// </summary>
internal static class LivePage
{
    private const string Counter = """
        (() => {
          const Native = window.EventSource;
          window.pictorPushes = 0;
          const counted = (listener) => function (event) { const result = listener.call(this, event); window.pictorPushes++; return result; };
          window.EventSource = class extends Native {
            constructor(...args) { super(...args); window.pictorLive = this; }
            addEventListener(type, listener, options) { super.addEventListener(type, typeof listener === 'function' ? counted(listener) : listener, options); }
            set onmessage(listener) { super.onmessage = listener ? counted(listener) : listener; }
            get onmessage() { return super.onmessage; }
          };
        })();
        """;

    /// <summary>Counts the live pushes of every document the page opens.</summary>
    public static Task CountPushesAsync(IPage page) => page.AddInitScriptAsync(Counter);

    /// <summary>
    /// Ticks the Gallery's clock once the page's latest live stream is open, and returns once the page has applied a push
    /// after it.
    /// </summary>
    public static async Task PushAsync(GalleryHost gallery, IPage page)
    {
        await page.WaitForFunctionAsync("() => window.pictorLive !== undefined && window.pictorLive.readyState === 1", null, new() { Timeout = (float)Patience.Limit.TotalMilliseconds });
        var before = await page.EvaluateAsync<int>("() => window.pictorPushes");
        var pushed = page.WaitForFunctionAsync("before => window.pictorPushes > before", before, new() { Timeout = (float)Patience.Limit.TotalMilliseconds });
        await gallery.TickAsync(TestContext.Current.CancellationToken);
        await pushed;
    }
}
