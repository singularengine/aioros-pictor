using Aioros.Pictor.Tests.Desktop;
using Avalonia.Styling;

namespace Aioros.Pictor.Tests.Themes;

/// <summary>The fidelity matrix's theme aspects: each family's reference example in the light theme (Recharts' render) and in the dark one (the light markup with the dark theme's colors).</summary>
public sealed class ThemeFidelityTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "THEME-20")]
    public async Task ALineChartInTheLightThemeMatchesRechartsAsync()
    {
        Assert.Empty(await ThemeHost.LightDifferencesAsync(desktop, "SimpleLineChart"));
    }

    [Fact]
    [Trait("AC", "THEME-21")]
    public async Task ALineChartInTheDarkThemeTakesTheDarkColorsOnlyAsync()
    {
        var (differences, themed) = await ThemeHost.DarkDifferencesAsync(desktop, "SimpleLineChart");

        Assert.Empty(differences);
        Assert.True(themed > 0, "no color took the dark theme's value");
    }

    [Fact]
    [Trait("AC", "THEME-22")]
    public async Task AnAreaChartInTheLightThemeMatchesRechartsAsync()
    {
        Assert.Empty(await ThemeHost.LightDifferencesAsync(desktop, "SimpleAreaChart"));
    }

    [Fact]
    [Trait("AC", "THEME-23")]
    public async Task AnAreaChartInTheDarkThemeTakesTheDarkColorsOnlyAsync()
    {
        var (differences, themed) = await ThemeHost.DarkDifferencesAsync(desktop, "SimpleAreaChart");

        Assert.Empty(differences);
        Assert.True(themed > 0, "no color took the dark theme's value");
    }

    [Fact]
    [Trait("AC", "THEME-24")]
    public async Task ABarChartInTheLightThemeMatchesRechartsAsync()
    {
        Assert.Empty(await ThemeHost.LightDifferencesAsync(desktop, "BarChartHasBackground"));
    }

    [Fact]
    [Trait("AC", "THEME-25")]
    public async Task ABarChartInTheDarkThemeTakesTheDarkColorsOnlyAsync()
    {
        var (differences, themed) = await ThemeHost.DarkDifferencesAsync(desktop, "BarChartHasBackground");

        Assert.Empty(differences);
        Assert.True(themed > 0, "no color took the dark theme's value");
    }

    [Fact]
    [Trait("AC", "THEME-26")]
    public async Task AComposedChartInTheLightThemeMatchesRechartsAsync()
    {
        Assert.Empty(await ThemeHost.LightDifferencesAsync(desktop, "LineBarAreaComposedChart"));
    }

    [Fact]
    [Trait("AC", "THEME-27")]
    public async Task AComposedChartInTheDarkThemeTakesTheDarkColorsOnlyAsync()
    {
        var (differences, themed) = await ThemeHost.DarkDifferencesAsync(desktop, "LineBarAreaComposedChart");

        Assert.Empty(differences);
        Assert.True(themed > 0, "no color took the dark theme's value");
    }

    [Fact]
    [Trait("AC", "THEME-28")]
    public async Task AScatterChartInTheLightThemeMatchesRechartsAsync()
    {
        Assert.Empty(await ThemeHost.LightDifferencesAsync(desktop, "SimpleScatterChart"));
    }

    [Fact]
    [Trait("AC", "THEME-29")]
    public async Task AScatterChartInTheDarkThemeTakesTheDarkColorsOnlyAsync()
    {
        var (differences, themed) = await ThemeHost.DarkDifferencesAsync(desktop, "SimpleScatterChart");

        Assert.Empty(differences);
        Assert.True(themed > 0, "no color took the dark theme's value");
    }

    [Fact]
    [Trait("AC", "THEME-30")]
    public async Task APieChartInTheLightThemeMatchesRechartsAsync()
    {
        Assert.Empty(await ThemeHost.LightDifferencesAsync(desktop, "StraightAnglePieChart"));
    }

    [Fact]
    [Trait("AC", "THEME-31")]
    public async Task APieChartInTheDarkThemeTakesTheDarkColorsOnlyAsync()
    {
        var (differences, themed) = await ThemeHost.DarkDifferencesAsync(desktop, "StraightAnglePieChart");

        Assert.Empty(differences);
        Assert.True(themed > 0, "no color took the dark theme's value");
    }

    [Fact]
    [Trait("AC", "THEME-32")]
    public async Task ARadarChartInTheLightThemeMatchesRechartsAsync()
    {
        Assert.Empty(await ThemeHost.LightDifferencesAsync(desktop, "SimpleRadarChart"));
    }

    [Fact]
    [Trait("AC", "THEME-33")]
    public async Task ARadarChartInTheDarkThemeTakesTheDarkColorsOnlyAsync()
    {
        var (differences, themed) = await ThemeHost.DarkDifferencesAsync(desktop, "SimpleRadarChart");

        Assert.Empty(differences);
        Assert.True(themed > 0, "no color took the dark theme's value");
    }

    [Fact]
    [Trait("AC", "THEME-34")]
    public async Task ARadialBarChartInTheLightThemeMatchesRechartsAsync()
    {
        Assert.Empty(await ThemeHost.LightDifferencesAsync(desktop, "SimpleRadialBarChart"));
    }

    [Fact]
    [Trait("AC", "THEME-35")]
    public async Task ARadialBarChartInTheDarkThemeTakesTheDarkColorsOnlyAsync()
    {
        var (differences, themed) = await ThemeHost.DarkDifferencesAsync(desktop, "SimpleRadialBarChart");

        Assert.Empty(differences);
        Assert.True(themed > 0, "no color took the dark theme's value");
    }

    [Fact]
    [Trait("AC", "THEME-36")]
    public async Task AFunnelChartInTheLightThemeMatchesRechartsAsync()
    {
        Assert.Empty(await ThemeHost.LightDifferencesAsync(desktop, "FunnelChart"));
    }

    [Fact]
    [Trait("AC", "THEME-37")]
    public async Task AFunnelChartInTheDarkThemeTakesTheDarkColorsOnlyAsync()
    {
        var (differences, themed) = await ThemeHost.DarkDifferencesAsync(desktop, "FunnelChart");

        Assert.Empty(differences);
        Assert.True(themed > 0, "no color took the dark theme's value");
    }

    [Fact]
    [Trait("AC", "THEME-38")]
    public async Task ATreemapInTheLightThemeMatchesRechartsAsync()
    {
        Assert.Empty(await ThemeHost.LightDifferencesAsync(desktop, "NestedTreemap"));
    }

    [Fact]
    [Trait("AC", "THEME-39")]
    public async Task ATreemapInTheDarkThemeTakesTheDarkColorsOnlyAsync()
    {
        var (differences, themed) = await ThemeHost.DarkDifferencesAsync(desktop, "NestedTreemap");

        Assert.Empty(differences);
        Assert.True(themed > 0, "no color took the dark theme's value");
    }

    [Fact]
    [Trait("AC", "THEME-40")]
    public async Task ASankeyInTheLightThemeMatchesRechartsAsync()
    {
        Assert.Empty(await ThemeHost.LightDifferencesAsync(desktop, "SimpleSankey"));
    }

    [Fact]
    [Trait("AC", "THEME-41")]
    public async Task ASankeyInTheDarkThemeTakesTheDarkColorsOnlyAsync()
    {
        var (differences, themed) = await ThemeHost.DarkDifferencesAsync(desktop, "SimpleSankey");

        Assert.Empty(differences);
        Assert.True(themed > 0, "no color took the dark theme's value");
    }

    [Fact]
    [Trait("AC", "THEME-42")]
    public async Task ASunburstChartInTheLightThemeMatchesRechartsAsync()
    {
        Assert.Empty(await ThemeHost.LightDifferencesAsync(desktop, "SimpleSunburst"));
    }

    [Fact]
    [Trait("AC", "THEME-43")]
    public async Task ASunburstChartInTheDarkThemeTakesTheDarkColorsOnlyAsync()
    {
        var (differences, themed) = await ThemeHost.DarkDifferencesAsync(desktop, "SimpleSunburst");

        Assert.Empty(differences);
        Assert.True(themed > 0, "no color took the dark theme's value");
    }
}
