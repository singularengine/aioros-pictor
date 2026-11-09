using System.Globalization;
using Aioros.Pictor.Data;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Ticks;

namespace Aioros.Pictor.Layout;

/// <summary>
/// A cartesian chart laid out as Recharts lays it out after mount (<c>generateCategoricalChart</c>): the plot's
/// offset, every axis with its scale and visible ticks, every series' geometry, the references and the legend.
/// </summary>
internal sealed class ChartLayout : IChartPlot
{
    private Dictionary<SeriesSpec, ChartData>? _seriesData;
    private List<ChartData>? _chartDataList;

    private ChartLayout(ChartSpec spec, ChartData data)
    {
        Spec = spec;
        WholeData = data;
        Data = data;
        if (spec.Brush is { } brush && data.Count > 0 && !spec.Series.Any(series => series.Data is not null))
        {
            var (start, end) = BrushLayout.Window(brush, data.Count);
            Data = data.Window(start, end);
            WindowStart = start;
        }
    }

    private ChartLayout(ChartLayout source)
    {
        Spec = source.Spec;
        WholeData = source.WholeData;
        Data = source.Data;
        WindowStart = source.WindowStart;
        Brush = source.Brush;
        Left = source.Left;
        Top = source.Top;
        PlotWidth = source.PlotWidth;
        PlotHeight = source.PlotHeight;
        Legend = source.Legend;
        HoverCount = source.HoverCount;
        XAxes.AddRange(source.XAxes);
        YAxes.AddRange(source.YAxes);
        ZAxes.AddRange(source.ZAxes);
        _seriesData = source._seriesData;
        _chartDataList = source._chartDataList;
    }

    public ChartSpec Spec { get; }

    /// <summary>The items the chart shows: all of them, or a brush's window.</summary>
    public ChartData Data { get; }

    /// <summary>Every item, which a brush spans.</summary>
    public ChartData WholeData { get; }

    /// <summary>The index in <see cref="WholeData"/> of the first item shown (a brush's start), 0 without a window.</summary>
    public int WindowStart { get; }

    /// <summary>The brush, if any.</summary>
    public BrushLayout? Brush { get; private set; }

    public double Width => Spec.Width;

    public double Height => Spec.Height;

    public double Left { get; private set; }

    public double Top { get; private set; }

    public double PlotWidth { get; private set; }

    public double PlotHeight { get; private set; }

    public List<AxisLayout> XAxes { get; } = [];

    public List<AxisLayout> YAxes { get; } = [];

    public List<SeriesLayout> Series { get; } = [];

    public LegendLayout? Legend { get; private set; }

    /// <summary>The category axis the tooltip follows, if any.</summary>
    public AxisLayout? Categories => Spec.Kind == ChartKind.Scatter ? null : (Vertical ? YAxes : XAxes).Find(axis => axis.IsCategory);

    /// <summary>Whether categories run down the y-axis (Recharts' <c>layout="vertical"</c>).</summary>
    public bool Vertical => Spec.Layout == ChartLayoutKind.Vertical;

    /// <summary>Whether the tooltip follows items (a scatter chart's points) rather than categories.</summary>
    public bool ItemTooltip => Spec.Kind == ChartKind.Scatter;

    /// <summary>Whether the tooltip follows bars (a bar chart's <c>shared={false}</c>, Recharts' <c>item</c> tooltip; line, area and composed charts keep the axis tooltip).</summary>
    public bool BarTooltip => Spec.Kind == ChartKind.Bar && Spec.Tooltip?.Shared == false;

    /// <summary>The hover state shown before any pointer event (Recharts' <c>defaultIndex</c>), or -1.</summary>
    public int DefaultTip => Spec.Tooltip?.DefaultIndex is { } index && index >= 0 && index < HoverCount && !BarTooltip ? index : -1;

    public List<ZAxisLayout> ZAxes { get; } = [];

    public int HoverCount { get; private set; }

    private int CountHovers()
    {
        if (Spec.Tooltip is null)
        {
            return 0;
        }

        if (BarTooltip)
        {
            return BarTips.Count(this);
        }

        if (!ItemTooltip)
        {
            return Categories is { } axis && Data.Count > 0 ? axis.Categories.Length : 0;
        }

        var count = 0;
        foreach (var series in Series)
        {
            count += series.PointCount;
        }

        return count;
    }

    /// <summary>The chart's items as the one list an axis displays when none of its series carries its own.</summary>
    public List<ChartData> ChartDataList => _chartDataList ??= [Data];

