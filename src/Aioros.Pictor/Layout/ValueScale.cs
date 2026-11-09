using Aioros.Graphs.Scales;

namespace Aioros.Pictor.Layout;

/// <summary>
/// A numeric axis' d3 scale by Recharts' <c>scale</c> name: linear, log, pow (exponent 1), sqrt, time, utc, sequential
/// (linear over its range), or one of the discrete scales Recharts builds over the axis' two range ends: identity,
/// quantile, quantize and threshold.
/// </summary>
internal readonly struct ValueScale
{
    private readonly LinearScale? _linear;
    private readonly LogScale? _log;
    private readonly PowScale? _pow;
    private readonly TimeScale? _time;
    private readonly Discrete? _discrete;

    private ValueScale(LinearScale? linear, LogScale? log, PowScale? pow, TimeScale? time, Discrete? discrete = null)
    {
        _linear = linear;
        _log = log;
        _pow = pow;
        _time = time;
        _discrete = discrete;
    }

    public static ValueScale Create(ScaleKind kind, double min, double max, double start, double end) => kind switch
    {
        ScaleKind.Log => new(null, LogScale.Create([min, max], [start, end]), null, null),
        ScaleKind.Pow => new(null, null, PowScale.Create([min, max], [start, end], 1), null),
        ScaleKind.Sqrt => new(null, null, PowScale.Sqrt([min, max], [start, end]), null),
        ScaleKind.Time => new(null, null, null, TimeScale.Create([min, max], [start, end], TimeZoneInfo.Local)),
        ScaleKind.Utc => new(null, null, null, TimeScale.Create([min, max], [start, end], TimeZoneInfo.Utc)),
        ScaleKind.Identity or ScaleKind.Quantile or ScaleKind.Quantize or ScaleKind.Threshold => new(null, null, null, null, new Discrete(kind, min, max, start, end)),
        _ => new(LinearScale.Create([min, max], [start, end]), null, null, null),
    };

    /// <summary>The scale kind a numeric axis' <c>scale</c> names; <c>auto</c>, an unknown name or none is linear, and <c>sequential</c> over a range is linear too.</summary>
    public static ScaleKind Kind(string? scale) => scale switch
    {
        "log" => ScaleKind.Log,
        "pow" => ScaleKind.Pow,
        "sqrt" => ScaleKind.Sqrt,
        "time" => ScaleKind.Time,
        "utc" => ScaleKind.Utc,
        "identity" => ScaleKind.Identity,
        "quantile" => ScaleKind.Quantile,
        "quantize" => ScaleKind.Quantize,
        "threshold" => ScaleKind.Threshold,
        _ => ScaleKind.Linear,
    };

    /// <summary>Whether Recharts rounds the axis' domain to recharts-scale's nice ticks (<c>getTicksOfScale</c>): only for <c>auto</c> (an explicit <c>linear</c> is d3's <c>scaleLinear</c>, which it does not round).</summary>
    public static bool IsNice(string? scale) => scale is null or "auto";

    public double Map(double value)
    {
        if (_linear is { } linear)
        {
            return linear.Map(value);
        }

        return _log?.Map(value) ?? _pow?.Map(value) ?? _time?.Map(value) ?? (_discrete ?? Discrete.None).Map(value);
    }

    /// <summary>d3's ticks of the scale for a count; a scale without ticks gives its domain, as Recharts' <c>getTicksOfAxis</c> falls back to.</summary>
    public double[] Ticks(int count) =>
        _linear?.Ticks(count) ?? _log?.Ticks(count) ?? _pow?.Ticks(count) ?? _time?.Ticks(count) ?? (_discrete ?? Discrete.None).Ticks(count);

    /// <summary>
    /// d3's identity, quantile, quantize and threshold scales as Recharts sets them up (<c>domain([min, max])</c> then
    /// <c>range([start, end])</c>): identity takes the range as its domain and maps a value to itself; the others map to
    /// one of the two range ends, split at the domain's middle (quantile, quantize) or at its first value (threshold,
    /// whose second threshold has no range left). Kept apart from the scale (rare), so the scale stays a few references.
    /// </summary>
    private sealed record Discrete(ScaleKind Type, double Min, double Max, double Start, double End)
    {
        /// <summary>What a scale without any maps through, as a default discrete scale did.</summary>
        public static readonly Discrete None = new(default, 0, 0, 0, 0);

        public double Map(double value) => double.IsNaN(value) ? double.NaN : Type switch
        {
            ScaleKind.Identity => value,
            ScaleKind.Threshold => value < Min ? Start : End,
            _ => value < Split ? Start : End,
        };

        public double[] Ticks(int count) => Type switch
        {
            ScaleKind.Identity => Linear(Start, End, count),
            ScaleKind.Quantize => Linear(Min, Max, count),
            ScaleKind.Quantile => [Math.Min(Min, Max), Math.Max(Min, Max)],
            _ => [Min, Max],
        };

        private double Split => Type == ScaleKind.Quantile ? (Math.Min(Min, Max) + Math.Max(Min, Max)) / 2 : (Min + Max) / 2;

        private static double[] Linear(double start, double stop, int count)
        {
            var ticks = LinearScale.Create([Math.Min(start, stop), Math.Max(start, stop)], [0, 1]).Ticks(count);
            if (stop < start)
            {
                Array.Reverse(ticks);
            }

            return ticks;
        }
    }
}
