using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>
/// Recharts' item tooltip of a bar chart (<c>shared={false}</c>, <c>tooltipEventType="item"</c>): one hover state per
/// drawn bar, entered on the bar, with no cursor and the default tooltip holding the bar's series and value without a
/// label, anchored at the bar's center (its <c>tooltipPosition</c>).
/// </summary>
internal static class BarTips
{
    public static void Hits<TCanvas>(ref TCanvas canvas, ChartLayout layout, int active)
        where TCanvas : struct, IChartCanvas
    {
        var state = 0;
        canvas.Open(TipPainter.HitsLayer(layout.Spec.Tooltip!));
        foreach (var series in layout.Series)
        {
            if (series.Spec.Kind != SeriesKind.Bar)
            {
                continue;
            }

            var target = Specs.SpecIndex.Of(layout.Spec.Series, series.Spec);
            for (var i = 0; i < series.Bars.Length; i++)
            {
                var bar = series.Bars[i];
                if (!SeriesPainter.IsDrawn(bar) || (state++ != active && active >= 0))
                {
                    continue;
                }

                canvas.Open(default);
                var marked = Toggles.Open(ref canvas, layout.Spec, target);
                canvas.Hit(Math.Min(bar.X, bar.X + bar.Width), Math.Min(bar.Y, bar.Y + bar.Height), Math.Abs(bar.Width), Math.Abs(bar.Height));
                if (marked)
                {
                    canvas.Close();
                }

                canvas.Close();
            }
        }

        canvas.Close();
    }

    public static void Tips<TCanvas>(ref TCanvas canvas, ChartLayout layout, int active)
        where TCanvas : struct, IChartCanvas
    {
        var buffers = (new TooltipItem[1], new TooltipEntry[1]);
        var state = 0;
        canvas.Open("pictor-tips"u8);
        foreach (var series in layout.Series)
        {
            if (series.Spec.Kind != SeriesKind.Bar)
            {
                continue;
            }

            for (var i = 0; i < series.Bars.Length; i++)
            {
                if (SeriesPainter.IsDrawn(series.Bars[i]) && (state++ == active || active < 0))
                {
                    Tip(ref canvas, layout, series, i, buffers);
                }
            }
        }

        canvas.Close();
    }

