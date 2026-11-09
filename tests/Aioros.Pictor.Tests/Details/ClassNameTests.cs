using System.Buffers;
using System.Text;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Svg;
using Aioros.Pictor.Tests.Desktop;

namespace Aioros.Pictor.Tests.Details;

public sealed class ClassNameTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "DETAIL-07")]
    public async Task ClassNamesAndTheChartsStyleAreWrittenAsRechartsWritesThemAsync()
    {
        foreach (var name in (string[])["DetailsClassedChart", "DetailsClassedPieChart"])
        {
            var verdict = await Reference.CompareAsync(name);
            Assert.True(verdict.Matches, name + ": " + string.Join("\n", verdict.Differences));
        }

        var cartesian = await Reference.ExampleAsync("DetailsClassedChart");
        var pie = await Reference.ExampleAsync("DetailsClassedPieChart");
        var markup = Reference.Pictor(cartesian);
        Assert.Contains("<div class=\"recharts-wrapper sales\" style=\"position:relative;cursor:default;width:500px;height:300px;font-size:14px;background:#fafafa;border:1px solid #eee\"", markup, StringComparison.Ordinal);

        var (fromXaml, pieFromXaml, untabbed) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(cartesian with { Spec = Unclassed(cartesian.Spec) });
                chart.Classes.Add("sales");
                chart.WrapperStyle = "background:#fafafa;border:1px solid #eee";
                foreach (var (element, classes) in chart.Elements.Select(element => (element, element switch
                {
                    CartesianGrid => "grid",
                    XAxis => "pages",
                    YAxis => "values",
                    Area => "amounts",
                    Bar => "visitors",
                    Line => "views",
                    _ => null,
                })).Where(pair => pair.Item2 is not null))
                {
                    ((Avalonia.StyledElement)element).Classes.Add(classes!);
                }

                var pieChart = SpecControls.Create(pie with { Spec = pie.Spec with { ClassName = null, Polar = pie.Spec.Polar! with { Pies = [pie.Spec.Polar.Pies[0] with { ClassName = null }] } } });
                pieChart.Classes.Add("shares");
                pieChart.WrapperStyle = "background:#fafafa";
                var pieElement = pieChart.Elements.OfType<Pie>().Single();
                pieElement.Classes.Add("pages");
                var withTab = pieChart.Spec(pie.Spec.Width, pie.Spec.Height);
                pieElement.RootTabIndex = null;
                return (chart.Spec(cartesian.Spec.Width, cartesian.Spec.Height), withTab, pieChart.Spec(pie.Spec.Width, pie.Spec.Height));
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(markup, Write(fromXaml, cartesian.Data));
        Assert.Equal(Reference.Pictor(pie), Write(pieFromXaml, pie.Data));
        Assert.DoesNotContain("recharts-pie pages\" tabindex", Write(untabbed, pie.Data), StringComparison.Ordinal);
    }

    private static Specs.ChartSpec Unclassed(Specs.ChartSpec spec) => spec with
    {
        ClassName = null,
        WrapperStyle = null,
        Grid = spec.Grid! with { ClassName = null },
        XAxes = [.. spec.XAxes.Select(axis => axis with { ClassName = null })],
        YAxes = [.. spec.YAxes.Select(axis => axis with { ClassName = null })],
        Series = [.. spec.Series.Select(series => series with { ClassName = null })],
    };

    private static string Write(Specs.ChartSpec spec, object? data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, data, static (_, _) => null);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
