using System.Buffers;
using System.Text;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Web;

public sealed class SlotWriterTests(Desktop.DesktopSession desktop)
{
    private sealed record Sale(string Name, double Pv, int Uv);

    [Fact]
    [Trait("AC", "WEB-01")]
    public async Task TheSlotWriterWritesTheChartFromTheBoundRecordsThroughTheMemberLookupAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");
        var records = Reference.Rows(example).Select(row => new Sale((string)row["name"]!, (double)row["pv"]!, (int)(double)row["uv"]!)).ToList();
        var spec = example.Spec with
        {
            XAxes = [.. example.Spec.XAxes.Select(axis => axis with { DataKey = "Name" })],
            Series = [.. example.Spec.Series.Select(series => series with { DataKey = series.DataKey == "pv" ? "Pv" : "Uv", Name = series.DataKey })],
        };
        var write = Slots.Writer(SpecJson.Write(spec), static (owner, member) => member switch
        {
            "Name" => static item => ((Sale)item).Name,
            "Pv" => static item => ((Sale)item).Pv,
            _ => static item => ((Sale)item).Uv,
        });
        var buffer = new ArrayBufferWriter<byte>();

        write(records, buffer);

        Assert.Equal(Reference.Pictor(example), Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact]
    [Trait("AC", "WEB-02")]
    public async Task AChartTellsAiorosToWriteItsBoundDataWithTheChartWriterAndItsSpecAsync()
    {
        var example = await Reference.ExampleAsync("MixBarChart");

        var (slot, size) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                chart.Measure(new Avalonia.Size(500, 300));
                chart.Arrange(new Avalonia.Rect(0, 0, 500, 300));
                return (chart.WrittenSlot!, chart.Bounds.Size);
            },
            TestContext.Current.CancellationToken);
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, SpecJson.Read(slot.Argument), example.Data, static (_, _) => null);

        Assert.Equal(typeof(ChartRenderer).FullName, slot.Writer);
        Assert.Equal("Data", slot.Property);
        Assert.Equal(new Avalonia.Size(500, 300), size);
        Assert.Empty(MarkupComparison.Compare(await Reference.RechartsAsync("MixBarChart"), Encoding.UTF8.GetString(buffer.WrittenSpan)).Differences);
    }
}
