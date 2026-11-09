using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;
using Aioros.Pictor.Tests.Desktop;
using Avalonia.Media;

namespace Aioros.Pictor.Tests.Charts;

public sealed partial class PaintTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "PAINT-01")]
    public async Task GradientFillsAreWrittenAsRechartsWritesThemAsync()
    {
        var verdict = await Reference.CompareAsync("AreaChartGradient");
        var markup = Reference.Pictor(await Reference.ExampleAsync("AreaChartGradient"));

        Assert.Empty(verdict.Differences);
        Assert.Contains("<defs><linearGradient id=\"colorUv\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"5%\" stop-color=\"#8884d8\" stop-opacity=\"0.8\"/><stop offset=\"95%\" stop-color=\"#8884d8\" stop-opacity=\"0\"/></linearGradient><linearGradient id=\"colorPv\"", markup, StringComparison.Ordinal);
        Assert.Contains("fill=\"url(#colorUv)\"", markup, StringComparison.Ordinal);
        Assert.Contains("fill=\"url(#colorPv)\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "PAINT-02")]
    public async Task AvaloniaBrushesAreTheSeriesPaintsAsync()
    {
        var verdict = await ReferenceDesktop.CompareXamlAsync(desktop, "AreaChartGradient");
        var spec = await ReferenceDesktop.SpecAsync(desktop, "AreaChartGradient");
        var (shared, solid) = await desktop.Session.Dispatch(
            () =>
            {
                var brush = Fade("#8884d8");
                var chart = new AreaChart { Width = 400, Height = 200 };
                chart.Elements.Add(new Area { DataKey = "Uv", Fill = brush });
                chart.Elements.Add(new Area { DataKey = "Pv", Fill = Fade("#8884d8") });
                chart.Elements.Add(new Area { DataKey = "Amt", Fill = Fade("#82ca9d"), Stroke = new SolidColorBrush(Color.Parse("#82ca9d")) });
                chart.Elements.Add(new Area { DataKey = "Cnt", Fill = new SolidColorBrush(Color.Parse("#82ca9d"), 0.5), Stroke = "red" });
                var built = chart.Spec(400, 200);
                return (built, built.Series);
            },
            TestContext.Current.CancellationToken);

        Assert.Empty(verdict.Differences);
        Assert.Equal(2, spec.Gradients.Length);
        Assert.All(spec.Gradients, gradient => Assert.Matches("^pictor-gradient-[0-9a-f]{8}$", gradient.Id));
        Assert.Equal(2, shared.Gradients.Length);
        Assert.Equal(solid[0].Fill, solid[1].Fill);
        Assert.NotEqual(solid[0].Fill, solid[2].Fill);
        Assert.Equal("#82ca9d", solid[2].Stroke);
        Assert.Equal("#82ca9d80", solid[3].Fill);
        Assert.Equal("red", solid[3].Stroke);
    }

    [Fact]
    [Trait("AC", "PAINT-03")]
    public async Task TheDesktopDrawsGradientsAsRechartsDoesAsync()
    {
        var area = await ReferenceDesktop.MeasureAsync(desktop, "AreaChartGradient");

        PixelVerdict.Within(area);
    }

    [Fact]
    [Trait("AC", "PAINT-04")]
    public async Task EachResponsiveLayoutDefinesItsOwnGradientAsync()
    {
        var example = await Reference.ExampleAsync("AreaChartGradient");
        var responsive = Write(example.Spec with { Responsive = true, ResponsiveWidths = [360, 720] }, example.Data);
        var single = Write(example.Spec, example.Data);

        Assert.Equal(["colorUv-pictor-w0-540", "colorPv-pictor-w0-540", "colorUv-pictor-w540-up", "colorPv-pictor-w540-up"], GradientId().Matches(responsive).Select(match => match.Groups[1].Value));
        Assert.Contains("fill=\"url(#colorUv-pictor-w0-540)\"", responsive, StringComparison.Ordinal);
        Assert.Contains("fill=\"url(#colorUv-pictor-w540-up)\"", responsive, StringComparison.Ordinal);
        Assert.Equal(["colorUv", "colorPv"], GradientId().Matches(single).Select(match => match.Groups[1].Value));
    }

    private static LinearGradientBrush Fade(string color)
    {
        var parsed = Color.Parse(color);
        return new LinearGradientBrush
        {
            StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
            EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.FromArgb(204, parsed.R, parsed.G, parsed.B), 0.05), new GradientStop(Color.FromArgb(0, parsed.R, parsed.G, parsed.B), 0.95) },
        };
    }

    private static string Write(ChartSpec spec, object? data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, data, static (_, _) => null);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    [GeneratedRegex("<linearGradient id=\"([^\"]+)\"")]
    private static partial Regex GradientId();
}
