using System.Collections.ObjectModel;
using Aioros.Pictor.Demo;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class DesktopChartTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "DESKTOP-01")]
    public async Task TheDesktopChartLooksLikeRechartsAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var output = Path.Combine(Path.GetTempPath(), "pictor-desktop-" + Guid.NewGuid().ToString("N") + ".png");

        var result = await desktop.Session.Dispatch(
            () => DesktopRender.Compare(example.Name, SpecControls.Create(example), Path.Combine(Reference.Root, "tools", "recharts", "out", "SimpleBarChart.png"), output),
            TestContext.Current.CancellationToken);
        File.Delete(output);

        PixelVerdict.Within(result);
    }

    [Fact]
    [Trait("AC", "DESKTOP-02")]
    public async Task HoveringACategoryShowsItsTooltipAsync()
    {
        var example = await Reference.ExampleAsync("DetailsFollowLineChart");
        var recharts = await Reference.RechartsAsync("DetailsFollowLineChart.0");
        var tip = HoverComparison.RechartsTip(recharts);
        var marks = HoverComparison.RechartsMarks(recharts);
        var cursor = MarkupComparison.Parse($"<svg><path d=\"{marks.Cursors[0].Attributes["d"]}\"/></svg>");

        var (active, left, hover, same) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var window = new Window { Width = example.Spec.Width, Height = example.Spec.Height, Content = chart };
                window.Show();
                var before = HoverPixels.Frame(chart);
                window.MouseMove(new Point(150, 40), RawInputModifiers.None);
                var hovered = chart.ActiveIndex;
                var pixels = new HoverPixels(before, HoverPixels.Frame(chart), (int)chart.Bounds.Width);
                window.MouseMove(new Point(example.Spec.Width - 1, example.Spec.Height - 1), RawInputModifiers.None);
                window.MouseMove(new Point(example.Spec.Width + 50, example.Spec.Height + 50), RawInputModifiers.None);
                var outside = chart.ActiveIndex;
                var unchanged = HoverPixels.Frame(chart).AsSpan().SequenceEqual(before);
                window.Close();
                return (hovered, outside, pixels, unchanged);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(1, active);
        Assert.Equal("M145,5L145,248", cursor[1].Attributes["d"]);
        Assert.True(hover.Painted((145, 6), (145, 247), "#ccc") > 0.8, "the cursor line is drawn down the category (the active dots over it)");
        Assert.Equal(2, marks.Dots.Count);
        Assert.All(marks.Dots, dot => Assert.True(hover.Near((int)Math.Round(double.Parse(dot.Attributes["cx"], System.Globalization.CultureInfo.InvariantCulture)), (int)Math.Round(double.Parse(dot.Attributes["cy"], System.Globalization.CultureInfo.InvariantCulture)), dot.Attributes["fill"]), $"active dot {dot.Attributes["fill"]} at ({dot.Attributes["cx"]}, {dot.Attributes["cy"]})"));
        Assert.True(hover.Near((int)tip.X, (int)tip.Y + 20, "#ccc"), $"the box's left border at Recharts' ({tip.X}, {tip.Y}): {hover.At((int)tip.X, (int)tip.Y + 20)}");
        Assert.True(hover.Written((int)tip.X + 2, (int)tip.Y + 2, (int)tip.X + 110, (int)tip.Y + 80) > 300, $"the box holds its label and two lines: {hover.Written((int)tip.X + 2, (int)tip.Y + 2, (int)tip.X + 110, (int)tip.Y + 80)}");
        Assert.True(left < 0);
        Assert.True(same, "leaving the plot draws the chart as before the hover");
    }

    [Fact]
    [Trait("AC", "DESKTOP-03")]
    public async Task AChangeInTheBoundCollectionLaysTheChartOutAgainAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");

        var (before, after) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var items = new ObservableCollection<IReadOnlyDictionary<string, object?>>(Reference.Rows(example));
                chart.Data = items;
                var window = new Window { Width = 500, Height = 300, Content = chart };
                window.Show();
                var count = ((Layout.ChartLayout)chart.Plot()).Data.Count;
                items.Add(new Dictionary<string, object?> { ["name"] = "Page H", ["pv"] = 20000.0, ["uv"] = 100.0 });
                var domain = ((Layout.ChartLayout)chart.Plot()).YAxes[0].DomainMax;
                window.Close();
                return (count, domain);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(7, before);
        Assert.Equal(20000, after);
    }
}
