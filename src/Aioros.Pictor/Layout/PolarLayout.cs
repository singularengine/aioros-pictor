using System.Collections.Immutable;
using Aioros.Graphs.Formatting;
using Aioros.Graphs.Scales;
using Aioros.Pictor.Data;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Ticks;

namespace Aioros.Pictor.Layout;

/// <summary>A tick of a polar axis: its value's text, its angle (angle axis) or radius (radius axis), and the item it stands for.</summary>
internal readonly record struct PolarTick(string Text, double Coordinate, int Index);

/// <summary>A radar laid out: one point per category, NaN where a value is missing.</summary>
internal sealed class RadarLayout
{
    public required RadarSpec Spec { get; init; }

    public required double[] Values { get; init; }

    public required double[] X { get; init; }

    public required double[] Y { get; init; }

    /// <summary>A range radar's baseline (its values' lower ends, a single value's own point), empty when no value is a pair.</summary>
    public double[] BaseX { get; init; } = [];

    /// <summary>The baseline's y coordinates.</summary>
    public double[] BaseY { get; init; } = [];

    /// <summary>The lower ends of the values that are pairs, NaN for single values; empty when none is a pair.</summary>
    public double[] Lows { get; init; } = [];

    /// <summary>Whether the radar's values are ranges (Recharts' <c>isRange</c>).</summary>
    public bool IsRange => Lows.Length > 0;

    public string Color => Spec.Stroke is { } stroke && stroke != "none" ? stroke : Spec.Fill ?? "#000";

    /// <summary>Whether the radar stays where it is during an update animation of its chart.</summary>
    public bool Still { get; init; }

    public string Name => Spec.Name ?? Spec.DataKey;
}

/// <summary>A radial bar's sector, its background, value and color.</summary>
internal readonly record struct RadialSector(SectorShape Shape, SectorShape Background, double Value, string? Fill, string Name);

/// <summary>The bars of one <c>RadialBar</c> laid out.</summary>
internal sealed class RadialBarLayout
{
    public required RadialBarSpec Spec { get; init; }

    public required RadialSector[] Sectors { get; init; }

    /// <summary>Whether the bars stay where they are during an update animation of their chart.</summary>
    public bool Still { get; init; }
}

/// <summary>
/// A polar chart laid out as Recharts lays it out (<c>generateCategoricalChart</c> with <c>formatAxisMap</c> of
/// <c>PolarUtils</c>): the offset of the plot, the center and radii, the angle and radius axes with their scales and
/// ticks, and the pies, radars and radial bars.
/// </summary>
internal sealed partial class PolarLayout : IChartPlot
{
    private BandScale? _band;
    private ValueScale? _values;
    private double _domainMin;
    private double _domainMax;
    private Dictionary<string, int>? _firstItems;
    private int _hoverCount = -1;
    private (RadialBarLayout Bar, int Index)[]? _itemStates;

    private PolarLayout(ChartSpec spec, PolarSpec polar, ChartData data)
    {
        Spec = spec;
        Polar = polar;
        Data = data;
    }

    private PolarLayout(PolarLayout source)
    {
        (Spec, Polar, Data) = (source.Spec, source.Polar, source.Data);
        (_band, _values, _domainMin, _domainMax, _firstItems) = (source._band, source._values, source._domainMin, source._domainMax, source._firstItems);
        (Left, Top, PlotWidth, PlotHeight, Right, Bottom) = (source.Left, source.Top, source.PlotWidth, source.PlotHeight, source.Right, source.Bottom);
        (Cx, Cy, InnerRadius, OuterRadius) = (source.Cx, source.Cy, source.InnerRadius, source.OuterRadius);
        (AngleTicks, RadiusTicks, CategoryTicks, BandSize, Categories, Legend) = (source.AngleTicks, source.RadiusTicks, source.CategoryTicks, source.BandSize, source.Categories, source.Legend);
    }

    public ChartSpec Spec { get; }

    public PolarSpec Polar { get; }

    public ChartData Data { get; }

    /// <summary>Whether the angle axis holds the categories (a pie or radar chart, Recharts' <c>centric</c> layout) rather than the radius axis (<c>radial</c>).</summary>
    public bool Centric => Spec.Kind != ChartKind.RadialBar || Polar.Centric;

    /// <summary>Whether the chart follows Recharts 3 where it differs from Recharts 2: a radial bar chart.</summary>
    public bool Recharts3 => Spec.Kind == ChartKind.RadialBar;

