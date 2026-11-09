using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Data;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Svg;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class SunburstDesktopTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "SUNBURST-05")]
    public async Task TheDesktopSunburstLooksLikeRechartsAsync()
    {
        var example = await Reference.ExampleAsync("SimpleSunburst");
        var output = Path.Combine(Reference.Root, "scratch", "desktop-SimpleSunburst-" + Guid.NewGuid().ToString("N") + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

        var result = await desktop.Session.Dispatch(
            () => DesktopRender.Compare(example.Name, SpecControls.Create(example), Path.Combine(Reference.Root, "tools", "recharts", "out", "SimpleSunburst.png"), output),
            TestContext.Current.CancellationToken);
        File.Delete(output);

        PixelVerdict.Within(result);
    }

    [Fact]
    [Trait("AC", "SUNBURST-06")]
    public async Task HoveringASectorShowsItsTooltipAsync()
    {
        var example = await Reference.ExampleAsync("SimpleSunburst");
        var plot = (SunburstPlot)Painting.Plots.Compute(example.Spec, ChartData.From(example.Data, static (_, _) => null));
        var index = Array.FindIndex(plot.Hovered, hovered => plot.Nodes[hovered].Name == "third child");
        var node = plot.Nodes[plot.Hovered[index]];
        var (x, y) = Polar.Point(plot.Cx, plot.Cy, (node.InnerRadius + node.OuterRadius) / 2, node.Start + (node.Arc / 2));

        var (over, hole) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var window = new Window { Width = example.Spec.Width, Height = example.Spec.Height, Content = chart };
                window.Show();
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                window.MouseMove(new Point(x, y), RawInputModifiers.None);
                var first = chart.ActiveIndex;
                window.MouseMove(new Point(plot.Cx, plot.Cy), RawInputModifiers.None);
                var second = chart.ActiveIndex;
                window.Close();
                return (first, second);
            },
            TestContext.Current.CancellationToken);
        var markup = Reference.Pictor(example);

        Assert.Equal(index, over);
        Assert.True(hole < 0);
        Assert.Equal(plot.Hovered.Length, Regex.Count(markup, "pictor-hit\""));
        Assert.Equal(plot.Hovered.Length, Regex.Count(markup, "class=\"recharts-default-tooltip\""));
        Assert.Contains("<foreignObject x=\"372\" y=\"260\" width=\"1\" height=\"1\"><div class=\"recharts-default-tooltip\"><p></p><ul><li style=\"color:#000\">third child : 10</li></ul>", markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "SUNBURST-08")]
    public async Task ASunburstControlIsWrittenFromItsPropsAsync()
    {
        var example = await Reference.ExampleAsync("CustomSunburst");

        var (argument, property) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                chart.Measure(new Size(example.Spec.Width, example.Spec.Height));
                chart.Arrange(new Rect(0, 0, example.Spec.Width, example.Spec.Height));
                return (chart.WrittenSlot!.Argument, chart.WrittenSlot!.Property);
            },
            TestContext.Current.CancellationToken);
        var write = Slots.Writer(argument, static (_, _) => null!);
        var buffer = new ArrayBufferWriter<byte>();
        write(example.Data, buffer);

        Assert.Equal("Data", property);
        Assert.Equal(Reference.Pictor(example), Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}
