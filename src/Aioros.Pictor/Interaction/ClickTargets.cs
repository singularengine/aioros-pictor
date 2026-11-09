using System.Globalization;
using Aioros.Pictor.Data;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Interaction;

/// <summary>The form of a click target's area.</summary>
internal enum ClickShapeKind : byte
{
    Rect,
    Circle,
    Path,
}

/// <summary>
/// An area of a chart that runs a command when clicked: the command's key, the parameter it runs with (an item's index
/// in the data, a node's path), its area, the hover state it shows while hovered or focused (or -1), the legend target
/// that hides it (or -1), whether it is out of the window or the zoom at this render (no button is written for it; a
/// choice field's option is written hidden), and its accessible name.
/// </summary>
internal sealed record ClickTarget(string Key, string Parameter, ClickShapeKind Kind, double X, double Y, double Width, double Height, string? Path, int State, string Label)
{
    public int Toggle { get; init; } = -1;

    public bool Hidden { get; init; }

    /// <summary>The address the target links to; a link runs no command.</summary>
    public string? Href { get; init; }

    /// <summary>Whether the target is an option of a choice field (its parameter the option's value) rather than a command's button.</summary>
    public bool Option { get; init; }

    /// <summary>Whether a point is inside the target's area (a path's through the desktop's own test).</summary>
    public bool Contains(double x, double y, Func<string, double, double, bool> inPath) => !Hidden && Kind switch
    {
        ClickShapeKind.Rect => x >= X && x <= X + Width && y >= Y && y <= Y + Height,
        ClickShapeKind.Circle => ((x - X) * (x - X)) + ((y - Y) * (y - Y)) <= Width * Width / 4,
        _ => inPath(Path!, x, y),
    };

    public static ClickTarget Rect(string key, string parameter, double x, double y, double width, double height, int state, string label) =>
        new(key, parameter, ClickShapeKind.Rect, Math.Min(x, x + width), Math.Min(y, y + height), Math.Abs(width), Math.Abs(height), null, state, label);

    public static ClickTarget Circle(string key, string parameter, double cx, double cy, double radius, int state, string label) =>
        new(key, parameter, ClickShapeKind.Circle, cx, cy, radius * 2, radius * 2, null, state, label);

    public static ClickTarget Shape(string key, string parameter, StateShape shape, int state, string label) => shape.Path is { } path
        ? new(key, parameter, ClickShapeKind.Path, 0, 0, 0, 0, path, state, label)
        : Rect(key, parameter, shape.X, shape.Y, shape.Width, shape.Height, state, label);

    public static ClickTarget Out(string key, string parameter, string label) => new(key, parameter, ClickShapeKind.Rect, 0, 0, 0, 0, null, -1, label) { Hidden = true };
}

/// <summary>
/// The click targets of a laid-out chart over its whole data, those out of a brush's window or off a treemap's zoom
/// marked hidden: the chart's own (Recharts' chart <c>onClick</c>, a category or a
/// hover state), then each series' items (Recharts' item <c>onClick</c>), then a nest treemap's zooming nodes.
/// </summary>
internal static class ClickTargets
{
    public static List<ClickTarget> Of(IChartPlot plot, ChartData data)
    {
        var targets = new List<ClickTarget>();
        switch (plot)
        {
            case ChartLayout layout:
                CartesianClicks.Add(targets, layout);
                break;
            case PolarLayout polar:
                PolarClicks.Add(targets, polar, data);
                break;
            case TreemapPlot treemap:
                StandaloneClicks.Treemap(targets, treemap);
                break;
            default:
                StandaloneClicks.Add(targets, plot, data);
                break;
        }

        return targets;
    }

    public static string Index(int index) => index.ToString(CultureInfo.InvariantCulture);

    /// <summary>The hover states' areas of a chart, laid out again with a tooltip when it has none (its states exist only with one).</summary>
    public static List<List<StateShape>> States(IChartPlot plot, ChartData data) =>
        StateShapes.Of(plot.HoverCount > 0 ? plot : Plots.Compute(plot.Spec with { Tooltip = new TooltipSpec() }, data));
}

/// <summary>The click targets of a cartesian chart: its categories' zones, its bars, its lines' and areas' dots and its scatter symbols.</summary>
internal static class CartesianClicks
{
    public static void Add(List<ClickTarget> targets, ChartLayout layout)
    {
        if (layout.Spec.ClickCommand is { } chart && layout.Categories is { } axis)
        {
            Zones(targets, layout, axis, chart);
        }

        var counters = new Counters();
        foreach (var series in layout.Series)
        {
            Series(targets, layout, series, counters);
        }

        foreach (var ticked in layout.XAxes.Concat(layout.YAxes))
        {
            if (ticked.Spec.ClickCommand is { } key && !ticked.Spec.Hide)
            {
                Ticks(targets, layout, ticked, key);
            }
        }
    }

