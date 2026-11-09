namespace Aioros.Pictor.Interaction;

/// <summary>The addresses a chart's data may link to: a path, a fragment, a query, or a web, mail or telephone address, so data can never write a script's.</summary>
internal static class Hrefs
{
    private static readonly string[] Schemes = ["http", "https", "mailto", "tel"];

    public static string? Safe(string? href)
    {
        if (string.IsNullOrEmpty(href))
        {
            return null;
        }

        var end = href.AsSpan().IndexOfAny(":/?#");
        return end < 0 || href[end] != ':' || Array.Exists(Schemes, scheme => href.AsSpan(0, end).Equals(scheme, StringComparison.OrdinalIgnoreCase)) ? href : null;
    }
}
