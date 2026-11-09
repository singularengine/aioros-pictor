using Aioros.Pages;
using Gallery.Compare;

namespace Gallery.Pages.Compare;

/// <summary>The family whose reference examples the page compares with Recharts' renders; an unknown family is not found.</summary>
public static class FamilyComparisonLoader
{
    public static Task<LoaderResult<Family>> LoadAsync(string family) =>
        Task.FromResult(ReferenceExamples.Families.FirstOrDefault(known => known.Route == family) is { } found
            ? LoaderResult.With(found, found.Title + " compared with Recharts")
            : (LoaderResult<Family>)LoaderResult.NotFound());
}
