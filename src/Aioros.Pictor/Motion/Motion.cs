using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Motion;

/// <summary>The timing of one series' enter animation: when it starts and ends, and how it eases, in milliseconds.</summary>
internal readonly record struct Timing(double Begin, double Duration, AnimationEasing Easing)
{
    /// <summary>
    /// How long after its begin Recharts' animation actually starts: react-smooth's manager reaches its first frame
    /// through five animation frames (two for <c>onAnimationStart</c>, two for the begin delay, one to start the frame
    /// loop), 80 ms at the 16 ms frames Recharts' references are recorded with.
    /// </summary>
    public const double SmoothLag = 80;

    /// <summary>
    /// How long after its begin Recharts 3's <c>JavascriptAnimate</c> actually starts, two animation frames earlier than
    /// react-smooth (recorded, <c>tools/recharts/motion.mjs --recharts3</c>): 48 ms at 16 ms frames.
    /// </summary>
    public const double JavascriptLag = 48;

    /// <summary>How long after its begin the animation actually starts: <see cref="SmoothLag"/>, or <see cref="JavascriptLag"/> for a series Pictor animates as Recharts 3 does.</summary>
    public double Lag { get; init; } = SmoothLag;

    /// <summary>
    /// How long after its last frame Recharts draws what waits for the end (dots, labels, error bars): react-smooth calls
    /// <c>onAnimationEnd</c> two animation frames later, 32 ms at 16 ms frames.
    /// </summary>
    public const double EndLag = 32;

    /// <summary>How long after a <c>Rectangle</c> mounts react-smooth sets its stroke's final dash with its CSS transition: four animation frames (recorded, <c>tools/recharts/strokes.mjs</c>).</summary>
    public const double StrokeLag = 64;

    /// <summary>The <c>Rectangle</c>'s own animation duration (its <c>defaultProps</c>).</summary>
    public const double StrokeDuration = 1500;

    /// <summary>When a stroke drawn in starts: <see cref="StrokeLag"/> after the shape mounts, at once or once the series has ended.</summary>
    public double StrokeStart(bool late) => (late ? End : 0) + StrokeLag;

    /// <summary>The eased progress of a stroke drawn in at a time since the chart appeared.</summary>
    public double StrokeProgress(double elapsed, bool late) => Easings.Ease(AnimationEasing.Ease, Math.Clamp((elapsed - StrokeStart(late)) / StrokeDuration, 0, 1));

    /// <summary>When the movement ends.</summary>
    public double Moved => Begin + Lag + Duration;

    /// <summary>When what waits for the end is drawn, which ends the animation.</summary>
    public double End => Moved + EndLag;

    /// <summary>The eased progress, 0 to 1, at a time since the chart appeared (or changed).</summary>
    public double Progress(double elapsed) =>
        Duration <= 0 ? (elapsed >= Begin + Lag ? 1 : 0) : Easings.Ease(Easing, Math.Clamp((elapsed - Begin - Lag) / Duration, 0, 1));

    /// <summary>The timing a spec gives, over the kind's defaults (Recharts' <c>defaultProps</c>); null when the series does not animate.</summary>
    public static Timing? Of(AnimationSpec? spec, Timing defaults) =>
        spec is { Active: false }
            ? null
            : new Timing(spec?.Begin ?? defaults.Begin, spec?.Duration ?? defaults.Duration, spec?.Easing ?? defaults.Easing);

    /// <summary><c>Line</c>, <c>Area</c>, <c>Radar</c> and <c>RadialBar</c>: 1500 ms, ease.</summary>
    public static Timing Slow { get; } = new(0, 1500, AnimationEasing.Ease);

    /// <summary><c>Bar</c>: 400 ms, ease.</summary>
    public static Timing Bar { get; } = new(0, 400, AnimationEasing.Ease);

    /// <summary><c>Scatter</c>: 400 ms, linear.</summary>
    public static Timing Scatter { get; } = new(0, 400, AnimationEasing.Linear);

    /// <summary><c>Pie</c> and <c>Funnel</c>: after 400 ms, 1500 ms, ease.</summary>
    public static Timing Delayed { get; } = new(400, 1500, AnimationEasing.Ease);

    /// <summary><c>Treemap</c>: 1500 ms, linear.</summary>
    public static Timing Treemap { get; } = new(0, 1500, AnimationEasing.Linear);
}

