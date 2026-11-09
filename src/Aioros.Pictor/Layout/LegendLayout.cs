using Aioros.Pictor.Specs;
using Aioros.Pictor.Text;

namespace Aioros.Pictor.Layout;

/// <summary>One legend entry: what it says, its color and icon, and where it sits.</summary>
internal readonly record struct LegendItem(string Name, string Color, LegendIcon Icon, double X, double Y)
{
    /// <summary>What the item toggles: a series' index, or an item's for a pie; -1 for nothing.</summary>
    public int Target { get; init; } = -1;

    /// <summary>The item's width, its icon, text and gap.</summary>
    public double Width { get; init; }

    /// <summary>The item's index in the payload (Recharts' <c>legend-item-N</c> class counts the entries it does not draw).</summary>
    public int Index { get; init; }

    /// <summary>Whether the item is inactive (Recharts' <c>inactive</c> class, its icon and text in the inactive color).</summary>
    public bool Inactive { get; init; }

    /// <summary>The dash pattern of a <c>plainline</c> icon (its series').</summary>
    public string? Dash { get; init; }

    /// <summary>Whether a legend's toggle conceals and shows the item's target: one that is drawn (a hidden series' item, Recharts' inactive one, has nothing to show).</summary>
    public bool Toggles => Target >= 0 && !Inactive;
}

/// <summary>What a legend lists for one series or item: its name, its color and its icon.</summary>
internal readonly record struct LegendEntry(string Name, string Color, LegendIcon Icon, int Target = -1, bool Inactive = false, string? Dash = null);

/// <summary>
/// Recharts' default legend: a box below (or above) the plot as wide as the chart less its margins, its items in
/// centered rows, measured with Inter's metrics where Recharts measures the rendered box; the plot shrinks by its
/// height rounded to whole pixels, as <c>offsetHeight</c> gives it.
/// </summary>
internal sealed class LegendLayout
{
    private const double IconGap = 4;
    private const double ItemGap = 10;
    private const double XHeight = 1536;

    public required LegendSpec Spec { get; init; }

    /// <summary>An inactive item's color: the legend's own, or the theme's.</summary>
    public required string Inactive { get; init; }

    public required List<LegendItem> Items { get; init; }

    public double Left { get; init; }

    public double Top { get; init; }

    public double Width { get; init; }

    public double Height { get; init; }

    public double RowHeight { get; init; }

    /// <summary>The distance from the chart's right edge, Recharts' wrapper <c>right</c> for a legend aligned right.</summary>
    public double RightEdge { get; init; }

    /// <summary>The distance from the chart's top or bottom edge, Recharts' wrapper <c>top</c> or <c>bottom</c>.</summary>
    public double Edge { get; init; }

    public static LegendLayout Compute(ChartSpec chart, LegendSpec spec, IReadOnlyList<LegendEntry> legend)
    {
        var entries = Entries(chart, spec, spec.Payload.IsDefaultOrEmpty ? legend : Payload(spec));
        if (spec.Frame is { } frame)
        {
            return Framed(chart, spec, entries, frame);
        }

        if (spec.Content is { } content)
        {
            return Custom(chart, spec, entries, content);
        }

        if (spec.Vertical)
        {
            return Column(chart, spec, entries);
        }

        var margin = chart.Margin;
        var width = spec.Width ?? chart.Width - margin.Left - margin.Right;
        var rowHeight = spec.LineHeight ?? Painting.Css.Length(Painting.Css.Value(spec.WrapperStyle, "line-height")) ?? LineHeight(chart.FontSize, spec.IconSize);
        var rows = Rows(entries, width);
        var height = spec.Height ?? rows.Count * rowHeight;
        var top = spec.VerticalAlign switch
        {
            "top" => margin.Top,
            "middle" => (chart.Height - Math.Round(height)) / 2,
            _ => chart.Height - margin.Bottom - height,
        };
        var left = spec.Align == "right" ? chart.Width - margin.Right - width : margin.Left;
        (left, top) = Placed(chart, spec, left, top, width, height);
        var items = new List<LegendItem>(entries.Count);
        for (var r = 0; r < rows.Count; r++)
        {
            var x = left + spec.Align switch
            {
                "left" => 0,
                "right" => width - rows[r].Width,
                _ => (width - rows[r].Width) / 2,
            };
            for (var i = rows[r].Start; i < rows[r].End; i++)
            {
                items.Add(entries[i] with { X = x, Y = top + (r * rowHeight) });
                x += entries[i].Width;
            }
        }

        return new LegendLayout
        {
            Spec = spec,
            Inactive = spec.InactiveColor ?? chart.Colors.LegendInactive,
            Items = items,
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            RowHeight = rowHeight,
            RightEdge = margin.Right,
            Edge = spec.VerticalAlign switch { "top" => margin.Top, "middle" => top, _ => margin.Bottom },
        };
    }

