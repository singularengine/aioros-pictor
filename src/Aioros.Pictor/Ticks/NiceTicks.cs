namespace Aioros.Pictor.Ticks;

/// <summary>
/// recharts-scale's tick values (<c>getNiceTickValues</c>, <c>getTickValuesFixedDomain</c>), ported operation for
/// operation with <see cref="decimal"/> in place of decimal.js-light.
/// </summary>
public static class NiceTicks
{
    private const int MaxSteps = 100000;

    private const int MaxCorrections = 64;

    /// <summary>
    /// Ticks covering <paramref name="min"/> to <paramref name="max"/> with exactly <paramref name="tickCount"/> values
    /// at an easy step (1, 2, 2.5, 5 and their powers of ten), zero among them when the interval holds it.
    /// </summary>
    /// <param name="min">The smallest value to cover.</param>
    /// <param name="max">The largest value to cover.</param>
    /// <param name="tickCount">How many ticks to return; at least two are computed.</param>
    /// <param name="allowDecimals">Whether a step may be fractional.</param>
    /// <returns>The tick values, ascending (descending when <paramref name="min"/> is above <paramref name="max"/>).</returns>
    public static double[] Compute(double min, double max, int tickCount = 6, bool allowDecimals = true)
    {
        var count = Math.Max(tickCount, 2);
        var (low, high) = min > max ? (max, min) : (min, max);
        double[] values;
        if (double.IsNegativeInfinity(low) || double.IsPositiveInfinity(high))
        {
            values = new double[Math.Max(tickCount, 1)];
            Array.Fill(values, double.IsPositiveInfinity(high) ? double.PositiveInfinity : double.NegativeInfinity);
            if (double.IsPositiveInfinity(high))
            {
                values[0] = low;
            }
            else
            {
                values[^1] = high;
            }
        }
        else if (low == high)
        {
            return SingleValue(low, tickCount, allowDecimals);
        }
        else if (!double.IsFinite((high - low) / (count - 1)) || !IsDecimal(low) || !IsDecimal(high))
        {
            values = [];
        }
        else
        {
            var (step, tickMin, tickMax) = Step(low, high, count, allowDecimals, 0);
            values = RangeStep(tickMin, tickMax + (0.1m * step), step);
        }

        if (min > max)
        {
            Array.Reverse(values);
        }

        return values;
    }

    /// <summary>Ticks from <paramref name="min"/> to <paramref name="max"/> exactly, at an easy step between them.</summary>
    /// <param name="min">The domain's start.</param>
    /// <param name="max">The domain's end.</param>
    /// <param name="tickCount">The count of ticks wanted.</param>
    /// <param name="allowDecimals">Whether a step may be fractional.</param>
    /// <returns>The tick values.</returns>
    public static double[] FixedDomain(double min, double max, int tickCount, bool allowDecimals = true)
    {
        var (low, high) = min > max ? (max, min) : (min, max);
        if (double.IsInfinity(low) || double.IsInfinity(high) || !IsDecimal(low) || !IsDecimal(high))
        {
            return [min, max];
        }

        if (low == high)
        {
            return [low];
        }

        var count = Math.Max(tickCount, 2);
        var step = FormatStep(((decimal)high - (decimal)low) / (count - 1), allowDecimals, 0);
        var stepped = RangeStep((decimal)low, (decimal)high - (0.99m * step), step);
        var values = new double[stepped.Length + 1];
        stepped.CopyTo(values, 0);
        values[^1] = high;
        if (min > max)
        {
            Array.Reverse(values);
        }

        return values;
    }

    private static bool IsDecimal(double value) => Math.Abs(value) < 7.9e27;

    private static (decimal Step, decimal TickMin, decimal TickMax) Step(double min, double max, int tickCount, bool allowDecimals, int correction)
    {
        while (true)
        {
            var step = FormatStep(((decimal)max - (decimal)min) / (tickCount - 1), allowDecimals, correction);
            if (step == 0 || correction > MaxCorrections)
            {
                return (0, 0, 0);
            }

            var middle = Middle(min, max, step);
            var below = (int)Math.Ceiling((double)((middle - (decimal)min) / step));
            var up = (int)Math.Ceiling((double)(((decimal)max - middle) / step));
            var missing = tickCount - (below + up + 1);
            if (missing < 0)
            {
                correction++;
                continue;
            }

            if (max > 0)
            {
                up += missing;
            }
            else
            {
                below += missing;
            }

            return (step, middle - (below * step), middle + (up * step));
        }
    }

    private static decimal Middle(double min, double max, decimal step)
    {
        if (min <= 0 && max >= 0)
        {
            return 0;
        }

        var middle = ((decimal)min + (decimal)max) / 2;
        return middle - (middle % step);
    }

    private static decimal FormatStep(decimal roughStep, bool allowDecimals, int correction)
    {
        if (roughStep <= 0)
        {
            return 0;
        }

        var digits = DigitCount(roughStep);
        var magnitude = Power(digits);
        var ratio = roughStep / magnitude;
        var ratioScale = digits != 1 ? 0.05m : 0.1m;
        var amended = (Math.Ceiling(ratio / ratioScale) + correction) * ratioScale;
        var step = amended * magnitude;
        return allowDecimals ? step : Math.Ceiling(step);
    }

    private static double[] SingleValue(double value, int tickCount, bool allowDecimals)
    {
        var (middle, step) = SingleMiddle(value, tickCount, allowDecimals);
        var middleIndex = (int)Math.Floor((tickCount - 1) / 2.0);
        var values = new double[Math.Max(tickCount, 0)];
        for (var n = 0; n < values.Length; n++)
        {
            values[n] = (double)(middle + ((n - middleIndex) * step));
        }

        return values;
    }

    private static (decimal Middle, decimal Step) SingleMiddle(double value, int tickCount, bool allowDecimals)
    {
        var middle = (decimal)value;
        if (middle != Math.Floor(middle) && allowDecimals)
        {
            var absolute = Math.Abs(value);
            if (absolute < 1)
            {
                var step = Power(DigitCount((decimal)value) - 1);
                return (Math.Floor(middle / step) * step, step);
            }

            return (absolute > 1 ? (decimal)Math.Floor(value) : middle, 1);
        }

        if (value == 0)
        {
            return (Math.Floor((tickCount - 1) / 2m), 1);
        }

        return (allowDecimals ? middle : (decimal)Math.Floor(value), 1);
    }

    private static double[] RangeStep(decimal start, decimal end, decimal step)
    {
        var values = new List<double>();
        var number = start;
        for (var i = 0; number < end && i < MaxSteps; i++)
        {
            values.Add((double)number);
            number += step;
        }

        return [.. values];
    }

    private static int DigitCount(decimal value)
    {
        if (value == 0)
        {
            return 1;
        }

        var absolute = Math.Abs(value);
        var digits = 0;
        while (absolute >= 1)
        {
            absolute /= 10;
            digits++;
        }

        while (absolute < 0.1m)
        {
            absolute *= 10;
            digits--;
        }

        return digits;
    }

    private static decimal Power(int exponent)
    {
        decimal power = 1;
        for (var i = 0; i < exponent; i++)
        {
            power *= 10;
        }

        for (var i = 0; i > exponent; i--)
        {
            power /= 10;
        }

        return power;
    }
}
