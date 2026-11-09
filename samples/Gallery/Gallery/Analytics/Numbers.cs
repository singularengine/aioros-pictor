using System.Globalization;

namespace Gallery.Analytics;

/// <summary>How the dashboard writes its numbers: compact counts, money, percentages, latencies and signed deltas.</summary>
public static class Numbers
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>842, 12.4k, 1.24M, 1.27B.</summary>
    public static string Compact(double value)
    {
        var magnitude = Math.Abs(value);
        return magnitude switch
        {
            >= 999_500_000 => (value / 1_000_000_000).ToString(magnitude >= 9_995_000_000 ? "0.#" : "0.##", Invariant) + "B",
            >= 999_500 => (value / 1_000_000).ToString(magnitude >= 99_950_000 ? "0" : magnitude >= 9_995_000 ? "0.#" : "0.##", Invariant) + "M",
            >= 999.5 => (value / 1_000).ToString(magnitude >= 99_950 ? "0" : "0.#", Invariant) + "k",
            _ => Math.Round(value).ToString("0", Invariant),
        };
    }

    /// <summary>$3,210, or compact from $100k up ($124k, $1.2M).</summary>
    public static string Money(double value) =>
        Math.Abs(value) >= 99_999.5 ? "$" + Compact(value) : "$" + Math.Round(value).ToString("#,0", Invariant);

    /// <summary>2.3%, with two decimals under 1% (0.42%).</summary>
    public static string Percent(double value) => value.ToString(Math.Abs(value) < 1 ? "0.00" : "0.0", Invariant) + "%";

    /// <summary>
    /// The tick format of an axis whose values reach <paramref name="max"/>: millions as 1.5M from 2,000,000 up, thousands as 1.5k from 2,000 up, whole numbers
    /// with separators below, so close ticks never round to the same text.
    /// </summary>
    public static string AxisFormat(double max) => max switch
    {
        >= 2_000_000 => "{0:0,,.#M;-0,,.#M;0}",
        >= 2_000 => "{0:0,.#k;-0,.#k;0}",
        _ => "{0:#,0;-#,0;0}",
    };

    /// <summary>184 ms.</summary>
    public static string Milliseconds(double value) => Math.Round(value).ToString("0", Invariant) + " ms";

    /// <summary>The change from <paramref name="previous"/> to <paramref name="current"/>, as a share of the previous.</summary>
    public static double Change(double current, double previous) => previous == 0 ? 0 : (current - previous) / previous;

    /// <summary>A delta written with its arrow and sign: ▲ +12.4%, ▼ −3.1%, or ● 0.0% when it rounds to nothing.</summary>
    public static string Delta(double change)
    {
        var percent = Math.Round(change * 100, 1);
        return percent switch
        {
            > 0 => "▲ +" + percent.ToString("0.0", Invariant) + "%",
            < 0 => "▼ −" + (-percent).ToString("0.0", Invariant) + "%",
            _ => "● 0.0%",
        };
    }
}
