using Aioros.Graphs.Avalonia;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Media;

namespace Aioros.Pictor.Drawing;

/// <summary>
/// The desktop's canvas: the painter's calls drawn through Avalonia's <see cref="DrawingContext"/>, every path built by
/// Aioros.Graphs into one <see cref="StreamGeometry"/> through its <see cref="GeometrySink"/>, texts placed on SVG's
/// baseline with the same anchors and shifts as the web's.
/// </summary>
internal readonly struct AvaloniaCanvas(DrawingContext context, ChartPaints paints, MotionFrame? motion = null, bool tooltips = true) : IChartCanvas
{
    private const double TooltipPadding = 10;

    public void Begin(IChartPlot plot)
    {
        paints.Clips.Reset();
        paints.Define(plot.Spec.Gradients);
    }

    public void Open(ReadOnlySpan<byte> classes)
    {
        paints.Clips.Open(context);
        motion?.Open(context, classes);
    }

    public void OpenSlider(ReadOnlySpan<byte> classes, string label, double value) => Open(classes);

    public void OpenFaded(ReadOnlySpan<byte> classes, double opacity)
    {
        paints.Clips.OpenFaded(context, opacity);
        motion?.Open(context, classes);
    }

    public void OpenMotion(ReadOnlySpan<byte> classes, in Motion.Timing timing)
    {
        paints.Clips.Open(context);
        motion?.OpenSeries(context, timing);
    }

    public void Animate(in Motion.Step step) => motion?.Animate(step);

    public readonly void MorphLayer(in Motion.MorphLayer layer)
    {
    }

    public readonly void Morph(in Motion.MorphMark mark)
    {
    }

    public void Grow(in Motion.GrowingArc arc)
    {
    }

    public void Close()
    {
        motion?.Close();
        paints.Clips.Close();
    }

    public void DefineClip(in ClipRect clip)
    {
    }

    public void OpenSurface(double x, double y, double width, double height) => paints.Clips.OpenSurface(context, new Rect(x, y, Math.Max(width, 0), Math.Max(height, 0)));

    public void CloseSurface() => paints.Clips.CloseSurface();

    public void BrushFields(BrushLayout brush, string startText, string endText)
    {
    }

    public void ClipNext(in ClipRect clip) => paints.Clips.Next(new Rect(clip.X, clip.Y, Math.Max(clip.Width, 0), Math.Max(clip.Height, 0)));

    /// <summary>
    /// Custom content for one item: its compiled nodes drawn with the item's values, or, for a template the compiler
    /// refused, what Avalonia lays out from the template for this item; it moves as the default shape would while its
    /// series animates in (a bar's content grows from the bar's base, as Recharts' animation grows the bar it is given).
    /// </summary>
    public void Content(ContentSpec content, in Content.ContentItem item)
    {
        var step = motion?.Take() ?? default;
        using var clip = paints.Clips.Element(context);
        if (Hidden)
        {
            return;
        }

        var nodes = content.Refused is null ? content.Nodes : paints.Resolve?.Invoke(content, item) ?? [];
        IDisposable? moved = null;
        if (motion is not null && step.Kind != Motion.StepKind.None && !motion.Push(context, step, new Rect(item.Box.X, item.Box.Y, item.Box.Width, item.Box.Height).Normalize(), out moved))
        {
            return;
        }

        using (moved)
        {
            ContentDrawing.Draw(context, paints, nodes, item);
        }
    }

    /// <summary>A tooltip's or a legend's custom content: its HTML boxes laid out as the browser lays them out, drawn from a point.</summary>
    public void Html(double x, double y, ContentSpec content, in Content.ContentItem item, TooltipSpec tooltip, in TipFollow follow, TooltipLines? lines)
    {
        if (!tooltips || Hidden)
        {
            return;
        }

        var nodes = content.Refused is null ? content.Nodes : paints.Resolve?.Invoke(content, item) ?? [];
        ContentDrawing.Html(context, paints, Pictor.Content.HtmlContent.Arrange(nodes, item, paints.FontSize, x, y, lines), item, lines);
    }

    private bool Hidden => motion?.Hidden == true;

    public void Line(double x1, double y1, double x2, double y2, in Paint paint, ReadOnlySpan<byte> classes)
    {
        using var clip = paints.Clips.Element(context);
        if (!Hidden && paints.Pen(paint) is { } pen)
        {
            using var faded = Faded(paint);
            context.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
        }
    }

    public void Shape<TShape>(in TShape shape, in Paint paint, ReadOnlySpan<byte> classes)
        where TShape : struct, IShape
    {
        var step = motion?.Take() ?? default;
        using var clip = paints.Clips.Element(context);
        var brush = paints.Brush(paint.Fill, paint.FillOpacity);
        var pen = paints.Pen(paint);
        if ((brush is null && pen is null) || Hidden)
        {
            return;
        }

        if (motion is not null && step.Kind is Motion.StepKind.GrowUp or Motion.StepKind.GrowDown or Motion.StepKind.GrowLeft or Motion.StepKind.GrowRight
            && shape is RectShape { Rounded: true } rounded)
        {
            GrownBar(rounded, brush, pen, step, paint);
            return;
        }

        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            var sink = new GeometrySink(geometryContext);
            shape.Write(ref sink);
            sink.Complete();
        }

        if (motion is null || step.Kind == Motion.StepKind.None)
        {
            using (Faded(paint))
            {
                context.DrawGeometry(brush, pen, geometry);
            }

            return;
        }

        Animated(geometry, brush, pen, step);
    }

    /// <summary>A rounded bar growing in, drawn again at its size of this frame as Recharts redraws its <c>Rectangle</c> (its corners never squashed).</summary>
    private void GrownBar(RectShape bar, IBrush? brush, IPen? pen, in Motion.Step step, in Paint paint)
    {
        var progress = motion!.Progress;
        if (progress <= 0)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            var sink = new GeometrySink(geometryContext);
            bar.Grown(step.Kind is Motion.StepKind.GrowLeft or Motion.StepKind.GrowRight, progress).Write(ref sink);
            sink.Complete();
        }

        using (Faded(paint))
        {
            context.DrawGeometry(brush, pen, geometry);
        }
    }

    private void Animated(StreamGeometry geometry, IBrush? brush, IPen? pen, in Motion.Step step)
    {
        if (step.Kind == Motion.StepKind.Stroke)
        {
            context.DrawGeometry(brush, null, geometry);
            var drawn = motion!.StrokeProgress(step.Offset > 0) * geometry.ContourLength;
            if (drawn >= geometry.ContourLength)
            {
                context.DrawGeometry(null, pen, geometry);
            }
            else if (drawn > 0 && geometry.TryGetSegment(0, drawn, true, out var stroked) && stroked is not null)
            {
                context.DrawGeometry(null, pen, stroked);
            }

            return;
        }

        if (step.Kind is Motion.StepKind.Draw or Motion.StepKind.DrawDashed)
        {
            var length = motion!.Progress * geometry.ContourLength;
            if (motion.Progress >= 1)
            {
                context.DrawGeometry(brush, pen, geometry);
            }
            else if (length > 0 && geometry.TryGetSegment(0, length, true, out var segment) && segment is not null)
            {
                context.DrawGeometry(null, pen, segment);
            }

            return;
        }

        if (!motion!.Push(context, step, geometry.Bounds, out var state))
        {
            return;
        }

        using (state)
        {
            context.DrawGeometry(brush, pen, geometry);
        }
    }

    /// <summary>A shape's own opacity (a cell's), pushed while it is drawn.</summary>
    private DrawingContext.PushedState? Faded(in Paint paint) => double.IsNaN(paint.Opacity) ? null : context.PushOpacity(paint.Opacity);

    public void Circle(double cx, double cy, double r, in Paint paint, ReadOnlySpan<byte> classes)
    {
        using var clip = paints.Clips.Element(context);
        if (!Hidden)
        {
            using var faded = Faded(paint);
            context.DrawEllipse(paints.Brush(paint.Fill, paint.FillOpacity), paints.Pen(paint), new Point(cx, cy), r, r);
        }
    }

    public void Text(double x, double y, string text, in TextPaint paint, ReadOnlySpan<byte> classes)
    {
        if (Hidden)
        {
            return;
        }

        using (paint.Angle == 0 ? default(DrawingContext.PushedState?) : context.PushTransform(Matrix.CreateTranslation(-x, -y) * Matrix.CreateRotation(paint.Angle * Math.PI / 180) * Matrix.CreateTranslation(x, y)))
        {
            var line = y + (paint.Dy * paint.FontSize);
            var start = 0;
            while (start <= text.Length)
            {
                var end = text.IndexOf('\n', start);
                Line(x, line, end < 0 ? text[start..] : text[start..end], paint);
                if (end < 0)
                {
                    break;
                }

                start = end + 1;
                line += paint.FontSize;
            }
        }
    }

    private void Line(double x, double baseline, string text, in TextPaint paint)
    {
        var formatted = paints.Text(text, paint.FontSize, paint.Fill, null, paint.FontWeight, paint.FontStyle);
        using var faded = double.IsNaN(paint.Opacity) ? default(DrawingContext.PushedState?) : context.PushOpacity(paint.Opacity);
        var left = paint.Anchor switch
        {
            TextAnchor.Middle => x - (formatted.WidthIncludingTrailingWhitespace / 2),
            TextAnchor.End => x - formatted.WidthIncludingTrailingWhitespace,
            _ => x,
        };
        var origin = new Point(left, baseline - formatted.Baseline);
        if (paints.Pen(paint.Stroke, paint.StrokeWidth, null) is { } pen && formatted.BuildGeometry(origin) is { } outline)
        {
            context.DrawGeometry(paints.Brush(paint.Fill), pen, outline);
            return;
        }

        context.DrawText(formatted, origin);
    }

    public void ArcText(in ArcPath arc, string text, in TextPaint paint, ReadOnlySpan<byte> classes)
    {
        if (Hidden)
        {
            return;
        }

        var direction = arc.Counterclockwise ? 1 : -1;
        var angle = arc.Angle;
        foreach (var rune in text.EnumerateRunes())
        {
            var glyph = rune.ToString();
            var formatted = paints.Text(glyph, paint.FontSize, paint.Fill);
            var advance = formatted.WidthIncludingTrailingWhitespace;
            var middle = angle + (direction * advance / 2 / arc.Radius / Pictor.Layout.Polar.Radian);
            var (x, y) = Pictor.Layout.Polar.Point(arc.Cx, arc.Cy, arc.Radius, middle);
            var rotation = (direction > 0 ? -middle - 90 : 90 - middle) * Math.PI / 180;
            var origin = new Point(-advance / 2, -formatted.Height / 2);
            using (context.PushTransform(Matrix.CreateTranslation(origin.X, origin.Y) * Matrix.CreateRotation(rotation) * Matrix.CreateTranslation(x, y)))
            {
                context.DrawText(formatted, default);
            }

            angle += direction * advance / arc.Radius / Pictor.Layout.Polar.Radian;
        }
    }

    public void EndSurface()
    {
    }

    public void Legend(LegendLayout legend)
    {
        if (legend.Spec.Frame is { } frame)
        {
            FramedLegend(legend, frame);
            return;
        }

        if (legend.Spec.Content is { } content)
        {
            CustomLegend(legend, content);
            return;
        }

        var size = legend.Spec.IconSize;
        var center = legend.RowHeight == LegendLayout.LineHeight(paints.FontSize, size) ? LegendLayout.IconCenter(paints.FontSize, size) : legend.RowHeight / 2;
        foreach (var item in legend.Items)
        {
            var middle = item.Y + (legend.RowHeight / 2);
            Icon(item, item.Y + center - (size / 2), size);
            Text(item.X + size + 4, middle, item.Name, new TextPaint(TextAnchor.Start, LabelPlacement.Middle, item.Color, paints.FontSize), default);
        }
    }

    /// <summary>A legend whose content is a whole template: its boxes, then its entries in its items node's box (Recharts' default items in rows, or each entry's template in a list).</summary>
    private void FramedLegend(LegendLayout legend, ContentSpec frame)
    {
        var values = new Pictor.Content.ContentValues(Pictor.Content.ContentSlots.Legend, frame, null);
        var item = ContentProps.Legend(values, legend.Items.Count, legend.Width, legend.Spec);
        var flow = new LegendLayout.LegendFlow(paints.FontSize, legend.Spec, legend.Items);
        var boxes = Pictor.Content.HtmlContent.Arrange(frame.Nodes, item, paints.FontSize, legend.Left, legend.Top, flow, legend.Width);
        ContentDrawing.Html(context, paints, boxes, item);
        var fontSize = paints.FontSize;
        foreach (var box in boxes.Where(box => box.Node.Element == ContentElement.Items))
        {
            var entries = legend.Spec.Content is { } content ? new Pictor.Content.ContentValues(Pictor.Content.ContentSlots.LegendEntry, content, null) : null;
            var placed = LegendLayout.Place(legend, box.X, box.Y, box.Width, entry => entries is null ? default : Pictor.Content.HtmlContent.Measure(entries.Spec.Nodes, ContentProps.LegendEntry(entries, entry), fontSize));
            if (entries is not null && LegendLayout.InRow(frame))
            {
                foreach (var entry in placed)
                {
                    var entryItem = ContentProps.LegendEntry(entries, entry);
                    ContentDrawing.Html(context, paints, Pictor.Content.HtmlContent.Arrange(entries.Spec.Nodes, entryItem, fontSize, entry.X, entry.Y), entryItem);
                }

                continue;
            }

            Legend(new LegendLayout { Spec = legend.Spec with { Frame = null }, Inactive = legend.Inactive, Items = placed, Left = box.X, Top = box.Y, Width = box.Width, Height = box.Height, RowHeight = legend.RowHeight });
        }
    }

    /// <summary>A legend with custom content: each entry's boxes laid out from its place in the list, after the list's default bullet.</summary>
    private void CustomLegend(LegendLayout legend, ContentSpec content)
    {
        var values = new Pictor.Content.ContentValues(Pictor.Content.ContentSlots.LegendEntry, content, null);
        var bullet = paints.Brush("#000");
        foreach (var entry in legend.Items)
        {
            var item = ContentProps.LegendEntry(values, entry);
            var boxes = Pictor.Content.HtmlContent.Arrange(content.Nodes, item, paints.FontSize, entry.X, entry.Y);
            if (boxes.Count > 0)
            {
                var first = boxes[0];
                context.DrawEllipse(bullet, null, new Point(entry.X - 17, first.Y + (first.Height / 2)), 2.5, 2.5);
            }

            ContentDrawing.Html(context, paints, boxes, item);
        }
    }

    public void Hit(double x, double y, double width, double height)
    {
    }

    public void HitShape<TShape>(in TShape shape)
        where TShape : struct, IShape
    {
    }

    public void Tooltip(double x, double y, string label, ReadOnlySpan<TooltipEntry> entries, TooltipSpec spec, in TipFollow follow)
    {
        if (!tooltips)
        {
            return;
        }

        var fontSize = paints.FontSize;
        var (width, height) = TooltipBox.Size(spec, label, entries, fontSize);
        var padding = Css.Length(spec.ContentStyle, "padding", TooltipPadding);
        var border = Css.BorderWidth(spec.ContentStyle, 1);
        var radius = Css.Length(spec.ContentStyle, "border-radius", 0);
        var pen = border > 0 ? paints.Pen(Css.BorderColor(spec.ContentStyle, paints.Theme.TooltipBorder), border, null) : null;
        context.DrawRectangle(paints.Brush(Css.Color(spec.ContentStyle, "background-color", paints.Theme.TooltipBackground)), pen, new Rect(x + (border / 2), y + (border / 2), width - border, height - border), radius, radius);
        var line = Pictor.Text.InterMetrics.LineHeight(fontSize);
        var left = x + border + padding;
        var top = y + border + padding;
        if (label.Length > 0)
        {
            Text(left, top + (line / 2), label, new TextPaint(TextAnchor.Start, LabelPlacement.Middle, Css.Color(spec.LabelStyle, "color", paints.Theme.TooltipLabel), fontSize), default);
            top += line;
        }

        top += 4;
        foreach (var entry in entries)
        {
            Text(left, top + (line / 2), entry.Name + spec.Separator + entry.Value, new TextPaint(TextAnchor.Start, LabelPlacement.Middle, Css.Color(spec.ItemStyle, "color", entry.Color), fontSize), default);
            top += line + 8;
        }
    }

    public void End()
    {
    }

    public void Rect(double x, double y, double width, double height, in Paint paint, ReadOnlySpan<byte> classes)
    {
        using var clip = paints.Clips.Element(context);
        var brush = paints.Brush(paint.Fill, paint.FillOpacity);
        var pen = paints.Pen(paint.Stroke, paint.StrokeWidth, paint.Dash);
        if (brush is not null || pen is not null)
        {
            context.DrawRectangle(brush, pen, new Rect(x, y, width, height));
        }
    }

    public void PlainText(double x, double y, string? text, double fontSize, string? fill, string? stroke)
    {
        if (Hidden)
        {
            return;
        }

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var formatted = paints.Text(text, fontSize, fill ?? "#000");
        var origin = new Point(x, y - formatted.Baseline);
        if (stroke is null)
        {
            context.DrawText(formatted, origin);
            return;
        }

        context.DrawGeometry(paints.Brush(fill ?? "#000"), paints.Pen(stroke, 1, null), formatted.BuildGeometry(origin)!);
    }

    public void OpenItem(string name) => motion?.Open(context, default);

    public void OptionsText(double x, double y, string text, TextOptionsSpec options)
    {
        if (Hidden)
        {
            return;
        }

        var fill = options.Fill ?? paints.Theme.LabelText;
        var formatted = paints.Text(text, ChartPaints.FontPixels(options.FontSize, paints.FontSize), fill, options.FontFamily, options.FontWeight);
        var origin = new Point(x - (formatted.WidthIncludingTrailingWhitespace / 2), y - formatted.Baseline);
        if (paints.Pen(options.Stroke, 1, null) is not { } pen || formatted.BuildGeometry(origin) is not { } geometry)
        {
            context.DrawText(formatted, origin);
            return;
        }

        if (options.PaintOrder?.TrimStart().StartsWith("stroke", StringComparison.Ordinal) == true)
        {
            context.DrawGeometry(null, pen, geometry);
            context.DrawGeometry(paints.Brush(fill), null, geometry);
            return;
        }

        context.DrawGeometry(paints.Brush(fill), pen, geometry);
    }

    public void NestIndex(double top, double width, double height, NestBox[] trail, Specs.ContentSpec? content)
    {
        var line = Pictor.Text.InterMetrics.LineHeight(paints.FontSize);
        context.DrawRectangle(paints.Brush(paints.Theme.NestIndexSurface), null, new Rect(0, top, width, height - top));
        var local = paints;
        var values = content is null ? null : new Pictor.Content.ContentValues(Pictor.Content.ContentSlots.NestIndex, content, null);
        var widths = new Dictionary<string, double>(StringComparer.Ordinal);
        for (var i = 0; values is not null && i < trail.Length; i++)
        {
            widths.TryAdd(trail[i].Name, Pictor.Content.HtmlContent.Measure(values.Spec.Nodes, ContentProps.NestIndex(values, i, trail[i]), local.FontSize).Width);
        }

        foreach (var (box, left, size) in NestBoxes.Place(trail, local.FontSize, width, name => widths.TryGetValue(name, out var measured) ? measured : local.Text(name, local.FontSize, local.Theme.NestIndexText).WidthIncludingTrailingWhitespace))
        {
            context.DrawRectangle(paints.Brush(paints.Theme.NestIndexBackground), null, new Rect(left, top + 8, size, line));
            if (values is not null)
            {
                var item = ContentProps.NestIndex(values, Array.IndexOf(trail, box), box);
                ContentDrawing.Html(context, paints, Pictor.Content.HtmlContent.Arrange(values.Spec.Nodes, item, paints.FontSize, left + 7, top + 8), item, paints.Theme.NestIndexText);
                continue;
            }

            var formatted = paints.Text(box.Name, paints.FontSize, paints.Theme.NestIndexText);
            context.DrawText(formatted, new Point(left + 7, top + 8 + ((line - formatted.Height) / 2)));
        }
    }

    private void Icon(in LegendItem item, double y, double size)
    {
        var (icon, color, x) = (item.Icon, item.Color, item.X);
        var scale = size / 32;
        switch (icon)
        {
            case LegendIcon.Line:
            case LegendIcon.PlainLine:
                var pen = paints.Pen(color, 4 * scale, icon == LegendIcon.PlainLine ? Scaled(item.Dash, scale) : null)!;
                var middle = y + (16 * scale);
                if (icon == LegendIcon.PlainLine)
                {
                    context.DrawLine(pen, new Point(x, middle), new Point(x + size, middle));
                    break;
                }

                context.DrawLine(pen, new Point(x, middle), new Point(x + (10.667 * scale), middle));
                context.DrawLine(pen, new Point(x + (21.333 * scale), middle), new Point(x + size, middle));
                context.DrawEllipse(null, pen, new Point(x + (16 * scale), middle), 5.333 * scale, 5.333 * scale);

                break;
            case LegendIcon.Rect:
                context.DrawRectangle(paints.Brush(color), null, new Rect(x, y + (4 * scale), size, 24 * scale));
                break;
            default:
                using (context.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(x + (16 * scale), y + (16 * scale))))
                {
                    Shape(LegendIcons.Symbol(icon), new Paint(null, double.NaN, color), default);
                }

                break;
        }
    }

    /// <summary>A legend icon's dash pattern scaled to the icon; a part that is not a number is left out rather than failing the draw.</summary>
    private static string? Scaled(string? dash, double scale) =>
        dash is null ? null : string.Join(' ', dash.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => double.TryParse(part, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var length) ? (length * scale).ToString(System.Globalization.CultureInfo.InvariantCulture) : null)
            .Where(part => part is not null));
}
