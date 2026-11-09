using System.Buffers;
using System.Text;
using Aioros.Graphs.Formatting;

namespace Aioros.Pictor.Svg;

/// <summary>Markup written straight to a UTF-8 buffer: literal bytes, numbers as d3-path rounds them, escaped text.</summary>
internal static class Utf8Markup
{
    private const int NumberLength = 32;

    public static void Raw(MarkupBuffer output, ReadOnlySpan<byte> bytes) => output.Write(bytes);

    /// <summary>A number rounded to three decimals and written as JavaScript writes it (<c>73.333</c>, <c>80</c>).</summary>
    public static void Number(MarkupBuffer output, double value)
    {
        var span = output.GetSpan(NumberLength);
        output.Advance(Rounded(value, span));
    }

    /// <summary>
    /// <see cref="JsNumber.WriteRounded"/> to three decimals with the digits written in place: the thousandths as one
    /// integer, its whole part and up to three fraction digits without trailing zeros; what that integer cannot hold
    /// (not finite, 1e15 and beyond) goes through <see cref="JsNumber"/>.
    /// </summary>
    internal static int Rounded(double value, Span<byte> destination)
    {
        var scaled = value * 1000;
        var floor = Math.Floor(scaled);
        var rounded = scaled - floor >= 0.5 ? floor + 1 : floor;
        if (!(Math.Abs(rounded) < 1e15))
        {
            return JsNumber.WriteRounded(value, 3, destination);
        }

        if (rounded == 0)
        {
            destination[0] = (byte)'0';
            return 1;
        }

        var written = 0;
        if (rounded < 0)
        {
            destination[written++] = (byte)'-';
            rounded = -rounded;
        }

        var thousandths = (ulong)rounded;
        var whole = thousandths / 1000;
        var fraction = (uint)(thousandths - (whole * 1000));
        written += Whole(whole, destination[written..]);
        if (fraction == 0)
        {
            return written;
        }

        destination[written++] = (byte)'.';
        destination[written++] = (byte)('0' + (fraction / 100));
        var rest = fraction % 100;
        if (rest != 0)
        {
            destination[written++] = (byte)('0' + (rest / 10));
            if (rest % 10 != 0)
            {
                destination[written++] = (byte)('0' + (rest % 10));
            }
        }

        return written;
    }

    private static int Whole(ulong value, Span<byte> destination)
    {
        var length = 1;
        for (var rest = value; rest >= 10; rest /= 10)
        {
            length++;
        }

        for (var i = length - 1; i > 0; i--)
        {
            var next = value / 10;
            destination[i] = (byte)('0' + (value - (next * 10)));
            value = next;
        }

        destination[0] = (byte)('0' + value);
        return length;
    }

    /// <summary>A text written as it is, for a style sheet (raw text in HTML), which must not hold <c>&lt;</c>.</summary>
    public static void Css(MarkupBuffer output, ReadOnlySpan<char> text)
    {
        var span = output.GetSpan(Encoding.UTF8.GetMaxByteCount(text.Length));
        output.Advance(Encoding.UTF8.GetBytes(text, span));
    }

    /// <summary>A text escaped for an attribute value or element content.</summary>
    public static void Text(MarkupBuffer output, ReadOnlySpan<char> text)
    {
        while (!text.IsEmpty)
        {
            var next = text.IndexOfAny("&<>\"");
            var plain = next < 0 ? text : text[..next];
            if (!plain.IsEmpty)
            {
                var span = output.GetSpan(Encoding.UTF8.GetMaxByteCount(plain.Length));
                output.Advance(Encoding.UTF8.GetBytes(plain, span));
            }

            if (next < 0)
            {
                return;
            }

            output.Write(text[next] switch
            {
                '&' => "&amp;"u8,
                '<' => "&lt;"u8,
                '>' => "&gt;"u8,
                _ => "&quot;"u8,
            });
            text = text[(next + 1)..];
        }
    }

    public static void Attribute(MarkupBuffer output, ReadOnlySpan<byte> name, double value)
    {
        output.Write(name);
        output.Write("=\""u8);
        Number(output, value);
        output.Write("\""u8);
    }

    public static void Attribute(MarkupBuffer output, ReadOnlySpan<byte> name, string value)
    {
        output.Write(name);
        output.Write("=\""u8);
        Text(output, value);
        output.Write("\""u8);
    }
}
