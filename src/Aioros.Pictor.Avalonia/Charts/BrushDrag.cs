using Aioros.Pictor.Layout;

namespace Aioros.Pictor;

/// <summary>
/// A drag of a brush on the desktop, as Recharts' <c>Brush</c> handles it: a traveller follows the pointer within the
/// strip, the slide moves both travellers without leaving it, and the window becomes the indexes at the travellers.
/// </summary>
internal sealed class BrushDrag
{
    private readonly bool _start;
    private readonly bool _end;
    private double _pointer;

    private BrushDrag(bool start, bool end, double pointer, double startX, double endX)
    {
        (_start, _end, _pointer, StartX, EndX) = (start, end, pointer, startX, endX);
    }

    public double StartX { get; private set; }

    public double EndX { get; private set; }

    /// <summary>A timer that runs <paramref name="leave"/> once, after Recharts' <c>leaveTimeOut</c> in milliseconds, on the clock given.</summary>
    public static ITimer Leave(TimeProvider time, double milliseconds, Action leave) =>
        time.CreateTimer(_ => leave(), null, TimeSpan.FromMilliseconds(milliseconds), Timeout.InfiniteTimeSpan);

    /// <summary>The drag a press starts: on a traveller, on the slide between them, or none.</summary>
    public static BrushDrag? Start(BrushLayout brush, double x, double y)
    {
        if (y < brush.Y || y > brush.Y + brush.Height)
        {
            return null;
        }

        var width = brush.Spec.TravellerWidth;
        if (x >= brush.StartX && x <= brush.StartX + width)
        {
            return new BrushDrag(true, false, x, brush.StartX, brush.EndX);
        }

        if (x >= brush.EndX && x <= brush.EndX + width)
        {
            return new BrushDrag(false, true, x, brush.StartX, brush.EndX);
        }

        var low = Math.Min(brush.StartX, brush.EndX) + width;
        var high = Math.Max(brush.StartX, brush.EndX);
        return x > low && x < high ? new BrushDrag(true, true, x, brush.StartX, brush.EndX) : null;
    }

    /// <summary>Moves the dragged part to the pointer, kept inside the strip, and returns the window it selects.</summary>
    public (int Start, int End) Move(BrushLayout brush, double x)
    {
        var first = brush.X;
        var last = brush.X + brush.Width - brush.Spec.TravellerWidth;
        var delta = x - _pointer;
        _pointer = x;
        if (_start && _end)
        {
            delta = delta > 0 ? Math.Min(delta, Math.Min(last - EndX, last - StartX)) : Math.Max(delta, Math.Max(first - StartX, first - EndX));
            StartX += delta;
            EndX += delta;
        }
        else if (_start)
        {
            StartX = Math.Max(Math.Min(StartX + delta, last), first);
        }
        else
        {
            EndX = Math.Max(Math.Min(EndX + delta, last), first);
        }

        return brush.Indexes(StartX, EndX);
    }
}
