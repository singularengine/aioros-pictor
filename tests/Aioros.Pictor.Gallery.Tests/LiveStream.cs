using System.Text;

namespace Aioros.Pictor.Gallery.Tests;

/// <summary>
/// A live page's event stream as the browser reads it: opened once the server has subscribed it (its <c>: connected</c>
/// comment) and named it (its <c>connection</c> event, Aioros's live state per connection), then read one event at a time. Every read is bounded by <see cref="Patience"/> and the test's cancellation, so
/// a server that stops pushing fails the test instead of hanging it.
/// </summary>
internal sealed class LiveStream : IAsyncDisposable
{
    private readonly HttpResponseMessage _answer;
    private readonly StreamReader _reader;

    private LiveStream(HttpResponseMessage answer, StreamReader reader) => (_answer, _reader) = (answer, reader);

    public static async Task<LiveStream> OpenAsync(GalleryHost gallery, string path, CancellationToken cancellationToken)
    {
        using var bound = new Patience(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        var answer = await gallery.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, bound.Token);
        answer.EnsureSuccessStatusCode();
        var stream = new LiveStream(answer, new StreamReader(await answer.Content.ReadAsStreamAsync(bound.Token), Encoding.UTF8));
        while (await stream._reader.ReadLineAsync(bound.Token) is { } line)
        {
            if (line == ": connected")
            {
                var (name, _) = await stream.NextAsync(cancellationToken);
                return name == "connection" ? stream : throw new InvalidOperationException($"{path} opened with '{name}' instead of naming its connection");
            }
        }

        await stream.DisposeAsync();
        throw new InvalidOperationException($"{path} ended before it was connected");
    }

    /// <summary>The next event: its name (empty when unnamed) and its data, lines joined.</summary>
    public async Task<(string Name, string Data)> NextAsync(CancellationToken cancellationToken)
    {
        using var bound = new Patience(cancellationToken);
        var name = string.Empty;
        var data = new StringBuilder();
        while (await _reader.ReadLineAsync(bound.Token) is { } line)
        {
            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                name = line[7..];
            }
            else if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                data.Append(line, 6, line.Length - 6).Append('\n');
            }
            else if (line.Length == 0 && data.Length > 0)
            {
                return (name, data.ToString());
            }
        }

        throw new InvalidOperationException("The live stream ended");
    }

    /// <summary>Ticks the Gallery once and returns the next event.</summary>
    public async Task<(string Name, string Data)> TickedAsync(GalleryHost gallery, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(gallery);
        var next = NextAsync(cancellationToken);
        await gallery.TickAsync(cancellationToken);
        return await next;
    }

    public ValueTask DisposeAsync()
    {
        _reader.Dispose();
        _answer.Dispose();
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// A test's bound on how long it waits for the Gallery or the browser: the test's own cancellation, or 30 seconds on the
/// system clock, after which the wait fails (the bound of a failure, never the signal a test waits for).
/// </summary>
internal sealed class Patience : IDisposable
{
    public static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    private readonly CancellationTokenSource _timeout = new(Limit, TimeProvider.System);
    private readonly CancellationTokenSource _linked;

    public Patience(CancellationToken cancellationToken) => _linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _timeout.Token);

    public CancellationToken Token => _linked.Token;

    public void Dispose()
    {
        _linked.Dispose();
        _timeout.Dispose();
    }
}
