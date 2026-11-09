using System.Globalization;

namespace PictorStarter.Analytics;

/// <summary>A bucket's start with its two counts.</summary>
public readonly record struct Bucket(DateTimeOffset Start, double Primary, double Secondary);

/// <summary>
/// Turns the generator's numbers into what the dashboard shows for a product, a range and a moment: the main chart's
/// buckets, the KPI tiles with their deltas against the previous period, the live feed and the rest of the page. Pure:
/// the same model, product, range and moment always give the same data.
/// </summary>
public static class Dashboards
{
    /// <summary>The range's buckets ending with the one that holds <paramref name="now"/>, for the product or one of its pages.</summary>
    public static Bucket[] Buckets(TrafficModel model, Product product, TimeRange range, DateTimeOffset now, int focus = -1, int periodsBack = 0, TimeSpan shift = default)
    {
        var start = range.Start(now) - (range.Span * periodsBack) - shift;
        var page = focus >= 0 && focus < product.Pages.Count ? product.Pages[focus] : null;
        var buckets = new Bucket[range.Buckets];
        for (var i = 0; i < buckets.Length; i++)
        {
            var at = start + (range.Bucket * i);
            var primary = model.Bucket(product, at, range.Bucket);
            if (range.IsLive && periodsBack == 0 && shift == TimeSpan.Zero && i == buckets.Length - 1)
            {
                primary = Live(model, product, range, now);
            }

            if (page is not null)
            {
                primary = model.Part(product, page, at, primary);
            }

            buckets[i] = new Bucket(at, primary, model.Secondary(product, at, primary));
        }

        return buckets;
    }

    /// <summary>The main chart's data: the product's (or the focused page's) primary count, and the same buckets one period earlier to compare with.</summary>
    public static TrafficData Traffic(TrafficModel model, Product product, TimeRange range, DateTimeOffset now, int focus = -1)
    {
        var buckets = Buckets(model, product, range, now, focus);
        var before = Buckets(model, product, range, now, focus, shift: Comparison(range));
        var page = focus >= 0 && focus < product.Pages.Count ? product.Pages[focus].Name : null;
        var earlier = Earlier(range);
        var total = buckets.Sum(bucket => bucket.Primary);
        return new TrafficData(
            page is null ? product.PrimaryName + ", compared with " + earlier.ToLowerInvariant() : product.PrimaryName + " · " + page,
            range.IsLive ? "Per 2 minutes, " + range.Caption + ", the last bucket live" : "Per " + Per(range) + ", " + range.Caption,
            product.PrimaryName,
            earlier,
            Numbers.Compact(total) + " " + product.PrimaryName.ToLowerInvariant(),
            range.IsLive,
            "Updated " + now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            [.. buckets.Select((bucket, i) => new TrafficPoint(range.Name(bucket.Start), Math.Round(bucket.Primary), Math.Round(before[i].Primary)))])
        {
            AxisFormat = Numbers.AxisFormat(Math.Max(buckets.Max(bucket => bucket.Primary), before.Max(bucket => bucket.Primary))),
        };
    }

    /// <summary>How far back the main chart's comparison lies: a day for the live window and a day's range, the range otherwise.</summary>
    public static TimeSpan Comparison(TimeRange range) => range.IsLive ? TimeSpan.FromDays(1) : range.Span;

    private static string Earlier(TimeRange range) => range.Id switch
    {
        "live" or "24h" => "Yesterday",
        "7d" => "Previous week",
        _ => "Previous 30 days",
    };

    /// <summary>The four KPI tiles, each with its delta against the previous period and its trend.</summary>
    public static KpiData Kpis(TrafficModel model, Product product, TimeRange range, DateTimeOffset now)
    {
        var current = Buckets(model, product, range, now);
        var previous = Buckets(model, product, range, now, periodsBack: 1);
        var caption = "vs previous " + Previous(range);
        var nowValue = model.Active(product, now);
        var before = model.Active(product, now - TimeSpan.FromMinutes(30));
        var live = Buckets(model, product, TimeRange.Live, now);
        var nowTile = Tile(
            product.Kind == ProductKind.Api ? "Requests per second" : "Active users now",
            Numbers.Compact(nowValue),
            Numbers.Change(nowValue, before),
            true,
            "vs 30 minutes ago",
            Trend(live, bucket => bucket.Primary));
        var volume = Tile(
            product.Kind == ProductKind.Api ? "Requests" : product.SecondaryName,
            Numbers.Compact(Total(current, product)),
            Numbers.Change(Total(current, product), Total(previous, product)),
            true,
            caption,
            Trend(current, bucket => product.Kind == ProductKind.Api ? bucket.Primary : bucket.Secondary));
        return new KpiData(nowTile, volume, RateTile(model, product, current, previous, caption), ValueTile(model, product, current, previous, caption));
    }

