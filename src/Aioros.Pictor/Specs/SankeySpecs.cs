namespace Aioros.Pictor.Specs;

/// <summary>Recharts' <c>Sankey</c>: nodes in columns by depth and links between them as wide curves.</summary>
public sealed record SankeySpec
{
    /// <summary>The custom content drawn for each node (Recharts' <c>node</c> element), compiled from its XAML template.</summary>
    public ContentSpec? NodeContent { get; init; }

    /// <summary>The custom content drawn for each link (Recharts' <c>link</c> element), compiled from its XAML template.</summary>
    public ContentSpec? LinkContent { get; init; }

    /// <summary>The data key of the nodes' names.</summary>
    public string NameKey { get; init; } = "name";

    /// <summary>The vertical gap between two nodes of a column.</summary>
    public double NodePadding { get; init; } = 10;

    /// <summary>The nodes' width.</summary>
    public double NodeWidth { get; init; } = 10;

    /// <summary>How far a link's control points sit from its ends, as a share of its length.</summary>
    public double LinkCurvature { get; init; } = 0.5;

    /// <summary>How many relaxation passes move the nodes toward their links.</summary>
    public int Iterations { get; init; } = 32;

    /// <summary>Whether the nodes of a column are sorted by position at every pass.</summary>
    public bool Sort { get; init; } = true;

    /// <summary>The nodes' fill.</summary>
    public string? NodeFill { get; init; }

    /// <summary>The nodes' fill opacity.</summary>
    public double NodeFillOpacity { get; init; } = 0.8;

    /// <summary>The nodes' stroke, if any.</summary>
    public string? NodeStroke { get; init; }

    /// <summary>The nodes' stroke width, when set.</summary>
    public double? NodeStrokeWidth { get; init; }

    /// <summary>The nodes' stroke opacity, when set.</summary>
    public double? NodeStrokeOpacity { get; init; }

    /// <summary>The nodes' stroke dash pattern, when set.</summary>
    public string? NodeStrokeDashArray { get; init; }

    /// <summary>A stroke width for every link instead of its value's width (Recharts spreads the <c>link</c> props over it), when set.</summary>
    public double? LinkStrokeWidth { get; init; }

    /// <summary>The links' fill, when set (none by default).</summary>
    public string? LinkFill { get; init; }

    /// <summary>The links' stroke dash pattern, when set.</summary>
    public string? LinkStrokeDashArray { get; init; }

    /// <summary>The links' stroke.</summary>
    public string? LinkStroke { get; init; }

    /// <summary>The links' stroke opacity.</summary>
    public double LinkStrokeOpacity { get; init; } = 0.2;
}
