using System.Buffers;
using Aioros.Pictor.Data;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;
using BenchmarkDotNet.Attributes;

namespace Aioros.Pictor.Benchmarks;

/// <summary>A monotone line chart with dots, a grid, axes, a legend and a tooltip, at growing point counts.</summary>
[MemoryDiagnoser]
public class ScaleBenchmarks
{
    private static readonly DataMembers NoMembers = static (_, _) => null;
    private readonly ArrayBufferWriter<byte> _buffer = new(1024 * 1024);
    private readonly ChartSpec _spec = new()
    {
        Kind = ChartKind.Line,
        Width = 960,
        Height = 500,
        Grid = new GridSpec { StrokeDashArray = "3 3" },
        XAxes = [new AxisSpec { DataKey = "name", Type = AxisType.Category }],
        YAxes = [new AxisSpec { Type = AxisType.Number, Orientation = AxisOrientation.Left, Order = 1 }],
        Series = [new SeriesSpec { Kind = SeriesKind.Line, DataKey = "value", Curve = CurveType.Monotone, Stroke = "#8884d8", Dot = true, Order = 2 }],
        Legend = new LegendSpec(),
    };

    private List<IReadOnlyDictionary<string, object?>> _data = [];

    [Params(100, 1000, 10000)]
    public int Points { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        var state = 42u;
        _data = [.. Enumerable.Range(0, Points).Select(index =>
        {
            state = (state * 1664525) + 1013904223;
            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { ["name"] = "P" + index, ["value"] = (double)(state % 10000) };
        })];
    }

    [Benchmark]
    public int WriteSvg()
    {
        _buffer.ResetWrittenCount();
        ChartSvg.Write(_buffer, _spec, _data, NoMembers);
        return _buffer.WrittenCount;
    }
}
