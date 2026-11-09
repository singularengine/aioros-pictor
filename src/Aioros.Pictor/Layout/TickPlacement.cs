using System.Buffers;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Text;

namespace Aioros.Pictor.Layout;

/// <summary>
/// Recharts' <c>getTicks</c>: which tick labels an axis shows so that none overlap, measured with Inter's metrics where
/// Recharts measures them in the DOM, and how the end ticks move to stay inside the chart.
/// </summary>
internal static class TickPlacement
{
    public static AxisTick[] Visible(AxisLayout axis, double chartWidth, double chartHeight, double fontSize) =>
        Visible(axis, axis.Ticks, chartWidth, chartHeight, fontSize);

    /// <summary>Which of these ticks the axis would show (a grid's own values go through the axis' rules too).</summary>
    public static AxisTick[] Visible(AxisLayout axis, AxisTick[] ticks, double chartWidth, double chartHeight, double fontSize)
    {
        if (ticks.Length == 0 || !axis.Spec.Tick)
        {
            return [];
        }

        if (axis.Spec.Interval == TickInterval.Fixed)
        {
            return EveryNth(ticks, axis.Spec.IntervalCount + 1);
        }

        var horizontal = axis.Spec.Orientation is AxisOrientation.Top or AxisOrientation.Bottom;
        var sign = ticks.Length >= 2 ? Math.Sign(ticks[1].Coordinate - ticks[0].Coordinate) : 1;
        if (sign == 0)
        {
            return ticks;
        }

        var start = sign == 1 ? 0 : horizontal ? chartWidth : chartHeight;
        var end = sign == 1 ? horizontal ? chartWidth : chartHeight : 0;
        var rentedSizes = ArrayPool<double>.Shared.Rent(ticks.Length);
        var rentedShown = ArrayPool<bool>.Shared.Rent(ticks.Length);
        var sizes = rentedSizes.AsSpan(0, ticks.Length);
        var shown = rentedShown.AsSpan(0, ticks.Length);
        shown.Clear();
        Sizes(ticks, sizes, horizontal, fontSize, axis.Spec.Unit, axis.Spec.Angle);
        var visible = Collide(axis.Spec, sign, start, end, sizes, ticks, shown);
        ArrayPool<double>.Shared.Return(rentedSizes);
        ArrayPool<bool>.Shared.Return(rentedShown);
        return visible;
    }

    private static AxisTick[] Collide(AxisSpec spec, int sign, double start, double end, ReadOnlySpan<double> sizes, AxisTick[] ticks, Span<bool> shown)
    {
        switch (spec.Interval)
        {
            case TickInterval.EquidistantPreserveStart:
                var step = Equidistant(sign, start, end, sizes, ticks, spec.MinTickGap);
                return step == 0 ? [] : EveryNth(ticks, step);
            case TickInterval.PreserveEnd:
                End(sign, start, end, sizes, ticks, shown, spec.MinTickGap);
                break;
            default:
                Start(sign, start, end, sizes, ticks, shown, spec.MinTickGap, spec.Interval == TickInterval.PreserveStartEnd);
                break;
        }

        return Shown(ticks, shown);
    }

    private static AxisTick[] Shown(AxisTick[] ticks, ReadOnlySpan<bool> shown)
    {
        var visible = new AxisTick[shown.Count(true)];
        var k = 0;
        for (var i = 0; i < ticks.Length; i++)
        {
            if (shown[i])
            {
                visible[k++] = ticks[i];
            }
        }

        return visible;
    }

    /// <summary>
    /// Each label's size along the axis. Recharts measures a unit apart from the value and adds the two boxes, and on a
    /// horizontal axis takes the width of the widest horizontal line inside the rotated box (<c>getAngledTickWidth</c>).
    /// </summary>
    private static void Sizes(AxisTick[] ticks, Span<double> sizes, bool horizontal, double fontSize, string? unit, double angle)
    {
        var lineHeight = InterMetrics.LineHeight(fontSize);
        var hasUnit = horizontal && !string.IsNullOrEmpty(unit);
        var unitWidth = hasUnit ? InterMetrics.Width(unit, fontSize) : 0;
        var height = hasUnit ? 2 * lineHeight : lineHeight;
        for (var i = 0; i < ticks.Length; i++)
        {
            var text = ticks[i].Text.AsSpan();
            sizes[i] = !horizontal ? lineHeight
                : AngledWidth(hasUnit && text.EndsWith(unit, StringComparison.Ordinal) ? InterMetrics.Width(text[..^unit!.Length], fontSize) + unitWidth : InterMetrics.Width(text, fontSize), height, angle);
        }
    }

