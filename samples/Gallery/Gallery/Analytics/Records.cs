namespace Gallery.Analytics;

/// <summary>One point of a sparkline.</summary>
public sealed record Spark(string Label, double Value);

/// <summary>
/// A KPI tile: its label, its formatted value, its change against the previous period written with an arrow and a sign,
/// whether that change is good, bad or flat (an API's latency going up is bad), and its trend over the range.
/// </summary>
public sealed record Kpi(string Label, string Value, string Delta, bool Good, bool Bad, string Caption, IReadOnlyList<Spark> Trend)
{
    public bool Flat => !Good && !Bad;
}

/// <summary>The four KPI tiles of a product over a range.</summary>
public sealed record KpiData(Kpi Now, Kpi Volume, Kpi Rate, Kpi Value);

/// <summary>One bucket of the main chart.</summary>
public sealed record TrafficPoint(string Label, double Primary, double Secondary);

/// <summary>The main chart: its series over the range's buckets, for the whole product or the page it is focused on.</summary>
/// <summary>The main chart's state in the address: its data, the product, range and focus it was loaded for, and the brush window in the query, if any.</summary>
public sealed record TrafficState(TrafficData Data, string App, string Range, int Focus, int? From, int? To);

public sealed record TrafficData(string Title, string Caption, string PrimaryName, string SecondaryName, string Total, bool Live, string Updated, IReadOnlyList<TrafficPoint> Points)
{
    /// <summary>The value axis' tick format, for the size of the values.</summary>
    public string AxisFormat { get; init; } = "{0:#,0}";

    public bool History => !Live;
}

/// <summary>An event of the live feed.</summary>
public sealed record FeedEvent(string Time, string Title, string Place, string Amount, string Tone)
{
    public bool Signup => Tone == "signup";

    public bool Purchase => Tone == "purchase";

    public bool Error => Tone == "error";
}

/// <summary>The live feed, newest first.</summary>
public sealed record FeedData(IReadOnlyList<FeedEvent> Events, string Rate);

/// <summary>A choice of the app or range selector: a link, marked when it is the current one.</summary>
public sealed record Choice(string Id, string Label, string Href, bool Selected);

/// <summary>A bar of the top pages: its count when it is shown in full, or when the chart is focused on another page.</summary>
public sealed record PageBar(string Name, double? Value, double? Dimmed, string Text);

/// <summary>A slice of the traffic sources.</summary>
public sealed record Slice(string Name, double Value, string Share);

/// <summary>A group of buckets split by device.</summary>
public sealed record DeviceRow(string Label, double First, double Second, double Third);

/// <summary>A step of the funnel.</summary>
public sealed record Step(string Name, double Value, string Text)
{
    /// <summary>The step's count, compact.</summary>
    public string Count => Numbers.Compact(Value);
}

/// <summary>A cell of the busiest hours: a two-hour slot and the traffic it usually gets, in percent of the week's busiest slot.</summary>
public sealed record Slot(double Hour, double Row, double Value);

/// <summary>A weekday of the busiest hours.</summary>
public sealed record DayRow(string Day, IReadOnlyList<Slot> Slots);

/// <summary>A bucket of the two synced charts.</summary>
public sealed record SyncPoint(string Label, double Users, double Sessions);

/// <summary>Everything the dashboard shows outside its live views, for one product and range.</summary>
public sealed record DashboardData(string App, string Name, string Pitch, string Kind, string Range, string RangeCaption)
{
    public IReadOnlyList<Choice> Apps { get; init; } = [];

    public IReadOnlyList<Choice> Ranges { get; init; } = [];

    public string PagesTitle { get; init; } = string.Empty;

    public IReadOnlyList<PageBar> Pages { get; init; } = [];

    /// <summary>The page the main chart is focused on, or empty.</summary>
    public string Focus { get; init; } = string.Empty;

    /// <summary>The index of the focused page, or -1.</summary>
    public int FocusIndex { get; init; } = -1;

    /// <summary>The dashboard's address without the focus.</summary>
    public string ClearHref { get; init; } = "/dashboard";

    public string SourcesTitle { get; init; } = string.Empty;

    public IReadOnlyList<Slice> Sources { get; init; } = [];

    public string SourcesTotal { get; init; } = string.Empty;

    public string DevicesTitle { get; init; } = string.Empty;

    public IReadOnlyList<string> DeviceNames { get; init; } = [];

    public IReadOnlyList<DeviceRow> Devices { get; init; } = [];

    public string DevicesFormat { get; init; } = "{0:#,0}";

    public string FunnelTitle { get; init; } = string.Empty;

    public IReadOnlyList<Step> Funnel { get; init; } = [];

    public string FunnelConversion { get; init; } = string.Empty;

    public IReadOnlyList<DayRow> Week { get; init; } = [];

    public string SyncFirst { get; init; } = string.Empty;

    public string SyncSecond { get; init; } = string.Empty;

    public IReadOnlyList<SyncPoint> Sync { get; init; } = [];

    public string SyncFirstFormat { get; init; } = "{0:#,0}";

    public string SyncSecondFormat { get; init; } = "{0:#,0}";
}
