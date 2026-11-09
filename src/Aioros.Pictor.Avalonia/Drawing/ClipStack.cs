using Avalonia;
using Avalonia.Media;

namespace Aioros.Pictor.Drawing;

/// <summary>
/// The clips the desktop pushes for the painter's <see cref="Painting.IChartCanvas.ClipNext"/>: one for the next layer,
/// popped when that layer closes, or one around the next element only. Kept per chart and reused on every frame.
/// </summary>
internal sealed class ClipStack
{
    private readonly List<(int Depth, DrawingContext.PushedState State)> _pushed = [];
    private readonly List<(DrawingContext.PushedState Transform, DrawingContext.PushedState Clip)> _surfaces = [];
    private Rect? _next;
    private int _depth;

    public void Reset()
    {
        for (var i = _pushed.Count - 1; i >= 0; i--)
        {
            _pushed[i].State.Dispose();
        }

        _pushed.Clear();
        for (var i = _surfaces.Count - 1; i >= 0; i--)
        {
            _surfaces[i].Clip.Dispose();
            _surfaces[i].Transform.Dispose();
        }

        _surfaces.Clear();
        _next = null;
        _depth = 0;
    }

    public void Next(Rect rect) => _next = rect;

    public void Open(DrawingContext context)
    {
        _depth++;
        if (_next is { } rect)
        {
            _pushed.Add((_depth, context.PushClip(rect)));
            _next = null;
        }
    }

    /// <summary>Opens a layer drawn at an opacity as a whole, popped when it closes.</summary>
    public void OpenFaded(DrawingContext context, double opacity)
    {
        Open(context);
        if (!double.IsNaN(opacity))
        {
            _pushed.Add((_depth, context.PushOpacity(opacity)));
        }
    }

    public void Close()
    {
        for (var i = _pushed.Count - 1; i >= 0 && _pushed[i].Depth == _depth; i--)
        {
            _pushed[i].State.Dispose();
            _pushed.RemoveAt(i);
        }

        _depth--;
    }

    /// <summary>Moves the origin to a nested surface and clips to it until <see cref="CloseSurface"/>.</summary>
    public void OpenSurface(DrawingContext context, Rect surface)
    {
        var transform = context.PushTransform(Matrix.CreateTranslation(surface.X, surface.Y));
        _surfaces.Add((transform, context.PushClip(new Rect(surface.Size))));
    }

    public void CloseSurface()
    {
        var (transform, clip) = _surfaces[^1];
        _surfaces.RemoveAt(_surfaces.Count - 1);
        clip.Dispose();
        transform.Dispose();
    }

    /// <summary>The clip pushed around one element, if one is pending; dispose it once the element is drawn.</summary>
    public DrawingContext.PushedState? Element(DrawingContext context)
    {
        if (_next is not { } rect)
        {
            return null;
        }

        _next = null;
        return context.PushClip(rect);
    }
}
