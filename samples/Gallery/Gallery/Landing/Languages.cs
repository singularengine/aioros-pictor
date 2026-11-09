using System.Collections.Immutable;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace Gallery.Landing;

/// <summary>A language of the landing page: its address segment and its culture.</summary>
public sealed record Language(string Slug, string Culture, string Name);

/// <summary>The landing page's languages, English first, and the one a visitor's <c>Accept-Language</c> prefers.</summary>
public static class Languages
{
    public static ImmutableArray<Language> All { get; } =
    [
        new("en", "en", "English"),
        new("es", "es", "Español"),
        new("zh", "zh-Hans", "简体中文"),
        new("ja", "ja", "日本語"),
    ];

    public static Language? FromSlug(string? slug) => All.FirstOrDefault(language => string.Equals(language.Slug, slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>The language the header prefers most among the landing page's, by its primary subtag (<c>es-MX</c> is Spanish), English when none is.</summary>
    public static Language Preferred(string? acceptLanguage) =>
        StringWithQualityHeaderValue.TryParseList(acceptLanguage?.Split(','), out var ranges)
            ? ranges.OrderByDescending(static range => range.Quality ?? 1)
                .Select(static range => range.Value.Value?.Split('-')[0])
                .Select(static primary => All.FirstOrDefault(language => string.Equals(language.Culture.Split('-')[0], primary, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault(static language => language is not null) ?? All[0]
            : All[0];

    /// <summary>The landing page's address in a language.</summary>
    public static string Path(Language language) => "/" + language.Slug;

    /// <summary>Sends <c>/</c> to the landing page in the visitor's language.</summary>
    public static void MapRoot(IEndpointRouteBuilder app) =>
        app.MapGet("/", static (HttpContext http) => Results.Redirect(Path(Preferred(http.Request.Headers.AcceptLanguage))));

    /// <summary>Request localization over the landing page's cultures, a page's culture read from its first path segment; the gallery's pages stay in English.</summary>
    public static void Configure(RequestLocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        string[] cultures = [.. All.Select(static language => language.Culture)];
        options.SetDefaultCulture(All[0].Culture).AddSupportedCultures(cultures).AddSupportedUICultures(cultures);
        options.RequestCultureProviders.Clear();
        options.RequestCultureProviders.Add(new CustomRequestCultureProvider(static http =>
        {
            var slug = http.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return Task.FromResult(FromSlug(slug) is { } language ? new ProviderCultureResult(language.Culture) : null);
        }));
    }
}
