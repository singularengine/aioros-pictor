using System.Buffers;
using Aioros.Graphs.Svg;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>The SVG writer's text elements, text attributes, arc text and shape paints.</summary>
internal static class ElementMarkup
{
    /// <summary>A label's own size, weight and stroke.</summary>
    public static void Style(MarkupBuffer output, in TextPaint paint)
    {
        if (paint.OwnSize)
        {
            Attribute(output, " font-size"u8, paint.FontSize);
        }

        if (paint.FontWeight is { } weight)
        {
            Attribute(output, " font-weight"u8, weight);
        }

        if (paint.FontStyle is { } style)
        {
            Attribute(output, " font-style"u8, style);
        }

        if (!double.IsNaN(paint.Opacity))
        {
            Attribute(output, " opacity"u8, paint.Opacity);
        }

        if (paint.Stroke is { } stroke)
        {
            Attribute(output, " stroke"u8, stroke);
        }

        if (!double.IsNaN(paint.StrokeWidth))
        {
            Attribute(output, " stroke-width"u8, paint.StrokeWidth);
        }
    }

    /// <summary>Text along an arc (a sunburst label): its path in a <c>defs</c>, the text on it.</summary>
    public static void Arc(MarkupBuffer output, in ArcPath arc, string text, in TextPaint paint, ReadOnlySpan<byte> classes)
    {
        Raw(output, "<text"u8);
        Attribute(output, " fill"u8, paint.Fill);
        Style(output, paint);
        Raw(output, " dominant-baseline=\"central\""u8);
        if (!classes.IsEmpty)
        {
            Raw(output, " class=\""u8);
            Raw(output, classes);
            Raw(output, "\""u8);
        }

        Raw(output, "><defs><path id=\""u8);
        ArcId(output, arc);
        Raw(output, "\" d=\""u8);
        var (x0, y0) = Polar.Point(arc.Cx, arc.Cy, arc.Radius, arc.Angle);
        var (x1, y1) = Polar.Point(arc.Cx, arc.Cy, arc.Radius, arc.EndAngle);
        var sink = new SvgPathWriter(output);
        sink.MoveTo(x0, y0);
        sink.ArcTo(arc.Radius, true, !arc.Counterclockwise, x1, y1);
        Raw(output, "\"/></defs><textPath href=\"#"u8);
        ArcId(output, arc);
        Raw(output, "\">"u8);
        Utf8Markup.Text(output, text);
        Raw(output, "</textPath></text>"u8);
    }

    private static void ArcId(MarkupBuffer output, in ArcPath arc)
    {
        Raw(output, "pictor-arc-"u8);
        Number(output, arc.Cx);
        Raw(output, "-"u8);
        Number(output, arc.Cy);
        Raw(output, "-"u8);
        Number(output, arc.Radius);
        Raw(output, "-"u8);
        Number(output, arc.Angle);
        Raw(output, arc.Counterclockwise ? "-l"u8 : "-r"u8);
    }

    /// <summary>A text element: its position, classes, anchor, fill, own style, baseline and rotation, then one <c>tspan</c> per line.</summary>
    public static void Text(MarkupBuffer output, double x, double y, string text, in TextPaint paint, ReadOnlySpan<byte> classes)
    {
        Raw(output, "<text"u8);
        Attribute(output, " x"u8, x);
        Attribute(output, " y"u8, y);
        if (!classes.IsEmpty)
        {
            Raw(output, " class=\""u8);
            Raw(output, classes);
            Raw(output, "\""u8);
        }
        Raw(output, paint.Anchor switch
        {
            TextAnchor.Start => " text-anchor=\"start\""u8,
            TextAnchor.End => " text-anchor=\"end\""u8,
            _ => " text-anchor=\"middle\""u8,
        });
        Attribute(output, " fill"u8, paint.Fill);
        ElementMarkup.Style(output, paint);
        if (paint.MiddleBaseline)
        {
            Raw(output, " alignment-baseline=\"middle\""u8);
        }

        if (paint.Angle != 0)
        {
            Raw(output, " transform=\"rotate("u8);
            Number(output, paint.Angle);
            Raw(output, ", "u8);
            Number(output, x);
            Raw(output, ", "u8);
            Number(output, y);
            Raw(output, ")\""u8);
        }

        Raw(output, ">"u8);
        var dy = paint.Dy;
        foreach (var line in text.AsSpan().EnumerateLines())
        {
            Raw(output, "<tspan"u8);
            Attribute(output, " x"u8, x);
            Raw(output, " dy=\""u8);
            Number(output, dy);
            Raw(output, "em\">"u8);
            Utf8Markup.Text(output, line);
            Raw(output, "</tspan>"u8);
            dy = 1;
        }

        Raw(output, "</text>"u8);
    }

    /// <summary>A shape's paint attributes: stroke, width, opacity, dashes, fill and opacities, a gradient named for the chart's layout.</summary>
    public static void Paint(MarkupBuffer output, in Paint paint, string? variant)
    {
        if (paint.Stroke is { } stroke)
        {
            Attribute(output, " stroke"u8, GradientMarkup.Paint(stroke, variant));
        }

        if (!double.IsNaN(paint.StrokeWidth))
        {
            Attribute(output, " stroke-width"u8, paint.StrokeWidth);
        }

        if (!double.IsNaN(paint.StrokeOpacity))
        {
            Attribute(output, " stroke-opacity"u8, paint.StrokeOpacity);
        }

        if (paint.Dash is { } dash)
        {
            Attribute(output, " stroke-dasharray"u8, dash);
        }

        if (paint.Fill is { } fill)
        {
            Attribute(output, " fill"u8, GradientMarkup.Paint(fill, variant));
        }

        if (!double.IsNaN(paint.FillOpacity))
        {
            Attribute(output, " fill-opacity"u8, paint.FillOpacity);
        }

        if (!double.IsNaN(paint.Opacity))
        {
            Attribute(output, " opacity"u8, paint.Opacity);
        }

        if (paint.LineCap is { } cap)
        {
            Attribute(output, " stroke-linecap"u8, cap);
        }

        if (paint.LineJoin is { } join)
        {
            Attribute(output, " stroke-linejoin"u8, join);
        }

        if (!double.IsNaN(paint.MiterLimit))
        {
            Attribute(output, " stroke-miterlimit"u8, paint.MiterLimit);
        }
    }
}
