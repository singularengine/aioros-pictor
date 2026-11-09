using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Demo;

/// <summary>The paints of an example as XAML sets them.</summary>
internal static class ExampleBrushes
{
    /// <summary>A paint as XAML sets it: a gradient the example defines as an Avalonia <c>LinearGradientBrush</c>, any other color as its text.</summary>
    public static object? Brush(string? paint, System.Collections.Immutable.ImmutableArray<GradientSpec> gradients)
    {
        if (paint is null || !paint.StartsWith("url(#", StringComparison.Ordinal) || gradients.FirstOrDefault(known => paint == "url(#" + known.Id + ")") is not { } gradient)
        {
            return paint;
        }

        var unit = gradient.UserSpace ? global::Avalonia.RelativeUnit.Absolute : global::Avalonia.RelativeUnit.Relative;
        var brush = new global::Avalonia.Media.LinearGradientBrush
        {
            StartPoint = new global::Avalonia.RelativePoint(gradient.X1, gradient.Y1, unit),
            EndPoint = new global::Avalonia.RelativePoint(gradient.X2, gradient.Y2, unit),
        };
        foreach (var stop in gradient.Stops)
        {
            var color = global::Avalonia.Media.Color.Parse(stop.Color);
            brush.GradientStops.Add(new global::Avalonia.Media.GradientStop(global::Avalonia.Media.Color.FromArgb((byte)Math.Round(255 * stop.Opacity), color.R, color.G, color.B), stop.Offset));
        }

        return brush;
    }
}