    /// <summary>
    /// An axis' ticks (Recharts' tick <c>onClick</c>, with the tick's value): one target per category of the whole data
    /// under a brush (out of the window, or thinned out, hidden), otherwise one per tick the axis computes, a thinned-out
    /// one hidden.
    /// </summary>
    private static void Ticks(List<ClickTarget> targets, ChartLayout layout, AxisLayout axis, string key)
    {
        var brushed = axis.IsCategory && layout.Brush is not null && ReferenceEquals(axis, layout.Categories);
        var start = brushed ? layout.Brush!.StartIndex : 0;
        var count = brushed ? layout.WholeData.Count : axis.Ticks.Length;
        var visible = new HashSet<AxisTick>(axis.Visible);
        for (var i = 0; i < count; i++)
        {
            var at = i - start;
            var tick = at >= 0 && at < axis.Ticks.Length ? axis.Ticks[at] : default;
            var parameter = brushed ? Category(layout, axis, i) : Value(axis, tick);
            if (at < 0 || at >= axis.Ticks.Length || !visible.Contains(tick))
            {
                targets.Add(ClickTarget.Out(key, parameter, parameter));
                continue;
            }

            targets.Add(new ClickTarget(key, parameter, ClickShapeKind.Path, 0, 0, 0, 0, ChartPainter.TickArea(layout, axis, tick), -1, tick.Text));
        }
    }

    /// <summary>A tick's value as Recharts' payload holds it: a category's own text, or the number as JavaScript writes it.</summary>
    private static string Value(AxisLayout axis, in AxisTick tick) =>
        axis.IsCategory && (int)tick.Value is var index && index >= 0 && index < axis.Categories.Length
            ? axis.Categories[index] ?? string.Empty
            : Aioros.Graphs.Formatting.JsNumber.ToShortestString(tick.Value);

    /// <summary>The category of an item of the whole data, read through the axis' key.</summary>
    private static string Category(ChartLayout layout, AxisLayout axis, int index)
    {
        if (axis.Spec.DataKey is not { } key)
        {
            return ClickTargets.Index(index);
        }

        var text = layout.WholeData.Texts(key)[index];
        return text ?? Aioros.Graphs.Formatting.JsNumber.ToShortestString(layout.WholeData.Numbers(key)[index]);
    }

    private static void Zones(List<ClickTarget> targets, ChartLayout layout, AxisLayout axis, string key)
    {
        var start = layout.Brush?.StartIndex ?? 0;
        var shared = layout.HoverCount > 0 && !layout.BarTooltip;
        for (var i = 0; i < layout.WholeData.Count; i++)
        {
            var j = i - start;
            if (j < 0 || j >= axis.Categories.Length)
            {
                targets.Add(ClickTarget.Out(key, ClickTargets.Index(i), string.Empty));
                continue;
            }

            var label = axis.Categories[j] ?? string.Empty;
            var (low, high) = TipPainter.Zone(layout, axis, j);
            targets.Add(layout.Vertical
                ? ClickTarget.Rect(key, ClickTargets.Index(i), layout.Left, low, layout.PlotWidth, high - low, shared ? j : -1, label)
                : ClickTarget.Rect(key, ClickTargets.Index(i), low, layout.Top, high - low, layout.PlotHeight, shared ? j : -1, label));
        }
    }

    private static void Series(List<ClickTarget> targets, ChartLayout layout, SeriesLayout series, Counters counters)
    {
        var spec = layout.Spec;
        var key = series.Spec.ClickCommand ?? spec.ItemClickCommand;
        if (key is null && series.Spec.HrefKey is null)
        {
            counters.Bars += series.Spec.Kind == SeriesKind.Bar ? series.Bars.Count(SeriesPainter.IsDrawn) : 0;
            counters.Points += series.Spec.Kind == SeriesKind.Scatter ? series.Defined.Count(defined => defined) : 0;
            return;
        }

        var own = series.Spec.Data is not null;
        var count = own ? series.Data.Count : layout.WholeData.Count;
        var offset = own ? 0 : layout.Brush?.StartIndex ?? 0;
        var toggle = Toggles.Marked(spec) ? Specs.SpecIndex.Of(spec.Series, series.Spec) : -1;
        var links = series.Spec.HrefKey is { } link ? (own ? series.Data : layout.WholeData).Texts(link) : null;
        for (var i = 0; i < count; i++)
        {
            targets.Add(Linked(Item(layout, series, key ?? string.Empty, i, i - offset, counters) with { Toggle = toggle }, links?[i], key is not null));
        }
    }

