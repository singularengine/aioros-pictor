using Aioros.Pictor.Painting;
using Aioros.Pictor.Tests.Desktop;

namespace Aioros.Pictor.Tests.Charts;

public sealed class ScatterLineTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "SCATTER-10")]
    public async Task AFittingLineAndLinePropsMatchRechartsAsync()
    {
        var verdict = await Reference.CompareAsync("FittingLineScatterChart");

        Assert.Empty(verdict.Differences);
    }

    [Fact]
    [Trait("AC", "SCATTER-11")]
    public void TheFitCountsMissingCoordinatesAsZeroAndAVerticalSpreadAsFlat()
    {
        var missing = ScatterPainter.Fitting([10, double.NaN, 30], [10, 20, 30]);
        var vertical = ScatterPainter.Fitting([5, 5, 5], [1, 2, 6]);

        Assert.Equal([0.0, 30.0], missing.X);
        Assert.Equal(14.285714285714286, missing.Y[0], 9);
        Assert.Equal(27.142857142857142, missing.Y[1], 9);
        Assert.Equal([5.0, 5.0], vertical.X);
        Assert.Equal([3.0, 3.0], vertical.Y);
    }

    [Fact]
    [Trait("AC", "SCATTER-12")]
    public async Task ScatterLinePropsAreSetInXamlAsync()
    {
        var verdict = await ReferenceDesktop.CompareXamlAsync(desktop, "FittingLineScatterChart");

        Assert.Empty(verdict.Differences);
    }
}
