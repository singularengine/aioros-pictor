using System.Buffers;
using System.Text;
using Aioros.Pictor.Data;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Audit;

public sealed class OutputAuditTests
{
    [Fact]
    [Trait("AC", "AUDIT-19")]
    public async Task KeptMarkupHoldsOnlyWhileKeysResolveAlikeAsync()
    {
        var example = await Reference.ExampleAsync("SimpleTreemap");
        var argument = Specs.SpecJson.Write(example.Spec);
        List<object?> capitalized = [Leaf(null, "Alpha", 10), Leaf(null, "Beta", 20)];
        List<object?> both = [Leaf("x", "Alpha", 10), Leaf("y", "Beta", 20)];
        var kept = Slots.Writer(argument, static (_, _) => null!);

        Write(kept, capitalized);
        Write(kept, capitalized);
        var served = Write(kept, both);

        Assert.Equal(Write(Slots.Writer(argument, static (_, _) => null!), both), served);
        Assert.Contains(">x<", served, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AUDIT-20")]
    public void EverySyncIdHasItsOwnClass()
    {
        string[] ids = ["ab", "a.b", "a_b", "a__b", "a_2e_b", "売上", "利益", "a b", "a-b"];

        var classes = ids.Select(SyncMarkup.Class).ToList();

        Assert.Equal(ids.Length, classes.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("pictor-y-ab", classes[0]);
        Assert.Equal("pictor-y-a-b", classes[^1]);
        Assert.All(classes, name => Assert.Matches("^pictor-y-[A-Za-z0-9_-]+$", name));
    }

    [Fact]
    [Trait("AC", "AUDIT-21")]
    public void ABooleanIsWrittenAsJavaScriptWritesIt()
    {
        var data = ChartData.From(new List<object?> { new Dictionary<string, object?> { ["flag"] = true }, new Dictionary<string, object?> { ["flag"] = false } }, static (_, _) => null);

        Assert.Equal(["true", "false"], data.Texts("flag"));
    }

    [Fact]
    [Trait("AC", "AUDIT-27")]
    public async Task AResponsiveLayoutWithMoreHoverStatesWritesItsRulesAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        List<object?> rows = [.. Enumerable.Range(0, 400).Select(i => (object?)new Dictionary<string, object?> { ["name"] = "p" + i, ["uv"] = 10 + i, ["pv"] = 5 + i })];
        var spec = example.Spec with { Responsive = true, ResponsiveWidths = [60, 1200], Legend = null, Tooltip = example.Spec.Tooltip! with { Shared = false } };

        var narrow = Painting.Plots.Compute(spec with { Width = 60 }, ChartData.From(rows, static (_, _) => null)).HoverCount;
        var wide = Painting.Plots.Compute(spec with { Width = 1200 }, ChartData.From(rows, static (_, _) => null)).HoverCount;
        var markup = Reference.Pictor(example with { Spec = spec, Data = rows });

        Assert.True(wide > narrow, $"{narrow} states at 150 px, {wide} at 1200 px");
        Assert.Contains(".pictor-hit{fill:transparent}", markup, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> Leaf(string? lower, string upper, double size)
    {
        var leaf = new Dictionary<string, object?> { ["Name"] = upper, ["size"] = size };
        if (lower is not null)
        {
            leaf["name"] = lower;
        }

        return leaf;
    }

    private static string Write(Aioros.Runtime.Rendering.SlotWrite write, object data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        write(data, buffer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
