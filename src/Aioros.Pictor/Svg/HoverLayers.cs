using System.Buffers;
using Aioros.Runtime.Rendering;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>
/// Where the web's hover states go. The painters draw a layer of hit groups (each a state's hit area and cursor) before
/// the series and a layer of tips after them; the page gets the cursors where the hit layer stands (<c>pictor-cursors</c>,
/// one group <c>pictor-cK</c> per state that draws one, below the series as Recharts draws its cursor), and the hit
/// groups (<c>pictor-hkK</c>, or <c>pictor-ckK</c> for a tooltip shown on click) moved into the tips' layer
/// (<c>pictor-hover</c>), each just before its tip (<c>pictor-tkK</c>). A tip is then the next sibling of its hit group,
/// so one rule for every state (<c>:hover+g</c>) shows it, which the browser invalidates from the group alone; only the
/// cursors below the series need a rule per state, through a <c>:has()</c> on the surface. Both layers keep one element
/// in the surface whatever the number of states, and the pairs follow each other, so the framework's positional morph
/// never shifts the series, nor another state's elements, when states come or go. The series between the layers take
/// no pointer, so the hit groups above them still take it.
/// </summary>
internal sealed class HoverLayers(FocusMarkup.Mode focus, int tabStop, string?[]? values, bool followButtons, PointerFollow? follow = null, int states = 0) : IDisposable
{
    private readonly PooledBuffer _pooled = new(8 * 1024);
    private MarkupBuffer? _markup;
    private readonly List<int> _ends = new(states);
    private int _written;
    private bool _wrapped;

    /// <summary>The hit groups written so far, until the tips' layer takes them.</summary>
    public MarkupBuffer Hits => HitsMarkup;

    private MarkupBuffer HitsMarkup => _markup ??= MarkupBuffer.Over(_pooled);

    /// <summary>The cursors' layer, opened where the painter opens its hit layer.</summary>
    public static void OpenCursors(MarkupBuffer output) => Raw(output, "<g class=\"pictor-cursors\">"u8);

    /// <summary>A state of the hit layer: its cursor group in the page, its hit group put aside.</summary>
    public void OpenState(MarkupBuffer output, int index)
    {
        Raw(output, "<g class=\"pictor-c"u8);
        Number(output, index);
        Value(output, index);
        Raw(output, "\">"u8);
        Raw(HitsMarkup, FocusMarkup.Group(focus, index == tabStop));
        HitClass(index, plain: false);
        Raw(HitsMarkup, ">"u8);
    }

    /// <summary>A state of the hit layer whose group has classes of its own (a legend toggle's around an item's hit shape): its hit group alone, put aside.</summary>
    public void OpenWrapped(ReadOnlySpan<byte> classes, int index)
    {
        _wrapped = true;
        Raw(HitsMarkup, FocusMarkup.Group(focus, index == tabStop));
        Raw(HitsMarkup, " class=\""u8);
        Raw(HitsMarkup, classes);
        Raw(HitsMarkup, focus == FocusMarkup.Mode.Click ? " pictor-ck"u8 : " pictor-hk"u8);
        Number(HitsMarkup, index);
        Value(HitsMarkup, index);
        Raw(HitsMarkup, "\">"u8);
    }

    /// <summary>The end of a state of the hit layer: its cursor group and its hit group.</summary>
    public void CloseState(MarkupBuffer output)
    {
        if (!_wrapped)
        {
            Raw(output, "</g>"u8);
        }

        _wrapped = false;
        Raw(HitsMarkup, "</g>"u8);
        _ends.Add(HitsMarkup.Length);
    }

    /// <summary>The end of a hit shape that is a state of its own.</summary>
    public void CloseBareHit() => _ends.Add(HitsMarkup.Length);

    /// <summary>The opening of a hit shape that is a state of its own (an item chart's, written straight in the hit layer): its focus and its classes.</summary>
    public void BareHit(int index)
    {
        Raw(HitsMarkup, FocusMarkup.Group(focus, index == tabStop)[2..]);
        HitClass(index, plain: true);
    }

    /// <summary>The tips' layer: the hover layer with its focus attributes.</summary>
    public void OpenTips(MarkupBuffer output)
    {
        Raw(output, focus == FocusMarkup.Mode.Click ? "<g class=\"pictor-hover pictor-click"u8 : "<g class=\"pictor-hover"u8);
        if (follow is null)
        {
            Raw(output, "\""u8);
        }
        else
        {
            follow.Layer(output);
        }

        FocusMarkup.Layer(output, focus, followButtons);
        Raw(output, ">"u8);
        _written = 0;
    }

    /// <summary>The end of the tips' layer: the hit groups no tip followed; the rented buffer goes back to the pool.</summary>
    public void CloseTips(MarkupBuffer output)
    {
        while (_written < _ends.Count)
        {
            HitAt(output, _written++);
        }

        _ends.Clear();
        Dispose();
    }

    /// <summary>A state's tip group, after the state's hit group.</summary>
    public void Tip(MarkupBuffer output, int index)
    {
        while (_written <= index && _written < _ends.Count)
        {
            HitAt(output, _written++);
        }

        Raw(output, "<g class=\"pictor-tk"u8);
        Number(output, index);
        Value(output, index);
        Raw(output, "\">"u8);
    }

    /// <summary>Returns the rented buffer to the pool.</summary>
    public void Dispose()
    {
        _markup?.Flush();
        _pooled.Dispose();
    }

    private void HitAt(MarkupBuffer output, int index)
    {
        HitsMarkup.Flush();
        output.Write(_pooled.WrittenSpan[(index == 0 ? 0 : _ends[index - 1]).._ends[index]]);
    }

    private void HitClass(int index, bool plain)
    {
        Raw(HitsMarkup, focus == FocusMarkup.Mode.Click ? " class=\"pictor-ck"u8 : " class=\"pictor-hk"u8);
        Number(HitsMarkup, index);
        Value(HitsMarkup, index);
        Raw(HitsMarkup, plain ? " pictor-hit\""u8 : "\""u8);
    }

    /// <summary>A state's category value class, when the chart syncs by value.</summary>
    private void Value(MarkupBuffer output, int index)
    {
        if (values is { } classes && index < classes.Length)
        {
            Raw(output, " "u8);
            Utf8Markup.Text(output, classes[index]!);
        }
    }
}
