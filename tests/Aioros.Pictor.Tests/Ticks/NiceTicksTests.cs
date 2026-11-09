using System.Text.Json;
using Aioros.Pictor.Ticks;

namespace Aioros.Pictor.Tests.Ticks;

public sealed class NiceTicksTests
{
    [Fact]
    [Trait("AC", "TICKS-01")]
    public async Task NiceTicksMatchRechartsScaleAsync()
    {
        var cases = await Golden.CasesAsync("ticks.json");

        var wrong = cases
            .Where(item => item.GetProperty("nice").ValueKind == JsonValueKind.Array)
            .Select(item => (Item: item, Actual: NiceTicks.Compute(item.GetProperty("min").GetDouble(), item.GetProperty("max").GetDouble(), item.GetProperty("count").GetInt32(), item.GetProperty("allowDecimals").GetBoolean())))
            .Where(pair => !pair.Actual.SequenceEqual(pair.Item.GetProperty("nice").EnumerateArray().Select(value => value.GetDouble())))
            .Select(pair => $"{pair.Item.GetProperty("min")}..{pair.Item.GetProperty("max")} x{pair.Item.GetProperty("count")}: [{string.Join(", ", pair.Actual)}]");

        Assert.Empty(wrong);
    }

    [Fact]
    [Trait("AC", "TICKS-02")]
    public async Task FixedDomainTicksMatchRechartsScaleAsync()
    {
        var cases = await Golden.CasesAsync("ticks.json");

        var wrong = cases
            .Where(item => item.GetProperty("fixed").ValueKind == JsonValueKind.Array)
            .Select(item => (Item: item, Actual: NiceTicks.FixedDomain(item.GetProperty("min").GetDouble(), item.GetProperty("max").GetDouble(), item.GetProperty("count").GetInt32(), item.GetProperty("allowDecimals").GetBoolean())))
            .Where(pair => !pair.Actual.SequenceEqual(pair.Item.GetProperty("fixed").EnumerateArray().Select(value => value.GetDouble())))
            .Select(pair => $"{pair.Item.GetProperty("min")}..{pair.Item.GetProperty("max")} x{pair.Item.GetProperty("count")}: [{string.Join(", ", pair.Actual)}]");

        Assert.Empty(wrong);
    }

    [Fact]
    [Trait("AC", "TICKS-03")]
    public void ADomainRechartsCannotTickEndsWithNoTicks()
    {
        var ticks = NiceTicks.Compute(-1, 1, 2);

        Assert.Empty(ticks);
    }
}
