using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Motion;
using Aioros.Pictor.Painting;

namespace Aioros.Pictor.Tests.Details;

public sealed partial class AnimateNewValuesTests
{
    [Fact]
    [Trait("AC", "DETAIL-09")]
    public async Task ALinesNewPointsStandAtTheirPlaceWithoutAnimateNewValuesAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");
        var spec = ExampleAnimations.Animated(example.Spec);
        spec = spec with { Series = [spec.Series[0] with { AnimateNewValues = false }, spec.Series[1]] };
        var rows = Reference.Rows(example);
        var fewer = Plots.Compute(spec, Pictor.Data.ChartData.From(rows.Take(0).ToList(), static (_, _) => null));
        var more = (ChartLayout)Plots.Compute(spec, Pictor.Data.ChartData.From(rows, static (_, _) => null));

        var frame = (ChartLayout)Morph.Plan(fewer, more)!.Frame(0);

        Assert.Equal(more.Series[0].X[6], frame.Series[0].X[6], 3);
        Assert.Equal(more.Series[0].Y[6], frame.Series[0].Y[6], 3);
        Assert.Equal(more.PlotWidth * 2, frame.Series[1].X[6], 3);
        var markup = Reference.Pictor(example with { Spec = spec });
        var curves = Curve().Matches(markup).Select(match => match.Value).ToList();
        Assert.DoesNotContain("data-aioros-morph-from", curves[0], StringComparison.Ordinal);
        Assert.Contains("data-aioros-morph-from=\"780 121.5\"", curves[1], StringComparison.Ordinal);
    }

    [GeneratedRegex("<path[^>]*recharts-line-curve[^>]*>")]
    private static partial Regex Curve();
}
