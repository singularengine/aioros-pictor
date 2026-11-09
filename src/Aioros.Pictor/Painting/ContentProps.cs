using Aioros.Pictor.Content;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>What Recharts passes a polar axis' custom tick: its label's point, index, coordinate and the count of ticks, the chart's center, the angle axis' radius or the radius axis' angle, its text and paint, its default anchor and the axis' orientation.</summary>
internal readonly record struct PolarTickProps(double X, double Y, int Index, double Coordinate, int Count, double Cx, double Cy, double Radius, double Angle, string Text, string Fill, string Stroke, TextAnchor Anchor, string Orientation);

/// <summary>
/// The props Recharts passes to custom content, filled per item into a template's buffers in its slot's order (see
/// <see cref="ContentSlots"/>), and the keys of the painter's calls.
/// </summary>
internal static class ContentProps
{
    /// <summary>The place of a template among a chart's: its element's order and its slot on that element.</summary>
    public static int Template(int order, int slot) => (order * 16) + slot;

    /// <summary>The buffers of a template, or null when the slot has none.</summary>
    public static ContentValues? Of(ContentSpec? content, ContentSlot slot, Data.ChartData? data) =>
        content is null ? null : new ContentValues(slot, content, data);

    /// <summary>A bar's props: its rectangle, value and index, its background's rectangle, and its paint, name and data key.</summary>
    public static ContentItem Bar(ContentValues values, SeriesLayout series, int index, in Paint paint, int template, int state = 0)
    {
        var bar = series.Bars[index];
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5]) = (bar.X, bar.Y, bar.Width, bar.Height, bar.Value, index);
        (numbers[6], numbers[7], numbers[8], numbers[9]) = series.ValueAxis.IsX
            ? (series.XAxis.X, bar.Y, series.XAxis.Width, bar.Height)
            : (bar.X, series.YAxis.Y, bar.Width, series.YAxis.Height);
        var texts = values.Texts;
        (texts[0], texts[1], texts[2], texts[3]) = (paint.Fill, paint.Stroke, series.Spec.Name ?? series.Spec.DataKey, series.Spec.DataKey);
        values.Read(index);
        return values.Item(ContentItem.KeyOf(template, index, state)) with { Box = (bar.X, bar.Y, bar.Width, bar.Height) };
    }

    /// <summary>A dot's props: its center, radius and stroke width, the point's value and index, and the dot's paint, the series' name and data key.</summary>
    public static ContentItem Dot(ContentValues values, SeriesLayout series, int index, double radius, in Paint paint, int template, int state = 0)
    {
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5]) = (series.X[index], series.Y[index], radius, paint.StrokeWidth, Value(series, index), index);
        var texts = values.Texts;
        (texts[0], texts[1], texts[2], texts[3]) = (paint.Fill, paint.Stroke, series.Spec.Name ?? series.Spec.DataKey, series.Spec.DataKey);
        values.Read(index);
        return values.Item(ContentItem.KeyOf(template, index, state)) with { Box = (series.X[index] - radius, series.Y[index] - radius, radius * 2, radius * 2) };
    }

    /// <summary>A point's value as Recharts passes it: the item's value of the series' key.</summary>
    public static double Value(SeriesLayout series, int index) => series.Values.Length > index ? series.Values[index] : double.NaN;

    /// <summary>
    /// A label's props: the box it is placed around, its value, index and offset, a polar label's sector, its text, its
    /// own fill and its series' name.
    /// </summary>
    public static ContentItem Label(in LabelContent content, LabelSpec label, string text, (double X, double Y, double Width, double Height) box, in PolarBox polar)
    {
        var values = content.Values;
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5], numbers[6]) = (box.X, box.Y, box.Width, box.Height, content.Value, content.Index, label.Offset);
        (numbers[7], numbers[8], numbers[9], numbers[10], numbers[11], numbers[12]) = (polar.Cx, polar.Cy, polar.InnerRadius, polar.OuterRadius, polar.StartAngle, polar.EndAngle);
        var texts = values.Texts;
        (texts[0], texts[1], texts[2]) = (text, label.Fill, content.Name);
        values.Read(content.Index);
        return values.Item(ContentItem.KeyOf(content.Template, content.Index)) with { Box = box };
    }

    /// <summary>A label list's content for one label, when the list has a template.</summary>
    public static LabelContent? Label(ContentValues? values, int template, int index, double value, string? name) =>
        values is null ? null : new LabelContent(values, template, index, value, name);

    /// <summary>A tick's props: its label's point, the axis' box, its index, coordinate and the count of visible ticks, its text, fill, the axis' stroke, its default anchor and the axis' orientation.</summary>
    public static ContentItem Tick(ContentValues values, AxisLayout axis, in AxisTick tick, (double X, double Y) label, in TextPaint paint, string stroke, int index)
    {
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5], numbers[6]) = (label.X, label.Y, axis.Width, axis.Height, index, tick.Coordinate, axis.Visible.Length);
        (numbers[7], numbers[8], numbers[9], numbers[10]) = (0, 0, 0, 0);
        var texts = values.Texts;
        (texts[0], texts[1], texts[2], texts[3], texts[4]) = (tick.Text, paint.Fill, stroke, paint.Anchor switch { TextAnchor.Start => "start", TextAnchor.End => "end", _ => "middle" }, axis.Spec.Orientation.ToString().ToLowerInvariant());
        return values.Item(ContentItem.KeyOf(ContentProps.Template(axis.Spec.Order, 8), index)) with { Box = (label.X, label.Y, 0, 0) };
    }

    /// <summary>A polar tick's props: its label's point, its index, coordinate and the count of ticks, the chart's center, the angle axis' radius or the radius axis' angle, its text, fill, the axis' stroke, its default anchor and the axis' orientation.</summary>
    public static ContentItem PolarTick(ContentValues values, in PolarTickProps tick, int order)
    {
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5], numbers[6]) = (tick.X, tick.Y, 0, 0, tick.Index, tick.Coordinate, tick.Count);
        (numbers[7], numbers[8], numbers[9], numbers[10]) = (tick.Cx, tick.Cy, tick.Radius, tick.Angle);
        var texts = values.Texts;
        (texts[0], texts[1], texts[2], texts[3], texts[4]) = (tick.Text, tick.Fill, tick.Stroke, tick.Anchor switch { TextAnchor.Start => "start", TextAnchor.End => "end", _ => "middle" }, tick.Orientation);
        return values.Item(ContentItem.KeyOf(Template(order, 8), tick.Index)) with { Box = (tick.X, tick.Y, 0, 0) };
    }

    /// <summary>
    /// A pie's sector's props: its center, radii, angles, share, value, index and corner radius, the cosine, sine and side
    /// of its middle angle and the point on its outer radius there (as Recharts' examples compute them), its paint, name
    /// and the anchor of its side.
    /// </summary>
    public static ContentItem Sector(ContentValues values, PieLayout pie, int index, int template, int state = 0)
    {
        var sector = pie.Sectors[index];
        var item = pie.Items[index];
        var paint = SeriesPainter.Celled(new Paint(pie.Spec.BlendStroke ? item.Fill : item.Stroke, double.NaN, item.Fill), item.Cell);
        return Sector(values, sector.Shape, sector.MidAngle, sector.Percent, item.Value, index, (paint.Fill, paint.Stroke, item.Name), template, state);
    }

    /// <summary>A sector's props from its shape, middle angle, share, value, index, paint and name.</summary>
    public static ContentItem Sector(ContentValues values, SectorShape shape, double midAngle, double percent, double value, int index, (string? Fill, string? Stroke, string? Name) look, int template, int state)
    {
        var radians = -midAngle * Math.PI / 180;
        var (cos, sin) = (Math.Cos(radians), Math.Sin(radians));
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5], numbers[6], numbers[7]) = (shape.Cx, shape.Cy, shape.InnerRadius, shape.OuterRadius, shape.StartAngle, shape.EndAngle, midAngle, percent);
        (numbers[8], numbers[9], numbers[10], numbers[11], numbers[12], numbers[13]) = (value, index, shape.CornerRadius, cos, sin, cos >= 0 ? 1 : -1);
        (numbers[14], numbers[15]) = (shape.Cx + (shape.OuterRadius * cos), shape.Cy + (shape.OuterRadius * sin));
        var texts = values.Texts;
        (texts[0], texts[1], texts[2], texts[3]) = (look.Fill, look.Stroke, look.Name, cos >= 0 ? "start" : "end");
        values.Read(index);
        return values.Item(ContentItem.KeyOf(template, index, state)) with { Box = (shape.Cx - shape.OuterRadius, shape.Cy - shape.OuterRadius, shape.OuterRadius * 2, shape.OuterRadius * 2) };
    }

    /// <summary>A radial bar's props, as a sector's: its center, radii, angles, value, index, paint and name (its share is 0, as Recharts passes none).</summary>
    public static ContentItem RadialBar(ContentValues values, RadialBarLayout bar, int index, int template, int state = 0)
    {
        var sector = bar.Sectors[index];
        var shape = sector.Shape;
        var paint = SeriesPainter.Celled(new Paint(bar.Spec.Stroke, double.NaN, sector.Fill), index < bar.Spec.Cells.Length ? bar.Spec.Cells[index] : null);
        return Sector(values, shape, (shape.StartAngle + shape.EndAngle) / 2, 0, sector.Value, index, (paint.Fill, paint.Stroke, sector.Name), template, state);
    }

    /// <summary>A scatter point's props: its center, its symbol's box (Recharts' <c>x</c>, <c>y</c>, <c>width</c>, <c>height</c> around a circle of the symbol's area), its area, index and values, and its paint and series' name.</summary>
    public static ContentItem Scatter(ContentValues values, SeriesLayout series, int index, in Paint paint, int template, int state = 0)
    {
        var radius = ScatterPainter.Radius(series, index);
        var (cx, cy) = (series.X[index], series.Y[index]);
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5]) = (cx, cy, cx - radius, cy - radius, radius * 2, radius * 2);
        (numbers[6], numbers[7], numbers[8], numbers[9], numbers[10]) = (series.Sizes.Length > index ? series.Sizes[index] : 64, index, At(series.XValues, index), At(series.Values, index), At(series.ZValues, index));
        var texts = values.Texts;
        (texts[0], texts[1], texts[2]) = (paint.Fill, paint.Stroke, series.Spec.Name ?? series.Spec.DataKey);
        values.Read(index);
        return values.Item(ContentItem.KeyOf(template, index, state)) with { Box = (cx - radius, cy - radius, radius * 2, radius * 2) };
    }

    private static double At(double[] values, int index) => index < values.Length ? values[index] : double.NaN;

    /// <summary>A funnel's trapezoid's props: its upper edge's left and its top, its edges' widths and height, the item's value, index and share of the first value, its paint and name.</summary>
    public static ContentItem Trapezoid(ContentValues values, FunnelSeries funnel, int index, in Paint paint, int template, int state = 0)
    {
        var item = funnel.Items[index];
        var first = funnel.Items[0].Value;
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5], numbers[6], numbers[7]) = (item.X, item.Y, item.UpperWidth, item.LowerWidth, item.Height, item.Value, index, first == 0 ? 0 : item.Value / first);
        var texts = values.Texts;
        (texts[0], texts[1], texts[2]) = (paint.Fill, paint.Stroke, item.Name);
        values.Read(index);
        return values.Item(ContentItem.KeyOf(template, index, state)) with { Box = (Math.Min(item.X, item.X + ((item.UpperWidth - item.LowerWidth) / 2)), item.Y, Math.Max(item.UpperWidth, item.LowerWidth), item.Height) };
    }

    /// <summary>A radar's props: the chart's center, its fill opacity, its points (x and y in turn), its paint and name.</summary>
    public static ContentItem Radar(ContentValues values, PolarLayout layout, RadarLayout radar, in Paint paint, int template)
    {
        var points = new double[radar.X.Length * 2];
        for (var i = 0; i < radar.X.Length; i++)
        {
            (points[2 * i], points[(2 * i) + 1]) = (radar.X[i], radar.Y[i]);
        }

        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2]) = (layout.Cx, layout.Cy, double.IsNaN(paint.FillOpacity) ? 1 : paint.FillOpacity);
        var texts = values.Texts;
        (texts[0], texts[1], texts[2]) = (paint.Fill, paint.Stroke, radar.Spec.Name ?? radar.Spec.DataKey);
        return values.Item(ContentItem.KeyOf(template, 0)) with { Points = points, Box = (layout.Cx - layout.OuterRadius, layout.Cy - layout.OuterRadius, layout.OuterRadius * 2, layout.OuterRadius * 2) };
    }

    /// <summary>A treemap node's props: its box, depth, index and value, whether it has children, its name, its panel color and the treemap's stroke; its data members from the node's item.</summary>
    public static ContentItem TreemapNode(ContentValues values, TreemapPlot plot, TreemapNode node, string fill, int serial)
    {
        var numbers = values.Numbers;
        var parent = node.Children is { Length: > 0 };
        (numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5], numbers[6], numbers[7], numbers[8]) = (node.X, node.Y, node.Width, node.Height, node.Depth, node.Index, node.Value, parent ? 1 : 0, parent ? 0 : 1);
        var texts = values.Texts;
        (texts[0], texts[1], texts[2]) = (node.Name, fill, plot.Treemap.Stroke ?? plot.Spec.Colors.TreemapStroke);
        values.Read(-1);
        return values.Item(ContentItem.KeyOf(Template(0, 0), serial)) with { Box = (node.X, node.Y, node.Width, node.Height) };
    }

    /// <summary>A Sankey node's props: its box (in the chart's coordinates), index, value and depth, the chart's width (Recharts' examples pass it as <c>containerWidth</c>), its name and paint.</summary>
    public static ContentItem SankeyNode(ContentValues values, SankeyPlot plot, int index, in Paint paint)
    {
        var node = plot.Nodes[index];
        var (x, y) = (node.X + plot.Spec.Margin.Left, node.Y + plot.Spec.Margin.Top);
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5], numbers[6], numbers[7]) = (x, y, node.Dx, node.Dy, index, node.Value, node.Depth, plot.Spec.Width);
        var texts = values.Texts;
        (texts[0], texts[1], texts[2]) = (node.Name, paint.Fill, paint.Stroke);
        return values.Item(ContentItem.KeyOf(Template(0, 0), index)) with { Box = (x, y, node.Dx, node.Dy) };
    }

    /// <summary>A Sankey link's props: Recharts' link props (its ends, their control points' x and its width), its index and value, its nodes' names and its stroke.</summary>
    public static ContentItem SankeyLink(ContentValues values, SankeyPlot plot, int index, in LinkCurve curve, in Paint paint)
    {
        var link = plot.Links[index];
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5], numbers[6], numbers[7], numbers[8]) = (curve.SourceX, curve.SourceY, curve.SourceControlX, curve.TargetX, curve.TargetY, curve.TargetControlX, curve.Width, index, link.Value);
        var texts = values.Texts;
        (texts[0], texts[1], texts[2]) = (plot.Nodes[link.Source].Name, plot.Nodes[link.Target].Name, paint.Stroke);
        return values.Item(ContentItem.KeyOf(Template(0, 1), index)) with { Box = (curve.SourceX, Math.Min(curve.SourceY, curve.TargetY) - (curve.Width / 2), curve.TargetX - curve.SourceX, Math.Abs(curve.TargetY - curve.SourceY) + curve.Width) };
    }

    /// <summary>A brush traveller's props: its rectangle and the brush's fill and stroke.</summary>
    public static ContentItem Traveller(ContentValues values, (double X, double Y, double Width, double Height) box, string fill, string stroke, int index)
    {
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3]) = box;
        var texts = values.Texts;
        (texts[0], texts[1]) = (fill, stroke);
        return values.Item(ContentItem.KeyOf(Template(0, 2), index)) with { Box = box };
    }

    /// <summary>A reference line's props: its segment, stroke width and paint.</summary>
    public static ContentItem ReferenceLine(ContentValues values, double x1, double y1, double x2, double y2, in Paint paint, int order)
    {
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3], numbers[4]) = (x1, y1, x2, y2, double.IsNaN(paint.StrokeWidth) ? 1 : paint.StrokeWidth);
        var texts = values.Texts;
        (texts[0], texts[1]) = (paint.Stroke, paint.Fill);
        return values.Item(ContentItem.KeyOf(Template(order, 0), 0)) with { Box = (Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x2 - x1), Math.Abs(y2 - y1)) };
    }

    /// <summary>A reference area's props: its rectangle and paint.</summary>
    public static ContentItem ReferenceArea(ContentValues values, (double X, double Y, double Width, double Height) box, in Paint paint, int order)
    {
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3]) = box;
        var texts = values.Texts;
        (texts[0], texts[1]) = (paint.Fill, paint.Stroke);
        return values.Item(ContentItem.KeyOf(Template(order, 0), 0)) with { Box = box };
    }

    /// <summary>A reference dot's props: its center, radius and paint.</summary>
    public static ContentItem ReferenceDot(ContentValues values, double cx, double cy, double r, in Paint paint, int order)
    {
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2]) = (cx, cy, r);
        var texts = values.Texts;
        (texts[0], texts[1]) = (paint.Fill, paint.Stroke);
        return values.Item(ContentItem.KeyOf(Template(order, 0), 0)) with { Box = (cx - r, cy - r, r * 2, r * 2) };
    }

    /// <summary>A <c>Customized</c> child's props: the chart's size and its plot's box (Recharts' <c>width</c>, <c>height</c> and <c>offset</c>).</summary>
    public static ContentItem Layout(ContentValues values, ChartLayout layout, int order)
    {
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5]) = (layout.Spec.Width, layout.Spec.Height, layout.Left, layout.Top, layout.PlotWidth, layout.PlotHeight);
        return values.Item(ContentItem.KeyOf(Template(order, 0), 0)) with { Box = (layout.Left, layout.Top, layout.PlotWidth, layout.PlotHeight) };
    }

    /// <summary>A tooltip's props: its anchor, the active item's index, how many lines the default content would show, its label, and its first line's name, value and color; its data members from the active item.</summary>
    public static ContentItem Tooltip(ContentValues values, double x, double y, int index, string label, ReadOnlySpan<TooltipEntry> entries)
    {
        var numbers = values.Numbers;
        (numbers[0], numbers[1], numbers[2], numbers[3]) = (x, y, index, entries.Length);
        var texts = values.Texts;
        texts[0] = label;
        for (var line = 0; line < Content.ContentSlots.TooltipLines; line++)
        {
            var entry = line < entries.Length ? entries[line] : default;
            (texts[1 + (3 * line)], texts[2 + (3 * line)], texts[3 + (3 * line)]) = (entry.Name, entry.Value, entry.Color);
        }

        values.Read(index);
        return values.Item(ContentItem.KeyOf(Template(0, 3), Math.Max(index, 0))) with { Box = (x, y, 0, 0) };
    }

    /// <summary>A tooltip line's props: its index in the payload, its name, value and color.</summary>
    public static ContentItem TooltipItem(ContentValues values, int index, in TooltipEntry entry)
    {
        values.Numbers[0] = index;
        var texts = values.Texts;
        (texts[0], texts[1], texts[2]) = (entry.Name, entry.Value, entry.Color);
        return values.Item(ContentItem.KeyOf(Template(0, 7), index));
    }

    /// <summary>A legend entry's props: its index, whether it is toggled off, its text, color and icon.</summary>
    public static ContentItem LegendEntry(ContentValues values, in LegendItem item)
    {
        var numbers = values.Numbers;
        (numbers[0], numbers[1]) = (item.Index, item.Inactive ? 1 : 0);
        var texts = values.Texts;
        (texts[0], texts[1], texts[2], texts[3]) = (item.Name, item.Color, item.Icon.ToString().ToLowerInvariant(), null);
        return values.Item(ContentItem.KeyOf(Template(0, 4), item.Index)) with { Box = (item.X, item.Y, item.Width, 0) };
    }

    /// <summary>A legend's whole props: how many entries it lists, its width, its alignment and its layout.</summary>
    public static ContentItem Legend(ContentValues values, int count, double width, LegendSpec spec)
    {
        var numbers = values.Numbers;
        (numbers[0], numbers[1]) = (count, width);
        var texts = values.Texts;
        (texts[0], texts[1]) = (spec.Align, spec.Vertical ? "vertical" : "horizontal");
        return values.Item(ContentItem.KeyOf(Template(0, 6), 0));
    }

    /// <summary>A nest treemap's index entry's props: its place in the trail, its depth (its path's length) and its name.</summary>
    public static ContentItem NestIndex(ContentValues values, int index, in NestBox box)
    {
        var numbers = values.Numbers;
        (numbers[0], numbers[1]) = (index, box.Path.Length == 0 ? 0 : box.Path.Count(c => c == '/') + 1);
        values.Texts[0] = box.Name;
        return values.Item(ContentItem.KeyOf(Template(0, 5), index));
    }
}
