using Aioros.Pictor.Specs;
using Avalonia;

namespace Aioros.Pictor.Demo;

/// <summary>A spec's SVG presentation attributes on the control built from it.</summary>
internal static class PresentationControls
{
    /// <summary>An element with the spec's SVG presentation attributes set as its attached properties.</summary>
    public static T Of<T>(T element, SvgPresentation? presentation)
        where T : StyledElement
    {
        if (presentation is not null)
        {
            Presentation.SetOpacity(element, presentation.Opacity);
            Presentation.SetStrokeOpacity(element, presentation.StrokeOpacity);
            Presentation.SetStrokeLinecap(element, presentation.StrokeLinecap);
            Presentation.SetStrokeLinejoin(element, presentation.StrokeLinejoin);
            Presentation.SetStrokeMiterlimit(element, presentation.StrokeMiterlimit);
            Presentation.SetFontWeight(element, presentation.FontWeight);
            Presentation.SetFontStyle(element, presentation.FontStyle);
        }

        return element;
    }
}