    /// <summary>A target with its link, when its item has a safe address; one with neither a link nor a command is left out.</summary>
    private static ClickTarget Linked(ClickTarget target, string? link, bool command) =>
        Hrefs.Safe(link) is { } href ? target with { Href = href } : command ? target : target with { Hidden = true };

    private static ClickTarget Item(ChartLayout layout, SeriesLayout series, string key, int index, int shown, Counters counters)
    {
        var parameter = ClickTargets.Index(index);
        var label = Label(layout, series, shown);
        var category = layout.HoverCount > 0 && !layout.ItemTooltip && !layout.BarTooltip ? shown : -1;
        var inside = shown >= 0 && shown < Math.Max(series.Bars.Length, series.X.Length);
        if (!inside)
        {
            return ClickTarget.Out(key, parameter, label);
        }

        return series.Spec.Kind switch
        {
            SeriesKind.Bar => Bar(layout, series, series.Bars[shown], key, parameter, layout.BarTooltip && layout.HoverCount > 0, category, label, counters),
            SeriesKind.Scatter => Symbol(series, shown, key, parameter, layout.ItemTooltip && layout.HoverCount > 0, label, counters),
            _ => series.Defined[shown] ? ClickTarget.Circle(key, parameter, series.X[shown], series.Y[shown], series.Spec.ActiveDotRadius + 2, category, label) : ClickTarget.Out(key, parameter, label),
        };
    }

    /// <summary>A bar's target: its rectangle, or its whole band when it draws a background, which Recharts' bar handlers cover too.</summary>
    private static ClickTarget Bar(ChartLayout layout, SeriesLayout series, BarRect bar, string key, string parameter, bool items, int category, string label, Counters counters)
    {
        var drawn = SeriesPainter.IsDrawn(bar);
        if (series.Spec.Background is not null)
        {
            var state = drawn && items ? counters.Bars : items ? -1 : category;
            counters.Bars += drawn ? 1 : 0;
            return layout.Vertical
                ? ClickTarget.Rect(key, parameter, series.XAxis.X, bar.Y, series.XAxis.Width, bar.Height, state, label)
                : ClickTarget.Rect(key, parameter, bar.X, series.YAxis.Y, bar.Width, series.YAxis.Height, state, label);
        }

        if (!drawn)
        {
            return ClickTarget.Out(key, parameter, label);
        }

        var shown = items ? counters.Bars : category;
        counters.Bars++;
        return ClickTarget.Rect(key, parameter, bar.X, bar.Y, bar.Width, bar.Height, shown, label);
    }

    private static ClickTarget Symbol(SeriesLayout series, int index, string key, string parameter, bool items, string label, Counters counters)
    {
        if (!series.Defined[index])
        {
            return ClickTarget.Out(key, parameter, label);
        }

        var state = items ? counters.Points : -1;
        counters.Points++;
        return ClickTarget.Shape(key, parameter, new StateShape(0, 0, 0, 0, StateShapes.PathOf(ScatterPainter.Symbol(series, index))), state, label);
    }

    private static string Label(ChartLayout layout, SeriesLayout series, int shown)
    {
        var name = series.Spec.Name ?? series.Spec.DataKey;
        if (series.Spec.Kind == SeriesKind.Scatter && shown >= 0 && shown < Math.Min(series.XValues.Length, series.Values.Length))
        {
            return string.Join(", ", ((string?[])[name, Coordinate(series, series.XAxis.Spec, series.XValues[shown]), Coordinate(series, series.YAxis.Spec, series.Values[shown])]).Where(static part => !string.IsNullOrEmpty(part)));
        }

        return layout.Categories is { } axis && shown >= 0 && shown < axis.Categories.Length && axis.Categories[shown] is { } category ? name + ", " + category : name;
    }

    /// <summary>A scatter point's coordinate as its tooltip names it: the axis' name and the value with its unit.</summary>
    private static string Coordinate(SeriesLayout series, AxisSpec axis, double value) =>
        $"{(axis.DataKey is null ? series.Spec.Name : axis.Name ?? axis.DataKey)} {Aioros.Graphs.Formatting.JsNumber.ToShortestString(value)}{axis.Unit}".Trim();

