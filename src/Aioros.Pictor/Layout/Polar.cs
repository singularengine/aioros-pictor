using System.Globalization;

namespace Aioros.Pictor.Layout;

/// <summary>Recharts' <c>PolarUtils</c> and <c>getPercentValue</c>: angles in degrees counterclockwise from 3 o'clock, y down.</summary>
internal static class Polar
{
    public const double Radian = Math.PI / 180;

    public static (double X, double Y) Point(double cx, double cy, double radius, double angle) =>
        (cx + (Math.Cos(-Radian * angle) * radius), cy + (Math.Sin(-Radian * angle) * radius));

    /// <summary>A number of pixels, or a percentage of <paramref name="total"/>; <paramref name="fallback"/> when it is neither.</summary>
    public static double Percent(string? value, double total, double fallback = 0, bool validate = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var text = value.AsSpan().Trim();
        double result;
        if (text.EndsWith('%'))
        {
            result = double.TryParse(text[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) ? total * percent / 100 : double.NaN;
        }
        else
        {
            result = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : double.NaN;
        }

        if (double.IsNaN(result))
        {
            return fallback;
        }

        return validate && result > total ? total : result;
    }

    public static double MaxRadius(double width, double height, double left, double right, double top, double bottom) =>
        Math.Min(Math.Abs(width - left - right), Math.Abs(height - top - bottom)) / 2;

    /// <summary>The distance and the angle of a point from a center, the angle in [0, 360).</summary>
    public static (double Radius, double Angle) AngleOf(double x, double y, double cx, double cy)
    {
        var radius = Math.Sqrt(((x - cx) * (x - cx)) + ((y - cy) * (y - cy)));
        if (radius <= 0)
        {
            return (radius, double.NaN);
        }

        var angle = Math.Acos((x - cx) / radius);
        if (y > cy)
        {
            angle = (2 * Math.PI) - angle;
        }

        return (radius, angle / Radian);
    }

    /// <summary>Recharts' <c>inRangeOfSector</c>: the point's angle within the sector, in the sector's turn, or NaN outside it.</summary>
    public static double InSector(double x, double y, double cx, double cy, double innerRadius, double outerRadius, double startAngle, double endAngle)
    {
        var (radius, angle) = AngleOf(x, y, cx, cy);
        if (radius < innerRadius || radius > outerRadius)
        {
            return double.NaN;
        }

        if (radius == 0)
        {
            return startAngle;
        }

        var turns = Math.Min(Math.Floor(startAngle / 360), Math.Floor(endAngle / 360));
        var start = startAngle - (turns * 360);
        var end = endAngle - (turns * 360);
        var low = Math.Min(start, end);
        var high = Math.Max(start, end);
        while (angle > high)
        {
            angle -= 360;
        }

        while (angle < low)
        {
            angle += 360;
        }

        return angle >= low && angle <= high ? angle + (turns * 360) : double.NaN;
    }
}
