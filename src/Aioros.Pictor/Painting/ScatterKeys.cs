using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>
/// Recharts' keyboard in a scatter chart with <c>accessibilityLayer</c>: its <c>AccessibilityManager</c> walks the
/// tooltip ticks of the x-axis (the scatters' x values in the order of their data, <c>categoricalDomain</c>) and spoofs
/// the pointer at each one's x and at the chart's top margin plus half its height; the chart keeps the pointer's y
/// (<c>getActiveCoordinate</c>) and draws only its <c>Cross</c> cursor there, since the spoofed payload has no value and
/// the tooltip's box stays hidden. A point whose spoofed pointer falls outside the plot draws nothing.
/// </summary>
internal static class ScatterKeys
{
    /// <summary>Whether the chart's keys draw Recharts' cross: a horizontal scatter chart with the keyboard layer and a tooltip drawing its cursor.</summary>
    public static bool Applies(IChartPlot plot) =>
        plot is ChartLayout { ItemTooltip: true, Vertical: false, HoverCount: > 0, XAxes.Count: > 0 } layout && layout.Spec.AccessibilityLayer
        && layout.Spec.Tooltip is { } tooltip && TipPainter.ShowsCursor(tooltip);

    /// <summary>The cross's x per key step, NaN where the spoofed pointer misses the plot, and the cross's y (NaN when it misses).</summary>
    public static (double[] X, double Y) Points(ChartLayout layout)
    {
        var axis = layout.XAxes[0];
        var y = Round(layout.Spec.Margin.Top + (layout.Spec.Height / 2));
        var inside = y >= layout.Top && y <= layout.Top + layout.PlotHeight;
        var xs = new List<double>();
        foreach (var series in layout.Series)
        {
            if (series.Spec.Kind != SeriesKind.Scatter || !ReferenceEquals(series.XAxis, axis))
            {
                continue;
            }

            for (var i = 0; i < series.X.Length; i++)
            {
                var x = series.Defined[i] ? series.X[i] : double.NaN;
                var pointer = Round(x);
                xs.Add(inside && pointer >= layout.Left && pointer <= layout.Left + layout.PlotWidth ? x : double.NaN);
            }
        }

        return ([.. xs], inside ? y : double.NaN);
    }

    /// <summary>The key step after a key (the arrows, clamped within the steps), or null when the key moves nothing.</summary>
    public static int? Move(bool right, bool left, int step, int count) =>
        right ? Math.Min(step + 1, count - 1) : left ? Math.Max(step - 1, 0) : null;

    /// <summary>Paints every step's cross in its own group of the <c>pictor-keys</c> layer (the web), or the one step's (the desktop).</summary>
    public static void Paint<TCanvas>(ref TCanvas canvas, ChartLayout layout, int step)
        where TCanvas : struct, IChartCanvas
    {
        var (xs, y) = Points(layout);
        var paint = TipPainter.CursorPaint(layout.Spec.Tooltip!, band: false, layout.Spec.Colors);
        canvas.Open("pictor-keys"u8);
        for (var i = 0; i < xs.Length; i++)
        {
            if (step >= 0 && i != step)
            {
                continue;
            }

            canvas.Open(default);
            if (!double.IsNaN(xs[i]))
            {
                canvas.Shape(new CrossShape(xs[i], y, layout.Left, layout.Top, layout.PlotWidth, layout.PlotHeight), paint, "recharts-cross recharts-tooltip-cursor"u8);
            }

            canvas.Close();
        }

        canvas.Close();
    }

    /// <summary>JavaScript's <c>Math.round</c>, as Recharts rounds the pointer's position in the chart.</summary>
    private static double Round(double value) => Math.Floor(value + 0.5);
}
