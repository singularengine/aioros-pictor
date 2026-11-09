using Aioros.Pictor.Data;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>
/// A sunburst node laid out: its sector (center, radii, start angle and arc), its fill, its value written at the
/// sector's middle (null when it has none) and its tooltip's anchor and value (NaN when it has no <c>value</c>, which
/// Recharts' default tooltip reads whatever the data key, or the chart has no tooltip, so a model is probed for
/// <c>value</c> only when a tooltip shows it).
/// </summary>
internal readonly record struct SunburstNode(string Name, string? Text, string Fill, double Start, double Arc, double InnerRadius, double OuterRadius, double TextX, double TextY, double TipX, double TipY, double TipValue)
{
    public SectorShape Sector(double cx, double cy) => new(cx, cy, InnerRadius, OuterRadius, Start, Start + Arc);
}

/// <summary>
/// A sunburst laid out as Recharts' <c>SunburstChart</c> lays it out (<c>drawArcs</c>): every ring is the radius
/// between the inner and outer radii shared by the tree's depth, every arc the node's value scaled from the root's
/// value to <c>endAngle</c> (d3's <c>scaleLinear([0, root], [0, endAngle])</c>), laid from <c>startAngle</c> one after
/// the other, a node's children starting where it starts one ring further out.
/// </summary>
internal sealed class SunburstPlot : IChartPlot
{
    private const string ChildrenKey = "children";
    private const string TooltipKey = "value";

    private readonly SunburstSpec _sunburst;
    private readonly List<SunburstNode> _nodes = [];
    private double _rootValue;

    private SunburstPlot(ChartSpec spec, SunburstSpec sunburst)
    {
        Spec = spec;
        _sunburst = sunburst;
        Cx = sunburst.Cx ?? spec.Width / 2;
        Cy = sunburst.Cy ?? spec.Height / 2;
    }

    public ChartSpec Spec { get; }

    public SunburstSpec Sunburst => _sunburst;

    public double Cx { get; }

    public double Cy { get; }

    /// <summary>Every node but the root, in the order Recharts draws them (depth first).</summary>
    public SunburstNode[] Nodes { get; private set; } = [];

    /// <summary>The nodes whose sector is drawn, which show a tooltip, by their index in <see cref="Nodes"/>.</summary>
    public int[] Hovered { get; private set; } = [];

    public int HoverCount => Spec.Tooltip is null ? 0 : Hovered.Length;

    public static SunburstPlot Compute(ChartSpec spec, ChartData data)
    {
        var sunburst = spec.Sunburst ?? new SunburstSpec();
        var plot = new SunburstPlot(spec, sunburst);
        var root = data.Root();
        plot._rootValue = root.Numbers(root.Resolve(sunburst.DataKey))[0];
        var children = root.Nested(0, root.Resolve(ChildrenKey));
        if (double.IsFinite(plot._rootValue))
        {
            var outer = sunburst.OuterRadius ?? Math.Min(spec.Width, spec.Height) / 2;
            var thickness = (outer - sunburst.InnerRadius) / Depth(root, 0);
            plot.Arcs(children, thickness, sunburst.InnerRadius, sunburst.StartAngle, null);
        }

        plot.Complete();
        return plot;
    }

    /// <summary>Recharts' <c>getMaxDepthOf</c>: 1 for a node without children, else one more than its deepest child.</summary>
    private static int Depth(ChartData data, int index)
    {
        var children = data.Nested(index, data.Resolve(ChildrenKey));
        var deepest = 0;
        for (var i = 0; i < children.Count; i++)
        {
            deepest = Math.Max(deepest, Depth(children, i));
        }

        return 1 + deepest;
    }

    /// <summary>d3's <c>scaleLinear([0, root], [0, endAngle])</c>: the range's middle when the root's value is 0.</summary>
    private double Scale(double value) => _rootValue == 0 ? _sunburst.EndAngle / 2 : _sunburst.EndAngle * (value / _rootValue);

    private void Arcs(ChartData data, double radius, double innerRadius, double initialAngle, string? childColor)
    {
        if (data.Count == 0)
        {
            return;
        }

        var names = data.Texts(data.Resolve(_sunburst.NameKey));
        var valueKey = data.Resolve(_sunburst.DataKey);
        var values = data.Numbers(valueKey);
        var texts = data.Texts(valueKey);
        var fills = _sunburst.FillKey is { } fillKey ? Painting.CssValues.Valid(data.Texts(data.Resolve(fillKey))) : null;
        var tips = Spec.Tooltip is null ? null : data.Resolve(TooltipKey) is var tipKey && tipKey == valueKey ? values : data.Numbers(tipKey);
        var childrenKey = data.Resolve(ChildrenKey);
        var current = initialAngle;
        var middle = innerRadius + (radius / 2);
        for (var i = 0; i < data.Count; i++)
        {
            var valued = double.IsFinite(values[i]);
            var arc = valued ? Scale(values[i]) : 0;
            var start = current;
            var fill = fills?[i] ?? childColor ?? _sunburst.Fill ?? Spec.Colors.SunburstFill;
            var (textX, textY) = Polar.Point(0, 0, middle, -(start + arc - (arc / 2)));
            var (tipX, tipY) = Polar.Point(Cx, Cy, middle, start);
            current += arc;
            _nodes.Add(new SunburstNode(names[i] ?? string.Empty, valued ? texts[i] : null, fill, start, arc, innerRadius, innerRadius + radius, textX + Cx, Cy - textY, tipX, tipY, tips?[i] ?? double.NaN));
            Arcs(data.Nested(i, childrenKey), radius, innerRadius + radius + _sunburst.RingPadding, start, fill);
        }
    }

    /// <summary>The nodes and the drawn ones; every tooltip anchored where the last node of its name is, as Recharts keys its positions by name.</summary>
    private void Complete()
    {
        var nodes = _nodes.ToArray();
        var last = new Dictionary<string, int>(nodes.Length, StringComparer.Ordinal);
        var drawn = 0;
        for (var i = 0; i < nodes.Length; i++)
        {
            last[nodes[i].Name] = i;
            drawn += nodes[i].Sector(Cx, Cy).Drawn && double.IsFinite(nodes[i].Arc) ? 1 : 0;
        }

        var hovered = new int[drawn];
        drawn = 0;
        for (var i = 0; i < nodes.Length; i++)
        {
            var anchor = nodes[last[nodes[i].Name]];
            nodes[i] = nodes[i] with { TipX = anchor.TipX, TipY = anchor.TipY };
            if (nodes[i].Sector(Cx, Cy).Drawn && double.IsFinite(nodes[i].Arc))
            {
                hovered[drawn++] = i;
            }
        }

        Nodes = nodes;
        Hovered = hovered;
    }
}
