using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Themes.Fluent;

namespace Aioros.Pictor.Demo;

/// <summary>The headless Avalonia app the desktop renders run in: Skia drawing and the Inter font, as Aioros apps have.</summary>
public sealed class DesktopApp : Application
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopApp>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
        .WithInterFont()
        .With(new FontManagerOptions { DefaultFamilyName = "fonts:Inter#Inter" });

    public override void Initialize() => Styles.Add(new FluentTheme());
}

/// <summary>
/// How a desktop chart compares with Recharts' render over the union of both images (a pixel only one image has differs
/// strongly), whether both have the same size, and what it costs to draw (zero when not timed).
/// </summary>
internal sealed record DesktopResult(string Name, int Pixels, int Inked, int Differ, int Strong, double FirstFrameUs, double RedrawUs, long FirstFrameBytes, long RedrawBytes, bool SameSize = true)
{
    /// <summary>The share of inked pixels that differ strongly.</summary>
    public double Share => (double)Strong / Math.Max(1, Inked);
}

/// <summary>
/// Draws a chart control as the desktop does (Skia, through Avalonia's <see cref="RenderTargetBitmap"/> at device scale
/// 1), compares it pixel by pixel with Recharts' screenshot, and times a first frame (layout, recorded scene, raster) and
/// a redraw (the recorded scene and the hover layer).
/// </summary>
internal static class DesktopRender
{
    public static HeadlessUnitTestSession Start() => HeadlessUnitTestSession.StartNew(typeof(DesktopApp));

    public static WriteableBitmap Draw(Chart chart) => Draw(chart, Avalonia.Styling.ThemeVariant.Light);

    /// <summary>Draws a chart in a theme variant, over the variant's page color (white, or black as Fluent's dark window).</summary>
    public static WriteableBitmap Draw(Chart chart, Avalonia.Styling.ThemeVariant variant)
    {
        var size = new PixelSize((int)chart.Width, (int)chart.Height);
        var page = new Border { Background = variant == Avalonia.Styling.ThemeVariant.Dark ? Avalonia.Media.Brushes.Black : Avalonia.Media.Brushes.White, Child = chart, Width = chart.Width, Height = chart.Height };
        var host = new ThemeVariantScope { RequestedThemeVariant = variant, Child = page };
        host.Measure(new Size(chart.Width, chart.Height));
        host.Arrange(new Rect(0, 0, chart.Width, chart.Height));
        using var target = new RenderTargetBitmap(size);
        target.Render(host);
        return Copy(target);
    }

    /// <summary>Draws the chart, saves the frame (a copy in the folder <c>PICTOR_KEEP_PNG</c> names, when set) and compares it with Recharts' screenshot, without timing it.</summary>
    public static DesktopResult Compare(string name, Chart chart, string referencePng, string outputPng)
    {
        using var drawn = Draw(chart);
        drawn.Save(outputPng, PngBitmapEncoderOptions.Default);
        if (Environment.GetEnvironmentVariable("PICTOR_KEEP_PNG") is { Length: > 0 } keep)
        {
            File.Copy(outputPng, Path.Combine(keep, name + ".desktop.png"), true);
        }

        using var reference = new Bitmap(referencePng);
        var expected = Pixels(reference);
        var actual = Pixels(drawn);
        var (pixels, inked, differ, strong) = Tally(expected, actual);
        return new DesktopResult(name, pixels, inked, differ, strong, 0, 0, 0, 0, expected.Width == actual.Width && expected.Height == actual.Height);
    }

    /// <summary>Compares two images as the desktop criteria do, for tests of the comparison itself.</summary>
    public static DesktopResult Compare(string name, (int Width, int Height, byte[] Bgra) expected, (int Width, int Height, byte[] Bgra) actual)
    {
        var (pixels, inked, differ, strong) = Tally(expected, actual);
        return new DesktopResult(name, pixels, inked, differ, strong, 0, 0, 0, 0, expected.Width == actual.Width && expected.Height == actual.Height);
    }