    public double Left { get; private set; }

    public double Top { get; private set; }

    public double PlotWidth { get; private set; }

    public double PlotHeight { get; private set; }

    public double Right { get; private set; }

    public double Bottom { get; private set; }

    public double Cx { get; private set; }

    public double Cy { get; private set; }

    public double InnerRadius { get; private set; }

    public double OuterRadius { get; private set; }

    /// <summary>The angle axis' range: Recharts' <c>startAngle</c> and <c>endAngle</c>.</summary>
    public double StartAngle => Polar.StartAngle;

    public double EndAngle => Polar.EndAngle;

    /// <summary>The ticks of the angle axis, as its grid and its labels place them.</summary>
    public PolarTick[] AngleTicks { get; private set; } = [];

    /// <summary>The ticks of the radius axis.</summary>
    public PolarTick[] RadiusTicks { get; private set; } = [];

    /// <summary>Every category at its coordinate (an angle for a radar, a radius for radial bars), which the hover states follow; the axis may show other ticks.</summary>
    public PolarTick[] CategoryTicks { get; private set; } = [];

    /// <summary>The category axis' band size (an angle for a radar, a radius for radial bars).</summary>
    public double BandSize { get; private set; }

    /// <summary>
    /// The offset of the category axis' ticks from their band's start (a band for an angle axis, half a band for a radius
    /// axis of type <c>category</c>), as Recharts 3's <c>combineAxisTicks</c> writes them; a radial bar stands on its tick.
    /// </summary>
    public double CategoryOffset { get; private set; }

    /// <summary>The categories' labels, as the tooltip names them.</summary>
    public string[] Categories { get; private set; } = [];

    public List<PieLayout> Pies { get; } = [];

    public List<RadarLayout> Radars { get; } = [];

    public List<RadialBarLayout> RadialBars { get; } = [];

    public LegendLayout? Legend { get; private set; }

    /// <summary>The legend target of a pie's first sector: the sectors of the pies before it.</summary>
    public int PieOffset(PieLayout pie)
    {
        var offset = 0;
        foreach (var other in Pies)
        {
            if (ReferenceEquals(other, pie))
            {
                break;
            }

            offset += other.Sectors.Length;
        }

        return offset;
    }

    /// <summary>The legend target of a radar: its index among the chart's radars.</summary>
    public int RadarTarget(RadarLayout radar) => SpecIndex.Of(Polar.Radars, radar.Spec);

    /// <summary>The chart's hover states: a pie's sectors, a radial bar chart's drawn sectors with an item tooltip, or its categories (none when no radial bar is shown); counted once, as the painters read it in their loops.</summary>
    public int HoverCount
    {
        get
        {
            if (_hoverCount < 0)
            {
                _hoverCount = PolarHovers.Count(this);
            }

            return _hoverCount;
        }
    }

    /// <summary>The radial bar and the sector of an item hover state (<see cref="ItemBars"/>): the drawn sectors in order, bar after bar, listed once.</summary>
    public (RadialBarLayout Bar, int Index) ItemState(int state)
    {
        _itemStates ??= PolarHovers.ItemStates(RadialBars);
        return _itemStates[state];
    }

    /// <summary>
    /// The item a category's tooltip reads: the category's own index, or, on a category axis that drops duplicated
    /// categories, the first item of that category (Recharts' <c>findEntryInArray</c>); -1 when no item has it.
    /// </summary>
    public int ItemAt(int category) =>
        _firstItems is null ? category : category < Categories.Length && _firstItems.TryGetValue(Categories[category], out var item) ? item : -1;

    /// <summary>Whether a radial bar chart's tooltip follows its bars (<c>shared={false}</c>, Recharts' <c>item</c> tooltip) rather than its categories.</summary>
    public bool ItemBars => Spec.Kind == ChartKind.RadialBar && Spec.Tooltip?.Shared == false;

    /// <summary>The category shown before any pointer event (Recharts' <c>defaultIndex</c>; a pie has no categories), or -1.</summary>
    public int DefaultTip => Spec.Kind != ChartKind.Pie && !ItemBars && Spec.Tooltip?.DefaultIndex is { } index && index >= 0 && index < HoverCount ? index : -1;

