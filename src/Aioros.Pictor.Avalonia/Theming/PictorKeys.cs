using Aioros.Pictor.Theming;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Aioros.Pictor;

/// <summary>
/// The resource keys of every default a chart draws, as <see cref="PictorTheme"/> defines them for the light and dark
/// variants: override one in a chart's, a page's, a window's or the application's resources to change it there
/// (<c>&lt;x:String x:Key="PictorGridStroke"&gt;#e5e5e5&lt;/x:String&gt;</c>, a <c>Color</c> or a <c>SolidColorBrush</c>).
/// </summary>
public static class PictorKeys
{
    /// <summary>The key of <see cref="ChartTheme.GridStroke"/>.</summary>
    public const string GridStroke = "PictorGridStroke";

    /// <summary>The key of <see cref="ChartTheme.AxisLine"/>.</summary>
    public const string AxisLine = "PictorAxisLine";

    /// <summary>The key of <see cref="ChartTheme.TickLine"/>.</summary>
    public const string TickLine = "PictorTickLine";

    /// <summary>The key of <see cref="ChartTheme.TickText"/>.</summary>
    public const string TickText = "PictorTickText";

    /// <summary>The key of <see cref="ChartTheme.LabelText"/>.</summary>
    public const string LabelText = "PictorLabelText";

    /// <summary>The key of <see cref="ChartTheme.RadialLabelText"/>.</summary>
    public const string RadialLabelText = "PictorRadialLabelText";

    /// <summary>The key of <see cref="ChartTheme.CursorStroke"/>.</summary>
    public const string CursorStroke = "PictorCursorStroke";

    /// <summary>The key of <see cref="ChartTheme.CursorFill"/>.</summary>
    public const string CursorFill = "PictorCursorFill";

    /// <summary>The key of <see cref="ChartTheme.ActiveDotStroke"/>.</summary>
    public const string ActiveDotStroke = "PictorActiveDotStroke";

    /// <summary>The key of <see cref="ChartTheme.DotFill"/>.</summary>
    public const string DotFill = "PictorDotFill";

    /// <summary>The key of <see cref="ChartTheme.SeriesColor"/>.</summary>
    public const string SeriesColor = "PictorSeriesColor";

    /// <summary>The key of <see cref="ChartTheme.ErrorBarStroke"/>.</summary>
    public const string ErrorBarStroke = "PictorErrorBarStroke";

    /// <summary>The key of <see cref="ChartTheme.TooltipBackground"/>.</summary>
    public const string TooltipBackground = "PictorTooltipBackground";

    /// <summary>The key of <see cref="ChartTheme.TooltipBorder"/>.</summary>
    public const string TooltipBorder = "PictorTooltipBorder";

    /// <summary>The key of <see cref="ChartTheme.TooltipLabel"/>.</summary>
    public const string TooltipLabel = "PictorTooltipLabel";

    /// <summary>The key of <see cref="ChartTheme.TooltipText"/>.</summary>
    public const string TooltipText = "PictorTooltipText";

    /// <summary>The key of <see cref="ChartTheme.TooltipSeparator"/>.</summary>
    public const string TooltipSeparator = "PictorTooltipSeparator";

    /// <summary>The key of <see cref="ChartTheme.LegendInactive"/>.</summary>
    public const string LegendInactive = "PictorLegendInactive";

    /// <summary>The key of <see cref="ChartTheme.ReferenceLineStroke"/>.</summary>
    public const string ReferenceLineStroke = "PictorReferenceLineStroke";

    /// <summary>The key of <see cref="ChartTheme.ReferenceAreaFill"/>.</summary>
    public const string ReferenceAreaFill = "PictorReferenceAreaFill";

    /// <summary>The key of <see cref="ChartTheme.ReferenceDotFill"/>.</summary>
    public const string ReferenceDotFill = "PictorReferenceDotFill";

    /// <summary>The key of <see cref="ChartTheme.ReferenceDotStroke"/>.</summary>
    public const string ReferenceDotStroke = "PictorReferenceDotStroke";

    /// <summary>The key of <see cref="ChartTheme.BrushStroke"/>.</summary>
    public const string BrushStroke = "PictorBrushStroke";

    /// <summary>The key of <see cref="ChartTheme.BrushFill"/>.</summary>
    public const string BrushFill = "PictorBrushFill";

    /// <summary>The key of <see cref="ChartTheme.BrushTraveller"/>.</summary>
    public const string BrushTraveller = "PictorBrushTraveller";

    /// <summary>The key of <see cref="ChartTheme.PolarGridStroke"/>.</summary>
    public const string PolarGridStroke = "PictorPolarGridStroke";

