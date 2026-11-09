using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Demo.ExampleJson;

namespace Aioros.Pictor.Demo;

/// <summary>Reads Recharts' examples of the charts with their own layouts: <c>FunnelChart</c>, <c>Treemap</c>, <c>Sankey</c> and <c>SunburstChart</c>.</summary>
internal static class StandaloneExamples
{
    public static ChartSpec Read(ChartSpec spec, string? kind, JsonElement root)
    {
        var props = root.TryGetProperty("props", out var value) ? value : default;
        spec = kind switch
        {
            "FunnelChart" => spec with { Kind = ChartKind.Funnel },
            "Treemap" => spec with { Kind = ChartKind.Treemap, Treemap = Treemap(props) },
            "SunburstChart" => spec with { Kind = ChartKind.Sunburst, Sunburst = Sunburst(props) },
            _ => spec with { Kind = ChartKind.Sankey, Sankey = Sankey(props) },
        };
        foreach (var child in root.GetProperty("children").EnumerateArray())
        {
            var childProps = child.GetProperty("props");
            spec = child.GetProperty("type").GetString() switch
            {
                "Tooltip" => spec with { Tooltip = Tooltip(child.GetProperty("props")) },
                "Legend" => spec with { Legend = Legend(childProps) },
                "Funnel" => spec with { Funnels = spec.Funnels.Add(Funnel(child, childProps)) },
                _ => spec,
            };
        }

        return spec;
    }

    private static FunnelSpec Funnel(JsonElement child, JsonElement props)
    {
        var funnel = new FunnelSpec
        {
            Animation = Animation(props),
            DataKey = TextOf(props, "dataKey") ?? "value",
            NameKey = TextOf(props, "nameKey") ?? "name",
            Name = TextOf(props, "name"),
            Fill = TextOf(props, "fill"),
            Stroke = TextOf(props, "stroke"),
            Reversed = FlagOf(props, "reversed") ?? false,
            LastRectangle = TextOf(props, "lastShapeType") == "rectangle",
            LegendType = Icon(TextOf(props, "legendType")) ?? LegendIcon.Rect,
            Hide = FlagOf(props, "hide") ?? false,
            TooltipNone = TextOf(props, "tooltipType") == "none",
            Width = TextOf(props, "width"),
            ActiveIndex = (int?)NumberOf(props, "activeIndex"),
            ActiveIndexes = props.TryGetProperty("activeIndex", out var active) && active.ValueKind == JsonValueKind.Array ? [.. active.EnumerateArray().Select(item => item.GetInt32())] : [],
            FillKey = "fill",
            StrokeKey = "stroke",
            Cells = [.. Children(child, "Cell").Select(cell => CellOf(cell.GetProperty("props")))],
            Labels = LabelListsOf(child, implicitLabel: false).First,
            LabelLists = LabelListsOf(child, implicitLabel: false).Others,
        };
        if (!props.TryGetProperty("data", out var data))
        {
            return funnel;
        }

        string?[] keys = [funnel.DataKey, funnel.NameKey, "fill", "stroke", funnel.Labels?.DataKey, .. funnel.LabelLists.Select(label => label.DataKey)];
        return funnel with { Data = DataTable.Read(Value(data) as IEnumerable, keys, static (_, _) => null) };
    }

    private static TreemapSpec Treemap(JsonElement props) => new()
    {
        Animation = Animation(props),
        UpdateAnimation = FlagOf(props, "isUpdateAnimationActive") ?? true,
        DataKey = TextOf(props, "dataKey") ?? "value",
        NameKey = TextOf(props, "nameKey"),
        AspectRatio = NumberOf(props, "aspectRatio") ?? 0.5 * (1 + Math.Sqrt(5)),
        Nest = TextOf(props, "type") == "nest",
        Fill = TextOf(props, "fill"),
        Stroke = TextOf(props, "stroke"),
        ColorPanel = props.ValueKind == JsonValueKind.Object && props.TryGetProperty("colorPanel", out var panel)
            ? [.. panel.EnumerateArray().Select(color => color.GetString()!)]
            : ImmutableArray<string>.Empty,
    };

    private static SunburstSpec Sunburst(JsonElement props)
    {
        var text = props.ValueKind == JsonValueKind.Object && props.TryGetProperty("textOptions", out var options) ? options : default;
        return new SunburstSpec
        {
            DataKey = TextOf(props, "dataKey") ?? "value",
            FillKey = "fill",
            Padding = NumberOf(props, "padding") ?? 2,
            RingPadding = NumberOf(props, "ringPadding") ?? 2,
            InnerRadius = NumberOf(props, "innerRadius") ?? 50,
            OuterRadius = NumberOf(props, "outerRadius"),
            Cx = NumberOf(props, "cx"),
            Cy = NumberOf(props, "cy"),
            StartAngle = NumberOf(props, "startAngle") ?? 0,
            EndAngle = NumberOf(props, "endAngle") ?? 360,
            Fill = TextOf(props, "fill"),
            Stroke = TextOf(props, "stroke"),
            TextOptions = text.ValueKind != JsonValueKind.Object ? TextOptionsSpec.Default : new TextOptionsSpec
            {
                FontFamily = TextOf(text, "fontFamily"),
                FontWeight = TextOf(text, "fontWeight"),
                PaintOrder = TextOf(text, "paintOrder"),
                FontSize = TextOf(text, "fontSize"),
                Stroke = TextOf(text, "stroke"),
                Fill = TextOf(text, "fill"),
                PointerEvents = TextOf(text, "pointerEvents"),
            },
        };
    }

    private static SankeySpec Sankey(JsonElement props)
    {
        var node = props.ValueKind == JsonValueKind.Object && props.TryGetProperty("node", out var nodeProps) ? nodeProps : default;
        var link = props.ValueKind == JsonValueKind.Object && props.TryGetProperty("link", out var linkProps) ? linkProps : default;
        return new SankeySpec
        {
            NameKey = TextOf(props, "nameKey") ?? "name",
            NodePadding = NumberOf(props, "nodePadding") ?? 10,
            NodeWidth = NumberOf(props, "nodeWidth") ?? 10,
            LinkCurvature = NumberOf(props, "linkCurvature") ?? 0.5,
            Iterations = (int)(NumberOf(props, "iterations") ?? 32),
            Sort = FlagOf(props, "sort") ?? true,
            NodeFill = TextOf(node, "fill"),
            NodeFillOpacity = NumberOf(node, "fillOpacity") ?? 0.8,
            NodeStroke = TextOf(node, "stroke"),
            NodeStrokeWidth = NumberOf(node, "strokeWidth"),
            NodeStrokeOpacity = NumberOf(node, "strokeOpacity"),
            NodeStrokeDashArray = TextOf(node, "strokeDasharray"),
            LinkStrokeWidth = NumberOf(link, "strokeWidth"),
            LinkFill = TextOf(link, "fill"),
            LinkStrokeDashArray = TextOf(link, "strokeDasharray"),
            LinkStroke = TextOf(link, "stroke"),
            LinkStrokeOpacity = NumberOf(link, "strokeOpacity") ?? 0.2,
        };
    }
}
