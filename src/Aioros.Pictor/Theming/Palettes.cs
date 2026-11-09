using System.Collections.Immutable;

namespace Aioros.Pictor.Theming;

/// <summary>A theme's palette as its list of colors, read once per layout; the light and dark themes' lists are read once.</summary>
internal static class Palettes
{
    private static readonly ImmutableArray<string> LightColors = Split(ChartTheme.Light.Palette);
    private static readonly ImmutableArray<string> DarkColors = Split(ChartTheme.Dark.Palette);

    public static ImmutableArray<string> Of(ChartTheme theme) =>
        theme.Palette == ChartTheme.Light.Palette ? LightColors
        : theme.Palette == ChartTheme.Dark.Palette ? DarkColors
        : Split(theme.Palette);

    public static ImmutableArray<string> Split(string palette) => [.. palette.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)];
}
