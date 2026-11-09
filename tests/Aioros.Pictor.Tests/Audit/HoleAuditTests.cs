using Aioros.Pictor.BoundValues;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Audit;

public sealed class HoleAuditTests
{
    [Fact]
    [Trait("AC", "AUDIT-35")]
    public async Task ABoundTableIsComparedOnItsLiveItemsAsync()
    {
        var example = await Reference.ExampleAsync("TwoSimplePieChart");
        var hole = new SpecHole("p0", HoleKind.Table, ["/Polar/Pies/0/Data"]) { Keys = ["name", "value"] };
        var spec = example.Spec with { Polar = example.Spec.Polar! with { Pies = example.Spec.Polar.Pies.SetItem(0, example.Spec.Polar.Pies[0] with { Hide = true }) } };
        var render = ChartRenderer.Create(SpecHoles.Write(spec, [hole]), static (_, _) => null!);
        var holes = (SpecHoles)render.Target!;
        var items = Enumerable.Range(0, 2000).Select(i => new Dictionary<string, object?> { ["name"] = "Item " + i, ["value"] = (double)(i % 50) + 1 }).ToList();
        var bound = new Dictionary<string, object?> { ["p0"] = items };

        var first = Slots.Render(render, example.Data, bound).Html;
        Slots.Render(render, example.Data, bound);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var again = Slots.Render(render, example.Data, bound).Html;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        var fills = holes.Fills;
        items[3]["value"] = 999.0;
        Slots.Render(render, example.Data, bound);

        Assert.Equal(first, again);
        Assert.Equal(1, fills);
        Assert.True(allocated < (8 * first.Length) + 16_000, $"{allocated} bytes allocated for a {first.Length}-character chart");
        Assert.Equal(2, holes.Fills);
    }
}
