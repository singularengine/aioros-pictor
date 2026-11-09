using Avalonia;
using Avalonia.Media;

namespace Aioros.Pictor;

/// <summary>
/// Avalonia's values as Pictor's specs read them, the same on the desktop and for the web's bound values: a color or a
/// solid brush as its color text, a linear gradient brush as its gradient, any other brush as no paint, a corner radius
/// as Recharts' radius list (<c>[topLeft, topRight, bottomRight, bottomLeft]</c>), and a side of a thickness, a corner
/// of a corner radius or a coordinate of a point when a hole reads one; any other value as it is.
/// </summary>
internal static class AvaloniaValues
{
    public static object? Plain(object? value, string? member) => value switch
    {
        Color color => Paints.Color(color, 1),
        ISolidColorBrush solid => Paints.Color(solid.Color, solid.Opacity),
        ILinearGradientBrush linear => Paints.Gradient(linear),
        IBrush => null,
        Thickness thickness => member switch
        {
            nameof(Thickness.Left) => thickness.Left,
            nameof(Thickness.Top) => thickness.Top,
            nameof(Thickness.Right) => thickness.Right,
            nameof(Thickness.Bottom) => thickness.Bottom,
            _ => value,
        },
        CornerRadius radius => member switch
        {
            nameof(CornerRadius.TopLeft) => radius.TopLeft,
            nameof(CornerRadius.TopRight) => radius.TopRight,
            nameof(CornerRadius.BottomRight) => radius.BottomRight,
            nameof(CornerRadius.BottomLeft) => radius.BottomLeft,
            _ => new[] { radius.TopLeft, radius.TopRight, radius.BottomRight, radius.BottomLeft },
        },
        Point point => member switch
        {
            nameof(Point.X) => point.X,
            nameof(Point.Y) => point.Y,
            _ => value,
        },
        _ => value,
    };
}
