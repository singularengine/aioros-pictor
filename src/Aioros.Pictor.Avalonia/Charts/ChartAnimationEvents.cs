using Aioros.Pictor.Motion;

namespace Aioros.Pictor;

/// <summary>An element whose animation raises Recharts' <c>onAnimationStart</c> and <c>onAnimationEnd</c> on the desktop: when its timing ends, and how to raise them.</summary>
internal interface IAnimatedElement
{
    /// <summary>The element's timing (its kind's defaults under its animation props), or null when it does not animate.</summary>
    Timing? Timing { get; }

    /// <summary>Raises the element's start or end event.</summary>
    void RaiseAnimation(bool ended);
}

/// <summary>
/// The animation events of a desktop chart's elements, as Recharts raises them (react-smooth's <c>onAnimationStart</c>
/// as the animation starts, before its <c>animationBegin</c> delay, and <c>onAnimationEnd</c> once it has moved): an
/// enter or an update animation starts every animated element's, and each element's end is raised at the first frame
/// past its own end (the last frame raises those left). The events are posted to the dispatcher, outside the render.
/// </summary>
internal sealed class ChartAnimationEvents(Func<IEnumerable<IAnimatedElement>> elements)
{
    private readonly List<(IAnimatedElement Element, double End)> _pending = [];

    /// <summary>An animation starts: every animated element's start, its end pending.</summary>
    public void Start()
    {
        Flush();
        foreach (var element in elements())
        {
            if (element.Timing is { } timing)
            {
                _pending.Add((element, timing.Moved));
                Post(element, ended: false);
            }
        }
    }

    /// <summary>A frame at this time of the animation: the ends it has passed.</summary>
    public void At(double elapsed)
    {
        for (var i = _pending.Count - 1; i >= 0; i--)
        {
            if (elapsed >= _pending[i].End)
            {
                Post(_pending[i].Element, ended: true);
                _pending.RemoveAt(i);
            }
        }
    }

    /// <summary>The animation is over: the ends left.</summary>
    public void Flush()
    {
        foreach (var (element, _) in _pending)
        {
            Post(element, ended: true);
        }

        _pending.Clear();
    }

    private static void Post(IAnimatedElement element, bool ended) =>
        Avalonia.Threading.Dispatcher.UIThread.Post(() => element.RaiseAnimation(ended));
}
