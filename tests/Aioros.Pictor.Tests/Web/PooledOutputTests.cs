using Aioros.Pictor.Svg;
using Aioros.Runtime.Rendering;

namespace Aioros.Pictor.Tests.Web;

public sealed class PooledOutputTests
{
    [Fact]
    [Trait("AC", "WEB-10")]
    public async Task AChartWithCommandsIsLaidOutIntoPooledBuffersAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var spec = example.Spec with { Series = [.. example.Spec.Series.Select(series => series with { ClickCommand = "pick" })] };
        var row = Reference.Rows(example)[0];
        var versions = Enumerable.Range(0, 6).Select(version => Enumerable.Range(0, 300)
            .Select(index => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>(row, StringComparer.Ordinal) { ["name"] = $"Item {index}", ["pv"] = (double)((index * 7) + version), ["uv"] = (double)((index * 3) + version) })
            .ToList()).ToList();
        var output = new ChartOutput(spec, static (_, _) => null);
        var length = Write(output, versions[0], out _);

        var least = long.MaxValue;
        for (var version = 1; version < versions.Count; version++)
        {
            Write(output, versions[version], out var allocated);
            least = Math.Min(least, allocated);
        }

        TestContext.Current.TestOutputHelper?.WriteLine($"markup {length} B, allocated {least} B a write");
        Assert.True(length > 85_000);
        Assert.Equal(versions.Count, output.Renders);
        Assert.True(least < length, $"{least} B allocated for markup of {length} B");
    }

    private static int Write(ChartOutput output, List<IReadOnlyDictionary<string, object?>> rows, out long allocated)
    {
        using var buffer = new PooledBuffer(1024 * 1024);
        var parts = new Commands();
        var start = GC.GetAllocatedBytesForCurrentThread();
        var slot = new SlotOutput(new HtmlOutput(buffer), rows, parts);
        output.Write(ref slot);
        allocated = GC.GetAllocatedBytesForCurrentThread() - start;
        return buffer.WrittenSpan.Length;
    }

    private sealed class Commands : ISlotParts
    {
        public bool TryRead(string key, object? scope, out object? value)
        {
            value = null;
            return false;
        }

        public void WriteFieldId(ref HtmlOutput output, string key, object? scope) => output.WriteFieldId(output.ReserveField(null, scope));

        public void WriteCommandId(ref HtmlOutput output, string key, string parameter, object? scope) => output.WriteFieldId(output.ReserveField(null, scope));

        public int WriteOptionId(ref HtmlOutput output, string key, string value, int reserved, object? scope)
        {
            var id = reserved < 0 ? output.ReserveField(null, scope) : reserved;
            output.WriteFieldId(id);
            return id;
        }
    }
}
