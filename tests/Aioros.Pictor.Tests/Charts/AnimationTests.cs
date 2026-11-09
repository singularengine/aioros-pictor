using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Charts;

public sealed partial class AnimationTests(Desktop.DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "ANIMATION-01")]
    public async Task TheReferenceExamplesStayStaticAsync()
    {
        var names = Directory.EnumerateFiles(Path.Combine(Reference.Root, "tools", "recharts", "examples"), "*.json").Select(Path.GetFileNameWithoutExtension).ToList();

        foreach (var name in names)
        {
            var markup = Reference.Pictor(await Reference.ExampleAsync(name!));

            Assert.DoesNotContain(" pictor-a\"", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("pictor-late", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("@keyframes", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("pathLength", markup, StringComparison.Ordinal);
        }

        Assert.True(names.Count >= 40);
    }

    [Fact]
    [Trait("AC", "ANIMATION-02")]
    public async Task CartesianSeriesAnimateAsRechartsDoesAsync()
    {
        var line = await AnimatedAsync("SimpleLineChart");
        var dashed = await AnimatedAsync("DashedLineChart");
        var area = await AnimatedAsync("SimpleAreaChart");
        var bars = await AnimatedAsync("PositiveAndNegativeBarChart");
        var vertical = await AnimatedAsync("VerticalBarChart");
        var scatter = await AnimatedAsync("SimpleScatterChart");

        Assert.Equal(2, Count(line, "class=\"recharts-layer recharts-line pictor-a\""));
        Assert.Equal(2, Count(line, "class=\"recharts-curve recharts-line-curve pictor-draw\" pathLength=\"1\""));
        Assert.Equal(2, Count(line, "recharts-line-dots pictor-late"));
        Assert.Equal(2, Count(dashed, "<path class=\"pictor-dash-mask\" pathLength=\"1\""));
        Assert.Equal(2, Count(area, "pictor-reveal\""));
        Assert.Contains("class=\"recharts-layer recharts-bar pictor-a\" style=\"--pb:0ms;--pd:400ms;--pe:ease;\"", bars, StringComparison.Ordinal);
        Assert.Equal(9, Count(bars, "recharts-rectangle pictor-grow-b"));
        Assert.Equal(5, Count(bars, "recharts-rectangle pictor-grow-t"));
        Assert.Equal(14, Count(vertical, "recharts-rectangle pictor-grow-l"));
        Assert.Contains("--pe:linear", scatter, StringComparison.Ordinal);
        Assert.Equal(6, Count(scatter, "recharts-symbols pictor-scale-area"));
        var example = await Reference.ExampleAsync("SimpleScatterChart");
        var (early, rest, centers) = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new Desktop.DesktopFrames.ManualClock();
                var (chart, host) = Desktop.DesktopFrames.Hosted(example, animated: true, clock);
                var points = ((Layout.ChartLayout)chart.Plot()).Series[0];
                Desktop.DesktopFrames.Frame(host);
                clock.Advance(180);
                var quarter = Desktop.DesktopFrames.Frame(host);
                clock.Advance(1000);
                var done = Desktop.DesktopFrames.Frame(host);
                var at = points.X.Zip(points.Y, (x, y) => ((int)Math.Round(x), (int)Math.Round(y))).ToArray();
                return (quarter, done, at);
            },
            TestContext.Current.CancellationToken);
        Assert.All(centers, center =>
        {
            var share = (double)Filled(early, center) / Filled(rest, center);
            Assert.InRange(share, 0.12, 0.4);
        });
        foreach (var markup in (string[])[line, dashed, area, bars, vertical, scatter])
        {
            Assert.All(Styles().Matches(markup).Where(style => style.Value.Contains("pictor-", StringComparison.Ordinal) && style.Value.Contains("animation", StringComparison.Ordinal)), style => Assert.StartsWith("<style>@media (prefers-reduced-motion:no-preference){", style.Value.Replace("<style>@property --pictor-n{syntax:'&lt;angle&gt;';inherits:false;initial-value:360deg}", "<style>", StringComparison.Ordinal).Replace("<style>@property --pictor-s{syntax:'&lt;number&gt;';inherits:false;initial-value:1}", "<style>", StringComparison.Ordinal), StringComparison.Ordinal));
        }
    }

    /// <summary>How many pixels of the 15-pixel square around a symbol's center are its fill (<c>#8884d8</c>, within 40): its drawn area.</summary>
    private static int Filled(byte[] frame, (int X, int Y) center) =>
        Enumerable.Range(-7, 15).Sum(dy => Enumerable.Range(-7, 15).Count(dx => Desktop.DesktopFrames.Pixel(frame, center.X + dx, center.Y + dy) is var (b, g, r) && Math.Abs(b - 216) < 40 && Math.Abs(g - 132) < 40 && Math.Abs(r - 136) < 40));

    [Fact]
    [Trait("AC", "ANIMATION-03")]
    public async Task PolarAndStandaloneChartsAnimateAsRechartsDoesAsync()
    {
        var pie = await AnimatedAsync("TwoLevelPieChart");
        var straight = await AnimatedAsync("StraightAnglePieChart");
        var radar = await AnimatedAsync("SimpleRadarChart");
        var radial = await AnimatedAsync("SimpleRadialBarChart");
        var funnel = await AnimatedAsync("FunnelChart");
        var treemap = await AnimatedAsync("SimpleTreemap");
        var again = await AnimatedAsync("SimpleTreemap");
        var sankey = await AnimatedAsync("SimpleSankey");

        Assert.Equal(2, Count(pie, "class=\"recharts-layer recharts-pie pictor-a\" style=\"--pb:400ms;--pd:1500ms;--pe:ease;\""));
        Assert.Equal(2, Count(pie, "<path class=\"pictor-arc\""));
        Assert.Contains("<path class=\"pictor-arc\" stroke=\"#8884d8\" stroke-width=\"80\" pathLength=\"360\" style=\"--a:0;--b:0;--l:180\" d=\"M160,200A40,40,0,0,1,240,200A40,40,0,0,1,160,200\"/>", straight, StringComparison.Ordinal);
        Assert.Matches("class=\"recharts-polygon pictor-center\" style=\"transform-origin:250px 2\\d\\d(\\.\\d+)?px\"", radar);
        Assert.Equal(7, Count(radial, "<path class=\"pictor-arc\""));
        Assert.Equal(7, Arcs().Matches(radial).Select(match => match.Groups[1].Value).Distinct().Count());
        Assert.Contains("recharts-radial-bar-sectors pictor-late", radial, StringComparison.Ordinal);
        Assert.DoesNotContain("mask-image", pie + radial, StringComparison.Ordinal);
        Assert.Equal(5, Count(funnel, "class=\"recharts-trapezoid pictor-scale\""));
        Assert.Contains("--pb:400ms", funnel, StringComparison.Ordinal);
        Assert.Contains("--pe:linear", treemap, StringComparison.Ordinal);
        Assert.True(Count(treemap, "pictor-slide\" style=\"--tx:") > 10);
        Assert.Equal(treemap, again);
        Assert.DoesNotContain("prefers-reduced-motion", sankey, StringComparison.Ordinal);
        Assert.DoesNotContain(" pictor-a", sankey, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "ANIMATION-04")]
    public async Task ASeriesOwnTimingIsWrittenOnItsLayerAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");
        var spec = example.Spec with
        {
            Series = [example.Spec.Series[0] with { Animation = new AnimationSpec { Begin = 200, Duration = 800, Easing = AnimationEasing.EaseOut } }, example.Spec.Series[1] with { Animation = null }],
        };

        var markup = Write(spec, example.Data);

        Assert.Contains("recharts-line pictor-a\" style=\"--pb:200ms;--pd:800ms;--pe:ease-out;\"", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"recharts-layer recharts-line pictor-a\" data-aioros-morph-path=\"line monotone-x\" data-aioros-morph-map=\"ratio\" data-aioros-morph-duration=\"1500\" data-aioros-morph-delay=\"80\">", markup, StringComparison.Ordinal);
        Assert.Equal(1, Count(markup, "@keyframes pictor-draw"));
    }

    [Fact]
    [Trait("AC", "ANIMATION-23")]
    public async Task EveryTreemapNodeSlidesInOnItsOwnAsync()
    {
        var treemap = await AnimatedAsync("SimpleTreemap");
        var nodes = TreemapNodes().Matches(treemap).ToList();
        var layers = Count(treemap, "class=\"recharts-layer recharts-treemap-depth-");

        Assert.DoesNotMatch("recharts-treemap-depth-\\d+[^\"]*pictor-slide", treemap);
        Assert.Equal(layers, nodes.Count);
        Assert.Equal("0", nodes[0].Groups[1].Value);
        for (var i = 0; i < nodes.Count; i++)
        {
            var offset = int.Parse(nodes[i].Groups[2].Value, CultureInfo.InvariantCulture);
            var width = nodes[i].Groups[5].Success ? double.Parse(nodes[i].Groups[5].Value, CultureInfo.InvariantCulture) : 0;
            Assert.Equal(Math.Truncate(((((i + 1) * 0.6180339887498949) % 1 * 2) - 1) * width), offset);
            Assert.True(Math.Abs(offset) < width || (width == 0 && offset == 0), $"node {i}: {offset} within {width}");
        }

        Assert.Contains("@keyframes pictor-slide{from{transform:translate(var(--tx),var(--tx))}}", treemap, StringComparison.Ordinal);
        Assert.Contains(".pictor-slide{animation:pictor-slide var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}", treemap, StringComparison.Ordinal);
        Assert.Contains("recharts-treemap-depth-0 pictor-a\" style=\"--pb:0ms;--pd:1500ms;--pe:linear;\"", treemap, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "ANIMATION-24")]
    public async Task TreemapNodesSlideOnTheirOwnOnTheDesktopAsync()
    {
        var example = await Reference.ExampleAsync("SimpleTreemap");
        var expected = TreemapNodes().Matches(await AnimatedAsync("SimpleTreemap"))
            .Where(node => node.Groups[5].Success && Number(node.Groups[1]) <= 2)
            .Select(node => (Bounds: new Avalonia.Rect(Number(node.Groups[3]), Number(node.Groups[4]), Number(node.Groups[5]), Number(node.Groups[6])), Shift: Number(node.Groups[2]) / 2))
            .ToList();
        var (half, end, still) = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new Desktop.DesktopFrames.ManualClock();
                var (chart, host) = Desktop.DesktopFrames.Hosted(example, animated: true, clock);
                ((Treemap)chart).IsAnimationActive = true;
                Desktop.DesktopFrames.Frame(host);
                clock.Advance(830);
                var drawn = Drawn(chart);
                clock.Advance(782);
                return (drawn, Desktop.DesktopFrames.Frame(host), Desktop.DesktopFrames.Frame(Desktop.DesktopFrames.Hosted(example, animated: false, clock).Host));
            },
            TestContext.Current.CancellationToken);

        Assert.All(expected, node =>
        {
            var drawn = Assert.Single(half, rect => rect.Bounds == node.Bounds);
            Assert.Equal((node.Shift, node.Shift), (drawn.X, drawn.Y));
        });
        Assert.True(expected.Count > 40);
        Assert.Equal(still, end);
    }

    private static double Number(Group group) => double.Parse(group.Value, CultureInfo.InvariantCulture);

    /// <summary>Each shape filled with the treemap's color the chart draws now: its own bounds and the translation the layers around it apply.</summary>
    private static List<(Avalonia.Rect Bounds, double X, double Y)> Drawn(Avalonia.Visual chart)
    {
        var group = new Avalonia.Media.DrawingGroup();
        using (var context = group.Open())
        {
            chart.Render(context);
        }

        var drawn = new List<(Avalonia.Rect Bounds, double X, double Y)>();
        Walk(group, Avalonia.Matrix.Identity, drawn);
        return drawn;
    }

    private static void Walk(Avalonia.Media.Drawing drawing, Avalonia.Matrix around, List<(Avalonia.Rect Bounds, double X, double Y)> drawn)
    {
        switch (drawing)
        {
            case Avalonia.Media.DrawingGroup group:
                var matrix = (group.Transform?.Value ?? Avalonia.Matrix.Identity) * around;
                foreach (var child in group.Children)
                {
                    Walk(child, matrix, drawn);
                }

                break;
            case Avalonia.Media.GeometryDrawing { Brush: Avalonia.Media.ISolidColorBrush { Color: var color }, Geometry: { } geometry } when color == Avalonia.Media.Color.Parse("#8884d8"):
                drawn.Add((geometry.Bounds, around.M31, around.M32));
                break;
        }
    }

    private static async Task<string> AnimatedAsync(string name)
    {
        var example = await Reference.ExampleAsync(name);
        return Write(Demo.ExampleAnimations.Animated(example.Spec), example.Data);
    }

    private static string Write(ChartSpec spec, object? data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, data, static (_, _) => null);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static int Count(string markup, string text) => markup.Split(text).Length - 1;

    [GeneratedRegex("<style>.*?</style>")]
    private static partial Regex Styles();

    [GeneratedRegex("pictor-arc\"[^>]*--l:([\\d.]+)")]
    private static partial Regex Arcs();

    [GeneratedRegex("<g class=\"recharts-layer recharts-treemap-depth-(\\d+)[^\"]*\"[^>]*>(?:<style>.*?</style>)*<g class=\"recharts-layer pictor-slide\" style=\"[^\"]*--tx:(-?\\d+)px\"><g>(?:<path [^>]*d=\"M([-\\d.]+),([-\\d.]+)h([\\d.]+)v([\\d.]+))?")]
    private static partial Regex TreemapNodes();
}
