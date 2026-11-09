using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Demo.ExampleJson;

namespace Aioros.Pictor.Demo;

/// <summary>Reads Recharts' polar examples (<c>PieChart</c>, <c>RadarChart</c>, <c>RadialBarChart</c>), prop for prop.</summary>
internal static class PolarExamples
{
    public static ChartSpec Read(ChartSpec spec, string? kind, JsonElement root)
    {
        var chart = root.TryGetProperty("chartProps", out var props) ? props : default;
        var chartKind = kind switch
        {
            "PieChart" => ChartKind.Pie,
            "RadarChart" => ChartKind.Radar,
            _ => ChartKind.RadialBar,
        };
        var polar = new PolarSpec
        {
            Cx = TextOf(chart, "cx") ?? "50%",
            Cy = TextOf(chart, "cy") ?? "50%",
            InnerRadius = TextOf(chart, "innerRadius") ?? "0",
            OuterRadius = TextOf(chart, "outerRadius") ?? "80%",
            StartAngle = NumberOf(chart, "startAngle") ?? (chartKind == ChartKind.Radar ? 90 : 0),
            EndAngle = NumberOf(chart, "endAngle") ?? (chartKind == ChartKind.Radar ? -270 : 360),
            Centric = chartKind == ChartKind.RadialBar && root.TryGetProperty("layout", out var layout) && layout.GetString() == "centric",
        };
        spec = spec with { Kind = chartKind, BarSize = TextOf(chart, "barSize"), MaxBarSize = NumberOf(chart, "maxBarSize"), BarGap = TextOf(chart, "barGap") ?? "4", BarCategoryGap = TextOf(chart, "barCategoryGap") ?? "10%", StackOffset = Enum.TryParse<StackOffsetKind>(TextOf(chart, "stackOffset"), ignoreCase: true, out var offset) ? offset : StackOffsetKind.None };
        var order = 0;
        foreach (var child in root.GetProperty("children").EnumerateArray())
        {
            (spec, polar) = Child(spec, polar, child, order++);
        }

        return spec with { Polar = polar };
    }

    private static (ChartSpec Spec, PolarSpec Polar) Child(ChartSpec spec, PolarSpec polar, JsonElement child, int order)
    {
        var props = child.GetProperty("props");
        return child.GetProperty("type").GetString() switch
        {
            "Tooltip" => (spec with { Tooltip = Tooltip(child.GetProperty("props")) }, polar),
            "Legend" => (spec with { Legend = Legend(props) }, polar),
            "PolarGrid" => (spec, polar with { Grid = new PolarGridSpec
            {
                Circle = TextOf(props, "gridType") == "circle",
                RadialLines = FlagOf(props, "radialLines") ?? true,
                Stroke = TextOf(props, "stroke"),
                StrokeDashArray = TextOf(props, "strokeDasharray"),
                StrokeWidth = NumberOf(props, "strokeWidth"),
                StrokeOpacity = NumberOf(props, "strokeOpacity"),
                Fill = TextOf(props, "fill"),
                PolarAngles = ListOf(props, "polarAngles", item => item.GetDouble()),
                PolarRadius = ListOf(props, "polarRadius", item => item.GetDouble()),
                Order = order,
            } }),
            "PolarAngleAxis" => (spec, polar.AngleAxis is null ? polar with { AngleAxis = AngleAxis(props, order) } : polar with { MoreAngleAxes = polar.MoreAngleAxes.Add(AngleAxis(props, order)) }),
            "PolarRadiusAxis" => (spec, polar.RadiusAxis is null ? polar with { RadiusAxis = RadiusAxis(child, props, order) } : polar with { MoreRadiusAxes = polar.MoreRadiusAxes.Add(RadiusAxis(child, props, order)) }),
            "Pie" => (spec, polar with { Pies = polar.Pies.Add(Pie(child, props, order)) }),
            "Radar" => (spec, polar with { Radars = polar.Radars.Add(Radar(child, props, order)) }),
            "RadialBar" => (spec, polar with { RadialBars = polar.RadialBars.Add(RadialBar(child, props, order)) }),
            _ => (spec, polar),
        };
    }

