using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>
/// Recharts' <c>ErrorBar</c>: for each point with an error, a whisker of three lines (the far end, the stem, the near
/// end) around the value, along the values' axis, or along x for a scatter's <c>direction="x"</c>.
/// </summary>
internal static class ErrorBarPainter
{
    public static void Paint<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series)
        where TCanvas : struct, IChartCanvas
    {
        foreach (var bar in series.Spec.ErrorBars)
        {
            Paint(ref canvas, layout, series, bar);
        }
    }

    private static void Paint<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series, ErrorBarSpec bar)
        where TCanvas : struct, IChartCanvas
    {
        var scatter = series.Spec.Kind == SeriesKind.Scatter;
        var alongX = scatter ? bar.AlongX : layout.Vertical;
        var axis = alongX ? series.XAxis : series.YAxis;
        var values = Values(series, scatter && alongX);
        var errors = series.Data.Numbers(bar.DataKey);
        var pairs = series.Data.Lows(bar.DataKey);
        var paint = Painting.Paint.Stroked(bar.Stroke ?? layout.Spec.Colors.ErrorBarStroke, bar.StrokeWidth).With(bar.Presentation) with { Opacity = double.NaN };
        var offset = Offset(series, layout.Vertical);
        canvas.Open("recharts-layer recharts-errorBars"u8);
        for (var i = 0; i < errors.Length && i < values.Length; i++)
        {
            var pair = pairs.Length > 0 && !double.IsNaN(pairs[i]);
            if ((!pair && (double.IsNaN(errors[i]) || errors[i] == 0)) || !Position(series, i, out var x, out var y))
            {
                continue;
            }

            var (below, above) = Layout.AxisDomains.Error(series.Data, bar.DataKey, i);
            var low = axis.Map(values[i] - below);
            var high = axis.Map(values[i] + above);
            canvas.OpenFaded("recharts-layer recharts-errorBar"u8, bar.Presentation?.Opacity ?? double.NaN);
            if (alongX)
            {
                var middle = y + offset;
                canvas.Line(high, middle + bar.Width, high, middle - bar.Width, paint, default);
                canvas.Line(low, middle, high, middle, paint, default);
                canvas.Line(low, middle + bar.Width, low, middle - bar.Width, paint, default);
            }
            else
            {
                var middle = x + offset;
                canvas.Line(middle - bar.Width, high, middle + bar.Width, high, paint, default);
                canvas.Line(middle, low, middle, high, paint, default);
                canvas.Line(middle - bar.Width, low, middle + bar.Width, low, paint, default);
            }

            canvas.Close();
        }

        canvas.Close();
    }

    /// <summary>The values the whiskers stand on: a scatter's x values along x, a stacked bar's stack tops, or the series' values.</summary>
    private static double[] Values(SeriesLayout series, bool scatterX) =>
        scatterX ? series.XValues : series.StackTops.Length > 0 ? series.StackTops : series.Values;

    private static double Offset(SeriesLayout series, bool vertical) =>
        series.Spec.Kind != SeriesKind.Bar || series.Bars.Length == 0 ? 0 : vertical ? series.Bars[0].Height / 2 : series.Bars[0].Width / 2;

    private static bool Position(SeriesLayout series, int index, out double x, out double y)
    {
        if (series.Spec.Kind == SeriesKind.Bar)
        {
            (x, y) = (series.Bars[index].X, series.Bars[index].Y);
            return !double.IsNaN(x) && !double.IsNaN(y);
        }

        (x, y) = (series.X[index], series.Y[index]);
        return series.Defined[index];
    }
}
