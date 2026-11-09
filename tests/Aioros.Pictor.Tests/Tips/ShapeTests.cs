using System.Buffers;
using System.Text;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Svg;
using Avalonia;

namespace Aioros.Pictor.Tests.Tips;

public sealed class ShapeTests(Desktop.DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "SHAPES-01")]
    public async Task LineDotsAndActiveDotsTakeTheirPropsAsync()
    {
        Assert.Empty((await Reference.CompareAsync("DotProps")).Differences);
        Assert.Empty(Reference.Differences(await Reference.HoverAsync("DotProps", 0)));
    }

    [Fact]
    [Trait("AC", "SHAPES-02")]
    public async Task AreaDotsTakeTheAreasPaintAsync() => Assert.Empty((await Reference.CompareAsync("AreaDotProps")).Differences);

    [Fact]
    [Trait("AC", "SHAPES-03")]
    public async Task CellsCarryTheirOtherPropsAsync() => Assert.Empty((await Reference.CompareAsync("CellProps")).Differences);

    [Fact]
    [Trait("AC", "SHAPES-04")]
    public async Task GridStripesAndBackgroundMatchRechartsAsync() => Assert.Empty((await Reference.CompareAsync("GridStripes")).Differences);

    [Fact]
    [Trait("AC", "SHAPES-05")]
    public async Task GridPointsAndSyncedLinesMatchRechartsAsync()
    {
        Assert.Empty((await Reference.CompareAsync("GridPoints")).Differences);
        Assert.Empty((await Reference.CompareAsync("GridSyncWithTicks")).Differences);
    }

    [Fact]
    [Trait("AC", "SHAPES-06")]
    public async Task PolarGridPropsAnglesAndRadiiMatchRechartsAsync()
    {
        Assert.Empty((await Reference.CompareAsync("PolarGridProps")).Differences);
        Assert.Empty((await Reference.CompareAsync("PolarGridAngles")).Differences);
    }

    [Fact]
    [Trait("AC", "SHAPES-07")]
    public async Task RadarDotsConnectedNullsAndLabelsMatchRechartsAsync()
    {
        var hover = await Reference.HoverAsync("RadarOptions", 0);

        Assert.Empty((await Reference.CompareAsync("RadarOptions")).Differences);
        Assert.Empty(HoverComparison.Compare(HoverComparison.RechartsMarks(hover.Recharts), HoverComparison.PictorMarks(hover.Pictor, hover.State)));
    }

    [Fact]
    [Trait("AC", "SHAPES-08")]
    public async Task ShapePropsOfXamlElementsBuildTheSpecRechartsPropsGiveAsync()
    {
        foreach (var name in new[] { "DotProps", "CellProps", "GridStripes", "PolarGridProps", "RadarOptions" })
        {
            var example = await Reference.ExampleAsync(name);
            var argument = await desktop.Session.Dispatch(
                () =>
                {
                    var chart = SpecControls.Create(example);
                    chart.Measure(new Size(example.Spec.Width, example.Spec.Height));
                    chart.Arrange(new Rect(0, 0, example.Spec.Width, example.Spec.Height));
                    return chart.WrittenSlot!.Argument;
                },
                TestContext.Current.CancellationToken);
            var buffer = new ArrayBufferWriter<byte>();
            Slots.Writer(argument, static (_, _) => null!)(example.Data, buffer);

            Assert.Equal(Reference.Pictor(example), Encoding.UTF8.GetString(buffer.WrittenSpan));
        }
    }
}
