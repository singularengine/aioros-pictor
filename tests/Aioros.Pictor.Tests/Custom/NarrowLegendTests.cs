using Aioros.Pictor.Data;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Text;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Custom;

public sealed class NarrowLegendTests(Desktop.DesktopSession desktop)
{
    private static readonly string[] Keys = ["a", "b", "c", "d", "e", "f"];
    private static readonly string[] English = ["Rent", "Groceries", "Eating out", "Subscriptions", "Transport", "Everything else"];
    private static readonly string[] Japanese = ["\u5BB6\u8CC3", "\u98DF\u6599\u54C1", "\u5916\u98DF", "\u30B5\u30D6\u30B9\u30AF\u30EA\u30D7\u30B7\u30E7\u30F3", "\u4EA4\u901A\u8CBB", "\u305D\u306E\u4ED6\u3059\u3079\u3066"];

    private static List<object?> Rows(RechartsExample example) =>
        [.. Reference.Rows(example).Select(static row => (object?)new Dictionary<string, object?>(row) { ["a"] = 1.0, ["b"] = 2.0, ["c"] = 3.0, ["d"] = 4.0, ["e"] = 5.0, ["f"] = 6.0 })];

    [Theory]
    [Trait("AC", "LEGEND-13")]
    [InlineData(false, false, 3)]
    [InlineData(false, false, 2)]
    [InlineData(false, true, 3)]
    [InlineData(false, true, 2)]
    [InlineData(true, false, 3)]
    [InlineData(true, false, 2)]
    [InlineData(true, true, 3)]
    [InlineData(true, true, 2)]
    public async Task ANarrowChartsLegendWrapsAndNeverCoversItsAxisAsync(bool framed, bool japanese, double aspect)
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        var written = framed
            ? await CustomReference.SpecAsync(desktop, "CustomFramedLegend", static (chart, templates) =>
            {
                var legend = chart.Elements.OfType<Legend>().Single();
                (legend.Template, legend.ContentTemplate) = (templates["RowLegend"], templates["SquareEntry"]);
            })
            : example.Spec;
        var names = japanese ? Japanese : English;
        var spec = written with
        {
            Kind = example.Spec.Kind,
            XAxes = example.Spec.XAxes,
            YAxes = example.Spec.YAxes,
            Width = 320,
            Height = 320 / aspect,
            Aspect = aspect,
            Responsive = true,
            Series = [.. Keys.Select((key, index) => example.Spec.Series[0] with { DataKey = key, Name = names[index], Order = index })],
        };

        var plot = (ChartLayout)ChartFit.Compute(spec, ChartData.From(Rows(example), static (_, _) => null));
        var legend = Plots.LegendOf(plot)!;
        var axis = plot.XAxes[0];

        Assert.True(framed ? legend.Height > 40 : legend.Items.Select(static item => item.Y).Distinct().Count() > 1, $"one line, {legend.Height} high");
        Assert.All(legend.Items.Where(_ => !framed), item => Assert.True(item.X + item.Width <= legend.Left + legend.Width + 0.5, $"{item.Name} ends at {item.X + item.Width}"));
        Assert.True(axis.Y + axis.Height <= legend.Top + 0.5, $"the axis ends at {axis.Y + axis.Height}, the legend starts at {legend.Top}");
        Assert.True(plot.PlotHeight >= 39.5, $"the plot is {plot.PlotHeight} high");
        Assert.Equal(plot.Spec.Width / plot.Spec.Height, plot.Spec.Aspect!.Value, 6);
    }

    [Fact]
    [Trait("AC", "LEGEND-14")]
    public void FullWidthCharactersCountOneEm()
    {
        Assert.Equal(24, InterMetrics.Width(Japanese[0], 12));
        Assert.Equal(24, InterMetrics.Width(Japanese[2], 12));
        Assert.True(InterMetrics.Width("Rent", 12) < 28);
    }

    [Fact]
    [Trait("AC", "LEGEND-15")]
    public async Task TheDesktopTakesTheHeightThatHoldsItsLegendAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");
        double Laid(int series, double width) => Dispatch(example, series, width);
        var (crowded, roomy) = await desktop.Session.Dispatch(() => (Laid(6, 320), Laid(1, 640)), TestContext.Current.CancellationToken);

        Assert.True(crowded > 320 / 3.0 + 5, $"{crowded} high");
        Assert.Equal(Math.Ceiling(640 / 3.0), roomy);
    }

    private static double Dispatch(RechartsExample example, int series, double width)
    {
        var chart = SpecControls.Create(example);
        chart.Data = Rows(example);
        var bars = chart.Elements.OfType<Bar>().ToList();
        bars.ForEach(bar => chart.Elements.Remove(bar));
        foreach (var (key, name) in Keys.Zip(English).Take(series))
        {
            chart.Elements.Add(new Bar { DataKey = key, Name = name });
        }

        chart.Width = double.NaN;
        chart.Height = double.NaN;
        chart.Aspect = 3;
        chart.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        var window = new Window { Width = width, Height = 900, Content = chart };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var height = chart.Bounds.Height;
        window.Close();
        return height;
    }
}
