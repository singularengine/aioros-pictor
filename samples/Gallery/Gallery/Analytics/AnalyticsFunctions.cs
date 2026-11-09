using Aioros.Functions;

namespace Gallery.Analytics;

/// <summary>The dashboard's data, as the loaders of its page and of its live views ask for it.</summary>
public interface IAnalyticsFunctions
{
    /// <summary>Everything outside the live views; read once per request, never invalidated.</summary>
    Task<DashboardData> OverviewAsync(string app, string range, int focus);

    /// <summary>The live tile (active users or requests per second now), refreshed each second.</summary>
    Task<Kpi> NowAsync(string app);

    /// <summary>The period's tiles (volume, rate, value against the previous period), refreshed each minute.</summary>
    Task<KpiData> KpisAsync(string app, string range);

    /// <summary>The main chart in the Live range, refreshed each second.</summary>
    Task<TrafficData> LiveTrafficAsync(string app, int focus);

    /// <summary>The main chart over a longer range, refreshed each minute.</summary>
    Task<TrafficData> TrafficAsync(string app, string range, int focus);

    /// <summary>The live feed, refreshed each second.</summary>
    Task<FeedData> FeedAsync(string app);
}

/// <summary>
/// Reads the dashboard's moment from its one owner. A result serves every visitor between two ticks (shared and kept a
/// second), so a thousand dashboards open on the same product cost one computation per tick.
/// </summary>
public sealed class AnalyticsFunctions(AnalyticsFeed feed) : IAnalyticsFunctions
{
    public Task<DashboardData> OverviewAsync(string app, string range, int focus) =>
        Task.FromResult(Dashboards.Overview(feed.Model, Products.Find(app), TimeRange.Find(range), feed.Current.Now, focus));

    [Cached(1, Shared = true)]
    [Reads(AnalyticsTicker.LiveKey)]
    public Task<Kpi> NowAsync(string app) => Task.FromResult(feed.Current.Products[Products.Find(app).Id].Kpis.Now);

    [Cached(1, Shared = true)]
    [Reads(AnalyticsTicker.HistoryKey)]
    public Task<KpiData> KpisAsync(string app, string range)
    {
        var moment = feed.Current;
        var product = Products.Find(app);
        var period = TimeRange.Find(range);
        return Task.FromResult(period.IsLive ? moment.Products[product.Id].Kpis : Dashboards.Kpis(feed.Model, product, period, moment.Now));
    }

    [Cached(1, Shared = true)]
    [Reads(AnalyticsTicker.LiveKey)]
    public Task<TrafficData> LiveTrafficAsync(string app, int focus)
    {
        var moment = feed.Current;
        var product = Products.Find(app);
        return Task.FromResult(focus < 0 ? moment.Products[product.Id].Traffic : Dashboards.Traffic(feed.Model, product, TimeRange.Live, moment.Now, focus));
    }

    [Cached(1, Shared = true)]
    [Reads(AnalyticsTicker.HistoryKey)]
    public Task<TrafficData> TrafficAsync(string app, string range, int focus) =>
        Task.FromResult(Dashboards.Traffic(feed.Model, Products.Find(app), TimeRange.Find(range), feed.Current.Now, focus));

    [Cached(1, Shared = true)]
    [Reads(AnalyticsTicker.LiveKey)]
    public Task<FeedData> FeedAsync(string app) => Task.FromResult(feed.Current.Products[Products.Find(app).Id].Feed);
}
