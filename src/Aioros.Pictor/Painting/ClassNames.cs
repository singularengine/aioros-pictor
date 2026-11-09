using System.Buffers;
using System.Text;

namespace Aioros.Pictor.Painting;

/// <summary>Recharts' <c>className</c>: an element's classes after the ones Recharts writes (<c>clsx('recharts-line', className)</c>).</summary>
internal static class ClassNames
{
    private const string Reserved = "pictor-";

    private static readonly SearchValues<char> Unsafe = SearchValues.Create("\"'<>&`=\\");

    /// <summary>The fixed classes with the element's own after them; the fixed ones alone when it has none.</summary>
    public static ReadOnlySpan<byte> With(ReadOnlySpan<byte> classes, string? className)
    {
        if (Tokens(className) is not { } own)
        {
            return classes;
        }

        if (classes.IsEmpty)
        {
            return Encoding.UTF8.GetBytes(own);
        }

        var joined = new byte[classes.Length + 1 + Encoding.UTF8.GetByteCount(own)];
        classes.CopyTo(joined);
        joined[classes.Length] = (byte)' ';
        Encoding.UTF8.GetBytes(own, joined.AsSpan(classes.Length + 1));
        return joined;
    }

    /// <summary>
    /// An app's classes as class tokens: the text itself when every token is one, otherwise the tokens that are, joined by
    /// spaces. A token holding a quote, an angle bracket, an ampersand, a backtick, an equals sign, a backslash or a
    /// control character is left out, and so is one starting with <c>pictor-</c>, the classes Pictor's own rules match.
    /// </summary>
    public static string? Tokens(string? className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return null;
        }

        var refused = false;
        foreach (var range in className.AsSpan().SplitAny(" \t\n\r\f"))
        {
            refused |= !className.AsSpan()[range].IsEmpty && !IsToken(className.AsSpan()[range]);
        }

        if (!refused)
        {
            return className;
        }

        var kept = string.Join(' ', className.Split((char[])[' ', '\t', '\n', '\r', '\f'], StringSplitOptions.RemoveEmptyEntries).Where(static token => IsToken(token)));
        return kept.Length == 0 ? null : kept;
    }

    private static bool IsToken(ReadOnlySpan<char> token) =>
        !token.StartsWith(Reserved, StringComparison.Ordinal) && !token.ContainsAny(Unsafe) && !token.ContainsAnyInRange('\0', '\u001f');
}
