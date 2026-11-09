using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Tests.Desktop;
using Aioros.Pictor.Theming;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

namespace Aioros.Pictor.Tests.Themes;

/// <summary>How a chart resolves its theme from resources, on both targets.</summary>
public sealed class ThemeResourceTests(DesktopSession desktop)
{
    private Task<T> OnDesktopAsync<T>(Func<T> work) => desktop.Session.Dispatch(work, TestContext.Current.CancellationToken);

    private static Task<RechartsExample> ExampleAsync() => Reference.ExampleAsync("SimpleLineChart");

    [Fact]
    [Trait("AC", "THEME-01")]
    public async Task TheLightThemeHoldsRechartsDefaultsAndWritesTheSameBytesAsync()
    {
        var light = ChartTheme.Light;
        var examples = new[] { "SimpleLineChart", "BarChartHasBackground", "SimpleRadarChart", "FunnelChart", "NestedTreemap", "SimpleSankey", "SimpleSunburst" };

        Assert.Equal(
            ["#ccc", "#666", "#666", "#666", "#808080", "black", "#ccc", "#ccc", "#fff", "#fff", "#3182bd", "black", "#fff", "#ccc", "#ccc", "#eee", "#808080", "#fff", "#0088fe", "#333", "#333", "#FFF"],
            [light.GridStroke, light.AxisLine, light.TickLine, light.TickText, light.LabelText, light.RadialLabelText, light.CursorStroke, light.CursorFill, light.ActiveDotStroke, light.DotFill, light.SeriesColor, light.ErrorBarStroke, light.TooltipBackground, light.TooltipBorder, light.LegendInactive, light.BarBackground, light.PieFill, light.PieStroke, light.SankeyNodeFill, light.SankeyLinkStroke, light.SunburstFill, light.SunburstStroke]);
        foreach (var name in examples)
        {
            var example = await Reference.ExampleAsync(name);
            Assert.Equal(ThemeHost.Write(example.Spec, example.Data), ThemeHost.Write(example.Spec with { Theme = ChartTheme.Light }, example.Data));
            Assert.DoesNotContain("\"Theme\"", SpecJson.Write(example.Spec), StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait("AC", "THEME-02")]
    public async Task ThePictorThemeHoldsEveryKeyForBothVariantsAsync()
    {
        var found = await OnDesktopAsync(() =>
        {
            var theme = new PictorTheme();
            string? Find(object key, ThemeVariant variant) => theme.TryGetResource(key, variant, out var value) ? value as string : null;
            return (
                Light: (Find(PictorKeys.GridStroke, ThemeVariant.Light), Find(PictorKeys.TooltipBackground, ThemeVariant.Light), Find(PictorKeys.PaletteColor(1), ThemeVariant.Light), Find(PictorKeys.PaletteColor(24), ThemeVariant.Light)),
                Dark: (Find(PictorKeys.GridStroke, ThemeVariant.Dark), Find(PictorKeys.TooltipBackground, ThemeVariant.Dark), Find(PictorKeys.PaletteColor(10), ThemeVariant.Dark), Find(PictorKeys.PaletteColor(25), ThemeVariant.Dark)),
                Keys: PictorKeys.Dictionary(ChartTheme.Dark).Keys.Count);
        });

        Assert.Equal(("#ccc", "#fff", "#1890FF", "#D598D9"), found.Light);
        Assert.Equal(("#313131", "#000", "#B7C1EA", null), found.Dark);
        Assert.Equal(45 + 24, found.Keys);
    }

    [Fact]
    [Trait("AC", "THEME-03")]
    public async Task AChartTakesTheDictionaryOfItsThemeVariantAsync()
    {
        var example = await ExampleAsync();

        var themes = await OnDesktopAsync(() =>
        {
            var light = SpecControls.Create(example);
            var dark = SpecControls.Create(example);
            var first = ThemeHost.Window(light, ThemeVariant.Light);
            var second = ThemeHost.Window(dark, ThemeVariant.Dark);
            var found = (ThemeHost.Spec(light).Theme, ThemeHost.Spec(dark).Theme);
            first.Close();
            second.Close();
            return found;
        });

        Assert.Null(themes.Item1);
        Assert.Equal(ChartTheme.Dark, themes.Item2);
    }

    [Fact]
    [Trait("AC", "THEME-04")]
    public async Task AKeyRedefinedInAWindowChangesTheChartsInsideItAsync()
    {
        var example = await ExampleAsync();

        var spec = await OnDesktopAsync(() =>
        {
            var chart = SpecControls.Create(example);
            var window = new Window { Width = 500, Height = 300, Content = chart };
            window.Styles.Add(new PictorTheme());
            window.Resources[PictorKeys.GridStroke] = "#e5e5e5";
            window.Show();
            var written = ThemeHost.Spec(chart);
            window.Close();
            return written;
        });

        Assert.Equal(ChartTheme.Light with { GridStroke = "#e5e5e5" }, spec.Theme);
        Assert.Contains("stroke=\"#e5e5e5\"", ThemeHost.Write(spec, example.Data), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "THEME-05")]
    public async Task AKeyRedefinedInAChartWinsOverItsWindowForThatChartOnlyAsync()
    {
        var example = await ExampleAsync();

        var strokes = await OnDesktopAsync(() =>
        {
            var own = SpecControls.Create(example);
            var other = SpecControls.Create(example);
            own.Resources[PictorKeys.GridStroke] = "#111111";
            var window = new Window { Width = 500, Height = 600, Content = new StackPanel { Children = { own, other } } };
            window.Styles.Add(new PictorTheme());
            window.Resources[PictorKeys.GridStroke] = "#e5e5e5";
            window.Show();
            var found = (ThemeHost.Spec(own).Theme?.GridStroke, ThemeHost.Spec(other).Theme?.GridStroke);
            window.Close();
            return found;
        });

        Assert.Equal(("#111111", "#e5e5e5"), strokes);
    }

    [Fact]
    [Trait("AC", "THEME-06")]
    public async Task AValueSetOnTheElementWinsOverEveryKeyAsync()
    {
        var example = await ExampleAsync();

        var markup = await OnDesktopAsync(() =>
        {
            var chart = SpecControls.Create(example);
            chart.Resources[PictorKeys.GridStroke] = "#111111";
            chart.Elements.OfType<CartesianGrid>().Single().Stroke = "#222222";
            var window = ThemeHost.Window(chart, ThemeVariant.Dark);
            var written = ThemeHost.Write(ThemeHost.Spec(chart), example.Data);
            window.Close();
            return written;
        });

        Assert.Contains("stroke=\"#222222\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("#111111", markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "THEME-07")]
    public async Task ColorsAndBrushesAreTakenAndOtherValuesFallBackToTheThemesAsync()
    {
        var example = await ExampleAsync();

        var theme = await OnDesktopAsync(() =>
        {
            var chart = SpecControls.Create(example);
            chart.Resources[PictorKeys.TooltipBackground] = Color.Parse("#102030");
            chart.Resources[PictorKeys.TooltipBorder] = new SolidColorBrush(Color.Parse("#102030"), 0.5);
            chart.Resources[PictorKeys.GridStroke] = 42;
            var window = ThemeHost.Window(chart, ThemeVariant.Dark);
            var found = ThemeHost.Spec(chart).Theme!;
            window.Close();
            return found;
        });

        Assert.Equal(("#102030", "rgba(16,32,48,0.5)", ChartTheme.Dark.GridStroke), (theme.TooltipBackground, theme.TooltipBorder, theme.GridStroke));
    }

    [Fact]
    [Trait("AC", "THEME-08")]
    public async Task WithoutThePictorThemeOrWithAnUnknownKeyTheChartKeepsPictorsValuesAsync()
    {
        var example = await ExampleAsync();

        var themes = await OnDesktopAsync(() =>
        {
            var bare = SpecControls.Create(example);
            var misspelt = SpecControls.Create(example);
            var first = ThemeHost.Window(bare, ThemeVariant.Dark, theme: false);
            misspelt.Resources["PictorGridStrokes"] = "#e5e5e5";
            var second = ThemeHost.Window(misspelt, ThemeVariant.Light);
            var found = (ThemeHost.Spec(bare).Theme, ThemeHost.Spec(misspelt).Theme);
            first.Close();
            second.Close();
            return found;
        });

        Assert.Equal(ChartTheme.Dark, themes.Item1);
        Assert.Null(themes.Item2);
    }

    [Fact]
    [Trait("AC", "THEME-09")]
    public async Task ARemovedKeyRestoresTheThemesValueAsync()
    {
        var example = await ExampleAsync();

        var (overridden, restored) = await OnDesktopAsync(() =>
        {
            var chart = SpecControls.Create(example);
            chart.Resources[PictorKeys.GridStroke] = "#111111";
            var window = ThemeHost.Window(chart, ThemeVariant.Light);
            var before = chart.Plot().Spec.Theme?.GridStroke;
            chart.Resources.Remove(PictorKeys.GridStroke);
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            var after = chart.Plot().Spec.Theme;
            window.Close();
            return (before, after);
        });

        Assert.Equal("#111111", overridden);
        Assert.Null(restored);
    }

    [Fact]
    [Trait("AC", "THEME-10")]
    public async Task SwitchingTheThemeVariantLaysTheDesktopChartOutAgainInTheNewColorsAsync()
    {
        var example = await ExampleAsync();

        var (layouts, theme) = await OnDesktopAsync(() =>
        {
            var chart = SpecControls.Create(example);
            var window = ThemeHost.Window(chart, ThemeVariant.Light);
            var before = chart.Layouts;
            window.RequestedThemeVariant = ThemeVariant.Dark;
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            var spec = chart.Plot().Spec;
            var found = (chart.Layouts - before, spec.Colors);
            window.Close();
            return found;
        });

        Assert.Equal(1, layouts);
        Assert.Equal(ChartTheme.Dark, theme);
    }

    [Fact]
    [Trait("AC", "THEME-11")]
    public async Task AResourceChangedAtRunTimeRedrawsTheDesktopChartAsync()
    {
        var example = await ExampleAsync();

        var stroke = await OnDesktopAsync(() =>
        {
            var chart = SpecControls.Create(example);
            var window = ThemeHost.Window(chart, ThemeVariant.Light, theme: false);
            try
            {
                _ = chart.Plot();
                Application.Current!.Resources[PictorKeys.GridStroke] = "#abcdef";
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                return chart.Plot().Spec.Colors.GridStroke;
            }
            finally
            {
                Application.Current!.Resources.Remove(PictorKeys.GridStroke);
                window.Close();
            }
        });

        Assert.Equal("#abcdef", stroke);
    }

    [Fact]
    [Trait("AC", "THEME-12")]
    public async Task TheWebChartCarriesItsThemeAsCustomPropertiesTheSharedRulesReadAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");

        var light = ThemeHost.Write(example.Spec, example.Data);
        var dark = ThemeHost.Write(example.Spec with { Theme = ChartTheme.Dark }, example.Data);

        Assert.DoesNotContain("--pictor-", light[..light.IndexOf("<svg", StringComparison.Ordinal)], StringComparison.Ordinal);
        Assert.Contains("font-size:14px;--pictor-cursor:#313131;--pictor-band:#313131;--pictor-ring:#000;--pictor-tip-bg:#000;--pictor-tip-border:#313131;--pictor-tip-label:#fff\"", dark, StringComparison.Ordinal);
        Assert.Equal(StyleBlock(light), StyleBlock(dark));
        Assert.Contains("<circle class=\"pictor-dot\"", dark, StringComparison.Ordinal);

        static string StyleBlock(string markup)
        {
            var start = markup.IndexOf("<style>", StringComparison.Ordinal);
            return markup[start..markup.IndexOf("</style>", start, StringComparison.Ordinal)];
        }
    }

    [Fact]
    [Trait("AC", "THEME-13")]
    public async Task TheThemesFontIsWrittenAndTheTextsAreStillMeasuredWithInterAsync()
    {
        var example = await ExampleAsync();

        var spec = await OnDesktopAsync(() =>
        {
            var chart = SpecControls.Create(example);
            chart.Resources[PictorKeys.FontFamily] = new FontFamily("Georgia");
            var window = ThemeHost.Window(chart, ThemeVariant.Light);
            var written = ThemeHost.Spec(chart);
            window.Close();
            return written;
        });
        var markup = ThemeHost.Write(spec, example.Data);

        Assert.Equal("Georgia", spec.Theme!.FontFamily);
        Assert.Contains("font-size:14px;font-family:Georgia\"", markup, StringComparison.Ordinal);
        Assert.Equal(ThemeHost.Write(example.Spec, example.Data), markup.Replace(";font-family:Georgia", string.Empty, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("AC", "THEME-14")]
    public async Task ATreemapWithoutFillTakesTheThemesPaletteAndItsOwnPanelWinsAsync()
    {
        var example = await Reference.ExampleAsync("NestedTreemap");

        TreemapPlot Plot(ChartSpec spec) => (TreemapPlot)Pictor.Painting.Plots.Compute(spec, Pictor.Data.ChartData.From(example.Data, static (_, _) => null));
        var dark = Plot(example.Spec with { Theme = ChartTheme.Dark });
        var own = Plot(example.Spec with { Theme = ChartTheme.Dark, Treemap = example.Spec.Treemap! with { ColorPanel = ["#010101", "#020202"] } });

        Assert.Equal(Palettes.Split(ChartTheme.Dark.Palette), dark.Colors);
        Assert.Equal(["#010101", "#020202"], own.Colors);
    }

    [Fact]
    [Trait("AC", "THEME-15")]
    public async Task ABackgroundSetToTrueTakesTheThemesColorAndAColorOfItsOwnWinsAsync()
    {
        var example = await Reference.ExampleAsync("BarChartHasBackground");
        ChartSpec Background(string fill) => example.Spec with { Series = [.. example.Spec.Series.Select(series => series.Background is null ? series : series with { Background = fill })] };
        var own = Background("#abcdef");

        var dark = ThemeHost.Write(Background("true") with { Theme = ChartTheme.Dark }, example.Data);
        var light = ThemeHost.Write(Background("true"), example.Data);

        Assert.Contains("fill=\"#161616\" class=\"recharts-rectangle recharts-bar-background-rectangle\"", dark, StringComparison.Ordinal);
        Assert.Contains("fill=\"#eee\" class=\"recharts-rectangle recharts-bar-background-rectangle\"", light, StringComparison.Ordinal);
        Assert.DoesNotContain("#161616", ThemeHost.Write(own with { Theme = ChartTheme.Dark }, example.Data), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "THEME-16")]
    public async Task TheTooltipSeparatorComesFromTheThemeUnlessTheTooltipSetsOneAsync()
    {
        var example = await ExampleAsync();

        var separators = await OnDesktopAsync(() =>
        {
            var themed = SpecControls.Create(example);
            var own = SpecControls.Create(example);
            var window = new Window { Width = 500, Height = 600, Content = new StackPanel { Children = { themed, own } } };
            window.Styles.Add(new PictorTheme());
            window.Resources[PictorKeys.TooltipSeparator] = " = ";
            own.Elements.OfType<Tooltip>().Single().Separator = " | ";
            themed.Elements.OfType<Tooltip>().Single().ClearValue(Tooltip.SeparatorProperty);
            window.Show();
            var found = (ThemeHost.Spec(themed).Tooltip!.Separator, ThemeHost.Spec(own).Tooltip!.Separator);
            window.Close();
            return found;
        });

        Assert.Equal((" = ", " | "), separators);
    }

    [Fact]
    [Trait("AC", "THEME-17")]
    public async Task TheDesktopDrawsTheChartInItsThemesColorsAsync()
    {
        var example = await ExampleAsync();

        var counts = await OnDesktopAsync(() => (Inked(SpecControls.Create(example), ThemeVariant.Light), Inked(SpecControls.Create(example), ThemeVariant.Dark)));

        Assert.True(counts.Item1.Light > 100 && counts.Item1.Dark == 0, $"light chart: {counts.Item1}");
        Assert.True(counts.Item2.Dark > 100 && counts.Item2.Light == 0, $"dark chart: {counts.Item2}");

        static (int Light, int Dark) Inked(Chart chart, ThemeVariant variant)
        {
            using var drawn = DesktopRender.Draw(chart, variant);
            var (_, _, bgra) = DesktopRender.Pixels(drawn);
            var (gridLight, gridDark) = (0, 0);
            for (var i = 0; i < bgra.Length; i += 4)
            {
                gridLight += Near(bgra, i, 0xcc) ? 1 : 0;
                gridDark += Near(bgra, i, 0x31) ? 1 : 0;
            }

            return (gridLight, gridDark);
        }

        static bool Near(byte[] bgra, int i, int gray) => Math.Abs(bgra[i] - gray) <= 2 && Math.Abs(bgra[i + 1] - gray) <= 2 && Math.Abs(bgra[i + 2] - gray) <= 2;
    }
}
