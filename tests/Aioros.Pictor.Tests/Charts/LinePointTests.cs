using System.Text.RegularExpressions;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Tests.Charts;

public sealed class LinePointTests
{
    [Fact]
    [Trait("AC", "PAINT-05")]
    public async Task ALineWithNoStrokeDrawsItsIsolatedPointsWithTheirDotAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");
        var line = example.Spec.Series[0];
        var rows = Reference.Rows(example).Select((row, index) => (object?)new Dictionary<string, object?>(row) { [line.DataKey] = index % 2 == 0 ? null : 5.0 * index }).ToList();
        string Written(string? fill) => Reference.Pictor(example with
        {
            Spec = example.Spec with { Series = [line with { StrokeWidth = 0, Dot = true, DotProps = new ActiveStyle(fill) { R = 4 } }] },
            Data = rows,
        });

        var filled = Regex.Matches(Written("red"), "<circle[^>]*recharts-line-dot[^>]*>").Select(static match => match.Value).ToList();
        var unfilled = Regex.Matches(Written(null), "<circle[^>]*recharts-line-dot[^>]*>").Select(static match => match.Value).ToList();

        Assert.Equal(rows.Count / 2, filled.Count);
        Assert.All(filled, static circle => Assert.Matches("r=\"4\".*stroke-width=\"0\".*fill=\"red\"|fill=\"red\".*r=\"4\"", circle));
        Assert.All(unfilled, static circle => Assert.Contains("fill=\"#fff\"", circle, StringComparison.Ordinal));
    }
}
