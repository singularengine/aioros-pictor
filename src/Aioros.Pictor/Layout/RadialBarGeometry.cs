using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>Recharts' <c>RadialBar.getComposedData</c>: each shown radial bar's sectors in its slot of its category's band, stacked when it stacks.</summary>
internal static class RadialBarGeometry
{
    public static void Lay(PolarLayout layout)
    {
        var bars = layout.Polar.RadialBars.Where(bar => !bar.Hide).ToList();
        var series = bars.ConvertAll(bar => new SeriesSpec { Kind = SeriesKind.Bar, DataKey = bar.DataKey, BarSize = bar.BarSize?.ToString(System.Globalization.CultureInfo.InvariantCulture), MaxBarSize = bar.MaxBarSize, StackId = bar.StackId, YAxisId = layout.NumericId(bar) });
        System.Collections.Immutable.ImmutableArray<SeriesSpec> stacked = [.. series];
        var groups = !layout.SeveralNumerics
            ? Stacking.Groups(stacked, layout.Spec, layout.PrimaryNumericId(), horizontal: true, layout.Data)
            : [.. layout.NumericIds().SelectMany(id => Stacking.Groups(stacked, layout.Spec, id, horizontal: true, layout.Data))];
        var slots = BarPositions.Compute(layout.Spec, groups, layout.BandSize, 0);
        for (var b = 0; b < bars.Count; b++)
        {
            layout.Use(series[b].YAxisId);
            var (min, max) = (layout.DomainMin, layout.DomainMax);
            var baseValue = min <= 0 && max >= 0 ? 0 : max < 0 ? max : min;
            var group = groups.Find(candidate => candidate.Bands is not null && candidate.Holds(series[b]));
            layout.RadialBars.Add(Bar(layout, bars[b], slots.TryGetValue(series[b], out var slot) ? slot : new BarSlot(0, 0), baseValue, group is null ? null : (group, series[b])));
        }
    }

    private static RadialBarLayout Bar(PolarLayout layout, RadialBarSpec bar, BarSlot slot, double baseValue, (StackGroup Group, SeriesSpec Series)? stack)
    {
        var polar = layout.Polar;
        var values = layout.Data.Numbers(bar.DataKey);
        var fills = bar.FillKey is { } key ? CssValues.Valid(layout.Data.Texts(key)) : null;
        var names = layout.Data.Texts(bar.NameKey);
        var corner = Polar.Percent(bar.CornerRadius, 0, 0);
        var sectors = new RadialSector[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            var (low, high) = stack is { } piled ? Truncate(layout, piled.Group.Band(piled.Series, i)) : (baseValue, values[i]);
            var (inner, outer, start, end) = polar.Centric ? CentricSector(layout, i, slot, low, high, bar.MinPointSize) : RadialSector(layout, i, slot, low, high, bar.MinPointSize);
            var cell = i < bar.Cells.Length ? bar.Cells[i] : null;
            sectors[i] = new RadialSector(
                new SectorShape(layout.Cx, layout.Cy, inner, outer, start, end, corner) { ForceCorner = bar.ForceCornerRadius, CornerExternal = bar.CornerIsExternal },
                polar.Centric ? default : new SectorShape(layout.Cx, layout.Cy, inner, outer, layout.StartAngle, layout.EndAngle, corner),
                values[i],
                cell?.Fill ?? fills?[i] ?? bar.Fill,
                names[i] ?? string.Empty);
        }

        return new RadialBarLayout { Spec = bar.OpacityKey is { } opacity ? bar with { Cells = ItemStyles.Cells(bar.Cells, layout.Data.Numbers(opacity)) } : bar, Sectors = sectors };
    }

    /// <summary>
    /// Recharts 3's radial <c>RadialBar</c> sector: a ring from its category's tick (<c>getCateCoordinateOfBar</c> on the
    /// ticks of <c>combineAxisTicks</c>, half a band past the band's start), from the base angle to the value's, grown to
    /// <c>minPointSize</c> degrees.
    /// </summary>
    private static (double Inner, double Outer, double Start, double End) RadialSector(PolarLayout layout, int index, BarSlot slot, double low, double high, double minPointSize)
    {
        var inner = layout.Band.Map(index) + slot.Offset;
        var start = layout.Angle(low);
        var end = layout.Angle(high);
        return (inner, inner + slot.Size, start, Grown(start, end, minPointSize));
    }

    /// <summary>Recharts 3's centric <c>RadialBar</c> sector (<c>layout="centric"</c>): a wedge from its category's angle tick (a band past the band's start, towards the range's start), from the base radius to the value's, grown to <c>minPointSize</c> pixels; no background.</summary>
    private static (double Inner, double Outer, double Start, double End) CentricSector(PolarLayout layout, int index, BarSlot slot, double low, double high, double minPointSize)
    {
        var start = layout.Band.Map(index) + layout.CategoryOffset + slot.Offset;
        var inner = layout.Radius(low);
        return (inner, Grown(inner, layout.Radius(high), minPointSize), start, start + slot.Size);
    }

    /// <summary>The far end of a bar grown to its smallest size (Recharts' <c>minPointSize</c>), away from the base.</summary>
    private static double Grown(double from, double to, double minPointSize)
    {
        var delta = to - from;
        return Math.Abs(minPointSize) > 0 && Math.Abs(delta) < Math.Abs(minPointSize)
            ? to + (Math.Sign(delta == 0 ? minPointSize : delta) * (Math.Abs(minPointSize) - Math.Abs(delta)))
            : to;
    }

    /// <summary>Recharts' <c>truncateByDomain</c> of a stacked band.</summary>
    private static (double Low, double High) Truncate(PolarLayout layout, Graphs.Shapes.StackPoint band)
    {
        var (min, max) = (layout.DomainMin, layout.DomainMax);
        var low = double.IsNaN(band.Lower) || band.Lower < min ? min : band.Lower;
        var high = double.IsNaN(band.Upper) || band.Upper > max ? max : band.Upper;
        return (Math.Min(low, max), Math.Max(high, min));
    }
}
