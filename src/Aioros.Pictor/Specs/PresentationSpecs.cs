namespace Aioros.Pictor.Specs;

/// <summary>
/// SVG presentation attributes Recharts spreads onto an element's shapes and texts (its <c>filterProps</c>): what the
/// chart's own props leave out. Each is drawn on both targets: written as the SVG attribute on the web, taken by the
/// pen, the opacity and the typeface on the desktop.
/// </summary>
public sealed record SvgPresentation
{
    /// <summary>The element's opacity (<c>opacity</c>).</summary>
    public double? Opacity { get; init; }

    /// <summary>The stroke's opacity (<c>strokeOpacity</c>).</summary>
    public double? StrokeOpacity { get; init; }

    /// <summary>The stroke's ends: <c>butt</c>, <c>round</c> or <c>square</c> (<c>strokeLinecap</c>).</summary>
    public string? StrokeLinecap { get; init; }

    /// <summary>The stroke's corners: <c>miter</c>, <c>round</c> or <c>bevel</c> (<c>strokeLinejoin</c>).</summary>
    public string? StrokeLinejoin { get; init; }

    /// <summary>The longest miter, over the stroke's width (<c>strokeMiterlimit</c>).</summary>
    public double? StrokeMiterlimit { get; init; }

    /// <summary>The texts' weight (<c>fontWeight</c>: <c>bold</c>, <c>600</c>...).</summary>
    public string? FontWeight { get; init; }

    /// <summary>The texts' style (<c>fontStyle</c>: <c>italic</c>, <c>oblique</c>).</summary>
    public string? FontStyle { get; init; }

    /// <summary>Whether no attribute is set.</summary>
    public bool IsEmpty => this is { Opacity: null, StrokeOpacity: null, StrokeLinecap: null, StrokeLinejoin: null, StrokeMiterlimit: null, FontWeight: null, FontStyle: null };
}
