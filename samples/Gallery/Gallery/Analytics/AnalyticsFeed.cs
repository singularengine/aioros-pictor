using System.Collections.Immutable;
using Aioros.Functions;

namespace Gallery.Analytics;

/// <summary>What the live views show of one product in the Live range at one moment.</summary>
public sealed record LiveProduct(KpiData Kpis, TrafficData Traffic, FeedData Feed);

/// <summary>The dashboard's moment: the time every view computes from, and the live views' data of every product then.</summary>
public sealed record AnalyticsMoment(DateTimeOffset Now, ImmutableDictionary<string, LiveProduct> Products);

/// <summary>
/// The dashboard's one owner of live state: the ticker advances it each second, and readers get the whole moment, replaced
/// at once (published with <see cref="Volatile"/>), so a view never mixes two ticks. Every number derives from the seeded
/// generator and the moment's time.
/// </summary>
public sealed class AnalyticsFeed
{
    private AnalyticsMoment _current;

    public AnalyticsFeed(TrafficModel model, TimeProvider time)
    {
        Model = model;
        _current = Compute(model, Second(time.GetLocalNow()));
    }

    public TrafficModel Model { get; }

    public AnalyticsMoment Current => Volatile.Read(ref _current);

    /// <summary>Moves the dashboard to <paramref name="now"/>; only the ticker calls it.</summary>
    public void Advance(DateTimeOffset now) => Volatile.Write(ref _current, Compute(Model, Second(now)));

    /// <summary>The moment's data of one product, computed for the moment.</summary>
    public static AnalyticsMoment Compute(TrafficModel model, DateTimeOffset now) =>
        new(now, Products.All.ToImmutableDictionary(
            product => product.Id,
            product => new LiveProduct(
                Dashboards.Kpis(model, product, TimeRange.Live, now),
                Dashboards.Traffic(model, product, TimeRange.Live, now),
                Dashboards.Feed(model, product, now))));

    private static DateTimeOffset Second(DateTimeOffset time) => new(time.Ticks - (time.Ticks % TimeSpan.TicksPerSecond), time.Offset);
}

/// <summary>
/// Advances the dashboard every second and invalidates its live key, so every live dashboard reruns only its live views'
/// loaders (KPIs, the main chart in the Live range, the feed); the history key follows each minute, for the main chart
/// over longer ranges.
/// </summary>
public sealed class AnalyticsTicker(AnalyticsFeed feed, IDataKeys keys, TimeProvider time) : Microsoft.Extensions.Hosting.BackgroundService
{
    public const string LiveKey = "analytics-live";

    public const string HistoryKey = "analytics-history";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), time, stoppingToken);
            Tick(feed, keys, time.GetLocalNow());
        }
    }

    /// <summary>One tick: the feed moves to <paramref name="now"/> and the keys whose data changed are invalidated.</summary>
    public static void Tick(AnalyticsFeed feed, IDataKeys keys, DateTimeOffset now)
    {
        var minute = feed.Current.Now.Minute;
        feed.Advance(now);
        keys.Invalidate(LiveKey);
        if (feed.Current.Now.Minute != minute)
        {
            keys.Invalidate(HistoryKey);
        }
    }
}
