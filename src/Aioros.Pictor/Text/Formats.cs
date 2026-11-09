using System.Globalization;

namespace Aioros.Pictor.Text;

/// <summary>
/// An app's .NET composite formats (<c>TickFormat="{0:N0}"</c>, Recharts' formatters), which a bound text can carry to the
/// request, written in the request's culture as a binding's <c>StringFormat</c> is: a format that does not format one value (<c>{1}</c>, an unclosed <c>{</c>, a lone <c>}</c>, a specifier
/// the value's type refuses) writes the value as it is without a format, as Recharts writes a value without a formatter,
/// instead of failing the request.
/// </summary>
internal static class Formats
{
    /// <summary>The value through the format, or <paramref name="plain"/> when there is no format or it formats no single value.</summary>
    public static string Apply<T>(string? format, T value, string plain)
    {
        if (format is null || !IsSingle(format))
        {
            return plain;
        }

        try
        {
            return value is string text && Date(text) is { } date
                ? string.Format(CultureInfo.CurrentCulture, format, date)
                : string.Format(CultureInfo.CurrentCulture, format, value);
        }
        catch (FormatException)
        {
            return plain;
        }
    }

    private static readonly string[] DateTexts = ["yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssK", "yyyy-MM-ddTHH:mm:ss.FFFFFFFK", "MM/dd/yyyy HH:mm:ss"];

    /// <summary>
    /// The date a category's text reads as, an ISO date or a <c>DateTime</c> written as data writes it, so a format
    /// formats the date as shadcn/ui's formatters format <c>new Date(value)</c>; nothing for any other text.
    /// </summary>
    private static DateTime? Date(string text) =>
        text.Length >= 10 && DateTime.TryParseExact(text, DateTexts, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date) ? date : null;

    /// <summary>Whether a composite format's items all format its one argument (<c>{0}</c>, with an alignment and a format string), its other braces doubled.</summary>
    public static bool IsSingle(ReadOnlySpan<char> format)
    {
        var rest = format;
        while (!rest.IsEmpty)
        {
            var brace = rest.IndexOfAny('{', '}');
            if (brace < 0)
            {
                return true;
            }

            var length = Brace(rest[brace..]);
            if (length < 0)
            {
                return false;
            }

            rest = rest[(brace + length)..];
        }

        return true;
    }

    /// <summary>How long the brace at the start is: a doubled brace, or a format item up to its closing brace; -1 when it is neither.</summary>
    private static int Brace(ReadOnlySpan<char> text)
    {
        if (text.Length > 1 && text[1] == text[0])
        {
            return 2;
        }

        if (text[0] == '}')
        {
            return -1;
        }

        var end = text[1..].IndexOfAny('{', '}') + 1;
        return end > 0 && text[end] == '}' && IsItem(text[1..end]) ? end + 1 : -1;
    }

    private static bool IsItem(ReadOnlySpan<char> item)
    {
        var colon = item.IndexOf(':');
        var head = colon < 0 ? item : item[..colon];
        var comma = head.IndexOf(',');
        var index = (comma < 0 ? head : head[..comma]).Trim();
        return index.SequenceEqual("0") && (comma < 0 || int.TryParse(head[(comma + 1)..].Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _));
    }
}