    /// <summary>Compares the chart with Recharts' screenshot as <see cref="Compare(string, Chart, string, string)"/> does, then times a first frame and a redraw.</summary>
    public static DesktopResult Measure(string name, Chart chart, string referencePng, string outputPng)
    {
        var compared = Compare(name, chart, referencePng, outputPng);
        var size = new PixelSize((int)chart.Width, (int)chart.Height);
        using var target = new RenderTargetBitmap(size);
        var (first, firstBytes) = Time(() =>
        {
            var title = chart.Title;
            chart.Title = "\u0001";
            chart.Title = title;
            target.Render(chart);
        });
        var (redraw, redrawBytes) = Time(() =>
        {
            chart.InvalidateVisual();
            target.Render(chart);
        });
        return compared with { FirstFrameUs = first, RedrawUs = redraw, FirstFrameBytes = firstBytes, RedrawBytes = redrawBytes };
    }

    private static (double Us, long Bytes) Time(Action draw)
    {
        for (var i = 0; i < 5; i++)
        {
            draw();
        }

        var samples = new double[11];
        var bytes = new long[11];
        for (var k = 0; k < samples.Length; k++)
        {
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var start = Stopwatch.GetTimestamp();
            draw();
            samples[k] = Stopwatch.GetElapsedTime(start).TotalMicroseconds;
            bytes[k] = GC.GetAllocatedBytesForCurrentThread() - allocated;
        }

        Array.Sort(samples);
        Array.Sort(bytes);
        return (samples[samples.Length / 2], bytes[bytes.Length / 2]);
    }

    private static WriteableBitmap Copy(Bitmap source)
    {
        var copy = new WriteableBitmap(source.PixelSize, source.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var buffer = copy.Lock();
        source.CopyPixels(new PixelRect(source.PixelSize), buffer.Address, buffer.RowBytes * source.PixelSize.Height, buffer.RowBytes);
        return copy;
    }

    public static (int Width, int Height, byte[] Bgra) Pixels(Bitmap bitmap)
    {
        var width = bitmap.PixelSize.Width;
        var height = bitmap.PixelSize.Height;
        var bytes = new byte[width * height * 4];
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(new PixelRect(bitmap.PixelSize), handle.AddrOfPinnedObject(), bytes.Length, width * 4);
        }
        finally
        {
            handle.Free();
        }

        return (width, height, bytes);
    }

    /// <summary>
    /// Compares two images over their union: a pixel both have is inked when either is not white and differs strongly
    /// past 64 in a channel; a pixel only one has (the images' sizes differ) is inked and differs strongly.
    /// </summary>
    private static (int Pixels, int Inked, int Differ, int Strong) Tally((int Width, int Height, byte[] Bgra) a, (int Width, int Height, byte[] Bgra) b)
    {
        var width = Math.Max(a.Width, b.Width);
        var height = Math.Max(a.Height, b.Height);
        int inked = 0, differ = 0, strong = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var (ink, d) = Pixel(a, b, x, y);
                inked += ink ? 1 : 0;
                differ += d > 0 ? 1 : 0;
                strong += d > 64 ? 1 : 0;
            }
        }

        return (width * height, inked, differ, strong);
    }

    /// <summary>Whether a pixel is inked in either image, and its largest channel difference (255 where only one image has it).</summary>
    private static (bool Inked, int Difference) Pixel((int Width, int Height, byte[] Bgra) a, (int Width, int Height, byte[] Bgra) b, int x, int y)
    {
        if (x >= a.Width || y >= a.Height || x >= b.Width || y >= b.Height)
        {
            return (true, 255);
        }

        var i = ((y * a.Width) + x) * 4;
        var j = ((y * b.Width) + x) * 4;
        var d = Math.Max(Math.Abs(a.Bgra[i] - b.Bgra[j]), Math.Max(Math.Abs(a.Bgra[i + 1] - b.Bgra[j + 1]), Math.Abs(a.Bgra[i + 2] - b.Bgra[j + 2])));
        return (a.Bgra[i] + a.Bgra[i + 1] + a.Bgra[i + 2] < 750 || b.Bgra[j] + b.Bgra[j + 1] + b.Bgra[j + 2] < 750, d);
    }
}
