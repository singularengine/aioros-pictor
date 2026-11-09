using System.Buffers;
using Aioros.Pictor.BoundValues;
using Aioros.Pictor.Data;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Svg;
using Aioros.Runtime.Rendering;
using BenchmarkDotNet.Attributes;

namespace Aioros.Pictor.Benchmarks;

/// <summary>
/// The paths the audit fixes touched beyond the reference examples: a layered Sankey's layout (8 layers of 4 nodes, each
/// linked to every node of the next) and a request of a chart whose own data (2,000 items) is a bound table hole, held.
/// </summary>
[MemoryDiagnoser]
public class AuditBenchmarks
{
    private static readonly DataMembers NoMembers = static (_, _) => null;
    private readonly ArrayBufferWriter<byte> _buffer = new(256 * 1024);
    private Specs.ChartSpec _sankey = null!;
    private ChartData _layers = null!;
    private SlotRender _holes = null!;
    private object? _pieData;
    private Parts _parts = null!;

    [GlobalSetup]
    public async Task SetUpAsync()
    {
        var examples = await RechartsExamples.LoadAsync(Folder(), CancellationToken.None);
        _sankey = examples.Single(example => example.Name == "BudgetSankey").Spec;
        const int layers = 8;
        _layers = ChartData.From(new Dictionary<string, object?>
        {
            ["nodes"] = Enumerable.Range(0, layers * 4).Select(i => (object?)new Dictionary<string, object?> { ["name"] = "n" + i }).ToList(),
            ["links"] = Enumerable.Range(0, layers - 1).SelectMany(layer => from a in Enumerable.Range(0, 4) from b in Enumerable.Range(0, 4) select (object?)new Dictionary<string, object?> { ["source"] = (layer * 4) + a, ["target"] = ((layer + 1) * 4) + b, ["value"] = 1 }).ToList(),
        }, NoMembers);
        var pie = examples.Single(example => example.Name == "TwoSimplePieChart");
        var spec = pie.Spec with { Polar = pie.Spec.Polar! with { Pies = pie.Spec.Polar.Pies.SetItem(0, pie.Spec.Polar.Pies[0] with { Hide = true }) } };
        _holes = ChartRenderer.Create(SpecHoles.Write(spec, [new SpecHole("p0", HoleKind.Table, ["/Polar/Pies/0/Data"]) { Keys = ["name", "value"] }]), static (_, _) => null!);
        _pieData = pie.Data;
        _parts = new Parts(Enumerable.Range(0, 2000).Select(i => new Dictionary<string, object?> { ["name"] = "Item " + i, ["value"] = (double)(i % 50) + 1 }).ToList());
        HeldTable();
        HeldTable();
    }

    [Benchmark]
    public int LayeredSankey() => SankeyPlot.Compute(_sankey, _layers).Nodes.Length;

    [Benchmark]
    public int HeldTable()
    {
        _buffer.ResetWrittenCount();
        var output = new SlotOutput(new HtmlOutput(_buffer), _pieData, _parts);
        _holes(ref output);
        return _buffer.WrittenCount;
    }

    private sealed class Parts(object items) : ISlotParts
    {
        public bool TryRead(string key, object? scope, out object? value)
        {
            value = items;
            return true;
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
