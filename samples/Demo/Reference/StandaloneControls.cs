using System.Globalization;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Demo;

/// <summary>Builds the controls of a funnel, treemap, Sankey or sunburst example, as its Gallery page declares them.</summary>
internal static class StandaloneControls
{
    public static Chart Create(RechartsExample example)
    {
        var spec = example.Spec;
        Chart chart = spec.Kind switch
        {
            ChartKind.Funnel => Funnel(spec),
            ChartKind.Treemap => Treemap(spec.Treemap!),
            ChartKind.Sunburst => Sunburst(spec.Sunburst!),
            _ => Sankey(spec.Sankey!),
        };
        SpecControls.Sized(chart, example);
        if (spec.Legend is not null)
        {
            chart.Elements.Add(SpecControls.LegendOf(spec.Legend));
        }

        if (spec.Tooltip is not null)
        {
            chart.Elements.Add(SpecControls.TooltipOf(spec.Tooltip));
        }

        return chart;
    }

    private static FunnelChart Funnel(ChartSpec spec)
    {
        var chart = new FunnelChart();
        foreach (var funnel in spec.Funnels)
        {
            var element = new Funnel
            {
                DataKey = funnel.DataKey,
                NameKey = funnel.NameKey,
                FillKey = funnel.FillKey,
                StrokeKey = funnel.StrokeKey,
                Name = funnel.Name,
                Fill = funnel.Fill,
                Stroke = funnel.Stroke,
                Reversed = funnel.Reversed,
                LastShapeType = funnel.LastRectangle ? "rectangle" : "triangle",
                LegendType = funnel.LegendType,
                Hide = funnel.Hide,
                TooltipType = funnel.TooltipNone ? "none" : null,
                Width = funnel.Width,
                ActiveIndex = funnel.ActiveIndex,
                ActiveIndexes = SpecControls.Join(funnel.ActiveIndexes),
                Data = funnel.Data is { } table ? Rows(table) : null,
            };
            SpecControls.Animated(element, funnel.Animation);
            SpecControls.AddLabelLists(element.Parts, funnel.Labels, funnel.LabelLists);

            foreach (var cell in funnel.Cells)
            {
                element.Parts.Add(SpecControls.CellOf(cell));
            }

            chart.Elements.Add(element);
        }

        return chart;
    }

    private static Treemap Treemap(TreemapSpec spec) => new()
    {
        IsAnimationActive = spec.Animation?.Active ?? true,
        IsUpdateAnimationActive = spec.UpdateAnimation,
        DataKey = spec.DataKey,
        NameKey = spec.NameKey,
        AspectRatio = spec.AspectRatio,
        Type = spec.Nest ? "nest" : "flat",
        Fill = spec.Fill,
        Stroke = spec.Stroke,
        ColorPanel = spec.ColorPanel.IsDefaultOrEmpty ? null : string.Join(',', spec.ColorPanel),
    };

    private static SunburstChart Sunburst(SunburstSpec spec) => new()
    {
        DataKey = spec.DataKey,
        NameKey = spec.NameKey,
        FillKey = spec.FillKey,
        SectorPadding = spec.Padding,
        RingPadding = spec.RingPadding,
        InnerRadius = spec.InnerRadius,
        OuterRadius = spec.OuterRadius,
        Cx = spec.Cx,
        Cy = spec.Cy,
        StartAngle = spec.StartAngle,
        EndAngle = spec.EndAngle,
        Fill = spec.Fill,
        Stroke = spec.Stroke,
        TextFontFamily = spec.TextOptions.FontFamily,
        TextFontWeight = spec.TextOptions.FontWeight,
        TextPaintOrder = spec.TextOptions.PaintOrder,
        TextFontSize = spec.TextOptions.FontSize,
        TextStroke = spec.TextOptions.Stroke,
        TextFill = spec.TextOptions.Fill,
        TextPointerEvents = spec.TextOptions.PointerEvents,
    };

    private static Sankey Sankey(SankeySpec spec) => new()
    {
        NameKey = spec.NameKey,
        NodePadding = spec.NodePadding,
        NodeWidth = spec.NodeWidth,
        LinkCurvature = spec.LinkCurvature,
        Iterations = spec.Iterations,
        Sort = spec.Sort,
        NodeFill = spec.NodeFill,
        NodeFillOpacity = spec.NodeFillOpacity,
        NodeStroke = spec.NodeStroke,
        NodeStrokeWidth = spec.NodeStrokeWidth,
        NodeStrokeOpacity = spec.NodeStrokeOpacity,
        NodeStrokeDashArray = spec.NodeStrokeDashArray,
        LinkStrokeWidth = spec.LinkStrokeWidth,
        LinkFill = spec.LinkFill,
        LinkStrokeDashArray = spec.LinkStrokeDashArray,
        LinkStroke = spec.LinkStroke,
        LinkStrokeOpacity = spec.LinkStrokeOpacity,
    };

    /// <summary>The items a table was read from, as dictionaries: numbers as numbers, the rest as texts.</summary>
    public static List<IReadOnlyDictionary<string, object?>> Rows(DataTable table) =>
        [.. Enumerable.Range(0, table.Count).Select(i => (IReadOnlyDictionary<string, object?>)table.Columns.ToDictionary(
            column => column.Key,
            column => column.Value[i] is { } text && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : (object?)column.Value[i],
            StringComparer.Ordinal))];
}
