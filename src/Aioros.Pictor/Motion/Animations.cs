using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Motion;

/// <summary>The enter animation of every animated part of a chart, with Recharts' defaults per kind.</summary>
internal static class Animations
{
    public static Timing? Of(SeriesSpec series) => Timing.Of(series.Animation, series.Kind switch
    {
        SeriesKind.Bar => Timing.Bar,
        SeriesKind.Scatter => Timing.Scatter,
        _ => Timing.Slow,
    });

    public static Timing? Of(PieSpec pie) => Timing.Of(pie.Animation, Timing.Delayed);

    public static Timing? Of(RadarSpec radar) => Timing.Of(radar.Animation, Timing.Slow);

    public static Timing? Of(RadialBarSpec bar) => Timing.Of(bar.Animation, Timing.Slow) is { } timing ? timing with { Lag = Timing.JavascriptLag } : null;

    public static Timing? Of(FunnelSpec funnel) => Timing.Of(funnel.Animation, Timing.Delayed);

    public static Timing? Of(TreemapSpec treemap) => Timing.Of(treemap.Animation, Timing.Treemap);

    /// <summary>A laid-out series' animation: none while it stays where it is during an update animation.</summary>
    public static Timing? Of(SeriesLayout series) => series.Still ? null : Of(series.Spec);

    public static Timing? Of(PieLayout pie) => pie.Still ? null : Of(pie.Spec);

    public static Timing? Of(RadarLayout radar) => radar.Still ? null : Of(radar.Spec);

    public static Timing? Of(RadialBarLayout bar) => bar.Still ? null : Of(bar.Spec);

    public static Timing? Of(FunnelSeries funnel) => funnel.Still ? null : Of(funnel.Spec);

    /// <summary>When the chart's last animation ends, in milliseconds after it appears; 0 when nothing animates.</summary>
    public static double End(ChartSpec spec)
    {
        var end = 0.0;
        foreach (var series in spec.Series)
        {
            end = Later(end, series.Hide ? null : Of(series));
        }

        if (spec.Polar is { } polar)
        {
            end = polar.Pies.Aggregate(end, (last, pie) => Later(last, Of(pie)));
            end = polar.Radars.Aggregate(end, (last, radar) => Later(last, Of(radar)));
            end = polar.RadialBars.Aggregate(end, (last, bar) => Later(last, Of(bar)));
        }

        end = spec.Funnels.Aggregate(end, (last, funnel) => Later(last, funnel.Hide ? null : Of(funnel)));
        return spec.Treemap is { } treemap ? Math.Max(Later(end, Of(treemap)), Strokes(treemap)) : end;
    }

    /// <summary>When a treemap's rectangles have drawn their strokes in, when they do (its <c>isUpdateAnimationActive</c> off): after its deeper nodes', which mount at its end.</summary>
    private static double Strokes(TreemapSpec treemap) =>
        !treemap.UpdateAnimation && treemap.Content is null && Of(treemap) is { } timing ? timing.StrokeStart(late: true) + Timing.StrokeDuration : 0;

    private static double Later(double end, Timing? timing) => timing is { } value ? Math.Max(end, value.End) : end;
}
