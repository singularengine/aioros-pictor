using System.Buffers;
using System.Text;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;
using Aioros.Pictor.Tests.Desktop;
using Avalonia;

namespace Aioros.Pictor.Tests;

/// <summary>A reference example drawn by the desktop control or written from the spec its XAML elements build.</summary>
internal static class ReferenceDesktop
{
    /// <summary>How the desktop control built from the example's XAML elements compares with Recharts' screenshot.</summary>
    public static async Task<DesktopResult> MeasureAsync(DesktopSession desktop, string name)
    {
        var example = await Reference.ExampleAsync(name);
        var output = Path.Combine(Path.GetTempPath(), "pictor-desktop-" + Guid.NewGuid().ToString("N") + ".png");
        var result = await desktop.Session.Dispatch(
            () => DesktopRender.Compare(example.Name, SpecControls.Create(example), ReferenceFiles.Of(Reference.Root, name + ".png"), output),
            TestContext.Current.CancellationToken);
        File.Delete(output);
        return result;
    }

    /// <summary>The desktop control's pixels (BGRA) for the example.</summary>
    public static async Task<(int Width, int Height, byte[] Bgra)> PixelsAsync(DesktopSession desktop, string name)
    {
        var example = await Reference.ExampleAsync(name);
        return await desktop.Session.Dispatch(
            () =>
            {
                using var bitmap = DesktopRender.Draw(SpecControls.Create(example));
                return DesktopRender.Pixels(bitmap);
            },
            TestContext.Current.CancellationToken);
    }

    /// <summary>Whether a pixel is white (nothing drawn there).</summary>
    public static bool Blank((int Width, int Height, byte[] Bgra) pixels, int x, int y)
    {
        var i = ((y * pixels.Width) + x) * 4;
        return pixels.Bgra[i] > 245 && pixels.Bgra[i + 1] > 245 && pixels.Bgra[i + 2] > 245;
    }

    /// <summary>The spec the example's XAML elements build, as Aioros receives it.</summary>
    public static async Task<ChartSpec> SpecAsync(DesktopSession desktop, string name)
    {
        var example = await Reference.ExampleAsync(name);
        var slot = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                chart.Measure(new Size(example.Spec.Width, example.Spec.Height));
                chart.Arrange(new Rect(0, 0, example.Spec.Width, example.Spec.Height));
                return chart.WrittenSlot!;
            },
            TestContext.Current.CancellationToken);
        return SpecJson.Read(slot.Argument);
    }

    /// <summary>How the markup written from the spec the example's XAML elements build compares with Recharts'.</summary>
    public static async Task<MarkupVerdict> CompareXamlAsync(DesktopSession desktop, string name)
    {
        var example = await Reference.ExampleAsync(name);
        var spec = await SpecAsync(desktop, name);
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, example.Data, static (_, _) => null);
        return MarkupComparison.Compare(await Reference.RechartsAsync(name), Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}
