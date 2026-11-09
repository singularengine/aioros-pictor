using Aioros.Pictor.Theming;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Aioros.Pictor;

/// <summary>
/// Pictor's theme, included once in the application's styles beside its own theme (<c>&lt;p:PictorTheme/&gt;</c> after
/// <c>&lt;FluentTheme/&gt;</c>): a light and a dark dictionary holding every default the charts draw under
/// <see cref="PictorKeys"/>, Recharts' own values in the light one. A key redefined in the application's, a window's,
/// a page's or a chart's resources wins there; a property set on a chart element wins over every key.
/// </summary>
public sealed class PictorTheme : Styles
{
    /// <summary>Creates the theme with its light and dark dictionaries.</summary>
    public PictorTheme() => Resources = Build(PictorHostTheme.Recharts);

    /// <summary>
    /// The theme the charts take their text font and colors from: Recharts' defaults (the default), or Avalonia's Fluent
    /// theme or Semi.Avalonia, whose resources the Pictor keys then take where they map (decision "Final demo rows and
    /// the matrix"); the keys a host theme does not map keep Pictor's values, and a key redefined by the app still wins.
    /// </summary>
    public PictorHostTheme Host
    {
        get;
        set
        {
            field = value;
            Resources = Build(value);
        }
    }

    private static ResourceDictionary Build(PictorHostTheme host)
    {
        var resources = HostThemes.Aliases(host);
        resources.ThemeDictionaries[ThemeVariant.Light] = PictorKeys.Dictionary(ChartTheme.Light, indexed: false);
        resources.ThemeDictionaries[ThemeVariant.Dark] = PictorKeys.Dictionary(ChartTheme.Dark, indexed: false);
        resources.MergedDictionaries.Add(new PaletteColors());
        return resources;
    }
}