    /// <summary>
    /// A legend with custom content: its entries' content as a list the browser lays out (a <c>ul</c> with its default
    /// margins and indent, one <c>li</c> per entry below the other), as Recharts measures a custom legend's box.
    /// </summary>
    private static LegendLayout Custom(ChartSpec chart, LegendSpec spec, List<LegendItem> entries, ContentSpec content)
    {
        var margin = chart.Margin;
        var values = new Content.ContentValues(Content.ContentSlots.LegendEntry, content, null);
        var heights = entries.ConvertAll(entry => Content.HtmlContent.Measure(content.Nodes, Painting.ContentProps.LegendEntry(values, entry), chart.FontSize).Height);
        var width = spec.Width ?? chart.Width - margin.Left - margin.Right;
        var height = spec.Height ?? (2 * chart.FontSize) + heights.Sum();
        var top = spec.VerticalAlign switch
        {
            "top" => margin.Top,
            "middle" => (chart.Height - Math.Round(height)) / 2,
            _ => chart.Height - margin.Bottom - height,
        };
        var left = spec.Align == "right" ? chart.Width - margin.Right - width : margin.Left;
        (left, top) = Placed(chart, spec, left, top, width, height);
        var items = new List<LegendItem>(entries.Count);
        var y = top + chart.FontSize;
        for (var i = 0; i < entries.Count; i++)
        {
            items.Add(entries[i] with { X = left + CustomIndent, Y = y });
            y += heights[i];
        }

        return new LegendLayout
        {
            Spec = spec,
            Inactive = spec.InactiveColor ?? chart.Colors.LegendInactive,
            Items = items,
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            RowHeight = heights.Count > 0 ? heights[0] : 0,
            RightEdge = margin.Right,
            Edge = spec.VerticalAlign switch { "top" => margin.Top, "middle" => top, _ => margin.Bottom },
        };
    }

    /// <summary>
    /// A legend whose content is a whole template: its height the template's, measured with the entries filling the
    /// width its containers leave of the legend's (Recharts' default list in rows, or the entries' templates in a list),
    /// placed as the default legend.
    /// </summary>
    private static LegendLayout Framed(ChartSpec chart, LegendSpec spec, List<LegendItem> entries, ContentSpec frame)
    {
        var margin = chart.Margin;
        var width = spec.Width ?? chart.Width - margin.Left - margin.Right;
        var values = new Content.ContentValues(Content.ContentSlots.Legend, frame, null);
        var item = Painting.ContentProps.Legend(values, entries.Count, width, spec);
        var height = spec.Height ?? Content.HtmlContent.Measure(frame.Nodes, item, chart.FontSize, new LegendFlow(chart.FontSize, spec, entries), width).Height;
        var top = spec.VerticalAlign switch
        {
            "top" => margin.Top,
            "middle" => (chart.Height - Math.Round(height)) / 2,
            _ => chart.Height - margin.Bottom - height,
        };
        var left = spec.Align == "right" ? chart.Width - margin.Right - width : margin.Left;
        (left, top) = Placed(chart, spec, left, top, width, height);
        return new LegendLayout
        {
            Spec = spec,
            Inactive = spec.InactiveColor ?? chart.Colors.LegendInactive,
            Items = entries,
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            RowHeight = LineHeight(chart.FontSize, spec.IconSize),
            ListMargin = chart.FontSize,
            RightEdge = margin.Right,
            Edge = spec.VerticalAlign switch { "top" => margin.Top, "middle" => top, _ => margin.Bottom },
        };
    }

