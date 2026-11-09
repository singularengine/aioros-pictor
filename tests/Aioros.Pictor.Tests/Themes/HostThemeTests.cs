using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Theming;
using Aioros.Pictor.Tests.Desktop;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Aioros.Pictor.Tests.Themes;

public sealed class HostThemeTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "THEME-19")]
    public async Task ChartsTakeTheFontAndColorsOfFluentAndSemiWhereTheirResourcesMapAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");

        var (fluent, semi, recharts, expected) = await desktop.Session.Dispatch(
            () =>
            {
                var fluentLight = Spec(example, ThemeVariant.Light, new PictorTheme { Host = PictorHostTheme.Fluent }, null);
                var fluentDark = Spec(example, ThemeVariant.Dark, new PictorTheme { Host = PictorHostTheme.Fluent }, null);
                var semiLight = Spec(example, ThemeVariant.Light, new PictorTheme { Host = PictorHostTheme.Semi }, Semi());
                var semiDark = Spec(example, ThemeVariant.Dark, new PictorTheme { Host = PictorHostTheme.Semi }, Semi());
                var plain = Spec(example, ThemeVariant.Light, new PictorTheme(), null);
                var window = new Window { RequestedThemeVariant = ThemeVariant.Dark };
                window.Resources.MergedDictionaries.Add(Semi());
                var colors = (
                    FluentLight: Css("SystemBaseMediumColor", ThemeVariant.Light, null), FluentDark: Css("SystemBaseMediumColor", ThemeVariant.Dark, null), FluentAccent: Css("SystemAccentColor", ThemeVariant.Light, null),
                    SemiLight: Css("SemiColorText2", ThemeVariant.Light, window), SemiDark: Css("SemiColorText2", ThemeVariant.Dark, window), SemiPrimary: Css("SemiColorPrimary", ThemeVariant.Light, window));
                return ((fluentLight, fluentDark), (semiLight, semiDark), plain, colors);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(expected.FluentLight, fluent.Item1.Theme!.TickText);
        Assert.Equal(expected.FluentDark, fluent.Item2.Theme!.TickText);
        Assert.NotEqual(fluent.Item1.Theme.TickText, fluent.Item2.Theme.TickText);
        Assert.Equal(expected.FluentAccent, fluent.Item1.Theme.SeriesColor);
        Assert.Equal("Inter", fluent.Item1.Theme.FontFamily);
        Assert.Equal(expected.SemiLight, semi.Item1.Theme!.TickText);
        Assert.Equal(expected.SemiDark, semi.Item2.Theme!.TickText);
        Assert.NotEqual(semi.Item1.Theme.TickText, semi.Item2.Theme.TickText);
        Assert.Equal(expected.SemiPrimary, semi.Item1.Theme.SeriesColor);
        Assert.Equal("Lato", semi.Item1.Theme.FontFamily);
        Assert.Equal(ChartTheme.Light.TreemapStroke, fluent.Item1.Theme.TreemapStroke);
        Assert.Null(recharts.Theme);
    }

    private static ChartSpec Spec(RechartsExample example, ThemeVariant variant, PictorTheme theme, ResourceDictionary? host)
    {
        var chart = SpecControls.Create(example);
        var window = new Window { Width = 800, Height = 600, RequestedThemeVariant = variant, Content = chart };
        window.Styles.Add(theme);
        if (host is not null)
        {
            window.Resources.MergedDictionaries.Add(host);
        }

        window.Show();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        var spec = ThemeHost.Spec(chart);
        window.Close();
        return spec;
    }

    /// <summary>
    /// Semi.Avalonia's color tokens as its theme includes them for the light and dark variants, and a stand-in for the font
    /// <c>SemiTheme</c> defines: <c>SemiTheme</c> itself cannot load in the tests' invariant globalization (its locales).
    /// </summary>
    private static ResourceDictionary Semi()
    {
        static Avalonia.Markup.Xaml.Styling.ResourceInclude Include(string path) => new((Uri?)null) { Source = new Uri("avares://Semi.Avalonia/Themes/" + path + "/_index.axaml") };
        var semi = new ResourceDictionary();
        semi.ThemeDictionaries[ThemeVariant.Light] = new ResourceDictionary { MergedDictionaries = { new global::Semi.Avalonia.Tokens.Palette.Light(), Include("Light") } };
        semi.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary { MergedDictionaries = { new global::Semi.Avalonia.Tokens.Palette.Dark(), Include("Dark") } };
        semi["DefaultFontFamily"] = new FontFamily("Lato");
        return semi;
    }

    private static string? Css(string key, ThemeVariant variant, Window? scope)
    {
        object? value = null;
        var found = scope is null ? Application.Current!.TryGetResource(key, variant, out value) : scope.TryFindResource(key, variant, out value);
        return found ? PictorKeys.Text(value) : null;
    }
}
