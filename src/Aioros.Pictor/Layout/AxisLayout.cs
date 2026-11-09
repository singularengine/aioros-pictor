using Aioros.Graphs.Scales;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>How an axis maps its values, Recharts' <c>realScaleType</c>.</summary>
internal enum ScaleKind
{
    Point,
    Band,
    Linear,
    Log,
    Pow,
    Sqrt,
    Time,
    Utc,
    Identity,
    Quantile,
    Quantize,
    Threshold,
}

/// <summary>One tick of an axis: where it is, where its label is and what the label says.</summary>
internal readonly record struct AxisTick(double Value, string Text, double Coordinate, double LabelCoordinate);

/// <summary>An axis laid out: its box, its scale and the ticks it shows, as Recharts' axis map holds it.</summary>
internal sealed class AxisLayout
{
    private BandScale? _band;
    private ValueScale _scale;
    private bool _scaled;

    public required AxisSpec Spec { get; init; }

    public required bool IsX { get; init; }

    public required ScaleKind Kind { get; init; }

    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public double RangeStart { get; private set; }

    public double RangeEnd { get; private set; }

    /// <summary>The labels of a category axis, one per item.</summary>
    public string?[] Categories { get; init; } = [];

    /// <summary>The domain of a numeric axis.</summary>
    public double DomainMin { get; set; }

    public double DomainMax { get; set; }

    /// <summary>The nice tick values of a numeric axis, when it has them.</summary>
    public double[]? NiceTicks { get; set; }

    public double BandSize { get; private set; }

    /// <summary>The smallest distance between two of a numeric axis' values (its data key's), which <c>padding="gap"</c> uses.</summary>
    public double Gap { get; set; } = double.PositiveInfinity;

    /// <summary>The width of a numeric axis' domain before nice ticks round it, which <c>padding="gap"</c> divides by.</summary>
    public double Spread { get; set; }

    /// <summary>Every tick at its coordinate (Recharts' <c>getTicksOfAxis(axis, true)</c>).</summary>
    public AxisTick[] Ticks { get; private set; } = [];

    /// <summary>The ticks shown once colliding labels are dropped.</summary>
    /// <summary>Where each category first stands (built once, on the first lookup).</summary>
    public int CategoryIndex(string? text) => TextIndex.Find(_categoryIndex ??= TextIndex.Of(Categories), text);

    private Dictionary<string, int>? _categoryIndex;

    public AxisTick[] Visible { get; set; } = [];

    /// <summary>The ticks the grid draws lines at: the shown ones, measured at the chart's font size.</summary>
    public AxisTick[] GridVisible { get; set; } = [];

    /// <summary>The grid's own values on this axis as the axis would show them (Recharts' <c>horizontalValues</c>, <c>verticalValues</c>), or null.</summary>
    public AxisTick[]? GridValues { get; set; }

    public bool IsCategory => Kind is ScaleKind.Point or ScaleKind.Band;

    /// <summary>
    /// The values a numeric axis that runs along the categories shows when its scale has no nice ticks (Recharts'
    /// <c>categoricalDomain</c>: its data key's values, in data order), or null.
    /// </summary>
    public double[]? CategoricalTicks { get; set; }

    /// <summary>The time zone of a <c>time</c> (the server's) or <c>utc</c> scale, whose values are milliseconds since the epoch; null otherwise.</summary>
    public TimeZoneInfo? Zone => Kind switch
    {
        ScaleKind.Time => TimeZoneInfo.Local,
        ScaleKind.Utc => TimeZoneInfo.Utc,
        _ => null,
    };

    /// <summary>Whether the ticks are a time scale's own (dates, which Recharts writes with JavaScript's <c>Date.toString</c>) rather than numbers.</summary>
    public bool DateTicks => Zone is not null && NiceTicks is null && CategoricalTicks is null && Spec.Ticks.IsEmpty;

    public double Bandwidth => _band?.Bandwidth ?? 0;

