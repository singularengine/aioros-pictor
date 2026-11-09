using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>
/// Several polar axes per chart, as Recharts' polar <c>formatAxisMap</c> lays them out: each numeric axis takes the
/// domain of the series that name its id (<c>angleAxisId</c>, <c>radiusAxisId</c>), over the chart's one range; a series
/// naming an id no axis declares gets an implicit axis of its own (Recharts' <c>getAxisMapByItems</c>); the category
/// axes after the first label the same bands with their own data key.
/// </summary>
internal sealed partial class PolarLayout
{
    private Dictionary<string, (ValueScale Scale, double Min, double Max)>? _numerics;

    private List<PolarTick[]>? _moreAngleTicks;
    private List<PolarTick[]>? _moreRadiusTicks;

    /// <summary>Whether the chart has several numeric axes, each laid out under its id.</summary>
    public bool SeveralNumerics => _numerics is not null;

    /// <summary>The ticks of <see cref="PolarSpec.MoreAngleAxes"/>, in order.</summary>
    public List<PolarTick[]> MoreAngleTicks => _moreAngleTicks ??= [];

    /// <summary>The ticks of <see cref="PolarSpec.MoreRadiusAxes"/>, in order.</summary>
    public List<PolarTick[]> MoreRadiusTicks => _moreRadiusTicks ??= [];

    /// <summary>The numeric axis a radial bar's values map through: its radius axis in a centric chart, its angle axis otherwise.</summary>
    public string NumericId(RadialBarSpec bar) => Centric ? bar.RadiusAxisId : bar.AngleAxisId;

    public List<string> NumericIds()
    {
        var ids = new List<string>(1);
        foreach (var radar in Polar.Radars)
        {
            Add(ids, radar.Hide ? null : radar.RadiusAxisId);
        }

        foreach (var bar in Polar.RadialBars)
        {
            Add(ids, bar.Hide || Spec.Kind != ChartKind.RadialBar ? null : NumericId(bar));
        }

        return ids;

        static void Add(List<string> ids, string? id)
        {
            if (id is not null && !ids.Contains(id))
            {
                ids.Add(id);
            }
        }
    }

    /// <summary>The first declared numeric axis' id, or the first visible series' when no axis is declared.</summary>
    public string PrimaryNumericId()
    {
        if ((Centric ? Polar.RadiusAxis?.Id : Polar.AngleAxis?.Id) is { } declared)
        {
            return declared;
        }

        foreach (var radar in Polar.Radars)
        {
            if (!radar.Hide && Spec.Kind == ChartKind.Radar)
            {
                return radar.RadiusAxisId;
            }
        }

        foreach (var bar in Polar.RadialBars)
        {
            if (!bar.Hide && Spec.Kind == ChartKind.RadialBar)
            {
                return NumericId(bar);
            }
        }

        return "0";
    }

    /// <summary>The data keys of the visible series on a numeric axis (every visible series' when the chart has one numeric axis).</summary>
    private IEnumerable<string> KeysOn(string id)
    {
        if (_numerics is null)
        {
            return Spec.Kind == ChartKind.Radar
                ? Polar.Radars.Where(static radar => !radar.Hide).Select(static radar => radar.DataKey)
                : Polar.RadialBars.Where(static bar => !bar.Hide).Select(static bar => bar.DataKey);
        }

        return KeysOnAxis(id);
    }

    /// <summary>The data keys of the visible series on one of several numeric axes.</summary>
    private IEnumerable<string> KeysOnAxis(string id) => Spec.Kind == ChartKind.Radar
        ? Polar.Radars.Where(radar => !radar.Hide && radar.RadiusAxisId == id).Select(radar => radar.DataKey)
        : Polar.RadialBars.Where(bar => !bar.Hide && NumericId(bar) == id).Select(bar => bar.DataKey);

    /// <summary>Keeps the numeric axis just laid out under its id, when the chart has more than one.</summary>
    private void Remember(string id)
    {
        if (_numerics is not null)
        {
            _numerics[id] = (_values!.Value, _domainMin, _domainMax);
        }
    }

    /// <summary>Maps the next values through a numeric axis by id (the primary one when the id is unknown).</summary>
    public void Use(string id)
    {
        if (_numerics is not null && _numerics.TryGetValue(id, out var axis))
        {
            (_values, _domainMin, _domainMax) = (axis.Scale, axis.Min, axis.Max);
        }
    }

    /// <summary>The numeric axes other than the primary one: the declared ones (their ticks drawn) and the implicit ones of ids no axis declares.</summary>
    private void MoreNumericAxes(string primary, double start, double end, bool radial)
    {
        if ((radial ? Polar.MoreAngleAxes.Length : Polar.MoreRadiusAxes.Length) == 0 && OnlyOn(primary))
        {
            return;
        }

        var ids = NumericIds();
        _numerics = new(StringComparer.Ordinal);
        var declared = new HashSet<string>(StringComparer.Ordinal) { primary };
        if (radial)
        {
            foreach (var angle in Polar.MoreAngleAxes)
            {
                declared.Add(angle.Id);
                MoreAngleTicks.Add(NumericTicks(PolarScales.Numeric(angle, Recharts3), start, end, KeysOn(angle.Id), angle.TickCount is not null, angle: true));
                Remember(angle.Id);
            }
        }
        else
        {
            foreach (var radius in Polar.MoreRadiusAxes)
            {
                declared.Add(radius.Id);
                MoreRadiusTicks.Add(NumericTicks(PolarScales.Numeric(radius), start, end, KeysOn(radius.Id), nice: true));
                Remember(radius.Id);
            }
        }

        foreach (var id in ids.Where(declared.Add))
        {
            NumericTicks(new NumericProps(), start, end, KeysOn(id), nice: !radial);
            Remember(id);
        }
    }

    /// <summary>Whether every visible series maps through the numeric axis of this id.</summary>
    private bool OnlyOn(string id)
    {
        foreach (var radar in Polar.Radars)
        {
            if (!radar.Hide && Spec.Kind == ChartKind.Radar && radar.RadiusAxisId != id)
            {
                return false;
            }
        }

        foreach (var bar in Polar.RadialBars)
        {
            if (!bar.Hide && Spec.Kind == ChartKind.RadialBar && NumericId(bar) != id)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The category axes after the first: their own data key's texts on the first axis' bands.</summary>
    private void MoreCategoryAxes(bool angles)
    {
        if ((angles ? Polar.MoreAngleAxes.Length : Polar.MoreRadiusAxes.Length) == 0)
        {
            return;
        }

        if (angles)
        {
            MoreAngleTicks.AddRange(Polar.MoreAngleAxes.Select(axis => MoreCategoryTicks(axis.DataKey, axis.TickFormat, Text.FormatKeys.TickFunction(axis.TickFormatter, axis.TickTexts, axis.TickConverter))));
        }
        else
        {
            MoreRadiusTicks.AddRange(Polar.MoreRadiusAxes.Select(axis => MoreCategoryTicks(axis.DataKey, axis.TickFormat, Text.FormatKeys.TickFunction(axis.TickFormatter, axis.TickTexts, axis.TickConverter))));
        }
    }

    private PolarTick[] MoreCategoryTicks(string? key, string? format, Func<object?, int, string?>? formatter)
    {
        var texts = CategoryTexts(key);
        var ticks = new PolarTick[Math.Min(texts.Length, CategoryTicks.Length)];
        for (var i = 0; i < ticks.Length; i++)
        {
            ticks[i] = new PolarTick(PolarScales.Format(texts[i] ?? string.Empty, i, format, formatter), CategoryTicks[i].Coordinate, i);
        }

        return ticks;
    }
}
