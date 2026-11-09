using System.Buffers;
using System.Text;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;
using Avalonia.Headless;

namespace Aioros.Pictor.Tests.Tips;

public sealed class TooltipTests(Desktop.DesktopSession desktop)
{
    private const double Measured = 0.5;

    [Fact]
    [Trait("AC", "TOOLTIP-01")]
    public async Task ABarChartWithoutASharedTooltipShowsOneTooltipPerBarAsync()
    {
        var pv = await Reference.HoverAsync("TooltipItemBarChart", 0);
        var uv = await Reference.HoverAsync("TooltipItemBarChart", 1);

        Assert.Empty(Reference.Differences(pv));
        Assert.Empty(Reference.Differences(uv));
        Assert.Equal(14, Count(pv.Pictor, "<rect class=\"pictor-hit\""));
    }

    [Fact]
    [Trait("AC", "TOOLTIP-02")]
    public async Task ALineChartKeepsTheAxisTooltipWhenNotSharedAsync()
    {
        var hover = await Reference.HoverAsync("TooltipSharedLineChart", 0);

        Assert.Equal(2, hover.State);
        Assert.Empty(Reference.Differences(hover));
    }

    [Fact]
    [Trait("AC", "TOOLTIP-03")]
    public async Task EntriesWithoutAValueAreLeftOutAsync()
    {
        Assert.Empty(Reference.Differences(await Reference.HoverAsync("TooltipGaps", 0)));
        Assert.Empty(Reference.Differences(await Reference.HoverAsync("TooltipGaps", 1)));
    }

    [Fact]
    [Trait("AC", "TOOLTIP-04")]
    public async Task EntriesWithoutAValueAreKeptWithoutFilterNullAsync()
    {
        Assert.Empty(Reference.Differences(await Reference.HoverAsync("TooltipKeepNulls", 0)));
        Assert.Empty(Reference.Differences(await Reference.HoverAsync("TooltipKeepNulls", 1)));
    }

    [Fact]
    [Trait("AC", "TOOLTIP-05")]
    public async Task TheSeparatorFormatsAndSorterShapeTheLinesAsRechartsAsync()
    {
        Assert.Empty(Reference.Differences(await Reference.HoverAsync("TooltipFormats", 0)));
        Assert.Empty(Reference.Differences(await Reference.HoverAsync("TooltipFormats", 1)));
    }

    [Fact]
    [Trait("AC", "TOOLTIP-06")]
    public async Task ContentLabelAndItemStylesAreWrittenOnTheBoxAsync()
    {
        var hover = await Reference.HoverAsync("TooltipFormats", 0);
        var tip = HoverComparison.Group(hover.Pictor, "pictor-tips", hover.State);

        Assert.Contains("<div class=\"recharts-default-tooltip\" style=\"background-color:#f8f8f8;border:2px solid #8884d8;border-radius:6px;padding:6px\"><p style=\"font-weight:bold\">Page: Page C</p>", tip, StringComparison.Ordinal);
        Assert.Contains("<li style=\"color:#82ca9d;color:#333\">uv = 2000 kg</li>", tip, StringComparison.Ordinal);
        Assert.Empty(Reference.Differences(hover));
        var example = await Reference.ExampleAsync("TooltipFormats");
        var (background, border) = await desktop.Session.Dispatch(
            () => Desktop.TooltipDesktopTests.Run(example, (window, chart) =>
            {
                window.MouseMove(new Avalonia.Point(210, 5), Avalonia.Input.RawInputModifiers.None);
                return (Desktop.TooltipDesktopTests.Pixel(chart, 224, 40), Desktop.TooltipDesktopTests.Pixel(chart, 220, 40));
            }),
            TestContext.Current.CancellationToken);
        Assert.Equal((0xf8, 0xf8, 0xf8), background);
        Assert.Equal((0x88, 0x84, 0xd8), border);
    }

    [Fact]
    [Trait("AC", "TOOLTIP-07")]
    public async Task ALineCursorTakesItsPropsAsync()
    {
        var hover = await Reference.HoverAsync("TooltipLineCursor", 0);

        Assert.Contains("style=\"stroke:red;fill:none;stroke-width:2;stroke-dasharray:5 5\"", HoverComparison.Group(hover.Pictor, "pictor-cursors", hover.State), StringComparison.Ordinal);
        Assert.Empty(Reference.Differences(hover));
    }

    [Fact]
    [Trait("AC", "TOOLTIP-08")]
    public async Task ABandCursorTakesItsPropsAndCanBeOffAsync()
    {
        var band = await Reference.HoverAsync("TooltipBarCursor", 0);
        var none = await Reference.HoverAsync("TooltipNoCursor", 0);

        Assert.Empty(Reference.Differences(band));
        Assert.Empty(Reference.Differences(none));
        Assert.DoesNotContain("recharts-tooltip-cursor\" d=", none.Pictor, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "TOOLTIP-09")]
    public async Task TheDefaultIndexIsShownUntilAStateIsHoveredAsync()
    {
        var example = await Reference.ExampleAsync("TooltipDefaultIndex");
        var markup = Reference.Pictor(example);
        var recharts = HoverComparison.RechartsTip(await Reference.RechartsAsync("TooltipDefaultIndex"));
        var pictor = HoverComparison.PictorTip(markup, 3);

        Assert.StartsWith("<div class=\"recharts-wrapper pictor-d3\"", markup, StringComparison.Ordinal);
        Assert.Contains(".pictor-d3:not(:has(.pictor-hover>:is(:hover,[tabindex=\"0\"]),:focus-visible)) .pictor-tk3{display:inline}.pictor-d3:not(:has(.pictor-hover>:is(:hover,[tabindex=\"0\"]),:focus-visible)) .pictor-c3{display:inline}", markup, StringComparison.Ordinal);
        Assert.Empty(HoverComparison.Compare(recharts, pictor));
        Assert.Empty(Reference.Differences(await Reference.HoverAsync("TooltipDefaultIndex", 0), Measured));
        var bars = Reference.Pictor(await Reference.ExampleAsync("TooltipDefaultIndexBar"));
        Assert.StartsWith("<div class=\"recharts-wrapper pictor-d1\"", bars, StringComparison.Ordinal);
        Assert.DoesNotContain("recharts-tooltip-cursor\" d=", bars, StringComparison.Ordinal);
        Assert.Empty(HoverComparison.Compare(HoverComparison.RechartsTip(await Reference.RechartsAsync("TooltipDefaultIndexBar")), HoverComparison.PictorTip(bars, 1), Measured));
    }

