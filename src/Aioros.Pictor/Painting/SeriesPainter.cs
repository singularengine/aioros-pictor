using System.Globalization;
using Aioros.Graphs.Formatting;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Motion;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>Paints a series as Recharts' <c>Line</c>, <c>Area</c> and <c>Bar</c> render it statically.</summary>
internal static class SeriesPainter
{
    public static void Paint<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series)
        where TCanvas : struct, IChartCanvas
    {
        switch (series.Spec.Kind)
        {
            case SeriesKind.Line:
                Line(ref canvas, layout, series);
                break;
            case SeriesKind.Area:
                Area(ref canvas, layout, series);
                break;
            case SeriesKind.Scatter:
                ScatterPainter.Paint(ref canvas, layout, series);
                break;
            default:
                Bars(ref canvas, layout, series);
                break;
        }
    }

    public static string Value(double value, string? format)
    {
        if (double.IsNaN(value))
        {
            return string.Empty;
        }

        return Pictor.Text.Formats.Apply(format, value, JsNumber.ToShortestString(value));
    }

    private static (double[] X, double[] Y, double[] Base, bool[] Defined) Points(SeriesLayout series)
    {
        if (!series.Spec.ConnectNulls)
        {
            return (series.X, series.Y, series.Base, series.Defined);
        }

        var count = series.Defined.Count(defined => defined);
        var x = new double[count];
        var y = new double[count];
        var bases = new double[count];
        var k = 0;
        for (var i = 0; i < series.Defined.Length; i++)
        {
            if (series.Defined[i])
            {
                (x[k], y[k], bases[k]) = (series.X[i], series.Y[i], series.Base[i]);
                k++;
            }
        }

        var defined = new bool[count];
        Array.Fill(defined, true);
        return (x, y, bases, defined);
    }

    private static void Line<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series)
        where TCanvas : struct, IChartCanvas
    {
        var spec = series.Spec;
        var (x, y, _, defined) = Points(series);
        var morph = Curves.Morph(spec.Curve, layout.Vertical);
        var timing = OpenSeries(ref canvas, series, "recharts-layer recharts-line"u8, morph is null ? MorphKind.None : MorphKind.Line, morph ?? 0, ratio: true);
        if (timing is not null)
        {
            canvas.Animate(new Step(spec.StrokeDashArray is null ? StepKind.Draw : StepKind.DrawDashed, Offset: spec.StrokeWidth));
        }

        var clip = DefineClips(ref canvas, layout, series);
        Clip(ref canvas, clip);
        if (timing is not null && morph is { } curved)
        {
            canvas.Morph(MorphMark.Points(MorphKind.Line, curved, series.X, series.Y, spec.AnimateNewValues ? (layout.PlotWidth * 2, layout.PlotHeight / 2) : null));
        }

        canvas.Shape(new CurveShape(Curves.Of(spec.Curve, layout.Vertical), x, y, defined), new Paint(series.Stroke, spec.StrokeWidth, "none", double.NaN, spec.StrokeDashArray).With(spec.Presentation), "recharts-curve recharts-line-curve"u8);
        Clip(ref canvas, clip);
        canvas.Open("recharts-layer"u8);
        ErrorBarPainter.Paint(ref canvas, layout, series);
        canvas.Close();
        if (spec.Dot)
        {
            Clip(ref canvas, DotsClip(layout, series, clip));
            Dots(ref canvas, series, "recharts-layer recharts-line-dots"u8, "recharts-dot recharts-line-dot"u8);
        }

        PointLabels(ref canvas, layout, series);
        canvas.Close();
    }

    private static void Area<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series)
        where TCanvas : struct, IChartCanvas
    {
        var spec = series.Spec;
        var (x, y, bases, defined) = Points(series);
        var curve = Curves.Of(spec.Curve, layout.Vertical);
        var opacity = spec.FillOpacity ?? 0.6;
        var fill = new Paint("none", double.NaN, series.Fill, opacity).With(spec.Presentation);
        var morph = Curves.Morph(spec.Curve, layout.Vertical);
        var timed = OpenSeries(ref canvas, series, "recharts-layer recharts-area"u8, morph is null ? MorphKind.None : MorphKind.Band, morph ?? 0, ratio: true) is not null;
        var (band, top, bottom) = timed && morph is { } curved ? AreaMarks(layout.Vertical, series, curved) : default;
        var reveal = timed ? new Step(layout.Vertical ? StepKind.RevealDown : StepKind.Reveal) : default;
        var clip = DefineClips(ref canvas, layout, series);
        Clip(ref canvas, clip);
        canvas.Open("recharts-layer"u8);
        canvas.Animate(reveal);
        Morph(ref canvas, band);
        if (layout.Vertical)
        {
            canvas.Shape(new VerticalAreaShape(curve, bases, y, x, defined), fill, "recharts-curve recharts-area-area"u8);
        }
        else
        {
            canvas.Shape(new AreaShape(curve, x, bases, y, defined), fill, "recharts-curve recharts-area-area"u8);
        }

        if (series.Stroke != "none")
        {
            canvas.Animate(reveal);
            Morph(ref canvas, top);
            canvas.Shape(new CurveShape(curve, x, y, defined), new Paint(series.Stroke, spec.StrokeWidth, "none", opacity, spec.StrokeDashArray).With(spec.Presentation), "recharts-curve recharts-area-curve"u8);
            if (series.IsRange)
            {
                canvas.Animate(reveal);
                Morph(ref canvas, bottom);

                canvas.Shape(BaseCurve(curve, layout.Vertical, x, y, bases), new Paint(series.Stroke, spec.StrokeWidth, "none", opacity, spec.StrokeDashArray).With(spec.Presentation), "recharts-curve recharts-area-curve"u8);
            }
        }

        canvas.Close();
        if (spec.Dot)
        {
            Clip(ref canvas, DotsClip(layout, series, clip));
            Dots(ref canvas, series, "recharts-layer recharts-area-dots"u8, "recharts-dot recharts-area-dot"u8);
        }

        PointLabels(ref canvas, layout, series);
        canvas.Close();
    }

    /// <summary>
    /// An area's numbers for the web's update: its band (points forward, base points back, as d3's <c>area</c>), its
    /// stroke through its points and a range area's stroke through its base points.
    /// </summary>
    private static (MorphMark? Band, MorphMark? Top, MorphMark? Bottom) AreaMarks(bool vertical, SeriesLayout series, byte curve) => vertical
        ? (MorphMark.Band(curve, series.X, series.Y, series.Base, series.Y), MorphMark.Points(MorphKind.Line, curve, series.X, series.Y), MorphMark.Points(MorphKind.Line, curve, series.Base, series.Y))
        : (MorphMark.Band(curve, series.X, series.Y, series.X, series.Base), MorphMark.Points(MorphKind.Line, curve, series.X, series.Y), MorphMark.Points(MorphKind.Line, curve, series.X, series.Base));

    /// <summary>A range area's baseline as a curve (Recharts strokes it when the area has a stroke), through the points where it is defined.</summary>
    private static CurveShape BaseCurve(Graphs.Shapes.Curve curve, bool vertical, double[] x, double[] y, double[] bases)
    {
        var along = vertical ? y : x;
        var defined = new bool[bases.Length];
        for (var i = 0; i < bases.Length; i++)
        {
            defined[i] = !double.IsNaN(bases[i]) && !double.IsNaN(along[i]);
        }

        return vertical ? new CurveShape(curve, bases, y, defined) : new CurveShape(curve, x, bases, defined);
    }

    private static void Dots<TCanvas>(ref TCanvas canvas, SeriesLayout series, ReadOnlySpan<byte> layer, ReadOnlySpan<byte> classes)
        where TCanvas : struct, IChartCanvas
    {
        var spec = series.Spec;
        var paint = spec.Kind == SeriesKind.Line
            ? new Paint(series.Stroke, spec.StrokeWidth, series.Fill, spec.FillOpacity ?? double.NaN, spec.StrokeDashArray)
            : new Paint(series.Stroke, spec.StrokeWidth, series.Fill, spec.FillOpacity ?? 0.6, spec.StrokeDashArray);
        var (radius, dot) = DotPaint(spec.DotProps, paint);
        dot = dot.With(spec.Presentation);
        var content = ContentProps.Of(spec.DotContent, Content.ContentSlots.Dot, series.Data);
        canvas.Open(layer);
        for (var i = 0; i < series.X.Length; i++)
        {
            if (!series.Defined[i])
            {
                continue;
            }

            if (content is not null)
            {
                canvas.Content(content.Spec, ContentProps.Dot(content, series, i, radius, dot, ContentProps.Template(spec.Order, 2)));
            }
            else
            {
                canvas.Circle(series.X[i], series.Y[i], radius, dot, classes);
            }
        }

        canvas.Close();
    }

    /// <summary>A dot's radius and paint: Recharts' 3 and the series' paint, under the <c>dot</c> object's props.</summary>
    public static (double Radius, Paint Paint) DotPaint(ActiveStyle? dot, in Paint paint) => dot is null
        ? (3, paint)
        : (dot.R ?? 3, new Paint(dot.Stroke ?? paint.Stroke, dot.StrokeWidth ?? paint.StrokeWidth, dot.Fill ?? paint.Fill, dot.FillOpacity ?? paint.FillOpacity, dot.StrokeDashArray ?? paint.Dash, paint.StrokeOpacity));

    public static void PointLabels<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series)
        where TCanvas : struct, IChartCanvas
    {
        var list = 0;
        foreach (var label in new LabelLists(series.Spec.Labels, series.Spec.LabelLists))
        {
            var content = ContentProps.Of(label.Content, Content.ContentSlots.Label, series.Data);
            var template = ContentProps.Template(series.Spec.Order, 4 + list++);
            canvas.Open("recharts-layer recharts-label-list"u8);
            for (var i = 0; i < series.X.Length; i++)
            {
                if (series.Defined[i] && Text(series, label, i) is { } text)
                {
                    LabelPainter.Cartesian(ref canvas, label, text, (series.X[i], series.Y[i], 0, 0), layout.Spec.FontSize, layout.Spec.Colors.LabelText, content: ContentProps.Label(content, template, i, ContentProps.Value(series, i), series.Spec.Name ?? series.Spec.DataKey));
                }
            }

            canvas.Close();
        }
    }

    /// <summary>The text of a series' label at an item: its value, or the list's own key's number or text.</summary>
    private static string? Text(SeriesLayout series, LabelSpec label, int index) =>
        label.DataKey is { } key ? LabelPainter.Text(series.Data, key, index, label.Format) : Value(series.StackTops.Length > 0 ? series.StackTops[index] : series.Values[index], label.Format);

    private static void Bars<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series)
        where TCanvas : struct, IChartCanvas
    {
        var spec = series.Spec;
        var morph = spec.Radius.IsDefaultOrEmpty || spec.Radius.All(radius => radius == 0) ? MorphKind.Rect : MorphKind.None;
        var animated = OpenSeries(ref canvas, series, "recharts-layer recharts-bar"u8, morph) is not null;
        var clip = DefineClips(ref canvas, layout, series);
        Clip(ref canvas, clip);
        canvas.Open("recharts-layer recharts-bar-rectangles"u8);
        if (spec.Background is { } background)
        {
            Backgrounds(ref canvas, layout, series, background);
        }

        var shape = ContentProps.Of(spec.ShapeContent, Content.ContentSlots.Bar, series.Data);
        for (var i = 0; i < series.Bars.Length; i++)
        {
            canvas.Open("recharts-layer recharts-bar-rectangle"u8);
            if (i == spec.ActiveIndex)
            {
                BarTips.StaticActive(ref canvas, layout, series, i, animated ? morph : null);
            }
            else if (IsDrawn(series.Bars[i]))
            {
                Bar(ref canvas, layout, series, i, animated ? morph : null, shape);
            }

            canvas.Close();
        }

        canvas.Close();
        Clip(ref canvas, clip);
        canvas.Open("recharts-layer"u8);
        ErrorBarPainter.Paint(ref canvas, layout, series);
        canvas.Close();
        BarLabels(ref canvas, layout, series);
        canvas.Close();
    }

    /// <summary>A bar drawn: its growth when its series animates in, then its rectangle or its custom shape.</summary>
    internal static void Bar<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series, int index, MorphKind? morph, Content.ContentValues? shape)
        where TCanvas : struct, IChartCanvas
    {
        var bar = series.Bars[index];
        if (morph is { } kind)
        {
            canvas.Animate(new Step(Growth(bar, layout.Vertical)));
            Morph(ref canvas, kind == MorphKind.Rect && shape is null ? BarMark(bar, layout.Vertical) : null);
        }

        if (shape is not null)
        {
            canvas.Content(shape.Spec, ContentProps.Bar(shape, series, index, BarPaint(series, index), ContentProps.Template(series.Spec.Order, 0)));
            return;
        }

        canvas.Shape(new RectShape(bar.X, bar.Y, bar.Width, bar.Height, series.Spec.Radius), BarPaint(series, index), ClassNames.With("recharts-rectangle"u8, series.Spec.ClassName));
    }

    /// <summary>
    /// Recharts' clip of a series on an axis that allows data overflow (<c>needClip</c>): the plot along that axis, twice
    /// the plot's size centered on it along the other; null when neither axis allows it.
    /// </summary>
    public static ClipRect? SeriesClip(ChartLayout layout, SeriesLayout series)
    {
        var alongX = series.XAxis.Spec.AllowDataOverflow;
        var alongY = series.YAxis.Spec.AllowDataOverflow;
        if (!alongX && !alongY)
        {
            return null;
        }

        var (left, top, width, height) = (layout.Left, layout.Top, layout.PlotWidth, layout.PlotHeight);
        return new ClipRect(alongX ? left : left - (width / 2), alongY ? top : top - (height / 2), alongX ? width : width * 2, alongY ? height : height * 2);
    }

    /// <summary>Defines a series' clips (its own and, for dots that are not clipped, the plot grown by a dot), returning its own.</summary>
    public static ClipRect? DefineClips<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series)
        where TCanvas : struct, IChartCanvas
    {
        if (SeriesClip(layout, series) is not { } clip)
        {
            return null;
        }

        canvas.DefineClip(clip);
        if (!series.Spec.ClipDot && series.Spec.Kind is SeriesKind.Line or SeriesKind.Area)
        {
            canvas.DefineClip(DotsClip(layout, series, clip)!.Value);
        }

        return clip;
    }

    /// <summary>The clip of a line's or an area's dots: the series' own, or with <c>clipDot</c> off the plot grown by Recharts' default dot (a radius of 3 and a stroke of 2).</summary>
    private static ClipRect? DotsClip(ChartLayout layout, SeriesLayout series, ClipRect? clip)
    {
        if (clip is null || series.Spec.ClipDot)
        {
            return clip;
        }

        const double size = (3 * 2) + 2;
        return new ClipRect(layout.Left - (size / 2), layout.Top - (size / 2), layout.PlotWidth + size, layout.PlotHeight + size);
    }

    /// <summary>Clips the next layer or shape to a clip, when there is one.</summary>
    public static void Clip<TCanvas>(ref TCanvas canvas, ClipRect? clip)
        where TCanvas : struct, IChartCanvas
    {
        if (clip is { } rect)
        {
            canvas.ClipNext(rect);
        }
    }

    /// <summary>Opens a series' layer, animated when the series animates in; its timing, or null.</summary>
    public static Timing? OpenSeries<TCanvas>(ref TCanvas canvas, SeriesLayout series, ReadOnlySpan<byte> classes, MorphKind morph = MorphKind.None, byte curve = 0, bool ratio = false)
        where TCanvas : struct, IChartCanvas
    {
        classes = ClassNames.With(classes, series.Spec.ClassName);
        var timing = Animations.Of(series);
        if (timing is { } value)
        {
            if (morph != MorphKind.None)
            {
                canvas.MorphLayer(new MorphLayer(morph, curve, ratio, value, series.Data.Key()));
            }

            canvas.OpenMotion(classes, value);
        }
        else
        {
            canvas.Open(classes);
        }

        return timing;
    }

    /// <summary>Declares the numbers the next shape moves by on the web, when it has some.</summary>
    public static void Morph<TCanvas>(ref TCanvas canvas, MorphMark? mark)
        where TCanvas : struct, IChartCanvas
    {
        if (mark is { } value)
        {
            canvas.Morph(value);
        }
    }

    /// <summary>A bar's rectangle, and where Recharts' update grows a bar with no previous one from: its height from 0 at its bottom edge (its width from 0 in a vertical chart).</summary>
    private static MorphMark BarMark(BarRect bar, bool vertical) => vertical
        ? MorphMark.Numbers(MorphKind.Rect, new Six(bar.X, bar.Y, bar.Width, bar.Height), 4, new Six(double.NaN, double.NaN, 0, double.NaN), 4)
        : MorphMark.Numbers(MorphKind.Rect, new Six(bar.X, bar.Y, bar.Width, bar.Height), 4, new Six(double.NaN, bar.Y + bar.Height, double.NaN, 0), 4);

    /// <summary>The edge a bar grows from: its base, below a positive bar and above a negative one (left and right in a vertical chart).</summary>
    private static StepKind Growth(BarRect bar, bool vertical) => vertical
        ? bar.Width >= 0 ? StepKind.GrowRight : StepKind.GrowLeft
        : bar.Height >= 0 ? StepKind.GrowUp : StepKind.GrowDown;

    /// <summary>Whether Recharts draws a bar's rectangle: it has a size and a position.</summary>
    public static bool IsDrawn(BarRect bar) => bar.Width != 0 && bar.Height != 0 && !double.IsNaN(bar.Y) && !double.IsNaN(bar.X);

    /// <summary>How a bar is painted: its cell's colors, or its series'.</summary>
    public static Paint BarPaint(SeriesLayout series, int index)
    {
        var spec = series.Spec;
        var cell = index < spec.Cells.Length ? spec.Cells[index] : null;
        var paint = new Paint(cell?.Stroke ?? spec.Stroke, spec.Stroke is null && cell?.Stroke is null && cell?.StrokeWidth is null ? double.NaN : cell?.StrokeWidth ?? spec.StrokeWidth, cell?.Fill ?? series.ItemFills?[index] ?? series.Fill, spec.FillOpacity ?? double.NaN, spec.StrokeDashArray);
        return Celled(ItemOpacity(series, paint, index), cell);
    }

    /// <summary>A shape's paint with its item's own opacity, for a series that names the member.</summary>
    public static Paint ItemOpacity(SeriesLayout series, in Paint paint, int index) =>
        series.Spec.OpacityKey is { } key && series.Data.Numbers(key)[index] is var opacity and not double.NaN ? paint with { Opacity = opacity } : paint;

    /// <summary>A shape's paint with its cell's other props (fill opacity, dashes, opacity) over its series'.</summary>
    public static Paint Celled(in Paint paint, CellSpec? cell) => cell is null
        ? paint
        : paint with { FillOpacity = cell.FillOpacity ?? paint.FillOpacity, Dash = cell.StrokeDashArray ?? paint.Dash, StrokeWidth = cell.StrokeWidth ?? paint.StrokeWidth, Opacity = cell.Opacity ?? paint.Opacity };

    private static void Backgrounds<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series, string background)
        where TCanvas : struct, IChartCanvas
    {
        var spec = series.Spec;
        var whole = background is not ("true" or "True");
        for (var i = 0; i < series.Bars.Length; i++)
        {
            var bar = series.Bars[i];
            var (x, y, width, height) = layout.Vertical ? (series.XAxis.X, bar.Y, series.XAxis.Width, bar.Height) : (bar.X, series.YAxis.Y, bar.Width, series.YAxis.Height);
            if (whole)
            {
                (width, height) = (BarTips.ParseInt(width), BarTips.ParseInt(height));
            }

            var active = i == spec.ActiveIndex;
            if (active)
            {
                canvas.Open("recharts-layer recharts-active-bar"u8);
            }

            if (!double.IsNaN(width) && !double.IsNaN(height) && width != 0 && height != 0)
            {
                canvas.Shape(new RectShape(x, y, width, height, spec.BackgroundRadius), Celled(new Paint(spec.BackgroundProps?.Stroke, double.NaN, layout.Spec.Colors.BackgroundOf(background)), spec.BackgroundProps), "recharts-rectangle recharts-bar-background-rectangle"u8);
            }

            if (active)
            {
                canvas.Close();
            }
        }
    }

    private static void BarLabels<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series)
        where TCanvas : struct, IChartCanvas
    {
        var list = 0;
        foreach (var label in new LabelLists(series.Spec.Labels, series.Spec.LabelLists))
        {
            var content = ContentProps.Of(label.Content, Content.ContentSlots.Label, series.Data);
            var template = ContentProps.Template(series.Spec.Order, 4 + list++);
            canvas.Open("recharts-layer recharts-label-list"u8);
            for (var i = 0; i < series.Bars.Length; i++)
            {
                if (Text(series, label, i) is not { Length: > 0 } text)
                {
                    continue;
                }

                var bar = series.Bars[i];
                var cell = i < series.Spec.Cells.Length ? series.Spec.Cells[i] : null;
                LabelPainter.Cartesian(ref canvas, label, text, (bar.X, bar.Y, bar.Width, bar.Height), layout.Spec.FontSize, cell?.Fill ?? layout.Spec.Colors.LabelText, cell?.Stroke, bar.Width, content: ContentProps.Label(content, template, i, bar.Value, series.Spec.Name ?? series.Spec.DataKey));
            }

            canvas.Close();
        }
    }
}
