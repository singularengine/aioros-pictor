using System.Buffers;
using System.Text;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Svg;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Tips;

public sealed class ResponsiveContainerTests(Desktop.DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "RESPONSIVE-01")]
    public async Task AnAspectRatioGivesTheHeightAsRechartsDoesAsync()
    {
        var (height, markup) = await LayOutAsync("ResponsiveAspect", 2, null, 0, 0);

        Assert.Equal(250, height);
        Assert.Empty(MarkupComparison.Compare(await Reference.RechartsAsync("ResponsiveAspect"), markup, MarkupDivergence.ResponsiveSurface).Differences);
        Assert.Contains("style=\"position:relative;cursor:default;width:100%;aspect-ratio:2;font-size:14px\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "RESPONSIVE-02")]
    public async Task TheMaximumHeightCapsTheAspectAndTheLimitsAreWrittenAsync()
    {
        var (height, markup) = await LayOutAsync("ResponsiveMaxHeight", 1, 300, 200, 120);

        Assert.Equal(300, height);
        Assert.Empty(MarkupComparison.Compare(await Reference.RechartsAsync("ResponsiveMaxHeight"), markup, MarkupDivergence.ResponsiveSurface).Differences);
        Assert.Contains("width:100%;aspect-ratio:1;min-width:200px;min-height:120px;max-height:300px", markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "RESPONSIVE-03")]
    public async Task AChartWithOnlyAHeightFillsItsParentsWidthAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        Task<string> WrittenAsync(string? widths) => desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                chart.Width = double.NaN;
                chart.Height = 260;
                chart.Aspect = double.NaN;
                chart.ResponsiveWidths = widths;
                var window = new Window { Width = 500, Height = 600, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var argument = chart.WrittenSlot!.Argument;
                window.Close();
                var buffer = new ArrayBufferWriter<byte>();
                Slots.Writer(argument, static (_, _) => null!)(example.Data, buffer);
                return Encoding.UTF8.GetString(buffer.WrittenSpan);
            },
            TestContext.Current.CancellationToken);

        var filling = await WrittenAsync(null);

        Assert.Contains("width:100%;aspect-ratio:1.923", filling, StringComparison.Ordinal);
        Assert.DoesNotContain("cursor:default;width:100%;height:100%", filling, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "RESPONSIVE-05")]
    public async Task EachLayoutOfAFixedHeightFillsTheWidthAtItsOwnProportionAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var markup = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                chart.Width = double.NaN;
                chart.Height = 260;
                chart.Aspect = double.NaN;
                chart.ResponsiveWidths = "360,720";
                var window = new Window { Width = 500, Height = 600, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var argument = chart.WrittenSlot!.Argument;
                window.Close();
                var buffer = new ArrayBufferWriter<byte>();
                Slots.Writer(argument, static (_, _) => null!)(example.Data, buffer);
                return Encoding.UTF8.GetString(buffer.WrittenSpan);
            },
            TestContext.Current.CancellationToken);

        Assert.Contains("viewBox=\"0 0 360 260\"", markup, StringComparison.Ordinal);
        Assert.Contains("viewBox=\"0 0 720 260\"", markup, StringComparison.Ordinal);
        Assert.Contains("width:100%;aspect-ratio:1.385", markup, StringComparison.Ordinal);
        Assert.Contains("width:100%;aspect-ratio:2.769", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("height:260px", markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "RESPONSIVE-04")]
    public async Task AMinimumHeightKeepsANarrowLayoutReadableAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var (height, markup) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                chart.Width = double.NaN;
                chart.Height = double.NaN;
                chart.Aspect = 3;
                chart.MinHeight = 200;
                chart.ResponsiveWidths = "320,640";
                chart.VerticalAlignment = VerticalAlignment.Top;
                var window = new Window { Width = 320, Height = 600, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var result = (chart.Bounds.Height, chart.WrittenSlot!.Argument);
                window.Close();
                var buffer = new ArrayBufferWriter<byte>();
                Slots.Writer(result.Argument, static (_, _) => null!)(example.Data, buffer);
                return (result.Height, Encoding.UTF8.GetString(buffer.WrittenSpan));
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(200, height);
        Assert.Contains("viewBox=\"0 0 320 200\"", markup, StringComparison.Ordinal);
        Assert.Contains("viewBox=\"0 0 640 213.33", markup, StringComparison.Ordinal);
    }

    private async Task<(double Height, string Markup)> LayOutAsync(string name, double aspect, double? maxHeight, double minWidth, double minHeight)
    {
        var example = await Reference.ExampleAsync(name);
        var (height, argument) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                chart.Width = double.NaN;
                chart.Height = double.NaN;
                chart.Aspect = aspect;
                chart.MaxHeight = maxHeight ?? double.PositiveInfinity;
                chart.MinWidth = minWidth;
                chart.MinHeight = minHeight;
                chart.VerticalAlignment = VerticalAlignment.Top;
                var window = new Window { Width = 500, Height = 600, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var result = (chart.Bounds.Height, chart.WrittenSlot!.Argument);
                window.Close();
                return result;
            },
            TestContext.Current.CancellationToken);
        var buffer = new ArrayBufferWriter<byte>();
        Slots.Writer(argument, static (_, _) => null!)(example.Data, buffer);
        return (height, Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}
