using System.Buffers;
using System.Text;
using System.Text.Json;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests;

/// <summary>A hover state: its index in Pictor's markup, Recharts' render after the pointer events and Pictor's markup.</summary>
internal sealed record Hover(int State, string Recharts, string Pictor, (double X, double Y) Point = default);

internal static class Reference
{
    public static string Root { get; } = FindRoot();

    public static async Task<RechartsExample> ExampleAsync(string name)
    {
        await using var stream = File.OpenRead(Path.Combine(Root, "tools", "recharts", "examples", name + ".json"));
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        return RechartsExamples.Read(document.RootElement);
    }

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows(RechartsExample example) =>
        [.. ((IEnumerable<object?>)example.Data!).Cast<IReadOnlyDictionary<string, object?>>()];

    /// <summary>Recharts' client markup of an example or of one of its interactions: Recharts 3's where the example follows Recharts 3 (<c>out/recharts3</c>), else Recharts 2's.</summary>
    public static Task<string> RechartsAsync(string name) =>
        File.ReadAllTextAsync(ReferenceFiles.Of(Root, name + ".client.html"), TestContext.Current.CancellationToken);

    public static string Pictor(RechartsExample example)
    {
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, example.Spec, example.Data, static (_, _) => null);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static async Task<MarkupVerdict> CompareAsync(string name)
    {
        var example = await ExampleAsync(name);
        return MarkupComparison.Compare(await RechartsAsync(name), Pictor(example));
    }

    /// <summary>
    /// A hover state of Pictor's markup and Recharts' render after the same pointer events: the state is the one Pictor's
    /// hit testing finds under the last click (or, without clicks, the last hover) of the example's interaction.
    /// </summary>
    public static async Task<Hover> HoverAsync(string name, int interaction)
    {
        var example = await ExampleAsync(name);
        await using var stream = File.OpenRead(Path.Combine(Root, "tools", "recharts", "examples", name + ".json"));
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var steps = document.RootElement.GetProperty("interactions")[interaction].EnumerateArray().ToList();
        var clicks = steps.Where(step => step.TryGetProperty("click", out _)).ToList();
        var point = clicks.Count > 0 ? clicks[^1].GetProperty("click") : steps[^1].GetProperty("hover");
        var plot = Aioros.Pictor.Painting.Plots.Compute(example.Spec, Aioros.Pictor.Data.ChartData.From(example.Data, static (_, _) => null));
        var state = Aioros.Pictor.Painting.Plots.Hovered(plot, point[0].GetDouble(), point[1].GetDouble());
        return new Hover(state, await RechartsAsync($"{name}.{interaction}"), Pictor(example), (point[0].GetDouble(), point[1].GetDouble()));
    }

    /// <summary>The markup of every hover state's group in a layer of Pictor's web chart (<c>pictor-hits</c>, <c>pictor-clicks</c>, <c>pictor-tips</c> or <c>pictor-cursors</c>), in order.</summary>
    public static List<string> States(string markup, string layer)
    {
        var groups = new List<string>();
        for (var i = 0; HoverComparison.Group(markup, layer, i) is { Length: > 0 } group; i++)
        {
            groups.Add(group);
        }

        return groups;
    }

    /// <summary>Compares a hover state's tooltip and active marks with Recharts'.</summary>
    public static List<string> Differences(Hover hover, double tolerance = 0.01)
    {
        var differences = HoverComparison.Compare(HoverComparison.RechartsTip(hover.Recharts), HoverComparison.PictorTip(hover.Pictor, hover.State, hover.Point), tolerance);
        differences.AddRange(HoverComparison.Compare(HoverComparison.RechartsMarks(hover.Recharts), HoverComparison.PictorMarks(hover.Pictor, hover.State)));
        return differences;
    }

    /// <summary>The differences of the legend wrapper's position and size (left, right, top, bottom, width, height) between Recharts' render and Pictor's.</summary>
    public static async Task<List<string>> LegendDifferencesAsync(string name)
    {
        var expected = LegendBox(await RechartsAsync(name));
        var actual = LegendBox(Pictor(await ExampleAsync(name)));
        return [.. expected.Keys.Union(actual.Keys).Where(key => !expected.TryGetValue(key, out var left) || !actual.TryGetValue(key, out var right) || Math.Abs(left - right) > 0.01)
            .Select(key => $"legend {key}: {(expected.TryGetValue(key, out var left) ? left : double.NaN)} in Recharts, {(actual.TryGetValue(key, out var right) ? right : double.NaN)} in Pictor")];
    }

    private static Dictionary<string, double> LegendBox(string markup)
    {
        var start = markup.IndexOf("class=\"recharts-legend-wrapper\" style=\"", StringComparison.Ordinal);
        var box = new Dictionary<string, double>(StringComparer.Ordinal);
        if (start < 0)
        {
            return box;
        }

        start = markup.IndexOf("style=\"", start, StringComparison.Ordinal) + 7;
        var style = markup[start..markup.IndexOf('"', start)];
        foreach (var declaration in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = declaration.Split(':', 2);
            var key = parts[0].Trim();
            var value = parts[1].Trim();
            if ((key is "left" or "right" or "top" or "bottom" or "width" or "height") && value.EndsWith("px", StringComparison.Ordinal))
            {
                box[key] = double.Parse(value[..^2], System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return box;
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Aioros.Pictor.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }
}
