using System.Collections.Immutable;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Content;

/// <summary>The content a refused template holds for a call: what Avalonia drew for that item when the page was rendered.</summary>
internal static class ContentResolution
{
    public static ImmutableArray<ContentNode> Nodes(ChartSpec? spec, long key)
    {
        foreach (var resolved in spec is null ? [] : spec.Resolved)
        {
            if (resolved.Key == key)
            {
                return resolved.Nodes;
            }
        }

        return [];
    }
}
