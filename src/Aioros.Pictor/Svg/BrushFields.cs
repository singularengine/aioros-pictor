using System.Buffers;
using Aioros.Pictor.Layout;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>
/// The web brush's two travellers as native range inputs over the strip, posted as the framework's fields without
/// script: each input spans the strip with a transparent thumb as wide and tall as Recharts' traveller, so its thumb
/// sits exactly over the traveller at every index (a thumb's left edge is at its value's share of the strip less a
/// thumb, as Recharts' point scale puts a traveller over the strip less a traveller). At rest the brush is Recharts'
/// drawing; while a thumb is dragged it is drawn as the traveller and the drawn one is hidden, and the change posts
/// the form, whose answer is the chart over the new window. The inputs take the focus and move by the arrow keys, as
/// Recharts' travellers do (<c>role="slider"</c>, <c>ArrowLeft</c>, <c>ArrowRight</c>).
/// </summary>
internal static class BrushFields
{
    public const string Start = "brush-start";

    public const string End = "brush-end";

    /// <summary>The slide's composite field over the two indexes: its posted value is the new start, the end keeping the span.</summary>
    public const string Slide = "brush-slide";

    /// <summary>
    /// The base the slide's posted value is written in: three digits of it, the item count, the start the slide was
    /// written at and the start it is moved to (<c>(count × base + written) × base + start</c>). The decoder, which reads
    /// only the indexes' current values, then knows the last index (an unset end) and keeps the window inside the data,
    /// and tells a slide the post did not move (the framework posts every field of the form) from one it moved. The
    /// largest value stays a whole number a browser's double holds exactly.
    /// </summary>
    public const long SlideBase = 200_000;

    /// <summary>
    /// The window a posted slide moves to, from the indexes' current values (a null end is the last item, as Recharts'
    /// <c>endIndex</c> defaults): the count, the written start and the new start decoded, the current window clamped to the
    /// data and ordered, and the new start clamped to the starts that keep its span inside the data, as Recharts' slide
    /// stops at the edges. None for a slide posted at the start it was written at: the post moved a traveller, not the slide.
    /// </summary>
    public static (int Start, int End)? Slid(long posted, object? start, object? end)
    {
        var count = posted / SlideBase / SlideBase;
        var written = posted / SlideBase % SlideBase;
        var moved = posted % SlideBase;
        if (posted < 0 || count < 1 || count >= SlideBase || moved == written)
        {
            return null;
        }

        var last = (int)count - 1;
        var from = Math.Clamp(start as int? ?? 0, 0, last);
        var to = end is int set ? Math.Clamp(set, 0, last) : last;
        var span = Math.Abs(to - from);
        var slid = (int)Math.Clamp(moved, 0, last - span);
        return (slid, slid + span);
    }

    /// <summary>
    /// The window a posted composite field moves to: a slide without a gap as <see cref="Slid"/> gives it; with a gap (named
    /// by the key, <c>brush-start/5</c>), Recharts' <c>getIndex</c> of the travellers, the smaller index snapped down to the
    /// gap and the larger too unless it is the last item. A move that snaps back onto the current window goes on in its
    /// direction to the next one, since the web traveller stands at its window's index where Recharts' stays where it was
    /// left. None for a post at the place it was written at, with nothing left in its direction, or under another key.
    /// </summary>
    public static (int Start, int End)? Moved(string key, long posted, object? start, object? end)
    {
        var at = key.IndexOf('/', StringComparison.Ordinal);
        if (at < 0)
        {
            return key == Slide ? Slid(posted, start, end) : null;
        }

        var field = key[..at];
        var count = posted / SlideBase / SlideBase;
        var written = posted / SlideBase % SlideBase;
        var moved = posted % SlideBase;
        if (field is not (Start or End or Slide) || !int.TryParse(key.AsSpan(at + 1), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var gap)
            || gap < 2 || posted < 0 || count < 1 || count >= SlideBase || moved == written)
        {
            return null;
        }

        var last = (int)count - 1;
        var from = Math.Clamp(start as int? ?? 0, 0, last);
        var to = end is int set ? Math.Clamp(set, 0, last) : last;
        return Snapped(field, gap, last, (from, to), Math.Sign(moved - written), moved);
    }

    /// <summary>The first window Recharts' <c>getIndex</c> gives that differs from the current one, from the moved place on in its direction.</summary>
    private static (int Start, int End)? Snapped(string field, int gap, int last, (int From, int To) travellers, int direction, long moved)
    {
        var current = (Math.Min(travellers.From, travellers.To), Math.Max(travellers.From, travellers.To));
        var span = current.Item2 - current.Item1;
        var other = field == Start ? travellers.To : travellers.From;
        var highest = field == Slide ? last - span : last;
        var place = (int)Math.Clamp(moved, 0, highest);
        while (true)
        {
            var window = GetIndex(place, field == Slide ? place + span : other, gap, last);
            if (window != current)
            {
                return window;
            }

            place += direction;
            if (place < 0 || place > highest || (field != Slide && place == other))
            {
                return null;
            }
        }
    }