    /// <summary>The drawn bars and points counted so far, which number a bar chart's and a scatter chart's item hover states.</summary>
    private sealed class Counters
    {
        public int Bars { get; set; }

        public int Points { get; set; }
    }
}

/// <summary>The click targets of a polar chart: its hover states (a category or a sector) and its pies' sectors, radars' dots and radial bars' sectors.</summary>
internal static class PolarClicks
{
    public static void Add(List<ClickTarget> targets, PolarLayout layout, ChartData data)
    {
        if (layout.Spec.ClickCommand is { } chart)
        {
            States(targets, layout, data, chart);
        }

        foreach (var pie in layout.Pies)
        {
            Pie(targets, layout, pie);
        }

        foreach (var radar in layout.Radars)
        {
            Radar(targets, layout, radar);
        }

        RadialBars(targets, layout);
    }

    private static void States(List<ClickTarget> targets, PolarLayout layout, ChartData data, string key)
    {
        var states = ClickTargets.States(layout, data);
        for (var i = 0; i < states.Count; i++)
        {
            var label = layout.Spec.Kind == ChartKind.Pie ? ClickTargets.Index(i) : Category(layout, i);
            foreach (var shape in states[i])
            {
                targets.Add(ClickTarget.Shape(key, ClickTargets.Index(i), shape, layout.HoverCount > 0 ? i : -1, label));
            }
        }
    }

    private static void Pie(List<ClickTarget> targets, PolarLayout layout, PieLayout pie)
    {
        if ((pie.Spec.ClickCommand ?? layout.Spec.ItemClickCommand) is not { } key)
        {
            return;
        }

        var toggles = Toggles.Marked(layout.Spec);
        var offset = layout.PieOffset(pie);
        for (var i = 0; i < pie.Sectors.Length; i++)
        {
            var shape = new StateShape(0, 0, 0, 0, StateShapes.PathOf(pie.Sectors[i].Shape));
            targets.Add(ClickTarget.Shape(key, ClickTargets.Index(i), shape, layout.HoverCount > 0 ? offset + i : -1, pie.Items[i].Name) with { Toggle = toggles ? offset + i : -1 });
        }
    }

    private static void Radar(List<ClickTarget> targets, PolarLayout layout, RadarLayout radar)
    {
        if ((radar.Spec.ClickCommand ?? layout.Spec.ItemClickCommand) is not { } key)
        {
            return;
        }

        var toggle = Toggles.Marked(layout.Spec) ? layout.RadarTarget(radar) : -1;
        for (var i = 0; i < radar.X.Length; i++)
        {
            targets.Add(ClickTarget.Circle(key, ClickTargets.Index(i), radar.X[i], radar.Y[i], 6, layout.HoverCount > 0 ? i : -1, radar.Name + ", " + Category(layout, i)) with { Toggle = toggle });
        }
    }

    private static string Category(PolarLayout layout, int index) => index < layout.Categories.Length ? layout.Categories[index] : string.Empty;

    private static void RadialBars(List<ClickTarget> targets, PolarLayout layout)
    {
        var ordinal = 0;
        foreach (var bar in layout.RadialBars)
        {
            var key = bar.Spec.ClickCommand ?? layout.Spec.ItemClickCommand;
            for (var i = 0; i < bar.Sectors.Length; i++)
            {
                var state = State(layout, ordinal, i);
                ordinal += bar.Sectors[i].Shape.Drawn ? 1 : 0;
                if (key is not null)
                {
                    targets.Add(Sector(bar.Sectors[i], bar.Spec.Background is not null, key, i, state));
                }
            }
        }
    }

    private static int State(PolarLayout layout, int ordinal, int index) => layout.HoverCount == 0 ? -1 : layout.ItemBars ? ordinal : index;

    /// <summary>A radial bar's target: its sector, or its background's when it draws one, which Recharts' radial bar handlers cover too.</summary>
    private static ClickTarget Sector(in RadialSector sector, bool background, string key, int index, int state)
    {
        var shape = background && sector.Background.Drawn ? sector.Background : sector.Shape;
        return shape.Drawn
            ? ClickTarget.Shape(key, ClickTargets.Index(index), new StateShape(0, 0, 0, 0, StateShapes.PathOf(shape)), sector.Shape.Drawn ? state : -1, sector.Name)
            : ClickTarget.Out(key, ClickTargets.Index(index), sector.Name);
    }
}