    /// <summary>The live feed: the events of the last minute, newest first, at most <paramref name="count"/>.</summary>
    public static FeedData Feed(TrafficModel model, Product product, DateTimeOffset now, int count = 7)
    {
        var events = new List<FeedEvent>(count);
        var second = now.ToUnixTimeSeconds();
        var total = 0;
        for (var s = second; s > second - 60 && events.Count < count; s--)
        {
            var at = DateTimeOffset.FromUnixTimeSeconds(s).ToOffset(now.Offset);
            var happened = Happened(model, product, at);
            total += happened;
            for (var j = 0; j < happened && events.Count < count; j++)
            {
                events.Add(Event(model, product, at, j));
            }
        }

        var rate = product.EventRate * TrafficModel.Shape(product, now) * 60;
        return new FeedData(events, "≈ " + Numbers.Compact(rate) + " events per minute");
    }

    /// <summary>Everything outside the live views, for one product and range.</summary>
    public static DashboardData Overview(TrafficModel model, Product product, TimeRange range, DateTimeOffset now, int focus = -1)
    {
        var buckets = Buckets(model, product, range, now);
        var total = buckets.Sum(bucket => bucket.Primary);
        var at = range.Start(now);
        var focused = focus >= 0 && focus < product.Pages.Count ? focus : -1;
        var sources = product.Sources.Select(share => model.Part(product, share, at, total)).ToArray();
        var sourceTotal = sources.Sum();
        var devices = Devices(model, product, range, buckets);
        var sync = Sync(model, product, range.IsLive ? TimeRange.Day : range, now);
        return new DashboardData(product.Id, product.Name, product.Pitch, product.Kind.ToString(), range.Id, range.Caption)
        {
            Apps = [.. Products.All.Select(other => new Choice(other.Id, other.Name, Links.Dashboard(other.Id, range.Id), other == product))],
            Ranges = [.. TimeRange.All.Select(other => new Choice(other.Id, other.Label, Links.Dashboard(product.Id, other.Id), other == range))],
            PagesTitle = product.PagesTitle,
            Pages = [.. product.Pages.Select((share, i) => Bar(model, product, share, at, total, focused < 0 || focused == i))],
            Focus = focused >= 0 ? product.Pages[focused].Name : string.Empty,
            FocusIndex = focused,
            ClearHref = Links.Dashboard(product.Id, range.Id),
            SourcesTitle = product.SourcesTitle,
            Sources = [.. product.Sources.Select((share, i) => new Slice(share.Name, Math.Round(sources[i]), Numbers.Percent(sources[i] / sourceTotal * 100)))],
            SourcesTotal = Numbers.Compact(sourceTotal),
            DevicesTitle = product.DevicesTitle,
            DeviceNames = [.. product.Devices.Select(device => device.Name)],
            Devices = devices,
            DevicesFormat = Numbers.AxisFormat(devices.Max(row => row.First + row.Second + row.Third)),
            FunnelTitle = product.FunnelTitle,
            Funnel = Funnel(model, product, at, total),
            FunnelConversion = Numbers.Percent(product.Conversion),
            Week = Week(model, product, now),
            SyncFirst = product.PrimaryName,
            SyncSecond = product.Kind == ProductKind.Api ? "p95 latency (ms)" : product.SecondaryName,
            Sync = sync,
            SyncFirstFormat = Numbers.AxisFormat(sync.Max(point => point.Users)),
            SyncSecondFormat = Numbers.AxisFormat(sync.Max(point => point.Sessions)),
        };
    }

    private static double Live(TrafficModel model, Product product, TimeRange range, DateTimeOffset now) =>
        model.Bucket(product, now - range.Bucket, range.Bucket) * (model.Active(product, now) / Math.Max(1, model.Active(product, now - (range.Bucket / 2))));

    private static double Total(Bucket[] buckets, Product product) =>
        buckets.Sum(bucket => product.Kind == ProductKind.Api ? bucket.Primary : bucket.Secondary);

