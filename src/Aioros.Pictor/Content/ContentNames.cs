using System.Globalization;
using Aioros.Graphs.Formatting;
using Aioros.Graphs.Paths;
using Aioros.Graphs.Shapes;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Content;

/// <summary>The attribute names of custom content that are not written as they are, and how many numbers their parts take.</summary>
internal static class ContentNames
{
    /// <summary>A text's content.</summary>
    public const string Content = "#text";

    /// <summary>A path's data: command letters and their coordinates.</summary>
    public const string Path = "d";

    /// <summary>The parameters of one of Recharts' shapes, whose path the painter draws.</summary>
    public const string Shape = "#shape";

    /// <summary>A polyline's or a polygon's points.</summary>
    public const string Points = "points";

    /// <summary>A transform: operations and their numbers.</summary>
    public const string Transform = "transform";

    /// <summary>How many numbers a path command takes (an arc's flags count as numbers).</summary>
    public static int Arity(char letter) => letter switch
    {
        'M' or 'L' => 2,
        'Q' => 4,
        'C' => 6,
        'A' => 7,
        _ => 0,
    };

    /// <summary>How many numbers a transform operation takes: a translation 2, a rotation 3 (its angle and center), a scale 2, a matrix 6.</summary>
    public static int TransformArity(char operation) => operation switch
    {
        't' or 's' => 2,
        'r' => 3,
        _ => 6,
    };
}

/// <summary>Texts of custom content: numbers as JavaScript writes them, numbers through a .NET format.</summary>
internal static class ContentTexts
{
    public static string Number(double value) => double.IsNaN(value) ? "NaN" : JsNumber.ToShortestString(value);

    public static string Format(string? format, double value) =>
        Text.Formats.Apply(format, value, Number(value));
}

/// <summary>The paths of Recharts' shapes a template uses (<c>Sector</c>, <c>Rectangle</c>, <c>Symbols</c>, <c>Trapezoid</c>, <c>Cross</c>, <c>Polygon</c>), drawn from their parameters.</summary>
internal static class ContentShapes
{
    /// <summary>Writes a shape's path from its parameters, in the order its node holds them.</summary>
    public static void Write<TSink>(ref TSink sink, ContentElement element, ReadOnlySpan<double> values, string? text, ReadOnlySpan<double> points)
        where TSink : struct, IPathSink
    {
        switch (element)
        {
            case ContentElement.Sector when values.Length >= 7:
                new SectorShape(values[0], values[1], values[2], values[3], values[4], values[5], values[6]).Write(ref sink);
                break;
            case ContentElement.Rectangle when values.Length >= 8:
                new RectShape(values[0], values[1], values[2], values[3], [values[4], values[5], values[6], values[7]]).Write(ref sink);
                break;
            case ContentElement.Symbols when values.Length >= 3:
                new SymbolShape(Symbols(text), values[2], values[0], values[1]).Write(ref sink);
                break;
            case ContentElement.Trapezoid when values.Length >= 5:
                new TrapezoidShape(values[0], values[1], values[2], values[3], values[4]).Write(ref sink);
                break;
            case ContentElement.Cross when values.Length >= 6:
                new CrossShape(values[0], values[1], values[2], values[3], values[4], values[5]).Write(ref sink);
                break;
            case ContentElement.PolygonPath:
                Polygon(ref sink, points.IsEmpty ? values : points);
                break;
        }
    }

    private static void Polygon<TSink>(ref TSink sink, ReadOnlySpan<double> points)
        where TSink : struct, IPathSink
    {
        for (var i = 0; i + 1 < points.Length; i += 2)
        {
            if (i == 0)
            {
                sink.MoveTo(points[i], points[i + 1]);
            }
            else
            {
                sink.LineTo(points[i], points[i + 1]);
            }
        }

        if (points.Length >= 2)
        {
            sink.ClosePath();
        }
    }

    /// <summary>d3's symbol type by Recharts' name (<c>circle</c>, <c>cross</c>, <c>diamond</c>, <c>square</c>, <c>star</c>, <c>triangle</c>, <c>wye</c>).</summary>
    public static SymbolType Symbols(string? name) => name?.ToLowerInvariant() switch
    {
        "cross" => SymbolType.Cross,
        "diamond" => SymbolType.Diamond,
        "square" => SymbolType.Square,
        "star" => SymbolType.Star,
        "triangle" => SymbolType.Triangle,
        "wye" => SymbolType.Wye,
        _ => SymbolType.Circle,
    };
}