    [Fact]
    [Trait("AC", "TOOLTIP-10")]
    public async Task AnInactiveTooltipShowsTheActiveDotsOnlyAsync()
    {
        var hover = await Reference.HoverAsync("TooltipActiveFalse", 0);

        Assert.Equal(2, HoverComparison.PictorMarks(hover.Pictor, hover.State).Dots.Count);
        Assert.Empty(Reference.Differences(hover));
    }

    [Fact]
    [Trait("AC", "TOOLTIP-11")]
    public async Task AClickTooltipIsShownByTheFocusOfTheClickedCategoryAsync()
    {
        var hover = await Reference.HoverAsync("TooltipClick", 0);

        Assert.Contains("<g class=\"pictor-hover pictor-click\"><g tabindex=\"-1\" class=\"pictor-ck0\"><rect class=\"pictor-hit\"", hover.Pictor, StringComparison.Ordinal);
        Assert.Contains(".pictor-hover>:focus+g{display:inline}", hover.Pictor, StringComparison.Ordinal);
        Assert.Contains("svg:has(>g>.pictor-ck0:focus)>g>.pictor-c0,", hover.Pictor, StringComparison.Ordinal);
        Assert.Contains(".recharts-wrapper .recharts-surface>.pictor-hover.pictor-click>:hover:not(:focus)+g{display:none}", hover.Pictor, StringComparison.Ordinal);
        Assert.DoesNotContain(":hover+g{display:inline}", hover.Pictor, StringComparison.Ordinal);
        Assert.Empty(Reference.Differences(hover));
        Assert.Empty(Reference.Differences(await Reference.HoverAsync("TooltipClick", 1)));
        Assert.Empty(Reference.Differences(await Reference.HoverAsync("TooltipClick", 2), Measured));
    }

    [Fact]
    [Trait("AC", "TOOLTIP-12")]
    public async Task AFixedPositionPlacesTheBoxAsync() => Assert.Empty(Reference.Differences(await Reference.HoverAsync("TooltipPosition", 0)));

    [Fact]
    [Trait("AC", "TOOLTIP-13")]
    public async Task TheOffsetAndReversedDirectionPlaceTheBoxAsync()
    {
        Assert.Empty(Reference.Differences(await Reference.HoverAsync("TooltipOffset", 0), Measured));
        Assert.Empty(Reference.Differences(await Reference.HoverAsync("TooltipOffset", 1), Measured));
    }

    [Fact]
    [Trait("AC", "TOOLTIP-14")]
    public async Task ABoxAllowedToEscapeIsNotFlippedAsync() => Assert.Empty(Reference.Differences(await Reference.HoverAsync("TooltipEscape", 0)));

    [Fact]
    [Trait("AC", "TOOLTIP-15")]
    public async Task HiddenNoneAndRepeatedSeriesShapeThePayloadAsync() => Assert.Empty(Reference.Differences(await Reference.HoverAsync("TooltipHiddenSeries", 0)));

    [Fact]
    [Trait("AC", "TOOLTIP-17")]
    public async Task WrapperStylesClassesAndTheAccessibilityLayerAreWrittenAsync()
    {
        var formats = await Reference.ExampleAsync("TooltipFormats");
        var offset = await Reference.ExampleAsync("TooltipOffset");
        var spec = formats.Spec with { Tooltip = formats.Spec.Tooltip! with { WrapperStyle = "z-index:3", WrapperClassName = "box", LabelClassName = "title", AccessibilityLayer = true } };

        var markup = Write(spec, formats.Data);

        Assert.Matches("<foreignObject x=\"220\" y=\"0\" width=\"1\" height=\"[\\d.]+\" style=\"z-index:3\">" + System.Text.RegularExpressions.Regex.Escape("<div class=\"recharts-default-tooltip box\" style=\"background-color:#f8f8f8;border:2px solid #8884d8;border-radius:6px;padding:6px\" role=\"status\" aria-live=\"assertive\"><p class=\"title\" style=\"font-weight:bold\">"), markup);
        Assert.Equal(162.5, Demo.HoverComparison.PictorTip(markup, 0).Y);
        var specs = await desktop.Session.Dispatch(() => (SpecOf(SpecControls.TooltipOf(formats.Spec.Tooltip)), SpecOf(SpecControls.TooltipOf(offset.Spec.Tooltip!))), TestContext.Current.CancellationToken);
        Assert.Equal(formats.Spec.Tooltip, specs.Item1);
        Assert.Equal(offset.Spec.Tooltip, specs.Item2);
    }

    private static TooltipSpec? SpecOf(Tooltip tooltip)
    {
        var chart = new LineChart();
        chart.Elements.Add(tooltip);
        return chart.Spec(500, 300).Tooltip;
    }

    private static int Count(string markup, string text) => (markup.Length - markup.Replace(text, string.Empty, StringComparison.Ordinal).Length) / text.Length;

    private static string Write(ChartSpec spec, object? data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, data, static (_, _) => null);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
