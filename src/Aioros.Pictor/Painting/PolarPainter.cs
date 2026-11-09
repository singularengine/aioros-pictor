using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>
/// Paints a polar chart in Recharts' element order: the surface, the polar grid, the axes, the pies, radars and radial
/// bars in the order the XAML declares them (a radial bar chart in Recharts 3's z-index layers), the hover layer, then
/// the legend. One painter for both targets.
/// </summary>
internal static class PolarPainter
{
    private const double Epsilon = 1e-5;

    public static void Paint<TCanvas>(ref TCanvas canvas, PolarLayout layout, int active)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Begin(layout);
        foreach (var (kind, index) in Order(layout))
        {
            switch (kind)
            {
                case 0:
                    Grid(ref canvas, layout, layout.Polar.Grid!);
                    break;
                case 1:
                    AngleAxis(ref canvas, layout, layout.Polar.AngleAxis!, layout.AngleTicks);
                    break;
                case 2:
                    RadiusAxis(ref canvas, layout, layout.Polar.RadiusAxis!, layout.RadiusTicks);
                    break;
                case 6:
                    AngleAxis(ref canvas, layout, layout.Polar.MoreAngleAxes[index], layout.MoreAngleTicks[index]);
                    break;
                case 7:
                    RadiusAxis(ref canvas, layout, layout.Polar.MoreRadiusAxes[index], layout.MoreRadiusTicks[index]);
                    break;
                case 3:
                    PiePainter.Pie(ref canvas, layout.Pies[index], layout.Spec, layout.PieOffset(layout.Pies[index]));
                    break;
                case 4:
                    Toggled(ref canvas, layout, layout.RadarTarget(layout.Radars[index]), layout.Radars[index]);
                    break;
                case 8:
                    RadialBarPainter.Labels(ref canvas, layout, layout.RadialBars[index]);
                    break;
                default:
                    RadialBarPainter.RadialBar(ref canvas, layout, layout.RadialBars[index]);
                    break;
            }
        }

        if (active != ChartPainter.NoTips && layout.HoverCount > 0)
        {
            PolarTips.Hits(ref canvas, layout, active);
            PolarTips.Tips(ref canvas, layout, active);
        }

        canvas.EndSurface();
        if (layout.Legend is { } legend)
        {
            canvas.Legend(legend);
        }