    /// <summary>The entries placed in a box of a width at a point: Recharts' default list in rows (aligned as the legend), or one entry's template per line after the list's top margin and indent.</summary>
    public static List<LegendItem> Place(LegendLayout legend, double x, double y, double width, Func<LegendItem, (double Width, double Height)> size)
    {
        var placed = new List<LegendItem>(legend.Items.Count);
        if (legend.Spec.Content is not null && LegendRow.Of(legend.Spec.Frame) is { } row)
        {
            var sizes = legend.Items.ConvertAll(entry => size(entry));
            var boxes = row.Lay(sizes, width, legend.Spec.Align).Boxes;
            for (var i = 0; i < boxes.Count; i++)
            {
                placed.Add(legend.Items[i] with { X = x + boxes[i].X + ((boxes[i].Width - sizes[i].Width) / 2), Y = y + boxes[i].Y });
            }

            return placed;
        }

        if (legend.Spec.Content is not null)
        {
            var top = y + legend.ListMargin;
            foreach (var entry in legend.Items)
            {
                placed.Add(entry with { X = x + CustomIndent, Y = top });
                top += size(entry).Height;
            }

            return placed;
        }

        var rows = Rows(legend.Items, width);
        for (var r = 0; r < rows.Count; r++)
        {
            var left = x + legend.Spec.Align switch
            {
                "left" => 0,
                "right" => width - rows[r].Width,
                _ => (width - rows[r].Width) / 2,
            };
            for (var i = rows[r].Start; i < rows[r].End; i++)
            {
                placed.Add(legend.Items[i] with { X = left, Y = y + (r * legend.RowHeight) });
                left += legend.Items[i].Width;
            }
        }

        return placed;
    }

    /// <summary>The top margin of a framed legend's list of entries' templates (a <c>ul</c>'s default 1em).</summary>
    public double ListMargin { get; init; }

    /// <summary>The height of a framed legend's entries at a width: Recharts' default list's rows, or a list of the entries' templates.</summary>
    internal sealed class LegendFlow(double fontSize, LegendSpec spec, List<LegendItem> entries, Action<Svg.MarkupBuffer>? write = null) : Content.IHtmlItems
    {
        public void Write(Svg.MarkupBuffer output) => write?.Invoke(output);

        public double Width => 0;

        public double Height(double width)
        {
            if (spec.Content is not { } content)
            {
                return Rows(entries, width).Count * LineHeight(fontSize, spec.IconSize);
            }

            var values = new Content.ContentValues(Content.ContentSlots.LegendEntry, content, null);
            if (LegendRow.Of(spec.Frame) is { } row)
            {
                return row.Lay(entries.ConvertAll(entry => Content.HtmlContent.Measure(content.Nodes, Painting.ContentProps.LegendEntry(values, entry), fontSize)), width, spec.Align).Height;
            }

            return (2 * fontSize) + entries.Sum(entry => Content.HtmlContent.Measure(content.Nodes, Painting.ContentProps.LegendEntry(values, entry), fontSize).Height);
        }
    }

    /// <summary>The indent of a custom legend's list (a <c>ul</c>'s default 40 px).</summary>
    public const double CustomIndent = 40;