    /// <summary>The items a series reads: its own data, or the chart's.</summary>
    public ChartData SeriesData(SeriesSpec series) => series.Data is not null && _seriesData is not null && _seriesData.TryGetValue(series, out var data) ? data : Data;

    /// <summary>The same chart drawing other series (a frame of an update animation), its axes, legend and data shared.</summary>
    public ChartLayout WithSeries(IEnumerable<SeriesLayout> series)
    {
        var copy = new ChartLayout(this);
        copy.Series.AddRange(series);
        return copy;
    }

    public static ChartLayout Compute(ChartSpec spec, ChartData data)
    {
        var layout = new ChartLayout(spec, data);
        layout.Run();
        return layout;
    }

    public AxisLayout XAxis(string id) => Axis(XAxes, id);

    public AxisLayout YAxis(string id) => Axis(YAxes, id);

    private static AxisLayout Axis(List<AxisLayout> axes, string id)
    {
        foreach (var axis in axes)
        {
            if (axis.Spec.Id == id)
            {
                return axis;
            }
        }

        return axes[0];
    }

    public ZAxisLayout? ZAxis(string id) => ZAxes.Find(axis => axis.Spec.Id == id);

    /// <summary>The axis a series' values map through: the y-axis, or the x-axis in a vertical chart.</summary>
    public AxisLayout ValueAxis(SeriesSpec series) => Vertical ? XAxis(series.XAxisId) : YAxis(series.YAxisId);

    /// <summary>The axis a series' categories map through: the x-axis, or the y-axis in a vertical chart.</summary>
    public AxisLayout CategoryAxis(SeriesSpec series) => Vertical ? YAxis(series.YAxisId) : XAxis(series.XAxisId);

    private void Run()
    {
        var hasBar = Spec.Series.Any(series => series.Kind == SeriesKind.Bar);
        var pointScale = Spec.Kind is ChartKind.Line or ChartKind.Area || (Spec.Kind == ChartKind.Composed && !hasBar);
        foreach (var series in Spec.Series)
        {
            if (series.Data is { } table)
            {
                (_seriesData ??= new Dictionary<SeriesSpec, ChartData>(ReferenceEqualityComparer.Instance))[series] = ChartData.From(table);
            }
        }

        var stacks = new Dictionary<(bool IsX, string Id), List<StackGroup>>();
        Axes(new AxisDomains(this, stacks), pointScale);
        Legend = Spec.Legend is { } legend ? LegendLayout.Compute(Spec, legend, LegendLayout.SeriesEntries(Spec, legend)) : null;
        Offset();
        Place();
        foreach (var axis in XAxes)
        {
            Ticks(axis);
        }

        foreach (var axis in YAxes)
        {
            Ticks(axis);
        }

        GridValues();
        SeriesGeometry.Compute(this, stacks);
        HoverCount = CountHovers();
    }

    /// <summary>
    /// The axis' ticks and the ones it shows. Recharts' axis measures its labels at its ticks' own font size, its grid at
    /// the chart's, so a tick font size of its own can show a grid line where the axis drops a label.
    /// </summary>
    private void Ticks(AxisLayout axis)
    {
        axis.Tick(TickText);
        axis.Visible = TickPlacement.Visible(axis, Width, Height, axis.Spec.TickFontSize ?? Spec.FontSize);
        axis.GridVisible = axis.Spec.TickFontSize is null ? axis.Visible : TickPlacement.Visible(axis, Width, Height, Spec.FontSize);
    }

    /// <summary>Recharts' grid over its own values: the first axis' ticks replaced by them, kept by the axis' rules at the chart's font size, the lines at the ticks only.</summary>
    private void GridValues()
    {
        if (Spec.Grid is not { } grid)
        {
            return;
        }

        if (!grid.HorizontalValues.IsEmpty && YAxes.Count > 0)
        {
            YAxes[0].GridValues = TickPlacement.Visible(YAxes[0], YAxes[0].Placed(grid.HorizontalValues, TickText), Width, Height, Spec.FontSize);
        }

        if (!grid.VerticalValues.IsEmpty && XAxes.Count > 0)
        {
            XAxes[0].GridValues = TickPlacement.Visible(XAxes[0], XAxes[0].Placed(grid.VerticalValues, TickText), Width, Height, Spec.FontSize);
        }
    }

