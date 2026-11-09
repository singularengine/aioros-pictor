namespace Aioros.Pictor.Theming;

/// <summary>
/// Every default a chart draws when its elements set no value of their own: the colors of the grid, the axes, the
/// labels, the tooltip, the cursor, the legend, the Brush, the references, the dots and the standalone charts, the
/// series palette and the text font. <see cref="Light"/> holds Recharts' own defaults; <see cref="Dark"/> is Pictor's
/// dark theme. The Avalonia controls resolve one from the theme's resources once per layout and carry it in the spec.
/// </summary>
public sealed record ChartTheme
{
    /// <summary>Recharts 2.15.4's defaults, so a chart without a theme draws exactly as Recharts does.</summary>
    public static ChartTheme Light { get; } = new();

    /// <summary>
    /// Pictor's dark theme, for a black background (Fluent's dark window): every gray of Recharts' defaults at the contrast it
    /// has against white, white and black swapped, the label text raised to WCAG AA (4.6:1), the series and Sankey blues
    /// kept (they contrast more with black than with white), and the palette's colors that would lose contrast lightened in
    /// their hue to the contrast they have against white.
    /// </summary>
    public static ChartTheme Dark { get; } = new()
    {
        GridStroke = "#313131",
        AxisLine = "#868686",
        TickLine = "#868686",
        TickText = "#868686",
        LabelText = "#767676",
        RadialLabelText = "#fff",
        CursorStroke = "#313131",
        CursorFill = "#313131",
        ActiveDotStroke = "#000",
        DotFill = "#000",
        ErrorBarStroke = "#fff",
        TooltipBackground = "#000",
        TooltipBorder = "#313131",
        TooltipLabel = "#fff",
        TooltipText = "#fff",
        LegendInactive = "#5c5c64",
        ReferenceLineStroke = "#313131",
        ReferenceAreaFill = "#313131",
        ReferenceDotFill = "#000",
        ReferenceDotStroke = "#313131",
        BrushStroke = "#868686",
        BrushFill = "#000",
        BrushTraveller = "#000",
        PolarGridStroke = "#313131",
        PolarAxisStroke = "#313131",
        PolarTickText = "#868686",
        PieFill = "#767676",
        PieStroke = "#000",
        FunnelFill = "#767676",
        FunnelStroke = "#000",
        BarBackground = "#161616",
        TreemapStroke = "#000",
        TreemapArrow = "#fff",
        NestIndexBackground = "#fff",
        NestIndexText = "#000",
        NestIndexSurface = "#000",
        SankeyLinkStroke = "#c9c9c9",
        SunburstFill = "#c9c9c9",
        SunburstStroke = "#000",
        SunburstText = "#fff",
        SunburstTextStroke = "#000",
        Palette = "#1890FF #66B5FF #41D9C7 #2FC25B #6EDB8F #9AE65C #FACC14 #E6965C #57AD71 #B7C1EA #738AE6 #7969CE #9B65E6 #A877ED #5C8EE6 #13C2C2 #70E0E0 #5CA3E6 #9C9DE5 #8082FF #DD81E6 #F04864 #FA7D92 #D598D9",
    };

    /// <summary>The cartesian grid's lines (Recharts' <c>#ccc</c>).</summary>
    public string GridStroke { get; init; } = "#ccc";

    /// <summary>A cartesian axis' line (Recharts' axis <c>stroke</c>, <c>#666</c>).</summary>
    public string AxisLine { get; init; } = "#666";

    /// <summary>A cartesian axis' tick lines (<c>#666</c>).</summary>
    public string TickLine { get; init; } = "#666";

    /// <summary>A cartesian axis' tick labels (<c>#666</c>).</summary>
    public string TickText { get; init; } = "#666";

    /// <summary>A label's and a label list's text (Recharts' <c>Text</c>, <c>#808080</c>), and a polar angle axis' tick labels.</summary>
    public string LabelText { get; init; } = "#808080";

    /// <summary>A polar radius axis' label along its arc (Recharts' radial label, a text without a fill: <c>black</c>).</summary>
    public string RadialLabelText { get; init; } = "black";

    /// <summary>A line cursor's stroke (<c>#ccc</c>).</summary>
    public string CursorStroke { get; init; } = "#ccc";

    /// <summary>A band cursor's fill (<c>#ccc</c>).</summary>
    public string CursorFill { get; init; } = "#ccc";

    /// <summary>The ring around an active dot (<c>#fff</c>, the background).</summary>
    public string ActiveDotStroke { get; init; } = "#fff";

    /// <summary>A line's dots' fill (a line's default <c>fill</c>, <c>#fff</c>).</summary>
    public string DotFill { get; init; } = "#fff";

    /// <summary>A series' color when it sets none: a line's and an area's stroke, an area's and a bar's fill (<c>#3182bd</c>).</summary>
    public string SeriesColor { get; init; } = "#3182bd";

    /// <summary>An error bar's stroke (<c>black</c>).</summary>
    public string ErrorBarStroke { get; init; } = "black";

    /// <summary>The tooltip box's background (<c>#fff</c>).</summary>
    public string TooltipBackground { get; init; } = "#fff";

    /// <summary>The tooltip box's border (<c>#ccc</c>).</summary>
    public string TooltipBorder { get; init; } = "#ccc";

    /// <summary>The tooltip's label; in this theme the web leaves it to the page's text color, as Recharts does, and the desktop draws it in this color.</summary>
    public string TooltipLabel { get; init; } = "#000";

