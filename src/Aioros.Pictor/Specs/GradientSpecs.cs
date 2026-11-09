using System.Collections.Immutable;
using System.Globalization;

namespace Aioros.Pictor.Specs;

/// <summary>
/// A linear gradient a chart's paints name as <c>url(#Id)</c>, SVG's <c>linearGradient</c>: the line it runs along, from
/// (<see cref="X1"/>, <see cref="Y1"/>) to (<see cref="X2"/>, <see cref="Y2"/>), as fractions of the painted shape's box
/// (SVG's <c>objectBoundingBox</c>, Avalonia's relative points) or, with <see cref="UserSpace"/>, in the chart's pixels.
/// </summary>
public sealed record GradientSpec
{
    /// <summary>The id the paints name.</summary>
    public required string Id { get; init; }

    /// <summary>The line's start along x.</summary>
    public double X1 { get; init; }

    /// <summary>The line's start along y.</summary>
    public double Y1 { get; init; }

    /// <summary>The line's end along x (SVG's default, the box's right edge).</summary>
    public double X2 { get; init; } = 1;

    /// <summary>The line's end along y.</summary>
    public double Y2 { get; init; }

    /// <summary>Whether the line is in the chart's pixels rather than fractions of the shape's box.</summary>
    public bool UserSpace { get; init; }

    /// <summary>The colors along the line, in order.</summary>
    public ImmutableArray<GradientStopSpec> Stops { get; init; } = [];

    /// <summary>A stable id for a gradient defined without one (from an Avalonia brush): the same definition always gets the same id, so charts on one page that share a gradient share its id and different gradients never collide.</summary>
    /// <param name="gradient">The gradient, its id aside.</param>
    /// <returns>The id.</returns>
    public static string IdOf(GradientSpec gradient)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        var hash = 2166136261u;
        void Add(string text)
        {
            foreach (var c in text)
            {
                hash = (hash ^ c) * 16777619u;
            }
        }

        Add(string.Create(CultureInfo.InvariantCulture, $"{gradient.X1},{gradient.Y1},{gradient.X2},{gradient.Y2},{gradient.UserSpace}"));
        foreach (var stop in gradient.Stops)
        {
            Add(string.Create(CultureInfo.InvariantCulture, $";{stop.Offset},{stop.Color},{stop.Opacity}"));
        }

        return "pictor-gradient-" + hash.ToString("x8", CultureInfo.InvariantCulture);
    }
}

/// <summary>A gradient's color at an offset along its line (0 to 1), with its opacity (SVG's <c>stop</c>).</summary>
/// <param name="Offset">Where the color sits along the line, from 0 to 1.</param>
/// <param name="Color">The color.</param>
/// <param name="Opacity">The color's opacity, from 0 to 1.</param>
public sealed record GradientStopSpec(double Offset, string Color, double Opacity = 1);
