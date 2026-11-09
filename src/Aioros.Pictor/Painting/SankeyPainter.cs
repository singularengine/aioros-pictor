using Aioros.Graphs.Paths;
using Aioros.Pictor.Layout;

namespace Aioros.Pictor.Painting;

/// <summary>Where a Sankey link runs: its ends and its control points' x, Recharts' link props.</summary>
internal readonly record struct LinkCurve(double SourceX, double SourceY, double SourceControlX, double TargetX, double TargetY, double TargetControlX, double Width)
{
    /// <summary>Whether every coordinate is a number (a link between nodes without a place is not).</summary>
    public bool Finite => double.IsFinite(SourceX) && double.IsFinite(SourceY) && double.IsFinite(SourceControlX) && double.IsFinite(TargetX) && double.IsFinite(TargetY) && double.IsFinite(TargetControlX) && double.IsFinite(Width);

    public double At(double t, bool vertical)
    {
        var u = 1 - t;
        return vertical
            ? (u * u * u * SourceY) + (3 * u * u * t * SourceY) + (3 * u * t * t * TargetY) + (t * t * t * TargetY)
            : (u * u * u * SourceX) + (3 * u * u * t * SourceControlX) + (3 * u * t * t * TargetControlX) + (t * t * t * TargetX);
    }
}

/// <summary>A Sankey link's curve: Recharts' default link path, stroked as wide as the link.</summary>
internal readonly struct LinkShape(LinkCurve curve) : IShape
{
    public void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink
    {
        sink.MoveTo(curve.SourceX, curve.SourceY);
        sink.BezierCurveTo(curve.SourceControlX, curve.SourceY, curve.TargetControlX, curve.TargetY, curve.TargetX, curve.TargetY);
    }
}

/// <summary>The area a Sankey link covers, its curve shifted up and down by half its width: the link's hit target.</summary>
internal readonly struct LinkBandShape(LinkCurve curve) : IShape
{
    public void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink
    {
        var half = curve.Width / 2;
        sink.MoveTo(curve.SourceX, curve.SourceY - half);
        sink.BezierCurveTo(curve.SourceControlX, curve.SourceY - half, curve.TargetControlX, curve.TargetY - half, curve.TargetX, curve.TargetY - half);
        sink.LineTo(curve.TargetX, curve.TargetY + half);
        sink.BezierCurveTo(curve.TargetControlX, curve.TargetY + half, curve.SourceControlX, curve.SourceY + half, curve.SourceX, curve.SourceY + half);
        sink.ClosePath();
    }
}

/// <summary>Paints a Sankey as Recharts renders it with its default node and link: the links, then the nodes, then the hover states.</summary>
internal static class SankeyPainter
{
    public static void Paint<TCanvas>(ref TCanvas canvas, SankeyPlot plot, int active)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Begin(plot);
        var sankey = plot.Sankey;
        canvas.Open("recharts-layer recharts-sankey-links"u8);
        var links = ContentProps.Of(sankey.LinkContent, Content.ContentSlots.SankeyLink, null);
        for (var i = 0; i < plot.Links.Length; i++)
        {
            canvas.Open("recharts-layer"u8);
            var curve = Curve(plot, plot.Links[i]);
            var paint = new Paint(sankey.LinkStroke ?? plot.Spec.Colors.SankeyLinkStroke, sankey.LinkStrokeWidth ?? curve.Width, sankey.LinkFill ?? "none", double.NaN, sankey.LinkStrokeDashArray, sankey.LinkStrokeOpacity);
            if (links is not null)
            {
                canvas.Content(links.Spec, ContentProps.SankeyLink(links, plot, i, curve, paint));
            }
            else if (curve.Finite)
            {
                canvas.Shape(new LinkShape(curve), paint, "recharts-sankey-link"u8);
            }

            canvas.Close();
        }

