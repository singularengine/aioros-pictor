using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>What an item's own members give its shape, as Recharts spreads an item over its shape and a <c>Cell</c>'s props over both.</summary>
internal static class ItemStyles
{
    /// <summary>The series' cells with each item's opacity under its cell's, one cell per item.</summary>
    public static ImmutableArray<CellSpec> Cells(ImmutableArray<CellSpec> cells, double[] opacities)
    {
        var merged = new CellSpec[opacities.Length];
        for (var i = 0; i < merged.Length; i++)
        {
            var cell = i < cells.Length ? cells[i] : new CellSpec(null);
            merged[i] = double.IsNaN(opacities[i]) || cell.Opacity is not null ? cell : cell with { Opacity = opacities[i] };
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(merged);
    }
}
