using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Motion;

/// <summary>CSS's named timing functions as cubic Bézier curves, evaluated as browsers evaluate them (the desktop's easing).</summary>
internal static class Easings
{
    public static double Ease(AnimationEasing easing, double t) => easing switch
    {
        AnimationEasing.Linear => t,
        AnimationEasing.EaseIn => Bezier(0.42, 0, 1, 1, t),
        AnimationEasing.EaseOut => Bezier(0, 0, 0.58, 1, t),
        AnimationEasing.EaseInOut => Bezier(0.42, 0, 0.58, 1, t),
        _ => Bezier(0.25, 0.1, 0.25, 1, t),
    };

    /// <summary>The curve's y where its x is <paramref name="t"/>: Newton's method on x, then bisection, as WebKit's <c>UnitBezier</c>.</summary>
    public static double Bezier(double x1, double y1, double x2, double y2, double t)
    {
        if (t <= 0 || t >= 1)
        {
            return Math.Clamp(t, 0, 1);
        }

        var s = t;
        for (var i = 0; i < 8; i++)
        {
            var error = Curve(x1, x2, s) - t;
            var slope = Slope(x1, x2, s);
            if (Math.Abs(error) < 1e-7)
            {
                return Curve(y1, y2, s);
            }

            if (Math.Abs(slope) < 1e-6)
            {
                break;
            }

            s -= error / slope;
        }

        var (low, high) = (0.0, 1.0);
        s = t;
        while (high - low > 1e-7)
        {
            if (Curve(x1, x2, s) < t)
            {
                low = s;
            }
            else
            {
                high = s;
            }

            s = (low + high) / 2;
        }

        return Curve(y1, y2, s);
    }

    private static double Curve(double p1, double p2, double s) => ((((1 - (3 * p2) + (3 * p1)) * s) + ((3 * p2) - (6 * p1))) * s + (3 * p1)) * s;

    private static double Slope(double p1, double p2, double s) => (3 * (1 - (3 * p2) + (3 * p1)) * s * s) + (2 * ((3 * p2) - (6 * p1)) * s) + (3 * p1);
}