    /// <summary>
    /// Recharts' vertical legend (<c>layout="vertical"</c>): one item per line, as wide as its widest item (its
    /// <c>offsetWidth</c>), at the chart's left, right or center and its top, middle or bottom.
    /// </summary>
    private static LegendLayout Column(ChartSpec chart, LegendSpec spec, List<LegendItem> entries)
    {
        var margin = chart.Margin;
        var rowHeight = LineHeight(chart.FontSize, spec.IconSize);
        var widest = 0.0;
        foreach (var entry in entries)
        {
            widest = Math.Max(widest, entry.Width);
        }

        var width = spec.Width ?? Math.Round(widest);
        var height = spec.Height ?? entries.Count * rowHeight;
        var left = spec.Align switch
        {
            "right" => chart.Width - margin.Right - width,
            "center" => (chart.Width - width) / 2,
            _ => margin.Left,
        };
        var top = spec.VerticalAlign switch
        {
            "middle" => (chart.Height - Math.Round(height)) / 2,
            "top" => margin.Top,
            _ => chart.Height - margin.Bottom - height,
        };
        (left, top) = Placed(chart, spec, left, top, width, height);
        var items = new List<LegendItem>(entries.Count);
        for (var i = 0; i < entries.Count; i++)
        {
            items.Add(entries[i] with { X = left, Y = top + (i * rowHeight) });
        }

        return new LegendLayout { Spec = spec, Inactive = spec.InactiveColor ?? chart.Colors.LegendInactive, Items = items, Left = left, Top = top, Width = width, Height = height, RowHeight = rowHeight, RightEdge = margin.Right, Edge = spec.VerticalAlign == "bottom" ? margin.Bottom : top };
    }

    /// <summary>The legend's corner where its <c>wrapperStyle</c> sets <c>left</c>, <c>right</c>, <c>top</c> or <c>bottom</c>, as Recharts drops its default position for them.</summary>
    private static (double Left, double Top) Placed(ChartSpec chart, LegendSpec spec, double left, double top, double width, double height)
    {
        if (spec.WrapperStyle is not { } style)
        {
            return (left, top);
        }

        var x = Painting.Css.Length(Painting.Css.Value(style, "left")) is { } l ? l : Painting.Css.Length(Painting.Css.Value(style, "right")) is { } r ? chart.Width - r - width : left;
        var y = Painting.Css.Length(Painting.Css.Value(style, "top")) is { } t ? t : Painting.Css.Length(Painting.Css.Value(style, "bottom")) is { } b ? chart.Height - b - height : top;
        return (x, y);
    }

    /// <summary>
    /// The height of a legend line in Chromium: the font's ascent and descent rounded to whole pixels, or more where the
    /// icon, centered on the baseline plus half the x-height (<c>vertical-align: middle</c>), reaches further, in layout
    /// units of 1/64 px.
    /// </summary>
    public static double LineHeight(double fontSize, double iconSize)
    {
        var ascent = Math.Round(fontSize * 2728 / 2816);
        var descent = Math.Round(fontSize * 680 / 2816);
        var middle = fontSize * XHeight / 2816 / 2;
        var height = Math.Max(ascent, middle + (iconSize / 2)) + Math.Max(descent, (iconSize / 2) - middle);
        return Math.Ceiling(height * 64) / 64;
    }

    /// <summary>Whether a framed legend lays its entries' templates in a row (<see cref="LegendRow"/>) rather than a list.</summary>
    public static bool InRow(ContentSpec frame) => LegendRow.Of(frame) is not null;

    /// <summary>Where a legend line's icon is centered below the line's top in Chromium: on the baseline (the rounded ascent, or lower where the icon reaches above it) less half the x-height.</summary>
    public static double IconCenter(double fontSize, double iconSize)
    {
        var middle = fontSize * XHeight / 2816 / 2;
        return Math.Max(Math.Round(fontSize * 2728 / 2816), middle + (iconSize / 2)) - middle;
    }

    /// <summary>The entries of a cartesian chart's series: one per series, in order, a hidden one inactive (Recharts' <c>getLegendProps</c>).</summary>
    public static List<LegendEntry> SeriesEntries(ChartSpec chart, LegendSpec spec)
    {
        var entries = new List<LegendEntry>(chart.Series.Length);
        for (var k = 0; k < chart.Series.Length; k++)
        {
            var i = chart.ReverseStackOrder ? chart.Series.Length - 1 - k : k;
            var series = chart.Series[i];
            var icon = spec.IconType ?? series.LegendType ?? series.Kind switch { SeriesKind.Bar => LegendIcon.Rect, SeriesKind.Scatter => LegendIcon.Circle, _ => LegendIcon.Line };
            entries.Add(new LegendEntry(series.Name ?? series.DataKey, SeriesLayout.ColorOf(series, chart.Colors), icon, i, series.Hide, series.StrokeDashArray));
        }

        return entries;
    }