    /// <summary>The key of <see cref="ChartTheme.PolarAxisStroke"/>.</summary>
    public const string PolarAxisStroke = "PictorPolarAxisStroke";

    /// <summary>The key of <see cref="ChartTheme.PolarTickText"/>.</summary>
    public const string PolarTickText = "PictorPolarTickText";

    /// <summary>The key of <see cref="ChartTheme.PieFill"/>.</summary>
    public const string PieFill = "PictorPieFill";

    /// <summary>The key of <see cref="ChartTheme.PieStroke"/>.</summary>
    public const string PieStroke = "PictorPieStroke";

    /// <summary>The key of <see cref="ChartTheme.FunnelFill"/>.</summary>
    public const string FunnelFill = "PictorFunnelFill";

    /// <summary>The key of <see cref="ChartTheme.FunnelStroke"/>.</summary>
    public const string FunnelStroke = "PictorFunnelStroke";

    /// <summary>The key of <see cref="ChartTheme.BarBackground"/>.</summary>
    public const string BarBackground = "PictorBarBackground";

    /// <summary>The key of <see cref="ChartTheme.TreemapStroke"/>.</summary>
    public const string TreemapStroke = "PictorTreemapStroke";

    /// <summary>The key of <see cref="ChartTheme.TreemapArrow"/>.</summary>
    public const string TreemapArrow = "PictorTreemapArrow";

    /// <summary>The key of <see cref="ChartTheme.NestIndexBackground"/>.</summary>
    public const string NestIndexBackground = "PictorNestIndexBackground";

    /// <summary>The key of <see cref="ChartTheme.NestIndexText"/>.</summary>
    public const string NestIndexText = "PictorNestIndexText";

    /// <summary>The key of <see cref="ChartTheme.NestIndexSurface"/>.</summary>
    public const string NestIndexSurface = "PictorNestIndexSurface";

    /// <summary>The key of <see cref="ChartTheme.SankeyNodeFill"/>.</summary>
    public const string SankeyNodeFill = "PictorSankeyNodeFill";

    /// <summary>The key of <see cref="ChartTheme.SankeyLinkStroke"/>.</summary>
    public const string SankeyLinkStroke = "PictorSankeyLinkStroke";

    /// <summary>The key of <see cref="ChartTheme.SunburstFill"/>.</summary>
    public const string SunburstFill = "PictorSunburstFill";

    /// <summary>The key of <see cref="ChartTheme.SunburstStroke"/>.</summary>
    public const string SunburstStroke = "PictorSunburstStroke";

    /// <summary>The key of <see cref="ChartTheme.SunburstText"/>.</summary>
    public const string SunburstText = "PictorSunburstText";

    /// <summary>The key of <see cref="ChartTheme.SunburstTextStroke"/>.</summary>
    public const string SunburstTextStroke = "PictorSunburstTextStroke";

    /// <summary>The key of <see cref="ChartTheme.Palette"/>.</summary>
    public const string Palette = "PictorPalette";

    /// <summary>The key of <see cref="ChartTheme.FontFamily"/>: a font family's name, or a <c>FontFamily</c>; unset by default (the window's or the page's font).</summary>
    public const string FontFamily = "PictorFontFamily";