    /// <summary>Recharts' <c>getIndex</c> over two indexes.</summary>
    private static (int Start, int End) GetIndex(int first, int second, int gap, int last)
    {
        var (min, max) = (Math.Min(first, second), Math.Max(first, second));
        return (min - (min % gap), max == last ? last : max - (max % gap));
    }

    private static ReadOnlySpan<byte> Rules => "<style>.pictor-brush-fields{overflow:visible}.pictor-brush-fields>div{position:relative;width:100%;height:100%}.pictor-brush-fields input{-webkit-appearance:none;appearance:none;position:absolute;left:0;top:0;width:100%;height:100%;margin:0;padding:0;border:0;background:none;pointer-events:none;outline:none}.pictor-brush-fields input::-webkit-slider-runnable-track{height:100%;background:none;border:0}.pictor-brush-fields input::-moz-range-track{height:100%;background:none;border:0}.pictor-brush-fields input::-webkit-slider-thumb{-webkit-appearance:none;appearance:none;width:var(--pictor-tw);height:var(--pictor-th);background:none;border:0;pointer-events:auto;cursor:col-resize}.pictor-brush-fields input::-moz-range-thumb{width:var(--pictor-tw);height:var(--pictor-th);background:none;border:0;border-radius:0;pointer-events:auto;cursor:col-resize}.pictor-brush-fields input:active::-webkit-slider-thumb{background:linear-gradient(#fff,#fff) 1px calc(50% - 1px)/calc(100% - 2px) 1px no-repeat,linear-gradient(#fff,#fff) 1px calc(50% + 1px)/calc(100% - 2px) 1px no-repeat,var(--pictor-ts)}.pictor-brush-fields input:active::-moz-range-thumb{background:linear-gradient(#fff,#fff) 1px calc(50% - 1px)/calc(100% - 2px) 1px no-repeat,linear-gradient(#fff,#fff) 1px calc(50% + 1px)/calc(100% - 2px) 1px no-repeat,var(--pictor-ts)}.pictor-brush-fields input:focus-visible::-webkit-slider-thumb{outline:2px solid var(--pictor-ts);outline-offset:1px}.pictor-brush-fields input:focus-visible::-moz-range-thumb{outline:2px solid var(--pictor-ts);outline-offset:1px}.recharts-surface:has(.pictor-b0:active) .recharts-brush>.recharts-brush-slide+g,.recharts-surface:has(.pictor-b1:active) .recharts-brush>.recharts-brush-slide+g+g{visibility:hidden}.pictor-brush-fields .pictor-b0,.pictor-brush-fields .pictor-b1{z-index:1}.pictor-brush-fields .pictor-bs::-webkit-slider-thumb{width:var(--pictor-sw);cursor:move}.pictor-brush-fields .pictor-bs::-moz-range-thumb{width:var(--pictor-sw);cursor:move}.pictor-brush-fields .pictor-bs:active::-webkit-slider-thumb{background:linear-gradient(#fff,#fff) 1px calc(50% - 1px)/calc(var(--pictor-tw) - 2px) 1px no-repeat,linear-gradient(#fff,#fff) 1px calc(50% + 1px)/calc(var(--pictor-tw) - 2px) 1px no-repeat,linear-gradient(#fff,#fff) right 1px top calc(50% - 1px)/calc(var(--pictor-tw) - 2px) 1px no-repeat,linear-gradient(#fff,#fff) right 1px top calc(50% + 1px)/calc(var(--pictor-tw) - 2px) 1px no-repeat,linear-gradient(var(--pictor-ts),var(--pictor-ts)) left/var(--pictor-tw) 100% no-repeat,linear-gradient(var(--pictor-ts),var(--pictor-ts)) right/var(--pictor-tw) 100% no-repeat,linear-gradient(var(--pictor-sf),var(--pictor-sf)) center/calc(100% - 2 * var(--pictor-tw)) 100% no-repeat}.pictor-brush-fields .pictor-bs:active::-moz-range-thumb{background:linear-gradient(#fff,#fff) 1px calc(50% - 1px)/calc(var(--pictor-tw) - 2px) 1px no-repeat,linear-gradient(#fff,#fff) 1px calc(50% + 1px)/calc(var(--pictor-tw) - 2px) 1px no-repeat,linear-gradient(#fff,#fff) right 1px top calc(50% - 1px)/calc(var(--pictor-tw) - 2px) 1px no-repeat,linear-gradient(#fff,#fff) right 1px top calc(50% + 1px)/calc(var(--pictor-tw) - 2px) 1px no-repeat,linear-gradient(var(--pictor-ts),var(--pictor-ts)) left/var(--pictor-tw) 100% no-repeat,linear-gradient(var(--pictor-ts),var(--pictor-ts)) right/var(--pictor-tw) 100% no-repeat,linear-gradient(var(--pictor-sf),var(--pictor-sf)) center/calc(100% - 2 * var(--pictor-tw)) 100% no-repeat}.pictor-brush-fields .pictor-bs:focus-visible::-webkit-slider-thumb{outline:2px solid var(--pictor-ts);outline-offset:1px}.pictor-brush-fields .pictor-bs:focus-visible::-moz-range-thumb{outline:2px solid var(--pictor-ts);outline-offset:1px}.recharts-surface:has(.pictor-bs:active) .recharts-brush>.recharts-brush-slide+g,.recharts-surface:has(.pictor-bs:active) .recharts-brush>.recharts-brush-slide+g+g{visibility:hidden}.recharts-surface:has(.pictor-b0:active,.pictor-b1:active,.pictor-bs:active) .recharts-brush-slide{visibility:hidden}.pictor-brush-fields .pictor-b0:active,.pictor-brush-fields .pictor-b1:active{clip-path:var(--pictor-c)}.pictor-brush-fields .pictor-b0:active::-webkit-slider-thumb{box-shadow:var(--pictor-sl) 0 0 var(--pictor-sl) var(--pictor-sf)}.pictor-brush-fields .pictor-b0:active::-moz-range-thumb{box-shadow:var(--pictor-sl) 0 0 var(--pictor-sl) var(--pictor-sf)}.pictor-brush-fields .pictor-b1:active::-webkit-slider-thumb{box-shadow:calc(-1 * var(--pictor-sl)) 0 0 var(--pictor-sl) var(--pictor-sf)}.pictor-brush-fields .pictor-b1:active::-moz-range-thumb{box-shadow:calc(-1 * var(--pictor-sl)) 0 0 var(--pictor-sl) var(--pictor-sf)}</style>"u8;

