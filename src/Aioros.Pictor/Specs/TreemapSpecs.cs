using System.Collections.Immutable;

namespace Aioros.Pictor.Specs;

/// <summary>Recharts' <c>Treemap</c>: nested items squarified into rectangles, the size of each its value.</summary>
public sealed record TreemapSpec
{
    /// <summary>
    /// Recharts' <c>isUpdateAnimationActive</c>, which Recharts passes negated to each node's <c>Rectangle</c>: when false
    /// (and the treemap animates), each default rectangle draws its stroke in on mounting, as the <c>Rectangle</c>'s own
    /// animation does.
    /// </summary>
    public bool UpdateAnimation { get; init; } = true;

    /// <summary>The custom content drawn for each node (Recharts' <c>content</c> element), compiled from its XAML template.</summary>
    public ContentSpec? Content { get; init; }

    /// <summary>The custom content drawn for each entry of a nest treemap's trail (Recharts' <c>nestIndexContent</c>), compiled from its XAML template.</summary>
    public ContentSpec? NestIndexContent { get; init; }

    /// <summary>The data key of the leaves' values.</summary>
    public string DataKey { get; init; } = "value";

    /// <summary>The data key of the names the tooltip and the nest index show, or none (Recharts' default: the tooltip shows no name and the nest index reads <c>root</c>); the nodes' labels read their <c>name</c>.</summary>
    public string? NameKey { get; init; }

    /// <summary>The ratio of width to height squarify aims for.</summary>
    public double AspectRatio { get; init; } = 0.5 * (1 + Math.Sqrt(5));

    /// <summary>Whether only the first level is shown, with Recharts' nest index below (<c>nest</c>), or every level (<c>flat</c>).</summary>
    public bool Nest { get; init; }

    /// <summary>The rectangles' fill, over the color panel; null for the panel's colors.</summary>
    public string? Fill { get; init; }

    /// <summary>The rectangles' stroke, which the names take too; white rectangles without a stroke on the names when null.</summary>
    public string? Stroke { get; init; }

    /// <summary>The colors of the first two levels, by index; Recharts' panel when empty.</summary>
    public ImmutableArray<string> ColorPanel { get; init; } = [];

    /// <summary>A nest treemap's zoomed node, as the indexes from the root separated by dots (<c>2.1</c>); the root when null or empty.</summary>
    public string? Zoom { get; init; }

    /// <summary>The key of the command a nest treemap's nodes and nest index run to zoom, written as buttons on the web; none when it does not zoom there.</summary>
    public string? ZoomCommand { get; init; }

    /// <summary>Whether the nodes and the nest index zoom through a choice field bound to the zoom (one option per node), planned without a command; otherwise through <see cref="ZoomCommand"/>'s buttons.</summary>
    public bool ZoomChoice { get; init; }

    /// <summary>The enter animation; null for Recharts' defaults of the kind.</summary>
    public AnimationSpec? Animation { get; init; }
}
