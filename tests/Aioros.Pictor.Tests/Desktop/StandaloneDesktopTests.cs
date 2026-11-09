using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Svg;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class StandaloneDesktopTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "FUNNEL-04")]
    public async Task AFunnelsOwnItemsTravelInTheSpecAsync()
    {
        var example = await Reference.ExampleAsync("FunnelChart");

        var (argument, property) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                chart.Measure(new Size(700, 432));
                chart.Arrange(new Rect(0, 0, 700, 432));
                return (chart.WrittenSlot!.Argument, chart.WrittenSlot!.Property);
            },
            TestContext.Current.CancellationToken);
        var write = Slots.Writer(argument, static (_, _) => null!);
        var buffer = new ArrayBufferWriter<byte>();
        write(null, buffer);

        Assert.Equal("Data", property);
        Assert.Equal(Reference.Pictor(example), Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Theory]
    [InlineData("FunnelChart")]
    [Trait("AC", "FUNNEL-05")]
    public async Task TheDesktopFunnelLooksLikeRechartsAsync(string name) => await LooksLikeRechartsAsync(name);

    [Theory]
    [InlineData("SimpleTreemap")]
    [Trait("AC", "TREEMAP-05")]
    public async Task TheDesktopTreemapLooksLikeRechartsAsync(string name) => await LooksLikeRechartsAsync(name);

    [Theory]
    [InlineData("SimpleSankey")]
    [Trait("AC", "SANKEY-05")]
    public async Task TheDesktopSankeyLooksLikeRechartsAsync(string name) => await LooksLikeRechartsAsync(name);

    [Fact]
    [Trait("AC", "FUNNEL-06")]
    public async Task HoveringATrapezoidShowsItsTooltipAsync()
    {
        var (over, outside) = await HoverAsync("FunnelChart", new Point(330, 216), new Point(20, 420));

        Assert.Equal(2, over);
        Assert.True(outside < 0);
        Assert.Equal((5, 5), await HitsAndTipsAsync("FunnelChart"));
    }

    [Fact]
    [Trait("AC", "TREEMAP-06")]
    public async Task HoveringALeafShowsItsTooltipAsync()
    {
        var example = await Reference.ExampleAsync("ColorPanelTreemap");
        var plot = (Layout.TreemapPlot)Painting.Plots.Compute(example.Spec, Pictor.Data.ChartData.From(example.Data, static (_, _) => null));
        var leaf = Array.FindIndex(plot.Hovered, node => node.Name == "Data");
        var node = plot.Hovered[leaf];

        var (over, _) = await HoverAsync("ColorPanelTreemap", new Point(node.X + (node.Width / 2), node.Y + (node.Height / 2)), new Point(-1, -1));

        Assert.Equal(leaf, over);
        Assert.Equal(plot.Hovered.Length, (await HitsAndTipsAsync("ColorPanelTreemap")).Hits);
        Assert.All(plot.Hovered, hovered => Assert.Null(hovered.Children));
    }

    [Fact]
    [Trait("AC", "SANKEY-06")]
    public async Task HoveringANodeOrALinkShowsItsTooltipAsync()
    {
        var example = await Reference.ExampleAsync("SimpleSankey");
        var plot = (Layout.SankeyPlot)Painting.Plots.Compute(example.Spec, Pictor.Data.ChartData.From(example.Data, static (_, _) => null));
        var pageClick = plot.Nodes[2];
        var nodePoint = new Point(pageClick.X + 5 + (pageClick.Dx / 2), pageClick.Y + 5 + (pageClick.Dy / 2));
        var link = plot.Links[3];
        var target = plot.Nodes[link.Target];
        var linkPoint = new Point((pageClick.X + pageClick.Dx + target.X) / 2, ((pageClick.Y + link.Sy + target.Y + link.Ty + link.Dy) / 2) + 5);

        var (node, linked) = await HoverAsync("SimpleSankey", nodePoint, linkPoint);

        Assert.Equal(plot.Links.Length + 2, node);
        Assert.Equal(3, linked);
        Assert.Equal((9, 9), await HitsAndTipsAsync("SimpleSankey"));
    }

    private async Task LooksLikeRechartsAsync(string name)
    {
        var example = await Reference.ExampleAsync(name);
        var output = Path.Combine(Reference.Root, "scratch", "desktop-" + name + "-" + Guid.NewGuid().ToString("N") + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

        var result = await desktop.Session.Dispatch(
            () => DesktopRender.Compare(example.Name, SpecControls.Create(example), ReferenceFiles.Of(Reference.Root, name + ".png"), output),
            TestContext.Current.CancellationToken);
        File.Delete(output);

        PixelVerdict.Within(result);
    }

    private async Task<(int First, int Second)> HoverAsync(string name, Point first, Point second)
    {
        var example = await Reference.ExampleAsync(name);
        return await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var window = new Window { Width = example.Spec.Width, Height = example.Spec.Height, Content = chart };
                window.Show();
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                window.MouseMove(first, RawInputModifiers.None);
                var a = chart.ActiveIndex;
                window.MouseMove(second, RawInputModifiers.None);
                var b = chart.ActiveIndex;
                window.Close();
                return (a, b);
            },
            TestContext.Current.CancellationToken);
    }

    private static async Task<(int Hits, int Tips)> HitsAndTipsAsync(string name)
    {
        var markup = Reference.Pictor(await Reference.ExampleAsync(name));
        var hits = Regex.Count(markup, "pictor-hit\"");
        var tips = Regex.Count(markup, "class=\"recharts-default-tooltip\"");
        return (hits, tips);
    }
}
