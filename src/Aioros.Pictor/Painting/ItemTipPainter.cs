using Aioros.Graphs.Formatting;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>
/// Recharts' item tooltip of a scatter chart (<c>tooltipEventType="item"</c>): one hover state per point, entered on its
/// symbol, with Recharts' <c>Cross</c> cursor through it and the default tooltip listing the point's x, y and z with
/// the axes' names and units. On the web every point's cross is written in its tip's group (the hit group's next
/// sibling, shown by the one shared rule) rather than in the cursors' layer under the series, which took one
/// <c>:has()</c> rule per point (5,000 for five series of 1,000 points): the cross is drawn over the other symbols it
/// crosses, where Recharts draws it under them. The desktop draws it under them, as Recharts.
/// </summary>
internal static class ItemTipPainter
{
    public static void Cursors<TCanvas>(ref TCanvas canvas, ChartLayout layout, int active)
        where TCanvas : struct, IChartCanvas
    {
        var tooltip = layout.Spec.Tooltip!;
        var cursor = TipPainter.CursorPaint(tooltip, band: false, layout.Spec.Colors);
        var item = 0;
        canvas.Open(TipPainter.HitsLayer(tooltip));
        foreach (var series in Scatters(layout))
        {
            for (var i = 0; i < series.X.Length; i++)
            {
                if (!series.Defined[i] || (item++ != active && active >= 0))
                {
                    continue;
                }

                Hit(ref canvas, layout, series, i, active == ChartPainter.AllTips || !TipPainter.ShowsCursor(tooltip) ? null : cursor);
            }
        }

        canvas.Close();
        if (active == ChartPainter.AllTips && ScatterKeys.Applies(layout))
        {
            ScatterKeys.Paint(ref canvas, layout, ChartPainter.AllTips);
        }
    }

