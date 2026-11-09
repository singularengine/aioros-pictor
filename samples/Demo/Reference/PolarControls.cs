using System.Globalization;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Demo;

/// <summary>Builds the elements of a polar example (what its Gallery page declares), so the desktop control can draw it.</summary>
internal static class PolarControls
{
    public static PolarChart Create(RechartsExample example)
    {
        var spec = example.Spec;
        var polar = spec.Polar ?? new PolarSpec();
        PolarChart chart = spec.Kind switch
        {
            ChartKind.Pie => new PieChart(),
            ChartKind.Radar => new RadarChart(),
            _ => new RadialBarChart { BarSize = spec.BarSize is { } size ? double.Parse(size, CultureInfo.InvariantCulture) : null, MaxBarSize = spec.MaxBarSize, BarGap = spec.BarGap, BarCategoryGap = spec.BarCategoryGap, StackOffset = spec.StackOffset, Layout = polar.Centric ? "centric" : "radial" },
        };
        SpecControls.Sized(chart, example);
        chart.Cx = polar.Cx;
        chart.Cy = polar.Cy;
        chart.InnerRadius = polar.InnerRadius;
        chart.OuterRadius = polar.OuterRadius;
        chart.StartAngle = polar.StartAngle;
        chart.EndAngle = polar.EndAngle;
        foreach (var element in Elements(spec, polar).OrderBy(pair => pair.Order).Select(pair => pair.Element))
        {
            chart.Elements.Add(element);
        }

        return chart;
    }

    private static IEnumerable<(int Order, IChartElement Element)> Elements(ChartSpec spec, PolarSpec polar)
    {
        if (polar.Grid is { } grid)
        {
            yield return (grid.Order, new PolarGrid
            {
                GridType = grid.Circle ? "circle" : "polygon",
                RadialLines = grid.RadialLines,
                Stroke = grid.Stroke,
                StrokeDashArray = grid.StrokeDashArray,
                StrokeWidth = grid.StrokeWidth,
                StrokeOpacity = grid.StrokeOpacity,
                Fill = grid.Fill,
                PolarAngles = SpecControls.Join(grid.PolarAngles),
                PolarRadius = SpecControls.Join(grid.PolarRadius),
            });
        }

        if (polar.AngleAxis is { } angle)
        {
            yield return (angle.Order, AngleAxis(angle));
        }

        if (polar.RadiusAxis is { } radius)
        {
            yield return (radius.Order, RadiusAxis(radius));
        }

        foreach (var more in polar.MoreAngleAxes)
        {
            yield return (more.Order, AngleAxis(more));
        }

        foreach (var more in polar.MoreRadiusAxes)
        {
            yield return (more.Order, RadiusAxis(more));
        }

        foreach (var pie in polar.Pies)
        {
            yield return (pie.Order, Pie(pie));
        }

        foreach (var radar in polar.Radars)
        {
            yield return (radar.Order, Radar(radar));
        }

        foreach (var bar in polar.RadialBars)
        {
            yield return (bar.Order, RadialBar(bar));
        }

        if (spec.Legend is { } legend)
        {
            yield return (int.MaxValue - 1, SpecControls.LegendOf(legend));
        }

        if (spec.Tooltip is not null)
        {
            yield return (int.MaxValue, SpecControls.TooltipOf(spec.Tooltip));
        }
    }

    private static string? Domain(string? min, string? max) =>
        min is null && max is null ? null : $"{min ?? "0"},{max ?? "auto"}";

    private static string? Ticks(System.Collections.Immutable.ImmutableArray<string> ticks) => ticks.IsDefaultOrEmpty ? null : string.Join(", ", ticks);