    private static string?[] Domain(JsonElement props) =>
        props.TryGetProperty("domain", out var range) && range.ValueKind == JsonValueKind.Array ? [.. range.EnumerateArray().Select(value => value.ValueKind == JsonValueKind.Number ? value.GetRawText() : value.GetString())] : [null, null];

    private static ImmutableArray<string> Ticks(JsonElement props) =>
        props.TryGetProperty("ticks", out var ticks) ? [.. ticks.EnumerateArray().Select(value => value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText())] : [];

    private static LineStyle? Line(JsonElement props, string name) =>
        props.TryGetProperty(name, out var line) && line.ValueKind == JsonValueKind.Object ? new LineStyle(TextOf(line, "stroke"), NumberOf(line, "strokeWidth"), TextOf(line, "strokeDasharray")) : null;

    private static PolarAngleAxisSpec AngleAxis(JsonElement props, int order)
    {
        var domain = Domain(props);
        var tick = props.TryGetProperty("tick", out var value) ? value : default;
        return new PolarAngleAxisSpec
        {
            Id = TextOf(props, "angleAxisId") ?? "0",
            DataKey = TextOf(props, "dataKey"),
            CircleLine = TextOf(props, "axisLineType") == "circle",
            AxisLine = FlagOf(props, "axisLine") ?? true,
            TickLine = FlagOf(props, "tickLine") ?? true,
            Tick = FlagOf(props, "tick") ?? true,
            Hide = FlagOf(props, "hide") ?? false,
            TickFormat = TextOf(props, "tickFormatter"),
            AllowDataOverflow = FlagOf(props, "allowDataOverflow") ?? false,
            Stroke = TextOf(props, "stroke"),
            TickSize = NumberOf(props, "tickSize") ?? 8,
            Inner = TextOf(props, "orientation") == "inner",
            Type = TextOf(props, "type") switch { "number" => AxisType.Number, "category" => AxisType.Category, _ => null },
            DomainMin = domain[0],
            DomainMax = domain.Length > 1 ? domain[1] : null,
            TickCount = (int?)NumberOf(props, "tickCount"),
            AllowDecimals = FlagOf(props, "allowDecimals") ?? true,
            Scale = TextOf(props, "scale"),
            Reversed = FlagOf(props, "reversed") ?? false,
            Ticks = Ticks(props),
            AllowDuplicatedCategory = FlagOf(props, "allowDuplicatedCategory") ?? true,
            TickFill = TextOf(tick, "fill"),
            TickFontSize = NumberOf(tick, "fontSize"),
            AxisLineStyle = Line(props, "axisLine"),
            TickLineStyle = Line(props, "tickLine"),
            Order = order,
        };
    }

    private static PolarRadiusAxisSpec RadiusAxis(JsonElement child, JsonElement props, int order)
    {
        var domain = Domain(props);
        var tick = props.TryGetProperty("tick", out var value) ? value : default;
        return new PolarRadiusAxisSpec
        {
            Id = TextOf(props, "radiusAxisId") ?? "0",
            Angle = NumberOf(props, "angle") ?? 0,
            DataKey = TextOf(props, "dataKey"),
            Category = TextOf(props, "type") == "category",
            Orientation = TextOf(props, "orientation") ?? "right",
            Stroke = TextOf(props, "stroke"),
            AxisLine = FlagOf(props, "axisLine") ?? true,
            Tick = FlagOf(props, "tick") ?? true,
            AllowDecimals = FlagOf(props, "allowDecimals") ?? true,
            AllowDataOverflow = FlagOf(props, "allowDataOverflow") ?? false,
            TickFormat = TextOf(props, "tickFormatter"),
            Hide = FlagOf(props, "hide") ?? false,
            DomainMin = domain[0],
            DomainMax = domain.Length > 1 ? domain[1] : null,
            TickCount = (int)(NumberOf(props, "tickCount") ?? 5),
            Scale = TextOf(props, "scale"),
            Reversed = FlagOf(props, "reversed") ?? false,
            Ticks = Ticks(props),
            AllowDuplicatedCategory = FlagOf(props, "allowDuplicatedCategory") ?? true,
            TickFill = TextOf(tick, "fill"),
            TickFontSize = NumberOf(tick, "fontSize"),
            AxisLineStyle = Line(props, "axisLine"),
            Label = CartesianExamples.Label(props),
            Labels = CartesianExamples.Labels(child),
            Order = order,
        };
    }

