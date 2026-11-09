using System.Buffers;
using System.Text;

namespace Aioros.Pictor.Text;

/// <summary>
/// Recharts' <c>Text</c> wrapping (<c>getWordsByLines</c>): when a text is given a width, its words (split on runs of
/// breaking spaces as JavaScript's <c>split</c> does, or, with <c>breakAll</c>, its characters) are measured as the page
/// measures them (Inter at the page's size) and laid on lines, a word joining a line while the line, the word and a
/// space stay narrower than the width; a width of 0 wraps nothing, as Recharts skips it. The lines are joined by
/// <c>\n</c>; a text that stays one line as it was written is returned as it is, without allocating. Bar and funnel
/// labels and axis tick labels share it.
/// </summary>
public static class TextLines
{
    private static readonly SearchValues<char> Breaking = SearchValues.Create(" \f\n\r\t\v\u2028\u2029");

    private static readonly SearchValues<char> OtherBreaking = SearchValues.Create("\f\n\r\t\v\u2028\u2029");

    /// <summary>The text laid on lines no wider than the width.</summary>
    /// <param name="text">The text.</param>
    /// <param name="width">The width the lines must stay under.</param>
    /// <param name="fontSize">The size the page measures the words at.</param>
    /// <param name="breakAll">Whether lines may break between any two characters.</param>
    /// <returns>The lines, separated by <c>\n</c>.</returns>
    public static string Wrap(string text, double width, double fontSize, bool breakAll)
    {
        if (text.Length == 0 || width == 0 || double.IsNaN(width) || (!breakAll && text.AsSpan().IndexOfAny(Breaking) < 0))
        {
            return text;
        }

        var space = breakAll ? 0 : InterMetrics.Width("\u00A0", fontSize);
        if (!breakAll && Count(text, width, fontSize, space) == 1 && !text.AsSpan().ContainsAny(OtherBreaking) && !text.Contains("  ", StringComparison.Ordinal))
        {
            return text;
        }

        var lines = new StringBuilder(text.Length + 4);
        var line = double.NaN;
        var start = 0;
        var last = false;
        while (!last)
        {
            var end = Next(text, start, breakAll);
            var word = text.AsSpan(start, end - start);
            var measured = InterMetrics.Width(word, fontSize);
            var joins = !double.IsNaN(line) && line + measured + space < width;
            lines.Append(Separator(joins, breakAll, double.IsNaN(line))).Append(word);
            line = joins ? line + measured + space : measured;
            last = end >= text.Length;
            start = breakAll ? end : Skip(text, end);
        }

        return lines.ToString();
    }

    /// <summary>How many lines a wrapped text holds.</summary>
    /// <param name="text">A text as <see cref="Wrap"/> returns it.</param>
    /// <returns>Its lines.</returns>
    public static int Count(string text) => text.AsSpan().Count('\n') + 1;

    private static int Count(string text, double width, double fontSize, double space)
    {
        var count = 1;
        var line = double.NaN;
        var start = 0;
        var last = false;
        while (!last)
        {
            var end = End(text, start);
            var measured = InterMetrics.Width(text.AsSpan(start, end - start), fontSize);
            var joins = !double.IsNaN(line) && line + measured + space < width;
            count += joins || double.IsNaN(line) ? 0 : 1;
            line = joins ? line + measured + space : measured;
            last = end >= text.Length;
            start = Skip(text, end);
        }

        return count;
    }

    private static int Next(string text, int start, bool breakAll) => breakAll ? start + 1 : End(text, start);

    private static string Separator(bool joins, bool breakAll, bool first) => joins ? (breakAll ? string.Empty : " ") : first ? string.Empty : "\n";

    private static int End(string text, int start)
    {
        var next = text.AsSpan(start).IndexOfAny(Breaking);
        return next < 0 ? text.Length : start + next;
    }

    private static int Skip(string text, int end)
    {
        var next = text.AsSpan(end).IndexOfAnyExcept(Breaking);
        return next < 0 ? text.Length : end + next;
    }
}
