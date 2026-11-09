using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Automation;

/// <summary>A canvas that draws nothing and keeps the tooltip a hover state shows, as the text assistive technologies read for it.</summary>
internal struct TipTexts : IChartCanvas
{
    /// <summary>The tooltip's text: its label, then each entry's name and value.</summary>
    public string? Read { get; private set; }

    /// <summary>The text of every hover state of a chart, in order.</summary>
    public static string[] Of(IChartPlot plot)
    {
        var texts = new string[plot.HoverCount];
        for (var i = 0; i < texts.Length; i++)
        {
            var canvas = default(TipTexts);
            Plots.HoverAbove(ref canvas, plot, i);
            texts[i] = canvas.Read ?? string.Empty;
        }

        return texts;
    }

    public void Tooltip(double x, double y, string label, ReadOnlySpan<TooltipEntry> entries, TooltipSpec spec, in TipFollow follow)
    {
        var parts = new List<string>(entries.Length + 1);
        if (!string.IsNullOrEmpty(label))
        {
            parts.Add(label);
        }

        foreach (var entry in entries)
        {
            parts.Add(string.IsNullOrEmpty(entry.Name) ? entry.Value : $"{entry.Name}: {entry.Value}");
        }

        Read = string.Join(", ", parts);
    }

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

    public readonly void Content(Specs.ContentSpec content, in Content.ContentItem item)
    {
    }

    public readonly void Html(double x, double y, Specs.ContentSpec content, in Content.ContentItem item, Specs.TooltipSpec tooltip, in TipFollow follow, Painting.TooltipLines? lines)
    {
    }
}
