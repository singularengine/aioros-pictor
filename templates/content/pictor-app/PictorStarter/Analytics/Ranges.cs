using System.Globalization;

namespace PictorStarter.Analytics;

/// <summary>
/// A time range of the dashboard: its id in the URL, its label, and the buckets it is drawn in (how many, how long),
/// the last one ending at the current bucket's end.
/// </summary>
public sealed record TimeRange(string Id, string Label, string Caption, int Buckets, TimeSpan Bucket)
{
    public static TimeRange Live { get; } = new("live", "Live", "last 30 minutes", 15, TimeSpan.FromMinutes(2));

    public static TimeRange Day { get; } = new("24h", "24 h", "last 24 hours", 48, TimeSpan.FromMinutes(30));

    public static TimeRange Week { get; } = new("7d", "7 d", "last 7 days", 56, TimeSpan.FromHours(3));

    public static TimeRange Month { get; } = new("30d", "30 d", "last 30 days", 30, TimeSpan.FromDays(1));

    public static IReadOnlyList<TimeRange> All { get; } = [Live, Day, Week, Month];

    public bool IsLive => ReferenceEquals(this, Live);

    /// <summary>The whole range.</summary>
    public TimeSpan Span => Bucket * Buckets;

    /// <summary>The range with this id, or <see cref="Live"/> for an unknown or missing id.</summary>
    public static TimeRange Find(string? id) => All.FirstOrDefault(range => string.Equals(range.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Live;

    /// <summary>The start of the bucket holding <paramref name="now"/>.</summary>
    public DateTimeOffset BucketStart(DateTimeOffset now)
    {
        var offset = now.Offset;
        var local = now.UtcDateTime + offset;
        var ticks = local.Ticks - (local.Ticks % Bucket.Ticks);
        return new DateTimeOffset(new DateTime(ticks, DateTimeKind.Unspecified), offset);
    }

    /// <summary>The start of the range's first bucket, so that its last bucket holds <paramref name="now"/>.</summary>
    public DateTimeOffset Start(DateTimeOffset now) => BucketStart(now) - (Bucket * (Buckets - 1));

    /// <summary>How a bucket starting at <paramref name="start"/> is labelled on the axis.</summary>
    public string Name(DateTimeOffset start) => Id switch
    {
        "live" => start.ToString("HH:mm", CultureInfo.InvariantCulture),
        "24h" => start.ToString("HH:mm", CultureInfo.InvariantCulture),
        "7d" => start.ToString("ddd HH'h'", CultureInfo.InvariantCulture),
        _ => start.ToString("MMM d", CultureInfo.InvariantCulture),
    };
}
