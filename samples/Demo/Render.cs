using System.Buffers;
using System.Text;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Demo;

internal static class Render
{
    public static async Task WriteAsync(IReadOnlyList<RechartsExample> examples, string output)
    {
        foreach (var example in examples)
        {
            var svg = Markup(example);
            await File.WriteAllTextAsync(Path.Combine(output, "pictor", example.Name + ".html"), svg);
        }
    }

    /// <summary>Writes every example with its animations on (Recharts' defaults), for <c>animations.mjs</c>.</summary>
    public static async Task AnimatedAsync(IReadOnlyList<RechartsExample> examples, string output)
    {
        Directory.CreateDirectory(Path.Combine(output, "animated"));
        foreach (var example in examples)
        {
            await File.WriteAllTextAsync(Path.Combine(output, "animated", example.Name + ".html"), Markup(example with { Spec = ExampleAnimations.Animated(example.Spec) }));
        }
    }

    /// <summary>Writes every example with Recharts' <c>accessibilityLayer</c> on, for the keyboard harness.</summary>
    public static async Task AccessibleAsync(IReadOnlyList<RechartsExample> examples, string output)
    {
        Directory.CreateDirectory(Path.Combine(output, "accessible"));
        foreach (var example in examples)
        {
            await File.WriteAllTextAsync(Path.Combine(output, "accessible", example.Name + ".html"), Markup(example with { Spec = example.Spec with { AccessibilityLayer = true } }));
        }
    }

    /// <summary>Writes each update case Recharts recorded (<c>tools/recharts/out/motion</c>) before and after its change, animated, for the browser to morph one into the other.</summary>
    public static async Task UpdatesAsync(string root, string output)
    {
        Directory.CreateDirectory(Path.Combine(output, "updates"));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "tools/recharts/out/motion"), "*.json"))
        {
            using var document = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(file));
            if (document.RootElement.TryGetProperty("enter", out var enter) && enter.GetBoolean())
            {
                continue;
            }

            foreach (var side in (string[])["from", "to"])
            {
                var example = RechartsExamples.Read(document.RootElement.GetProperty(side));
                await File.WriteAllTextAsync(Path.Combine(output, "updates", $"{Path.GetFileNameWithoutExtension(file)}.{side}.html"), Markup(example with { Spec = ExampleAnimations.Animated(example.Spec) }));
            }
        }
    }

    public static string Markup(RechartsExample example)
    {
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, example.Spec, example.Data, static (_, _) => null);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