    /// <summary>The key of the palette's color at an index from 1 (<c>PictorPalette1</c>), for series an app colors from the theme.</summary>
    /// <param name="index">The color's index, from 1.</param>
    /// <returns>The key.</returns>
    public static string PaletteColor(int index) => "PictorPalette" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>A theme's values as resources under their keys, with each palette color under its own key unless <paramref name="indexed"/> is off (the theme serves those from the palette it finds, <see cref="PaletteColors"/>).</summary>
    internal static ResourceDictionary Dictionary(ChartTheme theme, bool indexed = true)
    {
        var dictionary = new ResourceDictionary();
        dictionary[GridStroke] = theme.GridStroke;
        dictionary[AxisLine] = theme.AxisLine;
        dictionary[TickLine] = theme.TickLine;
        dictionary[TickText] = theme.TickText;
        dictionary[LabelText] = theme.LabelText;
        dictionary[RadialLabelText] = theme.RadialLabelText;
        dictionary[CursorStroke] = theme.CursorStroke;
        dictionary[CursorFill] = theme.CursorFill;
        dictionary[ActiveDotStroke] = theme.ActiveDotStroke;
        dictionary[DotFill] = theme.DotFill;
        dictionary[SeriesColor] = theme.SeriesColor;
        dictionary[ErrorBarStroke] = theme.ErrorBarStroke;
        dictionary[TooltipBackground] = theme.TooltipBackground;
        dictionary[TooltipBorder] = theme.TooltipBorder;
        dictionary[TooltipLabel] = theme.TooltipLabel;
        dictionary[TooltipText] = theme.TooltipText;
        dictionary[TooltipSeparator] = theme.TooltipSeparator;
        dictionary[LegendInactive] = theme.LegendInactive;
        dictionary[ReferenceLineStroke] = theme.ReferenceLineStroke;
        dictionary[ReferenceAreaFill] = theme.ReferenceAreaFill;
        dictionary[ReferenceDotFill] = theme.ReferenceDotFill;
        dictionary[ReferenceDotStroke] = theme.ReferenceDotStroke;
        dictionary[BrushStroke] = theme.BrushStroke;
        dictionary[BrushFill] = theme.BrushFill;
        dictionary[BrushTraveller] = theme.BrushTraveller;
        dictionary[PolarGridStroke] = theme.PolarGridStroke;
        dictionary[PolarAxisStroke] = theme.PolarAxisStroke;
        dictionary[PolarTickText] = theme.PolarTickText;
        dictionary[PieFill] = theme.PieFill;
        dictionary[PieStroke] = theme.PieStroke;
        dictionary[FunnelFill] = theme.FunnelFill;
        dictionary[FunnelStroke] = theme.FunnelStroke;
        dictionary[BarBackground] = theme.BarBackground;
        dictionary[TreemapStroke] = theme.TreemapStroke;
        dictionary[TreemapArrow] = theme.TreemapArrow;
        dictionary[NestIndexBackground] = theme.NestIndexBackground;
        dictionary[NestIndexText] = theme.NestIndexText;
        dictionary[NestIndexSurface] = theme.NestIndexSurface;
        dictionary[SankeyNodeFill] = theme.SankeyNodeFill;
        dictionary[SankeyLinkStroke] = theme.SankeyLinkStroke;
        dictionary[SunburstFill] = theme.SunburstFill;
        dictionary[SunburstStroke] = theme.SunburstStroke;
        dictionary[SunburstText] = theme.SunburstText;
        dictionary[SunburstTextStroke] = theme.SunburstTextStroke;
        dictionary[Palette] = theme.Palette;
        var colors = indexed ? Palettes.Split(theme.Palette) : [];
        for (var i = 0; i < colors.Length; i++)
        {
            dictionary[PaletteColor(i + 1)] = colors[i];
        }

        return dictionary;
    }

