using System.Collections.Immutable;
using Aioros.Graphs.Paths;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Text;

namespace Aioros.Pictor.Painting;

/// <summary>Recharts' <c>Polygon</c> of three points, closed back to the first: a nest treemap's arrow.</summary>
internal readonly struct ArrowShape(double x, double y, double height) : IShape
{
    public void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink
    {
        var middle = y + (height / 2);
        sink.MoveTo(x + 2, middle);
        sink.LineTo(x + 6, middle + 3);
        sink.LineTo(x + 2, middle + 6);
        sink.LineTo(x + 2, middle);
        sink.ClosePath();
    }
}

/// <summary>
/// Paints a treemap as Recharts renders it with its default content: a <c>recharts-treemap-depth-N</c> layer per node
/// holding the node's rectangle (its color from the panel down to depth 1), the name where Inter's measure of it fits,
/// a nest treemap's arrows, then the node's children; then the hover states and a nest treemap's index.
/// </summary>
internal static class TreemapPainter
{
    private const string Transparent = "rgba(255,255,255,0)";

    public static void Paint<TCanvas>(ref TCanvas canvas, TreemapPlot plot, int active)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Begin(plot);
        var serial = 0;
        Node(ref canvas, plot, plot.Root, ref serial);
        if (active != ChartPainter.NoTips && plot.HoverCount > 0)
        {
            Hits(ref canvas, plot, active);
            Tips(ref canvas, plot, active);
        }

        canvas.EndSurface();
        if (plot.Treemap.Nest)
        {
            canvas.NestIndex(plot.Spec.Height, plot.Spec.Width, plot.Spec.Height + 30, plot.Trail, plot.Treemap.NestIndexContent);
        }

