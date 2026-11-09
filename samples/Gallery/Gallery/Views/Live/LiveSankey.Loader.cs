using Aioros.Functions;
using Gallery.Models;

namespace Gallery.Views.Live;

/// <summary>Loads the live feed's snapshot for the sankey; it reads the family's own key (<c>live-sankey</c>), which the ticker invalidates every fourth second.</summary>
public sealed class LiveSankeyLoader(LiveFeed feed, IDataKeys keys)
{
    public Task<LiveData> LoadAsync() => LiveSchedule.ReadAsync(feed, keys, "sankey");
}
