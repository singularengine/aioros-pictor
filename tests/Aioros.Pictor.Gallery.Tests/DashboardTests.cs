using System.Text.Json;
using Aioros.Functions;
using Aioros.Navigation;
using Gallery.Analytics;

namespace Aioros.Pictor.Gallery.Tests;

public sealed class DashboardTests
{
    private static readonly DateTimeOffset Moment = new(2026, 10, 14, 14, 23, 41, TimeSpan.Zero);

    [Fact]
    [Trait("AC", "DASHBOARD-04")]
    public void TheGeneratorIsDeterministicForASeed()
    {
        var first = new TrafficModel(2026);
        var again = new TrafficModel(2026);
        var other = new TrafficModel(2027);

        foreach (var product in Products.All)
        {
            Assert.Equal(Json(Dashboards.Overview(first, product, TimeRange.Week, Moment)), Json(Dashboards.Overview(again, product, TimeRange.Week, Moment)));
            Assert.Equal(Json(Dashboards.Kpis(first, product, TimeRange.Live, Moment)), Json(Dashboards.Kpis(again, product, TimeRange.Live, Moment)));
            Assert.Equal(Json(Dashboards.Traffic(first, product, TimeRange.Live, Moment)), Json(Dashboards.Traffic(again, product, TimeRange.Live, Moment)));
            Assert.Equal(Json(Dashboards.Feed(first, product, Moment)), Json(Dashboards.Feed(again, product, Moment)));
            Assert.NotEqual(Json(Dashboards.Traffic(first, product, TimeRange.Day, Moment)), Json(Dashboards.Traffic(other, product, TimeRange.Day, Moment)));
        }

        var next = Moment.AddSeconds(1);
        Assert.Equal(Json(Dashboards.Traffic(first, Products.Web, TimeRange.Day, Moment)), Json(Dashboards.Traffic(first, Products.Web, TimeRange.Day, next) with { Updated = Dashboards.Traffic(first, Products.Web, TimeRange.Day, Moment).Updated }));
        var live = Dashboards.Traffic(first, Products.Web, TimeRange.Live, Moment).Points;
        var later = Dashboards.Traffic(first, Products.Web, TimeRange.Live, next).Points;
        Assert.Equal(live.Take(live.Count - 1), later.Take(later.Count - 1));
        Assert.True(Enumerable.Range(0, 6).Select(second => Dashboards.Traffic(first, Products.Web, TimeRange.Live, Moment.AddSeconds(second)).Points[^1]).Distinct().Count() > 1);
    }

    [Fact]
    [Trait("AC", "DASHBOARD-05")]
    public void TheQuerysProductAndRangeChooseTheData()
    {
        var model = new TrafficModel(2026);
        var data = Dashboards.Overview(model, Products.Find("api"), TimeRange.Find("7d"), Moment);
        var kpis = Dashboards.Kpis(model, Products.Find("api"), TimeRange.Find("7d"), Moment);
        var traffic = Dashboards.Traffic(model, Products.Find("api"), TimeRange.Find("7d"), Moment);

        Assert.Equal(("Relay", "Api", "7d"), (data.Name, data.Kind, data.Range));
        Assert.Equal(["Requests per second", "Requests", "Error rate", "p95 latency"], [kpis.Now.Label, kpis.Volume.Label, kpis.Rate.Label, kpis.Value.Label]);
        Assert.Equal(("Requests", 56), (traffic.PrimaryName, traffic.Points.Count));
        Assert.Matches(@"^(Mon|Tue|Wed|Thu|Fri|Sat|Sun) \d\dh$", traffic.Points[0].Label);
        Assert.Equal([false, false, true], data.Apps.Select(choice => choice.Selected));
        Assert.Equal(["/dashboard?app=web&range=7d", "/dashboard?app=mobile&range=7d", "/dashboard?app=api&range=7d"], data.Apps.Select(choice => choice.Href));
        Assert.Equal([false, false, true, false], data.Ranges.Select(choice => choice.Selected));
        Assert.Equal(["/dashboard?app=api&range=live", "/dashboard?app=api&range=24h", "/dashboard?app=api&range=7d", "/dashboard?app=api&range=30d"], data.Ranges.Select(choice => choice.Href));
        Assert.Same(Products.Web, Products.Find("nope"));
        Assert.Same(Products.Web, Products.Find(null));
        Assert.Same(TimeRange.Live, TimeRange.Find("1y"));
        Assert.Same(TimeRange.Live, TimeRange.Find(null));
    }

    [Fact]
    [Trait("AC", "DASHBOARD-06")]
    public void ClickingATopPageFocusesTheMainChartOnIt()
    {
        var model = new TrafficModel(2026);
        var navigator = new Recorder();
        var page = new DashboardModel(Dashboards.Overview(model, Products.Web, TimeRange.Day, Moment), navigator);
        var focused = new DashboardModel(Dashboards.Overview(model, Products.Web, TimeRange.Day, Moment, focus: 1), navigator);

        page.FocusPage.Execute("1");
        focused.FocusPage.Execute("1");
        page.FocusPage.Execute("9");
        page.FocusPage.Execute("-1");

        Assert.Equal(["/dashboard?app=web&range=24h&focus=1", "/dashboard?app=web&range=24h"], navigator.Paths);
        Assert.Equal(("/pricing", 1, true), (focused.Data.Focus, focused.Data.FocusIndex, focused.Focused));
        Assert.Equal([null, 1, null, null, null, null], focused.Data.Pages.Select((bar, i) => bar.Value is null ? (int?)null : i));
        var whole = Dashboards.Traffic(model, Products.Web, TimeRange.Day, Moment);
        var pricing = Dashboards.Traffic(model, Products.Web, TimeRange.Day, Moment, focus: 1);
        Assert.Equal("Visitors · /pricing", pricing.Title);
        Assert.All(whole.Points.Zip(pricing.Points), pair => Assert.True(pair.Second.Primary < pair.First.Primary));
    }

    [Fact]
    [Trait("AC", "DASHBOARD-08")]
    public void EachTickReplacesTheMomentAndInvalidatesTheLiveKey()
    {
        var feed = new AnalyticsFeed(new TrafficModel(2026), new Clock(Moment));
        var keys = new Keys();
        var before = feed.Current;

        AnalyticsTicker.Tick(feed, keys, Moment.AddSeconds(1.4));
        var after = feed.Current;
        AnalyticsTicker.Tick(feed, keys, Moment.AddSeconds(19.2));

        Assert.Equal(Moment, before.Now);
        Assert.Equal(Moment.AddSeconds(1), after.Now);
        Assert.NotSame(before, after);
        Assert.NotSame(before.Products["web"], after.Products["web"]);
        Assert.Equal([AnalyticsTicker.LiveKey, AnalyticsTicker.LiveKey, AnalyticsTicker.HistoryKey], keys.Invalidated);
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);

    private sealed class Recorder : INavigator
    {
        public List<string> Paths { get; } = [];

        public ValueTask NavigateAsync(NavigationRequest request, CancellationToken cancellationToken)
        {
            Paths.Add(request.Path);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class Keys : IDataKeys
    {
        public List<string> Invalidated { get; } = [];

        public void Read(string key)
        {
        }

        public void Invalidate(string key) => Invalidated.Add(key);
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