    /// <summary>A tooltip entry's text when its item has no color (<c>#000</c>).</summary>
    public string TooltipText { get; init; } = "#000";

    /// <summary>The text between an entry's name and its value (<c>" : "</c>).</summary>
    public string TooltipSeparator { get; init; } = " : ";

    /// <summary>An inactive legend item's color (Recharts' <c>inactiveColor</c>, <c>#ccc</c>).</summary>
    public string LegendInactive { get; init; } = "#ccc";

    /// <summary>A reference line's stroke (<c>#ccc</c>).</summary>
    public string ReferenceLineStroke { get; init; } = "#ccc";

    /// <summary>A reference area's fill (<c>#ccc</c>).</summary>
    public string ReferenceAreaFill { get; init; } = "#ccc";

    /// <summary>A reference dot's fill (<c>#fff</c>).</summary>
    public string ReferenceDotFill { get; init; } = "#fff";

    /// <summary>A reference dot's stroke (<c>#ccc</c>).</summary>
    public string ReferenceDotStroke { get; init; } = "#ccc";

    /// <summary>The Brush's frame, slide, travellers and texts (<c>#666</c>).</summary>
    public string BrushStroke { get; init; } = "#666";

    /// <summary>The Brush's background (<c>#fff</c>).</summary>
    public string BrushFill { get; init; } = "#fff";

    /// <summary>The lines inside the Brush's travellers (<c>#fff</c>).</summary>
    public string BrushTraveller { get; init; } = "#fff";

    /// <summary>The polar grid's lines (<c>#ccc</c>).</summary>
    public string PolarGridStroke { get; init; } = "#ccc";

    /// <summary>A polar radius axis' line and ticks (<c>#ccc</c>).</summary>
    public string PolarAxisStroke { get; init; } = "#ccc";

    /// <summary>A polar radius axis' tick labels (<c>#ccc</c>, Recharts' axis stroke); the dark theme raises them to WCAG AA, apart from the faint axis line.</summary>
    public string PolarTickText { get; init; } = "#ccc";

    /// <summary>A pie's sectors without a fill (<c>#808080</c>).</summary>
    public string PieFill { get; init; } = "#808080";

    /// <summary>A pie's sectors' stroke (<c>#fff</c>, the background).</summary>
    public string PieStroke { get; init; } = "#fff";

    /// <summary>A funnel's trapezoids without a fill (<c>#808080</c>).</summary>
    public string FunnelFill { get; init; } = "#808080";

    /// <summary>A funnel's trapezoids' stroke (<c>#fff</c>).</summary>
    public string FunnelStroke { get; init; } = "#fff";

    /// <summary>A bar's and a radial bar's background (Recharts' <c>background</c> set to true, <c>#eee</c>).</summary>
    public string BarBackground { get; init; } = "#eee";

    /// <summary>A treemap's rectangles' stroke (<c>#fff</c>).</summary>
    public string TreemapStroke { get; init; } = "#fff";

    /// <summary>A nest treemap's arrows (<c>#000</c>).</summary>
    public string TreemapArrow { get; init; } = "#000";

    /// <summary>A nest treemap's index boxes (<c>#000</c>).</summary>
    public string NestIndexBackground { get; init; } = "#000";

    /// <summary>A nest treemap's index texts (<c>#fff</c>).</summary>
    public string NestIndexText { get; init; } = "#fff";

    /// <summary>The surface behind a nest treemap's index on the desktop (<c>#fff</c>; the web shows the page).</summary>
    public string NestIndexSurface { get; init; } = "#fff";

    /// <summary>A Sankey's nodes (<c>#0088fe</c>).</summary>
    public string SankeyNodeFill { get; init; } = "#0088fe";

    /// <summary>A Sankey's links (<c>#333</c>).</summary>
    public string SankeyLinkStroke { get; init; } = "#333";

    /// <summary>A sunburst's nodes without a fill (<c>#333</c>).</summary>
    public string SunburstFill { get; init; } = "#333";

    /// <summary>A sunburst's sectors' stroke (<c>#FFF</c>).</summary>
    public string SunburstStroke { get; init; } = "#FFF";

    /// <summary>A sunburst's texts (<c>black</c>).</summary>
    public string SunburstText { get; init; } = "black";

    /// <summary>A sunburst's texts' outline (<c>#FFF</c>).</summary>
    public string SunburstTextStroke { get; init; } = "#FFF";

    /// <summary>
    /// The series palette, colors separated by spaces: what a treemap colors its nodes with when it has no fill
    /// (Recharts' <c>COLOR_PANEL</c>), and what apps bind their series to (<c>PictorPalette1</c>, <c>PictorPalette2</c>...).
    /// </summary>
    public string Palette { get; init; } =
        "#1890FF #66B5FF #41D9C7 #2FC25B #6EDB8F #9AE65C #FACC14 #E6965C #57AD71 #223273 #738AE6 #7564CC #8543E0 #A877ED #5C8EE6 #13C2C2 #70E0E0 #5CA3E6 #3436C7 #8082FF #DD81E6 #F04864 #FA7D92 #D598D9";

    /// <summary>A background's fill: the theme's for <c>true</c> (Recharts' <c>background</c> without a fill of its own), else the color given.</summary>
    internal string BackgroundOf(string background) => background is "true" or "True" ? BarBackground : background;

    /// <summary>The charts' font family, written on the web chart's wrapper and drawn on the desktop; the page's or the window's when null (Recharts takes the page's).</summary>
    public string? FontFamily { get; init; }
}
