namespace Aioros.Pictor.Specs;

/// <summary>Recharts' <c>animationEasing</c>: CSS's named timing functions.</summary>
public enum AnimationEasing
{
    /// <summary><c>ease</c>.</summary>
    Ease,

    /// <summary><c>ease-in</c>.</summary>
    EaseIn,

    /// <summary><c>ease-out</c>.</summary>
    EaseOut,

    /// <summary><c>ease-in-out</c>.</summary>
    EaseInOut,

    /// <summary><c>linear</c>.</summary>
    Linear,
}

/// <summary>
/// A series' enter animation, Recharts' <c>isAnimationActive</c>, <c>animationBegin</c>, <c>animationDuration</c> and
/// <c>animationEasing</c>; a null value takes the series kind's default (a line's 1500 ms ease, a bar's 400 ms...).
/// </summary>
public sealed record AnimationSpec
{
    /// <summary>No animation: the series is drawn in its final state at once.</summary>
    public static AnimationSpec Off { get; } = new() { Active = false };

    /// <summary>Whether the series animates in.</summary>
    public bool Active { get; init; } = true;

    /// <summary>The delay before the animation starts, in milliseconds.</summary>
    public double? Begin { get; init; }

    /// <summary>The animation's length, in milliseconds.</summary>
    public double? Duration { get; init; }

    /// <summary>The animation's timing function.</summary>
    public AnimationEasing? Easing { get; init; }
}