        canvas.End();
    }

    public static void Hits<TCanvas>(ref TCanvas canvas, TreemapPlot plot, int active)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Open("pictor-hits"u8);
        for (var i = 0; i < plot.Hovered.Length; i++)
        {
            if (active < 0 || active == i)
            {
                var node = plot.Hovered[i];
                canvas.HitShape(new RectShape(node.X, node.Y, node.Width, node.Height, []));
            }
        }

        canvas.Close();
    }

    public static void Tips<TCanvas>(ref TCanvas canvas, TreemapPlot plot, int active)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Open("pictor-tips"u8);
        for (var i = 0; i < plot.Hovered.Length; i++)
        {
            if (active < 0 || active == i)
            {
                var node = plot.Hovered[i];
                ItemTips.Tooltip(ref canvas, plot.Spec, new ItemTip(node.X + (node.Width / 2), node.Y + (node.Height / 2), node.TipName, node.Value), 0, 0, plot.Spec.Width, plot.Spec.Height, index: i);
            }
        }

        canvas.Close();
    }

    public static int Hovered(TreemapPlot plot, double x, double y)
    {
        if (plot.HoverCount == 0)
        {
            return ChartPainter.NoTips;
        }

        for (var i = plot.Hovered.Length - 1; i >= 0; i--)
        {
            var node = plot.Hovered[i];
            if (x >= node.X && x <= node.X + node.Width && y >= node.Y && y <= node.Y + node.Height && y <= plot.Spec.Height)
            {
                return i;
            }
        }

        return ChartPainter.NoTips;
    }

    private static void Node<TCanvas>(ref TCanvas canvas, TreemapPlot plot, TreemapNode node, ref int serial)
        where TCanvas : struct, IChartCanvas
    {
        var treemap = plot.Treemap;
        var at = serial++;
        OpenNode(ref canvas, treemap, node);
        if (Motion.Animations.Of(treemap) is not null && !node.Kept)
        {
            canvas.Animate(new Motion.Step(Motion.StepKind.Slide, Offset: Offset(node, at)));
        }

        canvas.Open("recharts-layer"u8);
        if (ContentProps.Of(treemap.Content, Content.ContentSlots.TreemapNode, null) is { } content)
        {
            canvas.Content(content.Spec, ContentProps.TreemapNode(content, plot, node, Fill(plot, node, panel: true), at));
            canvas.Close();
            Children(ref canvas, plot, node, ref serial);
            canvas.Close();
            return;
        }

        canvas.Open(default);
        if (node.Width != 0 && node.Height != 0)
        {
            if (!treemap.UpdateAnimation && Motion.Animations.Of(treemap) is not null && !node.Kept)
            {
                canvas.Animate(new Motion.Step(Motion.StepKind.Stroke, Offset: node.Depth > 2 ? 1 : 0));
            }

            var fill = Fill(plot, node, panel: false);
            canvas.Shape(new RectShape(node.X, node.Y, node.Width, node.Height, []), new Paint(treemap.Stroke ?? plot.Spec.Colors.TreemapStroke, double.NaN, fill), "recharts-rectangle"u8);
        }

        if (treemap.Nest && node.Width > 10 && node.Height > 10 && node.Children is not null)
        {
            canvas.Shape(new ArrowShape(node.X, node.Y, node.Height), new Paint(null, double.NaN, plot.Spec.Colors.TreemapArrow), "recharts-polygon"u8);
        }

        var fontSize = plot.Spec.FontSize;
        if (node.Width > 20 && node.Height > 20 && Measure(node.Name, fontSize) is var (width, height) && width < node.Width && height < node.Height)
        {
            canvas.PlainText(node.X + 8, node.Y + (node.Height / 2) + 7, node.Name, 14, treemap.Fill, treemap.Stroke);
        }

        canvas.Close();
        canvas.Close();
        Children(ref canvas, plot, node, ref serial);
        canvas.Close();
    }

    private static void Children<TCanvas>(ref TCanvas canvas, TreemapPlot plot, TreemapNode node, ref int serial)
        where TCanvas : struct, IChartCanvas
    {
        if (!plot.Treemap.Nest || node.Depth == 0)
        {
            foreach (var child in node.Children ?? [])
            {
                Node(ref canvas, plot, child, ref serial);
            }
        }
    }

    /// <summary>A node's fill: the treemap's, else the color panel's down to depth 1 (<paramref name="panel"/>: the panel's always, as custom content receives it), transparent below.</summary>
    public static string Fill(TreemapPlot plot, TreemapNode node, bool panel) =>
        (panel ? null : plot.Treemap.Fill) ?? (node.Depth < 2 ? plot.Colors[node.Index % plot.Colors.Length] : Transparent);

    /// <summary>Opens a node's depth layer, the root's animated when the treemap animates in; the node's own content slides in inside it (except a node an update keeps, which resizes), so no node moves by its ancestors' offsets.</summary>
    private static void OpenNode<TCanvas>(ref TCanvas canvas, Specs.TreemapSpec treemap, TreemapNode node)
        where TCanvas : struct, IChartCanvas
    {
        if (Motion.Animations.Of(treemap) is { } value && node.Depth == 0)
        {
            canvas.OpenMotion(Depth(0), value);
            return;
        }

        canvas.Open(Depth(node.Depth));
    }

    /// <summary>
    /// Where a node slides in from, along both axes: Recharts draws a random share of its width, strictly between minus
    /// and plus one; Pictor takes the golden-ratio sequence over the nodes in document order from 1, so every render is
    /// the same and a first child does not start a full width away.
    /// </summary>
    private static double Offset(TreemapNode node, int serial) => Math.Truncate(((((serial + 1) * 0.6180339887498949) % 1 * 2) - 1) * node.Width);

    private static (double Width, double Height) Measure(string? name, double fontSize) =>
        name is null ? (0, 0) : (InterMetrics.Width(name, fontSize), InterMetrics.LineHeight(fontSize));

    private static ReadOnlySpan<byte> Depth(int depth) => depth switch
    {
        0 => "recharts-layer recharts-treemap-depth-0"u8,
        1 => "recharts-layer recharts-treemap-depth-1"u8,
        2 => "recharts-layer recharts-treemap-depth-2"u8,
        3 => "recharts-layer recharts-treemap-depth-3"u8,
        4 => "recharts-layer recharts-treemap-depth-4"u8,
        5 => "recharts-layer recharts-treemap-depth-5"u8,
        6 => "recharts-layer recharts-treemap-depth-6"u8,
        7 => "recharts-layer recharts-treemap-depth-7"u8,
        _ => System.Text.Encoding.UTF8.GetBytes("recharts-layer recharts-treemap-depth-" + depth.ToString(System.Globalization.CultureInfo.InvariantCulture)),
    };
}