    private static ImmutableArray<CellSpec> Cells(JsonElement child) =>
        [.. Children(child, "Cell").Select(cell => CellOf(cell.GetProperty("props")))];

    private static PieSpec Pie(JsonElement child, JsonElement props, int order)
    {
        var dataKey = TextOf(props, "dataKey") ?? "value";
        var nameKey = TextOf(props, "nameKey") ?? "name";
        var active = ShapeOf(props, "activeShape");
        return Labelled(new PieSpec
        {
            ClassName = TextOf(props, "className"),
            RootTabIndex = NumberOf(props, "rootTabIndex") is { } tab ? (int)tab : 0,
            Animation = Animation(props),
            DataKey = dataKey,
            NameKey = nameKey,
            FillKey = "fill",
            Data = props.TryGetProperty("data", out var data) ? DataTable.Read((IEnumerable?)Value(data), [dataKey, nameKey, "fill", .. LabelListsOf(child, implicitLabel: false).Others.Prepend(LabelListsOf(child, implicitLabel: false).First).Select(list => list?.DataKey)], static (_, _) => null) : null,
            Cx = TextOf(props, "cx") ?? "50%",
            Cy = TextOf(props, "cy") ?? "50%",
            InnerRadius = TextOf(props, "innerRadius") ?? "0",
            OuterRadius = TextOf(props, "outerRadius") ?? "80%",
            StartAngle = NumberOf(props, "startAngle") ?? 0,
            EndAngle = NumberOf(props, "endAngle") ?? 360,
            PaddingAngle = NumberOf(props, "paddingAngle") ?? 0,
            MinAngle = NumberOf(props, "minAngle") ?? 0,
            CornerRadius = TextOf(props, "cornerRadius"),
            Fill = TextOf(props, "fill"),
            Stroke = TextOf(props, "stroke"),
            Label = FlagOf(props, "label") ?? props.TryGetProperty("label", out var labelled) && labelled.ValueKind == JsonValueKind.Object,
            LabelLine = FlagOf(props, "labelLine") ?? true,
            Cells = Cells(child),
            LabelLists = [.. LabelListsOf(child, implicitLabel: false).Others.Prepend(LabelListsOf(child, implicitLabel: false).First).OfType<LabelSpec>()],
            ActiveIndex = (int?)NumberOf(props, "activeIndex"),
            ActiveShape = active,
            LegendType = Icon(TextOf(props, "legendType")) ?? LegendIcon.Rect,
            Hide = FlagOf(props, "hide") ?? false,
            TooltipNone = TextOf(props, "tooltipType") == "none",

            Order = order,
        }, props);
    }

    /// <summary>A pie's label and label line objects.</summary>
    private static PieSpec Labelled(PieSpec pie, JsonElement props)
    {
        var label = props.TryGetProperty("label", out var labelProps) && labelProps.ValueKind == JsonValueKind.Object ? labelProps : default;
        var line = props.TryGetProperty("labelLine", out var lineProps) && lineProps.ValueKind == JsonValueKind.Object ? lineProps : default;
        var activeIndex = props.TryGetProperty("activeIndex", out var index) ? index : default;
        return pie with
        {
            ActiveIndexes = activeIndex.ValueKind == JsonValueKind.Array ? [.. activeIndex.EnumerateArray().Select(item => item.GetInt32())] : [],
            InactiveShape = ShapeOf(props, "inactiveShape"),
            BlendStroke = FlagOf(props, "blendStroke") ?? false,
            LabelProps = label.ValueKind == JsonValueKind.Object ? LabelOf(label) : null,
            LabelOffsetRadius = NumberOf(label, "offsetRadius") ?? 20,
            LabelLineStroke = TextOf(line, "stroke"),
            LabelLineStrokeWidth = NumberOf(line, "strokeWidth"),
            LabelLineStrokeDashArray = TextOf(line, "strokeDasharray"),
        };
    }

