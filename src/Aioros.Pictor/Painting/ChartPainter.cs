using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>
/// Paints a laid-out chart in Recharts' element order: the surface, then the chart's children in the order the XAML
/// declares them (grid, axes, references, series), the hover layer, then the legend. One painter for both targets.
/// </summary>
internal static class ChartPainter
{
    /// <summary>Paint every category's hover state, hidden until hovered (the web).</summary>
    public const int AllTips = -1;

    /// <summary>Paint no hover state (the desktop's recorded scene, which draws the hovered one over it).</summary>
    public const int NoTips = -2;

    /// <summary>Paints the chart with every hover state (<see cref="AllTips"/>), none (<see cref="NoTips"/>) or the hovered category's.</summary>
    public static void Paint<TCanvas>(ref TCanvas canvas, ChartLayout layout, int active)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Begin(layout);
        ReferencePainter.DefinePlotClip(ref canvas, layout);
        var hovered = active != NoTips && layout.HoverCount > 0;
        var hits = Children(ref canvas, layout, hovered, active);
        if (hovered && !hits)
        {
            TipPainter.Cursors(ref canvas, layout, active);
        }

        if (hovered)
        {
            TipPainter.Tips(ref canvas, layout, active);
        }

        canvas.EndSurface();
        if (layout.Legend is { } legend)
        {
            canvas.Legend(legend);
        }

