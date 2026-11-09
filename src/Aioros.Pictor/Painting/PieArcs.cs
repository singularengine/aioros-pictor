using Aioros.Pictor.Layout;
using Aioros.Pictor.Motion;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>
/// The arcs that grow a pie's sectors as Recharts' enter animation grows them (each from 0 to its angle, packed from the
/// first sector's start angle with the padding before every sector but the first), shown while the pie animates in; the
/// sectors themselves wait for the end. Neighbouring sectors of one color without padding grow as one arc (their union
/// grows the same way), and the white edges come after every arc, each boundary once: an edge drawn between arcs makes
/// Firefox paint every wide arc again in each frame.
/// </summary>
internal static class PieArcs
{
    public static void Write<TCanvas>(ref TCanvas canvas, PieLayout pie, ChartSpec chart, int offset)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Animate(new Step(StepKind.Shown));
        canvas.Open(default);
        var marked = Toggles.Marked(chart);
        var joined = !marked && pie.Spec.PaddingAngle == 0;
        GrowingArc? run = null;
        var previous = false;
        var swept = 0.0;
        for (var i = 0; i < pie.Sectors.Length; i++)
        {
            var drawn = !Toggles.Concealed(chart, offset + i);
            var arc = Arc(pie, i, swept);
            swept += arc.Length;
            if (!drawn)
            {
                previous = false;
                continue;
            }

            if (marked)
            {
                Toggles.Open(ref canvas, chart, offset + i);
                canvas.Grow(arc);
                canvas.Close();
            }
            else if (joined && previous && run is { } open && open.Color == arc.Color)
            {
                run = open with { Length = open.Length + arc.Length };
            }
            else
            {
                Grow(ref canvas, run);
                run = arc;
            }

            previous = true;
        }

        Grow(ref canvas, run);
        Edges(ref canvas, pie, chart, offset, joined);
        canvas.Close();
    }

    private static void Edges<TCanvas>(ref TCanvas canvas, PieLayout pie, ChartSpec chart, int offset, bool joined)
        where TCanvas : struct, IChartCanvas
    {
        string? previous = null;
        var swept = 0.0;
        for (var i = 0; i < pie.Sectors.Length; i++)
        {
            var arc = Arc(pie, i, swept);
            swept += arc.Length;
            if (Toggles.Concealed(chart, offset + i) || arc.Stroke is null or "none")
            {
                previous = null;
                continue;
            }

            var marked = Toggles.Open(ref canvas, chart, offset + i);
            canvas.Grow(arc with { Part = joined && previous == arc.Stroke ? ArcPart.TrailingEdge : ArcPart.Edges });
            if (marked)
            {
                canvas.Close();
            }

            previous = arc.Stroke;
        }
    }

    private static void Grow<TCanvas>(ref TCanvas canvas, GrowingArc? run)
        where TCanvas : struct, IChartCanvas
    {
        if (run is { } arc)
        {
            canvas.Grow(arc);
        }
    }

    private static GrowingArc Arc(PieLayout pie, int i, double swept)
    {
        var gap = i * Math.Abs(pie.Spec.PaddingAngle);
        var counterclockwise = pie.Spec.EndAngle >= pie.Spec.StartAngle;
        return new GrowingArc(pie.Cx, pie.Cy, (pie.InnerRadius + pie.OuterRadius) / 2, pie.OuterRadius - pie.InnerRadius, pie.Sectors[0].Shape.StartAngle, counterclockwise, gap, gap + swept, Span(pie, i), pie.Items[i].Fill, pie.Items[i].Stroke);
    }

    private static double Span(PieLayout pie, int i) => Math.Abs(pie.Sectors[i].Shape.EndAngle - pie.Sectors[i].Shape.StartAngle);
}