    private static PolarAngleAxis AngleAxis(PolarAngleAxisSpec angle) => new()
    {
        AngleAxisId = angle.Id,
        DataKey = angle.DataKey,
        AxisLineType = angle.CircleLine ? "circle" : "polygon",
        AxisLine = angle.AxisLine,
        TickLine = angle.TickLine,
        Tick = angle.Tick,
        Hide = angle.Hide,
        TickFormat = angle.TickFormat,
        AllowDataOverflow = angle.AllowDataOverflow,
        Stroke = angle.Stroke,
        TickSize = angle.TickSize,
        Orientation = angle.Inner ? "inner" : "outer",
        Type = angle.Type,
        Domain = Domain(angle.DomainMin, angle.DomainMax),
        TickCount = angle.TickCount,
        AllowDecimals = angle.AllowDecimals,
        Scale = angle.Scale,
        Reversed = angle.Reversed,
        Ticks = Ticks(angle.Ticks),
        AllowDuplicatedCategory = angle.AllowDuplicatedCategory,
        TickFill = angle.TickFill,
        TickFontSize = angle.TickFontSize,
        AxisLineStroke = angle.AxisLineStyle?.Stroke,
        AxisLineStrokeWidth = angle.AxisLineStyle?.StrokeWidth,
        TickLineStroke = angle.TickLineStyle?.Stroke,
        TickLineStrokeWidth = angle.TickLineStyle?.StrokeWidth,
    };

    private static PolarRadiusAxis RadiusAxis(PolarRadiusAxisSpec radius)
    {
        var axis = RadiusAxisOf(radius);
        SpecControls.AddLabels(axis.Labels, radius.Labels);
        return axis;
    }

    private static PolarRadiusAxis RadiusAxisOf(PolarRadiusAxisSpec radius) => CartesianControls.Labelled(
        new PolarRadiusAxis
        {
            RadiusAxisId = radius.Id,
            Angle = radius.Angle,
            Orientation = radius.Orientation,
            Stroke = radius.Stroke,
            AxisLine = radius.AxisLine,
            Tick = radius.Tick,
            AllowDecimals = radius.AllowDecimals,
            AllowDataOverflow = radius.AllowDataOverflow,
            TickFormat = radius.TickFormat,
            Hide = radius.Hide,
            Domain = Domain(radius.DomainMin, radius.DomainMax),
            DataKey = radius.DataKey,
            Type = radius.Category ? AxisType.Category : AxisType.Number,
            TickCount = radius.TickCount,
            Scale = radius.Scale,
            Reversed = radius.Reversed,
            Ticks = Ticks(radius.Ticks),
            AllowDuplicatedCategory = radius.AllowDuplicatedCategory,
            TickFill = radius.TickFill,
            TickFontSize = radius.TickFontSize,
            AxisLineStroke = radius.AxisLineStyle?.Stroke,
            AxisLineStrokeWidth = radius.AxisLineStyle?.StrokeWidth,
            Label = radius.Label?.Value,
            LabelPosition = radius.Label?.Position,
        },
        radius.Label);

    private static Pie Pie(PieSpec spec)
    {
        var pie = new Pie
        {
            IsAnimationActive = spec.Animation?.Active ?? true,
            DataKey = spec.DataKey,
            NameKey = spec.NameKey,
            FillKey = spec.FillKey,
            Data = spec.Data is { } table ? Rows(table) : null,
            Cx = spec.Cx,
            Cy = spec.Cy,
            InnerRadius = spec.InnerRadius,
            OuterRadius = spec.OuterRadius,
            StartAngle = spec.StartAngle,
            EndAngle = spec.EndAngle,
            PaddingAngle = spec.PaddingAngle,
            MinAngle = spec.MinAngle,
            CornerRadius = spec.CornerRadius,
            Fill = spec.Fill,
            Stroke = spec.Stroke,
            Label = spec.Label,
            LabelLine = spec.LabelLine,
            ActiveIndex = spec.ActiveIndex,
            ActiveShape = Shape(spec.ActiveShape),
            ActiveIndexes = SpecControls.Join(spec.ActiveIndexes),
            InactiveShape = Shape(spec.InactiveShape),
            BlendStroke = spec.BlendStroke,
            LabelOffsetRadius = spec.LabelOffsetRadius,
            LabelFill = spec.LabelProps?.Fill,
            LabelFontSize = spec.LabelProps?.FontSize,
            LabelFontWeight = spec.LabelProps?.FontWeight,
            LabelStroke = spec.LabelProps?.Stroke,
            LabelLineStroke = spec.LabelLineStroke,
            LabelLineStrokeWidth = spec.LabelLineStrokeWidth,
            LabelLineStrokeDashArray = spec.LabelLineStrokeDashArray,
            LegendType = spec.LegendType,
            Hide = spec.Hide,
            TooltipType = spec.TooltipNone ? "none" : null,
        };
        Cells(pie, spec.Cells);
        SpecControls.AddLabelLists(pie.Cells, null, spec.LabelLists);
        return pie;
    }