    /// <summary>The same chart drawing other pies, radars and radial bars (a frame of an update animation), its axes and legend shared.</summary>
    public PolarLayout With(IEnumerable<PieLayout> pies, IEnumerable<RadarLayout> radars, IEnumerable<RadialBarLayout> bars)
    {
        var copy = new PolarLayout(this);
        copy.Pies.AddRange(pies);
        copy.Radars.AddRange(radars);
        copy.RadialBars.AddRange(bars);
        return copy;
    }

    /// <summary>The chart before anything is drawn: its pies without sectors, its radars without points and its radial bars without sectors, from which Recharts' enter animation grows them.</summary>
    public static PolarLayout Empty(PolarLayout layout) => layout.With(
        layout.Pies.Select(pie => pie.Emptied()),
        layout.Radars.Select(radar => new RadarLayout { Spec = radar.Spec, Values = [], X = [], Y = [] }),
        layout.RadialBars.Select(bar => new RadialBarLayout { Spec = bar.Spec, Sectors = [] }));

    public static PolarLayout Compute(ChartSpec spec, ChartData data)
    {
        var layout = new PolarLayout(spec, spec.Polar ?? new PolarSpec(), data);
        layout.Run();
        return layout;
    }

    /// <summary>The category axis' band scale.</summary>
    public BandScale Band => _band!;

    /// <summary>The domain of the numeric axis the next values map through.</summary>
    public double DomainMin => _domainMin;

    /// <summary>The domain's end.</summary>
    public double DomainMax => _domainMax;

    /// <summary>The angle of a value of a numeric angle axis.</summary>
    public double Angle(double value) => _values.HasValue ? Nullable.GetValueRefOrDefaultRef(in _values).Map(value) : double.NaN;

    /// <summary>The radius of a value of a numeric radius axis.</summary>
    public double Radius(double value) => _values.HasValue ? Nullable.GetValueRefOrDefaultRef(in _values).Map(value) : double.NaN;

    private void Run()
    {
        List<PieLayout>? pies = null;
        if (Spec.Kind == ChartKind.Pie)
        {
            pies = [.. Polar.Pies.Select(pie => PieLayout.Prepare(pie, pie.Data is { } table ? ChartData.From(table) : Data, Spec.Colors))];
            Pies.AddRange(pies.Where(pie => !pie.Spec.Hide));
        }

        Legend = Spec.Legend is { } legend ? LegendLayout.Compute(Spec, legend, PolarLegend.Entries(this, legend, pies)) : null;
        Offset();
        if (Spec.Kind == ChartKind.Pie)
        {
            foreach (var pie in Pies)
            {
                pie.Place(Left, Top, PlotWidth, PlotHeight);
            }

            return;
        }

        Coordinates();
        if (Centric)
        {
            CentricAxes();
            RadarPoints();
            if (Spec.Kind == ChartKind.RadialBar)
            {
                RadialBarGeometry.Lay(this);
            }
        }
        else
        {
            RadialAxes();
            RadialBarGeometry.Lay(this);
        }
    }

    private void Offset()
    {
        var margin = Spec.Margin;
        var (left, right, top, bottom) = (margin.Left, margin.Right, margin.Top, margin.Bottom);
        Legend?.Apply(ref left, ref right, ref top, ref bottom, Recharts3);
        (Left, Top, Right, Bottom) = (left, top, right, bottom);
        PlotWidth = Math.Max(Spec.Width - left - right, 0);
        PlotHeight = Math.Max(Spec.Height - top - bottom, 0);
    }

    private void Coordinates()
    {
        Cx = Layout.Polar.Percent(Polar.Cx, Spec.Width, Spec.Width / 2);
        Cy = Layout.Polar.Percent(Polar.Cy, Spec.Height, Spec.Height / 2);
        var max = Layout.Polar.MaxRadius(Spec.Width, Spec.Height, Left, Right, Top, Bottom);
        InnerRadius = Layout.Polar.Percent(Polar.InnerRadius, max, 0);
        OuterRadius = Layout.Polar.Percent(Polar.OuterRadius, max, max * 0.8);
    }

    private string?[] CategoryTexts(string? key) =>
        key is not null ? Data.Texts(key) : [.. Enumerable.Range(0, Data.Count).Select(index => JsNumber.ToShortestString(index))];