    /// <summary>
    /// How far past a category's start its ticks and its tooltip stand (Recharts' <c>getTicksOfAxis</c> offset): half the
    /// band of an automatic band scale, but 2 pixels for a <c>scale="band"</c> axis (Recharts divides the bandwidth by
    /// <c>offsetForBand</c>, which is half the bandwidth for d3's <c>scaleBand</c>), and nothing for points.
    /// </summary>
    public double TickOffset => Kind != ScaleKind.Band ? 0 : Spec.Scale == "band" ? 2 : Bandwidth / 2;

    /// <summary>The coordinate of a category's tick and tooltip (Recharts' tooltip tick), by index.</summary>
    public double TickAt(int index) => Category(index) + TickOffset;

    public void SetRange(double start, double end)
    {
        (RangeStart, RangeEnd) = Spec.Reversed ? (end, start) : (start, end);
        if (!IsCategory)
        {
            (_scale, _scaled) = (ValueScale.Create(Kind, DomainMin, DomainMax, RangeStart, RangeEnd), true);
            BandSize = 0;
            return;
        }

        var bands = BandScale.Create(Categories.Length, RangeStart, RangeEnd);
        _band = Kind == ScaleKind.Point ? bands.WithPaddingInner(1) : bands;
        BandSize = Kind == ScaleKind.Band ? _band.Bandwidth : 0;
    }

    /// <summary>The coordinate of a numeric value.</summary>
    public double Map(double value) => _scaled ? _scale.Map(value) : double.NaN;

    /// <summary>The start coordinate of a category, by index.</summary>
    public double Category(int index) => _band is null || index < 0 || index >= Categories.Length ? double.NaN : _band.Map(index);

    /// <summary>Whether a value lies within the numeric domain.</summary>
    public bool InDomain(double value) => value >= Math.Min(DomainMin, DomainMax) && value <= Math.Max(DomainMin, DomainMax);

    public void Tick(Func<AxisLayout, double, int, string> format)
    {
        if (!Spec.Ticks.IsEmpty)
        {
            Explicit(format);
            return;
        }

        if (IsCategory)
        {
            var ticks = new AxisTick[Categories.Length];
            for (var i = 0; i < ticks.Length; i++)
            {
                var coordinate = TickAt(i);
                ticks[i] = new AxisTick(i, format(this, i, i), coordinate, coordinate);
            }

            Ticks = ticks;
            return;
        }

        var values = NiceTicks ?? CategoricalTicks ?? _scale.Ticks(Spec.TickCount);
        var numeric = new AxisTick[values.Length];
        var count = 0;
        for (var i = 0; i < values.Length; i++)
        {
            var coordinate = Map(values[i]);
            if (!double.IsNaN(coordinate))
            {
                numeric[count++] = new AxisTick(values[i], format(this, values[i], i), coordinate, coordinate);
            }
        }

        Ticks = count == numeric.Length ? numeric : numeric[..count];
    }

    /// <summary>The ticks the axis' <c>ticks</c> prop names, each at its coordinate; a value the scale cannot place is left out (Recharts' <c>getTicksOfAxis</c> with ticks).</summary>
    private void Explicit(Func<AxisLayout, double, int, string> format) => Ticks = Placed(Spec.Ticks, format);

    /// <summary>Values as ticks at their coordinates (Recharts' <c>getTicksOfAxis</c> over given ticks); a value the scale cannot place is left out.</summary>
    public AxisTick[] Placed(System.Collections.Immutable.ImmutableArray<string> values, Func<AxisLayout, double, int, string> format)
    {
        var ticks = new AxisTick[values.Length];
        var count = 0;
        var offset = TickOffset;
        for (var i = 0; i < ticks.Length; i++)
        {
            var (value, coordinate) = Place(values[i], offset);
            if (!double.IsNaN(coordinate))
            {
                ticks[count++] = new AxisTick(value, format(this, value, (int)Math.Max(value, 0)), coordinate, coordinate);
            }
        }

        return count == ticks.Length ? ticks : ticks[..count];
    }

    private (double Value, double Coordinate) Place(string text, double offset)
    {
        if (IsCategory)
        {
            var index = CategoryIndex(text);
            return (index, index < 0 ? double.NaN : Category(index) + offset);
        }

        var value = double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number) ? number : double.NaN;
        return (value, Map(value));
    }
}
