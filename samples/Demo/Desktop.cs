using System.Text.Json;

namespace Aioros.Pictor.Demo;

/// <summary>Draws every example with the desktop control, compares it with Recharts' screenshot and times it.</summary>
internal static class Desktop
{
    public static async Task WriteAsync(IReadOnlyList<RechartsExample> examples, string root, string output)
    {
        Directory.CreateDirectory(Path.Combine(output, "desktop"));
        using var session = DesktopRender.Start();
        var only = Environment.GetEnvironmentVariable("PICTOR_ONLY");
        var results = new List<DesktopResult>();
        foreach (var example in examples.Where(example => only is null || example.Name == only))
        {
            results.Add(await session.Dispatch(
                () => DesktopRender.Measure(
                    example.Name,
                    SpecControls.Create(example),
                    ReferenceFiles.Of(root, example.Name + ".png"),
                    Path.Combine(output, "desktop", example.Name + ".png")),
                CancellationToken.None));
        }

        foreach (var result in results)
        {
            await Console.Out.WriteLineAsync($"{result.Name}: {100.0 * result.Strong / Math.Max(1, result.Inked):F2}% of inked pixels differ strongly; first frame {result.FirstFrameUs:F0} us ({result.FirstFrameBytes} B), redraw {result.RedrawUs:F0} us ({result.RedrawBytes} B)");
        }

        await File.WriteAllTextAsync(Path.Combine(output, "desktop.json"), JsonSerializer.Serialize(new { Machine = Environment.ProcessorCount + " threads", Results = results }, Json.Options));
    }
}

/// <summary>Draws every example with the desktop control in the dark theme, for a look at Pictor's dark colors.</summary>
internal static class DarkDesktop
{
    public static async Task WriteAsync(IReadOnlyList<RechartsExample> examples, string output)
    {
        Directory.CreateDirectory(Path.Combine(output, "desktop-dark"));
        var session = DesktopRender.Start();
        foreach (var example in examples)
        {
            await session.Dispatch(
                () =>
                {
                    using var drawn = DesktopRender.Draw(SpecControls.Create(example), Avalonia.Styling.ThemeVariant.Dark);
                    drawn.Save(Path.Combine(output, "desktop-dark", example.Name + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                    return 0;
                },
                CancellationToken.None);
        }

        Environment.Exit(0);
    }
}

internal static class Json
{
    public static JsonSerializerOptions Options { get; } = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
