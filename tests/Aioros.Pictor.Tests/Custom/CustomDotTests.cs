using Aioros.Pictor.Tests.Desktop;
using Avalonia.Headless;

namespace Aioros.Pictor.Tests.Custom;

public sealed class CustomDotTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "CUSTOM-03")]
    public async Task ADotTemplateChoosingAnIconByValueIsWrittenAsRechartsDrawsItAsync()
    {
        var verdict = await CustomReference.CompareAsync(desktop, "CustomizedDotLineChart", static (chart, templates) => chart.Elements.OfType<Line>().First().DotTemplate = templates["CustomizedDot"]);

        Assert.True(verdict.Matches, string.Join("\n", verdict.Differences));
    }

    internal static void ActiveDots(Chart chart, CustomTemplates templates)
    {
        chart.Elements.OfType<Area>().Single().ActiveDotTemplate = templates["ActiveSquare"];
        chart.Elements.OfType<Line>().Single().ActiveDotTemplate = templates["ActiveRing"];
    }

    [Fact]
    [Trait("AC", "CUSTOM-23")]
    public async Task ActiveDotTemplatesOfAnAreaAndALineAreWrittenAsRechartsDrawsThemAsync()
    {
        var differences = await CustomReference.ActiveDifferencesAsync(desktop, "CustomActiveDotChart", ActiveDots, 0, "recharts-active-dot", all: true);
        var example = await Reference.ExampleAsync("CustomActiveDotChart");
        var ring = await desktop.Session.Dispatch(() => Aioros.Pictor.Tests.Desktop.TooltipDesktopTests.Run(example, (window, chart) =>
        {
            ActiveDots(chart, new CustomTemplates());
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            var before = Ink(chart);
            window.MouseMove(new Avalonia.Point(212, 100), Avalonia.Input.RawInputModifiers.None);
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            return Ink(chart) - before;
        }), TestContext.Current.CancellationToken);

        Assert.Empty(differences);
        Assert.True(ring > 60, $"{ring} more pixels of the line's color around the hovered point on the desktop");
    }

    /// <summary>How many pixels of the line's color (#8884d8) the desktop draws in the ring's box around Page C's point.</summary>
    private static int Ink(Chart chart)
    {
        using var target = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize(500, 300));
        target.Render(chart);
        var pixels = Aioros.Pictor.Demo.DesktopRender.Pixels(target);
        var count = 0;
        for (var y = 12; y < 38; y++)
        {
            for (var x = 197; x < 223; x++)
            {
                var i = ((y * pixels.Width) + x) * 4;
                count += Math.Abs(pixels.Bgra[i] - 0xd8) < 40 && Math.Abs(pixels.Bgra[i + 1] - 0x84) < 40 && Math.Abs(pixels.Bgra[i + 2] - 0x88) < 40 ? 1 : 0;
            }
        }

        return count;
    }
}
