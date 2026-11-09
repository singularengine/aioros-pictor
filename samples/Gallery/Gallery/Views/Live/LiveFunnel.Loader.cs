using Aioros.Functions;
using Gallery.Models;

namespace Gallery.Views.Live;

/// <summary>Loads the live feed's snapshot for the funnel chart; it reads the family's own key (<c>live-funnel</c>), which the ticker invalidates every fourth second.</summary>
public sealed class LiveFunnelLoader(LiveFeed feed, IDataKeys keys)
{
    public Task<LiveData> LoadAsync() => LiveSchedule.ReadAsync(feed, keys, "funnel");
}
