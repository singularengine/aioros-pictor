using Gallery.Analytics;

namespace Gallery.Views;

/// <summary>Loads the live tile of the product in the query; it reads the live key, so each tick reloads it.</summary>
public sealed class KpiNowLoader(IAnalyticsFunctions analytics)
{
    public Task<Kpi> LoadAsync(string? app) => analytics.NowAsync(app ?? Products.Web.Id);
}
