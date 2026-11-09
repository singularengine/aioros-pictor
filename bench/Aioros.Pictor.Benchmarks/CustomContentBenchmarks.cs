using System.Buffers;
using Aioros.Pictor.Data;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;
using BenchmarkDotNet.Attributes;

namespace Aioros.Pictor.Benchmarks;

/// <summary>
/// The custom content examples written with their compiled templates (the specs their XAML builds, kept in
/// <c>Templated</c> by the tests run with <c>PICTOR_KEEP_SPEC</c>) and with Recharts' default content, as the server
/// writes them per request and from a compiled page's kept markup.
/// </summary>
[MemoryDiagnoser]
public class CustomContentBenchmarks
{
    private static readonly DataMembers NoMembers = static (_, _) => null;
    private readonly ArrayBufferWriter<byte> _buffer = new(64 * 1024);
    private RechartsExample _example = null!;
    private ChartSpec _templated = null!;
    private ChartOutput _defaultOutput = null!;
    private ChartOutput _templatedOutput = null!;

    /// <summary>Every templated example, or the ones <c>PICTOR_EXAMPLES</c> names (separated by commas).</summary>
    public static IEnumerable<string> Examples => Directory.EnumerateFiles(Folder("bench", "Aioros.Pictor.Benchmarks", "Templated"), "*.json").Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal)
        .Where(name => Environment.GetEnvironmentVariable("PICTOR_EXAMPLES") is not { Length: > 0 } only || only.Split(',').Contains(name))!;

    [ParamsSource(nameof(Examples))]
    public string Example { get; set; } = string.Empty;

    [GlobalSetup]
    public async Task SetUpAsync()
    {
        var examples = await RechartsExamples.LoadAsync(Folder("tools", "recharts", "examples"), CancellationToken.None);
        _example = examples.Single(example => example.Name == Example);
        _templated = SpecJson.Read(await File.ReadAllTextAsync(Path.Combine(Folder("bench", "Aioros.Pictor.Benchmarks", "Templated"), Example + ".json")));
        _defaultOutput = new ChartOutput(_example.Spec, NoMembers);
        _templatedOutput = new ChartOutput(_templated, NoMembers);
    }

    [Benchmark(Baseline = true)]
    public int WriteSvgDefault() => Write(_example.Spec);

    [Benchmark]
    public int WriteSvgTemplated() => Write(_templated);

    [Benchmark]
    public int WriteCachedDefault()
    {
        _buffer.ResetWrittenCount();
        _defaultOutput.Write(_example.Data, _buffer);
        return _buffer.WrittenCount;
    }

    [Benchmark]
    public int WriteCachedTemplated()
    {
        _buffer.ResetWrittenCount();
        _templatedOutput.Write(_example.Data, _buffer);
        return _buffer.WrittenCount;
    }

    private int Write(ChartSpec spec)
    {
        _buffer.ResetWrittenCount();
        ChartSvg.Write(_buffer, spec, _example.Data, NoMembers);
        return _buffer.WrittenCount;
    }

    private static string Folder(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Aioros.Pictor.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine([directory!.FullName, .. parts]);
    }
}
