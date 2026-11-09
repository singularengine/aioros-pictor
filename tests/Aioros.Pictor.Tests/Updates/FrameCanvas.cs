using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Updates;

/// <summary>Writes a frame of an animation as the web's markup at that moment: the shapes where the frame puts them, without the movements the web's CSS would add.</summary>
internal struct FrameCanvas(SvgCanvas inner) : IChartCanvas
{
    private SvgCanvas _inner = inner;

    public void Begin(IChartPlot plot) => _inner.Begin(plot);

    public void Open(ReadOnlySpan<byte> classes) => _inner.Open(classes);

    public void OpenSlider(ReadOnlySpan<byte> classes, string label, double value) => _inner.OpenSlider(classes, label, value);

    public void OpenMotion(ReadOnlySpan<byte> classes, in Motion.Timing timing) => _inner.OpenMotion(classes, timing);

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

    public void Close() => _inner.Close();

    public void Line(double x1, double y1, double x2, double y2, in Paint paint, ReadOnlySpan<byte> classes) => _inner.Line(x1, y1, x2, y2, paint, classes);

    public void Shape<TShape>(in TShape shape, in Paint paint, ReadOnlySpan<byte> classes)
        where TShape : struct, IShape => _inner.Shape(shape, paint, classes);

    public void Circle(double cx, double cy, double r, in Paint paint, ReadOnlySpan<byte> classes) => _inner.Circle(cx, cy, r, paint, classes);

    public void Text(double x, double y, string text, in TextPaint paint, ReadOnlySpan<byte> classes) => _inner.Text(x, y, text, paint, classes);

    public void EndSurface() => _inner.EndSurface();

    public void Legend(LegendLayout legend) => _inner.Legend(legend);

    public void Hit(double x, double y, double width, double height) => _inner.Hit(x, y, width, height);

    public void HitShape<TShape>(in TShape shape)
        where TShape : struct, IShape => _inner.HitShape(shape);

    public void Tooltip(double x, double y, string label, ReadOnlySpan<TooltipEntry> entries, TooltipSpec spec, in TipFollow follow) => _inner.Tooltip(x, y, label, entries, spec, follow);

    public void ArcText(in ArcPath arc, string text, in TextPaint paint, ReadOnlySpan<byte> classes) => _inner.ArcText(arc, text, paint, classes);

    public void End() => _inner.End();

    public void PlainText(double x, double y, string? text, double fontSize, string? fill, string? stroke) => _inner.PlainText(x, y, text, fontSize, fill, stroke);

    public void NestIndex(double top, double width, double height, NestBox[] trail, Specs.ContentSpec? content) => _inner.NestIndex(top, width, height, trail, content);

    public void BrushFields(BrushLayout brush, string startText, string endText) => _inner.BrushFields(brush, startText, endText);

    public void OpenItem(string name) => _inner.OpenItem(name);

    public void OptionsText(double x, double y, string text, TextOptionsSpec options) => _inner.OptionsText(x, y, text, options);

    public void Rect(double x, double y, double width, double height, in Paint paint, ReadOnlySpan<byte> classes) => _inner.Rect(x, y, width, height, paint, classes);

    public void DefineClip(in ClipRect clip) => _inner.DefineClip(clip);

    public void OpenSurface(double x, double y, double width, double height) => _inner.OpenSurface(x, y, width, height);

    public void CloseSurface() => _inner.CloseSurface();

    public void ClipNext(in ClipRect clip) => _inner.ClipNext(clip);

    public void Content(ContentSpec content, in Content.ContentItem item) => _inner.Content(content, item);

    public void Html(double x, double y, ContentSpec content, in Content.ContentItem item, TooltipSpec tooltip, in TipFollow follow, Painting.TooltipLines? lines) => _inner.Html(x, y, content, item, tooltip, follow, lines);
}
