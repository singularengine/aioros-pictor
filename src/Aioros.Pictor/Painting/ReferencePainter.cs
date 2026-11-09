using System.Globalization;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>
/// Recharts' <c>ReferenceLine</c>, <c>ReferenceArea</c> and <c>ReferenceDot</c>: values mapped through their axes' scales
/// (a category at its band's start, middle or end), then by <c>ifOverflow</c> dropped when they fall outside the plot
/// (<c>discard</c>), clipped to the plot (<c>hidden</c>) or drawn where they fall (<c>visible</c>, <c>extendDomain</c>).
/// </summary>
internal static class ReferencePainter
{
    /// <summary>The plot as a clip, Recharts' chart-wide <c>clipPath</c>, defined once when a reference is hidden by it.</summary>
    public static ClipRect PlotClip(ChartLayout layout) => new(layout.Left, layout.Top, layout.PlotWidth, layout.PlotHeight);

    public static void DefinePlotClip<TCanvas>(ref TCanvas canvas, ChartLayout layout)
        where TCanvas : struct, IChartCanvas
    {
        var spec = layout.Spec;
        foreach (var line in spec.ReferenceLines)
        {
            if (line.IfOverflow == OverflowKind.Hidden)
            {
                canvas.DefineClip(PlotClip(layout));
                return;
            }
        }

        foreach (var area in spec.ReferenceAreas)
        {
            if (area.IfOverflow == OverflowKind.Hidden)
            {
                canvas.DefineClip(PlotClip(layout));
                return;
            }
        }

        foreach (var dot in spec.ReferenceDots)
        {
            if (dot.IfOverflow == OverflowKind.Hidden)
            {
                canvas.DefineClip(PlotClip(layout));
                return;
            }
        }
    }

    public static void Line<TCanvas>(ref TCanvas canvas, ChartLayout layout, ReferenceLineSpec reference)
        where TCanvas : struct, IChartCanvas
    {
        if (EndPoints(layout, reference) is not { } segment)
        {
            return;
        }

        canvas.Open("recharts-layer recharts-reference-line"u8);
        Hide(ref canvas, layout, reference.IfOverflow);
        var linePaint = new Paint(reference.Stroke ?? layout.Spec.Colors.ReferenceLineStroke, reference.StrokeWidth, "none", 1, reference.StrokeDashArray);
        if (ContentProps.Of(reference.ShapeContent, Content.ContentSlots.ReferenceLine, null) is { } content)
        {
            canvas.Content(content.Spec, ContentProps.ReferenceLine(content, segment.X1, segment.Y1, segment.X2, segment.Y2, linePaint, reference.Order));
        }
        else
        {
            canvas.Line(segment.X1, segment.Y1, segment.X2, segment.Y2, linePaint, "recharts-reference-line-line"u8);
        }
        ChartPainter.Labels(ref canvas, reference.Label, reference.Labels, (Math.Min(segment.X1, segment.X2), Math.Min(segment.Y1, segment.Y2), Math.Abs(segment.X2 - segment.X1), Math.Abs(segment.Y2 - segment.Y1)), layout.Spec.FontSize, layout.Spec.Colors.LabelText, reference.Order);

        canvas.Close();
    }

    /// <summary>Recharts' <c>getEndPoints</c>: a horizontal line at y, a vertical one at x, or a segment, ordered as Recharts orders them for the axes' sides.</summary>
    private static (double X1, double Y1, double X2, double Y2)? EndPoints(ChartLayout layout, ReferenceLineSpec reference)
    {
        var x = layout.XAxis(reference.XAxisId);
        var y = layout.YAxis(reference.YAxisId);
        var share = Share(reference.Position);
        var discard = reference.IfOverflow == OverflowKind.Discard;
        var (left, top, right, bottom) = (layout.Left, layout.Top, layout.Left + layout.PlotWidth, layout.Top + layout.PlotHeight);
        if (reference.Y is { } yText)
        {
            var coordinate = Apply(y, yText, share);
            return Drops(y, coordinate, discard) ? null
                : y.Spec.Orientation == AxisOrientation.Left ? (left, coordinate, right, coordinate) : (right, coordinate, left, coordinate);
        }

        if (reference.X is { } xText)
        {
            var coordinate = Apply(x, xText, share);
            return Drops(x, coordinate, discard) ? null
                : x.Spec.Orientation == AxisOrientation.Top ? (coordinate, top, coordinate, bottom) : (coordinate, bottom, coordinate, top);
        }

        return reference.Segment is { } segment ? Segment(x, y, segment, share, discard) : null;
    }

    private static (double X1, double Y1, double X2, double Y2)? Segment(AxisLayout x, AxisLayout y, ReferenceSegment segment, double share, bool discard)
    {
        if (segment is not { X1: { } x1, Y1: { } y1, X2: { } x2, Y2: { } y2 })
        {
            return null;
        }

        var points = (Apply(x, x1, share), Apply(y, y1, share), Apply(x, x2, share), Apply(y, y2, share));
        return Drops(x, points.Item1, discard) || Drops(y, points.Item2, discard) || Drops(x, points.Item3, discard) || Drops(y, points.Item4, discard) ? null : points;
    }

