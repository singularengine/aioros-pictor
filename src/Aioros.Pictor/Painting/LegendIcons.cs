using Aioros.Graphs.Shapes;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>Recharts' legend symbols: <c>Symbols</c> of 32 px in diameter (<c>sizeType="diameter"</c>), around the origin.</summary>
internal static class LegendIcons
{
    private const double Size = 32;

    /// <summary>The symbol of an icon that is not a line, a plain line or a rectangle, centered on the origin.</summary>
    public static SymbolShape Symbol(LegendIcon icon)
    {
        var type = icon switch
        {
            LegendIcon.Cross => SymbolType.Cross,
            LegendIcon.Diamond => SymbolType.Diamond,
            LegendIcon.Square => SymbolType.Square,
            LegendIcon.Star => SymbolType.Star,
            LegendIcon.Triangle => SymbolType.Triangle,
            LegendIcon.Wye => SymbolType.Wye,
            _ => SymbolType.Circle,
        };
        return new SymbolShape(type, Area(icon), 0, 0);
    }

    /// <summary>Recharts' <c>calculateAreaSize</c> of a symbol given by its diameter.</summary>
    public static double Area(LegendIcon icon) => icon switch
    {
        LegendIcon.Cross => 5 * Size * Size / 9,
        LegendIcon.Diamond => 0.5 * Size * Size / Math.Sqrt(3),
        LegendIcon.Square => Size * Size,
        LegendIcon.Star => 1.25 * Size * Size * (Math.Tan(18 * Math.PI / 180) - (Math.Tan(36 * Math.PI / 180) * Math.Pow(Math.Tan(18 * Math.PI / 180), 2))),
        LegendIcon.Triangle => Math.Sqrt(3) * Size * Size / 4,
        LegendIcon.Wye => (21 - (10 * Math.Sqrt(3))) * Size * Size / 8,
        _ => Math.PI * Size * Size / 4,
    };
}
