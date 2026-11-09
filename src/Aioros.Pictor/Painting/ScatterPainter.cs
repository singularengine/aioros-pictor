using Aioros.Graphs.Shapes;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Motion;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>Paints a scatter as Recharts' <c>Scatter</c> renders it statically: its joint line, its error bars, its symbols and their labels.</summary>
internal static class ScatterPainter
{
    private const double DefaultSize = 64;

    public static void Paint<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series)
        where TCanvas : struct, IChartCanvas
    {
        var spec = series.Spec;
        var clip = SeriesPainter.SeriesClip(layout, series);
        SeriesPainter.Clip(ref canvas, clip);
        var animated = SeriesPainter.OpenSeries(ref canvas, series, "recharts-layer recharts-scatter"u8, MorphKind.Glyph) is not null;
        if (clip is { } rect)
        {
            canvas.DefineClip(rect);
        }

        if (spec.JointLine)
        {
            canvas.Open("recharts-layer recharts-scatter-line"u8);
            var paint = new Paint(spec.LineStroke ?? series.Fill, spec.LineStrokeWidth ?? double.NaN, "none", double.NaN, spec.LineStrokeDashArray);
            if (spec.LineType == ScatterLineType.Fitting)
            {
                var (x, y) = Fitting(series.X, series.Y);
                canvas.Shape(new CurveShape(Curves.Of(spec.LineJointType), x, y, [true, true]), paint, "recharts-curve"u8);
            }
            else
            {
                canvas.Shape(new CurveShape(Curves.Of(spec.LineJointType), series.X, series.Y, series.Defined), paint, "recharts-curve"u8);
            }

            canvas.Close();
        }

        ErrorBarPainter.Paint(ref canvas, layout, series);
        canvas.Open("recharts-layer"u8);
        var content = ContentProps.Of(spec.ShapeContent, Content.ContentSlots.Scatter, series.Data);
        var active = ContentProps.Of(spec.ActiveShapeContent, Content.ContentSlots.Scatter, series.Data);
        for (var i = 0; i < series.X.Length; i++)
        {
            if (series.Defined[i])
            {
                Point(ref canvas, series, i, i == spec.ActiveIndex ? active : content, i == spec.ActiveIndex, animated);
            }
        }

        canvas.Close();
        SeriesPainter.PointLabels(ref canvas, layout, series);
        canvas.Close();
    }

    /// <summary>A point's symbol layer: its growth when its series animates in, its custom shape or its symbol, in Recharts' active layer when the series' <c>activeIndex</c> names it.</summary>
    private static void Point<TCanvas>(ref TCanvas canvas, SeriesLayout series, int i, Content.ContentValues? shown, bool active, bool animated)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Open("recharts-layer recharts-scatter-symbol"u8);
        if (active)
        {
            canvas.Open("recharts-layer recharts-active-shape"u8);
        }

        if (animated)
        {
            canvas.Animate(new Step(StepKind.ScaleArea));
            Morph(ref canvas, series, i, shown);
        }

        if (shown is not null)
        {
            canvas.Content(shown.Spec, ContentProps.Scatter(shown, series, i, Fill(series, i), ContentProps.Template(series.Spec.Order, active ? 1 : 0)));
        }
        else
        {
            canvas.Shape(Symbol(series, i), Fill(series, i), "recharts-symbols"u8);
        }

        if (active)
        {
            canvas.Close();
        }

        canvas.Close();
    }

    /// <summary>
    /// Recharts' <c>getLinearRegression</c> over the points' coordinates (a missing one counts as 0, as JavaScript's
    /// <c>|| 0</c>): the least-squares line from the smallest to the largest x.
    /// </summary>
    public static (double[] X, double[] Y) Fitting(double[] xs, double[] ys)
    {
        var count = xs.Length;
        double xsum = 0, ysum = 0, xysum = 0, xxsum = 0, xmin = double.PositiveInfinity, xmax = double.NegativeInfinity;
        for (var i = 0; i < count; i++)
        {
            var x = double.IsNaN(xs[i]) ? 0 : xs[i];
            var y = double.IsNaN(ys[i]) ? 0 : ys[i];
            xsum += x;
            ysum += y;
            xysum += x * y;
            xxsum += x * x;
            xmin = Math.Min(xmin, x);
            xmax = Math.Max(xmax, x);
        }

        var a = count * xxsum != xsum * xsum ? ((count * xysum) - (xsum * ysum)) / ((count * xxsum) - (xsum * xsum)) : 0;
        var b = (ysum - (a * xsum)) / count;
        return ([xmin, xmax], [(a * xmin) + b, (a * xmax) + b]);
    }

    /// <summary>The numbers a default symbol moves by on the web when the framework morphs it; none for custom content.</summary>
    private static void Morph<TCanvas>(ref TCanvas canvas, SeriesLayout series, int i, Content.ContentValues? content)
        where TCanvas : struct, IChartCanvas
    {
        if (content is null)
        {
            canvas.Morph(MorphMark.Numbers(MorphKind.Glyph, new Six(series.X[i], series.Y[i], series.Sizes.Length > i ? series.Sizes[i] : DefaultSize), 3, new Six(double.NaN, double.NaN, 0), 3));
        }
    }

    /// <summary>The symbol of a point: the series' shape at the point's area.</summary>
    public static SymbolShape Symbol(SeriesLayout series, int index) =>
        new(Type(series.Spec.Shape), series.Sizes.Length > index ? series.Sizes[index] : DefaultSize, series.X[index], series.Y[index]);

    /// <summary>The radius of the circle that holds a point's symbol, Recharts' <c>sqrt(size / PI)</c>.</summary>
    public static double Radius(SeriesLayout series, int index) => Math.Sqrt(Math.Max(series.Sizes.Length > index ? series.Sizes[index] : DefaultSize, 0) / Math.PI);

    public static Paint Fill(SeriesLayout series, int index)
    {
        var cell = index < series.Spec.Cells.Length ? series.Spec.Cells[index] : null;
        return SeriesPainter.Celled(SeriesPainter.ItemOpacity(series, new Paint(cell?.Stroke ?? series.Spec.Stroke, double.NaN, cell?.Fill ?? series.ItemFills?[index] ?? series.Fill), index), cell);
    }

    private static SymbolType Type(ScatterShape shape) => shape switch
    {
        ScatterShape.Cross => SymbolType.Cross,
        ScatterShape.Diamond => SymbolType.Diamond,
        ScatterShape.Square => SymbolType.Square,
        ScatterShape.Star => SymbolType.Star,
        ScatterShape.Triangle => SymbolType.Triangle,
        ScatterShape.Wye => SymbolType.Wye,
        _ => SymbolType.Circle,
    };
}
