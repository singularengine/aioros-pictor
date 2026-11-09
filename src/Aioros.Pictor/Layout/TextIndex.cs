namespace Aioros.Pictor.Layout;

/// <summary>Where each text first stands in a column (as <c>Array.IndexOf</c> finds it), built once instead of searched per item.</summary>
internal static class TextIndex
{
    /// <summary>Each text's first index; a missing text has none.</summary>
    public static Dictionary<string, int> Of(IReadOnlyList<string?> texts)
    {
        var first = new Dictionary<string, int>(texts.Count, StringComparer.Ordinal);
        for (var i = 0; i < texts.Count; i++)
        {
            if (texts[i] is { } text)
            {
                first.TryAdd(text, i);
            }
        }

        return first;
    }

    /// <summary>A text's first index in the index, or -1.</summary>
    public static int Find(Dictionary<string, int> index, string? text) => text is not null && index.TryGetValue(text, out var at) ? at : -1;
}
