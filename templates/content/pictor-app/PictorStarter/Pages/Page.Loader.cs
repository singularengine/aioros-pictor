using Aioros.Pages;
using PictorStarter.Analytics;

namespace PictorStarter.Pages;

/// <summary>Loads the dashboard for the product, range and focused page in the query (Nimbus, Live and none by default).</summary>
public sealed class HomeLoader(IAnalyticsFunctions analytics)
{
    public async Task<LoaderResult<DashboardData>> LoadAsync(string? app, string? range, int? focus) =>
        LoaderResult.With(
            await analytics.OverviewAsync(app ?? Products.Web.Id, range ?? TimeRange.Live.Id, focus ?? -1),
            "Live analytics",
            new PageMetadata("A live analytics dashboard drawn by Aioros.Pictor, charts in pure XAML: server-rendered on the web, native on the desktop."));
}
