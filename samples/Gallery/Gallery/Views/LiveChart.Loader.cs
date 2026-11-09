using Gallery.Models;

namespace Gallery.Views;

/// <summary>Loads the live feed's snapshot; the <c>live</c> key it reads makes every live page reload it on each tick.</summary>
public sealed class LiveChartLoader(ILiveFunctions live)
{
    public async Task<LiveData> LoadAsync() => await live.SnapshotAsync();
}