/// <summary>
/// The click targets of the charts whose items are their hover states (a funnel's trapezoids, a Sankey's links and
/// nodes, a sunburst's sectors, a treemap's nodes), with a nest treemap's zooming nodes.
/// </summary>
internal static class StandaloneClicks
{
    public static void Add(List<ClickTarget> targets, IChartPlot plot, ChartData data)
    {
        var spec = plot.Spec;
        if (plot is FunnelPlot funnel)
        {
            Funnel(targets, funnel);
            return;
        }

        if ((spec.ClickCommand ?? spec.ItemClickCommand) is not { } key)
        {
            return;
        }

        var states = ClickTargets.States(plot, data);
        for (var i = 0; i < states.Count; i++)
        {
            var (parameter, label) = Item(plot, i);
            foreach (var shape in states[i])
            {
                targets.Add(ClickTarget.Shape(key, parameter, shape, plot.HoverCount > 0 ? i : -1, label));
            }
        }
    }

    /// <summary>
    /// A treemap's targets over its whole data, in depth-first order, those off the zoom hidden: the nodes a click zooms into (a
    /// nest treemap's nodes with children, shown when they are children of the zoomed node), then the items a click
    /// runs the treemap's command with (its leaves, or a nest treemap's shown nodes without children).
    /// </summary>
    public static void Treemap(List<ClickTarget> targets, TreemapPlot plot)
    {
        var spec = plot.Spec;
        var hovered = new Dictionary<TreemapNode, int>();
        for (var i = 0; i < plot.Hovered.Length; i++)
        {
            hovered[plot.Hovered[i]] = i;
        }

        if ((spec.ClickCommand ?? spec.ItemClickCommand) is { } key)
        {
            foreach (var entry in plot.Entries.Where(entry => entry.Leaf || (plot.Treemap.Nest && plot.Treemap.ZoomCommand is null)))
            {
                targets.Add(Node(plot, entry, key, hovered));
            }
        }

        if (plot.Treemap.ZoomCommand is { } zoom)
        {
            foreach (var entry in plot.Entries.Where(entry => !entry.Leaf))
            {
                targets.Add(Node(plot, entry, zoom, hovered) with { Option = plot.Treemap.ZoomChoice });
            }
        }
    }

    private static ClickTarget Node(TreemapPlot plot, TreemapEntry entry, string key, Dictionary<TreemapNode, int> hovered)
    {
        var name = entry.Name ?? string.Empty;
        if (!plot.Paths.TryGetValue(entry.Path, out var node) || !hovered.TryGetValue(node, out var state) || node.Depth == 0)
        {
            return ClickTarget.Out(key, entry.Path, name);
        }

        return ClickTarget.Rect(key, entry.Path, node.X, node.Y, node.Width, node.Height, plot.HoverCount > 0 ? state : -1, name);
    }

    private static (string Parameter, string Label) Item(IChartPlot plot, int state) => plot switch
    {
        SankeyPlot sankey => state < sankey.Links.Length
            ? ("link:" + ClickTargets.Index(state), Link(sankey, state))
            : ("node:" + ClickTargets.Index(state - sankey.Links.Length), sankey.Nodes[state - sankey.Links.Length].Name ?? string.Empty),
        SunburstPlot sunburst => (ClickTargets.Index(sunburst.Hovered[state]), sunburst.Nodes[sunburst.Hovered[state]].Name),
        _ => (ClickTargets.Index(state), string.Empty),
    };

    private static string Link(SankeyPlot sankey, int index)
    {
        var link = sankey.Links[index];
        return (sankey.Nodes[link.Source].Name ?? string.Empty) + " - " + (sankey.Nodes[link.Target].Name ?? string.Empty);
    }

    private static void Funnel(List<ClickTarget> targets, FunnelPlot plot)
    {
        var ordinal = 0;
        foreach (var funnel in plot.Funnels)
        {
            var key = funnel.Spec.ClickCommand ?? plot.Spec.ItemClickCommand ?? plot.Spec.ClickCommand;
            for (var i = 0; i < funnel.Items.Length; i++)
            {
                var state = plot.HoverCount > 0 ? ordinal : -1;
                ordinal++;
                if (key is not null)
                {
                    var shape = new StateShape(0, 0, 0, 0, StateShapes.PathOf(FunnelPainter.Shape(funnel.Items[i])));
                    targets.Add(ClickTarget.Shape(key, ClickTargets.Index(i), shape, state, funnel.Data.Texts(funnel.Spec.NameKey ?? "name")[i] ?? string.Empty));
                }
            }
        }
    }
}
