using Aioros.Pages;

namespace Gallery.Compare;

/// <summary>The comparison pages' addresses, one per family, so that <c>build --precompile</c> compiles the page with a route parameter.</summary>
public sealed class CompareSamples : IPrecompileSource
{
    public IAsyncEnumerable<string> UrlsAsync(CancellationToken cancellationToken) =>
        ReferenceExamples.Families.Select(family => "/compare/" + family.Route).ToAsyncEnumerable();
}
