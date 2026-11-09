using Aioros.Pictor.Demo;
using Aioros.Pictor.Tests.Desktop;
using Avalonia.Media.Imaging;

namespace Aioros.Pictor.Tests.Oracles;

public sealed class PixelOracleTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "ORACLE-03")]
    public async Task ThePixelComparisonCoversBothImagesAndFailsOnVanishedInkAsync()
    {
        var reference = await desktop.Session.Dispatch(
            () =>
            {
                using var bitmap = new Bitmap(Path.Combine(Reference.Root, "tools", "recharts", "out", "SimpleBarChart.png"));
                return DesktopRender.Pixels(bitmap);
            },
            TestContext.Current.CancellationToken);
        var drawn = await ReferenceDesktop.PixelsAsync(desktop, "SimpleBarChart");
        var wider = (drawn.Width + 1, drawn.Height, new byte[(drawn.Width + 1) * drawn.Height * 4]);
        for (var y = 0; y < drawn.Height; y++)
        {
            Array.Copy(drawn.Bgra, y * drawn.Width * 4, wider.Item3, y * (drawn.Width + 1) * 4, drawn.Width * 4);
            Array.Fill(wider.Item3, (byte)255, ((y * (drawn.Width + 1)) + drawn.Width) * 4, 4);
        }

        var unlabeled = (drawn.Width, drawn.Height, (byte[])drawn.Bgra.Clone());
        for (var y = 255; y < 275; y++)
        {
            Array.Fill(unlabeled.Item3, (byte)255, ((y * drawn.Width) + 70) * 4, 420 * 4);
        }

        var same = DesktopRender.Compare("SimpleBarChart", reference, drawn);
        var grown = DesktopRender.Compare("SimpleBarChart", reference, wider);
        var vanished = DesktopRender.Compare("SimpleBarChart", reference, unlabeled);

        PixelVerdict.Within(same);
        Assert.False(grown.SameSize);
        Assert.Equal(same.Strong + drawn.Height, grown.Strong);
        Assert.Equal((drawn.Width + 1) * drawn.Height, grown.Pixels);
        Assert.Throws<Xunit.Sdk.TrueException>(() => PixelVerdict.Within(grown));
        Assert.True(vanished.Strong > same.Strong);
        Assert.Throws<Xunit.Sdk.TrueException>(() => PixelVerdict.Within(vanished));
    }
}
