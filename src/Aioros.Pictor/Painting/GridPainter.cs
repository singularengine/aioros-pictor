using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>
/// Recharts' <c>CartesianGrid</c>: its background, its horizontal and vertical lines (at the axes' ticks and the plot's
/// edges, at the ticks only with <c>syncWithTicks</c>, or at points of its own) and its stripes, in that order.
/// </summary>
internal static class GridPainter
{
    public static void Paint<TCanvas>(ref TCanvas canvas, ChartLayout layout, GridSpec grid)
        where TCanvas : struct, IChartCanvas
    {
        var (x, y, width, height) = (grid.X ?? layout.Left, grid.Y ?? layout.Top, grid.Width ?? layout.PlotWidth, grid.Height ?? layout.PlotHeight);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var (rows, columns) = (Room(grid.HorizontalPoints, layout.YAxes), Room(grid.VerticalPoints, layout.XAxes));
        var horizontal = rows <= 64 ? stackalloc double[rows] : new double[rows];
        var vertical = columns <= 64 ? stackalloc double[columns] : new double[columns];
        horizontal = horizontal[..Points(grid.HorizontalPoints, layout.YAxes, layout.Top, layout.Top + layout.PlotHeight, grid.SyncWithTicks, horizontal)];
        vertical = vertical[..Points(grid.VerticalPoints, layout.XAxes, layout.Left, layout.Left + layout.PlotWidth, grid.SyncWithTicks, vertical)];
        var box = (x, y, width, height);
        canvas.Open("recharts-cartesian-grid"u8);
        if (grid.Fill is { } fill && fill != "none")
        {
            canvas.Rect(x, y, width, height, new Paint("none", double.NaN, fill, grid.FillOpacity ?? double.NaN), "recharts-cartesian-grid-bg"u8);
        }

        if (grid.Horizontal)
        {
            GridLines(ref canvas, grid, layout.Spec.Colors.GridStroke, horizontal, box, across: false);
        }

        if (grid.Vertical)
        {
            GridLines(ref canvas, grid, layout.Spec.Colors.GridStroke, vertical, box, across: true);
        }

        if (grid.Horizontal && !grid.HorizontalFill.IsDefaultOrEmpty)
        {
            Stripes(ref canvas, grid, horizontal, box, across: false);
        }

        if (grid.Vertical && !grid.VerticalFill.IsDefaultOrEmpty)
        {
            Stripes(ref canvas, grid, vertical, box, across: true);
        }

        canvas.Close();
    }

    /// <summary>The most lines a direction can have: its own points, or its first axis' ticks and the plot's two edges.</summary>
    private static int Room(System.Collections.Immutable.ImmutableArray<double> points, List<AxisLayout> axes) =>
        !points.IsDefaultOrEmpty ? points.Length : axes.Count > 0 ? (axes[0].GridValues ?? axes[0].Visible).Length + 2 : 0;

    /// <summary>A grid's own points, or its first axis' lines, written into <paramref name="lines"/>; how many there are.</summary>
    private static int Points(System.Collections.Immutable.ImmutableArray<double> points, List<AxisLayout> axes, double min, double max, bool sync, Span<double> lines)
    {
        if (!points.IsDefaultOrEmpty)
        {
            points.AsSpan().CopyTo(lines);
            return points.Length;
        }

        return axes.Count > 0 ? Lines(axes[0].GridValues ?? axes[0].Visible, min, max, sync || axes[0].GridValues is not null, lines) : 0;
    }

    private static void GridLines<TCanvas>(ref TCanvas canvas, GridSpec grid, string themed, ReadOnlySpan<double> lines, (double X, double Y, double Width, double Height) box, bool across)
        where TCanvas : struct, IChartCanvas
    {
        if (lines.IsEmpty)
        {
            return;
        }

        var paint = new Paint(grid.Stroke ?? themed, grid.StrokeWidth ?? double.NaN, "none", grid.FillOpacity ?? double.NaN, grid.StrokeDashArray, grid.StrokeOpacity ?? double.NaN).With(grid.Presentation);
        canvas.Open(across ? "recharts-cartesian-grid-vertical"u8 : "recharts-cartesian-grid-horizontal"u8);
        foreach (var line in lines)
        {
            if (across)
            {
                canvas.Line(line, box.Y, line, box.Y + box.Height, paint, ClassNames.With(default, grid.ClassName));
            }
            else
            {
                canvas.Line(box.X, line, box.X + box.Width, line, paint, ClassNames.With(default, grid.ClassName));
            }
        }

        canvas.Close();
    }

    /// <summary>Recharts' <c>getCoordinatesOfGrid</c>: the ticks' coordinates, and the plot's edges unless synced with the ticks.</summary>
    private static int Lines(AxisTick[] ticks, double min, double max, bool sync, Span<double> lines)
    {
        var count = 0;
        var hasMin = false;
        var hasMax = false;
        for (var i = 0; i < ticks.Length; i++)
        {
            var coordinate = ticks[i].Coordinate;
            hasMin |= coordinate == min;
            hasMax |= coordinate == max;
            lines[count++] = coordinate;
        }

        if (!sync && !hasMin)
        {
            lines[count++] = min;
        }

        if (!sync && !hasMax)
        {
            lines[count++] = max;
        }

        return count;
    }

    /// <summary>
    /// Recharts' <c>HorizontalStripes</c> and <c>VerticalStripes</c>: the lines' coordinates rounded (half up) and sorted,
    /// a stripe from each to the next (from 0 when the first is not the grid's edge, as Recharts does), the last to the
    /// grid's far edge, the fills cycled.
    /// </summary>
    private static void Stripes<TCanvas>(ref TCanvas canvas, GridSpec grid, ReadOnlySpan<double> lines, (double X, double Y, double Width, double Height) box, bool across)
        where TCanvas : struct, IChartCanvas
    {
        var points = new List<double>(lines.Length + 1);
        foreach (var line in lines)
        {
            points.Add(Math.Floor(line + 0.5));
        }

        points.Sort();
        var (start, length) = across ? (box.X, box.Width) : (box.Y, box.Height);
        if (points.Count == 0 || start != points[0])
        {
            points.Insert(0, 0);
        }

        var fills = across ? grid.VerticalFill : grid.HorizontalFill;
        canvas.Open(across ? "recharts-cartesian-gridstripes-vertical"u8 : "recharts-cartesian-gridstripes-horizontal"u8);
        for (var i = 0; i < points.Count; i++)
        {
            var last = i + 1 >= points.Count || points[i + 1] == 0;
            var size = last ? start + length - points[i] : points[i + 1] - points[i];
            if (size <= 0)
            {
                continue;
            }

            var paint = new Paint("none", double.NaN, fills[i % fills.Length], grid.FillOpacity ?? double.NaN);
            if (across)
            {
                canvas.Rect(points[i], box.Y, size, box.Height, paint, "recharts-cartesian-grid-bg"u8);
            }
            else
            {
                canvas.Rect(box.X, points[i], box.Width, size, paint, "recharts-cartesian-grid-bg"u8);
            }
        }

        canvas.Close();
    }
}
