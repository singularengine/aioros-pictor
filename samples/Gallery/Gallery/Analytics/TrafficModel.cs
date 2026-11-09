namespace Gallery.Analytics;

/// <summary>
/// The seeded generator behind every number of the dashboard: a pure function of the seed, the product and the time, so
/// the same seed gives the same dashboard at the same moment, any range is computed without keeping history, and a
/// bucket keeps its value from one tick to the next. Traffic follows the product's daily bumps and weekend factor, a
/// slow trend over weeks, and value noise (smoothly interpolated hashes) at a few time scales.
/// </summary>
public sealed class TrafficModel(int seed)
{
    public int Seed { get; } = seed;

    /// <summary>The share of the product's peak active at <paramref name="time"/>, before noise.</summary>
    public static double Shape(Product product, DateTimeOffset time)
    {
        var hour = time.TimeOfDay.TotalHours;
        var day = product.Floor;
        foreach (var (center, width, height) in product.Bumps)
        {
            var distance = Math.Min(Math.Abs(hour - center), 24 - Math.Abs(hour - center));
            day += height * Math.Exp(-(distance * distance) / (2 * width * width));
        }

        var weekend = time.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? product.Weekend : 1;
        var days = time.ToUnixTimeSeconds() / 86400.0;
        var trend = 1 + (0.09 * Math.Sin(2 * Math.PI * days / 41)) + (0.05 * Math.Sin(2 * Math.PI * days / 13));
        return day * weekend * trend;
    }

    /// <summary>Users active at <paramref name="time"/> (requests per second for an API), with noise at three scales.</summary>
    public double Active(Product product, DateTimeOffset time)
    {
        var minutes = time.ToUnixTimeMilliseconds() / 60000.0;
        var noise = 1 + (0.07 * Smooth(product, 1, minutes / 45)) + (0.04 * Smooth(product, 2, minutes / 6)) + (0.025 * Smooth(product, 3, minutes * 3));
        return Math.Max(0, product.Peak * Shape(product, time) * noise);
    }

    /// <summary>
    /// The product's primary count over a bucket (visitors, active users, requests): the active level at the bucket's
    /// middle times its length, with a noise of its own so neighboring buckets differ.
    /// </summary>
    public double Bucket(Product product, DateTimeOffset start, TimeSpan length)
    {
        var middle = start + (length / 2);
        var minutes = length.TotalMinutes;
        var perMinute = product.Kind == ProductKind.Api ? Active(product, middle) * 60 : Active(product, middle) / 4.5;
        return perMinute * minutes * (1 + (0.05 * Unit(product, 4, start.ToUnixTimeSeconds())));
    }

    /// <summary>The secondary count over the same bucket (page views, sessions, cache hits).</summary>
    public double Secondary(Product product, DateTimeOffset start, double primary) =>
        primary * product.SecondaryRatio * (1 + (0.06 * Unit(product, 5, start.ToUnixTimeSeconds())));

    /// <summary>A share's count over a bucket: its weight of the total, with a noise of its own.</summary>
    public double Part(Product product, Share share, DateTimeOffset start, double total) =>
        total * share.Weight * (1 + (0.12 * Unit(product, 6 + Stable(share.Name), start.ToUnixTimeSeconds())));

    /// <summary>A rate that wanders around <paramref name="mean"/> by up to <paramref name="spread"/> of it over hours.</summary>
    public double Rate(Product product, int stream, DateTimeOffset time, double mean, double spread) =>
        mean * (1 + (spread * Smooth(product, 20 + stream, time.ToUnixTimeSeconds() / 5400.0)));

    /// <summary>A number in [-1, 1] that depends only on the seed, the product, the stream and the index.</summary>
    public double Unit(Product product, int stream, long index) => (Hash(product, stream, index) >> 11) * (2.0 / (1UL << 53)) - 1;

    /// <summary>A number in [0, 1).</summary>
    public double Fraction(Product product, int stream, long index) => (Hash(product, stream, index) >> 11) * (1.0 / (1UL << 53));

    /// <summary>Value noise: hashes at whole positions, smoothly interpolated between them.</summary>
    public double Smooth(Product product, int stream, double position)
    {
        var floor = Math.Floor(position);
        var t = position - floor;
        var eased = t * t * (3 - (2 * t));
        var index = (long)floor;
        return Unit(product, stream, index) + ((Unit(product, stream, index + 1) - Unit(product, stream, index)) * eased);
    }

    private ulong Hash(Product product, int stream, long index)
    {
        var x = (ulong)(uint)Seed;
        x = Mix(x ^ ((ulong)Stable(product.Id) << 32));
        x = Mix(x ^ (ulong)(uint)stream);
        return Mix(x ^ (ulong)index);
    }

    private static ulong Mix(ulong z)
    {
        z += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    private static int Stable(string text)
    {
        var hash = 2166136261u;
        foreach (var c in text)
        {
            hash = (hash ^ c) * 16777619u;
        }

        return (int)(hash & 0x7FFFFFFF) % 1000;
    }
}
