using Aioros.Functions;
using Gallery.Models;

namespace Gallery.Views.Live;

/// <summary>Loads the live feed's snapshot for the scatter chart; it reads the family's own key (<c>live-scatter</c>), which the ticker invalidates every fourth second.</summary>
public sealed class LiveScatterLoader(LiveFeed feed, IDataKeys keys)
{
    public Task<LiveData> LoadAsync() => LiveSchedule.ReadAsync(feed, keys, "scatter");
}