    private static Kpi RateTile(TrafficModel model, Product product, Bucket[] current, Bucket[] previous, string caption)
    {
        if (product.Kind == ProductKind.Api)
        {
            var errors = Average(current, bucket => ErrorRate(model, product, bucket.Start));
            var before = Average(previous, bucket => ErrorRate(model, product, bucket.Start));
            return Tile("Error rate", Numbers.Percent(errors), Numbers.Change(errors, before), false, caption, Trend(current, bucket => ErrorRate(model, product, bucket.Start)));
        }

        var rate = Average(current, bucket => Conversion(model, product, bucket.Start));
        var earlier = Average(previous, bucket => Conversion(model, product, bucket.Start));
        return Tile("Conversion rate", Numbers.Percent(rate), Numbers.Change(rate, earlier), true, caption, Trend(current, bucket => Conversion(model, product, bucket.Start)));
    }

    private static Kpi ValueTile(TrafficModel model, Product product, Bucket[] current, Bucket[] previous, string caption)
    {
        if (product.Kind == ProductKind.Api)
        {
            var latency = Average(current, bucket => Latency(model, product, bucket.Start));
            var before = Average(previous, bucket => Latency(model, product, bucket.Start));
            return Tile("p95 latency", Numbers.Milliseconds(latency), Numbers.Change(latency, before), false, caption, Trend(current, bucket => Latency(model, product, bucket.Start)));
        }

        var revenue = current.Sum(bucket => Revenue(model, product, bucket));
        var earlier = previous.Sum(bucket => Revenue(model, product, bucket));
        return Tile("Revenue", Numbers.Money(revenue), Numbers.Change(revenue, earlier), true, caption, Trend(current, bucket => Revenue(model, product, bucket)));
    }

    private static Kpi Tile(string label, string value, double change, bool upIsGood, string caption, IReadOnlyList<Spark> trend)
    {
        var rounded = Math.Round(change * 1000);
        var good = rounded != 0 && (rounded > 0) == upIsGood;
        var bad = rounded != 0 && !good;
        return new Kpi(label, value, Numbers.Delta(change), good, bad, caption, trend);
    }

    private static Spark[] Trend(Bucket[] buckets, Func<Bucket, double> value)
    {
        var step = Math.Max(1, buckets.Length / 24);
        var points = new List<Spark>();
        for (var i = buckets.Length - 1; i >= 0; i -= step)
        {
            points.Add(new Spark(i.ToString(CultureInfo.InvariantCulture), Math.Round(value(buckets[i]), 3)));
        }

        points.Reverse();
        return [.. points];
    }

    private static double Average(Bucket[] buckets, Func<Bucket, double> value) => buckets.Average(value);

    /// <summary>The share of visitors who convert, in percent.</summary>
    public static double Conversion(TrafficModel model, Product product, DateTimeOffset at) => model.Rate(product, 1, at, product.Conversion, 0.16);

    /// <summary>The share of an API's requests that fail, in percent.</summary>
    public static double ErrorRate(TrafficModel model, Product product, DateTimeOffset at) => model.Rate(product, 2, at, 0.24, 0.45);

    /// <summary>An API's 95th percentile latency in milliseconds, rising with load.</summary>
    public static double Latency(TrafficModel model, Product product, DateTimeOffset at) =>
        model.Rate(product, 3, at, 92, 0.12) + (64 * TrafficModel.Shape(product, at));

    private static double Revenue(TrafficModel model, Product product, Bucket bucket)
    {
        var buyers = bucket.Primary * Conversion(model, product, bucket.Start) / 100;
        return buyers * (product.Kind == ProductKind.Mobile ? 21 : 46);
    }

    private static string Per(TimeRange range) => range.Id switch
    {
        "24h" => "30 minutes",
        "7d" => "3 hours",
        _ => "day",
    };

    private static string Previous(TimeRange range) => range.Id switch
    {
        "live" => "30 minutes",
        "24h" => "24 hours",
        "7d" => "7 days",
        _ => "30 days",
    };

    private static PageBar Bar(TrafficModel model, Product product, Share share, DateTimeOffset at, double total, bool shown)
    {
        var value = Math.Round(model.Part(product, share, at, total * product.SecondaryRatio));
        return new PageBar(share.Name, shown ? value : null, shown ? null : value, Numbers.Compact(value));
    }

    private static Step[] Funnel(TrafficModel model, Product product, DateTimeOffset at, double total) =>
        [.. product.Funnel.Select((step, i) =>
        {
            var noise = i == 0 ? 1 : 1 + (0.04 * model.Unit(product, 40 + i, at.ToUnixTimeSeconds()));
            return new Step(step.Name, Math.Round(total * step.Weight * noise), Numbers.Percent(step.Weight * 100));
        })];

