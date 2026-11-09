using Aioros.Pictor.Specs;
using Aioros.Pictor.Text;

namespace Aioros.Pictor.Painting;

/// <summary>One line a tooltip may show, before Recharts filters, dedupes and sorts its payload.</summary>
/// <param name="Name">The series' or item's name.</param>
/// <param name="Key">The data key (what <c>payloadUniqBy</c> and <c>itemSorter="dataKey"</c> read).</param>
/// <param name="Value">The value, NaN when missing.</param>
/// <param name="Unit">The unit written after the value.</param>
/// <param name="Color">The line's color.</param>
/// <param name="Target">The legend toggle the line belongs to, or -1.</param>
/// <param name="Hidden">Whether its series is hidden (Recharts' <c>hide</c>).</param>
/// <param name="None">Whether its series has <c>tooltipType="none"</c>: it counts as content but writes no line.</param>
/// <param name="Formatted">The value as written, when it is not the value through the tooltip's format (a scatter's coordinates).</param>
internal readonly record struct TooltipItem(string Name, string Key, double Value, string? Unit, string Color, int Target = -1, bool Hidden = false, bool None = false, string? Formatted = null);

/// <summary>
/// Recharts' tooltip content and placement, shared by every chart family: the payload filtered as <c>Tooltip</c>
/// filters it (<c>filterNull</c>, <c>includeHidden</c>, <c>payloadUniqBy</c>), sorted by <c>itemSorter</c> and
/// formatted as <c>DefaultTooltipContent</c> writes it, and the box placed by <c>getTooltipTranslateXY</c>.
/// </summary>
internal static class TooltipBox
{
    /// <summary>Filters, dedupes and sorts the items in place; returns how many are kept (the payload).</summary>
    public static int Finish(TooltipSpec spec, Span<TooltipItem> items)
    {
        var count = 0;
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (spec.FilterNull && (double.IsNaN(item.Value) || (item.Hidden && !spec.IncludeHidden)))
            {
                continue;
            }

            if (spec.FilterNull && spec.PayloadUniqBy && Seen(items[..count], item.Key))
            {
                continue;
            }

            items[count++] = item;
        }

        if (spec.ItemSorter is not null)
        {
            Sort(items[..count], spec.ItemSorter);
        }