    private static SectorShapeSpec? ShapeOf(JsonElement props, string name) =>
        props.TryGetProperty(name, out var shape) && shape.ValueKind == JsonValueKind.Object
            ? new SectorShapeSpec
            {
                Fill = TextOf(shape, "fill"),
                Stroke = TextOf(shape, "stroke"),
                InnerRadius = NumberOf(shape, "innerRadius"),
                OuterRadius = NumberOf(shape, "outerRadius"),
                CornerRadius = TextOf(shape, "cornerRadius"),
                FillOpacity = NumberOf(shape, "fillOpacity"),
                Opacity = NumberOf(shape, "opacity"),
                StrokeWidth = NumberOf(shape, "strokeWidth"),
            }
            : null;

    private static RadarSpec Radar(JsonElement child, JsonElement props, int order) => new()
    {
        AngleAxisId = TextOf(props, "angleAxisId") ?? "0",
        RadiusAxisId = TextOf(props, "radiusAxisId") ?? "0",
        DotProps = DotOf(props),
        ActiveDot = FlagOf(props, "activeDot") ?? true,
        ActiveDotRadius = props.TryGetProperty("activeDot", out var active) ? NumberOf(active, "r") ?? 4 : 4,
        ActiveDotStyle = active.ValueKind == JsonValueKind.Object && (TextOf(active, "fill") is not null || TextOf(active, "stroke") is not null || NumberOf(active, "strokeWidth") is not null)
            ? new ActiveStyle(TextOf(active, "fill"), TextOf(active, "stroke"), NumberOf(active, "strokeWidth"))
            : null,
        ConnectNulls = FlagOf(props, "connectNulls") ?? false,
        Labels = LabelListsOf(child).First,
        LabelLists = LabelListsOf(child).Others,
        Animation = Animation(props),
        DataKey = TextOf(props, "dataKey") ?? string.Empty,
        Name = TextOf(props, "name"),
        Stroke = TextOf(props, "stroke"),
        Fill = TextOf(props, "fill"),
        FillOpacity = NumberOf(props, "fillOpacity"),
        Dot = FlagOf(props, "dot") ?? DotOf(props) is not null,
        StrokeWidth = NumberOf(props, "strokeWidth"),
        Unit = TextOf(props, "unit"),
        LegendType = Icon(TextOf(props, "legendType")) ?? LegendIcon.Rect,
        Hide = FlagOf(props, "hide") ?? false,
        TooltipNone = TextOf(props, "tooltipType") == "none",
        Order = order,
    };

    private static RadialBarSpec RadialBar(JsonElement child, JsonElement props, int order) => new()
    {
        AngleAxisId = TextOf(props, "angleAxisId") ?? "0",
        RadiusAxisId = TextOf(props, "radiusAxisId") ?? "0",
        ActiveIndex = (int?)NumberOf(props, "activeIndex"),
        Animation = Animation(props),
        DataKey = TextOf(props, "dataKey") ?? string.Empty,
        Name = TextOf(props, "name"),
        NameKey = TextOf(props, "nameKey") ?? "name",
        Stroke = TextOf(props, "stroke"),
        Unit = TextOf(props, "unit"),
        LegendType = Icon(TextOf(props, "legendType")) ?? LegendIcon.Rect,
        Hide = FlagOf(props, "hide") ?? false,
        TooltipNone = TextOf(props, "tooltipType") == "none",
        FillKey = "fill",
        Fill = TextOf(props, "fill"),
        Background = FlagOf(props, "background") == true ? "true" : props.TryGetProperty("background", out var background) && background.ValueKind == JsonValueKind.Object ? TextOf(background, "fill") ?? "true" : null,
        BackgroundProps = props.TryGetProperty("background", out var backdrop) && backdrop.ValueKind == JsonValueKind.Object ? CellOf(backdrop) : null,
        MaxBarSize = NumberOf(props, "maxBarSize"),
        BarSize = NumberOf(props, "barSize"),
        StackId = TextOf(props, "stackId"),
        CornerRadius = TextOf(props, "cornerRadius"),
        ForceCornerRadius = FlagOf(props, "forceCornerRadius") ?? false,
        CornerIsExternal = FlagOf(props, "cornerIsExternal") ?? false,
        MinPointSize = NumberOf(props, "minPointSize") ?? 0,
        Labels = LabelListsOf(child).First,
        LabelLists = LabelListsOf(child).Others,
        Cells = Cells(child),
        Order = order,
    };
}
