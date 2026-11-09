using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace Aioros.Pictor;

/// <summary>Avalonia's colors and brushes as the specs write paints: colors as hex text, linear gradients as gradient specs.</summary>
internal static class Paints
{
    /// <summary>A color as <c>#rrggbb</c>, or <c>#rrggbbaa</c> when it, or the brush holding it, is translucent.</summary>
    public static string Color(Color color, double opacity)
    {
        var alpha = (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1));
        return alpha == 255
            ? string.Create(CultureInfo.InvariantCulture, $"#{color.R:x2}{color.G:x2}{color.B:x2}")
            : string.Create(CultureInfo.InvariantCulture, $"#{color.R:x2}{color.G:x2}{color.B:x2}{alpha:x2}");
    }

    /// <summary>
    /// A linear gradient brush as a gradient spec: its points relative to the painted shape's box (or in the chart's pixels
    /// when absolute), each stop's color with its alpha and the brush's opacity as the stop's opacity, named by its content.
    /// </summary>
    public static Specs.GradientSpec Gradient(ILinearGradientBrush brush)
    {
        var user = brush.StartPoint.Unit == RelativeUnit.Absolute;
        var gradient = new Specs.GradientSpec
        {
            Id = string.Empty,
            X1 = brush.StartPoint.Point.X,
            Y1 = brush.StartPoint.Point.Y,
            X2 = brush.EndPoint.Point.X,
            Y2 = brush.EndPoint.Point.Y,
            UserSpace = user,
            Stops = [.. brush.GradientStops.OrderBy(stop => stop.Offset).Select(stop =>
                new Specs.GradientStopSpec(stop.Offset, Color(Avalonia.Media.Color.FromRgb(stop.Color.R, stop.Color.G, stop.Color.B), 1), Math.Round(stop.Color.A / 255.0 * Math.Clamp(brush.Opacity, 0, 1), 4)))],
        };
        return gradient with { Id = Specs.GradientSpec.IdOf(gradient) };
    }
}