        canvas.End();
    }

    private static List<(int Kind, int Index)> Order(PolarLayout layout)
    {
        var polar = layout.Polar;
        var order = new List<(int Order, int Kind, int Index)>();
        if (polar.Grid is { } grid && layout.Spec.Kind != ChartKind.Pie)
        {
            order.Add((grid.Order, 0, 0));
        }

        if (polar.AngleAxis is { } angle && layout.Spec.Kind != ChartKind.Pie)
        {
            order.Add((angle.Order, 1, 0));
        }

        if (polar.RadiusAxis is { } radius && layout.Spec.Kind != ChartKind.Pie)
        {
            order.Add((radius.Order, 2, 0));
        }

        for (var i = 0; layout.Spec.Kind != ChartKind.Pie && i < polar.MoreAngleAxes.Length; i++)
        {
            order.Add((polar.MoreAngleAxes[i].Order, 6, i));
        }

        for (var i = 0; layout.Spec.Kind != ChartKind.Pie && i < polar.MoreRadiusAxes.Length; i++)
        {
            order.Add((polar.MoreRadiusAxes[i].Order, 7, i));
        }

        order.AddRange(layout.Pies.Select((pie, i) => (pie.Spec.Order, 3, i)));
        order.AddRange(layout.Radars.Select((radar, i) => (radar.Spec.Order, 4, i)));
        order.AddRange(layout.RadialBars.Select((bar, i) => (bar.Spec.Order, 5, i)));
        order.AddRange(layout.RadialBars.Select((bar, i) => (bar.Spec.Order, 8, i)));
        order.Sort((a, b) => Layer(layout, a.Kind) != Layer(layout, b.Kind) ? Layer(layout, a.Kind).CompareTo(Layer(layout, b.Kind)) : a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Kind.CompareTo(b.Kind));
        return order.ConvertAll(static entry => (entry.Kind, entry.Index));
    }

    /// <summary>
    /// The z-index layer Recharts 3 draws an element of a radial bar chart in (<c>DefaultZIndexes</c>): the grid, the
    /// radial bars, the axes, then the label lists; every element of another polar chart in one layer, in the order the
    /// XAML declares them, as Recharts 2 draws it.
    /// </summary>
    private static int Layer(PolarLayout layout, int kind) => !layout.Recharts3 ? 0 : kind switch
    {
        0 => -100,
        5 => 300,
        8 => 2000,
        _ => 500,
    };

    private static void Grid<TCanvas>(ref TCanvas canvas, PolarLayout layout, PolarGridSpec grid)
        where TCanvas : struct, IChartCanvas
    {
        if (layout.OuterRadius <= 0)
        {
            return;
        }

        var angles = grid.PolarAngles.IsDefaultOrEmpty ? Coordinates(layout.AngleTicks) : [.. grid.PolarAngles];
        var radii = grid.PolarRadius.IsDefaultOrEmpty ? Coordinates(layout.RadiusTicks) : [.. grid.PolarRadius];
        var lines = new Paint(grid.Stroke ?? layout.Spec.Colors.PolarGridStroke, grid.StrokeWidth ?? double.NaN, grid.Fill, double.NaN, grid.StrokeDashArray, grid.StrokeOpacity ?? double.NaN);
        var concentric = lines with { Fill = "none" };
        canvas.Open("recharts-polar-grid"u8);
        if (grid.RadialLines && angles.Length > 0)
        {
            canvas.Open("recharts-polar-grid-angle"u8);
            foreach (var angle in angles)
            {
                var (x1, y1) = Layout.Polar.Point(layout.Cx, layout.Cy, layout.InnerRadius, angle);
                var (x2, y2) = Layout.Polar.Point(layout.Cx, layout.Cy, layout.OuterRadius, angle);
                canvas.Line(x1, y1, x2, y2, lines, default);
            }

            canvas.Close();
        }

        if (radii.Length > 0)
        {
            canvas.Open("recharts-polar-grid-concentric"u8);
            foreach (var radius in radii)
            {
                if (grid.Circle)
                {
                    canvas.Circle(layout.Cx, layout.Cy, radius, concentric, "recharts-polar-grid-concentric-circle"u8);
                }
                else
                {
                    canvas.Shape(Ring(layout, angles, radius, repeatFirst: false), concentric, "recharts-polar-grid-concentric-polygon"u8);
                }
            }

            canvas.Close();
        }

        canvas.Close();
    }

    private static double[] Coordinates(PolarTick[] ticks)
    {
        var coordinates = new double[ticks.Length];
        for (var i = 0; i < ticks.Length; i++)
        {
            coordinates[i] = ticks[i].Coordinate;
        }

        return coordinates;
    }

    private static PolygonShape Ring(PolarLayout layout, double[] angles, double radius, bool repeatFirst = true)
    {
        var xs = new double[angles.Length];
        var ys = new double[angles.Length];
        for (var i = 0; i < xs.Length; i++)
        {
            (xs[i], ys[i]) = Layout.Polar.Point(layout.Cx, layout.Cy, radius, angles[i]);
        }

        return new PolygonShape(xs, ys, repeatFirst);
    }

    private static void AngleAxis<TCanvas>(ref TCanvas canvas, PolarLayout layout, PolarAngleAxisSpec axis, PolarTick[] ticks)
        where TCanvas : struct, IChartCanvas
    {
        var radius = layout.OuterRadius;
        if (radius <= 0 || ticks.Length == 0)
        {
            return;
        }

        if (layout.Recharts3)
        {
            ticks = [.. ticks.DistinctBy(static tick => (tick.Coordinate + 360) % 360)];
        }

        var axisLine = Line(axis.Stroke, axis.AxisLineStyle);
        var line = Line(axis.Stroke, axis.TickLineStyle);
        canvas.Open("recharts-layer recharts-polar-angle-axis angleAxis"u8);
        if (axis.AxisLine)
        {
            if (axis.CircleLine)
            {
                canvas.Circle(layout.Cx, layout.Cy, radius, axisLine, "recharts-dot angleAxis"u8);
            }
            else
            {
                canvas.Shape(Ring(layout, Coordinates(ticks), radius), axisLine, "recharts-polygon angleAxis"u8);
            }
        }

        canvas.Open("recharts-layer recharts-polar-angle-axis-ticks"u8);
        var size = axis.Inner ? -axis.TickSize : axis.TickSize;
        var content = ContentProps.Of(axis.TickContent, Content.ContentSlots.Tick, null);
        for (var i = 0; i < ticks.Length; i++)
        {
            AngleTick(ref canvas, layout, axis, ticks, i, (radius, size, line), content);
        }

        canvas.Close();
        canvas.Close();
    }

    /// <summary>An angle axis' tick: its line from the radius out by the tick size, then its label or its custom content past the line.</summary>
    private static void AngleTick<TCanvas>(ref TCanvas canvas, PolarLayout layout, PolarAngleAxisSpec axis, PolarTick[] ticks, int i, (double Radius, double Size, Paint Line) look, Content.ContentValues? content)
        where TCanvas : struct, IChartCanvas
    {
        var tick = ticks[i];
        canvas.Open("recharts-layer recharts-polar-angle-axis-tick"u8);
        var (x1, y1) = Layout.Polar.Point(layout.Cx, layout.Cy, look.Radius, tick.Coordinate);
        var (x2, y2) = Layout.Polar.Point(layout.Cx, layout.Cy, look.Radius + look.Size, tick.Coordinate);
        if (axis.TickLine)
        {
            canvas.Line(x1, y1, x2, y2, look.Line, "angleAxis"u8);
        }

        var fill = axis.TickFill ?? axis.Stroke ?? layout.Spec.Colors.LabelText;
        if (axis.Tick && content is not null)
        {
            var props = new PolarTickProps(x2, y2, i, tick.Coordinate, ticks.Length, layout.Cx, layout.Cy, look.Radius, 0, tick.Text, fill, axis.Stroke ?? layout.Spec.Colors.PolarAxisStroke, Anchor(tick.Coordinate, axis.Inner), axis.Inner ? "inner" : "outer");
            canvas.Content(content.Spec, ContentProps.PolarTick(content, props, axis.Order));
        }
        else if (axis.Tick)
        {
            canvas.Text(x2, y2, tick.Text, new TextPaint(Anchor(tick.Coordinate, axis.Inner), layout.Recharts3 ? VerticalAnchor(tick.Coordinate) : LabelPlacement.End, fill, axis.TickFontSize ?? layout.Spec.FontSize, OwnSize: axis.TickFontSize is not null), "recharts-text recharts-polar-angle-axis-tick-value"u8);
        }

        canvas.Close();
    }

    /// <summary>A polar axis line's paint: the axis' stroke under the line's own props object; never filled.</summary>
    private static Paint Line(string? stroke, LineStyle? style) =>
        new(style?.Stroke ?? stroke, style?.StrokeWidth ?? double.NaN, "none", double.NaN, style?.StrokeDashArray);

    /// <summary>Recharts 3's <c>getTickTextVerticalAnchor</c>: an angle tick's label hangs below its point in the top quarter, sits above it in the bottom one and is centered on it at the sides.</summary>
    private static double VerticalAnchor(double angle)
    {
        var cos = Math.Cos(-angle * Layout.Polar.Radian);
        if (Math.Abs(cos) <= Math.Cos(45 * Layout.Polar.Radian))
        {
            return Math.Sin(-angle * Layout.Polar.Radian) > 0 ? LabelPlacement.Start : LabelPlacement.End;
        }

        return LabelPlacement.Middle;
    }

    private static TextAnchor Anchor(double angle, bool inner)
    {
        var cos = Math.Cos(-angle * Layout.Polar.Radian);
        if (cos > Epsilon)
        {
            return inner ? TextAnchor.End : TextAnchor.Start;
        }

        if (cos < -Epsilon)
        {
            return inner ? TextAnchor.Start : TextAnchor.End;
        }

        return TextAnchor.Middle;
    }

    private static void RadiusAxis<TCanvas>(ref TCanvas canvas, PolarLayout layout, PolarRadiusAxisSpec axis, PolarTick[] ticks)
        where TCanvas : struct, IChartCanvas
    {
        if (ticks.Length == 0)
        {
            return;
        }

        canvas.Open("recharts-layer recharts-polar-radius-axis radiusAxis"u8);
        var min = ticks.Min(tick => tick.Coordinate);
        var max = ticks.Max(tick => tick.Coordinate);
        if (axis.AxisLine)
        {
            var (x1, y1) = Layout.Polar.Point(layout.Cx, layout.Cy, min, axis.Angle);
            var (x2, y2) = Layout.Polar.Point(layout.Cx, layout.Cy, max, axis.Angle);
            canvas.Line(x1, y1, x2, y2, Line(axis.Stroke ?? layout.Spec.Colors.PolarAxisStroke, axis.AxisLineStyle), "radiusAxis"u8);
        }

        if (axis.Tick)
        {
            var anchor = axis.Orientation switch
            {
                "left" => TextAnchor.End,
                "right" => TextAnchor.Start,
                _ => TextAnchor.Middle,
            };
            canvas.Open("recharts-layer recharts-polar-radius-axis-ticks"u8);
            var content = ContentProps.Of(axis.TickContent, Content.ContentSlots.Tick, null);
            for (var i = 0; i < ticks.Length; i++)
            {
                var tick = ticks[i];
                canvas.Open("recharts-layer recharts-polar-radius-axis-tick"u8);
                var (x, y) = Layout.Polar.Point(layout.Cx, layout.Cy, tick.Coordinate, axis.Angle);
                var fill = axis.TickFill ?? axis.Stroke ?? layout.Spec.Colors.PolarTickText;
                if (content is not null)
                {
                    var props = new PolarTickProps(x, y, i, tick.Coordinate, ticks.Length, layout.Cx, layout.Cy, 0, axis.Angle, tick.Text, fill, axis.Stroke ?? layout.Spec.Colors.PolarAxisStroke, anchor, axis.Orientation ?? "right");
                    canvas.Content(content.Spec, ContentProps.PolarTick(content, props, axis.Order));
                    canvas.Close();
                    continue;
                }

                canvas.Text(x, y, tick.Text, new TextPaint(anchor, LabelPlacement.End, fill, axis.TickFontSize ?? layout.Spec.FontSize, 90 - axis.Angle, OwnSize: axis.TickFontSize is not null), "recharts-text recharts-polar-radius-axis-tick-value"u8);
                canvas.Close();
            }

            canvas.Close();
        }

        var box = new PolarBox(layout.Cx, layout.Cy, min, max, axis.Angle, axis.Angle);
        RadiusLabel(ref canvas, axis.Label, box, layout.Spec.FontSize, layout.Spec.Colors, ContentProps.Template(axis.Order, 4));
        for (var i = 0; i < axis.Labels.Length; i++)
        {
            RadiusLabel(ref canvas, axis.Labels[i], box, layout.Spec.FontSize, layout.Spec.Colors, ContentProps.Template(axis.Order, 5 + i));
        }

        canvas.Close();
    }

    /// <summary>
    /// A radius axis' label at its <c>viewBox</c> (the axis' radii at its angle), or its custom content there: gray as
    /// Recharts' <c>Text</c>, or black along its arc, where Recharts writes a bare <c>text</c> without a fill.
    /// </summary>
    private static void RadiusLabel<TCanvas>(ref TCanvas canvas, LabelSpec? label, in PolarBox box, double fontSize, Theming.ChartTheme colors, int template)
        where TCanvas : struct, IChartCanvas
    {
        if (label?.Value is { } text)
        {
            var fill = (label.PositionX is null || label.PositionY is null) && label.Position is LabelPosition.InsideStart or LabelPosition.InsideEnd or LabelPosition.End ? colors.RadialLabelText : colors.LabelText;
            var content = ContentProps.Label(ContentProps.Of(label.Content, Content.ContentSlots.Label, null), template, 0, double.NaN, null);
            LabelPainter.Polar(ref canvas, label, ChartPainter.Formatted(text, label.Format), box, fontSize, fill, content: content);
        }
    }

    private static void Toggled<TCanvas>(ref TCanvas canvas, PolarLayout layout, int target, RadarLayout radar)
        where TCanvas : struct, IChartCanvas
    {
        if (Toggles.Concealed(layout.Spec, target))
        {
            return;
        }

        var marked = Toggles.Open(ref canvas, layout.Spec, target);
        Radar(ref canvas, layout, radar);
        if (marked)
        {
            canvas.Close();
        }
    }

    private static void Radar<TCanvas>(ref TCanvas canvas, PolarLayout layout, RadarLayout radar)
        where TCanvas : struct, IChartCanvas
    {
        var spec = radar.Spec;
        var stroked = new Paint(spec.Stroke, spec.StrokeWidth ?? double.NaN, spec.Fill, spec.FillOpacity ?? double.NaN);
        var timing = Motion.Animations.Of(radar);
        if (timing is { } value)
        {
            if (!radar.IsRange)
            {
                canvas.MorphLayer(new Motion.MorphLayer(Motion.MorphKind.Polygon, 0, true, value, layout.Data.Key()));
            }

            canvas.OpenMotion("recharts-layer recharts-radar"u8, value);
        }
        else
        {
            canvas.Open("recharts-layer recharts-radar"u8);
        }

        canvas.Open("recharts-layer recharts-radar-polygon"u8);
        var polygon = new PolygonShape(radar.X, radar.Y, connectNulls: spec.ConnectNulls);
        if (timing is not null)
        {
            canvas.Animate(new Motion.Step(Motion.StepKind.Center, layout.Cx, layout.Cy));
        }

        if (ContentProps.Of(spec.ShapeContent, Content.ContentSlots.Radar, layout.Data) is { } content)
        {
            canvas.Content(content.Spec, ContentProps.Radar(content, layout, radar, stroked, ContentProps.Template(spec.Order, 0)));
        }
        else if (radar.IsRange)
        {
            RangePolygon(ref canvas, radar, stroked);
        }
        else
        {
            SeriesPainter.Morph(ref canvas, timing is null ? null : Motion.MorphMark.Points(Motion.MorphKind.Polygon, 0, radar.X, radar.Y, (layout.Cx, layout.Cy)));
            canvas.Shape(polygon, polygon.Closed ? stroked : stroked with { Fill = "none" }, "recharts-polygon"u8);
        }

        if (spec.Dot)
        {
            RadarDots(ref canvas, layout, radar, stroked, timing is not null);
        }

        canvas.Close();
        RadarLabels(ref canvas, layout, radar);
        canvas.Close();
    }

    /// <summary>A radar's dots, each coming from the center with its point while the radar animates in (Recharts draws them at the animated points).</summary>
    private static void RadarDots<TCanvas>(ref TCanvas canvas, PolarLayout layout, RadarLayout radar, in Paint stroked, bool animated)
        where TCanvas : struct, IChartCanvas
    {
        var (radius, dot) = SeriesPainter.DotPaint(radar.Spec.DotProps, stroked);
        canvas.Open("recharts-layer recharts-radar-dots"u8);
        for (var i = 0; i < radar.X.Length; i++)
        {
            if (!double.IsFinite(radar.X[i]))
            {
                continue;
            }

            if (animated)
            {
                canvas.Animate(new Motion.Step(Motion.StepKind.Converge, layout.Cx - radar.X[i], layout.Cy - radar.Y[i]));
                SeriesPainter.Morph(ref canvas, radar.IsRange ? null : Motion.MorphMark.Numbers(Motion.MorphKind.Center, new Motion.Six(radar.X[i], radar.Y[i]), 2, new Motion.Six(layout.Cx, layout.Cy), 2));
            }

            canvas.Circle(radar.X[i], radar.Y[i], radius, dot, "recharts-dot recharts-radar-dot"u8);
        }

        canvas.Close();
    }

    private static void RadarLabels<TCanvas>(ref TCanvas canvas, PolarLayout layout, RadarLayout radar)
        where TCanvas : struct, IChartCanvas
    {
        var list = 0;
        foreach (var label in new LabelLists(radar.Spec.Labels, radar.Spec.LabelLists))
        {
            var content = ContentProps.Of(label.Content, Content.ContentSlots.Label, layout.Data);
            var template = ContentProps.Template(radar.Spec.Order, 4 + list++);
            canvas.Open("recharts-layer recharts-label-list"u8);
            for (var i = 0; i < radar.X.Length; i++)
            {
                var text = label.DataKey is { } key ? LabelPainter.Text(layout.Data, key, i, label.Format) : SeriesPainter.Value(radar.Values[i], label.Format);
                if (double.IsFinite(radar.X[i]) && !string.IsNullOrEmpty(text))
                {
                    LabelPainter.Cartesian(ref canvas, label, text, (radar.X[i], radar.Y[i], 0, 0), layout.Spec.FontSize, layout.Spec.Colors.LabelText, angle: i < layout.CategoryTicks.Length ? layout.CategoryTicks[i].Coordinate : 0, content: ContentProps.Label(content, template, i, radar.Values[i], radar.Spec.Name ?? radar.Spec.DataKey));
                }
            }

            canvas.Close();
        }
    }

    /// <summary>Recharts' range <c>Polygon</c>: the band between the upper and lower ends filled, then both edges stroked when the radar has a stroke.</summary>
    private static void RangePolygon<TCanvas>(ref TCanvas canvas, RadarLayout radar, Paint paint)
        where TCanvas : struct, IChartCanvas
    {
        var band = new RangePolygonShape(radar.X, radar.Y, radar.BaseX, radar.BaseY);
        canvas.Open("recharts-polygon"u8);
        canvas.Shape(band, paint with { Stroke = "none", Fill = band.Closed ? paint.Fill : "none" }, default);
        if (paint.Stroke is { } stroke && stroke != "none")
        {
            canvas.Shape(new PolygonShape(radar.X, radar.Y, connectNulls: radar.Spec.ConnectNulls), paint with { Fill = "none" }, default);
            var reversedX = new double[radar.BaseX.Length];
            var reversedY = new double[radar.BaseY.Length];
            for (var i = 0; i < reversedX.Length; i++)
            {
                (reversedX[i], reversedY[i]) = (radar.BaseX[^(i + 1)], radar.BaseY[^(i + 1)]);
            }

            canvas.Shape(new PolygonShape(reversedX, reversedY, connectNulls: radar.Spec.ConnectNulls), paint with { Fill = "none" }, default);
        }

        canvas.Close();
    }
}
