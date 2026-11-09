using System.Runtime.InteropServices;
using Aioros.Pictor.Demo;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace Aioros.Pictor.Tests.Desktop;

/// <summary>A reference example's desktop chart hosted at its size with a test clock, and its frames as pixels.</summary>
internal static class DesktopFrames
{
    public static (Chart Chart, Border Host) Hosted(RechartsExample example, bool animated, TimeProvider clock)
    {
        var chart = SpecControls.Create(example);
        foreach (var element in chart.Elements.OfType<AvaloniaObject>())
        {
            element.SetValue(Series.IsAnimationActiveProperty, animated);
        }

        chart.Time = clock;
        var host = new Border { Background = Avalonia.Media.Brushes.White, Child = chart, Width = chart.Width, Height = chart.Height };
        host.Measure(new Size(chart.Width, chart.Height));
        host.Arrange(new Rect(0, 0, chart.Width, chart.Height));
        return (chart, host);
    }

    public static byte[] Frame(Border host)
    {
        var size = new PixelSize((int)host.Width, (int)host.Height);
        using var target = new RenderTargetBitmap(size);
        target.Render(host);
        var bytes = new byte[size.Width * size.Height * 4];
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            target.CopyPixels(new PixelRect(size), handle.AddrOfPinnedObject(), bytes.Length, size.Width * 4);
        }
        finally
        {
            handle.Free();
        }

        return bytes;
    }

    public static (byte B, byte G, byte R) Pixel(byte[] frame, int x, int y, int width = 500)
    {
        var at = ((y * width) + x) * 4;
        return (frame[at], frame[at + 1], frame[at + 2]);
    }

    /// <summary>How many pixels of a column are the first bar's purple (<c>#8884d8</c>).</summary>
    public static int Ink(byte[] frame, int column, int height = 300) =>
        Enumerable.Range(0, height).Count(y => Pixel(frame, column, y) is var (b, g, r) && Math.Abs(b - 216) < 20 && Math.Abs(g - 132) < 20 && Math.Abs(r - 136) < 20);

    /// <summary>A clock that moves only when the test advances it.</summary>
    public sealed class ManualClock : TimeProvider
    {
        private long _ticks = 1;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks;

        public void Advance(double milliseconds) => _ticks += (long)(milliseconds * TimeSpan.TicksPerMillisecond);
    }
}
