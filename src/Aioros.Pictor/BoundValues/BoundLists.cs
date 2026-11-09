using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using Aioros.Graphs.Formatting;

namespace Aioros.Pictor.BoundValues;

/// <summary>
/// The list forms Recharts' props take, read the same way on the desktop and on the web: a number list (a bar's
/// <c>radius</c>) from a text of numbers separated by commas or spaces, a number or a list of numbers; a dash pattern
/// (<c>strokeDasharray</c>) from a text as it is or a list of numbers.
/// </summary>
internal static class BoundLists
{
    /// <summary>The numbers of a text separated by commas or spaces, none for no text.</summary>
    public static ImmutableArray<double> Numbers(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : [.. text.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries).Select(part => double.Parse(part, NumberStyles.Float, CultureInfo.InvariantCulture))];

    /// <summary>A number list from a text, a number or a list of numbers; null for any other value.</summary>
    public static ImmutableArray<double>? Numbers(object? value) => value switch
    {
        null => [],
        string text => Numbers(text),
        IConvertible number and not bool => [number.ToDouble(CultureInfo.InvariantCulture)],
        IEnumerable items when Doubles(items) is { } numbers => numbers,
        _ => null,
    };

    /// <summary>A dash pattern from a text as it is or a list of numbers separated by spaces; null for no value or any other.</summary>
    public static string? Dashes(object? value) => value switch
    {
        string text => text,
        IEnumerable items when Doubles(items) is { } numbers => string.Join(' ', numbers.Select(JsNumber.ToShortestString)),
        _ => null,
    };

    private static ImmutableArray<double>? Doubles(IEnumerable items)
    {
        var numbers = ImmutableArray.CreateBuilder<double>();
        foreach (var item in items)
        {
            if (item is not IConvertible number || item is string or bool)
            {
                return null;
            }

            numbers.Add(number.ToDouble(CultureInfo.InvariantCulture));
        }

        return numbers.ToImmutable();
    }
}
