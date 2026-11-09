using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>
/// Recharts' <c>Scatter.getComposedData</c> in a scatter chart: each point at its x and y (the axes' data keys, or the
/// series' own), its symbol's area from the z-axis (the range's lower bound without a z value).
/// </summary>
internal static class ScatterGeometry
{
    private const double DefaultSize = 64;

    public static SeriesLayout Points(ChartLayout layout, SeriesSpec series, AxisLayout x, AxisLayout y)
    {
        var data = layout.SeriesData(series);
        var count = data.Count;
        var xValues = data.Numbers(x.Spec.DataKey ?? series.DataKey);
        var yValues = data.Numbers(y.Spec.DataKey ?? series.DataKey);
        var zAxis = layout.ZAxis(series.ZAxisId);
        var zValues = zAxis?.Spec.DataKey is { } zKey ? data.Numbers(zKey) : [];
        var xs = new double[count];
        var ys = new double[count];
        var sizes = new double[count];
        var defined = new bool[count];
        for (var i = 0; i < count; i++)
        {
            xs[i] = Coordinate(x, xValues, i);
            ys[i] = Coordinate(y, yValues, i);
            sizes[i] = zAxis is null ? DefaultSize : zAxis.Size(data, i);
            defined[i] = !double.IsNaN(xs[i]) && !double.IsNaN(ys[i]);
        }

        return new SeriesLayout
        {
            Spec = series,
            Theme = layout.Spec.Colors,
            XAxis = x,
            YAxis = y,
            ValueAxis = y,
            Data = data,
            Values = yValues,
            XValues = xValues,
            ZValues = zValues,
            Sizes = sizes,
            X = xs,
            Y = ys,
            Defined = defined,
        };
    }

    private static double Coordinate(AxisLayout axis, double[] values, int index) =>
        axis.IsCategory ? axis.Category(index) + (axis.BandSize / 2) : axis.Map(values[index]);
}