    private void Axes(AxisDomains domains, bool pointScale)
    {
        foreach (var axis in Spec.XAxes.IsEmpty ? [new AxisSpec { Hide = true, Type = Vertical ? AxisType.Number : AxisType.Category, Orientation = AxisOrientation.Bottom }] : Spec.XAxes)
        {
            XAxes.Add(domains.Axis(axis, isX: true, pointScale));
        }

        foreach (var axis in Spec.YAxes.IsEmpty ? [new AxisSpec { Hide = true, Type = Vertical ? AxisType.Category : AxisType.Number, Orientation = AxisOrientation.Left }] : Spec.YAxes)
        {
            YAxes.Add(domains.Axis(axis, isX: false, pointScale));
        }

        foreach (var axis in Spec.ZAxes)
        {
            ZAxes.Add(domains.ZAxis(axis));
        }
    }

    private void Offset()
    {
        var margin = Spec.Margin;
        var left = margin.Left;
        var right = margin.Right;
        var top = margin.Top;
        var bottom = margin.Bottom;
        AxesSpace(ref left, ref right, ref top, ref bottom);
        var brushBottom = bottom;
        if (Spec.Brush is { } brush)
        {
            bottom += brush.Height;
        }

        if (Legend is { } legend)
        {
            legend.Apply(ref left, ref right, ref top, ref bottom);
        }

        Left = left;
        Top = top;
        PlotWidth = Math.Max(Width - left - right, 0);
        PlotHeight = Math.Max(Height - top - bottom, 0);
        if (Spec.Brush is { } spec && WholeData.Count > 0)
        {
            var (start, end) = BrushLayout.Window(spec, WholeData.Count);
            var width = spec.Width ?? PlotWidth;
            var layout = new BrushLayout
            {
                Spec = spec,
                X = spec.X ?? Left,
                Y = spec.Y ?? Top + PlotHeight + brushBottom - margin.Bottom,
                Width = width,
                Count = WholeData.Count,
                StartIndex = start,
                EndIndex = end,
                AriaLabel = spec.AriaLabel ?? $"Min value: {NameAt(start)}, Max value: {NameAt(end)}",
                Panorama = spec.Panorama is { } panorama ? Compute(panorama with { Width = width, Height = spec.Height, Margin = spec.Padding, Legend = null, Tooltip = null, Brush = null }, WholeData) : null,
            };
            layout.StartX = layout.Position(start);
            layout.EndX = layout.Position(end);
            layout.Texts = spec.AlwaysShowText ? (layout.TextAt(WholeData, start), layout.TextAt(WholeData, end)) : null;
            Brush = layout;
        }
    }

    /// <summary>An item's <c>name</c> as JavaScript writes it into a string (<c>undefined</c> when it has none).</summary>
    private string NameAt(int index) => WholeData.Texts("name")[index] ?? "undefined";

    private void AxesSpace(ref double left, ref double right, ref double top, ref double bottom)
    {
        foreach (var axis in YAxes)
        {
            if (axis.Spec.Mirror || axis.Spec.Hide)
            {
                continue;
            }

            if (axis.Spec.Orientation == AxisOrientation.Right)
            {
                right += Size(axis);
            }
            else
            {
                left += Size(axis);
            }
        }

        foreach (var axis in XAxes)
        {
            if (axis.Spec.Mirror || axis.Spec.Hide)
            {
                continue;
            }

            if (axis.Spec.Orientation == AxisOrientation.Top)
            {
                top += Size(axis);
            }
            else
            {
                bottom += Size(axis);
            }
        }
    }

    private static double Size(AxisLayout axis) => axis.Spec.Size > 0 ? axis.Spec.Size : axis.IsX ? 30 : 60;

    private void Place()
    {
        var right = Left + PlotWidth;
        var bottom = Top + PlotHeight;
        Span<double> steps = stackalloc double[8];
        steps[Slot(AxisOrientation.Left, false)] = steps[Slot(AxisOrientation.Left, true)] = Left;
        steps[Slot(AxisOrientation.Right, false)] = steps[Slot(AxisOrientation.Right, true)] = right;
        steps[Slot(AxisOrientation.Top, false)] = steps[Slot(AxisOrientation.Top, true)] = Top;
        steps[Slot(AxisOrientation.Bottom, false)] = steps[Slot(AxisOrientation.Bottom, true)] = bottom;
        foreach (var axis in XAxes)
        {
            var needSpace = (axis.Spec.Orientation == AxisOrientation.Top) != axis.Spec.Mirror;
            var step = Step(steps, axis, needSpace, Size(axis));
            axis.Width = PlotWidth;
            axis.Height = Size(axis);
            axis.X = Left;
            axis.Y = step;
            var gap = GapPadding(axis, PlotWidth);
            axis.SetRange(Left + axis.Spec.PaddingStart + gap, Left + PlotWidth - axis.Spec.PaddingEnd - gap);
        }

        foreach (var axis in YAxes)
        {
            var needSpace = (axis.Spec.Orientation == AxisOrientation.Left) != axis.Spec.Mirror;
            var step = Step(steps, axis, needSpace, Size(axis));
            axis.Width = Size(axis);
            axis.Height = PlotHeight;
            axis.X = step;
            axis.Y = Top;
            if (Vertical)
            {
                var gap = GapPadding(axis, PlotHeight);
                axis.SetRange(Top + axis.Spec.PaddingEnd + gap, Top + PlotHeight - axis.Spec.PaddingStart - gap);
            }
            else
            {
                axis.SetRange(Top + PlotHeight - axis.Spec.PaddingStart, Top + axis.Spec.PaddingEnd);
            }
        }
    }

