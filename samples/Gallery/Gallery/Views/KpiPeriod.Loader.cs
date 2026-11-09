using Gallery.Analytics;

namespace Gallery.Views;

/// <summary>Loads the period's tiles of the product and range in the query; they read the history key, so they refresh each minute.</summary>
public sealed class KpiPeriodLoader(IAnalyticsFunctions analytics)
{
    public Task<KpiData> LoadAsync(string? app, string? range) => analytics.KpisAsync(app ?? Products.Web.Id, range ?? TimeRange.Live.Id);
}