    /// <summary>The key a brush posts a field under: a brush with a gap posts its travellers and its slide as composite fields whose key names the gap, so the decoder snaps the window to it.</summary>
    public static string Key(string field, int gap) => gap > 1 ? field + "/" + gap.ToString(System.Globalization.CultureInfo.InvariantCulture) : field;

    public static void Write(MarkupBuffer output, ChartMarks? marks, BrushLayout brush, string stroke, string startText, string endText)
    {
        var spec = brush.Spec;
        Raw(output, Rules);
        Raw(output, "<foreignObject class=\"pictor-brush-fields\""u8);
        Attribute(output, " x"u8, brush.X);
        Attribute(output, " y"u8, brush.Y);
        Attribute(output, " width"u8, brush.Width);
        Attribute(output, " height"u8, brush.Height);
        Raw(output, " style=\"--pictor-tw:"u8);
        Number(output, spec.TravellerWidth);
        Raw(output, "px;--pictor-th:"u8);
        Number(output, brush.Height);
        Raw(output, "px;--pictor-ts:"u8);
        Utf8Markup.Text(output, stroke);
        Raw(output, ";--pictor-sl:"u8);
        Number(output, brush.Width);
        Raw(output, "px;--pictor-sf:color-mix(in srgb,"u8);
        Utf8Markup.Text(output, stroke);
        Raw(output, " 20%,transparent)\"><div>"u8);
        if (spec.Gap > 1 && brush.Count < SlideBase)
        {
            Hidden(output, marks, Start, brush.StartIndex);
            Hidden(output, marks, End, brush.EndIndex);
        }

        Input(output, marks, brush, Start, brush.StartIndex, startText);
        Input(output, marks, brush, End, brush.EndIndex, endText);
        SlideInput(output, marks, brush, startText, endText);
        Raw(output, "</div></foreignObject>"u8);
    }

