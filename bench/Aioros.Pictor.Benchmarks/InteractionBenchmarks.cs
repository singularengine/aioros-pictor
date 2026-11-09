using System.Buffers;
using System.IO.Compression;
using Aioros.Pictor.BoundValues;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;
using Aioros.Runtime.Rendering;
using BenchmarkDotNet.Attributes;

namespace Aioros.Pictor.Benchmarks;

/// <summary>
/// What the interactive web costs per chart per request: the chart written through Aioros's slot renderer (the kept
/// markup copied through a slot output), the same chart with a command on its items (its buttons' ids reserved and
/// written into the kept markup), the first render of that chart with its buttons, and a chart whose bound child
/// property fills a hole of its spec (the spec kept while the value stays the same).
/// </summary>
[MemoryDiagnoser]
public class InteractionBenchmarks
{
    private static readonly MemberLookup NoMembers = static (_, _) => static _ => null;
    private readonly ArrayBufferWriter<byte> _buffer = new(64 * 1024);
    private RechartsExample _example = null!;
    private ChartSpec _commanded = null!;
    private SlotRender _plain = null!;
    private SlotRender _commands = null!;
    private SlotRender _bound = null!;
    private readonly Parts _parts = new();

    [ParamsSource(nameof(Examples))]
    public string Example { get; set; } = string.Empty;

    public static IEnumerable<string> Examples => ExampleBenchmarks.Examples;

    [GlobalSetup]
    public async Task SetUpAsync()
    {
        var examples = await RechartsExamples.LoadAsync(Folder(), CancellationToken.None);
        _example = examples.Single(example => example.Name == Example);
        _commanded = _example.Spec with { ItemClickCommand = "item" };
        _plain = ChartRenderer.Create(SpecJson.Write(_example.Spec), NoMembers);
        _commands = ChartRenderer.Create(SpecJson.Write(_commanded), NoMembers);
        _bound = ChartRenderer.Create(SpecHoles.Write(_example.Spec with { Title = "Chart" }, [new SpecHole("p0", HoleKind.Text, ["/Title"])]), NoMembers);
        var plain = Bytes(_example.Spec);
        var commanded = Bytes(_commanded);
        await Console.Out.WriteLineAsync($"BYTES {Example} plain={plain.Length} brotli={Brotli(plain)} commands={commanded.Length} brotli={Brotli(commanded)}");
    }

    [Benchmark]
    public int Render()
    {
        _buffer.ResetWrittenCount();
        var output = new SlotOutput(new HtmlOutput(_buffer), _example.Data, _parts);
        _plain(ref output);
        return _buffer.WrittenCount;
    }

    [Benchmark]
    public int RenderCommands()
    {
        _buffer.ResetWrittenCount();
        var output = new SlotOutput(new HtmlOutput(_buffer), _example.Data, _parts);
        _commands(ref output);
        return _buffer.WrittenCount;
    }

    [Benchmark]
    public int WriteCommands()
    {
        _buffer.ResetWrittenCount();
        ChartSvg.Write(_buffer, _commanded, _example.Data, static (_, _) => null);
        return _buffer.WrittenCount;
    }

    [Benchmark]
    public int RenderBound()
    {
        _buffer.ResetWrittenCount();
        var output = new SlotOutput(new HtmlOutput(_buffer), _example.Data, _parts);
        _bound(ref output);
        return _buffer.WrittenCount;
    }

    private byte[] Bytes(ChartSpec spec)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var output = new SlotOutput(new HtmlOutput(buffer), _example.Data, new Parts());
        ChartRenderer.Create(SpecJson.Write(spec), NoMembers)(ref output);
        return buffer.WrittenSpan.ToArray();
    }

    private static int Brotli(byte[] bytes)
    {
        using var memory = new MemoryStream();
        using (var brotli = new BrotliStream(memory, CompressionLevel.Optimal, leaveOpen: true))
        {
            brotli.Write(bytes);
        }

        return (int)memory.Length;
    }

    private static string Folder()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Aioros.Pictor.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, "tools", "recharts", "examples");
    }

    /// <summary>The plan's parts: the bound title's value, and ids for the fields and commands, numbered per render.</summary>
    private sealed class Parts : ISlotParts
    {
        public bool TryRead(string key, object? scope, out object? value)
        {
            value = "Chart";
            return key == "p0";
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