    /// <summary>
    /// The theme a chart draws with: each key from the nearest resources (the chart's, its ancestors', the application's,
    /// then <see cref="PictorTheme"/>'s dictionary for the chart's variant), else Pictor's own value for the variant.
    /// </summary>
    internal static ChartTheme Resolve(StyledElement element)
    {
        var variant = element.ActualThemeVariant;
        var fallback = IsDark(variant) ? ChartTheme.Dark : ChartTheme.Light;
        var theme = new ChartTheme
        {
            GridStroke = Find(element, variant, GridStroke, fallback.GridStroke),
            AxisLine = Find(element, variant, AxisLine, fallback.AxisLine),
            TickLine = Find(element, variant, TickLine, fallback.TickLine),
            TickText = Find(element, variant, TickText, fallback.TickText),
            LabelText = Find(element, variant, LabelText, fallback.LabelText),
            RadialLabelText = Find(element, variant, RadialLabelText, fallback.RadialLabelText),
            CursorStroke = Find(element, variant, CursorStroke, fallback.CursorStroke),
            CursorFill = Find(element, variant, CursorFill, fallback.CursorFill),
            ActiveDotStroke = Find(element, variant, ActiveDotStroke, fallback.ActiveDotStroke),
            DotFill = Find(element, variant, DotFill, fallback.DotFill),
            SeriesColor = Find(element, variant, SeriesColor, fallback.SeriesColor),
            ErrorBarStroke = Find(element, variant, ErrorBarStroke, fallback.ErrorBarStroke),
            TooltipBackground = Find(element, variant, TooltipBackground, fallback.TooltipBackground),
            TooltipBorder = Find(element, variant, TooltipBorder, fallback.TooltipBorder),
            TooltipLabel = Find(element, variant, TooltipLabel, fallback.TooltipLabel),
            TooltipText = Find(element, variant, TooltipText, fallback.TooltipText),
            TooltipSeparator = Find(element, variant, TooltipSeparator, fallback.TooltipSeparator),
            LegendInactive = Find(element, variant, LegendInactive, fallback.LegendInactive),
            ReferenceLineStroke = Find(element, variant, ReferenceLineStroke, fallback.ReferenceLineStroke),
            ReferenceAreaFill = Find(element, variant, ReferenceAreaFill, fallback.ReferenceAreaFill),
            ReferenceDotFill = Find(element, variant, ReferenceDotFill, fallback.ReferenceDotFill),
            ReferenceDotStroke = Find(element, variant, ReferenceDotStroke, fallback.ReferenceDotStroke),
            BrushStroke = Find(element, variant, BrushStroke, fallback.BrushStroke),
            BrushFill = Find(element, variant, BrushFill, fallback.BrushFill),
            BrushTraveller = Find(element, variant, BrushTraveller, fallback.BrushTraveller),
            PolarGridStroke = Find(element, variant, PolarGridStroke, fallback.PolarGridStroke),
            PolarAxisStroke = Find(element, variant, PolarAxisStroke, fallback.PolarAxisStroke),
            PolarTickText = Find(element, variant, PolarTickText, fallback.PolarTickText),
            PieFill = Find(element, variant, PieFill, fallback.PieFill),
            PieStroke = Find(element, variant, PieStroke, fallback.PieStroke),
            FunnelFill = Find(element, variant, FunnelFill, fallback.FunnelFill),
            FunnelStroke = Find(element, variant, FunnelStroke, fallback.FunnelStroke),
            BarBackground = Find(element, variant, BarBackground, fallback.BarBackground),
            TreemapStroke = Find(element, variant, TreemapStroke, fallback.TreemapStroke),
            TreemapArrow = Find(element, variant, TreemapArrow, fallback.TreemapArrow),
            NestIndexBackground = Find(element, variant, NestIndexBackground, fallback.NestIndexBackground),
            NestIndexText = Find(element, variant, NestIndexText, fallback.NestIndexText),
            NestIndexSurface = Find(element, variant, NestIndexSurface, fallback.NestIndexSurface),
            SankeyNodeFill = Find(element, variant, SankeyNodeFill, fallback.SankeyNodeFill),
            SankeyLinkStroke = Find(element, variant, SankeyLinkStroke, fallback.SankeyLinkStroke),
            SunburstFill = Find(element, variant, SunburstFill, fallback.SunburstFill),
            SunburstStroke = Find(element, variant, SunburstStroke, fallback.SunburstStroke),
            SunburstText = Find(element, variant, SunburstText, fallback.SunburstText),
            SunburstTextStroke = Find(element, variant, SunburstTextStroke, fallback.SunburstTextStroke),
            Palette = Find(element, variant, Palette, fallback.Palette),
            FontFamily = Font(Lookup(element, variant, FontFamily)),
        };
        return theme == ChartTheme.Light ? ChartTheme.Light : theme == ChartTheme.Dark ? ChartTheme.Dark : theme;
    }

    internal static bool IsDark(ThemeVariant? variant)
    {
        for (var current = variant; current is not null; current = current.InheritVariant as ThemeVariant)
        {
            if (current == ThemeVariant.Dark)
            {
                return true;
            }
        }

        return false;
    }

    private static string Find(StyledElement element, ThemeVariant variant, string key, string fallback) =>
        Lookup(element, variant, key) is { } value && Text(value) is { } text ? text : fallback;

    /// <summary>A key's resource, or the resource a <see cref="ResourceAlias"/> under it names (one level, so aliases cannot loop).</summary>
    private static object? Lookup(StyledElement element, ThemeVariant variant, string key)
    {
        if (!element.TryFindResource(key, variant, out var value))
        {
            return null;
        }

        return value is ResourceAlias alias ? element.TryFindResource(alias.Key, variant, out var aliased) ? aliased : null : value;
    }

    /// <summary>A resource as the CSS color text the charts write: a text as it is, a color or a solid brush as hex (with its alpha as <c>rgba</c>).</summary>
    internal static string? Text(object? value) => value switch
    {
        string text => text,
        Color color => Css(color, 1),
        ISolidColorBrush brush => Css(brush.Color, brush.Opacity),
        _ => null,
    };

    private static string Css(Color color, double opacity)
    {
        var alpha = color.A / 255.0 * opacity;
        return alpha >= 1
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"#{color.R:x2}{color.G:x2}{color.B:x2}")
            : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"rgba({color.R},{color.G},{color.B},{Math.Round(alpha, 3)})");
    }

    private static string? Font(object? value) => value switch
    {
        string text => text,
        Avalonia.Media.FontFamily family => string.Join(", ", family.FamilyNames.Where(static name => !name.StartsWith('$'))) is { Length: > 0 } names ? names : null,
        _ => null,
    };
}
