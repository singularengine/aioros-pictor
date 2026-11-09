using System.Globalization;

namespace Aioros.Pictor.Painting;

/// <summary>
/// Reads the CSS declarations a XAML style prop holds (Recharts' style objects as CSS text, <c>padding:6px;border:2px
/// solid #8884d8</c>): the web writes them as they are, the desktop draws the few it understands. No allocation.
/// </summary>
internal static class Css
{
    /// <summary>The value of the last declaration of a property, or an empty span.</summary>
    public static ReadOnlySpan<char> Value(string? style, ReadOnlySpan<char> property)
    {
        var found = ReadOnlySpan<char>.Empty;
        if (style is null)
        {
            return found;
        }

        var rest = style.AsSpan();
        while (!rest.IsEmpty)
        {
            var end = rest.IndexOf(';');
            var declaration = end < 0 ? rest : rest[..end];
            rest = end < 0 ? [] : rest[(end + 1)..];
            var colon = declaration.IndexOf(':');
            if (colon > 0 && declaration[..colon].Trim().Equals(property, StringComparison.OrdinalIgnoreCase))
            {
                found = declaration[(colon + 1)..].Trim();
            }
        }

        return found;
    }

    /// <summary>A length in pixels (<c>6px</c> or <c>6</c>), or null.</summary>
    public static double? Length(ReadOnlySpan<char> value)
    {
        if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^2];
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var length) ? length : null;
    }

    /// <summary>A property's length in pixels, or a default.</summary>
    public static double Length(string? style, ReadOnlySpan<char> property, double fallback) => Length(Value(style, property)) ?? fallback;

    /// <summary>The width of a <c>border</c> shorthand (its first length) or of <c>border-width</c>.</summary>
    public static double BorderWidth(string? style, double fallback)
    {
        var width = Value(style, "border-width");
        if (Length(width) is { } explicitWidth)
        {
            return explicitWidth;
        }

        var border = Value(style, "border");
        if (border.IsEmpty)
        {
            return fallback;
        }

        foreach (var range in border.Split(' '))
        {
            if (Length(border[range]) is { } length)
            {
                return length;
            }
        }

        return border.StartsWith("none", StringComparison.OrdinalIgnoreCase) ? 0 : fallback;
    }

    /// <summary>The color of <c>border-color</c> or of a <c>border</c> shorthand (its last word), or a default.</summary>
    public static string BorderColor(string? style, string fallback)
    {
        var color = Value(style, "border-color");
        if (!color.IsEmpty)
        {
            return color.ToString();
        }

        var border = Value(style, "border");
        if (border.IsEmpty)
        {
            return fallback;
        }

        var last = border.LastIndexOf(' ');
        var word = last < 0 ? border : border[(last + 1)..];
        return Length(word) is null && !word.Equals("solid", StringComparison.OrdinalIgnoreCase) ? word.ToString() : fallback;
    }

    /// <summary>A color property (<c>color</c>, <c>background-color</c>, or the <c>background</c> shorthand), or a default.</summary>
    public static string Color(string? style, ReadOnlySpan<char> property, string fallback)
    {
        var value = Value(style, property);
        if (value.IsEmpty && property.SequenceEqual("background-color"))
        {
            value = Value(style, "background");
        }

        return value.IsEmpty ? fallback : value.ToString();
    }
}
