using System.Collections;
using Avalonia;
using Avalonia.Data;

namespace Aioros.Pictor.Tests.Desktop;

/// <summary>Walks a control's bindings as Aioros's plan does: its own, and those of the Avalonia objects its styled properties hold, three levels deep.</summary>
internal static class BindingWalk
{
    public static IEnumerable<string> Bound(AvaloniaObject target, int depth)
    {
        foreach (var property in AvaloniaPropertyRegistry.Instance.GetRegistered(target))
        {
            if (BindingOperations.GetBindingExpressionBase(target, property) is not null)
            {
                yield return $"{target.GetType().Name}.{property.Name}";
            }

            if (depth >= 3 || property.IsDirect)
            {
                continue;
            }

            foreach (var held in Held(target.GetValue(property)).SelectMany(held => Bound(held, depth + 1)))
            {
                yield return held;
            }
        }
    }

    private static IEnumerable<AvaloniaObject> Held(object? value) => value switch
    {
        Visual => [],
        AvaloniaObject held => [held],
        IEnumerable items when value.GetType().Namespace?.StartsWith("Avalonia", StringComparison.Ordinal) == true => items.OfType<AvaloniaObject>().Where(item => item is not Visual),
        _ => [],
    };
}
