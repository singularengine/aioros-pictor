using System.Collections.Immutable;

namespace Aioros.Pictor.Specs;

/// <summary>Recharts' <c>Funnel</c>: one trapezoid per item, its width the item's value over the largest.</summary>
public sealed record FunnelSpec
{
    /// <summary>The custom content drawn in place of each trapezoid (Recharts' <c>shape</c> element), compiled from its XAML template.</summary>
    public ContentSpec? ShapeContent { get; init; }

    /// <summary>The custom content drawn in place of the hovered trapezoid (Recharts' <c>activeShape</c> element), compiled from its XAML template.</summary>
    public ContentSpec? ActiveShapeContent { get; init; }

    /// <summary>The trapezoid drawn active whatever the pointer does (Recharts' <c>activeIndex</c>).</summary>
    public int? ActiveIndex { get; init; }

    /// <summary>The trapezoids drawn active whatever the pointer does, when <c>activeIndex</c> is an array.</summary>
    public ImmutableArray<int> ActiveIndexes { get; init; } = [];

    /// <summary>The key of the command its items run when clicked (Recharts' <c>onClick</c>), written as buttons on the web; none when no command is set.</summary>
    public string? ClickCommand { get; init; }

    /// <summary>The data key of the values.</summary>
    public string DataKey { get; init; } = "value";

    /// <summary>The data key of the items' names (the tooltip's names).</summary>
    public string NameKey { get; init; } = "name";

    /// <summary>The funnel's name in the legend; the data key when null.</summary>
    public string? Name { get; init; }

    /// <summary>The trapezoids' fill, where an item and its cell have none.</summary>
    public string? Fill { get; init; }

    /// <summary>The trapezoids' stroke.</summary>
    public string? Stroke { get; init; }

    /// <summary>Whether the items run from the bottom up, the widest at the bottom.</summary>
    public bool Reversed { get; init; }

    /// <summary>Whether the last item ends in a point (<c>triangle</c>) or keeps its width (<c>rectangle</c>).</summary>
    public bool LastRectangle { get; init; }

    /// <summary>The funnel's width: pixels, a percentage of the plot (<c>80%</c>), or null for the whole plot.</summary>
    public string? Width { get; init; }

    /// <summary>The funnel's own items (Recharts' <c>data</c>), or null to draw the chart's data.</summary>
    public DataTable? Data { get; init; }

    /// <summary>The colors of individual trapezoids (Recharts' <c>Cell</c>), by item.</summary>
    public ImmutableArray<CellSpec> Cells { get; init; } = [];

    /// <summary>The labels of the trapezoids (Recharts' <c>LabelList</c>), if any.</summary>
    public LabelSpec? Labels { get; init; }

    /// <summary>The funnel's further label lists, drawn after <see cref="Labels"/>.</summary>
    public ImmutableArray<LabelSpec> LabelLists { get; init; } = [];

    /// <summary>The legend icon.</summary>
    public LegendIcon LegendType { get; init; } = LegendIcon.Rect;

    /// <summary>Whether the funnel is hidden.</summary>
    public bool Hide { get; init; }

    /// <summary>Whether the tooltip leaves its entries out (Recharts' <c>tooltipType="none"</c>).</summary>
    public bool TooltipNone { get; init; }

    /// <summary>The key of an item's own fill (Recharts spreads an item's <c>fill</c> onto its trapezoid); none by default.</summary>
    public string? FillKey { get; init; }

    /// <summary>The data key of each item's own opacity, as Recharts spreads an item's <c>opacity</c> onto its trapezoid; none when null.</summary>
    public string? OpacityKey { get; init; }

    /// <summary>The key of an item's own stroke; none by default.</summary>
    public string? StrokeKey { get; init; }

    /// <summary>The enter animation; null for Recharts' defaults of the kind.</summary>
    public AnimationSpec? Animation { get; init; }
}
