namespace Aioros.Pictor.Painting;

/// <summary>
/// The colors a chart writes where markup or a style declaration holds them: a hex color, <c>rgb()</c>, <c>rgba()</c>,
/// <c>hsl()</c> or <c>hsla()</c> of numbers, a named color (a word of letters, which covers <c>none</c>,
/// <c>transparent</c> and <c>currentColor</c>), a reference to a paint of the page (<c>url(#id)</c>) or a custom
/// property (<c>var(--name)</c>). Anything else (a declaration list, a remote <c>url()</c>, an expression) is refused.
/// </summary>
internal static class CssValues
{
    private const int LongestName = 32;

    /// <summary>The color when it is one, otherwise null (as if the item had none).</summary>
    public static string? Valid(string? color) => color is not null && IsColor(color) ? color : null;

    /// <summary>The colors of a column with every one that is not a color dropped; the column itself when all of them are.</summary>
    public static string?[] Valid(string?[] colors)
    {
        var index = Array.FindIndex(colors, static color => color is not null && !IsColor(color));
        if (index < 0)
        {
            return colors;
        }

        var valid = (string?[])colors.Clone();
        for (; index < valid.Length; index++)
        {
            valid[index] = Valid(valid[index]);
        }

        return valid;
    }

    public static bool IsColor(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return false;
        }

        if (text[0] == '#')
        {
            return text.Length is 4 or 5 or 7 or 9 && !text[1..].ContainsAnyExcept(Hex);
        }

        var open = text.IndexOf('(');
        if (open < 0)
        {
            return text.Length <= LongestName && !text.ContainsAnyExcept(Letters);
        }

        if (text[^1] != ')')
        {
            return false;
        }

        var name = text[..open];
        var arguments = text[(open + 1)..^1];
        return name switch
        {
            "rgb" or "rgba" or "hsl" or "hsla" => Numbers(arguments),
            "url" => arguments is ['#', _, ..] && !arguments[1..].ContainsAnyExcept(Identifier),
            "var" => arguments is ['-', '-', _, ..] && !arguments[2..].ContainsAnyExcept(Identifier),
            _ => false,
        };
    }

    /// <summary>Whether a font family list holds names only (<c>Inter, "Segoe UI", sans-serif</c>): no declaration, block or function can follow it.</summary>
    public static bool IsFontFamily(ReadOnlySpan<char> text) => !text.IsEmpty && !text.ContainsAny(FamilyBreakers) && !text.ContainsAnyInRange('\0', '\u001f');

    /// <summary>Whether a dash pattern is a list of lengths (<c>5 5</c>, <c>3,1</c>, <c>4px 2%</c>), as a style declaration may hold it.</summary>
    public static bool IsDash(ReadOnlySpan<char> text) => !text.IsEmpty && !text.ContainsAnyExcept(DashCharacters);

    private static bool Numbers(ReadOnlySpan<char> arguments)
    {
        foreach (var range in arguments.SplitAny(", /"))
        {
            var token = arguments[range];
            if (token.IsEmpty)
            {
                continue;
            }

            var unit = token.IndexOfAnyExcept(NumberCharacters);
            var number = unit < 0 ? token : token[..unit];
            if (number.IsEmpty || !double.TryParse(number, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                return token.SequenceEqual("none");
            }

            if (unit >= 0 && token[unit..] is not ("%" or "deg" or "rad" or "grad" or "turn"))
            {
                return false;
            }
        }

        return true;
    }

    private static readonly System.Buffers.SearchValues<char> Hex = System.Buffers.SearchValues.Create("0123456789abcdefABCDEF");

    private static readonly System.Buffers.SearchValues<char> Letters = System.Buffers.SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");

    private static readonly System.Buffers.SearchValues<char> Identifier = System.Buffers.SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_");

    private static readonly System.Buffers.SearchValues<char> NumberCharacters = System.Buffers.SearchValues.Create("0123456789.+-eE");

    private static readonly System.Buffers.SearchValues<char> FamilyBreakers = System.Buffers.SearchValues.Create(";:{}()<>\\/!@");

    private static readonly System.Buffers.SearchValues<char> DashCharacters = System.Buffers.SearchValues.Create("0123456789., %px");

}
