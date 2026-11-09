using System.Collections.Immutable;
using System.ComponentModel;
using Aioros.Functions;

namespace Gallery.Models;

/// <summary>One reading of the live feed.</summary>
public sealed record Tick(string Time, double Value);

/// <summary>The live feed at one moment: its last readings and how many ticks it has had, the same for every visitor.</summary>
public sealed record LiveData(IReadOnlyList<Tick> Readings, int Ticks = 0);

public interface ILiveFunctions
{
    Task<LiveData> SnapshotAsync();
}

/// <summary>The feed read by the live chart: invalidated by the ticker each second, so every live page reloads it.</summary>
public sealed class LiveFunctions(LiveFeed feed) : ILiveFunctions
{
    [Reads("live")]
    public Task<LiveData> SnapshotAsync() => Task.FromResult(feed.Current);
}

/// <summary>The live chart's model, which receives each new snapshot.</summary>
public sealed class LiveModel : INotifyPropertyChanged
{
    public LiveModel(LiveData data) => Receive(data);

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<Tick> Readings { get; private set; } = [];

    public string Last { get; private set; } = string.Empty;

    public void Receive(LiveData data)
    {
        Readings = data.Readings;
        Last = data.Readings.Count > 0 ? $"Last reading {data.Readings[^1].Time}: {data.Readings[^1].Value}" : string.Empty;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Readings)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Last)));
    }
}

/// <summary>
/// The live charts' model: every family's reference data with each value scaled at the feed's tick by a factor of its
/// own (a sine of the tick, stepping about the golden angle so that no two ticks near each other repeat after rounding, and of the item's place), so each tick gives every chart new data
/// and the charts that Recharts animates morph into it.
/// </summary>
public sealed class LiveFamilies : INotifyPropertyChanged
{
    private static readonly string[] Changed =
        [nameof(Updated), nameof(Pages), nameof(Composed), nameof(Groups), nameof(Subjects), nameof(Ages), nameof(Stages), nameof(Packages), nameof(Visits), nameof(Points), nameof(Tree)];

    private int _tick;

    public LiveFamilies(LiveData data) => Receive(data);

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Updated { get; private set; } = string.Empty;

    public IReadOnlyList<Point> Pages => [.. new Datasets().Pages.Select((row, i) => row with { Uv = Scaled(row.Uv, i), Pv = Scaled(row.Pv, i + 7), Amt = Scaled(row.Amt, i + 14) })];

    public IReadOnlyList<Point> Composed => [.. new Datasets().Composed.Select((row, i) => row with { Uv = Scaled(row.Uv, i), Pv = Scaled(row.Pv, i + 7), Amt = Scaled(row.Amt, i + 14), Cnt = Scaled(row.Cnt, i + 21) })];

    public IReadOnlyList<Slice> Groups => [.. PolarData.SixGroups.Select((row, i) => row with { Value = Scale(row.Value, i) })];

    public IReadOnlyList<Subject> Subjects => [.. new PolarData().Subjects.Select((row, i) => row with { A = Scale(row.A, i), B = Scale(row.B, i + 7) })];

    public IReadOnlyList<AgeGroup> Ages => [.. new PolarData().Ages.Select((row, i) => row with { Uv = Scale(row.Uv, i), Pv = Scale(row.Pv, i + 7) })];

    public IReadOnlyList<Stage> Stages => [.. new Hierarchies().Funnel.Select((stage, i) => stage with { Value = Scale(stage.Value, i) })];

    public IReadOnlyList<Package> Packages => [.. new Hierarchies().FlareTop.Select((package, i) => Grown(package, i))];

    public Flow Visits
    {
        get
        {
            var flow = new Hierarchies().Visits;
            return flow with { Links = [.. flow.Links.Select((link, i) => link with { Value = Scale(link.Value, i) })] };
        }
    }

    public IReadOnlyList<ScatterPoint> Points => [.. ScatterSets.SchoolA.Select((row, i) => row with { Y = Scale(row.Y, i), Z = Scaled(row.Z, i + 7) })];

    public Segment Tree
    {
        get
        {
            var tree = new SunburstData().Tree;
            return tree with { Children = [.. (tree.Children ?? []).Select((child, i) => Grown(child, i))] };
        }
    }

