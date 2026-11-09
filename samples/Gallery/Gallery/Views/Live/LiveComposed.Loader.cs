using Aioros.Functions;
using Gallery.Models;

namespace Gallery.Views.Live;

/// <summary>Loads the live feed's snapshot for the composed chart; it reads the family's own key (<c>live-composed</c>), which the ticker invalidates every fourth second.</summary>
public sealed class LiveComposedLoader(LiveFeed feed, IDataKeys keys)
{
    public Task<LiveData> LoadAsync() => LiveSchedule.ReadAsync(feed, keys, "composed");
}
