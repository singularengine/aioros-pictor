using System.Buffers;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Demo;

internal static class Profile
{
    public static async Task RunAsync(IReadOnlyList<RechartsExample> examples)
    {
        var example = examples.Single(item => item.Name == "SimpleLineChart");
        var buffer = new ArrayBufferWriter<byte>(64 * 1024);
        foreach (var (name, spec) in new (string, ChartSpec)[]
        {
            ("full", example.Spec),
            ("no tooltip", example.Spec with { Tooltip = null }),
            ("no tooltip, legend", example.Spec with { Tooltip = null, Legend = null }),
            ("no tooltip, legend, series", example.Spec with { Tooltip = null, Legend = null, Series = [] }),
            ("only axes", example.Spec with { Tooltip = null, Legend = null, Series = [], Grid = null }),
        })
        {
            var (ns, _) = Timing.Measure(() =>
            {
                buffer.ResetWrittenCount();
                ChartSvg.Write(buffer, spec, example.Data, static (_, _) => null);
            });
            await Console.Out.WriteLineAsync($"{name}: {ns / 1000:F1} us, {buffer.WrittenCount} bytes");
        }
    }
}
