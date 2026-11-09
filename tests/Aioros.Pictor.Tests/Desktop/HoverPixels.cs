using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Desktop;

/// <summary>
/// What a desktop chart draws for a hover state, read from its frame against the frame without it: a cursor along a line
/// or over a band, a dot's fill, a tooltip box (its background, its border and the text inside it).
/// </summary>
internal sealed class HoverPixels(byte[] before, byte[] after, int width)
{
    /// <summary>The frame of a chart in a window, at its size, BGRA over a white page (the chart's own background is transparent).</summary>
    public static byte[] Frame(Control chart)
    {
        Dispatcher.UIThread.RunJobs();
        var size = new PixelSize((int)chart.Bounds.Width, (int)chart.Bounds.Height);
        using var target = new RenderTargetBitmap(size);
        target.Render(chart);
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

        for (var i = 0; i < bytes.Length; i += 4)
        {
            var clear = 255 - bytes[i + 3];
            (bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3]) = ((byte)(bytes[i] + clear), (byte)(bytes[i + 1] + clear), (byte)(bytes[i + 2] + clear), 255);
        }

        return bytes;
    }

    /// <summary>The hovered frame's color at a point, as #rrggbb.</summary>
    public string At(int x, int y)
    {
        var i = ((y * width) + x) * 4;
        return $"#{after[i + 2]:x2}{after[i + 1]:x2}{after[i]:x2}";
    }

    /// <summary>Whether the hovered frame's color at a point is within 24 of <paramref name="color"/> in every channel.</summary>
    public bool Near(int x, int y, string color)
    {
        var i = ((y * width) + x) * 4;
        var expected = Avalonia.Media.Color.Parse(color);
        return Math.Abs(after[i + 2] - expected.R) <= 24 && Math.Abs(after[i + 1] - expected.G) <= 24 && Math.Abs(after[i] - expected.B) <= 24;
    }

    /// <summary>
    /// The share of the points of a segment left white without the hover state that the hovered frame paints with
    /// <paramref name="color"/>, covering at least a third of the pixel (a one-pixel line between two pixel columns covers
    /// half of each): each channel moved from white toward the color by a third of the way or more, none past it.
    /// </summary>
    public double Painted((int X, int Y) from, (int X, int Y) to, string color)
    {
        var expected = Avalonia.Media.Color.Parse(color);
        var steps = Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y));
        int blank = 0, painted = 0;
        for (var k = 0; k <= steps; k++)
        {
            var x = from.X + (int)Math.Round((to.X - from.X) * (double)k / Math.Max(1, steps));
            var y = from.Y + (int)Math.Round((to.Y - from.Y) * (double)k / Math.Max(1, steps));
            var i = ((y * width) + x) * 4;
            if (before[i] + before[i + 1] + before[i + 2] < 765)
            {
                continue;
            }

            blank++;
            painted += Toward(after[i + 2], expected.R) && Toward(after[i + 1], expected.G) && Toward(after[i], expected.B) ? 1 : 0;
        }

        return blank == 0 ? 0 : (double)painted / blank;

        static bool Toward(byte value, byte target) => value >= target - 24 && 255 - value >= (255 - target) / 3.0;
    }

    /// <summary>How many pixels of a rectangle the hovered frame inks with other colors than the frame before (the text of a box drawn over the chart).</summary>
    public int Written(int left, int top, int right, int bottom)
    {
        var written = 0;
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                var i = ((y * width) + x) * 4;
                var changed = Math.Abs(after[i] - before[i]) + Math.Abs(after[i + 1] - before[i + 1]) + Math.Abs(after[i + 2] - before[i + 2]);
                written += after[i] + after[i + 1] + after[i + 2] < 600 && changed > 60 ? 1 : 0;
            }
        }

        return written;
    }
}
