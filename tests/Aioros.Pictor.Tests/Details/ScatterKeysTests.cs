using System.Text.Json;
using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;

namespace Aioros.Pictor.Tests.Details;

public sealed partial class ScatterKeysTests
{
    [Theory]
    [Trait("AC", "DETAIL-04")]
    [InlineData("DetailsKeyboardScatterChart", 6)]
    [InlineData("DetailsKeyboardTwoScatterChart", 12)]
    public async Task AScatterChartsKeysDrawRechartsCrossOnTheWebAsync(string name, int points)
    {
        var markup = Reference.Pictor(await Reference.ExampleAsync(name));
        var layer = Layer().Match(markup);

        Assert.True(layer.Success);
        Assert.Contains("role=\"application\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("tabindex=\"0\" role=\"application\"", markup, StringComparison.Ordinal);
        var groups = Group().Matches(layer.Value);
        Assert.Equal(points, groups.Count);
        Assert.Equal(" tabindex=\"00\"", groups[0].Groups[1].Value);
        Assert.All(groups.Skip(1), group => Assert.Equal(" tabindex=\"-1\"", group.Groups[1].Value));
        Assert.Contains(".pictor-keys>g:focus-visible>*,.recharts-wrapper:not(:has(.pictor-hover>:hover,.pictor-keys>:focus)) .pictor-keys>[tabindex=\"0\"]>*{visibility:visible}", markup, StringComparison.Ordinal);

        var interactions = await StepsAsync(name, points);
        for (var i = 0; i < interactions.Count; i++)
        {
            var recharts = await Reference.RechartsAsync($"{name}.{i}");
            Assert.DoesNotContain("visibility: visible", Wrapper().Match(recharts).Value, StringComparison.Ordinal);
            var expected = MarkupComparison.Parse(recharts).Single(element => element.Classes.Contains("recharts-tooltip-cursor", StringComparison.Ordinal));
            var actual = MarkupComparison.Parse(groups[interactions[i]].Value).Single(element => element.Tag == "path");
            var differences = new List<string>();
            MarkupComparison.CompareElements(expected, actual, differences);
            Assert.True(differences.Count == 0, $"{name}.{i}: " + string.Join("\n", differences));
        }
    }

    /// <summary>The key step each interaction of the example ends at, as Recharts' manager counts them (clamped arrows).</summary>
    internal static async Task<List<int>> StepsAsync(string name, int points)
    {
        await using var stream = File.OpenRead(Path.Combine(Reference.Root, "tools", "recharts", "examples", name + ".json"));
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var steps = new List<int>();
        foreach (var interaction in document.RootElement.GetProperty("interactions").EnumerateArray())
        {
            var step = 0;
            foreach (var key in interaction.EnumerateArray().Where(entry => entry.TryGetProperty("key", out _)).Select(entry => entry.GetProperty("key").GetString()))
            {
                step = key switch { "ArrowRight" => Math.Min(step + 1, points - 1), "ArrowLeft" => Math.Max(step - 1, 0), _ => step };
            }

            steps.Add(step);
        }

        return steps;
    }

    [GeneratedRegex("<g class=\"pictor-keys\" data-aioros-roving=\"horizontal\" data-aioros-roving-escape data-aioros-roving-ends=\"off\">.*?</g></g>")]
    private static partial Regex Layer();

    [GeneratedRegex("<g( tabindex=\"[^\"]*\")>(.*?)</g>")]
    private static partial Regex Group();

    [GeneratedRegex("<div[^>]*recharts-tooltip-wrapper[^>]*>")]
    private static partial Regex Wrapper();
}