    /// <summary>A point's hit: its symbol alone, straight in the hit layer (the web, whose cross is in the tip), or in a state's group with the cross under the series (the desktop's hovered point).</summary>
    private static void Hit<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series, int i, Paint? cursor)
        where TCanvas : struct, IChartCanvas
    {
        if (cursor is not { } paint)
        {
            canvas.HitShape(ScatterPainter.Symbol(series, i));
            return;
        }

        canvas.Open(default);
        canvas.HitShape(ScatterPainter.Symbol(series, i));
        canvas.Shape(new CrossShape(series.X[i], series.Y[i], layout.Left, layout.Top, layout.PlotWidth, layout.PlotHeight), paint, "recharts-cross recharts-tooltip-cursor"u8);
        canvas.Close();
    }

    public static void Tips<TCanvas>(ref TCanvas canvas, ChartLayout layout, int active)
        where TCanvas : struct, IChartCanvas
    {
        var tooltip = layout.Spec.Tooltip!;
        Span<TooltipItem> items = new TooltipItem[3];
        Span<TooltipEntry> entries = new TooltipEntry[3];
        var viewBox = (layout.Left, layout.Top, layout.PlotWidth, layout.PlotHeight);
        var item = 0;
        canvas.Open("pictor-tips"u8);
        foreach (var series in Scatters(layout))
        {
            var z = layout.ZAxis(series.Spec.ZAxisId);
            for (var i = 0; i < series.X.Length; i++)
            {
                if (!series.Defined[i] || (item++ != active && active >= 0))
                {
                    continue;
                }

                var payload = TooltipBox.Finish(tooltip, items[..Items(series, z, i, tooltip.Format, items, layout.Spec.Colors.TooltipText)]);
                var count = TooltipBox.Entries(tooltip, items[..payload], entries);
                canvas.Open(default);
                if (TipPainter.ShowsCursor(tooltip) && active == ChartPainter.AllTips)
                {
                    canvas.Shape(new CrossShape(series.X[i], series.Y[i], layout.Left, layout.Top, layout.PlotWidth, layout.PlotHeight), TipPainter.CursorPaint(tooltip, band: false, layout.Spec.Colors), "recharts-cross recharts-tooltip-cursor"u8);
                }

                ActiveShape(ref canvas, series, i);
                TooltipBox.Write(ref canvas, tooltip, layout.Spec.FontSize, series.X[i], series.Y[i], string.Empty, entries[..count], payload, viewBox, default, ContentProps.Of(tooltip.Content, Content.ContentSlots.Tooltip, series.Data), i);
                canvas.Close();
            }
        }

        canvas.Close();
    }

    /// <summary>A scatter's active shape at the hovered point, when it has a template for it (Recharts' <c>activeShape</c> element) and no set <c>activeIndex</c> (which wins over the pointer, as Recharts'), in its active layer.</summary>
    private static void ActiveShape<TCanvas>(ref TCanvas canvas, SeriesLayout series, int index)
        where TCanvas : struct, IChartCanvas
    {
        if (series.Spec.ActiveIndex is null && ContentProps.Of(series.Spec.ActiveShapeContent, Content.ContentSlots.Scatter, series.Data) is { } content)
        {
            canvas.Open("recharts-layer recharts-active-shape"u8);
            canvas.Content(content.Spec, ContentProps.Scatter(content, series, index, ScatterPainter.Fill(series, index), ContentProps.Template(series.Spec.Order, 1), state: 1));
            canvas.Close();
        }
    }

    /// <summary>The point whose symbol holds a point of the chart (the last drawn, which is on top), or <see cref="ChartPainter.NoTips"/>.</summary>
    public static int Hovered(ChartLayout layout, double x, double y)
    {
        var found = ChartPainter.NoTips;
        var item = 0;
        foreach (var series in Scatters(layout))
        {
            for (var i = 0; i < series.X.Length; i++)
            {
                if (!series.Defined[i])
                {
                    continue;
                }

                var radius = ScatterPainter.Radius(series, i);
                if (((series.X[i] - x) * (series.X[i] - x)) + ((series.Y[i] - y) * (series.Y[i] - y)) <= radius * radius)
                {
                    found = item;
                }

                item++;
            }
        }

        return found;
    }

    private static IEnumerable<SeriesLayout> Scatters(ChartLayout layout) => layout.Series.Where(series => series.Spec.Kind == SeriesKind.Scatter);

    private static int Items(SeriesLayout series, ZAxisLayout? zAxis, int index, string? format, Span<TooltipItem> items, string color)
    {
        items[0] = Item(series.XAxis.Spec, series, series.XValues[index], format, color);
        items[1] = Item(series.YAxis.Spec, series, series.Values[index], format, color);
        if (zAxis?.Spec is { Type: AxisType.Category, DataKey: { } category })
        {
            return LabelPainter.Text(series.Data, category, index, format) is { Length: > 0 } text
                ? Item(items, new TooltipItem(zAxis.Spec.Name ?? category, category, 0, zAxis.Spec.Unit, color, None: series.Spec.TooltipNone, Formatted: text))
                : 2;
        }
        if (zAxis?.Spec.DataKey is not { } key || series.ZValues.Length <= index || double.IsNaN(series.ZValues[index]) || series.ZValues[index] == 0)
        {
            return 2;
        }

        items[2] = new TooltipItem(zAxis.Spec.Name ?? key, key, series.ZValues[index], zAxis.Spec.Unit, color, None: series.Spec.TooltipNone, Formatted: SeriesPainter.Value(series.ZValues[index], format));
        return 3;
    }

    /// <summary>The z entry after the x and y ones: Recharts lists the z value when it is truthy (<c>value || '-'</c>).</summary>
    private static int Item(Span<TooltipItem> items, in TooltipItem z)
    {
        items[2] = z;
        return 3;
    }

    private static TooltipItem Item(AxisSpec axis, SeriesLayout series, double value, string? format, string color) =>
        new(axis.DataKey is null ? series.Name : axis.Name ?? axis.DataKey, axis.DataKey ?? series.Spec.DataKey, value, axis.Unit, color, None: series.Spec.TooltipNone, Formatted: format is null ? JsNumber.ToShortestString(value) : SeriesPainter.Value(value, format));
}
