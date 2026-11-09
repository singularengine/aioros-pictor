using Aioros.Pictor.Drawing;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Motion;
using Aioros.Pictor.Painting;

namespace Aioros.Pictor;

/// <summary>
/// A desktop chart's animations, read from its clock at each frame: Recharts' enter animation from the time the chart
/// is first drawn (a polar chart's as Recharts plays it, its update from nothing), then its update animation from the layout drawn last to the new one whenever the data, the size or
/// the inner margin change (Recharts' <c>animationId</c>), each series moving by its own timing. A change of the chart's
/// children (a prop, a toggled legend item, a dragged brush) draws the new layout at once, as Recharts does when its
/// <c>animationId</c> stays.
/// </summary>
internal sealed class ChartMotion
{
    private long? _start;
    private bool _played;
    private IChartPlot? _drawn;
    private IChartPlot? _from;
    private Morph? _morph;
    private long _morphStart;
    private (IChartPlot Plot, Morph? Morph)? _enter;

    /// <summary>The chart's elements' animation events, when it raises them.</summary>
    public ChartAnimationEvents? Events { get; set; }

    /// <summary>The clock the animations read (the system's; a test's own).</summary>
    public TimeProvider Time { get; set; } = TimeProvider.System;

    /// <summary>Whether the enter animation has played to its end.</summary>
    public bool EnterPlayed => _played;

    /// <summary>Whether an update animation is drawing its frames.</summary>
    public bool Updating => _morph is not null || _from is not null;

    /// <summary>The data, size or margin changed: the next layout animates from the one drawn last (while the enter animation plays, from its end), from now.</summary>
    public void Update()
    {
        if (_drawn is null)
        {
            return;
        }

        _from = _drawn;
        _morph = null;
        _played = true;
        _morphStart = Time.GetTimestamp();
        Events?.Start();
    }

    /// <summary>Plays the enter animation again from now, as a remounted Recharts chart does.</summary>
    public void Replay()
    {
        (_start, _played, _enter, _from, _morph) = (null, false, null, null, null);
    }

    /// <summary>Something else changed: the next layout is drawn at once, ending any update animation.</summary>
    public void Reset()
    {
        _from = null;
        _morph = null;
    }

    /// <summary>
    /// The plot to draw now and the frame that animates it, or null when the chart is at rest: the enter animation until
    /// it ends, then an update animation while one runs. Reduced motion draws every layout at rest.
    /// </summary>
    public (IChartPlot Plot, MotionFrame Motion)? Frame(IChartPlot plot, bool reduced)
    {
        if (reduced)
        {
            (_played, _from, _morph, _drawn) = (true, null, null, plot);
            return null;
        }

        if (!_played && Entering(plot) is { } entering)
        {
            _drawn = plot;
            if (_enter is not { } enter || !ReferenceEquals(enter.Plot, plot))
            {
                enter = (plot, plot is PolarLayout polar ? PolarMorphs.Enter(polar) : null);
                _enter = enter;
            }

            return enter.Morph is { } grow ? (grow.Frame(entering), new MotionFrame(entering, enters: false)) : (plot, new MotionFrame(entering));
        }

        _enter = null;

        if (_from is { } from)
        {
            _morph = Morph.Plan(from, plot);
            _from = null;
        }

        _drawn = plot;
        if (_morph is { } morph)
        {
            var elapsed = Time.GetElapsedTime(_morphStart).TotalMilliseconds;
            if (elapsed < morph.End)
            {
                Events?.At(elapsed);
                return (morph.Frame(elapsed), new MotionFrame(elapsed, morph.Enters));
            }

            _morph = null;
            Events?.Flush();
        }

        return null;
    }

    private double? Entering(IChartPlot plot)
    {
        var end = Animations.End(plot.Spec);
        if (_start is null)
        {
            _start = Time.GetTimestamp();
            Events?.Start();
        }

        var elapsed = Time.GetElapsedTime(_start.Value).TotalMilliseconds;
        if (elapsed >= end)
        {
            _played = true;
            Events?.Flush();
            return null;
        }

        Events?.At(elapsed);
        return elapsed;
    }
}
