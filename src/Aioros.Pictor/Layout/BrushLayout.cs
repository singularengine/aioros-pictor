using Aioros.Pictor.Data;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>
/// Recharts' <c>Brush</c> laid out: its strip under the x-axis, its point scale over every item (from the strip's left
/// to its right less a traveller) and the window it selects, with the travellers' positions.
/// </summary>
internal sealed class BrushLayout
{
    public required BrushSpec Spec { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    /// <summary>How many items the brush spans (all of the chart's, not the window's).</summary>
    public required int Count { get; init; }

    public required int StartIndex { get; init; }

    public required int EndIndex { get; init; }

    /// <summary>The travellers' accessible name: the brush's <c>ariaLabel</c>, or Recharts' <c>Min value: …, Max value: …</c> from the items' <c>name</c>.</summary>
    public string AriaLabel { get; init; } = string.Empty;

    public double Height => Spec.Height;

    /// <summary>Where the start traveller is drawn: its index's position, or where a drag holds it.</summary>
    public double StartX { get; set; }

    /// <summary>Where the end traveller is drawn.</summary>
    public double EndX { get; set; }

    /// <summary>The texts at the window's ends, shown while the brush moves (Recharts' <c>isTravellerMoving</c>); none at rest.</summary>
    public (string Start, string End)? Texts { get; set; }

    /// <summary>The panorama laid out over every item inside the strip, if the brush has one.</summary>
    public ChartLayout? Panorama { get; init; }

    /// <summary>The text at an item, as Recharts' <c>getTextOfTick</c> gives it: the data key's value (the index when it has none) through the tick format.</summary>
    public string TextAt(ChartData whole, int index)
    {
        var text = (Spec.DataKey is { } key ? whole.Texts(key)[index] : null) ?? index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Text.Formats.Apply(Spec.TickFormat, text, text);
    }

    /// <summary>The window an index pair selects: clamped to the data, from the smaller index to the larger.</summary>
    public static (int Start, int End) Window(BrushSpec spec, int count)
    {
        var last = Math.Max(count - 1, 0);
        var start = Math.Clamp(spec.StartIndex ?? 0, 0, last);
        var end = Math.Clamp(spec.EndIndex ?? last, 0, last);
        return (Math.Min(start, end), Math.Max(start, end));
    }

    /// <summary>d3's point scale of the items over the strip less a traveller (no padding, centered when there is one item).</summary>
    public double Position(int index)
    {
        var span = Width - Spec.TravellerWidth;
        return Count <= 1 ? X + (span / 2) : X + (index * span / (Count - 1));
    }

    /// <summary>Recharts' <c>getIndex</c>: the indexes nearest at or before two positions, snapped down to the gap except at the last item.</summary>
    public (int Start, int End) Indexes(double startX, double endX)
    {
        var last = Count - 1;
        var min = IndexAt(Math.Min(startX, endX));
        var max = IndexAt(Math.Max(startX, endX));
        var gap = Math.Max(Spec.Gap, 1);
        return (min - (min % gap), max == last ? last : max - (max % gap));
    }

    private int IndexAt(double x)
    {
        var start = 0;
        var end = Count - 1;
        while (end - start > 1)
        {
            var middle = (start + end) / 2;
            if (Position(middle) > x)
            {
                end = middle;
            }
            else
            {
                start = middle;
            }
        }

        return x >= Position(end) ? end : start;
    }
}
