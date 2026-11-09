using Aioros.Pictor.Layout;
using Aioros.Pictor.Theming;

namespace Aioros.Pictor.Painting;

/// <summary>Paints Recharts' <c>Brush</c> as it renders at rest: its background, the slide between the travellers, and the two travellers.</summary>
internal static class BrushPainter
{
    public static void Paint<TCanvas>(ref TCanvas canvas, BrushLayout? brush, double fontSize, ChartTheme theme, Data.ChartData whole)
        where TCanvas : struct, IChartCanvas
    {
        if (brush is null || brush.Width <= 0 || brush.Height <= 0)
        {
            return;
        }

        var spec = brush.Spec;
        var stroke = spec.Stroke ?? theme.BrushStroke;
        canvas.Open("recharts-layer recharts-brush"u8);
        canvas.Rect(brush.X, brush.Y, brush.Width, brush.Height, new Paint(stroke, double.NaN, spec.Fill ?? theme.BrushFill), default);
        if (brush.Panorama is { } panorama)
        {
            canvas.OpenSurface(brush.X, brush.Y, brush.Width, brush.Height);
            ChartPainter.PaintCompact(ref canvas, panorama);
            canvas.CloseSurface();
        }

        var slideX = Math.Min(brush.StartX, brush.EndX) + spec.TravellerWidth;
        var slideWidth = Math.Max(Math.Abs(brush.EndX - brush.StartX) - spec.TravellerWidth, 0);
        canvas.Rect(slideX, brush.Y, slideWidth, brush.Height, new Paint("none", double.NaN, stroke, 0.2), "recharts-brush-slide"u8);
        var fields = spec.Fields == true;
        var content = ContentProps.Of(spec.TravellerContent, Content.ContentSlots.Traveller, null);
        Traveller(ref canvas, brush, brush.StartX, (stroke, spec.Fill ?? theme.BrushFill, theme.BrushTraveller), fields, content, 0);
        Traveller(ref canvas, brush, brush.EndX, (stroke, spec.Fill ?? theme.BrushFill, theme.BrushTraveller), fields, content, 1);
        if (brush.Texts is { } texts)
        {
            var y = brush.Y + (brush.Height / 2);
            canvas.Open("recharts-layer recharts-brush-texts"u8);
            canvas.Text(Math.Min(brush.StartX, brush.EndX) - 5, y, texts.Start, new TextPaint(TextAnchor.End, LabelPlacement.Middle, stroke, fontSize), "recharts-text"u8);
            canvas.Text(Math.Max(brush.StartX, brush.EndX) + spec.TravellerWidth + 5, y, texts.End, new TextPaint(TextAnchor.Start, LabelPlacement.Middle, stroke, fontSize), "recharts-text"u8);
            canvas.Close();
        }

        if (fields)
        {
            canvas.BrushFields(brush, brush.TextAt(whole, brush.StartIndex), brush.TextAt(whole, brush.EndIndex));
        }

        canvas.Close();
    }

    /// <summary>A traveller: Recharts' named slider group, or a drawn group when the web brush's range inputs are the sliders.</summary>
    private static void Traveller<TCanvas>(ref TCanvas canvas, BrushLayout brush, double travellerX, (string Stroke, string Fill, string Inner) paints, bool fields, Content.ContentValues? content, int index)
        where TCanvas : struct, IChartCanvas
    {
        var spec = brush.Spec;
        var (stroke, inner) = (paints.Stroke, paints.Inner);
        var x = Math.Max(travellerX, brush.X);
        var lineY = Math.Floor(brush.Y + (brush.Height / 2)) - 1;
        var line = Painting.Paint.Stroked(inner);
        if (fields)
        {
            canvas.Open("recharts-layer recharts-brush-traveller"u8);
        }
        else
        {
            canvas.OpenSlider("recharts-layer recharts-brush-traveller"u8, brush.AriaLabel, travellerX);
        }

        if (content is not null)
        {
            canvas.Content(content.Spec, ContentProps.Traveller(content, (x, brush.Y, spec.TravellerWidth, brush.Height), paints.Fill, stroke, index));
            canvas.Close();
            return;
        }

        canvas.Rect(x, brush.Y, spec.TravellerWidth, brush.Height, new Paint("none", double.NaN, stroke), default);
        canvas.Line(x + 1, lineY, x + spec.TravellerWidth - 1, lineY, line, default);
        canvas.Line(x + 1, lineY + 2, x + spec.TravellerWidth - 1, lineY + 2, line, default);
        canvas.Close();
    }
}
