using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class InteractionDesktopTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "LEGEND-02")]
    public async Task ClickingALegendItemTogglesItsSeriesOnTheDesktopAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");

        var result = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                chart.Elements.OfType<Legend>().Single().Toggles = true;
                var window = new Window { Width = 500, Height = 300, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var layout = (ChartLayout)chart.Plot();
                var item = layout.Legend!.Items[1];
                var domain = layout.YAxes[0].DomainMax;
                window.MouseMove(new Point(212, 120), RawInputModifiers.None);
                var before = Green(chart);
                Click(window, new Point(item.X + 5, item.Y + (layout.Legend.RowHeight / 2)));
                window.MouseMove(new Point(212, 120), RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
                var hidden = (ChartLayout)chart.Plot();
                var after = Green(chart);
                var legendColor = hidden.Legend!.Items[1].Color;
                var concealed = chart.Concealed;
                Click(window, new Point(5, 5));
                var beside = chart.Concealed;
                Click(window, new Point(item.X + 5, item.Y + (layout.Legend.RowHeight / 2)));
                window.MouseMove(new Point(212, 120), RawInputModifiers.None);
                var back = Green(chart);
                window.Close();
                return (before, after, back, legendColor, concealed, beside, domain, hidden.YAxes[0].DomainMax);
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.before > 100, $"{result.before} pixels of the uv line before");
        Assert.Equal(0, result.after);
        Assert.Equal(result.before, result.back);
        Assert.Equal("#ccc", result.legendColor);
        Assert.Equal([1], result.concealed);
        Assert.Equal([1], result.beside);
        Assert.Equal(result.domain, result.Item8);
    }

    [Fact]
    [Trait("AC", "BRUSH-04")]
    public async Task DraggingTheBrushMovesTheWindowOnTheDesktopAsync()
    {
        var example = await Reference.ExampleAsync("BrushWindowBarChart");

        var result = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var window = new Window { Width = 500, Height = 300, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var brush = ((ChartLayout)chart.Plot()).Brush!;
                var step = (brush.Width - brush.Spec.TravellerWidth) / (brush.Count - 1);
                var y = brush.Y + (brush.Height / 2);
                Drag(window, brush.EndX + 2, brush.EndX + 2 - (2.5 * step), y);
                var element = chart.Elements.OfType<Brush>().Single();
                var afterEnd = (element.StartIndex, element.EndIndex, ((ChartLayout)chart.Plot()).Data.Count);
                var moved = ((ChartLayout)chart.Plot()).Brush!;
                var middle = (moved.StartX + moved.EndX) / 2;
                Drag(window, middle, middle + (2.5 * step), y);
                var afterSlide = (element.StartIndex, element.EndIndex);
                Drag(window, middle + (2.5 * step), middle + (100 * step), y);
                var clamped = (element.StartIndex, element.EndIndex);
                window.Close();
                return (afterEnd, afterSlide, clamped);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal((5, 12, 8), result.afterEnd);
        Assert.Equal((7, 14), result.afterSlide);
        Assert.Equal((31, 38), result.clamped);
    }

    private static void Click(Window window, Point point)
    {
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Drag(Window window, double from, double to, double y)
    {
        window.MouseDown(new Point(from, y), MouseButton.Left, RawInputModifiers.None);
        window.MouseMove(new Point((from + to) / 2, y), RawInputModifiers.LeftMouseButton);
        window.MouseMove(new Point(to, y), RawInputModifiers.LeftMouseButton);
        window.MouseUp(new Point(to, y), MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static int Green(Control chart)
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

        var count = 0;
        for (var i = 0; i < bytes.Length; i += 4)
        {
            if (Math.Abs(bytes[i + 2] - 0x82) < 24 && Math.Abs(bytes[i + 1] - 0xca) < 24 && Math.Abs(bytes[i] - 0x9d) < 24)
            {
                count++;
            }
        }

        return count;
    }
}
