using Aioros.Graphs.Formatting;
using Aioros.Pictor.Data;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>An item of a pie: its name, value and colors, as Recharts' <c>getRealPieData</c> spreads them.</summary>
internal readonly record struct PieItem(string Name, double Value, string Fill, string Stroke, Specs.CellSpec? Cell = null);

/// <summary>A sector of a pie laid out: its shape, angles and the point its tooltip is anchored at.</summary>
internal readonly record struct PieSector(SectorShape Shape, double MidAngle, double Percent, double TooltipX, double TooltipY);

/// <summary>A <c>Pie</c> laid out as Recharts' <c>Pie.getComposedData</c> lays it out.</summary>
internal sealed class PieLayout
{
    public required PieSpec Spec { get; init; }

    public required PieItem[] Items { get; init; }

    /// <summary>The items the pie reads, which its label lists read too.</summary>
    public required ChartData Data { get; init; }

    public PieSector[] Sectors { get; private set; } = [];

    public double Cx { get; private set; }

    public double Cy { get; private set; }

    public double InnerRadius { get; private set; }

    public double OuterRadius { get; private set; }

    public double CornerRadius { get; private set; }

    /// <summary>Whether the pie stays where it is during an update animation of its chart.</summary>
    public bool Still { get; private init; }

    /// <summary>The pie before it is drawn, without sectors (Recharts' empty <c>prevSectors</c> when it enters).</summary>
    public PieLayout Emptied() => new() { Spec = Spec, Items = Items, Data = Data, Cx = Cx, Cy = Cy, InnerRadius = InnerRadius, OuterRadius = OuterRadius, CornerRadius = CornerRadius };

    /// <summary>A copy of the pie whose sectors an update animation moves (it owns its sector array), or unchanged and still.</summary>
    public PieLayout Morphed(bool still) => new()
    {
        Spec = Spec,
        Items = Items,
        Data = Data,
        Sectors = still ? Sectors : (PieSector[])Sectors.Clone(),
        Cx = Cx,
        Cy = Cy,
        InnerRadius = InnerRadius,
        OuterRadius = OuterRadius,
        CornerRadius = CornerRadius,
        Still = still,
    };

    /// <summary>The pie's items, read before the plot's size is known (the legend lists them).</summary>
    public static PieLayout Prepare(PieSpec spec, ChartData data, Theming.ChartTheme theme)
    {
        var values = data.Numbers(spec.DataKey);
        var names = data.Texts(spec.NameKey);
        var fills = spec.FillKey is { } key ? Painting.CssValues.Valid(data.Texts(key)) : null;
        spec = spec.OpacityKey is { } opacity ? spec with { Cells = Painting.ItemStyles.Cells(spec.Cells, data.Numbers(opacity)) } : spec;
        var items = new PieItem[data.Count];
        for (var i = 0; i < items.Length; i++)
        {
            var cell = i < spec.Cells.Length ? spec.Cells[i] : null;
            items[i] = new PieItem(names[i] ?? JsNumber.ToShortestString(i), values[i], cell?.Fill ?? fills?[i] ?? spec.Fill ?? theme.PieFill, cell?.Stroke ?? spec.Stroke ?? theme.PieStroke, cell);
        }

        return new PieLayout { Spec = spec, Items = items, Data = data };
    }

    /// <summary>The sectors in a plot: Recharts' <c>parseCoordinateOfPie</c> and its angles, padding and minimum angle.</summary>
    public void Place(double left, double top, double width, double height)
    {
        var max = Polar.MaxRadius(width, height, 0, 0, 0, 0);
        Cx = left + Polar.Percent(Spec.Cx, width, width / 2);
        Cy = top + Polar.Percent(Spec.Cy, height, height / 2);
        InnerRadius = Polar.Percent(Spec.InnerRadius, max, 0);
        OuterRadius = Polar.Percent(Spec.OuterRadius, max, max * 0.8);
        CornerRadius = Polar.Percent(Spec.CornerRadius, OuterRadius - InnerRadius, 0, validate: true);
        var sign = Math.Sign(Spec.EndAngle - Spec.StartAngle);
        var delta = sign * Math.Min(Math.Abs(Spec.EndAngle - Spec.StartAngle), 360);
        var minAngle = Math.Abs(Spec.MinAngle);
        var nonZero = Items.Count(item => Value(item) != 0);
        var padding = (Math.Abs(delta) >= 360 ? nonZero : nonZero - 1) * Spec.PaddingAngle;
        var total = Math.Abs(delta) - (nonZero * minAngle) - padding;
        var sum = Items.Sum(Value);
        if (sum <= 0)
        {
            Sectors = [];
            return;
        }

        var sectors = new PieSector[Items.Length];
        var previousEnd = 0.0;
        var middleRadius = (InnerRadius + OuterRadius) / 2;
        for (var i = 0; i < Items.Length; i++)
        {
            var value = Value(Items[i]);
            var percent = value / sum;
            var start = i == 0 ? Spec.StartAngle : previousEnd + (Math.Sign(delta) * Spec.PaddingAngle * (value != 0 ? 1 : 0));
            var end = start + (Math.Sign(delta) * ((value != 0 ? minAngle : 0) + (percent * total)));
            var mid = (start + end) / 2;
            var (x, y) = Polar.Point(Cx, Cy, middleRadius, mid);
            sectors[i] = new PieSector(new SectorShape(Cx, Cy, InnerRadius, OuterRadius, start, end, CornerRadius), mid, percent, x, y);
            previousEnd = end;
        }

        Sectors = sectors;
    }

    private static double Value(PieItem item) => double.IsNaN(item.Value) ? 0 : item.Value;
}
