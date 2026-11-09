using Aioros.Pictor.Specs;
using Aioros.Pictor.Tests.Desktop;
using Aioros.Pictor.Theming;
using Avalonia.Styling;

namespace Aioros.Pictor.Tests.Themes;

/// <summary>Avalonia's styles and control themes on chart elements, as the spec Aioros records holds them.</summary>
public sealed class StyleTests(DesktopSession desktop)
{
    private static readonly IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows =
    [
        new Dictionary<string, object?> { ["name"] = "A", ["uv"] = 1.0 },
        new Dictionary<string, object?> { ["name"] = "B", ["uv"] = 3.0 },
    ];

    private Task<(ChartSpec Lines, ChartSpec Bars)> SpecsAsync(Action<StyledChartsView>? change = null, ThemeVariant? variant = null) =>
        desktop.Session.Dispatch(
            () =>
            {
                var view = new StyledChartsView();
                view.Lines.Data = Rows;
                view.Bars.Data = Rows;
                var window = ThemeHost.Window(view, variant ?? ThemeVariant.Light);
                change?.Invoke(view);
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                var specs = (ThemeHost.Spec(view.Lines), ThemeHost.Spec(view.Bars));
                window.Close();
                return specs;
            },
            TestContext.Current.CancellationToken);

    [Fact]
    [Trait("AC", "STYLE-01")]
    public async Task ATypeSelectorStylesEveryElementOfItsTypeAsync()
    {
        var (lines, _) = await SpecsAsync();

        Assert.Equal("#eeeeee", lines.Grid!.Stroke);
    }

    [Fact]
    [Trait("AC", "STYLE-02")]
    public async Task AClassSelectorStylesOnlyTheElementsWithTheClassAsync()
    {
        var (lines, _) = await SpecsAsync();

        Assert.Equal(("#ff0000", 2.0), (lines.Series[0].Stroke, lines.Series[0].StrokeWidth));
        Assert.Equal((null, 1.0), (lines.Series[1].Stroke, lines.Series[1].StrokeWidth));
    }

    [Fact]
    [Trait("AC", "STYLE-03")]
    public async Task AValueSetOnTheElementWinsOverAStyleAsync()
    {
        var (lines, _) = await SpecsAsync();

        Assert.Equal(("#0000ff", 2.0), (lines.Series[2].Stroke, lines.Series[2].StrokeWidth));
    }

    [Fact]
    [Trait("AC", "STYLE-04")]
    public async Task ControlThemesSetTheirElementsPropertiesExplicitOrImplicitAsync()
    {
        var (lines, bars) = await SpecsAsync();

        Assert.Equal(("#654321", 3.0), (lines.Series[3].Stroke, lines.Series[3].StrokeWidth));
        Assert.Equal(("#ff0000", 2.0), (lines.Series[4].Stroke, lines.Series[4].StrokeWidth));
        Assert.Equal(("#123456", "#abcdef"), (bars.Series[0].Fill, bars.Series[1].Fill));
    }

    [Fact]
    [Trait("AC", "STYLE-05")]
    public async Task AStyleReachesTheCellsOfASeriesAsync()
    {
        var (_, bars) = await SpecsAsync();

        Assert.Equal(["#00ff00", null], bars.Series[0].Cells.Select(cell => cell.Fill));
    }

    [Fact]
    [Trait("AC", "STYLE-06")]
    public async Task AClassAddedAtRunTimeRestylesTheChartAsync()
    {
        var (lines, _) = await SpecsAsync(view =>
        {
            view.Lines.Classes.Add("muted");
            view.Lines.Elements.OfType<Line>().ElementAt(1).Classes.Add("accent");
        });

        Assert.Equal("#999999", lines.XAxes[0].Stroke);
        Assert.Equal("#ff0000", lines.Series[1].Stroke);
    }

    [Fact]
    [Trait("AC", "STYLE-07")]
    public async Task ADynamicResourceOnAnElementTakesTheThemesValueInEachVariantAsync()
    {
        var (light, _) = await SpecsAsync();
        var (dark, _) = await SpecsAsync(variant: ThemeVariant.Dark);

        Assert.Equal(("#223273", "#B7C1EA"), (light.Series[5].Stroke, dark.Series[5].Stroke));
        Assert.Equal(Palettes.Split(ChartTheme.Dark.Palette)[9], dark.Series[5].Stroke);
    }
}
