using Aioros.Pictor.Specs;
using Avalonia;

namespace Aioros.Pictor;

/// <summary>
/// SVG presentation attributes Recharts spreads onto an element's shapes and texts, as attached properties of the chart
/// elements that take them (<see cref="XAxis"/>, <see cref="YAxis"/>, <see cref="Line"/>, <see cref="Area"/>,
/// <see cref="ErrorBar"/>, <see cref="CartesianGrid"/>): <c>&lt;p:Line p:Presentation.StrokeLinecap="round" p:Presentation.Opacity="0.8" /&gt;</c>.
/// </summary>
public static class Presentation
{
    /// <summary>Defines the element's opacity (Recharts' <c>opacity</c>).</summary>
    public static readonly AttachedProperty<double?> OpacityProperty = AvaloniaProperty.RegisterAttached<StyledElement, double?>("Opacity", typeof(Presentation));

    /// <summary>Defines the stroke's opacity (<c>strokeOpacity</c>).</summary>
    public static readonly AttachedProperty<double?> StrokeOpacityProperty = AvaloniaProperty.RegisterAttached<StyledElement, double?>("StrokeOpacity", typeof(Presentation));

    /// <summary>Defines the stroke's ends: <c>butt</c>, <c>round</c> or <c>square</c> (<c>strokeLinecap</c>).</summary>
    public static readonly AttachedProperty<string?> StrokeLinecapProperty = AvaloniaProperty.RegisterAttached<StyledElement, string?>("StrokeLinecap", typeof(Presentation));

    /// <summary>Defines the stroke's corners: <c>miter</c>, <c>round</c> or <c>bevel</c> (<c>strokeLinejoin</c>).</summary>
    public static readonly AttachedProperty<string?> StrokeLinejoinProperty = AvaloniaProperty.RegisterAttached<StyledElement, string?>("StrokeLinejoin", typeof(Presentation));

    /// <summary>Defines the stroke's miter limit (<c>strokeMiterlimit</c>).</summary>
    public static readonly AttachedProperty<double?> StrokeMiterlimitProperty = AvaloniaProperty.RegisterAttached<StyledElement, double?>("StrokeMiterlimit", typeof(Presentation));

    /// <summary>Defines the texts' weight (<c>fontWeight</c>: <c>bold</c>, <c>600</c>...).</summary>
    public static readonly AttachedProperty<string?> FontWeightProperty = AvaloniaProperty.RegisterAttached<StyledElement, string?>("FontWeight", typeof(Presentation));

    /// <summary>Defines the texts' style (<c>fontStyle</c>: <c>italic</c>, <c>oblique</c>).</summary>
    public static readonly AttachedProperty<string?> FontStyleProperty = AvaloniaProperty.RegisterAttached<StyledElement, string?>("FontStyle", typeof(Presentation));

    /// <summary>Gets the element's opacity.</summary>
    /// <param name="element">The chart element.</param>
    /// <returns>The opacity, or null.</returns>
    public static double? GetOpacity(StyledElement element) => Checked(element).GetValue(OpacityProperty);

    /// <summary>Sets the element's opacity.</summary>
    /// <param name="element">The chart element.</param>
    /// <param name="value">The opacity, or null.</param>
    public static void SetOpacity(StyledElement element, double? value) => Checked(element).SetValue(OpacityProperty, value);

    /// <summary>Gets the stroke's opacity.</summary>
    /// <param name="element">The chart element.</param>
    /// <returns>The opacity, or null.</returns>
    public static double? GetStrokeOpacity(StyledElement element) => Checked(element).GetValue(StrokeOpacityProperty);

    /// <summary>Sets the stroke's opacity.</summary>
    /// <param name="element">The chart element.</param>
    /// <param name="value">The opacity, or null.</param>
    public static void SetStrokeOpacity(StyledElement element, double? value) => Checked(element).SetValue(StrokeOpacityProperty, value);

    /// <summary>Gets the stroke's ends.</summary>
    /// <param name="element">The chart element.</param>
    /// <returns>The ends, or null.</returns>
    public static string? GetStrokeLinecap(StyledElement element) => Checked(element).GetValue(StrokeLinecapProperty);

    /// <summary>Sets the stroke's ends.</summary>
    /// <param name="element">The chart element.</param>
    /// <param name="value">The ends, or null.</param>
    public static void SetStrokeLinecap(StyledElement element, string? value) => Checked(element).SetValue(StrokeLinecapProperty, value);

    /// <summary>Gets the stroke's corners.</summary>
    /// <param name="element">The chart element.</param>
    /// <returns>The corners, or null.</returns>
    public static string? GetStrokeLinejoin(StyledElement element) => Checked(element).GetValue(StrokeLinejoinProperty);

    /// <summary>Sets the stroke's corners.</summary>
    /// <param name="element">The chart element.</param>
    /// <param name="value">The corners, or null.</param>
    public static void SetStrokeLinejoin(StyledElement element, string? value) => Checked(element).SetValue(StrokeLinejoinProperty, value);

    /// <summary>Gets the stroke's miter limit.</summary>
    /// <param name="element">The chart element.</param>
    /// <returns>The limit, or null.</returns>
    public static double? GetStrokeMiterlimit(StyledElement element) => Checked(element).GetValue(StrokeMiterlimitProperty);

    /// <summary>Sets the stroke's miter limit.</summary>
    /// <param name="element">The chart element.</param>
    /// <param name="value">The limit, or null.</param>
    public static void SetStrokeMiterlimit(StyledElement element, double? value) => Checked(element).SetValue(StrokeMiterlimitProperty, value);

    /// <summary>Gets the texts' weight.</summary>
    /// <param name="element">The chart element.</param>
    /// <returns>The weight, or null.</returns>
    public static string? GetFontWeight(StyledElement element) => Checked(element).GetValue(FontWeightProperty);

    /// <summary>Sets the texts' weight.</summary>
    /// <param name="element">The chart element.</param>
    /// <param name="value">The weight, or null.</param>
    public static void SetFontWeight(StyledElement element, string? value) => Checked(element).SetValue(FontWeightProperty, value);

    /// <summary>Gets the texts' style.</summary>
    /// <param name="element">The chart element.</param>
    /// <returns>The style, or null.</returns>
    public static string? GetFontStyle(StyledElement element) => Checked(element).GetValue(FontStyleProperty);

    /// <summary>Sets the texts' style.</summary>
    /// <param name="element">The chart element.</param>
    /// <param name="value">The style, or null.</param>
    public static void SetFontStyle(StyledElement element, string? value) => Checked(element).SetValue(FontStyleProperty, value);

    /// <summary>The element's presentation attributes as the spec carries them, or null when none is set.</summary>
    internal static SvgPresentation? Of(StyledElement element)
    {
        var presentation = new SvgPresentation
        {
            Opacity = element.GetValue(OpacityProperty),
            StrokeOpacity = element.GetValue(StrokeOpacityProperty),
            StrokeLinecap = element.GetValue(StrokeLinecapProperty),
            StrokeLinejoin = element.GetValue(StrokeLinejoinProperty),
            StrokeMiterlimit = element.GetValue(StrokeMiterlimitProperty),
            FontWeight = element.GetValue(FontWeightProperty),
            FontStyle = element.GetValue(FontStyleProperty),
        };
        return presentation.IsEmpty ? null : presentation;
    }

    private static StyledElement Checked(StyledElement element) => element ?? throw new ArgumentNullException(nameof(element));
}
