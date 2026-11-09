using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor;

/// <summary>
/// A canvas that draws nothing and lays out every refused template's content for each item it is called for, as the
/// spec of a page rendered by Avalonia carries it (<see cref="ChartSpec.Resolved"/>), so the web writes it as Avalonia
/// laid it out.
/// </summary>
internal struct ContentRecorder(ContentTemplates templates) : IChartCanvas
{
    private readonly Dictionary<long, ResolvedContent> _resolved = [];

    /// <summary>The content of every refused template's call, in the order they were made.</summary>
    public readonly IEnumerable<ResolvedContent> Resolved => _resolved.Values;

    /// <summary>The resolved content of every call a chart's paint makes, with every hover state.</summary>
    public static System.Collections.Immutable.ImmutableArray<ResolvedContent> Of(IChartPlot plot, ContentTemplates templates)
    {
        var canvas = new ContentRecorder(templates);
        Plots.Paint(ref canvas, plot, ChartPainter.AllTips);
        return [.. canvas.Resolved];
    }

    public readonly void Content(ContentSpec content, in Content.ContentItem item)
    {
        if (content.Refused is not null && !_resolved.ContainsKey(item.Key))
        {
            _resolved[item.Key] = new ResolvedContent { Key = item.Key, Nodes = templates.Resolve(content, item) };
        }
    }

    public readonly void Tooltip(double x, double y, string label, ReadOnlySpan<TooltipEntry> entries, TooltipSpec spec, in TipFollow follow)
    {
    }

    public readonly void Html(double x, double y, ContentSpec content, in Content.ContentItem item, TooltipSpec tooltip, in TipFollow follow, TooltipLines? lines) => Content(content, item);

    public readonly void Begin(IChartPlot plot)
    {
    }

    public readonly void Open(ReadOnlySpan<byte> classes)
    {
    }

    public readonly void OpenSlider(ReadOnlySpan<byte> classes, string label, double value)
    {
    }

    public readonly void OpenMotion(ReadOnlySpan<byte> classes, in Motion.Timing timing)
    {
    }

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

    public readonly void Close()
    {
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

    public readonly void Hit(double x, double y, double width, double height)
    {
    }

    public readonly void HitShape<TShape>(in TShape shape)
        where TShape : struct, IShape
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

    public readonly void BrushFields(BrushLayout brush, string startText, string endText)
    {
    }

    public readonly void OpenItem(string name)
    {
    }

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

}
