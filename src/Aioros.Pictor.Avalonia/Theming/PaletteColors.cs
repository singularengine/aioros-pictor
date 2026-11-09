using Aioros.Pictor.Theming;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Aioros.Pictor;

/// <summary>
/// The palette's colors under their own keys (<c>PictorPalette1</c>, <c>PictorPalette2</c>...), taken from the
/// <c>PictorPalette</c> the theme's owner finds (an application's own palette included), so that redefining the palette
/// recolors the series bound to its indexed keys; Pictor's palette for the variant when none is found.
/// </summary>
internal sealed class PaletteColors : ResourceProvider
{
    private const string Prefix = "PictorPalette";

    /// <inheritdoc />
    public override bool HasResources => true;

    /// <inheritdoc />
    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        value = null;
        if (key is not string name || name.Length <= Prefix.Length || !name.StartsWith(Prefix, StringComparison.Ordinal)
            || !int.TryParse(name.AsSpan(Prefix.Length), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var index) || index < 1)
        {
            return false;
        }

        var palette = Owner is { } owner && owner.TryFindResource(PictorKeys.Palette, theme, out var found) && PictorKeys.Text(found) is { } text
            ? text
            : (PictorKeys.IsDark(theme) ? ChartTheme.Dark : ChartTheme.Light).Palette;
        var colors = Palettes.Split(palette);
        if (index > colors.Length)
        {
            return false;
        }

        value = colors[index - 1];
        return true;
    }
}
