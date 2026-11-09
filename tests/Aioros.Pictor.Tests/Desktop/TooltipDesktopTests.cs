using System.Runtime.InteropServices;
using Aioros.Pictor.Demo;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class TooltipDesktopTests(DesktopSession desktop)
{
    private static readonly Point Outside = new(550, 350);

    [Fact]
    [Trait("AC", "TOOLTIP-16")]
    public async Task DefaultClickAndActiveTooltipsFollowThePointerOnTheDesktopAsync()
    {
        var examples = (
            Default: await Reference.ExampleAsync("TooltipDefaultIndex"),
            Click: await Reference.ExampleAsync("TooltipClick"),
            Inactive: await Reference.ExampleAsync("TooltipActiveFalse"),
            Plain: await Reference.ExampleAsync("TooltipSharedLineChart"));

        var result = await desktop.Session.Dispatch(
            () =>
            {
                var initial = Run(examples.Default, (window, chart) =>
                {
                    var before = chart.ActiveIndex;
                    window.MouseMove(new Point(405, 5), RawInputModifiers.None);
                    var moved = chart.ActiveIndex;
                    window.MouseMove(Outside, RawInputModifiers.None);
                    return (before, moved, chart.ActiveIndex);
                });
                var click = Run(examples.Click, (window, chart) =>
                {
                    window.MouseMove(new Point(205, 5), RawInputModifiers.None);
                    var hovered = chart.ActiveIndex;
                    window.MouseDown(new Point(205, 5), MouseButton.Left, RawInputModifiers.None);
                    window.MouseUp(new Point(205, 5), MouseButton.Left, RawInputModifiers.None);
                    var clicked = chart.ActiveIndex;
                    window.MouseMove(new Point(405, 5), RawInputModifiers.None);
                    var after = chart.ActiveIndex;
                    window.MouseMove(Outside, RawInputModifiers.None);
                    return (hovered, clicked, after, chart.ActiveIndex);
                });
                var boxes = (
                    Shown: Run(examples.Plain, (window, chart) => Hovered(window, chart)),
                    Inactive: Run(examples.Inactive, (window, chart) => Hovered(window, chart)));
                var kept = Run(examples.Plain, (window, chart) =>
                {
                    chart.Elements.OfType<Tooltip>().Single().Active = true;
                    window.MouseMove(new Point(210, 5), RawInputModifiers.None);
                    window.MouseMove(Outside, RawInputModifiers.None);
                    return chart.ActiveIndex;
                });
                return (initial, click, boxes, kept);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal((3, 5, -2), result.initial);
        Assert.True(result.click.hovered < 0);
        Assert.Equal((2, 2, 2), (result.click.clicked, result.click.after, result.click.Item4));
        Assert.Equal(((0xcc, 0xcc, 0xcc), true), result.boxes.Shown);
        Assert.Equal(((0xff, 0xff, 0xff), false), result.boxes.Inactive);
        Assert.Equal(2, result.kept);
    }

    /// <summary>The pixel where a box shown for the hovered category has its border (over a blank part of the plot), and whether text was written where its lines go.</summary>
    private static ((int R, int G, int B) Border, bool Text) Hovered(Window window, Chart chart)
    {
        var before = HoverPixels.Frame(chart);
        window.MouseMove(new Point(210, 5), RawInputModifiers.None);
        var pixels = new HoverPixels(before, HoverPixels.Frame(chart), (int)chart.Bounds.Width);
        var border = Avalonia.Media.Color.Parse(pixels.At(220, 50));
        return ((border.R, border.G, border.B), pixels.Written(222, 17, 320, 80) > 100);
    }

    internal static T Run<T>(RechartsExample example, Func<Window, Chart, T> act)
    {
        var chart = SpecControls.Create(example);
        chart.HorizontalAlignment = HorizontalAlignment.Left;
        chart.VerticalAlignment = VerticalAlignment.Top;
        var window = new Window { Width = Math.Max(600, example.Spec.Width + 100), Height = Math.Max(400, example.Spec.Height + 100), Content = chart };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var result = act(window, chart);
        window.Close();
        return result;
    }

    internal static (int R, int G, int B) Pixel(Control chart, int x, int y)
    {
        Dispatcher.UIThread.RunJobs();
        using var target = new RenderTargetBitmap(new PixelSize(500, 300));
        target.Render(chart);
        var bytes = new byte[500 * 300 * 4];
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            target.CopyPixels(new PixelRect(0, 0, 500, 300), handle.AddrOfPinnedObject(), bytes.Length, 500 * 4);
        }
        finally
        {
            handle.Free();
        }

        var i = ((y * 500) + x) * 4;
        return (bytes[i + 2], bytes[i + 1], bytes[i]);
    }
}
