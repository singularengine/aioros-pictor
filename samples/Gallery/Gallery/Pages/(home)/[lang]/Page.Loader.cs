using Aioros.Pages;
using Gallery.Landing;

namespace Gallery.Pages;

/// <summary>Pictor's landing page in the language its address names, the others named as its alternates; not found for any other.</summary>
public static class LandingLoader
{
    public static LoaderResult<LandingData> Load(string lang) =>
        Languages.FromSlug(lang) is { } language
            ? LoaderResult.With(
                new LandingData(language.Slug),
                "Aioros.Pictor",
                new PageMetadata(LandingCopy.For(language.Slug).Lead, Languages.Path(language), Alternates: [.. Languages.All.Select(static other => new PageAlternate(other.Culture, Languages.Path(other))), new("x-default", "/en")]))
            : LoaderResult.NotFound();
}
