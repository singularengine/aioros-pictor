using System.Buffers;
using System.Text;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Web;

public sealed class ChartOutputTests
{
    [Fact]
    [Trait("AC", "WEB-04")]
    public async Task TheSameDataIsWrittenAgainWithoutLayingTheChartOutAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var rows = Reference.Rows(example);
        var output = new ChartOutput(example.Spec, static (_, _) => null);
        var expected = Reference.Pictor(example);

        var writes = Enumerable.Range(0, 3).Select(_ => Write(output, rows.ToList())).ToList();
        var cached = output.Renders;
        List<IReadOnlyDictionary<string, object?>> valued = [.. rows.Select((row, i) => i == 2 ? With(row, "pv", 5000.0) : row)];
        var changedValue = Write(output, valued);
        Write(output, valued);
        var afterValue = output.Renders;
        List<IReadOnlyDictionary<string, object?>> renamed = [.. valued.Select((row, i) => i == 0 ? With(row, "name", "First") : row)];
        var changedText = Write(output, renamed);
        var afterText = output.Renders;
        var added = Write(output, [.. renamed, With(rows[0], "name", "Page H")]);

        Assert.All(writes, write => Assert.Equal(expected, write));
        Assert.Equal(2, cached);
        Assert.Equal(4, afterValue);
        Assert.NotEqual(expected, changedValue);
        Assert.Equal(5, afterText);
        Assert.Contains(">First<", changedText, StringComparison.Ordinal);
        Assert.Equal(6, output.Renders);
        Assert.Contains(">Page H<", added, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "TICKS-11")]
    public async Task CompositeFormatsFollowTheCurrentCultureAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var spec = example.Spec with
        {
            XAxes = [example.Spec.XAxes[0] with { TickFormat = "{0:MMM}" }],
            YAxes = [example.Spec.YAxes[0] with { TickFormat = "{0:N1}" }],
            Tooltip = example.Spec.Tooltip! with { LabelFormat = "{0:MMMM}" },
        };
        List<IReadOnlyDictionary<string, object?>> rows = [.. Reference.Rows(example).Select((row, index) => With(row, "name", new DateTime(2024, 4, index + 1, 0, 0, 0, DateTimeKind.Unspecified)))];
        var output = new ChartOutput(spec, static (_, _) => null);
        string In(System.Globalization.CultureInfo culture)
        {
            var previous = System.Globalization.CultureInfo.CurrentCulture;
            System.Globalization.CultureInfo.CurrentCulture = culture;
            try
            {
                return Write(output, rows);
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }
        }

        var english = Enumerable.Range(0, 3).Select(_ => In(new Named("en-US", "Apr", "April", "."))).Last();
        var kept = output.Renders;
        var spanish = In(new Named("es-MX", "abr", "abril", ","));
        var japanese = In(new Named("ja-JP", "4月", "4月", "."));

        Assert.Contains(">Apr<", english, StringComparison.Ordinal);
        Assert.Contains(">April<", english, StringComparison.Ordinal);
        Assert.Contains(">abr<", spanish, StringComparison.Ordinal);
        Assert.Contains(">abril<", spanish, StringComparison.Ordinal);
        Assert.Contains(">4月<", japanese, StringComparison.Ordinal);
        Assert.Contains("0,0<", spanish, StringComparison.Ordinal);
        Assert.Equal(kept + 2, output.Renders);
    }

    private sealed class Named : System.Globalization.CultureInfo
    {
        public Named(string name, string abbreviated, string month, string separator)
            : base(string.Empty)
        {
            Name = name;
            string[] Months(string fourth) => [.. Enumerable.Range(1, 13).Select(index => index == 4 ? fourth : "m" + index)];
            DateTimeFormat.AbbreviatedMonthNames = Months(abbreviated);
            DateTimeFormat.MonthNames = Months(month);
            NumberFormat.NumberDecimalSeparator = separator;
        }

        public override string Name { get; }
    }

    private static string Write(ChartOutput output, List<IReadOnlyDictionary<string, object?>> rows)
    {
        var buffer = new ArrayBufferWriter<byte>();
        output.Write(rows, buffer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static Dictionary<string, object?> With(IReadOnlyDictionary<string, object?> row, string key, object? value) =>
        new(row, StringComparer.Ordinal) { [key] = value };
}
