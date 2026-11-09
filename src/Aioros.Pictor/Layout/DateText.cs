using System.Globalization;

namespace Aioros.Pictor.Layout;

/// <summary>Dates of a time scale's ticks: the instant a value names, and the text JavaScript's <c>Date.toString</c> gives it.</summary>
internal static class DateText
{
    private static readonly string[] Days = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
    private static readonly string[] Months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    /// <summary>The instant of a value in milliseconds since the epoch, in a zone.</summary>
    public static DateTimeOffset Instant(double milliseconds, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds((long)Math.Round(milliseconds)), zone);

    /// <summary>JavaScript's <c>Date.prototype.toString</c> in a zone: <c>Mon Jan 01 2024 00:00:00 GMT+0000 (Coordinated Universal Time)</c>.</summary>
    public static string JavaScript(double milliseconds, TimeZoneInfo zone)
    {
        var instant = Instant(milliseconds, zone);
        var offset = instant.Offset;
        var sign = offset < TimeSpan.Zero ? '-' : '+';
        var name = zone == TimeZoneInfo.Utc ? "Coordinated Universal Time" : zone.IsDaylightSavingTime(instant) ? zone.DaylightName : zone.StandardName;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Days[(int)instant.DayOfWeek]} {Months[instant.Month - 1]} {instant.Day:00} {instant.Year:0000} {instant.Hour:00}:{instant.Minute:00}:{instant.Second:00} GMT{sign}{Math.Abs(offset.Hours):00}{Math.Abs(offset.Minutes):00} ({name})");
    }
}
