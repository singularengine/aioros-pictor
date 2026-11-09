using Aioros.Pictor.Data;
using BenchmarkDotNet.Attributes;

namespace Aioros.Pictor.Benchmarks;

/// <summary>
/// Reading a numeric member of an app's records: through a getter that returns the value boxed (what Aioros's generated
/// accessors give a slot writer today) and through a typed getter (what a typed accessor would give), at growing counts.
/// </summary>
[MemoryDiagnoser]
public class DataBenchmarks
{
    private List<Sale> _items = [];

    [Params(100, 1000, 10000)]
    public int Points { get; set; }

    [GlobalSetup]
    public void SetUp() => _items = [.. Enumerable.Range(0, Points).Select(index => new Sale("P" + index, index * 1.5))];

    [Benchmark]
    public double Column() => ChartData.From(_items, static (_, member) => member == "Value" ? static item => ((Sale)item).Value : null).Numbers("Value")[^1];

    [Benchmark(Baseline = true)]
    public double Boxed()
    {
        Func<object, object?> read = static item => ((Sale)item).Value;
        var numbers = new double[_items.Count];
        for (var i = 0; i < numbers.Length; i++)
        {
            numbers[i] = ChartData.Number(read(_items[i]));
        }

        return numbers[^1];
    }

    [Benchmark]
    public double Typed()
    {
        Func<object, double> read = static item => ((Sale)item).Value;
        var numbers = new double[_items.Count];
        for (var i = 0; i < numbers.Length; i++)
        {
            numbers[i] = read(_items[i]);
        }

        return numbers[^1];
    }

    public sealed record Sale(string Name, double Value);
}
