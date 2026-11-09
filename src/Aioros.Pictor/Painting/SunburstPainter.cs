using Aioros.Pictor.Layout;

namespace Aioros.Pictor.Painting;

/// <summary>
/// Paints a sunburst as Recharts' <c>SunburstChart</c> renders it: a <c>recharts-sunburst</c> layer holding, for every
/// node in depth-first order, a focusable group named after the node with its <c>recharts-sector</c> and its value in
/// the chart's text options; then the hover states, one per drawn sector.
/// </summary>
internal static class SunburstPainter
{
    /// <summary>Recharts' default text options in the theme's colors; options of the chart's own as they are.</summary>
    private static Specs.TextOptionsSpec Themed(Specs.TextOptionsSpec options, Theming.ChartTheme theme) =>
        options == Specs.TextOptionsSpec.Default && (options.Fill != theme.SunburstText || options.Stroke != theme.SunburstTextStroke)
            ? options with { Fill = theme.SunburstText, Stroke = theme.SunburstTextStroke }
            : options;

    public static void Paint<TCanvas>(ref TCanvas canvas, SunburstPlot plot, int active)
        where TCanvas : struct, IChartCanvas
    {
        var sunburst = plot.Sunburst;
        var theme = plot.Spec.Colors;
        var options = Themed(sunburst.TextOptions, theme);
        canvas.Begin(plot);
        canvas.Open("recharts-layer recharts-sunburst"u8);
        foreach (var node in plot.Nodes)
        {
            canvas.OpenItem(node.Name);
            var sector = node.Sector(plot.Cx, plot.Cy);
            if (sector.Drawn && double.IsFinite(node.Arc))
            {
                canvas.Shape(sector, new Paint(sunburst.Stroke ?? theme.SunburstStroke, sunburst.Padding, node.Fill), "recharts-sector"u8);
            }

            if (node.Text is { } text)
            {
                canvas.OptionsText(node.TextX, node.TextY, text, options);
            }

            canvas.Close();
        }

        canvas.Close();
        if (active != ChartPainter.NoTips && plot.HoverCount > 0)
        {
            Hits(ref canvas, plot, active);
            Tips(ref canvas, plot, active);
        }

        canvas.EndSurface();
        canvas.End();
    }

    public static void Tips<TCanvas>(ref TCanvas canvas, SunburstPlot plot, int active)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Open("pictor-tips"u8);
        for (var i = 0; i < plot.Hovered.Length; i++)
        {
            if (active >= 0 && active != i)
            {
                continue;
            }

            var node = plot.Nodes[plot.Hovered[i]];
            if (double.IsNaN(node.TipValue))
            {
                canvas.Open(default);
                canvas.Close();
                continue;
            }

            ItemTips.Tooltip(ref canvas, plot.Spec, new ItemTip(node.TipX, node.TipY, node.Name, node.TipValue), 0, 0, plot.Spec.Width, plot.Spec.Height, index: i);
        }

        canvas.Close();
    }

    /// <summary>The drawn sector under a point (the last drawn, which is on top), or <see cref="ChartPainter.NoTips"/>.</summary>
    public static int Hovered(SunburstPlot plot, double x, double y)
    {
        if (plot.HoverCount == 0)
        {
            return ChartPainter.NoTips;
        }

        var dx = x - plot.Cx;
        var dy = y - plot.Cy;
        var radius = Math.Sqrt((dx * dx) + (dy * dy));
        var angle = Math.Atan2(-dy, dx) / Polar.Radian;
        for (var i = plot.Hovered.Length - 1; i >= 0; i--)
        {
            var node = plot.Nodes[plot.Hovered[i]];
            if (radius >= node.InnerRadius && radius <= node.OuterRadius && Within(angle, node.Start, node.Arc))
            {
                return i;
            }
        }

        return ChartPainter.NoTips;
    }

    private static void Hits<TCanvas>(ref TCanvas canvas, SunburstPlot plot, int active)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Open("pictor-hits"u8);
        for (var i = 0; i < plot.Hovered.Length; i++)
        {
            if (active < 0 || active == i)
            {
                canvas.HitShape(plot.Nodes[plot.Hovered[i]].Sector(plot.Cx, plot.Cy));
            }
        }

        canvas.Close();
    }

    private static bool Within(double angle, double start, double arc)
    {
        if (Math.Abs(arc) >= 360)
        {
            return true;
        }

        var from = arc >= 0 ? start : start + arc;
        var delta = (((angle - from) % 360) + 360) % 360;
        return delta <= Math.Abs(arc);
    }
}
