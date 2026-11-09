using System.Globalization;
using Aioros.Pictor.Data;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>One trapezoid of a funnel: its shape, its label's box, its tooltip's anchor, what it shows and its colors.</summary>
internal readonly record struct Trapezoid(
    double X,
    double Y,
    double UpperWidth,
    double LowerWidth,
    double Height,
    double LabelX,
    double LabelY,
    double LabelWidth,
    double TipX,
    double TipY,
    string Name,
    double Value,
    string Fill,
    string Stroke);

/// <summary>A funnel laid out: its trapezoids and the items they come from.</summary>
internal sealed class FunnelSeries
{
    public required FunnelSpec Spec { get; init; }

    public required ChartData Data { get; init; }

    public required Trapezoid[] Items { get; init; }

    /// <summary>Whether the funnel stays where it is during an update animation of its chart.</summary>
    public bool Still { get; init; }
}

/// <summary>
/// A funnel chart laid out as Recharts lays it out (<c>generateCategoricalChart</c> without axes, then
/// <c>Funnel.getComposedData</c>): the offset less the legend, then one trapezoid per item, its width the item's value
/// over the largest one.
/// </summary>
internal sealed class FunnelPlot : IChartPlot
{
    private FunnelPlot(ChartSpec spec) => Spec = spec;

    public ChartSpec Spec { get; }

    public List<FunnelSeries> Funnels { get; } = [];

    public LegendLayout? Legend { get; private set; }

    public double Left { get; private set; }

    public double Top { get; private set; }

    public double PlotWidth { get; private set; }

    public double PlotHeight { get; private set; }

    public int HoverCount => Spec.Tooltip is null ? 0 : Funnels.Sum(funnel => funnel.Items.Length);

    /// <summary>The same chart drawing other funnels (a frame of an update animation), its legend shared.</summary>
    public FunnelPlot With(IEnumerable<FunnelSeries> funnels)
    {
        var copy = new FunnelPlot(Spec) { Legend = Legend, Left = Left, Top = Top, PlotWidth = PlotWidth, PlotHeight = PlotHeight };
        copy.Funnels.AddRange(funnels);
        return copy;
    }

    public static FunnelPlot Compute(ChartSpec spec, ChartData data)
    {
        var plot = new FunnelPlot(spec);
        var margin = spec.Margin;
        var (left, right, top, bottom) = (margin.Left, margin.Right, margin.Top, margin.Bottom);
        if (spec.Legend is { } legend)
        {
            plot.Legend = LegendLayout.Compute(spec, legend, [.. spec.Funnels.Select((funnel, target) => new LegendEntry(funnel.Name ?? funnel.DataKey, funnel.Fill ?? spec.Colors.FunnelFill, legend.IconType ?? funnel.LegendType, target, funnel.Hide))]);
            plot.Legend.Apply(ref left, ref right, ref top, ref bottom);
        }

        plot.Left = left;
        plot.Top = top;
        plot.PlotWidth = spec.Width - left - right;
        plot.PlotHeight = spec.Height - top - bottom;
        foreach (var funnel in spec.Funnels.Where(funnel => !funnel.Hide))
        {
            var items = funnel.Data is { } table ? ChartData.From(table) : data;
            plot.Funnels.Add(new FunnelSeries { Spec = funnel.OpacityKey is { } opacity ? funnel with { Cells = Painting.ItemStyles.Cells(funnel.Cells, items.Numbers(items.Resolve(opacity))) } : funnel, Data = items, Items = plot.Trapezoids(funnel, items, right, bottom) });
        }

        return plot;
    }

    private Trapezoid[] Trapezoids(FunnelSpec funnel, ChartData data, double right, double bottom)
    {
        var count = data.Count;
        var values = data.Numbers(data.Resolve(funnel.DataKey));
        var names = data.Texts(data.Resolve(funnel.NameKey));
        var fills = funnel.FillKey is { } fillKey ? Painting.CssValues.Valid(data.Texts(data.Resolve(fillKey))) : new string?[count];
        var strokes = funnel.StrokeKey is { } strokeKey ? Painting.CssValues.Valid(data.Texts(data.Resolve(strokeKey))) : new string?[count];
        var realWidth = funnel.Width is { } custom ? Size(custom, PlotWidth) : PlotWidth;
        var width = realWidth - Left - right - 50;
        var height = PlotHeight - bottom - Top;
        var offsetX = (PlotWidth - realWidth) / 2;
        var max = double.NegativeInfinity;
        for (var i = 0; i < count; i++)
        {
            max = Math.Max(max, Value(values[i]));
        }

        var row = height / count;
        var items = new Trapezoid[count];
        for (var i = 0; i < count; i++)
        {
            var value = Value(values[i]);
            var next = i < count - 1 ? Value(values[i + 1]) : funnel.LastRectangle ? value : 0;
            var (inset, upper, lower) = Widths(value, next, max, width);
            var x = inset + Top + 25 + offsetX;
            var y = (row * i) + Left;
            var cell = i < funnel.Cells.Length ? funnel.Cells[i] : null;
            var item = new Trapezoid(
                x, y, upper, lower, row, x + ((upper - lower) / 4), y, (Math.Abs(upper - lower) / 2) + Math.Min(upper, lower), x + (upper / 2), y + (row / 2),
                names[i] ?? i.ToString(CultureInfo.InvariantCulture), value, cell?.Fill ?? fills[i] ?? funnel.Fill ?? Spec.Colors.FunnelFill, cell?.Stroke ?? strokes[i] ?? funnel.Stroke ?? Spec.Colors.FunnelStroke);
            items[i] = funnel.Reversed ? Reverse(item, i, count, row) : item;
        }

        return items;
    }

    private static Trapezoid Reverse(Trapezoid item, int index, int count, double row)
    {
        var y = item.Y - (index * row) + ((count - 1 - index) * row);
        return item with
        {
            UpperWidth = item.LowerWidth,
            LowerWidth = item.UpperWidth,
            X = item.X - ((item.LowerWidth - item.UpperWidth) / 2),
            Y = y,
            LabelY = y,
            TipY = y + (row / 2),
        };
    }

    private static double Value(double value) => double.IsNaN(value) ? 0 : value;

    /// <summary>A trapezoid's inset from the funnel's left and its upper and lower widths, as shares of the largest value (Recharts' <c>getComposedData</c>); a funnel whose values are all 0 has none, its trapezoids at its middle.</summary>
    private static (double Inset, double Upper, double Lower) Widths(double value, double next, double max, double width) =>
        max > 0 ? ((max - value) * width / (2 * max), value / max * width, next / max * width) : (width / 2, 0, 0);

    /// <summary>A custom width in pixels or as a share of the whole (<c>80%</c>); the whole when it is neither, as no width.</summary>
    private static double Size(string text, double whole)
    {
        var percent = text.EndsWith('%');
        if (!double.TryParse(percent ? text[..^1] : text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number))
        {
            return whole;
        }

        return percent ? whole * number / 100 : number;
    }
}