    /// <summary>
    /// The slide as one range input over the whole strip, below the travellers' inputs: its value the start index, its
    /// largest the last start that keeps the span, its thumb spanning both travellers and the slide between them (the
    /// span's share of the strip less a traveller, plus a traveller), so at every value it covers the window Recharts
    /// draws there; dragging it, or its arrow keys, post the composite field whose start moves the end by the same amount.
    /// While it is dragged it is drawn as the two travellers at its ends with the slide's fill between them.
    /// </summary>
    private static void SlideInput(MarkupBuffer output, ChartMarks? marks, BrushLayout brush, string startText, string endText)
    {
        var (start, end) = (Math.Min(brush.StartIndex, brush.EndIndex), Math.Max(brush.StartIndex, brush.EndIndex));
        var span = end - start;
        var steps = Math.Max(brush.Count - 1, 1);
        if (brush.Count >= SlideBase)
        {
            return;
        }

        var offset = Offset(brush.Count, start);
        Raw(output, "<input type=\"range\" class=\"pictor-bs\""u8);
        Attribute(output, " min"u8, offset);
        Attribute(output, " max"u8, offset + Math.Max(brush.Count - 1 - span, 0));
        if (Steps(brush.Spec.Gap, start))
        {
            Attribute(output, " step"u8, brush.Spec.Gap);
        }

        Attribute(output, " value"u8, offset + start);
        Raw(output, " style=\"--pictor-sw:"u8);
        Number(output, (span * (brush.Width - brush.Spec.TravellerWidth) / steps) + brush.Spec.TravellerWidth);
        Raw(output, "px\""u8);
        Attribute(output, " aria-valuetext"u8, startText + " – " + endText);
        Attribute(output, " aria-label"u8, brush.Spec.AriaLabel is { } label ? label + " (window)" : "Window");
        marks?.Field(Key(Slide, brush.Spec.Gap));
        Raw(output, ">"u8);
    }

    /// <summary>
    /// An index the brush was written at as a hidden plain field, before the composite fields of a brush with a gap: the post
    /// binds it first, so a composite's decoder reads the window the page was written at (a fresh model holds its defaults).
    /// </summary>
    private static void Hidden(MarkupBuffer output, ChartMarks? marks, string key, int index)
    {
        Raw(output, "<input type=\"hidden\""u8);
        Attribute(output, " value"u8, index);
        marks?.Field(key);
        Raw(output, ">"u8);
    }

    /// <summary>The value a composite field's range starts at: the item count and the index it was written at, in <see cref="SlideBase"/>'s digits.</summary>
    private static double Offset(int count, int written) => (((long)count * SlideBase) + written) * SlideBase;

    /// <summary>
    /// Whether an input steps by the brush's gap: only when its value is on a step, since a browser moves a value off its
    /// steps onto one and posts that (a brush at 3 with a gap of 5 posts 5 with any other field of the form), and an end's
    /// only when its largest value, the last item, is on one too (or the end could never reach it).
    /// </summary>
    private static bool Steps(int gap, int value) => gap > 1 && value % gap == 0;

    /// <summary>
    /// A traveller's range input: its index as a plain field, or, for a brush with a gap, as a composite field whose range
    /// starts at the item count and the index it was written at (so the decoder snaps the window as Recharts' <c>getIndex</c>
    /// does and refuses a post that did not move it). While it is dragged, it is clipped at the other traveller's edge and
    /// its thumb's shadow draws the slide's fill up to there.
    /// </summary>
    private static void Input(MarkupBuffer output, ChartMarks? marks, BrushLayout brush, string key, int index, string text)
    {
        var last = Math.Max(brush.Count - 1, 0);
        var composite = brush.Spec.Gap > 1 && brush.Count < SlideBase;
        var offset = composite ? Offset(brush.Count, index) : 0;
        Raw(output, key == Start ? "<input type=\"range\" class=\"pictor-b0\" style=\"--pictor-c:inset(0 "u8 : "<input type=\"range\" class=\"pictor-b1\" style=\"--pictor-c:inset(0 0 0 "u8);
        Number(output, key == Start ? brush.X + brush.Width - brush.EndX : brush.StartX + brush.Spec.TravellerWidth - brush.X);
        Raw(output, key == Start ? "px 0 0)\""u8 : "px)\""u8);
        Attribute(output, " min"u8, offset);
        Attribute(output, " max"u8, offset + last);
        if (Steps(brush.Spec.Gap, index) && (key == Start || Steps(brush.Spec.Gap, last)))
        {
            Attribute(output, " step"u8, brush.Spec.Gap);
        }

        Attribute(output, " value"u8, offset + index);
        Attribute(output, " aria-valuetext"u8, text);
        if (brush.Spec.AriaLabel is { } label)
        {
            Attribute(output, " aria-label"u8, label);
        }
        else
        {
            Raw(output, key == Start ? " aria-label=\"Start of the window\""u8 : " aria-label=\"End of the window\""u8);
        }
        marks?.Field(composite ? Key(key, brush.Spec.Gap) : key);
        Raw(output, ">"u8);
    }
}
