using System.Diagnostics;

namespace Aioros.Pictor.Demo;

/// <summary>
/// The in-process timing protocol of Aioros.Graphs' demo: a 300 ms warm-up, iterations doubled until a sample takes
/// 10 ms, then the median of 11 samples.
/// </summary>
internal static class Timing
{
    private const int Samples = 11;

    public static (double MedianNs, long Iterations) Measure(Action run)
    {
        var warm = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(warm).TotalMilliseconds < 300)
        {
            run();
        }

        long iterations = 1;
        while (Elapsed(run, iterations).TotalMilliseconds < 10)
        {
            iterations *= 2;
        }

        var samples = new double[Samples];
        for (var k = 0; k < Samples; k++)
        {
            samples[k] = Elapsed(run, iterations).TotalNanoseconds / iterations;
        }

        Array.Sort(samples);
        return (samples[Samples / 2], iterations);
    }

    private static TimeSpan Elapsed(Action run, long iterations)
    {
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < iterations; i++)
        {
            run();
        }

        return Stopwatch.GetElapsedTime(start);
    }
}
