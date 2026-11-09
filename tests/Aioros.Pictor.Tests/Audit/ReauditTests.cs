using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Data;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Svg;
using Aioros.Pictor.Theming;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Audit;

public sealed partial class ReauditTests(Desktop.DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "AUDIT-40")]
    public async Task TheDarkInactiveLegendItemIsReadableButFaintAsync()
    {
        var inactive = ChartTheme.Dark.LegendInactive;
        var resource = await desktop.Session.Dispatch(() => PictorKeys.Dictionary(ChartTheme.Dark)[PictorKeys.LegendInactive], TestContext.Current.CancellationToken);

        Assert.Equal("#ccc", ChartTheme.Light.LegendInactive);
        Assert.Equal(inactive, resource);
        Assert.InRange(Contrast(inactive, "#09090b"), 2.8, 3.5);
        Assert.InRange(Contrast(inactive, "#000000"), 2.8, 3.5);
    }

    [Fact]
    [Trait("AC", "AUDIT-42")]
    public async Task AScattersButtonsAreNamedByTheirPointsAsync()
    {
        var example = await Reference.ExampleAsync("SimpleScatterChart");
        string Labels(string? name)
        {
            var spec = example.Spec with { Series = [.. example.Spec.Series.Select(series => series with { Name = name, DataKey = string.Empty, ClickCommand = "pick" })] };
            var html = Slots.Render(ChartRenderer.Create(Specs.SpecJson.Write(spec), static (_, _) => null!), example.Data, new Dictionary<string, object?>()).Html;
            return string.Join('|', ButtonLabel().Matches(html).Select(match => match.Groups[1].Value));
        }

        var unnamed = Labels(null);
        var named = Labels("A school");

        Assert.StartsWith("stature 100cm, weight 200kg|stature 120cm, weight 100kg|", unnamed, StringComparison.Ordinal);
        Assert.Equal(6, unnamed.Split('|').Length);
        Assert.DoesNotContain("||", "|" + unnamed + "|", StringComparison.Ordinal);
        Assert.StartsWith("A school, stature 100cm, weight 200kg|", named, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AUDIT-43")]
    public async Task AHiddenSeriesLegendItemOffersNoToggleAsync()
    {
        var example = await Reference.ExampleAsync("LegendInactive");
        var spec = example.Spec with { Legend = example.Spec.Legend! with { Toggles = true } };
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, example.Data, static (_, _) => null);
        var html = Encoding.UTF8.GetString(buffer.WrittenSpan);
        var items = Regex.Matches(html, "<li class=\"recharts-legend-item [^\"]*\"[^>]*>(.*?)</li>", RegexOptions.Singleline).Select(match => match.Value).ToList();

        var toggled = example with { Spec = spec };
        var clicked = await desktop.Session.Dispatch(
            () => Desktop.TooltipDesktopTests.Run(toggled, (window, chart) =>
            {
                int[] Click(int index)
                {
                    var item = Plots.LegendOf(chart.Plot())!.Items[index];
                    var point = new Point(item.X + 5, item.Y + 5);
                    window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
                    window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
                    Dispatcher.UIThread.RunJobs();
                    return [.. chart.Concealed];
                }

                return (Click(0), Click(1));
            }),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, items.Count);
        Assert.Contains(" inactive\"", items[0], StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"checkbox\"", items[0], StringComparison.Ordinal);
        Assert.Contains("type=\"checkbox\"", items[1], StringComparison.Ordinal);
        Assert.Contains("color:#999", items[0], StringComparison.Ordinal);
        Assert.Empty(clicked.Item1);
        Assert.Equal([1], clicked.Item2);
    }

    [Fact]
    [Trait("AC", "AUDIT-45")]
    public async Task ABrushWithAGapMovesToRechartsSnappedWindowsAsync()
    {
        string?[] Moved(string key, long count, long written, long moved, object? start, object? end)
        {
            var decoded = new string?[2];
            var posted = ((((count * BrushFields.SlideBase) + written) * BrushFields.SlideBase) + moved).ToString(CultureInfo.InvariantCulture);
            return ChartRenderer.Decode(key, posted, [start, end], decoded) ? decoded : [];
        }

        var example = await Reference.ExampleAsync("BrushBarChart");
        IReadOnlyList<string> Keys(int gap)
        {
            var spec = example.Spec with { Brush = example.Spec.Brush! with { Fields = true, Gap = gap, StartIndex = 3, EndIndex = 17 } };
            var rendered = Slots.Render(ChartRenderer.Create(Specs.SpecJson.Write(spec), static (_, _) => null!), Reference.Rows(example).Take(31).ToList(), new Dictionary<string, object?>());
            return [.. rendered.Reserved.Select(reserved => reserved.Key)];
        }

        Assert.Equal(["brush-start", "brush-end", "brush-start/5", "brush-end/5", "brush-slide/5"], Keys(5));
        Assert.Equal(["brush-start", "brush-end", "brush-slide"], Keys(1));
        Assert.Equal(["0", "15"], Moved("brush-start/5", 31, 3, 4, 3, 17));
        Assert.Equal(["5", "10"], Moved("brush-end/5", 31, 15, 14, 5, 15));
        Assert.Equal(["5", "15"], Moved("brush-end/5", 31, 10, 11, 5, 10));
        Assert.Equal(["10", "20"], Moved("brush-slide/5", 31, 5, 7, 5, 15));
        Assert.Equal(["5", "30"], Moved("brush-end/5", 31, 15, 30, 5, 15));
        Assert.Empty(Moved("brush-end/5", 31, 15, 15, 5, 15));
        Assert.Empty(Moved("brush-start/5", 33, 30, 31, 30, 32));
        Assert.Empty(Moved("brush-start/x", 31, 3, 4, 3, 17));
    }

    [Fact]
    [Trait("AC", "AUDIT-46")]
    public async Task NoLayoutOfAResponsiveChartIsHiddenByDisplayNoneAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var spec = example.Spec with { Responsive = true, ResponsiveWidths = [360, 520] };
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, example.Data, static (_, _) => null);
        var html = Encoding.UTF8.GetString(buffer.WrittenSpan);
        const string Hidden = "{position:absolute!important;top:0;left:0;width:0!important;height:0!important;overflow:hidden;visibility:hidden}";

        Assert.DoesNotContain("display:none", html[..html.IndexOf("<div class=\"recharts-wrapper", StringComparison.Ordinal)], StringComparison.Ordinal);
        Assert.Contains(".pictor-responsive{container-type:inline-size;position:relative;width:100%;height:100%}", html, StringComparison.Ordinal);
        Assert.Contains("@container (width >= 440px){.pictor-responsive>.recharts-wrapper.pictor-w0-440" + Hidden + ".pictor-responsive>.recharts-wrapper.pictor-w0-440 *{visibility:hidden!important}}", html, StringComparison.Ordinal);
        Assert.Contains("@container (width < 440px){.pictor-responsive>.recharts-wrapper.pictor-w440-up" + Hidden + ".pictor-responsive>.recharts-wrapper.pictor-w440-up *{visibility:hidden!important}}", html, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AUDIT-47")]
    public async Task TheSlideFollowsADraggedTravellerAsync()
    {
        var example = await Reference.ExampleAsync("BrushWindowBarChart");
        var spec = example.Spec with { Brush = example.Spec.Brush! with { Fields = true } };
        var html = Slots.Render(ChartRenderer.Create(Specs.SpecJson.Write(spec), static (_, _) => null!), example.Data, new Dictionary<string, object?>()).Html;
        var brush = ChartLayout.Compute(spec, ChartData.From(example.Data, static (_, _) => null)).Brush!;
        string Px(double value) => Aioros.Graphs.Formatting.JsNumber.ToShortestString(Math.Round(value, 3));

        Assert.Contains(".recharts-surface:has(.pictor-b0:active,.pictor-b1:active,.pictor-bs:active) .recharts-brush-slide{visibility:hidden}", html, StringComparison.Ordinal);
        Assert.Contains("--pictor-sf:color-mix(in srgb,#8884d8 20%,transparent)", html, StringComparison.Ordinal);
        Assert.Contains(".pictor-brush-fields .pictor-b0:active::-webkit-slider-thumb{box-shadow:var(--pictor-sl) 0 0 var(--pictor-sl) var(--pictor-sf)}", html, StringComparison.Ordinal);
        Assert.Contains(".pictor-brush-fields .pictor-b1:active::-moz-range-thumb{box-shadow:calc(-1 * var(--pictor-sl)) 0 0 var(--pictor-sl) var(--pictor-sf)}", html, StringComparison.Ordinal);
        Assert.Contains("class=\"pictor-b0\" style=\"--pictor-c:inset(0 " + Px(brush.X + brush.Width - brush.EndX) + "px 0 0)\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"pictor-b1\" style=\"--pictor-c:inset(0 0 0 " + Px(brush.StartX + brush.Spec.TravellerWidth - brush.X) + "px)\"", html, StringComparison.Ordinal);
        Assert.Contains("linear-gradient(var(--pictor-sf),var(--pictor-sf)) center/calc(100% - 2 * var(--pictor-tw)) 100% no-repeat}", html, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AUDIT-48")]
    public async Task AResponsiveChartsLegendTogglesItsSeriesAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");
        var spec = example.Spec with { Responsive = true, Legend = example.Spec.Legend! with { Toggles = true } };
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, example.Data, static (_, _) => null);
        var html = Encoding.UTF8.GetString(buffer.WrittenSpan);

        Assert.Equal(2, Regex.Count(html, "<g class=\"recharts-legend-item legend-item-\\d\"><svg [^>]*>.*?</svg><text [^>]*>[^<]*</text><foreignObject [^>]*><label><input type=\"checkbox\" hidden class=\"pictor-t[01]\"></label></foreignObject></g>", RegexOptions.Singleline));
        Assert.Contains("<g class=\"pictor-s0\">", html, StringComparison.Ordinal);
        Assert.Contains("<g class=\"pictor-s1\">", html, StringComparison.Ordinal);
        Assert.Contains(".recharts-wrapper:has(.pictor-t0:checked) .pictor-s0", html, StringComparison.Ordinal);
        Assert.Contains(".recharts-legend-item:has(:checked) text.recharts-legend-item-text{fill:var(--pictor-inactive,#ccc)}", html, StringComparison.Ordinal);
    }

    private static double Contrast(string first, string second)
    {
        static double Luminance(string hex)
        {
            static double Channel(int value) => value / 255.0 is var c && c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            var full = hex.Length == 4 ? string.Concat(hex[1], hex[1], hex[2], hex[2], hex[3], hex[3]) : hex[1..];
            var rgb = Convert.FromHexString(full);
            return (0.2126 * Channel(rgb[0])) + (0.7152 * Channel(rgb[1])) + (0.0722 * Channel(rgb[2]));
        }

        var (a, b) = (Luminance(first), Luminance(second));
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    [GeneratedRegex("<button[^>]* aria-label=\"([^\"]*)\"")]
    private static partial Regex ButtonLabel();
}
