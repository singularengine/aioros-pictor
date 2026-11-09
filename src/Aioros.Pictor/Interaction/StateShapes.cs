using System.Buffers;
using System.Text;
using Aioros.Graphs.Svg;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Interaction;

/// <summary>The area of a hover state (or of an item) as the painter's hit areas give it: a rectangle, or a shape's path.</summary>
internal readonly record struct StateShape(double X, double Y, double Width, double Height, string? Path);

/// <summary>
/// The hover states' areas of a laid-out chart, read from the painter's own hit areas: a canvas that draws nothing and
/// keeps the rectangle or the path of each state's hit area, so a click target of a state (a category, a sector, a
/// node, a link) covers exactly what hovering it covers, on every family.
/// </summary>
internal struct StateShapes : IChartCanvas
{
    private readonly List<List<StateShape>> _states;
    private int _depth;
    private int _layerDepth;
    private bool _inHits;

    private StateShapes(List<List<StateShape>> states) => _states = states;

    /// <summary>The areas of every hover state, in the states' order.</summary>
    public static List<List<StateShape>> Of(IChartPlot plot)
    {
        var canvas = new StateShapes([]);
        Plots.Paint(ref canvas, plot, ChartPainter.AllTips);
        return canvas._states;
    }

    /// <summary>A shape's SVG path, as the web's clip and the desktop's hit test read it.</summary>
    public static string PathOf<TShape>(in TShape shape)
        where TShape : struct, IShape
    {
        var buffer = new ArrayBufferWriter<byte>(256);
        var sink = new SvgPathWriter(buffer, TShape.Digits);
        shape.Write(ref sink);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public readonly void Begin(IChartPlot plot)
    {
    }

    public void OpenSlider(ReadOnlySpan<byte> classes, string label, double value) => Open(classes);

    public void Open(ReadOnlySpan<byte> classes)
    {
        _depth++;
        if (!_inHits && HoverMarkup.LayerOf(classes) == HoverMarkup.Layer.Hits)
        {
            (_inHits, _layerDepth) = (true, _depth);
        }
        else if (_inHits && _depth == _layerDepth + 1)
        {
            _states.Add([]);
        }
    }

    public void OpenMotion(ReadOnlySpan<byte> classes, in Motion.Timing timing) => _depth++;

    public readonly void Animate(in Motion.Step step)
    {
    }

    public readonly void Grow(in Motion.GrowingArc arc)
    {
    }

    public readonly void MorphLayer(in Motion.MorphLayer layer)
    {
    }

    public readonly void Morph(in Motion.MorphMark mark)
    {
    }

    public void Close()
    {
        if (_inHits && _depth == _layerDepth)
        {
            _inHits = false;
        }

        _depth--;
    }

    public readonly void Line(double x1, double y1, double x2, double y2, in Paint paint, ReadOnlySpan<byte> classes)
    {
    }

    public readonly void Shape<TShape>(in TShape shape, in Paint paint, ReadOnlySpan<byte> classes)
        where TShape : struct, IShape
    {
    }

    public readonly void Circle(double cx, double cy, double r, in Paint paint, ReadOnlySpan<byte> classes)
    {
    }

    public readonly void Text(double x, double y, string text, in TextPaint paint, ReadOnlySpan<byte> classes)
    {
    }

    public readonly void EndSurface()
    {
    }

    public readonly void Legend(LegendLayout legend)
    {
    }

    public readonly void Hit(double x, double y, double width, double height) => Add(new StateShape(x, y, width, height, null));

    public readonly void HitShape<TShape>(in TShape shape)
        where TShape : struct, IShape => Add(new StateShape(0, 0, 0, 0, PathOf(shape)));

    /// <summary>Keeps a hit area: a state of its own when it is a direct child of the hits layer, else a part of the state its group opened.</summary>
    private readonly void Add(StateShape shape)
    {
        if (!_inHits)
        {
            return;
        }

        if (_depth == _layerDepth || _states.Count == 0)
        {
            _states.Add([shape]);
        }
        else
        {
            _states[^1].Add(shape);
        }
    }

    public readonly void Tooltip(double x, double y, string label, ReadOnlySpan<TooltipEntry> entries, TooltipSpec spec, in TipFollow follow)
    {
    }

    public readonly void ArcText(in ArcPath arc, string text, in TextPaint paint, ReadOnlySpan<byte> classes)
    {
    }

    public readonly void End()
    {
    }

    public readonly void PlainText(double x, double y, string? text, double fontSize, string? fill, string? stroke)
    {
    }

    public readonly void NestIndex(double top, double width, double height, NestBox[] trail, Specs.ContentSpec? content)
    {
    }

    public void OpenItem(string name) => _depth++;

    public readonly void OptionsText(double x, double y, string text, TextOptionsSpec options)
    {
    }

    public readonly void Rect(double x, double y, double width, double height, in Paint paint, ReadOnlySpan<byte> classes)
    {
    }

    public readonly void DefineClip(in ClipRect clip)
    {
    }

    public readonly void OpenSurface(double x, double y, double width, double height)
    {
    }

    public readonly void CloseSurface()
    {
    }

    public readonly void ClipNext(in ClipRect clip)
    {
    }

    public readonly void Content(Specs.ContentSpec content, in Content.ContentItem item)
    {
    }

    public readonly void Html(double x, double y, Specs.ContentSpec content, in Content.ContentItem item, Specs.TooltipSpec tooltip, in TipFollow follow, Painting.TooltipLines? lines)
    {
    }

    public readonly void BrushFields(BrushLayout brush, string startText, string endText)
    {
    }
}
