using Gallery.Analytics;

namespace Gallery.Views;

/// <summary>Loads the live feed of the product in the query; it reads the live key, so each tick reloads it.</summary>
public sealed class EventFeedLoader(IAnalyticsFunctions analytics)
{
    public Task<FeedData> LoadAsync(string? app) => analytics.FeedAsync(app ?? Products.Web.Id);
}
