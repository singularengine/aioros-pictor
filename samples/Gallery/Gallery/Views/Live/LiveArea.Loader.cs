using Aioros.Functions;
using Gallery.Models;

namespace Gallery.Views.Live;

/// <summary>Loads the live feed's snapshot for the area chart; it reads the family's own key (<c>live-area</c>), which the ticker invalidates every fourth second.</summary>
public sealed class LiveAreaLoader(LiveFeed feed, IDataKeys keys)
{
    public Task<LiveData> LoadAsync() => LiveSchedule.ReadAsync(feed, keys, "area");
}
