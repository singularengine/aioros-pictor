using System.Runtime.CompilerServices;
using Aioros.Pages;

namespace Gallery.Landing;

/// <summary>The landing page in each language, so the warm-up and the precompilation plan all of them.</summary>
public sealed class LandingUrls : IPrecompileSource
{
    public async IAsyncEnumerable<string> UrlsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var language in Languages.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return Languages.Path(language);
        }

        await Task.CompletedTask;
    }
}
