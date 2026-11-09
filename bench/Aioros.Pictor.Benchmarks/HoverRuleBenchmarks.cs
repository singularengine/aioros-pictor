using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Data;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Svg;
using BenchmarkDotNet.Attributes;

namespace Aioros.Pictor.Benchmarks;

/// <summary>
/// The hover rules of the charts with the most hover states: a scatter chart of five series of 1,000 points (an item
/// tooltip per point) and three charts synced over 365 days, as a dashboard writes them. <c>selectors</c> as the
/// program's argument prints each one's bytes and its <c>:has(</c> selectors instead of running the benchmarks.
/// </summary>
[MemoryDiagnoser]
public class HoverRuleBenchmarks
{
    private static readonly DataMembers NoMembers = static (_, _) => null;
    private readonly ArrayBufferWriter<byte> _buffer = new(1024 * 1024);
    private (Specs.ChartSpec Spec, object? Data)[] _charts = [];

    [Params("Scatter5x1000", "Synced3x365")]
    public string Chart { get; set; } = string.Empty;

    [GlobalSetup]
    public async Task SetUpAsync() => _charts = await ChartsAsync(Chart);

    [Benchmark]
    public int WriteSvg()
    {
        _buffer.ResetWrittenCount();
        foreach (var (spec, data) in _charts)
        {
            ChartSvg.Write(_buffer, spec, data, NoMembers);
        }

        return _buffer.WrittenCount;
    }

    /// <summary>Prints each chart's bytes and <c>:has(</c> selectors, and writes its markup into a folder when one is given.</summary>
    public static async Task PrintAsync(string? folder)
    {
        foreach (var name in (string[])["Scatter5x1000", "Synced3x365"])
        {
            var markup = new StringBuilder();
            foreach (var (spec, data) in await ChartsAsync(name))
            {
                var buffer = new ArrayBufferWriter<byte>();
                ChartSvg.Write(buffer, spec, data, NoMembers);
                markup.Append(Encoding.UTF8.GetString(buffer.WrittenSpan));
            }

            var text = markup.ToString();
            Console.WriteLine($"{name}: {text.Length} bytes, {Regex.Count(text, ":has\\(")} :has( selectors, {Regex.Count(text, ":root")} :root");
            if (folder is not null)
            {
                await File.WriteAllTextAsync(Path.Combine(folder, name + ".html"), $"<!doctype html><meta charset=utf-8><body>{text}</body>");
            }
        }
    }

    private static async Task<(Specs.ChartSpec Spec, object? Data)[]> ChartsAsync(string name)
    {
        var examples = await RechartsExamples.LoadAsync(Folder(), CancellationToken.None);
        if (name == "Scatter5x1000")
        {
            var scatter = examples.Single(example => example.Name == "SimpleScatterChart");
            var series = scatter.Spec.Series[0];
            List<object?> rows = [.. Enumerable.Range(0, 1000).Select(i => (object?)new Dictionary<string, object?> { ["x"] = i, ["y0"] = Spread(i, 0, 1000), ["y1"] = Spread(i, 1, 1000), ["y2"] = Spread(i, 2, 1000), ["y3"] = Spread(i, 3, 1000), ["y4"] = Spread(i, 4, 1000) })];
            var spec = scatter.Spec with
            {
                XAxes = [scatter.Spec.XAxes[0] with { DataKey = "x" }],
                YAxes = [scatter.Spec.YAxes[0] with { DataKey = null }],
                Series = [.. Enumerable.Range(0, 5).Select(s => series with { Data = null, DataKey = "y" + s, Name = "s" + s })],
            };
            return [(spec, rows)];
        }

        var line = examples.Single(example => example.Name == "SimpleLineChart");
        List<object?> days = [.. Enumerable.Range(0, 365).Select(i => (object?)new Dictionary<string, object?> { ["name"] = "Day " + i, ["uv"] = Spread(i, 0, 5000), ["pv"] = Spread(i, 1, 5000), ["amt"] = Spread(i, 2, 5000) })];
        return [.. Enumerable.Range(0, 3).Select(_ => (line.Spec with { SyncId = "dashboard" }, (object?)days))];
    }

    /// <summary>A value that looks scattered and is the same on every run.</summary>
    private static int Spread(int index, int series, int range) => (int)((((long)index * 7919) + (series * 104729)) % range);

    private static string Folder()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Aioros.Pictor.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, "tools", "recharts", "examples");
    }
}