    private static void Tip<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series, int index, (TooltipItem[] Items, TooltipEntry[] Entries) buffers)
        where TCanvas : struct, IChartCanvas
    {
        var tooltip = layout.Spec.Tooltip!;
        var spec = series.Spec;
        var bar = series.Bars[index];
        var target = Specs.SpecIndex.Of(layout.Spec.Series, spec);
        canvas.Open(default);
        var wrapped = Toggles.Open(ref canvas, layout.Spec, target);
        ActiveBar(ref canvas, series, index);
        buffers.Items[0] = new TooltipItem(series.Name, spec.DataKey, series.Values[index], spec.Unit, series.Color, Toggles.Marked(layout.Spec) ? target : -1, None: spec.TooltipNone);
        var payload = TooltipBox.Finish(tooltip, buffers.Items);
        var count = TooltipBox.Entries(tooltip, buffers.Items.AsSpan(0, payload), buffers.Entries);
        TooltipBox.Write(ref canvas, tooltip, layout.Spec.FontSize, bar.X + (bar.Width / 2), bar.Y + (bar.Height / 2), string.Empty, buffers.Entries.AsSpan(0, count), payload, (layout.Left, layout.Top, layout.PlotWidth, layout.PlotHeight), default, ContentProps.Of(tooltip.Content, Content.ContentSlots.Tooltip, series.Data), index);
        if (wrapped)
        {
            canvas.Close();
        }

        canvas.Close();
    }

    /// <summary>The bar under a point (the last drawn, which is on top), or <see cref="ChartPainter.NoTips"/>.</summary>
    public static int Hovered(ChartLayout layout, double x, double y)
    {
        var found = ChartPainter.NoTips;
        var state = 0;
        foreach (var series in layout.Series)
        {
            if (series.Spec.Kind != SeriesKind.Bar)
            {
                continue;
            }

            for (var i = 0; i < series.Bars.Length; i++)
            {
                var bar = series.Bars[i];
                if (!SeriesPainter.IsDrawn(bar))
                {
                    continue;
                }

                if (x >= Math.Min(bar.X, bar.X + bar.Width) && x <= Math.Max(bar.X, bar.X + bar.Width) && y >= Math.Min(bar.Y, bar.Y + bar.Height) && y <= Math.Max(bar.Y, bar.Y + bar.Height))
                {
                    found = state;
                }

                state++;
            }
        }

        return found;
    }

    /// <summary>How many hover states a bar chart with item tooltips has: its drawn bars.</summary>
    public static int Count(ChartLayout layout)
    {
        var count = 0;
        foreach (var series in layout.Series)
        {
            if (series.Spec.Kind != SeriesKind.Bar)
            {
                continue;
            }

            foreach (var bar in series.Bars)
            {
                count += SeriesPainter.IsDrawn(bar) ? 1 : 0;
            }
        }

        return count;
    }

    /// <summary>
    /// A bar drawn active by its series' <c>activeIndex</c>, in place: in Recharts' active layer, its <c>activeBar</c>
    /// template, its <c>activeBar</c> props over its own (its size cut to whole pixels), or its own shape.
    /// </summary>
    public static void StaticActive<TCanvas>(ref TCanvas canvas, ChartLayout layout, SeriesLayout series, int index, Motion.MorphKind? morph)
        where TCanvas : struct, IChartCanvas
    {
        var bar = series.Bars[index];
        canvas.Open("recharts-layer recharts-active-bar"u8);
        if (series.Spec.ActiveShapeContent is { } custom)
        {
            if (SeriesPainter.IsDrawn(bar))
            {
                var content = new Content.ContentValues(Content.ContentSlots.Bar, custom, series.Data);
                canvas.Content(custom, ContentProps.Bar(content, series, index, SeriesPainter.BarPaint(series, index), ContentProps.Template(series.Spec.Order, 1)));
            }
        }
        else if (series.Spec.ActiveBar is not null)
        {
            Styled(ref canvas, series, index);
        }
        else if (SeriesPainter.IsDrawn(bar))
        {
            SeriesPainter.Bar(ref canvas, layout, series, index, morph, null);
        }

        canvas.Close();
    }

    /// <summary>
    /// A size as Recharts' <c>typeguardBarRectangleProps</c> passes an object option's: JavaScript's
    /// <c>parseInt(String(value), 10)</c> (cut towards zero), where a zero reads as <c>undefined</c> (its
    /// <c>props.height || option.height</c>) and gives NaN, which draws nothing.
    /// </summary>
    public static double ParseInt(double value)
    {
        if (value == 0 || double.IsNaN(value))
        {
            return double.NaN;
        }

        if (Math.Abs(value) is >= 1e-6 and < 1e21)
        {
            return Math.Truncate(value);
        }

        var text = Aioros.Graphs.Formatting.JsNumber.ToShortestString(value).AsSpan();
        var sign = text[0] == '-' ? -1 : 1;
        text = text.TrimStart('-');
        var digits = 0;
        while (digits < text.Length && char.IsAsciiDigit(text[digits]))
        {
            digits++;
        }

        return digits == 0 ? double.NaN : sign * double.Parse(text[..digits], System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>A bar drawn with its <c>activeBar</c> props over its own, its size cut to whole pixels.</summary>
    private static void Styled<TCanvas>(ref TCanvas canvas, SeriesLayout series, int index)
        where TCanvas : struct, IChartCanvas
    {
        var bar = series.Bars[index];
        var style = series.Spec.ActiveBar!;
        var (width, height) = (ParseInt(bar.Width), ParseInt(bar.Height));
        if (double.IsNaN(width) || double.IsNaN(height) || width == 0 || height == 0)
        {
            return;
        }

        var paint = SeriesPainter.BarPaint(series, index);
        canvas.Shape(new RectShape(bar.X, bar.Y, width, height, series.Spec.Radius), new Paint(style.Stroke ?? paint.Stroke, style.StrokeWidth ?? paint.StrokeWidth, style.Fill ?? paint.Fill), ClassNames.With("recharts-rectangle"u8, series.Spec.ClassName));
    }

    /// <summary>A bar drawn again with its <c>activeBar</c> props, when it has them and its series sets no <c>activeIndex</c> (a set one wins over the pointer, as Recharts').</summary>
    public static void ActiveBar<TCanvas>(ref TCanvas canvas, SeriesLayout series, int index)
        where TCanvas : struct, IChartCanvas
    {
        var bar = series.Bars[index];
        if (series.Spec.ActiveIndex is not null)
        {
            return;
        }

        if (series.Spec.ActiveShapeContent is { } custom && SeriesPainter.IsDrawn(bar))
        {
            var content = new Content.ContentValues(Content.ContentSlots.Bar, custom, series.Data);
            canvas.Open("recharts-layer recharts-active-bar"u8);
            canvas.Content(custom, ContentProps.Bar(content, series, index, SeriesPainter.BarPaint(series, index), ContentProps.Template(series.Spec.Order, 1), state: 1));
            canvas.Close();
            return;
        }

        if (series.Spec.ActiveBar is not null && SeriesPainter.IsDrawn(bar))
        {
            Styled(ref canvas, series, index);
        }
    }
}