    private static RadialBar RadialBar(RadialBarSpec bar)
    {
        var element = new RadialBar
        {
            AngleAxisId = bar.AngleAxisId,
            RadiusAxisId = bar.RadiusAxisId,
            ActiveIndex = bar.ActiveIndex,
            DataKey = bar.DataKey,
            Name = bar.Name,
            NameKey = bar.NameKey,
            Stroke = bar.Stroke,
            Unit = bar.Unit,
            LegendType = bar.LegendType,
            Hide = bar.Hide,
            TooltipType = bar.TooltipNone ? "none" : null,
            FillKey = bar.FillKey,
            Fill = bar.Fill,
            Background = bar.Background,
            BackgroundStroke = bar.BackgroundProps?.Stroke,
            BackgroundStrokeWidth = bar.BackgroundProps?.StrokeWidth,
            BackgroundFillOpacity = bar.BackgroundProps?.FillOpacity,
            MinPointSize = bar.MinPointSize,
            MaxBarSize = bar.MaxBarSize,
            BarSize = bar.BarSize,
            StackId = bar.StackId,
            CornerRadius = bar.CornerRadius,
            ForceCornerRadius = bar.ForceCornerRadius,
            CornerIsExternal = bar.CornerIsExternal,
        };
        SpecControls.Animated(element, bar.Animation);
        Cells(element, bar.Cells);
        SpecControls.AddLabelLists(element.Cells, bar.Labels, bar.LabelLists);
        return element;
    }

    private static ActiveShape? Shape(SectorShapeSpec? shape) => shape is null
        ? null
        : new ActiveShape { Fill = shape.Fill, Stroke = shape.Stroke, InnerRadius = shape.InnerRadius, OuterRadius = shape.OuterRadius, CornerRadius = shape.CornerRadius, FillOpacity = shape.FillOpacity, Opacity = shape.Opacity, StrokeWidth = shape.StrokeWidth };

    private static Radar Radar(RadarSpec radar)
    {
        var element = new Radar
        {
            AngleAxisId = radar.AngleAxisId,
            RadiusAxisId = radar.RadiusAxisId,
            DataKey = radar.DataKey,
            Name = radar.Name,
            Stroke = radar.Stroke,
            Fill = radar.Fill,
            FillOpacity = radar.FillOpacity,
            Dot = radar.Dot,
            DotRadius = radar.DotProps?.R,
            DotFill = radar.DotProps?.Fill,
            DotStroke = radar.DotProps?.Stroke,
            DotStrokeWidth = radar.DotProps?.StrokeWidth,
            DotFillOpacity = radar.DotProps?.FillOpacity,
            DotStrokeDashArray = radar.DotProps?.StrokeDashArray,
            ActiveDot = radar.ActiveDot ? radar.ActiveDotRadius : 0,
            ActiveDotFill = radar.ActiveDotStyle?.Fill,
            ActiveDotStroke = radar.ActiveDotStyle?.Stroke,
            ActiveDotStrokeWidth = radar.ActiveDotStyle?.StrokeWidth,
            ConnectNulls = radar.ConnectNulls,
            StrokeWidth = radar.StrokeWidth,
            Unit = radar.Unit,
            LegendType = radar.LegendType,
            Hide = radar.Hide,
            TooltipType = radar.TooltipNone ? "none" : null,
        };
        SpecControls.AddLabelLists(element.Parts, radar.Labels, radar.LabelLists);
        return SpecControls.Animated(element, radar.Animation);
    }

    private static void Cells(PolarSeries series, System.Collections.Immutable.ImmutableArray<CellSpec> cells)
    {
        foreach (var cell in cells)
        {
            series.Cells.Add(SpecControls.CellOf(cell));
        }
    }

    /// <summary>A table's items back as dictionaries, as the example's JSON gave them.</summary>
    public static List<IReadOnlyDictionary<string, object?>> Rows(DataTable table) =>
        [.. Enumerable.Range(0, table.Count).Select(i => (IReadOnlyDictionary<string, object?>)table.Columns.ToDictionary(pair => pair.Key, pair => (object?)pair.Value[i], StringComparer.Ordinal))];
}
