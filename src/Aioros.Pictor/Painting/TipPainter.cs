using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>
/// Recharts' hover state per category: the cursor (a line through the category, or its band in a bar chart), the active
/// bars and dots and the default tooltip, along x in a horizontal chart and down y in a vertical one; a scatter chart's
/// hover states are its points (<see cref="ItemTipPainter"/>) and a bar chart's with <c>shared={false}</c> its bars
/// (<see cref="BarTips"/>). The web paints every state hidden and shows one on hover (or on click) through CSS; the
/// desktop paints the hovered one.
/// </summary>
internal static class TipPainter
{
    /// <summary>The class of the layer of hit areas: <c>pictor-hits</c>, shown on hover, or <c>pictor-clicks</c>, shown on focus after a click.</summary>
    public static ReadOnlySpan<byte> HitsLayer(TooltipSpec tooltip) => tooltip.ClickTrigger ? "pictor-clicks"u8 : "pictor-hits"u8;

    /// <summary>Whether a hover state draws its cursor: the tooltip has one and is not inactive (Recharts' <c>active={false}</c> hides it).</summary>
    public static bool ShowsCursor(TooltipSpec tooltip) => tooltip.Cursor && tooltip.Active != false;

    public static void Cursors<TCanvas>(ref TCanvas canvas, ChartLayout layout, int active)
        where TCanvas : struct, IChartCanvas
    {
        if (layout.ItemTooltip)
        {
            ItemTipPainter.Cursors(ref canvas, layout, active);
            return;
        }

        if (layout.BarTooltip)
        {
            BarTips.Hits(ref canvas, layout, active);
            return;
        }

        var axis = layout.Categories!;
        var tooltip = layout.Spec.Tooltip!;
        canvas.Open(HitsLayer(tooltip));
        for (var i = 0; i < axis.Categories.Length; i++)
        {
            if (active >= 0 && i != active)
            {
                continue;
            }

            var (start, end) = Zone(layout, axis, i);
            canvas.Open(default);
            if (layout.Vertical)
            {
                canvas.Hit(layout.Left, start, layout.PlotWidth, end - start);
            }
            else
            {
                canvas.Hit(start, layout.Top, end - start, layout.PlotHeight);
            }

            if (ShowsCursor(tooltip))
            {
                Cursor(ref canvas, layout, axis, i);
            }

            canvas.Close();
        }

        canvas.Close();
    }

    /// <summary>
    /// The tips of the categories (or the hovered one's): each box at its category's tick and, across the axis, at the
    /// pointer (Recharts' <c>activeCoordinate</c> keeps the pointer's other coordinate), without one where Recharts
    /// places its default index; the web's boxes follow the pointer there through the framework's pointer variables. A
    /// click tooltip's boxes, which follow nothing on the web, stand at the plot's edge, where Recharts places them after
    /// a click at the plot's top (it keeps the click's coordinate, which the web's CSS cannot read).
    /// </summary>
    public static void Tips<TCanvas>(ref TCanvas canvas, ChartLayout layout, int active, (double X, double Y)? pointer = null)
        where TCanvas : struct, IChartCanvas
    {
        if (layout.ItemTooltip)
        {
            ItemTipPainter.Tips(ref canvas, layout, active);
            return;
        }

        if (layout.BarTooltip)
        {
            BarTips.Tips(ref canvas, layout, active);
            return;
        }

        var axis = layout.Categories!;
        var tooltip = layout.Spec.Tooltip!;
        var items = new TooltipItem[layout.Spec.Series.Length];
        var entries = new TooltipEntry[items.Length];
        var laid = new SeriesLayout?[items.Length];
        foreach (var layer in layout.Series)
        {
            laid[Specs.SpecIndex.Of(layout.Spec.Series, layer.Spec)] = layer;
        }

        var viewBox = (layout.Left, layout.Top, layout.PlotWidth, layout.PlotHeight);
        var content = ContentProps.Of(tooltip.Content, Content.ContentSlots.Tooltip, layout.Data);
        var follow = Follow(layout);
        var free = pointer is { } at ? Round(layout.Vertical ? at.X : at.Y) : tooltip.ClickTrigger ? follow.Start : follow.Rest;
        canvas.Open("pictor-tips"u8);
        for (var i = 0; i < axis.Categories.Length; i++)
        {
            if (active >= 0 && i != active)
            {
                continue;
            }

            canvas.Open(default);
            var payload = TooltipBox.Finish(tooltip, items.AsSpan(0, Items(ref canvas, layout, i, items, laid)));
            var count = TooltipBox.Entries(tooltip, items.AsSpan(0, payload), entries);
            var coordinate = axis.TickAt(i);
            var (x, y) = layout.Vertical ? (free, coordinate) : (coordinate, free);
            TooltipBox.Write(ref canvas, tooltip, layout.Spec.FontSize, x, y, axis.Categories[i] ?? string.Empty, entries.AsSpan(0, count), payload, viewBox, follow, content, i);
            canvas.Close();
        }

        canvas.Close();
    }