    /// <summary>
    /// A category axis' band scale over its range (reversed by <c>reversed</c>), its ticks at the band start plus an
    /// offset (a band for an angle axis, half a band for a radius axis of type <c>category</c> or any radius axis of a
    /// radial bar chart, whose ticks Recharts 3's <c>combineAxisTicks</c> centers in their bands), and the ticks it shows:
    /// every category, or those its <c>ticks</c> prop names (Recharts' <c>getTicksOfAxis</c>).
    /// </summary>
    private (PolarTick[] Categories, PolarTick[] Shown) CategoryAxis(string?[] texts, double start, double end, bool reversed, ImmutableArray<string> explicitTicks, string? format, bool angle, Func<object?, int, string?>? formatter = null)
    {
        (start, end) = reversed ? (end, start) : (start, end);
        _band = BandScale.Create(texts.Length, start, end);
        BandSize = _band.Bandwidth;
        var offset = angle ? Math.Sign(start - end) * BandSize : Recharts3 || Polar.RadiusAxis?.Category == true ? BandSize / 2 : 0;
        CategoryOffset = offset;
        var ticks = new PolarTick[texts.Length];
        for (var i = 0; i < ticks.Length; i++)
        {
            ticks[i] = new PolarTick(PolarScales.Format(texts[i] ?? string.Empty, i, format, formatter), _band.Map(i) + offset, i);
        }

        Categories = [.. texts.Select(text => text ?? string.Empty)];
        if (explicitTicks.IsEmpty)
        {
            return (ticks, ticks);
        }

        var shown = new List<PolarTick>(explicitTicks.Length);
        foreach (var text in explicitTicks)
        {
            var index = Array.IndexOf(texts, text);
            if (index >= 0)
            {
                shown.Add(new PolarTick(PolarScales.Format(text, shown.Count, format, formatter), _band.Map(index) + offset, shown.Count));
            }
        }

        return (ticks, [.. shown]);
    }

    private void CentricAxes()
    {
        var angle = Polar.AngleAxis;
        var texts = PolarHovers.Distinct(CategoryTexts(angle?.DataKey), angle?.AllowDuplicatedCategory ?? true);
        _firstItems = (angle?.AllowDuplicatedCategory ?? true) ? null : PolarHovers.FirstIndexes(CategoryTexts(angle?.DataKey));
        (CategoryTicks, AngleTicks) = CategoryAxis(texts, StartAngle, EndAngle, angle?.Reversed ?? false, angle?.Ticks ?? [], angle?.TickFormat, angle: true, Text.FormatKeys.TickFunction(angle?.TickFormatter, angle?.TickTexts, angle?.TickConverter));
        MoreCategoryAxes(angles: true);
        var primary = PrimaryNumericId();
        MoreNumericAxes(primary, InnerRadius, OuterRadius, radial: false);
        RadiusTicks = NumericTicks(PolarScales.Numeric(Polar.RadiusAxis), InnerRadius, OuterRadius, KeysOn(primary), nice: true);
        Remember(primary);
    }

    private void RadialAxes()
    {
        var radius = Polar.RadiusAxis;
        var texts = PolarHovers.Distinct(CategoryTexts(radius?.DataKey), radius?.AllowDuplicatedCategory ?? true);
        _firstItems = (radius?.AllowDuplicatedCategory ?? true) ? null : PolarHovers.FirstIndexes(CategoryTexts(radius?.DataKey));
        (CategoryTicks, RadiusTicks) = CategoryAxis(texts, InnerRadius, OuterRadius, radius?.Reversed ?? false, radius?.Ticks ?? [], radius?.TickFormat, angle: false, Text.FormatKeys.TickFunction(radius?.TickFormatter, radius?.TickTexts, radius?.TickConverter));
        MoreCategoryAxes(angles: false);
        var angle = Polar.AngleAxis;
        var props = PolarScales.Numeric(angle, Recharts3);
        var primary = PrimaryNumericId();
        MoreNumericAxes(primary, StartAngle, EndAngle, radial: true);
        AngleTicks = NumericTicks(props, StartAngle, EndAngle, KeysOn(primary), angle?.TickCount is not null, angle: true);
        Remember(primary);
    }