        canvas.Close();
        canvas.Open("recharts-layer recharts-sankey-nodes"u8);
        var nodes = ContentProps.Of(sankey.NodeContent, Content.ContentSlots.SankeyNode, null);
        for (var i = 0; i < plot.Nodes.Length; i++)
        {
            canvas.Open("recharts-layer"u8);
            var node = plot.Nodes[i];
            var paint = new Paint(sankey.NodeStroke, sankey.NodeStrokeWidth ?? double.NaN, sankey.NodeFill ?? plot.Spec.Colors.SankeyNodeFill, sankey.NodeFillOpacity, sankey.NodeStrokeDashArray, sankey.NodeStrokeOpacity ?? double.NaN);
            if (nodes is not null)
            {
                canvas.Content(nodes.Spec, ContentProps.SankeyNode(nodes, plot, i, paint));
            }
            else if (Drawable(node))
            {
                canvas.Shape(Box(plot, node), paint, "recharts-rectangle recharts-sankey-node"u8);
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

    public static void Hits<TCanvas>(ref TCanvas canvas, SankeyPlot plot, int active)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Open("pictor-hits"u8);
        for (var i = 0; i < plot.Links.Length; i++)
        {
            if ((active < 0 || active == i) && Curve(plot, plot.Links[i]) is var curve)
            {
                canvas.HitShape(curve.Finite ? new LinkBandShape(curve) : default);
            }
        }

        for (var i = 0; i < plot.Nodes.Length; i++)
        {
            if (active < 0 || active == plot.Links.Length + i)
            {
                canvas.HitShape(Drawable(plot.Nodes[i]) ? Box(plot, plot.Nodes[i]) : Nowhere);
            }
        }

        canvas.Close();
    }

    public static void Tips<TCanvas>(ref TCanvas canvas, SankeyPlot plot, int active)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Open("pictor-tips"u8);
        for (var i = 0; i < plot.HoverCount; i++)
        {
            if (active < 0 || active == i)
            {
                ItemTips.Tooltip(ref canvas, plot.Spec, Tip(plot, i), 0, 0, plot.Spec.Width, plot.Spec.Height, index: i);
            }
        }

        canvas.Close();
    }

    public static int Hovered(SankeyPlot plot, double x, double y)
    {
        if (plot.HoverCount == 0)
        {
            return ChartPainter.NoTips;
        }

        for (var i = plot.Nodes.Length - 1; i >= 0; i--)
        {
            var node = plot.Nodes[i];
            var left = node.X + plot.Spec.Margin.Left;
            var top = node.Y + plot.Spec.Margin.Top;
            if (Drawable(node) && x >= left && x <= left + node.Dx && y >= top && y <= top + node.Dy)
            {
                return plot.Links.Length + i;
            }
        }

        for (var i = plot.Links.Length - 1; i >= 0; i--)
        {
            if (Covers(Curve(plot, plot.Links[i]), x, y))
            {
                return i;
            }
        }

        return ChartPainter.NoTips;
    }

    private static bool Covers(LinkCurve curve, double x, double y)
    {
        if (!(x >= curve.SourceX && x <= curve.TargetX))
        {
            return false;
        }

        var (low, high) = (0.0, 1.0);
        for (var step = 0; step < 32; step++)
        {
            var middle = (low + high) / 2;
            (low, high) = curve.At(middle, vertical: false) < x ? (middle, high) : (low, middle);
        }

        return Math.Abs(curve.At(low, vertical: true) - y) <= curve.Width / 2;
    }

    private static ItemTip Tip(SankeyPlot plot, int index)
    {
        if (index < plot.Links.Length)
        {
            var link = plot.Links[index];
            var curve = Curve(plot, link);
            var name = (plot.Nodes[link.Source].Name ?? string.Empty) + " - " + (plot.Nodes[link.Target].Name ?? string.Empty);
            return new ItemTip(Finite((curve.SourceX + curve.TargetX) / 2), Finite((curve.SourceY + curve.TargetY) / 2), name, link.Value);
        }

        var node = plot.Nodes[index - plot.Links.Length];
        return new ItemTip(Finite(node.X + plot.Spec.Margin.Left + (node.Dx / 2)), Finite(node.Y + plot.Spec.Margin.Top + (node.Dy / 2)), node.Name ?? string.Empty, node.Value);
    }

    /// <summary>A coordinate of a node or link that has no place (a node without links, a link of no value): the origin, where its tip stands unseen since its hit target covers nothing.</summary>
    private static double Finite(double value) => double.IsFinite(value) ? value : 0;

    /// <summary>The hit target of a node or link that is not drawn: a rectangle of no size, so the state keeps its place among the others and is never entered.</summary>
    private static RectShape Nowhere => new(0, 0, 0, 0, []);

    private static LinkCurve Curve(SankeyPlot plot, SankeyLink link)
    {
        var margin = plot.Spec.Margin;
        var source = plot.Nodes[link.Source];
        var target = plot.Nodes[link.Target];
        var sourceX = source.X + source.Dx + margin.Left;
        var targetX = target.X + margin.Left;
        var curvature = plot.Sankey.LinkCurvature;
        return new LinkCurve(
            sourceX,
            source.Y + link.Sy + (link.Dy / 2) + margin.Top,
            sourceX + ((targetX - sourceX) * curvature),
            targetX,
            target.Y + link.Ty + (link.Dy / 2) + margin.Top,
            sourceX + ((targetX - sourceX) * (1 - curvature)),
            link.Dy);
    }

    private static RectShape Box(SankeyPlot plot, SankeyNode node) => new(node.X + plot.Spec.Margin.Left, node.Y + plot.Spec.Margin.Top, node.Dx, node.Dy, []);

    private static bool Drawable(SankeyNode node) => double.IsFinite(node.X) && double.IsFinite(node.Y) && double.IsFinite(node.Dx) && double.IsFinite(node.Dy) && node.Dx != 0 && node.Dy != 0;
}