    /// <summary>
    /// How a category's tooltip follows the pointer: across the category axis, over the plot; without a pointer (the
    /// default index, the keyboard) halfway between the plot's start and the chart's far edge, as Recharts'
    /// <c>combineCoordinateForDefaultIndex</c> places it.
    /// </summary>
    public static TipFollow Follow(ChartLayout layout) => layout.Vertical
        ? new TipFollow(TipAxis.X, layout.Left, layout.PlotWidth, (layout.Left + layout.Spec.Width) / 2)
        : new TipFollow(TipAxis.Y, layout.Top, layout.PlotHeight, (layout.Top + layout.Spec.Height) / 2);

    /// <summary>JavaScript's <c>Math.round</c>, as Recharts rounds the pointer's position in the chart.</summary>
    public static double Round(double value) => Math.Floor(value + 0.5);

    /// <summary>
    /// The active shapes and tooltip items of the series at a category, in the series' order: a hidden series gives an
    /// item without a shape (the tooltip keeps it only with <c>includeHidden</c>); a series the desktop toggled off gives none.
    /// </summary>
    private static int Items<TCanvas>(ref TCanvas canvas, ChartLayout layout, int index, TooltipItem[] items, SeriesLayout?[] laid)
        where TCanvas : struct, IChartCanvas
    {
        var spec = layout.Spec;
        var marked = Toggles.Marked(spec);
        var count = 0;
        for (var k = 0; k < spec.Series.Length; k++)
        {
            var target = spec.ReverseStackOrder ? spec.Series.Length - 1 - k : k;
            var series = spec.Series[target];
            var layer = laid[target];
            var item = layer is null ? index : layout.ItemAt(layer, index);
            var values = layer?.Values ?? layout.SeriesData(series).Numbers(series.DataKey);
            if (item < 0 || item >= values.Length || Toggles.Concealed(spec, target))
            {
                continue;
            }

            if (layer is not null)
            {
                var wrapped = Toggles.Open(ref canvas, spec, target);
                Active(ref canvas, layer, item);
                if (wrapped)
                {
                    canvas.Close();
                }
            }

            items[count++] = Item(series, layer, values[item], item, marked ? target : -1, spec.Tooltip!.Format, spec.Colors);
        }

        return count;
    }

    /// <summary>A series' tooltip item at an item of its data: a range value written as Recharts writes it.</summary>
    private static TooltipItem Item(SeriesSpec series, SeriesLayout? layer, double value, int item, int target, string? format, Theming.ChartTheme theme)
    {
        var range = layer is not null && layer.Lows.Length > item && !double.IsNaN(layer.Lows[item]);
        return new TooltipItem(series.Name ?? series.DataKey, series.DataKey, value, series.Unit, SeriesLayout.ColorOf(series, theme), target, series.Hide, series.TooltipNone, range ? ValueText(layer!, item, format) : null);
    }

    /// <summary>A series' value in the tooltip: a range value as Recharts' default content writes it (<c>low ~ high</c>), or the value.</summary>
    public static string ValueText(SeriesLayout series, int index, string? format) =>
        series.Lows.Length > index && !double.IsNaN(series.Lows[index]) && !double.IsNaN(series.Values[index])
            ? SeriesPainter.Value(series.Lows[index], format) + " ~ " + SeriesPainter.Value(series.Values[index], format)
            : SeriesPainter.Value(series.Values[index], format);

    /// <summary>The hover state under a point of the plot: the nearest category, a scatter's point, a bar, or <see cref="ChartPainter.NoTips"/>.</summary>
    public static int Hovered(ChartLayout layout, double x, double y)
    {
        if (layout.HoverCount == 0 || x < layout.Left || x > layout.Left + layout.PlotWidth || y < layout.Top || y > layout.Top + layout.PlotHeight)
        {
            return ChartPainter.NoTips;
        }

        if (layout.ItemTooltip)
        {
            return ItemTipPainter.Hovered(layout, x, y);
        }

        if (layout.BarTooltip)
        {
            return BarTips.Hovered(layout, x, y);
        }

        var axis = layout.Categories!;
        var along = layout.Vertical ? y : x;
        var nearest = 0;
        var distance = double.PositiveInfinity;
        for (var i = 0; i < axis.Categories.Length; i++)
        {
            var gap = Math.Abs(axis.TickAt(i) - along);
            if (gap < distance)
            {
                (nearest, distance) = (i, gap);
            }
        }

        return nearest;
    }

