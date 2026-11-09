using Aioros.Pictor.Demo;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class KeyboardDesktopTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "KEYBOARD-01")]
    public Task TheArrowKeysMoveALineChartsTooltipAsync() => AssertArrowsAsync("SimpleLineChart", 0);

    [Fact]
    [Trait("AC", "KEYBOARD-02")]
    public Task TheArrowKeysMoveAnAreaChartsTooltipAsync() => AssertArrowsAsync("SimpleAreaChart", 0);

    [Fact]
    [Trait("AC", "KEYBOARD-03")]
    public Task TheArrowKeysMoveABarChartsTooltipAsync() => AssertArrowsAsync("SimpleBarChart", 0);

    [Fact]
    [Trait("AC", "KEYBOARD-04")]
    public Task TheArrowKeysMoveAComposedChartsTooltipAsync() => AssertArrowsAsync("LineBarAreaComposedChart", 0);

    [Fact]
    [Trait("AC", "KEYBOARD-05")]
    public Task FocusStartsAtTheTooltipsDefaultIndexAsync() => AssertArrowsAsync("TooltipDefaultIndex", 3);

    [Theory]
    [Trait("AC", "KEYBOARD-06")]
    [InlineData("VerticalBarChart")]
    [InlineData("TwoSimplePieChart")]
    [InlineData("SpecifiedDomainRadarChart")]
    [InlineData("SimpleRadialBarChart")]
    [InlineData("FunnelChart")]
    public async Task OtherChartsTakeFocusButTheKeysMoveNothingAsync(string name)
    {
        var example = await Reference.ExampleAsync(name);

        var (focused, states) = await desktop.Session.Dispatch(
            () =>
            {
                var (chart, window) = Shown(example);
                var focus = chart.IsFocused;
                var seen = new List<int> { chart.ActiveIndex };
                foreach (var key in (Key[])[Key.Right, Key.Left])
                {
                    window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
                    seen.Add(chart.ActiveIndex);
                }

                window.Close();
                return (focus, seen);
            },
            TestContext.Current.CancellationToken);

        Assert.True(focused);
        Assert.All(states, state => Assert.True(state < 0, $"no tooltip from the keyboard ({state})"));
    }

    [Fact]
    [Trait("AC", "KEYBOARD-07")]
    public async Task EscapeHidesTheTooltipsBoxUntilItMovesAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");
        var markup = Reference.Pictor(example);
        var first = HoverComparison.PictorTip(markup, 1);
        var second = HoverComparison.PictorTip(markup, 2);

        var (shown, escaped, moved, layout) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                chart.AccessibilityLayer = true;
                var window = new Window { Width = example.Spec.Width, Height = example.Spec.Height, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var plain = HoverPixels.Frame(chart);
                chart.Focus();
                Dispatcher.UIThread.RunJobs();
                window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
                var focused = new HoverPixels(plain, HoverPixels.Frame(chart), (int)example.Spec.Width);
                window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                var hidden = new HoverPixels(plain, HoverPixels.Frame(chart), (int)example.Spec.Width);
                window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
                var next = new HoverPixels(plain, HoverPixels.Frame(chart), (int)example.Spec.Width);
                var plot = (Layout.ChartLayout)chart.Plot();
                window.Close();
                return (focused, hidden, next, plot);
            },
            TestContext.Current.CancellationToken);
        var x = (int)Math.Round(layout.Categories!.Category(1));
        var top = (int)layout.Top + 1;
        var bottom = (int)(layout.Top + layout.PlotHeight) - 1;

        Assert.True(shown.Written((int)first.X + 2, (int)first.Y + 2, (int)first.X + 110, (int)first.Y + 80) > 300, "the focused category's box shows");
        Assert.True(escaped.Written((int)first.X + 2, (int)first.Y + 2, (int)first.X + 110, (int)first.Y + 80) == 0, "Escape hides the box");
        Assert.True(escaped.Painted((x, top), (x, bottom), "#ccc") > 0.8, "the cursor stays");
        Assert.All(layout.Series, series => Assert.True(escaped.Near(x, (int)Math.Round(series.Y[1]), series.Color), $"the active dot of {series.Name} stays"));
        Assert.True(moved.Written((int)second.X + 2, (int)second.Y + 2, (int)second.X + 110, (int)second.Y + 80) > 300, "the box shows again at the next category");
    }

    private async Task AssertArrowsAsync(string name, int start)
    {
        var example = await Reference.ExampleAsync(name);

        var states = await desktop.Session.Dispatch(
            () =>
            {
                var (chart, window) = Shown(example);
                var seen = new List<int> { chart.ActiveIndex };
                foreach (var key in (Key[])[Key.Right, Key.Right, Key.Left, Key.Down, Key.Home, Key.End])
                {
                    window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
                    seen.Add(chart.ActiveIndex);
                }

                for (var i = 0; i < 20; i++)
                {
                    window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.None, null);
                }

                seen.Add(chart.ActiveIndex);
                seen.Add(chart.Plot().HoverCount - 1);
                window.Close();
                return seen;
            },
            TestContext.Current.CancellationToken);

        Assert.Equal([start, start + 1, start + 2, start + 1, start + 1, start + 1, start + 1], states[..7]);
        Assert.Equal(states[8], states[7]);
    }

    private static (Chart Chart, Window Window) Shown(RechartsExample example)
    {
        var chart = SpecControls.Create(example);
        chart.AccessibilityLayer = true;
        var window = new Window { Width = example.Spec.Width, Height = example.Spec.Height, Content = chart };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        chart.Focus();
        Dispatcher.UIThread.RunJobs();
        return (chart, window);
    }


}