    /// <summary>
    /// A numeric polar axis: the domain of its series' values (each series' own values even where radial bars stack, as
    /// Recharts 3's <c>selectPolarNumericalDomain</c> takes no stack groups in a polar chart) and its explicit ticks, its
    /// scale over its range (reversed by <c>reversed</c>), recharts-scale's nice ticks for an <c>auto</c> scale, d3's
    /// ticks otherwise, or its explicit ticks. A radial bar chart's angle axis keeps its domain where its nice ticks would
    /// extend it, as Recharts 3's <c>combineAxisDomainWithNiceTicks</c> does.
    /// </summary>
    private PolarTick[] NumericTicks(NumericProps axis, double rangeStart, double rangeEnd, IEnumerable<string> keys, bool nice, bool angle = false)
    {
        var (min, max) = Expanded(keys) ? (0d, 1d) : PolarScales.Extent(Data, keys);
        var specified = PolarScales.Parsed(axis.Ticks);
        foreach (var value in specified.Where(value => !double.IsNaN(value)))
        {
            (min, max) = (Math.Min(min, value), Math.Max(max, value));
        }

        var low = AxisDomains.Bound(axis.DomainMin ?? "0", min, max, axis.Overflow, lower: true);
        var high = AxisDomains.Bound(axis.DomainMax ?? "auto", min, max, axis.Overflow, lower: false);
        var kind = ValueScale.Kind(axis.Scale);
        (rangeStart, rangeEnd) = axis.Reversed ? (rangeEnd, rangeStart) : (rangeStart, rangeEnd);
        var (domainLow, domainHigh) = (low, high);
        var values = PolarScales.TickValues(axis, kind, nice, ref low, ref high, rangeStart, rangeEnd);
        if (angle && Recharts3)
        {
            (low, high) = (domainLow, domainHigh);
        }

        (_domainMin, _domainMax) = (Math.Min(low, high), Math.Max(low, high));
        _values = ValueScale.Create(kind, low, high, rangeStart, rangeEnd);
        return PolarScales.Ticks(_values.Value, specified.Length > 0 ? specified : values, axis.Format, axis.Formatter);
    }

    /// <summary>Whether radial bars on these keys stack with the <c>expand</c> offset, whose domain is [0, 1] by design (Recharts 3's <c>combineAxisDomain</c>).</summary>
    private bool Expanded(IEnumerable<string> keys) =>
        Spec.Kind == ChartKind.RadialBar && Spec.StackOffset == StackOffsetKind.Expand && keys.Any(key => Polar.RadialBars.Any(bar => !bar.Hide && bar.StackId is not null && bar.DataKey == key));

    private void RadarPoints()
    {
        foreach (var radar in Polar.Radars.Where(radar => !radar.Hide))
        {
            Use(radar.RadiusAxisId);
            Radars.Add(RadarOf(radar));
        }
    }

    /// <summary>Recharts' <c>Radar.getComposedData</c>: a point per category at its value's radius, and a range radar's baseline at the lower ends.</summary>
    private RadarLayout RadarOf(RadarSpec radar)
    {
        var values = Data.Numbers(radar.DataKey);
        var lows = Data.Lows(radar.DataKey);
        var xs = new double[values.Length];
        var ys = new double[values.Length];
        var baseX = lows.Length > 0 ? new double[values.Length] : [];
        var baseY = lows.Length > 0 ? new double[values.Length] : [];
        var names = (Polar.AngleAxis?.AllowDuplicatedCategory ?? true) ? null : CategoryTexts(Polar.AngleAxis?.DataKey);
        var places = names is null ? null : PolarHovers.FirstIndexes(Categories);
        for (var i = 0; i < values.Length; i++)
        {
            var angle = _band!.Map(places is null ? i : places.GetValueOrDefault(names![i] ?? string.Empty, -1)) + BandSize;
            (xs[i], ys[i]) = double.IsNaN(values[i]) ? (double.NaN, double.NaN) : Layout.Polar.Point(Cx, Cy, Radius(values[i]), angle);
            if (lows.Length > 0)
            {
                (baseX[i], baseY[i]) = double.IsNaN(lows[i]) ? (xs[i], ys[i]) : Layout.Polar.Point(Cx, Cy, Radius(lows[i]), angle);
            }
        }

        return new RadarLayout { Spec = radar, Values = values, X = xs, Y = ys, BaseX = baseX, BaseY = baseY, Lows = lows };
    }

}

internal readonly record struct NumericProps(string? DomainMin, string? DomainMax, bool Overflow, int TickCount, bool Decimals, string? Scale, bool Reversed, ImmutableArray<string> Ticks, string? Format)
{
    /// <summary>The axis' <c>tickFormatter</c> function, over <see cref="Format"/>.</summary>
    public Func<object?, int, string?>? Formatter { get; init; }

    public NumericProps()
        : this(null, null, false, 5, true, null, false, [], null)
    {
    }
}