    /// <summary>Recharts' active shapes of a series at a category: the bar drawn with its <c>activeBar</c> props, or the line's or area's active dot.</summary>
    private static void Active<TCanvas>(ref TCanvas canvas, SeriesLayout series, int index)
        where TCanvas : struct, IChartCanvas
    {
        var spec = series.Spec;
        if (spec.Kind == SeriesKind.Bar)
        {
            BarTips.ActiveBar(ref canvas, series, index);
            return;
        }

        if (spec.ActiveDot && spec.Kind != SeriesKind.Scatter && series.Defined[index])
        {
            var style = spec.ActiveDotStyle;
            var paint = new Paint(style?.Stroke ?? series.Theme.ActiveDotStroke, style?.StrokeWidth ?? 2, style?.Fill ?? series.Color);
            canvas.Open("recharts-layer recharts-active-dot"u8);
            if (ContentProps.Of(spec.ActiveDotContent, Content.ContentSlots.Dot, series.Data) is { } content)
            {
                canvas.Content(content.Spec, ContentProps.Dot(content, series, index, spec.ActiveDotRadius, paint, ContentProps.Template(spec.Order, 3), state: 1));
                canvas.Close();
                return;
            }

            canvas.Circle(series.X[index], series.Y[index], spec.ActiveDotRadius, paint, "recharts-dot"u8);
            canvas.Close();
            if (series.IsRange && !double.IsNaN(series.Base[index]))
            {
                var (bx, by) = series.ValueAxis.IsX ? (series.Base[index], series.Y[index]) : (series.X[index], series.Base[index]);
                canvas.Open("recharts-layer recharts-active-dot"u8);
                canvas.Circle(bx, by, spec.ActiveDotRadius, paint, "recharts-dot"u8);
                canvas.Close();
            }
        }
    }

    internal static (double Start, double End) Zone(ChartLayout layout, AxisLayout axis, int index)
    {
        var (first, last) = layout.Vertical ? (layout.Top, layout.Top + layout.PlotHeight) : (layout.Left, layout.Left + layout.PlotWidth);
        var center = axis.TickAt(index);
        var previous = index > 0 ? (axis.TickAt(index - 1) + center) / 2 : first;
        var next = index < axis.Categories.Length - 1 ? (axis.TickAt(index + 1) + center) / 2 : last;
        return (Math.Min(previous, next), Math.Max(previous, next));
    }

    /// <summary>The cursor's paint: Recharts' defaults (a <c>#ccc</c> line, or a <c>#ccc</c> band without stroke) under the tooltip's cursor props.</summary>
    public static Paint CursorPaint(TooltipSpec tooltip, bool band, Theming.ChartTheme theme) => new(
        band ? tooltip.CursorStroke : tooltip.CursorStroke ?? theme.CursorStroke,
        tooltip.CursorStrokeWidth ?? double.NaN,
        tooltip.CursorFill ?? (band ? theme.CursorFill : "none"),
        tooltip.CursorFillOpacity ?? double.NaN,
        tooltip.CursorStrokeDashArray,
        tooltip.CursorStrokeOpacity ?? double.NaN);

    private static void Cursor<TCanvas>(ref TCanvas canvas, ChartLayout layout, AxisLayout axis, int index)
        where TCanvas : struct, IChartCanvas
    {
        var tooltip = layout.Spec.Tooltip!;
        if (layout.Spec.Kind == ChartKind.Bar)
        {
            var start = axis.TickAt(index) - (axis.Bandwidth / 2);
            var band = layout.Vertical
                ? new RectShape(layout.Left + 0.5, start, layout.PlotWidth - 1, axis.Bandwidth, [])
                : new RectShape(start, layout.Top + 0.5, axis.Bandwidth, layout.PlotHeight - 1, []);
            canvas.Shape(band, CursorPaint(tooltip, band: true, layout.Spec.Colors), "recharts-rectangle recharts-tooltip-cursor"u8);
            return;
        }

        var c = axis.TickAt(index);
        var paint = CursorPaint(tooltip, band: false, layout.Spec.Colors);
        if (layout.Vertical)
        {
            canvas.Line(layout.Left, c, layout.Left + layout.PlotWidth, c, paint, "recharts-curve recharts-tooltip-cursor"u8);
        }
        else
        {
            canvas.Line(c, layout.Top, c, layout.Top + layout.PlotHeight, paint, "recharts-curve recharts-tooltip-cursor"u8);
        }
    }
}
