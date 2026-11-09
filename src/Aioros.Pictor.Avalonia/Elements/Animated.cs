using Aioros.Pictor.Specs;
using Avalonia;

namespace Aioros.Pictor;

/// <summary>Recharts' animation props, shared by every animated element (series, pies, radars, radial bars, funnels, treemaps).</summary>
internal static class Animated
{
    public static readonly AttachedProperty<bool> IsActiveProperty = AvaloniaProperty.RegisterAttached<Series, AvaloniaObject, bool>("IsAnimationActive", true);

    public static readonly AttachedProperty<double?> BeginProperty = AvaloniaProperty.RegisterAttached<Series, AvaloniaObject, double?>("AnimationBegin");

    public static readonly AttachedProperty<double?> DurationProperty = AvaloniaProperty.RegisterAttached<Series, AvaloniaObject, double?>("AnimationDuration");

    public static readonly AttachedProperty<AnimationEasing?> EasingProperty = AvaloniaProperty.RegisterAttached<Series, AvaloniaObject, AnimationEasing?>("AnimationEasing");

    /// <summary>Whether the property is one of the animation props.</summary>
    public static bool Owns(AvaloniaProperty property) =>
        property == IsActiveProperty || property == BeginProperty || property == DurationProperty || property == EasingProperty;

    /// <summary>The element's animation: null for the kind's defaults, <see cref="AnimationSpec.Off"/> when inactive.</summary>
    public static AnimationSpec? Spec(AvaloniaObject element)
    {
        var begin = element.GetValue(BeginProperty);
        var duration = element.GetValue(DurationProperty);
        var easing = element.GetValue(EasingProperty);
        if (!element.GetValue(IsActiveProperty))
        {
            return AnimationSpec.Off;
        }

        return begin is null && duration is null && easing is null ? null : new AnimationSpec { Begin = begin, Duration = duration, Easing = easing };
    }
}
