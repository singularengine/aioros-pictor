namespace Aioros.Pictor.Motion;

/// <summary>How the framework's morph rebuilds a moving element from its numbers on the web (Aioros's <c>data-aioros-morph-path</c>).</summary>
internal enum MorphKind : byte
{
    /// <summary>Nothing declared.</summary>
    None,

    /// <summary>A path through points (<c>line</c>), with the series' curve.</summary>
    Line,

    /// <summary>A closed path through points (<c>polygon</c>).</summary>
    Polygon,

    /// <summary>A band: points forward, their base points back, closed (<c>band</c>), with the series' curve.</summary>
    Band,

    /// <summary>Recharts' rectangle path from x, y, width and height (<c>rect</c>).</summary>
    Rect,

    /// <summary>Recharts' sector path from its center, radii and angles (<c>sector</c>).</summary>
    Sector,

    /// <summary>The element's own symbol moved to a center and scaled to an area (<c>glyph</c>).</summary>
    Glyph,

    /// <summary>A circle's center (<c>cx cy</c>).</summary>
    Center,
}

/// <summary>
/// The update animation of an animated series' layer on the web: the kind its elements are rebuilt as unless they say
/// otherwise, whether previous points map by proportional index (Recharts' <c>prevPoints</c>), its timing, and a key of
/// the data its items were drawn from, so a series whose shapes stay animates anyway when an item changed.
/// </summary>
internal readonly record struct MorphLayer(MorphKind Kind, byte Curve, bool Ratio, Timing Timing, uint Key);

/// <summary>
/// The numbers of one moving element at rest and where they start when the element has no previous version (Aioros's
/// <c>data-aioros-morph</c> and <c>data-aioros-morph-from</c>): points from arrays (a missing coordinate written as 0,
/// as JavaScript reads Recharts' <c>null</c>), or up to six numbers; an origin number that is NaN keeps the element's own.
/// </summary>
internal readonly record struct MorphMark(MorphKind Kind)
{
    public double[]? X { get; init; }

    public double[]? Y { get; init; }

    public double[]? BaseX { get; init; }

    public double[]? BaseY { get; init; }

    /// <summary>The curve of a line or a band: 0 linear, 1 monotone along x, 2 monotone along y.</summary>
    public byte Curve { get; init; }

    public Six Values { get; init; }

    public int Count { get; init; }

    public Six From { get; init; }

    public int FromCount { get; init; }

    public static MorphMark Points(MorphKind kind, byte curve, double[] x, double[] y, (double X, double Y)? from = null) =>
        new(kind) { X = x, Y = y, Curve = curve, From = from is { } at ? Six.Of(at.X, at.Y) : default, FromCount = from is null ? 0 : 2 };

    public static MorphMark Band(byte curve, double[] x, double[] y, double[] baseX, double[] baseY) =>
        new(MorphKind.Band) { X = x, Y = y, BaseX = baseX, BaseY = baseY, Curve = curve };

    public static MorphMark Numbers(MorphKind kind, Six values, int count, Six from, int fromCount) =>
        new(kind) { Values = values, Count = count, From = from, FromCount = fromCount };
}

/// <summary>Up to six numbers held inline; a missing one is NaN.</summary>
internal readonly record struct Six(double A, double B = double.NaN, double C = double.NaN, double D = double.NaN, double E = double.NaN, double F = double.NaN)
{
    public double this[int index] => index switch
    {
        0 => A,
        1 => B,
        2 => C,
        3 => D,
        4 => E,
        _ => F,
    };

    public static Six Of(double a, double b) => new(a, b);
}
