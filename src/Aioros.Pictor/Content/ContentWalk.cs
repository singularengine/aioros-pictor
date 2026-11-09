using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Content;

/// <summary>Every template of custom content a chart's spec holds.</summary>
internal static class ContentWalk
{
    public static IEnumerable<ContentSpec> All(ChartSpec spec) =>
        spec.Series.SelectMany(static series => (ContentSpec?[])[series.ShapeContent, series.ActiveShapeContent, series.DotContent, series.ActiveDotContent, .. Labels(series.Labels, series.LabelLists)])
            .Concat(spec.XAxes.Concat(spec.YAxes).SelectMany(static axis => (ContentSpec?[])[axis.TickContent, .. Labels(axis.Label, axis.Labels)]))
            .Concat((spec.Polar?.Pies ?? []).SelectMany(static pie => (ContentSpec?[])[pie.ShapeContent, pie.ActiveShapeContent, .. Labels(null, pie.LabelLists)]))
            .Concat((spec.Polar?.RadialBars ?? []).SelectMany(static bar => (ContentSpec?[])[bar.ShapeContent, bar.ActiveShapeContent, .. Labels(bar.Labels, bar.LabelLists)]))
            .Concat((spec.Polar?.Radars ?? []).SelectMany(static radar => (ContentSpec?[])[radar.ShapeContent, .. Labels(radar.Labels, radar.LabelLists)]))
            .Concat(spec.Funnels.SelectMany(static funnel => (ContentSpec?[])[funnel.ShapeContent, funnel.ActiveShapeContent, .. Labels(funnel.Labels, funnel.LabelLists)]))
            .Concat(spec.ReferenceLines.SelectMany(static line => (ContentSpec?[])[line.ShapeContent, .. Labels(line.Label, line.Labels)]))
            .Concat(spec.ReferenceAreas.SelectMany(static area => (ContentSpec?[])[area.ShapeContent, .. Labels(area.Label, area.Labels)]))
            .Concat(spec.ReferenceDots.SelectMany(static dot => (ContentSpec?[])[dot.ShapeContent, .. Labels(dot.Label, dot.Labels)]))
            .Concat(spec.Customized.Select(static customized => customized.Content))
            .Concat((ContentSpec?[])[spec.Treemap?.Content, spec.Treemap?.NestIndexContent, spec.Sankey?.NodeContent, spec.Sankey?.LinkContent, spec.Brush?.TravellerContent, spec.Tooltip?.Content, spec.Legend?.Content])
            .OfType<ContentSpec>();

    private static IEnumerable<ContentSpec?> Labels(LabelSpec? first, System.Collections.Immutable.ImmutableArray<LabelSpec> others) =>
        others.IsDefault ? [first?.Content] : [first?.Content, .. others.Select(static label => label.Content)];

    /// <summary>The templates the compiler refused, with their reasons.</summary>
    public static IEnumerable<ContentSpec> Refused(ChartSpec spec) => All(spec).Where(content => content.Refused is not null);
}
