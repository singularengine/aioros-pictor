using System.Buffers;
using Aioros.Pictor.Data;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Svg;
using BenchmarkDotNet.Attributes;

namespace Aioros.Pictor.Benchmarks;

/// <summary>
/// What a server does per chart per request: read the bound data, lay the chart out and write Recharts' markup into a
/// reused buffer, for each of Recharts' fifteen reference examples (Recharts' own time is <c>tools/recharts/bench.mjs</c>).
/// </summary>
[MemoryDiagnoser]
public class ExampleBenchmarks
{
    private static readonly DataMembers NoMembers = static (_, _) => null;
    private readonly ArrayBufferWriter<byte> _buffer = new(64 * 1024);
    private RechartsExample _example = null!;
    private ChartOutput _output = null!;
    private Specs.ChartSpec _animated = null!;

    /// <summary>Every reference example, or the ones <c>PICTOR_EXAMPLES</c> names (separated by commas).</summary>
    public static IEnumerable<string> Examples => Directory.EnumerateFiles(Folder(), "*.json").Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal)
        .Where(name => Environment.GetEnvironmentVariable("PICTOR_EXAMPLES") is not { Length: > 0 } only || only.Split(',').Contains(name))!;

    [ParamsSource(nameof(Examples))]
    public string Example { get; set; } = string.Empty;

    [GlobalSetup]
    public async Task SetUpAsync()
    {
        var examples = await RechartsExamples.LoadAsync(Folder(), CancellationToken.None);
        _example = examples.Single(example => example.Name == Example);
        _output = new ChartOutput(_example.Spec, NoMembers);
        _animated = ExampleAnimations.Animated(_example.Spec);
    }

    [Benchmark]
    public int WriteSvg()
    {
        _buffer.ResetWrittenCount();
        ChartSvg.Write(_buffer, _example.Spec, _example.Data, NoMembers);
        return _buffer.WrittenCount;
    }

    /// <summary>The same chart with Recharts' enter animations on, as an app that does not switch them off writes it.</summary>
    [Benchmark]
    public int WriteAnimated()
    {
        _buffer.ResetWrittenCount();
        ChartSvg.Write(_buffer, _animated, _example.Data, NoMembers);
        return _buffer.WrittenCount;
    }

    /// <summary>What a compiled page's writer does when the data holds the values it wrote last: read and compare them, write the kept markup.</summary>
    [Benchmark]
    public int WriteCached()
    {
        _buffer.ResetWrittenCount();
        _output.Write(_example.Data, _buffer);
        return _buffer.WrittenCount;
    }

    [Benchmark]
    public int Layout() => Plots.Compute(_example.Spec, ChartData.From(_example.Data, NoMembers)).HoverCount;

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