/// <summary>What one shape or layer does while its series animates in, as Recharts' animations move it.</summary>
internal enum StepKind
{
    /// <summary>No movement.</summary>
    None,

    /// <summary>A line drawn along its length (Recharts' growing stroke dash).</summary>
    Draw,

    /// <summary>Revealed from its left edge to its right (an area's growing clip).</summary>
    Reveal,

    /// <summary>
    /// A dashed line drawn along its length with its own dashes, as Recharts' <c>getStrokeDasharray</c> repeats the
    /// pattern up to the drawn length: on the web through a mask of its path drawn in, on the desktop as a segment of it.
    /// </summary>
    DrawDashed,

    /// <summary>Revealed from its top edge down (a vertical chart's).</summary>
    RevealDown,

    /// <summary>Grown from its bottom edge (a bar above its base).</summary>
    GrowUp,

    /// <summary>Grown from its top edge (a bar below its base).</summary>
    GrowDown,

    /// <summary>Grown from its left edge (a vertical chart's bar right of its base).</summary>
    GrowRight,

    /// <summary>Grown from its right edge (a vertical chart's bar left of its base).</summary>
    GrowLeft,

    /// <summary>Scaled from its center (a funnel's trapezoid).</summary>
    Scale,

    /// <summary>Scaled from its center by the square root of the progress (a scatter symbol whose area grows linearly).</summary>
    ScaleArea,

    /// <summary>Scaled from the chart's center (a radar's polygon).</summary>
    Center,

    /// <summary>Hidden until its series has animated in (a pie's or a radial bar's sectors, drawn by growing arcs meanwhile).</summary>
    Hold,

    /// <summary>Shown only while its series animates in (the arcs that grow a pie's or a radial bar's sectors).</summary>
    Shown,

    /// <summary>Moved in from an offset (<see cref="Step.Cx"/>, <see cref="Step.Cy"/>) to its place, linearly with the progress (a radar's dot coming from the center with its point).</summary>
    Converge,

    /// <summary>Slid in from an offset (a treemap's node).</summary>
    Slide,

    /// <summary>
    /// Its stroke drawn in by its own timing (a treemap's default rectangle when the treemap's
    /// <c>isUpdateAnimationActive</c> is off): over 1500 ms, <c>ease</c>, from 64 ms after it mounts (after the series'
    /// end for a node that waits for it, <see cref="Step.Offset"/> 1).
    /// </summary>
    Stroke,
}

/// <summary>
/// One shape's or layer's movement: its kind, the center it moves around (<see cref="StepKind.Center"/>) or the offset
/// it comes from (<see cref="StepKind.Converge"/>), or the slide's offset.
/// </summary>
internal readonly record struct Step(StepKind Kind, double Cx = 0, double Cy = 0, double Offset = 0);

/// <summary>
/// An arc that grows a sector while its series animates in, as Recharts grows the sector's angles: a circle at the
/// sector's middle radius, as wide as the sector, starting at <paramref name="Angle"/> and running counterclockwise (or
/// clockwise) over a path length of 360, whose dash runs from <paramref name="From"/> to <paramref name="To"/> along it
/// while its length grows from 0 to <paramref name="Length"/>, all in degrees, by the series' eased progress; the
/// sector's <paramref name="Stroke"/> (a pie's white edges) is drawn as its radial edges turning with the arc's ends;
/// <paramref name="Part"/> says whether this is the arc or its edges.
/// </summary>
internal readonly record struct GrowingArc(double Cx, double Cy, double Radius, double Width, double Angle, bool Counterclockwise, double From, double To, double Length, string Color, string? Stroke = null, ArcPart Part = ArcPart.Arc);

/// <summary>Which part of a growing arc is written: the arc itself, its two edges, or only its trailing edge (its leading one is the previous arc's trailing edge).</summary>
internal enum ArcPart
{
    Arc,
    Edges,
    TrailingEdge,
}
