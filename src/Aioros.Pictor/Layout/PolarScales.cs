using System.Collections.Immutable;
using Aioros.Graphs.Scales;
using Aioros.Pictor.Specs;
using Aioros.Graphs.Formatting;
using Aioros.Pictor.Ticks;

namespace Aioros.Pictor.Layout;

/// <summary>The numeric polar axes' props, tick values and tick texts (Recharts' polar <c>formatAxisMap</c>).</summary>
internal static class PolarScales
{
    /// <summary>An angle axis' numeric props when it is of type <c>number</c>, or the defaults; Recharts 3 ignores its <c>allowDataOverflow</c>.</summary>
    public static NumericProps Numeric(PolarAngleAxisSpec? angle, bool recharts3 = false) => angle?.Type == AxisType.Number
        ? new NumericProps(angle.DomainMin, angle.DomainMax, angle.AllowDataOverflow && !recharts3, angle.TickCount ?? 0, angle.AllowDecimals, angle.Scale, angle.Reversed, angle.Ticks, angle.TickFormat) { Formatter = Text.FormatKeys.TickFunction(angle.TickFormatter, angle.TickTexts, angle.TickConverter) }
        : new NumericProps();

    /// <summary>A radius axis' numeric props, or the defaults without one.</summary>
    public static NumericProps Numeric(PolarRadiusAxisSpec? axis) => axis is null
        ? new NumericProps()
        : new NumericProps(axis.DomainMin, axis.DomainMax, axis.AllowDataOverflow, axis.TickCount, axis.AllowDecimals, axis.Scale, axis.Reversed, axis.Ticks, axis.TickFormat) { Formatter = Text.FormatKeys.TickFunction(axis.TickFormatter, axis.TickTexts, axis.TickConverter) };

    public static double[] Parsed(ImmutableArray<string> ticks) =>
        ticks.IsDefaultOrEmpty ? [] : [.. ticks.Select(tick => double.TryParse(tick, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : double.NaN)];

    /// <summary>The tick values: recharts-scale's nice ticks for an <c>auto</c> scale (rounding an automatic domain to them), d3's ticks of the scale otherwise.</summary>
    public static double[] TickValues(NumericProps axis, ScaleKind kind, bool nice, ref double low, ref double high, double rangeStart, double rangeEnd)
    {
        var count = axis.TickCount == 0 ? 5 : axis.TickCount;
        if (!nice || !ValueScale.IsNice(axis.Scale))
        {
            return ValueScale.Create(kind, low, high, rangeStart, rangeEnd).Ticks(nice ? count : 10);
        }

        if ((axis.DomainMin ?? "0") != "auto" && (axis.DomainMax ?? "auto") != "auto")
        {
            return NiceTicks.FixedDomain(low, high, count, axis.Decimals);
        }

        var values = NiceTicks.Compute(low, high, count, axis.Decimals);
        if (values.Length > 0)
        {
            (low, high) = (values.Min(), values.Max());
        }

        return values;
    }

    public static string Format(string text, string? format) =>
        Text.Formats.Apply(format, text, text);

    /// <summary>A category tick's text: its axis' function's, else its format's.</summary>
    public static string Format(string text, int index, string? format, Func<object?, int, string?>? formatter) =>
        formatter?.Invoke(text, index) ?? Format(text, format);

    public static string Format(string text, double value, string? format) =>
        Text.Formats.Apply(format, value, text);

    /// <summary>The extent of the values (and the lower ends of pairs) of the data keys.</summary>
    public static (double Min, double Max) Extent(Data.ChartData data, IEnumerable<string> keys)
    {
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var key in keys)
        {
            foreach (var value in data.Numbers(key).Concat(data.Lows(key)).Where(value => !double.IsNaN(value)))
            {
                (min, max) = (Math.Min(min, value), Math.Max(max, value));
            }
        }

        return (min, max);
    }

    /// <summary>The ticks of the values the scale places (a value it maps to no number left out), with their texts.</summary>
    public static PolarTick[] Ticks(ValueScale scale, double[] values, string? format, Func<object?, int, string?>? formatter = null)
    {
        var ticks = new List<PolarTick>(values.Length);
        foreach (var value in values)
        {
            var coordinate = scale.Map(value);
            if (!double.IsNaN(coordinate))
            {
                ticks.Add(new PolarTick(formatter?.Invoke(value + 0d, ticks.Count) ?? Format(JsNumber.ToShortestString(value), value, format), coordinate, ticks.Count));
            }
        }

        return [.. ticks];
    }
}
