using Aioros.Pictor.Data;

namespace Aioros.Pictor.Tests.Data;

public sealed class ChartDataTests
{
    private sealed record Row(string Name, object? Value);

    [Fact]
    [Trait("AC", "DATA-01")]
    public void ValuesAreReadAsNumbersWithMissingOnesAsGaps()
    {
        var rows = new object?[]
        {
            new Row("a", 1),
            new Row("b", 2.5m),
            new Row("c", "3.25"),
            new Row("d", null),
            new Row("e", "n/a"),
            new Dictionary<string, object?> { ["Value"] = 7L },
            null,
        };
        var data = ChartData.From(rows, static (owner, member) => member == "Value" && owner == typeof(Row) ? static item => ((Row)item).Value : null);

        var numbers = data.Numbers("Value");

        Assert.Equal([1, 2.5, 3.25, double.NaN, double.NaN, 7, double.NaN], numbers);
    }

    [Fact]
    [Trait("AC", "DATA-02")]
    public void CategoriesAreWrittenAsJavaScriptWritesThem()
    {
        var rows = new object?[] { new Row("Page A", null), new Row("x", null) };
        var data = ChartData.From(new[] { 1.0, 2.5, 1e21, 0.1 + 0.2 }.Select(value => (object?)new Dictionary<string, object?> { ["k"] = value }).Concat(rows).ToList(), static (owner, member) => member == "k" && owner == typeof(Row) ? static item => ((Row)item).Name : null);

        var texts = data.Texts("k");

        Assert.Equal(["1", "2.5", "1e+21", "0.30000000000000004", "Page A", "x"], texts);
    }

    [Fact]
    [Trait("AC", "DATA-03")]
    public void AnUnknownDataKeyLeavesGapsInsteadOfFailing()
    {
        var data = ChartData.From(new[] { new Row("a", 1) }, static (_, _) => null);

        Assert.True(double.IsNaN(data.Numbers("Missing")[0]));
        Assert.Null(data.Texts("Missing")[0]);
    }
}
