namespace Aioros.Pictor.Tests.Updates;

public sealed class UpdateFrameTests
{
    [Theory]
    [Trait("AC", "UPDATE-01")]
    [InlineData("LineValues")]
    [InlineData("LineMore")]
    [InlineData("LineFewer")]
    [InlineData("LineFromEmpty")]
    public async Task LinesMoveFromTheirPreviousPointsFrameForFrameAsRechartsAsync(string name) => await AssertFramesAsync(name, 90);

    [Theory]
    [Trait("AC", "UPDATE-02")]
    [InlineData("AreaValues")]
    [InlineData("StackedAreaMore")]
    public async Task AreasMoveFromTheirPreviousPointsAndBaselineFrameForFrameAsRechartsAsync(string name) => await AssertFramesAsync(name, 90);

    [Theory]
    [Trait("AC", "UPDATE-03")]
    [InlineData("BarMore")]
    [InlineData("VerticalBarMore")]
    public async Task BarsMoveFromTheirPreviousRectanglesAndNewOnesGrowFrameForFrameAsRechartsAsync(string name) => await AssertFramesAsync(name, 22);

    [Fact]
    [Trait("AC", "UPDATE-04")]
    public async Task ScatterSymbolsMoveAndNewOnesGrowFrameForFrameAsRechartsAsync() => await AssertFramesAsync("ScatterMore", 22);

    [Fact]
    [Trait("AC", "UPDATE-05")]
    public async Task PieSectorsSweepFromTheirPreviousAnglesFrameForFrameAsRechartsAsync() => await AssertFramesAsync("PieValues", 90);

    [Fact]
    [Trait("AC", "UPDATE-06")]
    public async Task RadarPointsMoveFromTheirPreviousPointsFrameForFrameAsRechartsAsync() => await AssertFramesAsync("RadarValues", 90);

    [Fact]
    [Trait("AC", "UPDATE-07")]
    public async Task RadialBarsSweepFromTheirPreviousAnglesFrameForFrameAsRechartsAsync() => await AssertFramesAsync("RadialBarValues", 90);

    [Fact]
    [Trait("AC", "UPDATE-08")]
    public async Task FunnelTrapezoidsMoveAndNewOnesGrowFromTheirCentersFrameForFrameAsRechartsAsync() => await AssertFramesAsync("FunnelMore", 90);

    [Fact]
    [Trait("AC", "UPDATE-09")]
    public async Task TreemapNodesKeptByPlaceAndNameResizeFrameForFrameAsRechartsAsync() => await AssertFramesAsync("TreemapValues", 90);

    [Theory]
    [Trait("AC", "ANIMATION-09")]
    [InlineData("PieEnter")]
    [InlineData("StraightPieEnter")]
    public async Task PieSectorsGrowFromNothingFrameForFrameAsRechartsAsync(string name) => await AssertFramesAsync(name, 90);

    [Fact]
    [Trait("AC", "ANIMATION-10")]
    public async Task RadarPointsComeFromTheCenterFrameForFrameAsRechartsAsync() => await AssertFramesAsync("RadarEnter", 90);

    [Fact]
    [Trait("AC", "ANIMATION-11")]
    public async Task RadialBarsSweepFromTheirStartAngleFrameForFrameAsRechartsAsync() => await AssertFramesAsync("RadialBarEnter", 90);

    [Fact]
    [Trait("AC", "UPDATE-26")]
    public async Task TreemapRectanglesAnimatingThemselvesFollowTheirNodesOneFrameLateAsync()
    {
        var (differences, moved) = await UpdateReference.CompareAsync("TreemapRectanglesValues", until: 1584);

        Assert.True(differences.Count == 0, string.Join("\n", differences));
        Assert.InRange(moved, 90, 100);
    }

    private static async Task AssertFramesAsync(string name, int moving)
    {
        var (differences, moved) = await UpdateReference.CompareAsync(name);

        Assert.True(differences.Count == 0, string.Join("\n", differences));
        Assert.InRange(moved, moving, moving + 10);
    }
}
