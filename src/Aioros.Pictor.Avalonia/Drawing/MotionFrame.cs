using Aioros.Pictor.Motion;
using Aioros.Pictor.Svg;
using Avalonia;
using Avalonia.Media;

namespace Aioros.Pictor.Drawing;

/// <summary>
/// One frame of a chart's enter animation on the desktop: the time since the chart appeared and, while the painter walks
/// an animated series, its timing and the clips and transforms its layers pushed, so each shape is drawn as Recharts'
/// animation (and the web's CSS) shows it at that time; dots, labels and error bars wait for the series' end. A frame of
/// an update animation (<paramref name="enters"/> false) draws the morphed geometry as it is and only holds back what
/// waits for the end.
/// </summary>
internal sealed class MotionFrame(double elapsed, bool enters = true)
{
    private const double Margin = 20;
    private readonly List<(int Depth, IDisposable State)> _pushed = [];
    private int _depth;
    private int _late;
    private Timing _timing;
    private Step _step;

    /// <summary>Whether what is drawn now is hidden until its series has animated in.</summary>
    public bool Hidden => _late > 0;

    /// <summary>The eased progress of the series being drawn.</summary>
    public double Progress => _timing.Progress(elapsed);

    /// <summary>The eased progress of a stroke drawn in by its own timing (<see cref="StepKind.Stroke"/>), mounted at once or at the series' end.</summary>
    public double StrokeProgress(bool late) => _timing.StrokeProgress(elapsed, late);

    public void Animate(in Step step) => _step = enters ? step : default;

    public Step Take()
    {
        var step = _step;
        _step = default;
        return step;
    }

    public void OpenSeries(DrawingContext context, in Timing timing)
    {
        _timing = timing;
        _depth = 1;
        Layer(context);
    }

    public void Open(DrawingContext context, ReadOnlySpan<byte> classes)
    {
        if (_depth == 0)
        {
            _step = default;
            return;
        }

        _depth++;
        if (_late == 0 && elapsed < _timing.End && (SvgMotion.IsLate(classes) || _step.Kind == StepKind.Hold))
        {
            _late = _depth;
        }

        Layer(context);
    }

    public void Close()
    {
        if (_depth == 0)
        {
            return;
        }

        for (var i = _pushed.Count - 1; i >= 0 && _pushed[i].Depth == _depth; i--)
        {
            _pushed[i].State.Dispose();
            _pushed.RemoveAt(i);
        }

        _late = _late == _depth ? 0 : _late;
        _depth--;
    }

    /// <summary>Pushes what a step does to a shape with the given bounds; false when the shape is not drawn at all yet.</summary>
    public bool Push(DrawingContext context, in Step step, Rect bounds, out IDisposable? state)
    {
        state = null;
        var p = Progress;
        if (p >= 1 || step.Kind == StepKind.None)
        {
            return true;
        }

        if (p <= 0)
        {
            return false;
        }

        state = step.Kind switch
        {
            StepKind.Reveal => context.PushClip(new Rect(bounds.Left - Margin, bounds.Top - Margin, (p * bounds.Width) + Margin, bounds.Height + (2 * Margin))),
            StepKind.RevealDown => context.PushClip(new Rect(bounds.Left - Margin, bounds.Top - Margin, bounds.Width + (2 * Margin), (p * bounds.Height) + Margin)),
            StepKind.GrowUp => context.PushTransform(Scale(bounds.Center.X, bounds.Bottom, 1, p)),
            StepKind.GrowDown => context.PushTransform(Scale(bounds.Center.X, bounds.Top, 1, p)),
            StepKind.GrowRight => context.PushTransform(Scale(bounds.Left, bounds.Center.Y, p, 1)),
            StepKind.GrowLeft => context.PushTransform(Scale(bounds.Right, bounds.Center.Y, p, 1)),
            StepKind.Scale => context.PushTransform(Scale(bounds.Center.X, bounds.Center.Y, p, p)),
            StepKind.ScaleArea => context.PushTransform(Scale(bounds.Center.X, bounds.Center.Y, Math.Sqrt(p), Math.Sqrt(p))),
            StepKind.Center => context.PushTransform(Scale(step.Cx, step.Cy, p, p)),
            StepKind.Converge => context.PushTransform(Matrix.CreateTranslation((1 - p) * step.Cx, (1 - p) * step.Cy)),
            StepKind.Slide => context.PushTransform(Matrix.CreateTranslation((1 - p) * step.Offset, (1 - p) * step.Offset)),
            _ => null,
        };
        return true;
    }

    private void Layer(DrawingContext context)
    {
        var step = Take();
        if (step.Kind is StepKind.None or StepKind.Hold or StepKind.Shown)
        {
            return;
        }

        if (!Push(context, step, default, out var state))
        {
            _late = _late == 0 ? _depth : _late;
        }
        else if (state is not null)
        {
            _pushed.Add((_depth, state));
        }
    }

    private static Matrix Scale(double x, double y, double sx, double sy) =>
        Matrix.CreateTranslation(-x, -y) * Matrix.CreateScale(sx, sy) * Matrix.CreateTranslation(x, y);
}
