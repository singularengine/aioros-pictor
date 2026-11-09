using Gallery.Analytics;

namespace Gallery.Views;

/// <summary>
/// Loads the main chart for the product, range and focused page in the query: in the Live range it reads the live key and
/// reloads each tick; over a longer range it reads the history key, which changes each minute. The brush window in the
/// query (<c>from</c>, <c>to</c>) comes with it, so a live connection opened at that address keeps the window.
/// </summary>
public sealed class TrafficChartLoader(IAnalyticsFunctions analytics)
{
    public async Task<TrafficState> LoadAsync(string? app, string? range, int? focus, int? from, int? to)
    {
        var product = app ?? Products.Web.Id;
        var period = TimeRange.Find(range);
        var data = period.IsLive
            ? await analytics.LiveTrafficAsync(product, focus ?? -1)
            : await analytics.TrafficAsync(product, range ?? TimeRange.Live.Id, focus ?? -1);
        return new TrafficState(data, product, period.Id, focus ?? -1, from, to);
    }
}