    /// <summary>Whether a coordinate leaves its element out: it has none, or it falls outside the plot and the element is discarded.</summary>
    private static bool Drops(AxisLayout axis, double coordinate, bool discard) => double.IsNaN(coordinate) || (discard && !InRange(axis, coordinate));

    private static double Share(BandPosition position) => position switch
    {
        BandPosition.Start => 0,
        BandPosition.End => 1,
        _ => 0.5,
    };

    public static void Area<TCanvas>(ref TCanvas canvas, ChartLayout layout, ReferenceAreaSpec area)
        where TCanvas : struct, IChartCanvas
    {
        if ((area.X1 ?? area.X2 ?? area.Y1 ?? area.Y2) is null)
        {
            return;
        }

        var x = layout.XAxis(area.XAxisId);
        var y = layout.YAxis(area.YAxisId);
        var x1 = area.X1 is { } first ? Apply(x, first, 0) : x.RangeStart;
        var y1 = area.Y1 is { } low ? Apply(y, low, 0) : y.RangeStart;
        var x2 = area.X2 is { } second ? Apply(x, second, 1) : x.RangeEnd;
        var y2 = area.Y2 is { } high ? Apply(y, high, 1) : y.RangeEnd;
        if (area.IfOverflow == OverflowKind.Discard && !(InRange(x, x1) && InRange(y, y1) && InRange(x, x2) && InRange(y, y2)))
        {
            return;
        }

        var (left, top, width, height) = (Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x2 - x1), Math.Abs(y2 - y1));
        canvas.Open("recharts-layer recharts-reference-area"u8);
        Hide(ref canvas, layout, area.IfOverflow);
        var areaPaint = new Paint(area.Stroke, area.StrokeWidth, area.Fill ?? layout.Spec.Colors.ReferenceAreaFill, area.FillOpacity, null, area.StrokeOpacity ?? double.NaN);
        if (ContentProps.Of(area.ShapeContent, Content.ContentSlots.ReferenceArea, null) is { } content)
        {
            canvas.Content(content.Spec, ContentProps.ReferenceArea(content, (left, top, width, height), areaPaint, area.Order));
        }
        else
        {
            canvas.Shape(new RectShape(left, top, width, height, area.Radius), areaPaint, "recharts-rectangle recharts-reference-area-rect"u8);
        }
        ChartPainter.Labels(ref canvas, area.Label, area.Labels, (left, top, width, height), layout.Spec.FontSize, layout.Spec.Colors.LabelText, area.Order);

        canvas.Close();
    }

    public static void Dot<TCanvas>(ref TCanvas canvas, ChartLayout layout, ReferenceDotSpec dot)
        where TCanvas : struct, IChartCanvas
    {
        if (dot.X is not { } xText || dot.Y is not { } yText)
        {
            return;
        }

        var x = layout.XAxis(dot.XAxisId);
        var y = layout.YAxis(dot.YAxisId);
        var cx = Apply(x, xText, 0.5);
        var cy = Apply(y, yText, 0.5);
        if (double.IsNaN(cx) || double.IsNaN(cy) || (dot.IfOverflow == OverflowKind.Discard && !(InRange(x, cx) && InRange(y, cy))))
        {
            return;
        }

        canvas.Open("recharts-layer recharts-reference-dot"u8);
        Hide(ref canvas, layout, dot.IfOverflow);
        var dotPaint = new Paint(dot.Stroke ?? layout.Spec.Colors.ReferenceDotStroke, dot.StrokeWidth, dot.Fill ?? layout.Spec.Colors.ReferenceDotFill, dot.FillOpacity);
        if (ContentProps.Of(dot.ShapeContent, Content.ContentSlots.ReferenceDot, null) is { } content)
        {
            canvas.Content(content.Spec, ContentProps.ReferenceDot(content, cx, cy, dot.R, dotPaint, dot.Order));
        }
        else
        {
            canvas.Circle(cx, cy, dot.R, dotPaint, "recharts-dot recharts-reference-dot-dot"u8);
        }
        ChartPainter.Labels(ref canvas, dot.Label, dot.Labels, (cx - dot.R, cy - dot.R, 2 * dot.R, 2 * dot.R), layout.Spec.FontSize, layout.Spec.Colors.LabelText, dot.Order);

        canvas.Close();
    }

    /// <summary>Clips the reference's shape (not its label) to the plot when it overflows hidden.</summary>
    private static void Hide<TCanvas>(ref TCanvas canvas, ChartLayout layout, OverflowKind overflow)
        where TCanvas : struct, IChartCanvas
    {
        if (overflow == OverflowKind.Hidden)
        {
            canvas.ClipNext(PlotClip(layout));
        }
    }

    /// <summary>A value through an axis' scale, plus a share of a category's band (0 at its start, 0.5 at its middle, 1 at its end).</summary>
    private static double Apply(AxisLayout axis, string text, double band)
    {
        if (axis.IsCategory)
        {
            var index = axis.CategoryIndex(text);
            return index < 0 ? double.NaN : axis.Category(index) + (axis.Bandwidth * band);
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? axis.Map(value) : double.NaN;
    }

    private static bool InRange(AxisLayout axis, double coordinate) =>
        coordinate >= Math.Min(axis.RangeStart, axis.RangeEnd) && coordinate <= Math.Max(axis.RangeStart, axis.RangeEnd);
}