    private static List<LegendEntry> Payload(LegendSpec spec)
    {
        var entries = new List<LegendEntry>(spec.Payload.Length);
        foreach (var entry in spec.Payload)
        {
            entries.Add(new LegendEntry(entry.Value, entry.Color ?? string.Empty, entry.Type ?? LegendIcon.Circle, -1, entry.Inactive));
        }

        return entries;
    }

    /// <summary>The items to draw: entries of a <c>none</c> icon and repeated texts (with <c>payloadUniqBy</c>) left out, each keeping its payload index, its text formatted and measured.</summary>
    private static List<LegendItem> Entries(ChartSpec chart, LegendSpec spec, IReadOnlyList<LegendEntry> legend)
    {
        var entries = new List<LegendItem>(legend.Count);
        var index = 0;
        for (var i = 0; i < legend.Count; i++)
        {
            var entry = legend[i];
            if (spec.PayloadUniqBy && Repeats(legend, i))
            {
                continue;
            }

            var position = index++;
            if (entry.Icon == LegendIcon.None)
            {
                continue;
            }

            var inactive = entry.Inactive || (entry.Target >= 0 && chart.Concealed.Contains(entry.Target));
            var name = Text.Formats.Apply(spec.Format, entry.Name, entry.Name);
            var width = spec.IconSize + IconGap + InterMetrics.Width(name, chart.FontSize) + ItemGap;
            entries.Add(new LegendItem(name, inactive ? spec.InactiveColor ?? chart.Colors.LegendInactive : entry.Color, entry.Icon, 0, 0) { Target = entry.Target, Width = width, Index = position, Inactive = entry.Inactive, Dash = entry.Dash });
        }

        return entries;
    }

    private static bool Repeats(IReadOnlyList<LegendEntry> legend, int index)
    {
        for (var i = 0; i < index; i++)
        {
            if (legend[i].Name == legend[index].Name)
            {
                return true;
            }
        }

        return false;
    }

    private static List<(int Start, int End, double Width)> Rows(List<LegendItem> entries, double width)
    {
        var rows = new List<(int Start, int End, double Width)>();
        var start = 0;
        var used = 0.0;
        for (var i = 0; i < entries.Count; i++)
        {
            if (i > start && used + entries[i].Width > width)
            {
                rows.Add((start, i, used));
                start = i;
                used = 0;
            }

            used += entries[i].Width;
        }

        if (entries.Count > start)
        {
            rows.Add((start, entries.Count, used));
        }

        return rows;
    }

    /// <summary>
    /// Recharts' <c>appendOffsetOfLegend</c>: the plot gives the legend its measured width at a side or its measured
    /// height at the top or bottom, rounded to whole pixels as Recharts 2's <c>offsetHeight</c> gives them, or as they
    /// are when the chart follows Recharts 3, which measures the box with <c>getBoundingClientRect</c>.
    /// </summary>
    public void Apply(ref double left, ref double right, ref double top, ref double bottom, bool recharts3 = false)
    {
        Func<double, double> measure = recharts3 ? static size => size : Math.Round;
        var measured = measure(Height);
        var vertical = Spec.Vertical;
        if ((vertical || Spec.VerticalAlign == "middle") && Spec.Align != "center")
        {
            if (Spec.Align == "left")
            {
                left += measure(Width);
            }
            else
            {
                right += measure(Width);
            }
        }
        else if ((!vertical || Spec.Align == "center") && Spec.VerticalAlign == "top")
        {
            top += measured;
        }
        else if ((!vertical || Spec.Align == "center") && Spec.VerticalAlign == "bottom")
        {
            bottom += measured;
        }
    }
}
