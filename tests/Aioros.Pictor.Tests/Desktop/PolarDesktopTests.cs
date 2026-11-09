using Aioros.Pictor.Demo;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class PolarDesktopTests(DesktopSession desktop)
{
    [Theory]
    [Trait("AC", "POLAR-11")]
    [InlineData("SimpleRadarChart")]
    [InlineData("TwoLevelPieChart")]
    public async Task TheDesktopPolarChartLooksLikeRechartsAsync(string name)
    {
        var example = await Reference.ExampleAsync(name);
        var output = Path.Combine(Path.GetTempPath(), "pictor-polar-" + Guid.NewGuid().ToString("N") + ".png");

        var result = await desktop.Session.Dispatch(
            () => DesktopRender.Compare(example.Name, SpecControls.Create(example), ReferenceFiles.Of(Reference.Root, name + ".png"), output),
            TestContext.Current.CancellationToken);
        File.Delete(output);

        PixelVerdict.Within(result);
    }

    [Fact]
    [Trait("AC", "POLAR-12")]
    public async Task HoveringADonutSectorShowsItsTooltipAsync()
    {
        var example = await Reference.ExampleAsync("TwoSimplePieChart");

        var (active, outside) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var window = new Window { Width = 800, Height = 400, Content = chart };
                window.Show();
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                window.MouseMove(new Point(445, 205), RawInputModifiers.None);
                var hovered = chart.ActiveIndex;
                window.MouseMove(new Point(795, 395), RawInputModifiers.None);
                var corner = chart.ActiveIndex;
                window.Close();
                return (hovered, corner);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(9, active);
        Assert.True(outside < 0);
    }
}
