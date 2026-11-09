using System.ComponentModel;
using Aioros.Pictor.Data;
using Avalonia.Threading;
using BenchmarkDotNet.Attributes;

namespace Aioros.Pictor.Benchmarks;

/// <summary>
/// What a desktop chart in the visual tree adds to each layout to follow its items: walking what the layout read and
/// taking the set of items that notify their changes again, for items that notify and for plain records.
/// </summary>
[MemoryDiagnoser]
public class FollowBenchmarks
{
    private static readonly DataMembers Values = static (_, member) => member == "Value" ? static item => item is Notifying notifying ? notifying.Value : ((Plain)item).Value : null;
    private readonly ItemFollower _follower = new(static () => { }, Dispatcher.CurrentDispatcher);
    private ChartData _data = null!;

    [Params(10_000)]
    public int Points { get; set; }

    [Params(true, false)]
    public bool Notifies { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        object[] items = Notifies ? [.. Enumerable.Range(0, Points).Select(i => new Notifying { Value = i })] : [.. Enumerable.Range(0, Points).Select(i => new Plain(i))];
        _data = ChartData.From(items, Values);
        _data.Numbers("Value");
        _follower.Follow(_data, []);
    }

    /// <summary>A layout's follow when the items have not changed: the set taken again, nobody subscribed or let go.</summary>
    [Benchmark]
    public void Follow() => _follower.Follow(_data, []);

    /// <summary>A bare loop over the items, the floor of any walk.</summary>
    [Benchmark(Baseline = true)]
    public int Walk()
    {
        var count = 0;
        foreach (var item in _data.Items)
        {
            count += item is INotifyPropertyChanged ? 1 : 0;
        }

        return count;
    }

    public sealed record Plain(double Value);

    public sealed class Notifying : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }

        public double Value { get; init; }
    }
}
