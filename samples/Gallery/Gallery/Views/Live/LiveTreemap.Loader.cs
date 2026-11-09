using Aioros.Functions;
using Gallery.Models;

namespace Gallery.Views.Live;

/// <summary>Loads the live feed's snapshot for the treemap; it reads the family's own key (<c>live-treemap</c>), which the ticker invalidates every fourth second.</summary>
public sealed class LiveTreemapLoader(LiveFeed feed, IDataKeys keys)
{
    public Task<LiveData> LoadAsync() => LiveSchedule.ReadAsync(feed, keys, "treemap");
}
