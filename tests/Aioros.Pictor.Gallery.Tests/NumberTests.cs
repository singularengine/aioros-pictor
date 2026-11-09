using System.Globalization;
using Gallery.Analytics;

namespace Aioros.Pictor.Gallery.Tests;

public sealed class NumberTests
{
    [Fact]
    [Trait("AC", "DASHBOARD-02")]
    public void ValuesAreWrittenCompactAndFormatted()
    {
        Assert.Equal("842", Numbers.Compact(842.4));
        Assert.Equal("12.4k", Numbers.Compact(12_420));
        Assert.Equal("1.24M", Numbers.Compact(1_243_000));
        Assert.Equal("1M", Numbers.Compact(999_999));
        Assert.Equal("1.27B", Numbers.Compact(1_266_260_214));
        Assert.Equal("$3,210", Numbers.Money(3_210.2));
        Assert.Equal("$124k", Numbers.Money(124_300));
        Assert.Equal("2.3%", Numbers.Percent(2.34));
        Assert.Equal("0.42%", Numbers.Percent(0.4211));
        Assert.Equal("184 ms", Numbers.Milliseconds(183.6));
        Assert.Equal("30M", Axis(45_000_000, 30_000_000));
        Assert.Equal("1.5k", Axis(2_000, 1_500));
        Assert.Equal("0", Axis(2_000, 0));
        Assert.Equal("1,500", Axis(1_999, 1_500));
        Assert.Equal("0", Axis(1_999, -0.0));
    }

    [Fact]
    [Trait("AC", "DASHBOARD-03")]
    public void DeltasCarryAnArrowAndASignAndTilesKnowWhichWayIsGood()
    {
        Assert.Equal("▲ +12.4%", Numbers.Delta(0.1238));
        Assert.Equal("▼ −3.1%", Numbers.Delta(-0.031));
        Assert.Equal("● 0.0%", Numbers.Delta(0.0004));

        var model = new TrafficModel(7);
        var morning = new DateTimeOffset(2026, 10, 14, 9, 0, 0, TimeSpan.Zero);
        var evening = new DateTimeOffset(2026, 10, 14, 23, 0, 0, TimeSpan.Zero);
        var rising = Dashboards.Kpis(model, Products.Web, TimeRange.Live, morning);
        var falling = Dashboards.Kpis(model, Products.Web, TimeRange.Live, evening);
        var api = Dashboards.Kpis(model, Products.Api, TimeRange.Day, evening);

        Assert.StartsWith("▲", rising.Now.Delta, StringComparison.Ordinal);
        Assert.True(rising.Now.Good);
        Assert.False(rising.Now.Bad);
        Assert.StartsWith("▼", falling.Now.Delta, StringComparison.Ordinal);
        Assert.True(falling.Now.Bad);
        Assert.Equal("p95 latency", api.Value.Label);
        Assert.Equal(api.Value.Delta.StartsWith('▲'), api.Value.Bad);
        Assert.Equal(api.Value.Delta.StartsWith('▼'), api.Value.Good);
        Assert.Equal("Error rate", api.Rate.Label);
        Assert.Equal(api.Rate.Delta.StartsWith('▲'), api.Rate.Bad);
        Assert.All([rising.Now, falling.Now, api.Value, api.Rate], tile => Assert.Equal(tile.Delta.StartsWith('●'), tile.Flat));
    }

    private static string Axis(double max, double value) => string.Format(CultureInfo.InvariantCulture, Numbers.AxisFormat(max), value);
}
