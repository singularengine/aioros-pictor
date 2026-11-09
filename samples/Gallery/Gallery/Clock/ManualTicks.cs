using System.Threading.Channels;
using Aioros.Functions;
using Gallery.Analytics;
using Gallery.Models;

namespace Gallery.Clock;

/// <summary>
/// The Gallery's time when the environment variable <c>GALLERY_CLOCK</c> names a start (an ISO 8601 moment): it stands
/// still until <see cref="ManualTicker"/> moves it, so the tests drive the live views' ticks instead of waiting on seconds.
/// </summary>
public sealed class SteppedClock(DateTimeOffset start) : TimeProvider
{
    private long _ticks = start.UtcTicks;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => new(Volatile.Read(ref _ticks), TimeSpan.Zero);

    /// <summary>Moves the clock forward; only the ticker calls it.</summary>
    public void Advance(TimeSpan step) => Interlocked.Add(ref _ticks, step.Ticks);
}

/// <summary>
/// The live views' ticks on request, in place of the one-second tickers: each request queues a tick that one reader runs
/// in order (the clock moves a second, the live charts' feed advances with the keys of the families due, and the analytics feed follows the clock, with their
/// keys invalidated as the tickers do), and the request completes once its tick has run.
/// </summary>
public sealed class ManualTicker(SteppedClock clock, LiveFeed live, AnalyticsFeed analytics, IDataKeys keys) : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly Channel<TaskCompletionSource> _requests = Channel.CreateUnbounded<TaskCompletionSource>(new UnboundedChannelOptions { SingleReader = true });

    /// <summary>Runs one tick and completes when it has run.</summary>
    public async Task TickAsync(CancellationToken cancellationToken)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await _requests.Writer.WriteAsync(done, cancellationToken);
        await done.Task.WaitAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var done in _requests.Reader.ReadAllAsync(stoppingToken))
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            LiveTicker.Tick(live, keys);
            AnalyticsTicker.Tick(analytics, keys, clock.GetLocalNow());
            done.SetResult();
        }
    }
}
