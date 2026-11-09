using System.Collections.Frozen;
using Avalonia.Controls;

namespace Aioros.Pictor;

/// <summary>The theme a chart takes its text font and colors from, besides its own keys.</summary>
public enum PictorHostTheme
{
    /// <summary>Recharts' own defaults in the light variant, Pictor's dark ones in the dark variant.</summary>
    Recharts,

    /// <summary>Avalonia's Fluent theme: the text and line colors from its base colors, the series color from its accent, its font.</summary>
    Fluent,

    /// <summary>Semi.Avalonia (and Ursa's Semi theme): the text, border and fill colors of Semi Design, its primary color, its font.</summary>
    Semi,
}

/// <summary>
/// A Pictor key that takes the value of another resource (a host theme's color, brush or font), looked up from the chart
/// for its variant, so a chart follows the host theme without copying its values: <c>&lt;p:ResourceAlias
/// x:Key="PictorTickText" Key="SemiColorText2"/&gt;</c>. When the other resource is missing, the chart keeps Pictor's value.
/// </summary>
public sealed class ResourceAlias
{
    /// <summary>The key of the resource whose value the Pictor key takes.</summary>
    public string Key { get; set; } = string.Empty;
}

/// <summary>The Pictor keys each host theme maps, to the resources of that theme they take.</summary>
internal static class HostThemes
{
    private static readonly FrozenDictionary<string, string> Fluent = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [PictorKeys.GridStroke] = "SystemBaseLowColor",
        [PictorKeys.PolarGridStroke] = "SystemBaseLowColor",
        [PictorKeys.CursorStroke] = "SystemBaseLowColor",
        [PictorKeys.TooltipBorder] = "SystemBaseLowColor",
        [PictorKeys.LegendInactive] = "SystemBaseLowColor",
        [PictorKeys.ReferenceLineStroke] = "SystemBaseLowColor",
        [PictorKeys.AxisLine] = "SystemBaseMediumColor",
        [PictorKeys.TickLine] = "SystemBaseMediumColor",
        [PictorKeys.PolarAxisStroke] = "SystemBaseMediumColor",
        [PictorKeys.PolarTickText] = "SystemBaseMediumColor",
        [PictorKeys.BrushStroke] = "SystemBaseMediumColor",
        [PictorKeys.TickText] = "SystemBaseMediumColor",
        [PictorKeys.LabelText] = "SystemBaseMediumColor",
        [PictorKeys.TooltipBackground] = "SystemAltHighColor",
        [PictorKeys.ActiveDotStroke] = "SystemAltHighColor",
        [PictorKeys.DotFill] = "SystemAltHighColor",
        [PictorKeys.TooltipText] = "SystemBaseHighColor",
        [PictorKeys.TooltipLabel] = "SystemBaseHighColor",
        [PictorKeys.CursorFill] = "SystemListLowColor",
        [PictorKeys.BarBackground] = "SystemListLowColor",
        [PictorKeys.SeriesColor] = "SystemAccentColor",
        [PictorKeys.FontFamily] = "ContentControlThemeFontFamily",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, string> Semi = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [PictorKeys.GridStroke] = "SemiColorBorder",
        [PictorKeys.PolarGridStroke] = "SemiColorBorder",
        [PictorKeys.CursorStroke] = "SemiColorBorder",
        [PictorKeys.TooltipBorder] = "SemiColorBorder",
        [PictorKeys.LegendInactive] = "SemiColorText3",
        [PictorKeys.ReferenceLineStroke] = "SemiColorBorder",
        [PictorKeys.AxisLine] = "SemiColorText3",
        [PictorKeys.TickLine] = "SemiColorText3",
        [PictorKeys.PolarAxisStroke] = "SemiColorText3",
        [PictorKeys.PolarTickText] = "SemiColorText2",
        [PictorKeys.BrushStroke] = "SemiColorText3",
        [PictorKeys.TickText] = "SemiColorText2",
        [PictorKeys.LabelText] = "SemiColorText2",
        [PictorKeys.TooltipBackground] = "SemiColorBackground0",
        [PictorKeys.ActiveDotStroke] = "SemiColorBackground0",
        [PictorKeys.DotFill] = "SemiColorBackground0",
        [PictorKeys.TooltipText] = "SemiColorText0",
        [PictorKeys.TooltipLabel] = "SemiColorText0",
        [PictorKeys.CursorFill] = "SemiColorFill0",
        [PictorKeys.BarBackground] = "SemiColorFill0",
        [PictorKeys.SeriesColor] = "SemiColorPrimary",
        [PictorKeys.FontFamily] = "DefaultFontFamily",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>The aliases of a host theme, as resources of one dictionary that wins over Pictor's own theme dictionaries.</summary>
    public static ResourceDictionary Aliases(PictorHostTheme host)
    {
        var dictionary = new ResourceDictionary();
        foreach (var (key, target) in host switch { PictorHostTheme.Fluent => Fluent, PictorHostTheme.Semi => Semi, _ => FrozenDictionary<string, string>.Empty })
        {
            dictionary[key] = new ResourceAlias { Key = target };
        }

        return dictionary;
    }

    /// <summary>The keys a host theme maps and the resources they take, for documentation and tests.</summary>
    public static IReadOnlyDictionary<string, string> Of(PictorHostTheme host) => host switch
    {
        PictorHostTheme.Fluent => Fluent,
        PictorHostTheme.Semi => Semi,
        _ => FrozenDictionary<string, string>.Empty,
    };
}
