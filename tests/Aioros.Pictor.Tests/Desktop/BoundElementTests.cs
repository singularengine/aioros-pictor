using System.Collections;
using Avalonia;
using Avalonia.Data;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class BoundElementTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "DESKTOP-04")]
    public async Task ABoundSeriesColorIsFollowedOnTheDesktopAsync()
    {
        var (before, after) = await desktop.Session.Dispatch(
            () =>
            {
                var model = new BoundChartModel();
                var view = new BoundChartView { DataContext = model };
                var window = new Avalonia.Controls.Window { Width = 500, Height = 300, Content = view };
                window.Show();
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                var chart = view.LineChart;
                var first = chart.Plot().Spec.Series[0].Stroke;
                model.Color = "#ff0000";
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                var second = chart.Plot().Spec.Series[0].Stroke;
                window.Close();
                return (first, second);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("#8884d8", before);
        Assert.Equal("#ff0000", after);
    }
}
