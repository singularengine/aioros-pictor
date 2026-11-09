using System.Collections.Immutable;
using System.Globalization;
using Aioros.Graphs.Avalonia;
using Aioros.Pictor.Content;
using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Media;

namespace Aioros.Pictor.Drawing;

/// <summary>
/// Draws custom content on the desktop for one item, as the web writes it: each node's element with its holes filled,
/// SVG's semantics kept (a missing fill is black, a nested <c>svg</c> maps its view box to its box and clips to it, a
/// text sits on its baseline at its anchor, antialiased in gray as browsers draw SVG text).
/// </summary>
internal static class ContentDrawing
{
    public static void Draw(DrawingContext context, ChartPaints paints, ImmutableArray<ContentNode> nodes, in ContentItem item)
    {
        foreach (var node in nodes)
        {
            if (item.Shows(node))
            {
                Node(context, paints, node, item);
            }
        }
    }

    private static void Node(DrawingContext context, ChartPaints paints, ContentNode node, in ContentItem item)
    {
        using var transform = Transform(context, node, item);
        using var opacity = Number(node, "opacity", item, 1) is var value && value < 1 ? context.PushOpacity(value) : default(DrawingContext.PushedState?);
        switch (node.Element)
        {
            case ContentElement.Group:
                Draw(context, paints, node.Children, item);
                break;
            case ContentElement.Svg:
                Svg(context, paints, node, item);
                break;
            case ContentElement.Text:
                Text(context, paints, node, item);
                break;
            default:
                Shape(context, paints, node, item);
                break;
        }
    }

    private static DrawingContext.PushedState? Transform(DrawingContext context, ContentNode node, in ContentItem item)
    {
        if (Find(node, ContentNames.Transform) is not { } prop)
        {
            return null;
        }

        var matrix = Matrix.Identity;
        var values = new double[prop.Numbers.Length];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = item.Evaluate(prop.Numbers[i]);
        }

        var n = 0;
        foreach (var operation in prop.Text ?? string.Empty)
        {
            var at = n;
            n += ContentNames.TransformArity(operation);
            double V(int i) => at + i < values.Length ? values[at + i] : 0;
            var step = operation switch
            {
                't' => Matrix.CreateTranslation(V(0), V(1)),
                'r' => Matrix.CreateTranslation(-V(1), -V(2)) * Matrix.CreateRotation(V(0) * Math.PI / 180) * Matrix.CreateTranslation(V(1), V(2)),
                's' => Matrix.CreateScale(V(0), V(1)),
                _ => new Matrix(V(0), V(1), V(2), V(3), V(4), V(5)),
            };
            matrix = step * matrix;
        }

