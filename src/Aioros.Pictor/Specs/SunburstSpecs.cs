namespace Aioros.Pictor.Specs;

/// <summary>Recharts' <c>SunburstChart</c>: a tree drawn as rings of sectors around a center, each arc its value's share of the root's.</summary>
public sealed record SunburstSpec
{
    /// <summary>The data key of the nodes' values, which size their arcs and are written on them.</summary>
    public string DataKey { get; init; } = "value";

    /// <summary>The data key of the nodes' names (their accessible names and tooltips); Recharts reads <c>name</c>.</summary>
    public string NameKey { get; init; } = "name";

    /// <summary>The data key of a node's own fill, which its descendants inherit (Recharts reads <c>fill</c>); none when null, so a model without it is not probed for it.</summary>
    public string? FillKey { get; init; }

    /// <summary>The width of the stroke that separates the sectors (Recharts' <c>padding</c>).</summary>
    public double Padding { get; init; } = 2;

    /// <summary>The gap between a ring and the next.</summary>
    public double RingPadding { get; init; } = 2;

    /// <summary>The radius of the hole in the middle.</summary>
    public double InnerRadius { get; init; } = 50;

    /// <summary>The radius the rings share out; half the chart's smaller side when null.</summary>
    public double? OuterRadius { get; init; }

    /// <summary>The center's abscissa; the middle of the chart when null.</summary>
    public double? Cx { get; init; }

    /// <summary>The center's ordinate; the middle of the chart when null.</summary>
    public double? Cy { get; init; }

    /// <summary>The angle the first sector starts at, in degrees.</summary>
    public double StartAngle { get; init; }

    /// <summary>The angle the root's value spans, in degrees (Recharts scales the values to it, from 0).</summary>
    public double EndAngle { get; init; } = 360;

    /// <summary>The fill of a node without a fill of its own or inherited.</summary>
    public string? Fill { get; init; }

    /// <summary>The sectors' stroke.</summary>
    public string? Stroke { get; init; }

    /// <summary>How the values are written on the sectors (Recharts' <c>textOptions</c>, which replace its defaults as a whole).</summary>
    public TextOptionsSpec TextOptions { get; init; } = TextOptionsSpec.Default;
}

/// <summary>Recharts' <c>TextOptions</c>: the SVG text props a sunburst's values are written with; a null prop is left out.</summary>
public sealed record TextOptionsSpec
{
    /// <summary>Recharts' default options: bold, <c>.75rem</c>, black outlined in white, the outline painted first, no pointer events.</summary>
    public static TextOptionsSpec Default { get; } = new()
    {
        FontWeight = "bold",
        PaintOrder = "stroke fill",
        FontSize = ".75rem",
        Stroke = "#FFF",
        Fill = "black",
        PointerEvents = "none",
    };

    /// <summary>The font family.</summary>
    public string? FontFamily { get; init; }

    /// <summary>The font weight (<c>bold</c>, <c>normal</c> or a number).</summary>
    public string? FontWeight { get; init; }

    /// <summary>The paint order (<c>stroke fill</c> paints the outline under the fill).</summary>
    public string? PaintOrder { get; init; }

    /// <summary>The outline's color.</summary>
    public string? Stroke { get; init; }

    /// <summary>The text's color; Recharts' <c>Text</c> takes <c>#808080</c> when null.</summary>
    public string? Fill { get; init; }

    /// <summary>The font size as CSS writes it (<c>12px</c>, <c>.75rem</c>, <c>1em</c>).</summary>
    public string? FontSize { get; init; }

    /// <summary>The pointer events (<c>none</c> lets the pointer through to the sector).</summary>
    public string? PointerEvents { get; init; }
}
