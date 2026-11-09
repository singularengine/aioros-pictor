using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Interaction;

/// <summary>What a chart's spec asks the web to write for the framework to post: commands run by clicks, a brush's fields, a treemap's zoom.</summary>
internal static class Interactions
{
    /// <summary>Whether the chart has click targets over its surface (any click command but the legend's).</summary>
    public static bool Clicks(ChartSpec spec) =>
        spec.ClickCommand is not null
        || spec.ItemClickCommand is not null
        || spec.Treemap?.ZoomCommand is not null
        || spec.Series.Any(static series => series.ClickCommand is not null || series.HrefKey is not null)
        || spec.XAxes.Any(static axis => axis.ClickCommand is not null)
        || spec.YAxes.Any(static axis => axis.ClickCommand is not null)
        || spec.Funnels.Any(static funnel => funnel.ClickCommand is not null)
        || (spec.Polar is { } polar && (polar.Pies.Any(static pie => pie.ClickCommand is not null) || polar.Radars.Any(static radar => radar.ClickCommand is not null) || polar.RadialBars.Any(static bar => bar.ClickCommand is not null)));

    /// <summary>Whether a series of the chart links its items.</summary>
    public static bool Links(ChartSpec spec) => spec.Series.Any(static series => series.HrefKey is not null);

    /// <summary>Whether the chart writes fields or commands, whose attributes the framework writes at marks of its markup.</summary>
    public static bool Marked(ChartSpec spec) => Clicks(spec) || spec.Legend?.ClickCommand is not null || spec.Brush is { Fields: true };
}