    /// <summary>
    /// Recharts' <c>calculatedPadding</c> of a numeric axis that runs along the categories with <c>padding="gap"</c>
    /// (half the smallest distance between two values) or <c>"no-gap"</c> (that half less the bar category gap); 0 otherwise.
    /// </summary>
    private double GapPadding(AxisLayout axis, double range)
    {
        if (axis.Spec.PaddingKind == AxisPaddingKind.Fixed || axis.Spec.Type != AxisType.Number || axis.IsX == Vertical || !double.IsFinite(axis.Gap))
        {
            return 0;
        }

        var share = axis.Gap / axis.Spread;
        var half = share * range / 2;
        if (axis.Spec.PaddingKind == AxisPaddingKind.Gap)
        {
            return Finite(half);
        }

        var gap = Polar.Percent(Spec.BarCategoryGap, share * range);
        return Finite(half - gap - ((half - gap) / range * gap));
    }

    /// <summary>Recharts' <c>calculatedPadding || 0</c>: a padding that is not a number (one value, so no distance; an empty range) is none.</summary>
    private static double Finite(double padding) => double.IsFinite(padding) ? padding : 0;

    /// <summary>The item of a series shown at a category: by index, or the item whose category is the tick's when the axis drops duplicated categories (Recharts' <c>findEntryInArray</c>); -1 when none is.</summary>
    public int ItemAt(SeriesLayout series, int category)
    {
        var axis = CategoryAxis(series.Spec);
        if (axis.Spec.AllowDuplicatedCategory || axis.Spec.DataKey is not { } key || category >= axis.Categories.Length)
        {
            return category;
        }

        return TextIndex.Find(series.FirstItems(key), axis.Categories[category]);
    }

    private static int Slot(AxisOrientation orientation, bool mirror) => ((int)orientation * 2) + (mirror ? 1 : 0);

    private static double Step(Span<double> steps, AxisLayout axis, bool needSpace, double size)
    {
        var key = Slot(axis.Spec.Orientation, axis.Spec.Mirror);
        var position = steps[key] - (needSpace ? size : 0);
        if (!axis.Spec.Hide)
        {
            steps[key] += (needSpace ? -1 : 1) * size;
        }

        return position;
    }

    private static string TickText(AxisLayout axis, double value, int index)
    {
        var text = axis.IsCategory ? axis.Categories[index] ?? string.Empty
            : axis.DateTicks ? DateText.JavaScript(value, axis.Zone!)
            : Graphs.Formatting.JsNumber.ToShortestString(value);
        text = Text.FormatKeys.TickFunction(axis.Spec.TickFormatter, axis.Spec.TickTexts, axis.Spec.TickConverter) is { } formatter ? formatter(Raw(axis, text, value), index) ?? Formatted(axis, text, value) : Formatted(axis, text, value);
        return axis.Spec.Unit is { } unit ? text + unit : text;
    }

    /// <summary>What a tick's function receives: a category's text, a date axis' instant, or the number (a negative zero as zero, as JavaScript writes it).</summary>
    private static object Raw(AxisLayout axis, string text, double value) =>
        axis.IsCategory ? text : axis.Zone is { } zone ? DateText.Instant(value, zone) : value + 0d;

    private static string Formatted(AxisLayout axis, string text, double value) => axis.Spec.TickFormat is not { } format ? text
        : axis.IsCategory ? Text.Formats.Apply(format, text, text)
        : axis.Zone is { } zone ? Text.Formats.Apply(format, DateText.Instant(value, zone), text)
        : Text.Formats.Apply(format, value, text);
}