    public void Receive(LiveData data)
    {
        _tick = data.Ticks;
        Updated = data.Readings.Count > 0 ? $"Tick {data.Ticks} at {data.Readings[^1].Time}" : string.Empty;
        foreach (var name in Changed)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    private double Scale(double value, int place) => Math.Round(value * (1 + (0.35 * Math.Sin((2.4 * _tick) + (1.3 * place)))));

    private double? Scaled(double? value, int place) => value is { } known ? Scale(known, place) : null;

    private Segment Grown(Segment segment, int place) =>
        segment with { Value = Scale(segment.Value, place), Children = segment.Children is { } children ? [.. children.Select((child, i) => Grown(child, place + i + 1))] : null };

    private Package Grown(Package package, int place) =>
        package with { Size = Scale(package.Size, place), Children = package.Children is { } children ? [.. children.Select((child, i) => Grown(child, place + i + 1))] : null };
}

/// <summary>
/// When each live chart family moves: every family's view reads a key of its own, and each second the ticker invalidates
/// the keys of a quarter of the families, so a push carries three views and each family moves every fourth second, its
/// morph (Recharts' 1.5 s at most) settling before the next.
/// </summary>
public static class LiveSchedule
{
    /// <summary>The seconds between two moves of one family.</summary>
    public const int Interval = 4;

    /// <summary>The families in the live charts page's order.</summary>
    public static readonly ImmutableArray<string> Families = ["line", "area", "bar", "composed", "scatter", "pie", "radar", "radial-bar", "funnel", "treemap", "sankey", "sunburst"];

    /// <summary>The key a family's view reads.</summary>
    public static string Key(string family) => "live-" + family;

    /// <summary>The keys of the families that move at a tick.</summary>
    public static IEnumerable<string> Due(int tick) => Families.Where((_, place) => place % Interval == tick % Interval).Select(Key);

    /// <summary>The feed's snapshot for a family's view, read under the family's key.</summary>
    public static Task<LiveData> ReadAsync(LiveFeed feed, Aioros.Functions.IDataKeys keys, string family)
    {
        ArgumentNullException.ThrowIfNull(feed);
        ArgumentNullException.ThrowIfNull(keys);
        keys.Read(Key(family));
        return Task.FromResult(feed.Current);
    }
}

/// <summary>The feed's current snapshot, replaced whole on each tick so readers always see a consistent one.</summary>
public sealed class LiveFeed
{
    private LiveData _current = new([.. Enumerable.Range(0, 20).Select(i => new Tick(Label(i), Value(i)))], 20);

    private int _ticks = 20;

    public LiveData Current => Volatile.Read(ref _current);

    /// <summary>Adds the next reading and drops the oldest.</summary>
    public void Advance()
    {
        var next = _ticks++;
        var readings = Current.Readings;
        Volatile.Write(ref _current, new LiveData([.. readings.Skip(1), new Tick(Label(next), Value(next))], next + 1));
    }

    private static string Label(int tick) => $"{tick / 60:00}:{tick % 60:00}";

    private static double Value(int tick) => Math.Round(50 + (30 * Math.Sin(tick / 3.0)) + (10 * Math.Cos(tick / 1.7)), 1);
}

/// <summary>Ticks the feed every second and invalidates the <c>live</c> key, so every live page reloads its chart, and the keys of the live chart families due at that tick.</summary>
public sealed class LiveTicker(LiveFeed feed, Aioros.Functions.IDataKeys keys, TimeProvider time) : Microsoft.Extensions.Hosting.BackgroundService
{
    /// <summary>One tick: the feed advances, the <c>live</c> key and the keys of the families due are invalidated.</summary>
    public static void Tick(LiveFeed feed, Aioros.Functions.IDataKeys keys)
    {
        ArgumentNullException.ThrowIfNull(feed);
        ArgumentNullException.ThrowIfNull(keys);
        feed.Advance();
        keys.Invalidate("live");
        foreach (var key in LiveSchedule.Due(feed.Current.Ticks))
        {
            keys.Invalidate(key);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), time, stoppingToken);
            Tick(feed, keys);
        }
    }
}