        return count;
    }

    /// <summary>The lines written for a payload: one per item except those of <c>tooltipType="none"</c>, the values formatted.</summary>
    public static int Entries(TooltipSpec spec, ReadOnlySpan<TooltipItem> items, Span<TooltipEntry> entries)
    {
        var count = 0;
        foreach (var item in items)
        {
            if (!item.None)
            {
                entries[count++] = new TooltipEntry(item.Name, ((item.Formatted is null ? Text.FormatKeys.ValueFunction(spec.Formatter, spec.Texts, spec.Converter)?.Invoke(item.Value, item.Name) : null) ?? item.Formatted ?? SeriesPainter.Value(item.Value, spec.Format)) + item.Unit, item.Color, item.Target);
            }
        }

        return count;
    }

    /// <summary>The label as <c>labelFormatter</c> writes it.</summary>
    public static string Label(TooltipSpec spec, string label) =>
        label.Length > 0 ? Text.FormatKeys.LabelFunction(spec.LabelFormatter, spec.LabelTexts, spec.LabelConverter)?.Invoke(label) ?? Text.Formats.Apply(spec.LabelFormat, label, label) : label;

    /// <summary>The size of Recharts' default tooltip box for these texts, with its content style's padding and border; an empty label takes no line.</summary>
    public static (double Width, double Height) Size(TooltipSpec spec, string label, ReadOnlySpan<TooltipEntry> entries, double fontSize)
    {
        var line = InterMetrics.LineHeight(fontSize);
        var separator = InterMetrics.Width(spec.Separator, fontSize);
        var width = InterMetrics.Width(label, fontSize);
        foreach (var entry in entries)
        {
            width = Math.Max(width, InterMetrics.Width(entry.Name, fontSize) + separator + InterMetrics.Width(entry.Value, fontSize));
        }

        var edges = (2 * Css.Length(spec.ContentStyle, "padding", 10)) + (2 * Css.BorderWidth(spec.ContentStyle, 1));
        return (width + edges, (label.Length > 0 ? line : 0) + (entries.Length * (line + 8)) + edges);
    }

    /// <summary>Recharts' <c>getTooltipTranslateXY</c> along one direction, in a view box from <paramref name="start"/> over <paramref name="length"/>.</summary>
    public static double Translate(TooltipSpec spec, bool alongX, double coordinate, double size, double start, double length)
    {
        if ((alongX ? spec.PositionX : spec.PositionY) is { } position)
        {
            return position;
        }

        var negative = coordinate - size - spec.Offset;
        var positive = coordinate + spec.Offset;
        var reverse = alongX ? spec.ReverseX : spec.ReverseY;
        if (alongX ? spec.AllowEscapeX : spec.AllowEscapeY)
        {
            return reverse ? negative : positive;
        }

        if (reverse)
        {
            return negative < start ? Math.Max(positive, start) : Math.Max(negative, start);
        }

        return positive + size > start + length ? Math.Max(negative, start) : Math.Max(positive, start);
    }

    /// <summary>
    /// Writes the tooltip of a payload anchored at a point of a view box; no box when the payload is empty or the tooltip
    /// is never active. A box that follows the pointer along a coordinate (<paramref name="follow"/>) does so unless the
    /// tooltip fixes that coordinate (<c>position</c>) or shows on click (Recharts places it at the click).
    /// </summary>
    public static void Write<TCanvas>(ref TCanvas canvas, TooltipSpec spec, double fontSize, double x, double y, string label, ReadOnlySpan<TooltipEntry> entries, int payload, (double Left, double Top, double Width, double Height) viewBox, TipFollow follow = default, Content.ContentValues? content = null, int index = -1)
        where TCanvas : struct, IChartCanvas
    {
        if (payload == 0 || spec.Active == false)
        {
            return;
        }

        label = Label(spec, label);
        if (content is not null)
        {
            var item = ContentProps.Tooltip(content, x, y, index, label, entries);
            var lines = TooltipLines.Of(spec, content.Spec, entries, fontSize, content.Data);
            var (contentWidth, contentHeight) = Content.HtmlContent.Measure(content.Spec.Nodes, item, fontSize, lines);
            var followed = Follows(spec, follow) ? follow with { Size = follow.Axis == TipAxis.X ? contentWidth : contentHeight, Width = contentWidth } : default;
            canvas.Html(Translate(spec, true, x, contentWidth, viewBox.Left, viewBox.Width), Translate(spec, false, y, contentHeight, viewBox.Top, viewBox.Height), content.Spec, item, spec, followed, lines);
            return;
        }

        var (width, height) = Size(spec, label, entries, fontSize);
        canvas.Tooltip(Translate(spec, true, x, width, viewBox.Left, viewBox.Width), Translate(spec, false, y, height, viewBox.Top, viewBox.Height), label, entries, spec, Follows(spec, follow) ? follow with { Size = follow.Axis == TipAxis.X ? width : height, Width = width } : default);
    }

    /// <summary>Whether the box follows the pointer: along a coordinate the tooltip leaves free, and not shown on click.</summary>
    public static bool Follows(TooltipSpec spec, TipFollow follow) => !spec.ClickTrigger && follow.Axis switch
    {
        TipAxis.X => spec.PositionX is null,
        TipAxis.Y => spec.PositionY is null,
        TipAxis.Radius or TipAxis.Angle => spec is { PositionX: null, PositionY: null, ReverseX: false, ReverseY: false, AllowEscapeX: false, AllowEscapeY: false },
        _ => false,
    };

    private static bool Seen(ReadOnlySpan<TooltipItem> items, string key)
    {
        foreach (var item in items)
        {
            if (item.Key == key)
            {
                return true;
            }
        }

        return false;
    }

    private static void Sort(Span<TooltipItem> items, string sorter)
    {
        for (var i = 1; i < items.Length; i++)
        {
            var item = items[i];
            var j = i - 1;
            while (j >= 0 && Compare(items[j], item, sorter) > 0)
            {
                items[j + 1] = items[j];
                j--;
            }

            items[j + 1] = item;
        }
    }

    /// <summary>lodash's <c>sortBy</c> order: ascending, missing values last.</summary>
    private static int Compare(in TooltipItem a, in TooltipItem b, string sorter)
    {
        if (sorter == "value")
        {
            return double.IsNaN(a.Value) ? (double.IsNaN(b.Value) ? 0 : 1) : double.IsNaN(b.Value) ? -1 : a.Value.CompareTo(b.Value);
        }

        return string.CompareOrdinal(sorter == "name" ? a.Name : a.Key, sorter == "name" ? b.Name : b.Key);
    }
}
