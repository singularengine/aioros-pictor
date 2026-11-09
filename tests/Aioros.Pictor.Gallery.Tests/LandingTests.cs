using System.Text.RegularExpressions;

namespace Aioros.Pictor.Gallery.Tests;

public sealed class LandingTests(GalleryHost gallery)
{
    private static readonly string[] Families = ["/line", "/area", "/bar", "/composed", "/scatter", "/pie", "/radar", "/radial-bar", "/funnel", "/treemap", "/sankey", "/sunburst"];

    [Fact]
    [Trait("AC", "GALLERY-07")]
    public async Task TheLandingPageAnswersInEachLanguageWithItsLiveChartsAsync()
    {
        foreach (var (slug, culture, title) in new[] { ("en", "en", "charts, with no JavaScript of yours"), ("es", "es", "Las gráficas de Recharts, sin JavaScript tuyo"), ("zh", "zh-Hans", "Recharts 的图表，无需你写 JavaScript"), ("ja", "ja", "Recharts のグラフを、JavaScript を書かずに") })
        {
            using var answer = await gallery.Client.GetAsync(slug, TestContext.Current.CancellationToken);
            var body = await answer.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(System.Net.HttpStatusCode.OK, answer.StatusCode);
            Assert.Contains($"<html lang=\"{culture}\"", body, StringComparison.Ordinal);
            Assert.Contains(title, body, StringComparison.Ordinal);
            Assert.All((string[])[">12<", ">96×<", ">2.15.4<", "docs/benchmarks.md"], figure => Assert.Contains(figure, body, StringComparison.Ordinal));
            Assert.All(Families, family => Assert.Contains($"href=\"{family}\"", body, StringComparison.Ordinal));
            Assert.Contains("href=\"/compare\"", body, StringComparison.Ordinal);
            Assert.Contains("href=\"https://github.com/singularengine/aioros\"", body, StringComparison.Ordinal);
            Assert.Contains("&lt;p:AreaChart", body, StringComparison.Ordinal);
            Assert.True(Regex.Count(body, "class=\"recharts-surface") >= 12, $"/{slug} draws fewer than twelve charts");
            Assert.DoesNotContain("Every interaction", body, StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait("AC", "GALLERY-08")]
    public async Task TheRootRedirectsToTheVisitorsLanguageAsync()
    {
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = gallery.Client.BaseAddress ?? gallery.Address };

        async Task<string?> TargetAsync(string? accept)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            if (accept is not null)
            {
                request.Headers.TryAddWithoutValidation("Accept-Language", accept);
            }

            using var answer = await client.SendAsync(request, TestContext.Current.CancellationToken);
            return answer.Headers.Location?.OriginalString;
        }

        using var missing = await gallery.Client.GetAsync("fr", TestContext.Current.CancellationToken);

        Assert.Equal(["/ja", "/es", "/en", "/en"], [await TargetAsync("ja,en;q=0.5"), await TargetAsync("es-MX"), await TargetAsync("fr"), await TargetAsync(null)]);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, missing.StatusCode);
    }
}
