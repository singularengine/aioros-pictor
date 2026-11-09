using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>
/// Recharts' hover states of the polar charts: per pie sector (Recharts' <c>item</c> tooltip, with the active shape when
/// the pie has one), per angle category of a radar (the cursor line, the active dots) and per band of radial bars (the
/// cursor arc). The web paints them all hidden behind their hit shapes; the desktop paints the hovered one.
/// </summary>
internal static class PolarTips
{
    public static void Hits<TCanvas>(ref TCanvas canvas, PolarLayout layout, int active)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Open(TipPainter.HitsLayer(layout.Spec.Tooltip!));
        for (var i = 0; i < layout.HoverCount; i++)
        {
            if (active < 0 || i == active)
            {
                canvas.Open(default);
                Hit(ref canvas, layout, i);
                canvas.Close();
            }
        }

        canvas.Close();
    }

    public static void Tips<TCanvas>(ref TCanvas canvas, PolarLayout layout, int active, (double X, double Y)? pointer = null)
        where TCanvas : struct, IChartCanvas
    {
        var buffers = Buffers(layout);
        canvas.Open("pictor-tips"u8);
        for (var i = 0; i < layout.HoverCount; i++)
        {
            if (active < 0 || i == active)
            {
                canvas.Open(default);
                Tip(ref canvas, layout, i, buffers, pointer);
                canvas.Close();
            }
        }

        canvas.Close();
    }

    /// <summary>The desktop's hover state: the cursor, the active shape or dots and the tooltip of one category or item.</summary>
    public static void Hover<TCanvas>(ref TCanvas canvas, PolarLayout layout, int active, (double X, double Y)? pointer = null)
        where TCanvas : struct, IChartCanvas
    {
        if (active < 0 || active >= layout.HoverCount)
        {
            return;
        }

        if (layout.Spec.Kind != ChartKind.Pie && !layout.ItemBars)
        {
            Cursor(ref canvas, layout, active);
        }

        Tip(ref canvas, layout, active, Buffers(layout), pointer);
    }

    /// <summary>The category or item under a point, or <see cref="ChartPainter.NoTips"/>: Recharts' <c>inRangeOfSector</c> and active tick.</summary>
    public static int Hovered(PolarLayout layout, double x, double y)
    {
        if (layout.HoverCount == 0)
        {
            return ChartPainter.NoTips;
        }

        if (layout.ItemBars)
        {
            return BarUnder(layout, x, y);
        }

        if (layout.Spec.Kind == ChartKind.Pie)
        {
            var index = 0;
            foreach (var shape in layout.Pies.SelectMany(pie => pie.Sectors).Select(sector => sector.Shape))
            {
                if (!Toggles.Concealed(layout.Spec, index) && !double.IsNaN(Layout.Polar.InSector(x, y, shape.Cx, shape.Cy, shape.InnerRadius, shape.OuterRadius, shape.StartAngle, shape.EndAngle)))
                {
                    return index;
                }

                index++;
            }

            return ChartPainter.NoTips;
        }

        var angle = Layout.Polar.InSector(x, y, layout.Cx, layout.Cy, layout.InnerRadius, layout.OuterRadius, layout.StartAngle, layout.EndAngle);
        if (double.IsNaN(angle))
        {
            return ChartPainter.NoTips;
        }

        var hovered = layout.Centric ? NearestAngle(layout, angle) : Band(layout, Layout.Polar.AngleOf(x, y, layout.Cx, layout.Cy).Radius);
        return !layout.Centric && hovered >= 0 && Toggles.Concealed(layout.Spec, hovered) ? ChartPainter.NoTips : hovered;
    }

    private static int NearestAngle(PolarLayout layout, double angle)
    {
        var nearest = 0;
        var distance = double.PositiveInfinity;
        for (var i = 0; i < layout.CategoryTicks.Length; i++)
        {
            var gap = Math.Abs(((((angle - layout.CategoryTicks[i].Coordinate) % 360) + 540) % 360) - 180);
            if (gap < distance)
            {
                (nearest, distance) = (i, gap);
            }
        }

        return nearest;
    }

    private static int Band(PolarLayout layout, double radius)
    {
        var ticks = layout.CategoryTicks;
        for (var i = 0; i < ticks.Length; i++)
        {
            var (low, high) = Zone(layout, i);
            if (radius > low && radius <= high)
            {
                return i;
            }
        }

        return ChartPainter.NoTips;
    }

    private static (double Low, double High) Zone(PolarLayout layout, int index)
    {
        var ticks = layout.CategoryTicks;
        var low = index == 0 ? layout.InnerRadius : (ticks[index - 1].Coordinate + ticks[index].Coordinate) / 2;
        var high = index == ticks.Length - 1 ? layout.OuterRadius : (ticks[index].Coordinate + ticks[index + 1].Coordinate) / 2;
        return (low, high);
    }

    /// <summary>The radial bar under a point (the last drawn), or <see cref="ChartPainter.NoTips"/>.</summary>
    private static int BarUnder(PolarLayout layout, double x, double y)
    {
        var found = ChartPainter.NoTips;
        var state = 0;
        for (var b = 0; b < layout.RadialBars.Count; b++)
        {
            var sectors = layout.RadialBars[b].Sectors;
            for (var i = 0; i < sectors.Length; i++)
            {
                var shape = sectors[i].Shape;
                if (!shape.Drawn)
                {
                    continue;
                }

                found = !Toggles.Concealed(layout.Spec, i) && !double.IsNaN(Layout.Polar.InSector(x, y, shape.Cx, shape.Cy, shape.InnerRadius, shape.OuterRadius, shape.StartAngle, shape.EndAngle)) ? state : found;
                state++;
            }
        }

        return found;
    }

    /// <summary>The radial bar of an item hover state: its series and its index.</summary>
    private static (RadialBarLayout Bar, int Index) Bar(PolarLayout layout, int state) => layout.ItemState(state);

    private static void Hit<TCanvas>(ref TCanvas canvas, PolarLayout layout, int index)
        where TCanvas : struct, IChartCanvas
    {
        if (layout.ItemBars)
        {
            var (bar, sector) = Bar(layout, index);
            var marked = Toggles.Open(ref canvas, layout.Spec, sector);
            canvas.HitShape(bar.Sectors[sector].Shape);
            if (marked)
            {
                canvas.Close();
            }

            return;
        }

        switch (layout.Spec.Kind)
        {
            case ChartKind.Pie:
                var (pie, sector) = Locate(layout, index);
                var marked = Toggles.Open(ref canvas, layout.Spec, index);
                canvas.HitShape(pie.Sectors[sector].Shape);
                if (marked)
                {
                    canvas.Close();
                }

                break;
            case ChartKind.RadialBar when !layout.Centric:
                var (low, high) = Zone(layout, index);
                var toggled = Toggles.Open(ref canvas, layout.Spec, index);
                canvas.HitShape(new SectorShape(layout.Cx, layout.Cy, low, high, layout.StartAngle, layout.EndAngle));
                Cursor(ref canvas, layout, index);
                if (toggled)
                {
                    canvas.Close();
                }

                break;
            default:
                var step = Math.Abs(layout.EndAngle - layout.StartAngle) / Math.Max(1, layout.CategoryTicks.Length);
                var angle = layout.CategoryTicks[index].Coordinate;
                canvas.HitShape(new SectorShape(layout.Cx, layout.Cy, layout.InnerRadius, layout.OuterRadius, angle - (step / 2), angle + (step / 2)));
                Cursor(ref canvas, layout, index);
                break;
        }
    }

    private static void Cursor<TCanvas>(ref TCanvas canvas, PolarLayout layout, int index)
        where TCanvas : struct, IChartCanvas
    {
        if (!TipPainter.ShowsCursor(layout.Spec.Tooltip!))
        {
            return;
        }

        if (layout.Centric)
        {
            var angle = layout.CategoryTicks[index].Coordinate;
            var (x1, y1) = Layout.Polar.Point(layout.Cx, layout.Cy, layout.InnerRadius, angle);
            var (x2, y2) = Layout.Polar.Point(layout.Cx, layout.Cy, layout.OuterRadius, angle);
            canvas.Shape(new SegmentShape(x1, y1, x2, y2), TipPainter.CursorPaint(layout.Spec.Tooltip!, band: false, layout.Spec.Colors), "recharts-curve recharts-tooltip-cursor"u8);
            return;
        }

        var radius = layout.CategoryTicks[index].Coordinate;
        canvas.Shape(new SectorShape(layout.Cx, layout.Cy, radius, radius, layout.StartAngle, layout.EndAngle), TipPainter.CursorPaint(layout.Spec.Tooltip!, band: false, layout.Spec.Colors) with { Fill = layout.Spec.Tooltip!.CursorFill }, "recharts-sector recharts-tooltip-cursor"u8);
    }

    private static (PieLayout Pie, int Sector) Locate(PolarLayout layout, int index)
    {
        foreach (var pie in layout.Pies)
        {
            if (index < pie.Sectors.Length)
            {
                return (pie, index);
            }

            index -= pie.Sectors.Length;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <summary>The tooltip items and lines of one pass over the hover states, allocated once for all of them.</summary>
    private static (TooltipItem[] Items, TooltipEntry[] Entries) Buffers(PolarLayout layout)
    {
        var size = Math.Max(1, Math.Max(layout.Radars.Count, layout.RadialBars.Count));
        return (new TooltipItem[size], new TooltipEntry[size]);
    }

    private static void Tip<TCanvas>(ref TCanvas canvas, PolarLayout layout, int index, (TooltipItem[] Items, TooltipEntry[] Entries) buffers, (double X, double Y)? pointer)
        where TCanvas : struct, IChartCanvas
    {
        if (layout.ItemBars)
        {
            var (bar, i) = Bar(layout, index);
            var tooltip = layout.Spec.Tooltip!;
            buffers.Items[0] = new TooltipItem(bar.Spec.Name ?? bar.Spec.DataKey, bar.Spec.DataKey, bar.Sectors[i].Value, null, bar.Spec.Fill ?? layout.Spec.Colors.TooltipText, None: bar.Spec.TooltipNone);
            Box(ref canvas, layout, 0, 0, string.Empty, buffers.Items.AsSpan(0, 1), buffers.Entries, index: i, spec: tooltip with { PositionX = tooltip.PositionX ?? 0, PositionY = tooltip.PositionY ?? 0 });
            return;
        }

        switch (layout.Spec.Kind)
        {
            case ChartKind.Pie:
                var (pie, sector) = Locate(layout, index);
                if ((pie.Spec.ActiveShape is not null || pie.Spec.ActiveShapeContent is not null) && !PiePainter.HasActive(pie.Spec))
                {
                    PiePainter.Active(ref canvas, pie, sector);
                }

                var item = pie.Items[sector];
                var place = pie.Sectors[sector];
                buffers.Items[0] = new TooltipItem(item.Name, pie.Spec.DataKey, item.Value, null, layout.Spec.Colors.TooltipText, None: pie.Spec.TooltipNone);
                Box(ref canvas, layout, place.TooltipX, place.TooltipY, string.Empty, buffers.Items.AsSpan(0, 1), buffers.Entries, index: sector, data: pie.Data);
                break;
            case ChartKind.RadialBar:
                var bars = buffers.Items;
                var shown = layout.ItemAt(index);
                for (var i = 0; i < layout.RadialBars.Count; i++)
                {
                    var bar = layout.RadialBars[i];
                    if (shown >= 0)
                    {
                        ActiveBar(ref canvas, layout, bar, shown);
                    }

                    bars[i] = new TooltipItem(bar.Spec.Name ?? bar.Spec.DataKey, bar.Spec.DataKey, shown >= 0 && shown < bar.Sectors.Length ? bar.Sectors[shown].Value : double.NaN, null, bar.Spec.Fill ?? layout.Spec.Colors.TooltipText, None: bar.Spec.TooltipNone);
                }

                var (bx, by, follow) = Followed(layout, index, pointer);
                Box(ref canvas, layout, bx, by, layout.Categories[index], bars.AsSpan(0, layout.RadialBars.Count), buffers.Entries, follow, index);
                break;
            default:
                Radar(ref canvas, layout, index, buffers, pointer);
                break;
        }
    }

    private static void Radar<TCanvas>(ref TCanvas canvas, PolarLayout layout, int index, (TooltipItem[] Items, TooltipEntry[] Entries) buffers, (double X, double Y)? pointer)
        where TCanvas : struct, IChartCanvas
    {
        var items = buffers.Items;
        var count = 0;
        var marked = Toggles.Marked(layout.Spec);
        var item = layout.ItemAt(index);
        foreach (var radar in layout.Radars)
        {
            var target = layout.RadarTarget(radar);
            if (Toggles.Concealed(layout.Spec, target) || item < 0 || item >= radar.Values.Length)
            {
                continue;
            }

            if (radar.Spec.ActiveDot && double.IsFinite(radar.X[item]))
            {
                var wrapped = Toggles.Open(ref canvas, layout.Spec, target);
                canvas.Open("recharts-layer recharts-active-dot"u8);
                var style = radar.Spec.ActiveDotStyle;
                canvas.Circle(radar.X[item], radar.Y[item], radar.Spec.ActiveDotRadius, new Paint(style?.Stroke ?? layout.Spec.Colors.ActiveDotStroke, style?.StrokeWidth ?? 2, style?.Fill ?? radar.Color), "recharts-dot"u8);
                canvas.Close();
                if (wrapped)
                {
                    canvas.Close();
                }
            }

            items[count++] = new TooltipItem(radar.Name, radar.Spec.DataKey, radar.Values[item], radar.Spec.Unit, radar.Color, marked ? target : -1, None: radar.Spec.TooltipNone, Formatted: radar.IsRange && !double.IsNaN(radar.Lows[item]) ? SeriesPainter.Value(radar.Lows[item], layout.Spec.Tooltip!.Format) + " ~ " + SeriesPainter.Value(radar.Values[item], layout.Spec.Tooltip.Format) : null);
        }

        var (x, y, follow) = Followed(layout, index, pointer);
        Box(ref canvas, layout, x, y, layout.Categories[index], items.AsSpan(0, count), buffers.Entries, follow, index);
    }

    /// <summary>
    /// Where a category's box stands (Recharts' polar <c>activeCoordinate</c>): in a centric chart (radars, centric radial
    /// bars) at the pointer's distance from the center along the category's angle, in a radial one at the pointer's angle on
    /// the category's radius; without a pointer at half the outer radius, or at the start angle. The web's box follows
    /// the pointer the same way.
    /// </summary>
    public static (double X, double Y, TipFollow Follow) Followed(PolarLayout layout, int index, (double X, double Y)? pointer)
    {
        var tick = layout.CategoryTicks[index].Coordinate;
        var viewBox = (layout.Left, layout.PlotWidth);
        if (layout.Centric)
        {
            var radius = pointer is { } at ? Math.Sqrt(Square(TipPainter.Round(at.X) - layout.Cx) + Square(TipPainter.Round(at.Y) - layout.Cy)) : layout.OuterRadius / 2;
            var (x, y) = Layout.Polar.Point(layout.Cx, layout.Cy, radius, tick);
            return (x, y, new TipFollow(TipAxis.Radius, viewBox.Left, viewBox.PlotWidth, layout.OuterRadius / 2, Param: tick));
        }

        var (px, py) = pointer is { } p ? (TipPainter.Round(p.X) - layout.Cx, TipPainter.Round(p.Y) - layout.Cy) : (0, 0);
        var distance = Math.Sqrt(Square(px) + Square(py));
        var (rx, ry) = distance > 0 ? (layout.Cx + (tick * px / distance), layout.Cy + (tick * py / distance)) : Layout.Polar.Point(layout.Cx, layout.Cy, tick, layout.StartAngle);
        return (rx, ry, new TipFollow(TipAxis.Angle, viewBox.Left, viewBox.PlotWidth, layout.StartAngle, Param: tick));
    }

    private static double Square(double value) => value * value;

    /// <summary>A polar tooltip's box: its default content, or the tooltip's template with the active item's index in the data (a pie's own data for a sector).</summary>
    private static void Box<TCanvas>(ref TCanvas canvas, PolarLayout layout, double x, double y, string label, Span<TooltipItem> items, Span<TooltipEntry> entries, TipFollow follow = default, int index = -1, Data.ChartData? data = null, TooltipSpec? spec = null)
        where TCanvas : struct, IChartCanvas
    {
        var tooltip = spec ?? layout.Spec.Tooltip!;
        var payload = TooltipBox.Finish(tooltip, items);
        var count = TooltipBox.Entries(tooltip, items[..payload], entries);
        TooltipBox.Write(ref canvas, tooltip, layout.Spec.FontSize, x, y, label, entries[..count], payload, (layout.Left, layout.Top, layout.PlotWidth, layout.PlotHeight), follow, ContentProps.Of(tooltip.Content, Content.ContentSlots.Tooltip, data ?? layout.Data), index);
    }

    /// <summary>A radial bar's active shape at the hovered category, when it has a template for it (Recharts' <c>activeShape</c> element), in its active layer.</summary>
    private static void ActiveBar<TCanvas>(ref TCanvas canvas, PolarLayout layout, RadialBarLayout bar, int index)
        where TCanvas : struct, IChartCanvas
    {
        if (index < bar.Sectors.Length && bar.Sectors[index].Shape.Drawn && ContentProps.Of(bar.Spec.ActiveShapeContent, Content.ContentSlots.Sector, layout.Data) is { } content)
        {
            canvas.Open("recharts-layer recharts-active-shape"u8);
            canvas.Content(content.Spec, ContentProps.RadialBar(content, bar, index, ContentProps.Template(bar.Spec.Order, 1), state: 1));
            canvas.Close();
        }
    }
}