    private static DeviceRow[] Devices(TrafficModel model, Product product, TimeRange range, Bucket[] buckets)
    {
        var size = Math.Max(1, buckets.Length / 10);
        var rows = new List<DeviceRow>();
        for (var i = buckets.Length % size; i + size <= buckets.Length; i += size)
        {
            var group = buckets.AsSpan(i, size);
            var sum = 0.0;
            foreach (var bucket in group)
            {
                sum += bucket.Primary;
            }

            rows.Add(new DeviceRow(
                range.Name(group[0].Start),
                Math.Round(model.Part(product, product.Devices[0], group[0].Start, sum)),
                Math.Round(model.Part(product, product.Devices[1], group[0].Start, sum)),
                Math.Round(model.Part(product, product.Devices[2], group[0].Start, sum))));
        }

        return [.. rows];
    }

    private static DayRow[] Week(TrafficModel model, Product product, DateTimeOffset now)
    {
        var monday = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
        var values = new double[7, 12];
        var peak = 0.0;
        for (var day = 0; day < 7; day++)
        {
            for (var slot = 0; slot < 12; slot++)
            {
                var at = new DateTimeOffset(monday.AddDays(day).AddHours((slot * 2) + 1), now.Offset);
                values[day, slot] = TrafficModel.Shape(product, at) * (1 + (0.08 * model.Unit(product, 50, (day * 12) + slot)));
                peak = Math.Max(peak, values[day, slot]);
            }
        }

        var rows = new DayRow[7];
        for (var day = 0; day < 7; day++)
        {
            var slots = new Slot[12];
            for (var slot = 0; slot < 12; slot++)
            {
                slots[slot] = new Slot(slot * 2, 1, Math.Round(values[day, slot] / peak * 100));
            }

            rows[day] = new DayRow(monday.AddDays(day).ToString("ddd", CultureInfo.InvariantCulture), slots);
        }

        return rows;
    }

    private static SyncPoint[] Sync(TrafficModel model, Product product, TimeRange range, DateTimeOffset now) =>
        [.. Buckets(model, product, range, now).Select(bucket => new SyncPoint(
            range.Name(bucket.Start),
            Math.Round(bucket.Primary),
            product.Kind == ProductKind.Api ? Math.Round(Latency(model, product, bucket.Start)) : Math.Round(bucket.Secondary)))];

    private static int Happened(TrafficModel model, Product product, DateTimeOffset at)
    {
        var expected = product.EventRate * TrafficModel.Shape(product, at);
        var whole = Math.Floor(expected);
        return (int)whole + (model.Fraction(product, 60, at.ToUnixTimeSeconds()) < expected - whole ? 1 : 0);
    }

    private static FeedEvent Event(TrafficModel model, Product product, DateTimeOffset at, int index)
    {
        var key = (at.ToUnixTimeSeconds() * 4) + index;
        var pick = model.Fraction(product, 61, key) * product.Events.Sum(kind => kind.Weight);
        var kind = product.Events[^1];
        foreach (var candidate in product.Events)
        {
            pick -= candidate.Weight;
            if (pick < 0)
            {
                kind = candidate;
                break;
            }
        }

        var place = product.Places[(int)(model.Fraction(product, 62, key) * product.Places.Count)];
        var amount = kind.Amount <= 0 ? string.Empty
            : product.Kind == ProductKind.Api ? Numbers.Money(kind.Amount * (0.2 + (3 * model.Fraction(product, 63, key))))
            : "$" + kind.Amount.ToString(kind.Amount % 1 == 0 ? "0" : "0.00", CultureInfo.InvariantCulture);
        return new FeedEvent(at.ToString("HH:mm:ss", CultureInfo.InvariantCulture), kind.Title, place, amount, kind.Tone);
    }
}

/// <summary>The dashboard's addresses: the product, the range and the focused page live in the URL, so both targets share them.</summary>
public static class Links
{
    public static string Dashboard(string app, string range, int focus = -1) =>
        "/?app=" + app + "&range=" + range + (focus >= 0 ? "&focus=" + focus.ToString(CultureInfo.InvariantCulture) : string.Empty);

    /// <summary>The dashboard's address with the main chart's brush window in its query, so a live stream opened at it keeps the window.</summary>
    public static string Dashboard(string app, string range, int focus, int from, int to) =>
        Dashboard(app, range, focus) + string.Create(CultureInfo.InvariantCulture, $"&from={from}&to={to}");
}
