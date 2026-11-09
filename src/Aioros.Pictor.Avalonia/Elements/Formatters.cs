using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Aioros.Pictor;

/// <summary>
/// Recharts' formatter functions as Avalonia's value converters (<c>tickFormatter</c>, <c>formatter</c>,
/// <c>labelFormatter</c>): a converter set in XAML, or a function set from code through Avalonia's
/// <see cref="FuncValueConverter{TIn, TOut}"/>, called with the value, Recharts' second argument as the parameter and
/// the current culture, as a binding calls it; a result that is no text keeps the format's.
/// </summary>
internal static class Formatters
{
    /// <summary>A tick's function: the tick's value, its index as the parameter.</summary>
    public static Func<object?, int, string?>? Tick(IValueConverter? converter) =>
        converter is null ? null : (value, index) => Text(converter.Convert(value, typeof(string), index, CultureInfo.CurrentCulture));

    /// <summary>A tooltip line's function: the line's value, its series' name as the parameter.</summary>
    public static Func<object?, string, string?>? Value(IValueConverter? converter) =>
        converter is null ? null : (value, name) => Text(converter.Convert(value, typeof(string), name, CultureInfo.CurrentCulture));

    /// <summary>A tooltip label's function: the active category.</summary>
    public static Func<object?, string?>? Label(IValueConverter? converter) =>
        converter is null ? null : value => Text(converter.Convert(value, typeof(string), null, CultureInfo.CurrentCulture));

    /// <summary>
    /// How the web's writer rebuilds a converter: Aioros's plans file reference of its type (<c>type:</c>) when the app's
    /// generated converters can create it, which Aioros's generator does for a converter class with a parameterless
    /// constructor and no settings; nothing otherwise (a converter with settings, a function), and the chart then records
    /// what it writes.
    /// </summary>
    public static string? Reference(IValueConverter? converter) =>
        converter is null ? null : "type:" + converter.GetType().AssemblyQualifiedName is var reference && Aioros.Runtime.Slots.PlanConverters.Find(reference) is not null ? reference : null;

    private static string? Text(object? result) =>
        result is null || result == AvaloniaProperty.UnsetValue || result is BindingNotification ? null : Convert.ToString(result, CultureInfo.CurrentCulture);
}

/// <summary>One function made from a converter and kept while the converter stays, so a spec built again compares equal.</summary>
internal sealed class Formatted<TFunction>(Func<IValueConverter?, TFunction?> make)
    where TFunction : Delegate
{
    private (IValueConverter? Converter, TFunction? Function) _made;

    public TFunction? Of(IValueConverter? converter)
    {
        if (!ReferenceEquals(_made.Converter, converter))
        {
            _made = (converter, make(converter));
        }

        return _made.Function;
    }
}