        canvas.End();
    }

    /// <summary>
    /// Paints a chart compact (Recharts' <c>compact</c> render, a brush's panorama): its children in their order, at the
    /// origin of the surface it is drawn on, without hover states, legend or wrapper.
    /// </summary>
    public static void PaintCompact<TCanvas>(ref TCanvas canvas, ChartLayout layout)
        where TCanvas : struct, IChartCanvas
    {
        ReferencePainter.DefinePlotClip(ref canvas, layout);
        Children(ref canvas, layout, hovered: false, NoTips);
    }

    /// <summary>The chart's children in Recharts' order, with the hit areas before the first series when hover states are drawn; whether they were.</summary>
    private static bool Children<TCanvas>(ref TCanvas canvas, ChartLayout layout, bool hovered, int active)
        where TCanvas : struct, IChartCanvas
    {
        var hits = false;
        var spec = layout.Spec;
        var count = (spec.Grid is null ? 0 : 1) + spec.XAxes.Length + spec.YAxes.Length + spec.ReferenceLines.Length + spec.Series.Length
            + spec.ReferenceAreas.Length + (spec.Brush is null ? 0 : 1) + spec.ReferenceDots.Length + spec.Customized.Length;
        Span<(int Order, int Kind, int Index)> order = count <= 64 ? stackalloc (int, int, int)[count] : new (int, int, int)[count];
        Order(spec, order);
        foreach (var (_, kind, index) in order)
        {
            if (kind == 4 && hovered && !hits)
            {
                TipPainter.Cursors(ref canvas, layout, active);
                hits = true;
            }

            switch (kind)
            {
                case 0:
                    GridPainter.Paint(ref canvas, layout, layout.Spec.Grid!);
                    break;
                case 1:
                    Axis(ref canvas, layout, layout.XAxes[index]);
                    break;
                case 2:
                    Axis(ref canvas, layout, layout.YAxes[index]);
                    break;
                case 3:
                    ReferencePainter.Line(ref canvas, layout, layout.Spec.ReferenceLines[index]);
                    break;
                case 5:
                    ReferencePainter.Area(ref canvas, layout, layout.Spec.ReferenceAreas[index]);
                    break;
                case 6:
                    ReferencePainter.Dot(ref canvas, layout, layout.Spec.ReferenceDots[index]);
                    break;
                case 7:
                    BrushPainter.Paint(ref canvas, layout.Brush, layout.Spec.FontSize, layout.Spec.Colors, layout.WholeData);
                    break;
                case 8:
                    Customized(ref canvas, layout, layout.Spec.Customized[index]);
                    break;
                default:
                    Series(ref canvas, layout, index);
                    break;
            }
        }

        return hits;
    }

    /// <summary>A <c>Customized</c> child: its content drawn with the chart's layout in Recharts' wrapper.</summary>
    private static void Customized<TCanvas>(ref TCanvas canvas, ChartLayout layout, CustomizedSpec customized)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Open("recharts-layer recharts-customized-wrapper"u8);
        var content = new Content.ContentValues(Content.ContentSlots.Layout, customized.Content, null);
        canvas.Content(customized.Content, ContentProps.Layout(content, layout, customized.Order));
        canvas.Close();
    }

    private static void Series<TCanvas>(ref TCanvas canvas, ChartLayout layout, int index)
        where TCanvas : struct, IChartCanvas
    {
        if (Toggles.Concealed(layout.Spec, index) || Laid(layout, layout.Spec.Series[index]) is not { } series)
        {
            return;
        }

        var marked = Toggles.Open(ref canvas, layout.Spec, index);
        SeriesPainter.Paint(ref canvas, layout, series);
        if (marked)
        {
            canvas.Close();
        }
    }

    /// <summary>The laid-out series of a spec, by reference.</summary>
    private static SeriesLayout? Laid(ChartLayout layout, SeriesSpec spec)
    {
        for (var i = 0; i < layout.Series.Count; i++)
        {
            if (ReferenceEquals(layout.Series[i].Spec, spec))
            {
                return layout.Series[i];
            }
        }

        return null;
    }

    /// <summary>The chart's children (kind and index) sorted by their order in the chart's children, then by kind.</summary>
    private static void Order(ChartSpec spec, Span<(int Order, int Kind, int Index)> order)
    {
        var count = 0;
        if (spec.Grid is { } grid)
        {
            order[count++] = (grid.Order, 0, 0);
        }

        for (var i = 0; i < spec.XAxes.Length; i++)
        {
            order[count++] = (spec.XAxes[i].Order, 1, i);
        }

        for (var i = 0; i < spec.YAxes.Length; i++)
        {
            order[count++] = (spec.YAxes[i].Order, 2, i);
        }

        for (var i = 0; i < spec.ReferenceLines.Length; i++)
        {
            order[count++] = (spec.ReferenceLines[i].Order, 3, i);
        }

        for (var i = 0; i < spec.Series.Length; i++)
        {
            order[count++] = (spec.Series[i].Order, 4, i);
        }

        for (var i = 0; i < spec.ReferenceAreas.Length; i++)
        {
            order[count++] = (spec.ReferenceAreas[i].Order, 5, i);
        }

        if (spec.Brush is { } brush)
        {
            order[count++] = (brush.Order, 7, 0);
        }

        for (var i = 0; i < spec.ReferenceDots.Length; i++)
        {
            order[count++] = (spec.ReferenceDots[i].Order, 6, i);
        }

        for (var i = 0; i < spec.Customized.Length; i++)
        {
            order[count++] = (spec.Customized[i].Order, 8, i);
        }

        order.Sort(static (a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Kind.CompareTo(b.Kind));
    }

    private static void Axis<TCanvas>(ref TCanvas canvas, ChartLayout layout, AxisLayout axis)
        where TCanvas : struct, IChartCanvas
    {
        var spec = axis.Spec;
        if (spec.Hide || axis.Width <= 0 || axis.Height <= 0 || axis.Ticks.Length == 0)
        {
            return;
        }

        canvas.Open(ClassNames.With(axis.IsX ? "recharts-layer recharts-cartesian-axis recharts-xAxis xAxis"u8 : "recharts-layer recharts-cartesian-axis recharts-yAxis yAxis"u8, spec.ClassName));
        var theme = layout.Spec.Colors;
        var paint = LinePaint(spec.Stroke ?? theme.TickLine, spec.TickLineStyle).With(spec.Presentation);
        if (spec.AxisLine)
        {
            var (x1, y1, x2, y2) = AxisLine(axis);
            canvas.Line(x1, y1, x2, y2, LinePaint(spec.Stroke ?? theme.AxisLine, spec.AxisLineStyle).With(spec.Presentation), "recharts-cartesian-axis-line"u8);
        }

        canvas.Open("recharts-cartesian-axis-ticks"u8);
        var text = TickText(axis, layout.Spec.FontSize, theme.TickText).With(spec.Presentation);
        var content = ContentProps.Of(spec.TickContent, Content.ContentSlots.Tick, null);
        var index = 0;
        foreach (var tick in axis.Visible)
        {
            canvas.Open("recharts-layer recharts-cartesian-axis-tick"u8);
            var (line, labelX, labelY) = TickCoordinates(axis, tick);
            if (spec.TickLine)
            {
                canvas.Line(line.X1, line.Y1, line.X2, line.Y2, paint, "recharts-cartesian-axis-tick-line"u8);
            }

            if (content is not null)
            {
                canvas.Content(content.Spec, ContentProps.Tick(content, axis, tick, (labelX, labelY), text, spec.Stroke ?? theme.AxisLine, index++));
                canvas.Close();
                continue;
            }

            var wrapped = axis.Width > 0 ? Text.TextLines.Wrap(tick.Text, axis.Width, layout.Spec.FontSize, breakAll: false) : tick.Text;
            var lines = Text.TextLines.Count(wrapped);
            canvas.Text(labelX, labelY, wrapped, lines == 1 ? text : text with { Dy = FirstLine(text.Dy, lines) }, "recharts-text recharts-cartesian-axis-tick-value"u8);
            canvas.Close();
        }

        canvas.Close();
        Labels(ref canvas, spec.Label, spec.Labels, (axis.X, axis.Y, axis.Width, axis.Height), layout.Spec.FontSize, theme.LabelText, spec.Order);

        canvas.Close();
    }

    /// <summary>
    /// Where a tick takes the pointer, as Recharts' tick group does through what it paints: its label's lines (each the
    /// box of its text, its font's ascent and descent tall, turned by the tick's angle) and its tick line, as one path.
    /// </summary>
    public static string TickArea(ChartLayout layout, AxisLayout axis, in AxisTick tick)
    {
        var spec = axis.Spec;
        var text = TickText(axis, layout.Spec.FontSize, layout.Spec.Colors.TickText);
        var (line, x, y) = TickCoordinates(axis, tick);
        var path = new System.Text.StringBuilder();
        if (spec.TickLine)
        {
            Box(path, Math.Min(line.X1, line.X2) - 0.5, Math.Min(line.Y1, line.Y2) - 0.5, Math.Abs(line.X2 - line.X1) + 1, Math.Abs(line.Y2 - line.Y1) + 1, 0, 0, 0);
        }

        if (!spec.Tick)
        {
            return path.ToString();
        }

        var wrapped = axis.Width > 0 ? Text.TextLines.Wrap(tick.Text, axis.Width, layout.Spec.FontSize, breakAll: false) : tick.Text;
        var lines = wrapped.Split('\n');
        var size = text.FontSize;
        var ascent = Math.Round(size * 2728 / 2816);
        var baseline = y + ((lines.Length == 1 ? text.Dy : FirstLine(text.Dy, lines.Length)) * size);
        foreach (var part in lines)
        {
            var width = Text.InterMetrics.Width(part, size);
            var left = text.Anchor switch { TextAnchor.Start => x, TextAnchor.End => x - width, _ => x - (width / 2) };
            Box(path, left, baseline - ascent, width, Text.InterMetrics.LineHeight(size), text.Angle, x, y);
            baseline += size;
        }

        return path.ToString();
    }

    /// <summary>A rectangle's closed outline in a path, turned by an angle in degrees around a point.</summary>
    private static void Box(System.Text.StringBuilder path, double x, double y, double width, double height, double angle, double cx, double cy)
    {
        var (cos, sin) = (Math.Cos(angle * Math.PI / 180), Math.Sin(angle * Math.PI / 180));
        ReadOnlySpan<(double X, double Y)> corners = [(x, y), (x + width, y), (x + width, y + height), (x, y + height)];
        for (var i = 0; i < 4; i++)
        {
            var (px, py) = (corners[i].X - cx, corners[i].Y - cy);
            path.Append(i == 0 ? 'M' : 'L').Append(Aioros.Graphs.Formatting.JsNumber.ToShortestString(Math.Round(cx + (px * cos) - (py * sin), 3))).Append(',').Append(Aioros.Graphs.Formatting.JsNumber.ToShortestString(Math.Round(cy + (px * sin) + (py * cos), 3)));
        }

        path.Append('Z');
    }

    private static (double X1, double Y1, double X2, double Y2) AxisLine(AxisLayout axis)
    {
        var mirror = axis.Spec.Mirror;
        return axis.Spec.Orientation switch
        {
            AxisOrientation.Top or AxisOrientation.Bottom => Horizontal(axis, (axis.Spec.Orientation == AxisOrientation.Top) != mirror ? axis.Height : 0),
            _ => Vertical(axis, (axis.Spec.Orientation == AxisOrientation.Left) != mirror ? axis.Width : 0),
        };

        static (double, double, double, double) Horizontal(AxisLayout axis, double shift) => (axis.X, axis.Y + shift, axis.X + axis.Width, axis.Y + shift);

        static (double, double, double, double) Vertical(AxisLayout axis, double shift) => (axis.X + shift, axis.Y, axis.X + shift, axis.Y + axis.Height);
    }

    /// <summary>The first line's shift of a text of several lines, by its vertical anchor (Recharts' <c>Text</c>: start 0.71em, middle up by half the extra lines, end up by all of them).</summary>
    private static double FirstLine(double dy, int lines) => dy switch
    {
        LabelPlacement.Start => dy,
        LabelPlacement.Middle => ((lines - 1) / 2.0 * -1) + dy,
        _ => -(lines - 1),
    };

    /// <summary>A line's paint from the axis' stroke and the line's own props object, if any.</summary>
    private static Paint LinePaint(string stroke, LineStyle? style) =>
        style is null ? Painting.Paint.Stroked(stroke) : Painting.Paint.Stroked(style.Stroke ?? stroke, style.StrokeWidth ?? double.NaN, style.StrokeDashArray);

    /// <summary>Recharts' <c>getTickTextAnchor</c> and <c>getTickVerticalAnchor</c>, with the tick props (anchor, fill, size, rotation) over them.</summary>
    private static TextPaint TickText(AxisLayout axis, double fontSize, string themed)
    {
        var spec = axis.Spec;
        var mirror = spec.Mirror;
        var (anchor, dy) = spec.Orientation switch
        {
            AxisOrientation.Left => (mirror ? TextAnchor.Start : TextAnchor.End, LabelPlacement.Middle),
            AxisOrientation.Right => (mirror ? TextAnchor.End : TextAnchor.Start, LabelPlacement.Middle),
            AxisOrientation.Top => (TextAnchor.Middle, mirror ? LabelPlacement.Start : LabelPlacement.End),
            _ => (TextAnchor.Middle, mirror ? LabelPlacement.End : LabelPlacement.Start),
        };
        if (spec.TextAnchor is { } own)
        {
            anchor = (TextAnchor)(int)own;
        }

        return new TextPaint(anchor, dy, spec.TickFill ?? spec.Stroke ?? themed, spec.TickFontSize ?? fontSize, spec.Angle, OwnSize: spec.TickFontSize is not null);
    }

    private static ((double X1, double Y1, double X2, double Y2) Line, double X, double Y) TickCoordinates(AxisLayout axis, AxisTick tick)
    {
        var spec = axis.Spec;
        var sign = spec.Mirror ? -1 : 1;
        var c = tick.Coordinate;
        switch (spec.Orientation)
        {
            case AxisOrientation.Top:
                {
                    var y2 = axis.Y + (spec.Mirror ? 0 : axis.Height);
                    var y1 = y2 - (sign * spec.TickSize);
                    return ((c, y1, c, y2), tick.LabelCoordinate, y1 - (sign * spec.TickMargin));
                }

            case AxisOrientation.Left:
                {
                    var x2 = axis.X + (spec.Mirror ? 0 : axis.Width);
                    var x1 = x2 - (sign * spec.TickSize);
                    return ((x1, c, x2, c), x1 - (sign * spec.TickMargin), tick.LabelCoordinate);
                }

            case AxisOrientation.Right:
                {
                    var x2 = axis.X + (spec.Mirror ? axis.Width : 0);
                    var x1 = x2 + (sign * spec.TickSize);
                    return ((x1, c, x2, c), x1 + (sign * spec.TickMargin), tick.LabelCoordinate);
                }

            default:
                {
                    var y2 = axis.Y + (spec.Mirror ? axis.Height : 0);
                    var y1 = y2 + (sign * spec.TickSize);
                    return ((c, y1, c, y2), tick.LabelCoordinate, y1 + (sign * spec.TickMargin));
                }
        }
    }

    public static void Label<TCanvas>(ref TCanvas canvas, LabelSpec label, LabelPosition position, double x, double y, double width, double height, double fontSize, string fill)
        where TCanvas : struct, IChartCanvas
    {
        if (label.Value is { } text)
        {
            Label(ref canvas, label, text, position, x, y, width, height, fontSize, fill);
        }
    }

    public static void Label<TCanvas>(ref TCanvas canvas, LabelSpec label, string text, LabelPosition position, double x, double y, double width, double height, double fontSize, string fill)
        where TCanvas : struct, IChartCanvas
    {
        var (lx, ly, anchor, dy) = label.PositionX is { } px && label.PositionY is { } py
            ? (x + Layout.Polar.Percent(px, width, 0), y + Layout.Polar.Percent(py, height, 0), TextAnchor.End, LabelPlacement.End)
            : LabelPlacement.Of(position, x, y, width, height, label.Offset);
        canvas.Text(lx, ly, Formatted(text, label.Format), new TextPaint(anchor, dy, label.Fill ?? fill, label.FontSize ?? fontSize, label.Angle, OwnSize: label.FontSize is not null), "recharts-text recharts-label"u8);
    }

    /// <summary>
    /// Recharts' <c>Label.renderCallByParent</c>: an element's label prop, then its <c>Label</c> children, each at its
    /// position around the element's box (the center by default).
    /// </summary>
    public static void Labels<TCanvas>(ref TCanvas canvas, LabelSpec? label, System.Collections.Immutable.ImmutableArray<LabelSpec> labels, (double X, double Y, double Width, double Height) box, double fontSize, string fill, int order)
        where TCanvas : struct, IChartCanvas
    {
        var index = 0;
        if (label is not null)
        {
            Owned(ref canvas, label, box, fontSize, fill, order, index++);
        }

        foreach (var child in labels)
        {
            Owned(ref canvas, child, box, fontSize, fill, order, index++);
        }
    }

    /// <summary>One label of an element: its template with the element's box (Recharts' <c>viewBox</c>), its value and offset, or its text at its position.</summary>
    private static void Owned<TCanvas>(ref TCanvas canvas, LabelSpec label, (double X, double Y, double Width, double Height) box, double fontSize, string fill, int order, int index)
        where TCanvas : struct, IChartCanvas
    {
        if (ContentProps.Of(label.Content, Content.ContentSlots.Label, null) is { } values)
        {
            var text = label.Value is { } value ? Formatted(value, label.Format) : string.Empty;
            var number = double.TryParse(label.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : double.NaN;
            canvas.Content(label.Content!, ContentProps.Label(new LabelContent(values, ContentProps.Template(order, 12), index, number, null), label, text, box, default));
            return;
        }

        Label(ref canvas, label, label.Position ?? LabelPosition.Center, box.X, box.Y, box.Width, box.Height, fontSize, fill);
    }

    /// <summary>A label's value through its format (Recharts' <c>formatter</c>): a number formatted as a number, other text as text.</summary>
    public static string Formatted(string text, string? format) => format is null
        ? text
        : double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number)
            ? Text.Formats.Apply(format, number, text)
            : Text.Formats.Apply(format, text, text);
}