    /// <summary>Recharts' <c>getAngledRectangleWidth</c>: the widest horizontal line inside a box rotated by an angle.</summary>
    internal static double AngledWidth(double width, double height, double angle)
    {
        var normalized = ((angle % 180) + 180) % 180;
        if (normalized == 0)
        {
            return Math.Abs(width);
        }

        var radians = normalized * Math.PI / 180;
        var threshold = Math.Atan(height / width);
        return Math.Abs(radians > threshold && radians < Math.PI - threshold ? height / Math.Sin(radians) : width / Math.Cos(radians));
    }

    /// <summary>Recharts' <c>getEquidistantTicks</c>: the smallest step that shows the first tick and every step-th after it without collisions, or 0 when none does.</summary>
    private static int Equidistant(int sign, double initialStart, double end, ReadOnlySpan<double> sizes, AxisTick[] ticks, double gap)
    {
        for (var step = 1; step <= ticks.Length; step++)
        {
            var start = initialStart;
            var fits = true;
            for (var index = 0; index < ticks.Length && fits; index += step)
            {
                var coordinate = ticks[index].Coordinate;
                fits = index == 0 || IsVisible(sign, coordinate, sizes[index], start, end);
                start = coordinate + (sign * ((sizes[index] / 2) + gap));
            }

            if (fits)
            {
                return step;
            }
        }

        return 0;
    }

    private static void End(int sign, double start, double end, ReadOnlySpan<double> sizes, AxisTick[] ticks, Span<bool> shown, double gap)
    {
        for (var i = ticks.Length - 1; i >= 0; i--)
        {
            var coordinate = ticks[i].Coordinate;
            if (i == ticks.Length - 1)
            {
                var over = sign * (coordinate + (sign * sizes[i] / 2) - end);
                ticks[i] = ticks[i] with { LabelCoordinate = over > 0 ? coordinate - (over * sign) : coordinate };
            }

            if (IsVisible(sign, ticks[i].LabelCoordinate, sizes[i], start, end))
            {
                end = ticks[i].LabelCoordinate - (sign * ((sizes[i] / 2) + gap));
                shown[i] = true;
            }
        }
    }

    private static void Start(int sign, double start, double end, ReadOnlySpan<double> sizes, AxisTick[] ticks, Span<bool> shown, double gap, bool preserveEnd)
    {
        var count = ticks.Length;
        if (preserveEnd)
        {
            var last = count - 1;
            var over = sign * (ticks[last].Coordinate + (sign * sizes[last] / 2) - end);
            ticks[last] = ticks[last] with { LabelCoordinate = over > 0 ? ticks[last].Coordinate - (over * sign) : ticks[last].Coordinate };
            if (IsVisible(sign, ticks[last].LabelCoordinate, sizes[last], start, end))
            {
                end = ticks[last].LabelCoordinate - (sign * ((sizes[last] / 2) + gap));
                shown[last] = true;
            }

            count--;
        }

        for (var i = 0; i < count; i++)
        {
            if (i == 0)
            {
                var under = sign * (ticks[i].Coordinate - (sign * sizes[i] / 2) - start);
                ticks[i] = ticks[i] with { LabelCoordinate = under < 0 ? ticks[i].Coordinate - (under * sign) : ticks[i].Coordinate };
            }

            if (IsVisible(sign, ticks[i].LabelCoordinate, sizes[i], start, end))
            {
                start = ticks[i].LabelCoordinate + (sign * ((sizes[i] / 2) + gap));
                shown[i] = true;
            }
        }
    }

    private static bool IsVisible(int sign, double position, double size, double start, double end) =>
        sign * position >= sign * start && sign * position <= sign * end
        && sign * (position - (sign * size / 2) - start) >= 0 && sign * (position + (sign * size / 2) - end) <= 0;

    private static AxisTick[] EveryNth(AxisTick[] ticks, int n)
    {
        if (n <= 1)
        {
            return ticks;
        }

        var kept = new AxisTick[((ticks.Length - 1) / n) + 1];
        for (var i = 0; i < kept.Length; i++)
        {
            kept[i] = ticks[i * n];
        }

        return kept;
    }
}
