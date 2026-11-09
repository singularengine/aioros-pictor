using System.Globalization;
using Aioros.Graphs.Formatting;

namespace Aioros.Pictor.Text;

/// <summary>
/// The keys under which a spec records what its formatter functions wrote (an axis' <c>TickTexts</c>, a tooltip's
/// <c>Texts</c> and <c>LabelTexts</c>), so the web writer, which reads the spec back without the functions, writes the
/// same texts: the function's input as text (a number as JavaScript writes it, a date in ISO 8601), with a tick's index
/// or a line's series name.
/// </summary>
public static class FormatKeys
{
    /// <summary>A tick's key: its index and its value.</summary>
    /// <param name="value">The tick's value.</param>
    /// <param name="index">The tick's index.</param>
    /// <returns>The key.</returns>
    public static string Tick(object? value, int index) => index.ToString(CultureInfo.InvariantCulture) + "|" + Text(value);

    /// <summary>A tooltip line's key: its series' name and its value.</summary>
    /// <param name="value">The line's value.</param>
    /// <param name="name">The series' name.</param>
    /// <returns>The key.</returns>
    public static string Value(object? value, string name) => name + "|" + Text(value);

    /// <summary>A tooltip label's key: the category.</summary>
    /// <param name="label">The category.</param>
    /// <returns>The key.</returns>
    public static string Label(object? label) => Text(label);

    /// <summary>A tick's function: its own (the desktop's), its converter rebuilt by the web's writer, the texts recorded from it, or null.</summary>
    internal static Func<object?, int, string?>? TickFunction(Func<object?, int, string?>? function, System.Collections.Immutable.ImmutableDictionary<string, string>? texts, string? converter = null) =>
        function
        ?? (Converters.Of(converter) is { } convert ? (Func<object?, int, string?>)((value, index) => convert(value, index) ?? texts?.GetValueOrDefault(Tick(value, index))) : null)
        ?? (texts is null ? null : (value, index) => texts.GetValueOrDefault(Tick(value, index)));

    /// <summary>A tooltip line's function: its own, its converter rebuilt by the web's writer, the texts recorded from it, or null.</summary>
    internal static Func<object?, string, string?>? ValueFunction(Func<object?, string, string?>? function, System.Collections.Immutable.ImmutableDictionary<string, string>? texts, string? converter = null) =>
        function
        ?? (Converters.Of(converter) is { } convert ? (Func<object?, string, string?>)((value, name) => convert(value, name) ?? texts?.GetValueOrDefault(Value(value, name))) : null)
        ?? (texts is null ? null : (value, name) => texts.GetValueOrDefault(Value(value, name)));

    /// <summary>A tooltip label's function: its own, its converter rebuilt by the web's writer, the texts recorded from it, or null.</summary>
    internal static Func<object?, string?>? LabelFunction(Func<object?, string?>? function, System.Collections.Immutable.ImmutableDictionary<string, string>? texts, string? converter = null) =>
        function
        ?? (Converters.Of(converter) is { } convert ? (Func<object?, string?>)(label => convert(label, null) ?? texts?.GetValueOrDefault(Label(label))) : null)
        ?? (texts is null ? null : label => texts.GetValueOrDefault(Label(label)));

    private static string Text(object? value) => value switch
    {
        null => string.Empty,
        double number => JsNumber.ToShortestString(number),
        DateTimeOffset instant => instant.ToString("O", CultureInfo.InvariantCulture),
        DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };
}
