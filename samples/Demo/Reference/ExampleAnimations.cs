using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Demo;

/// <summary>An example with every animated part given Recharts' default animation (the references switch it off).</summary>
internal static class ExampleAnimations
{
    public static ChartSpec Animated(ChartSpec spec) => spec with
    {
        Series = [.. spec.Series.Select(series => series with { Animation = null })],
        Funnels = [.. spec.Funnels.Select(funnel => funnel with { Animation = null })],
        Treemap = spec.Treemap is { } treemap ? treemap with { Animation = null } : null,
        Polar = spec.Polar is { } polar ? polar with
        {
            Pies = [.. polar.Pies.Select(pie => pie with { Animation = null })],
            Radars = [.. polar.Radars.Select(radar => radar with { Animation = null })],
            RadialBars = [.. polar.RadialBars.Select(bar => bar with { Animation = null })],
        } : null,
    };
}