        return context.PushTransform(matrix);
    }

    private static void Svg(DrawingContext context, ChartPaints paints, ContentNode node, in ContentItem item)
    {
        var box = new Rect(Number(node, "x", item, 0), Number(node, "y", item, 0), Math.Max(Number(node, "width", item, 0), 0), Math.Max(Number(node, "height", item, 0), 0));
        var view = Find(node, "viewBox") is { Numbers.Length: 4 } prop
            ? new Rect(item.Evaluate(prop.Numbers[0]), item.Evaluate(prop.Numbers[1]), item.Evaluate(prop.Numbers[2]), item.Evaluate(prop.Numbers[3]))
            : new Rect(0, 0, box.Width, box.Height);
        using var clip = context.PushClip(box);
        var aspect = Find(node, "preserveAspectRatio") is { } ratio ? ContentMarkup.Value(ratio, item) : null;
        var (sx, sy) = (view.Width > 0 ? box.Width / view.Width : 1, view.Height > 0 ? box.Height / view.Height : 1);
        var (dx, dy) = (0.0, 0.0);
        if (aspect != "none")
        {
            var scale = aspect?.EndsWith("slice", StringComparison.Ordinal) == true ? Math.Max(sx, sy) : Math.Min(sx, sy);
            (dx, dy) = ((box.Width - (view.Width * scale)) / 2, (box.Height - (view.Height * scale)) / 2);
            (sx, sy) = (scale, scale);
        }

        using var mapped = context.PushTransform(Matrix.CreateTranslation(-view.X, -view.Y) * Matrix.CreateScale(sx, sy) * Matrix.CreateTranslation(box.X + dx, box.Y + dy));
        Draw(context, paints, node.Children, item);
    }

    private static void Shape(DrawingContext context, ChartPaints paints, ContentNode node, in ContentItem item)
    {
        var fill = Find(node, "fill") is { } fillProp ? ContentMarkup.Value(fillProp, item) : "#000";
        var brush = paints.Brush(fill, Number(node, "fill-opacity", item, double.NaN));
        var pen = Find(node, "stroke") is { } strokeProp
            ? paints.Pen(ContentMarkup.Value(strokeProp, item), Number(node, "stroke-width", item, 1), Find(node, "stroke-dasharray") is { } dash ? ContentMarkup.Value(dash, item) : null, Number(node, "stroke-opacity", item, double.NaN))
            : null;
        if (brush is null && pen is null)
        {
            return;
        }

        switch (node.Element)
        {
            case ContentElement.Rect:
                context.DrawRectangle(brush, pen, new Rect(Number(node, "x", item, 0), Number(node, "y", item, 0), Math.Max(Number(node, "width", item, 0), 0), Math.Max(Number(node, "height", item, 0), 0)), Number(node, "rx", item, 0), Number(node, "ry", item, Number(node, "rx", item, 0)));
                break;
            case ContentElement.Circle or ContentElement.Dot:
                var r = Number(node, "r", item, 0);
                context.DrawEllipse(brush, pen, new Point(Number(node, "cx", item, 0), Number(node, "cy", item, 0)), r, r);
                break;
            case ContentElement.Ellipse:
                context.DrawEllipse(brush, pen, new Point(Number(node, "cx", item, 0), Number(node, "cy", item, 0)), Number(node, "rx", item, 0), Number(node, "ry", item, 0));
                break;
            case ContentElement.Line:
                if (pen is not null)
                {
                    context.DrawLine(pen, new Point(Number(node, "x1", item, 0), Number(node, "y1", item, 0)), new Point(Number(node, "x2", item, 0), Number(node, "y2", item, 0)));
                }

                break;
            default:
                if (Geometry(node, item) is { } geometry)
                {
                    context.DrawGeometry(brush, pen, geometry);
                }

                break;
        }
    }

    private static StreamGeometry? Geometry(ContentNode node, in ContentItem item)
    {
        var geometry = new StreamGeometry();
        using var stream = geometry.Open();
        if (node.Element is ContentElement.Path && Find(node, ContentNames.Path) is { } path)
        {
            PathData(stream, path, item);
        }
        else if (node.Element is ContentElement.Polyline or ContentElement.Polygon && Find(node, ContentNames.Points) is { } points)
        {
            Points(stream, points, item, node.Element == ContentElement.Polygon);
        }
        else if (Find(node, ContentNames.Shape) is { } shape)
        {
            Span<double> values = stackalloc double[Math.Min(shape.Numbers.Length, 16)];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = item.Evaluate(shape.Numbers[i]);
            }

            var sink = new GeometrySink(stream);
            ContentShapes.Write(ref sink, node.Element, values, shape.Text, shape.Member >= 0 ? item.Points : default);
            sink.Complete();
        }
        else
        {
            return null;
        }

        return geometry;
    }

    private static void PathData(StreamGeometryContext stream, ContentProp path, in ContentItem item)
    {
        var values = new double[path.Numbers.Length];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = item.Evaluate(path.Numbers[i]);
        }

        var n = 0;
        var open = false;
        foreach (var letter in path.Text ?? string.Empty)
        {
            var at = n;
            double V(int i) => at + i < values.Length ? values[at + i] : 0;
            open = Command(stream, letter, V, open);
            n += ContentNames.Arity(letter);
        }

        if (open)
        {
            stream.EndFigure(false);
        }
    }

    /// <summary>One path command drawn; whether a figure is open after it.</summary>
    private static bool Command(StreamGeometryContext stream, char letter, Func<int, double> v, bool open)
    {
        switch (letter)
        {
            case 'M':
                if (open)
                {
                    stream.EndFigure(false);
                }

                stream.BeginFigure(new Point(v(0), v(1)), true);
                return true;
            case 'L':
                stream.LineTo(new Point(v(0), v(1)));
                return open;
            case 'C':
                stream.CubicBezierTo(new Point(v(0), v(1)), new Point(v(2), v(3)), new Point(v(4), v(5)));
                return open;
            case 'Q':
                stream.QuadraticBezierTo(new Point(v(0), v(1)), new Point(v(2), v(3)));
                return open;
            case 'A':
                stream.ArcTo(new Point(v(5), v(6)), new Size(v(0), v(1)), v(2), v(3) > 0.5, v(4) > 0.5 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
                return open;
            case 'Z':
                stream.EndFigure(true);
                return false;
            default:
                return open;
        }
    }

    private static void Points(StreamGeometryContext stream, ContentProp prop, in ContentItem item, bool closed)
    {
        var points = item.Points;
        var count = prop.Member >= 0 ? points.Length / 2 : prop.Numbers.Length / 2;
        for (var i = 0; i < count; i++)
        {
            var point = prop.Member >= 0 ? new Point(points[2 * i], points[(2 * i) + 1]) : new Point(item.Evaluate(prop.Numbers[2 * i]), item.Evaluate(prop.Numbers[(2 * i) + 1]));
            if (i == 0)
            {
                stream.BeginFigure(point, closed);
            }
            else
            {
                stream.LineTo(point);
            }
        }

        if (count > 0)
        {
            stream.EndFigure(closed);
        }
    }

    private static void Text(DrawingContext context, ChartPaints paints, ContentNode node, in ContentItem item)
    {
        var text = Find(node, ContentNames.Content) is { } content ? ContentMarkup.Value(content, item) ?? string.Empty : string.Empty;
        if (text.Length == 0)
        {
            return;
        }

        var size = Number(node, "font-size", item, paints.FontSize);
        var fill = Find(node, "fill") is { } fillProp ? ContentMarkup.Value(fillProp, item) ?? "#000" : "#000";
        var weight = Find(node, "font-weight") is { } weightProp ? ContentMarkup.Value(weightProp, item) : null;
        var formatted = paints.Text(text, size, fill, null, weight);
        var anchor = Find(node, "text-anchor") is { } anchorProp ? ContentMarkup.Value(anchorProp, item) : null;
        var x = Number(node, "x", item, 0) + Number(node, "dx", item, 0);
        var y = Number(node, "y", item, 0) + Number(node, "dy", item, 0);
        var left = anchor switch
        {
            "middle" => x - (formatted.WidthIncludingTrailingWhitespace / 2),
            "end" => x - formatted.WidthIncludingTrailingWhitespace,
            _ => x,
        };
        var origin = new Point(left, y - formatted.Baseline);
        var stroke = Find(node, "stroke") is { } strokeProp ? paints.Pen(ContentMarkup.Value(strokeProp, item), Number(node, "stroke-width", item, 1), null, Number(node, "stroke-opacity", item, double.NaN)) : null;
        if (stroke is not null && formatted.BuildGeometry(origin) is { } outline)
        {
            context.DrawGeometry(paints.Brush(fill, Number(node, "fill-opacity", item, double.NaN)), stroke, outline);
            return;
        }

        var opacity = Number(node, "fill-opacity", item, 1);
        using var faded = opacity < 1 ? context.PushOpacity(opacity) : default(DrawingContext.PushedState?);
        using var grayscale = context.PushTextOptions(new TextOptions { TextRenderingMode = TextRenderingMode.Antialias });
        context.DrawText(formatted, origin);
    }

    private static ContentProp? Find(ContentNode node, string name)
    {
        foreach (var prop in node.Attributes)
        {
            if (prop.Name == name)
            {
                return prop;
            }
        }

        return null;
    }

    private static double Number(ContentNode node, string name, in ContentItem item, double fallback) =>
        Find(node, name) is { Numbers.Length: > 0 } prop ? item.Evaluate(prop.Numbers[0]) : fallback;

    /// <summary>Draws HTML content's boxes: a division's shadow, background and border, a paragraph's text on its line in its font, at its box's end when it aligns right.</summary>
    public static void Html(DrawingContext context, ChartPaints paints, List<HtmlBox> boxes, in ContentItem item, string color = "#000")
    {
        foreach (var box in boxes)
        {
            if (box.Text is null)
            {
                Division(context, paints, box, item);
            }
            else
            {
                Paragraph(context, paints, box, item, color);
            }
        }
    }

    /// <summary>Draws a tooltip's content's boxes, then its lines down its items node's box.</summary>
    public static void Html(DrawingContext context, ChartPaints paints, List<HtmlBox> boxes, in ContentItem item, Painting.TooltipLines? lines)
    {
        Html(context, paints, boxes, item);
        if (lines is null)
        {
            return;
        }

        foreach (var box in boxes.Where(static box => box.Node.Element == ContentElement.Items))
        {
            var top = box.Y;
            for (var i = 0; i < lines.Count; i++)
            {
                var lineBoxes = lines.Arrange(i, box.X, top, box.Width, out var line, out top);
                Html(context, paints, lineBoxes, line);
            }
        }
    }

    /// <summary>A border's dashes as Chromium draws a <c>dashed</c> (dashes and gaps three widths long) or <c>dotted</c> (one width) border; none for a solid one.</summary>
    private static string? Dashes(string? style, double width) => style switch
    {
        "dashed" => string.Create(CultureInfo.InvariantCulture, $"{3 * width} {3 * width}"),
        "dotted" => string.Create(CultureInfo.InvariantCulture, $"{width} {width}"),
        _ => null,
    };

    private static void Division(DrawingContext context, ChartPaints paints, in HtmlBox box, in ContentItem item)
    {
        var node = box.Node;
        var background = Find(node, "background") is { } fill ? paints.Brush(ContentMarkup.Value(fill, item)) : null;
        var width = Number(node, "border-width", item, 0);
        var pen = width > 0 && Find(node, "border-color") is { } border ? paints.Pen(ContentMarkup.Value(border, item), width, Dashes(Find(node, "border-style") is { } style ? ContentMarkup.Value(style, item) : null, width)) : null;
        var radius = Number(node, "border-radius", item, 0);
        if (Find(node, "box-shadow") is { } shadow && ContentMarkup.Value(shadow, item) is { } css && HtmlContent.IsShadow(css))
        {
            context.DrawRectangle(Brushes.Transparent, null, new RoundedRect(new Rect(box.X, box.Y, box.Width, box.Height), radius), Shadows(css));
        }

        if (background is not null || pen is not null)
        {
            context.DrawRectangle(background, pen, new Rect(box.X + (width / 2), box.Y + (width / 2), Math.Max(box.Width - width, 0), Math.Max(box.Height - width, 0)), radius, radius);
        }
    }

    /// <summary>CSS's shadows as Avalonia's: each one's offsets, blur, spread and color.</summary>
    private static BoxShadows Shadows(string css)
    {
        var shadows = css.Split(", ").Select(static part =>
        {
            var tokens = part.Split(' ');
            var inset = tokens[0] == "inset";
            var values = inset ? tokens[1..] : tokens;
            double Length(int i) => double.Parse(values[i].EndsWith("px", StringComparison.Ordinal) ? values[i][..^2] : values[i], System.Globalization.CultureInfo.InvariantCulture);
            return new BoxShadow { IsInset = inset, OffsetX = Length(0), OffsetY = Length(1), Blur = Length(2), Spread = Length(3), Color = Color.TryParse(values[4], out var color) ? color : Colors.Transparent };
        }).ToArray();
        return shadows.Length == 1 ? new BoxShadows(shadows[0]) : new BoxShadows(shadows[0], shadows[1..]);
    }

    private static void Paragraph(DrawingContext context, ChartPaints paints, in HtmlBox box, in ContentItem item, string inherited)
    {
        var node = box.Node;
        var color = Find(node, "color") is { } paint ? ContentMarkup.Value(paint, item) ?? inherited : inherited;
        var family = Find(node, "font-family") is { } font ? ContentMarkup.Value(font, item) : null;
        var formatted = paints.Text(box.Text!, box.FontSize, color, family, Find(node, "font-weight") is { } weight ? ContentMarkup.Value(weight, item) : null);
        var right = Find(node, "h-align") is { } align && ContentMarkup.Value(align, item) == "right";
        using var grayscale = context.PushTextOptions(new TextOptions { TextRenderingMode = TextRenderingMode.Antialias });
        context.DrawText(formatted, new Point(right ? box.X + box.Width - formatted.Width : box.X, box.Y + ((box.Height - formatted.Height) / 2)));
    }
}
