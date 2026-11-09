using System.Text.RegularExpressions;

namespace Aioros.Pictor.Gallery.Tests;

public sealed partial class LiveMatrixTests(GalleryHost gallery)
{
    private static readonly string[] Titles =
        ["Line chart", "Area chart", "Bar chart", "Composed chart", "Scatter chart", "Pie chart", "Radar chart", "Radial bar chart", "Funnel chart", "Treemap", "Sankey", "Sunburst chart"];

    [Fact]
    [Trait("AC", "GALLERY-06")]
    public async Task EachFamilyIsPushedOnlyAtItsOwnTicksAndItsMorphsSettleBeforeItsNextPushAsync()
    {
        var cancel = TestContext.Current.CancellationToken;
        await using var stream = await LiveStream.OpenAsync(gallery, "live-charts/live", cancel);
        var pushes = new List<string>();
        while (pushes.Count < 30 && (pushes.Count < 6 || !Titles.All(title => pushes.Count(push => Region(push, title) is not null) >= 2)))
        {
            pushes.Add((await stream.TickedAsync(gallery, cancel)).Data);
        }

        var interval = global::Gallery.Models.LiveSchedule.Interval;
        var ticks = pushes.SelectMany(push => Titles.Select(title => Region(push, title)).OfType<string>()).Select(Tick).ToList();
        var span = ticks.Max() - ticks.Min();

        Assert.True(ticks.Count <= pushes.Count * Titles.Length / 2, $"{ticks.Count} views in {pushes.Count} pushes");
        foreach (var title in Titles)
        {
            var own = pushes.Select(push => Region(push, title)).OfType<string>().Select(Tick).ToList();
            Assert.True(own.Count >= 2 && own.Count <= (span / interval) + 1, $"{title} pushed at ticks {string.Join(", ", own)} of {ticks.Min()} to {ticks.Max()}");
        }

        Assert.All(pushes.SelectMany(push => Titles.Select(title => Region(push, title)).OfType<string>()).SelectMany(Morphs), length => Assert.True(length < interval * 1000, $"a morph of {length} ms"));
    }

    private static int Tick(string region) => int.Parse(Ticks().Match(region).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

    private static IEnumerable<int> Morphs(string region) =>
        MorphTag().Matches(region).Select(match => Number(match.Value, MorphDuration()) + Number(match.Value, MorphDelay()))
            .Concat(CssTiming().Matches(region).Select(match => int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)));

    private static int Number(string tag, Regex attribute) => attribute.Match(tag) is { Success: true } found ? int.Parse(found.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0;

    [Fact]
    [Trait("AC", "MATRIX-06")]
    public async Task TheLineViewIsPushedWithTheNextDataAndItsMorphAsync() => await PushedAsync("Line chart", "recharts-line-curve", "line monotone-x");

    [Fact]
    [Trait("AC", "MATRIX-07")]
    public async Task TheAreaViewIsPushedWithTheNextDataAndItsMorphAsync() => await PushedAsync("Area chart", "recharts-area-area", "band monotone-x");

    [Fact]
    [Trait("AC", "MATRIX-08")]
    public async Task TheBarViewIsPushedWithTheNextDataAndItsMorphAsync() => await PushedAsync("Bar chart", "recharts-rectangle", "rect");

    [Fact]
    [Trait("AC", "MATRIX-09")]
    public async Task TheComposedViewIsPushedWithTheNextDataAndItsMorphsAsync()
    {
        var region = await PushedAsync("Composed chart", "recharts-line-curve", "line monotone-x");

        Assert.Contains("data-aioros-morph-path=\"rect\"", region, StringComparison.Ordinal);
        Assert.Contains("data-aioros-morph-path=\"band monotone-x\"", region, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "MATRIX-10")]
    public async Task TheScatterViewIsPushedWithTheNextDataAndItsMorphAsync() => await PushedAsync("Scatter chart", "recharts-symbols", "glyph");

    [Fact]
    [Trait("AC", "MATRIX-11")]
    public async Task ThePieViewIsPushedWithTheNextDataAndItsMorphAsync() => await PushedAsync("Pie chart", "recharts-sector", "sector");

    [Fact]
    [Trait("AC", "MATRIX-12")]
    public async Task TheRadarViewIsPushedWithTheNextDataAndItsMorphAsync()
    {
        var region = await PushedAsync("Radar chart", "recharts-polygon", "polygon");

        Assert.Contains("data-aioros-morph-map=\"ratio\"", region, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "MATRIX-13")]
    public async Task TheRadialBarViewIsPushedWithTheNextDataAndItsMorphAsync() => await PushedAsync("Radial bar chart", "recharts-radial-bar-sector", "sector");

    [Fact]
    [Trait("AC", "MATRIX-14")]
    public async Task TheFunnelViewIsPushedWithTheNextDataAndItsMorphAsync()
    {
        var region = await PushedAsync("Funnel chart", "recharts-trapezoid", "polygon");

        Assert.Contains("data-aioros-morph-from=", region, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "MATRIX-15")]
    public async Task TheTreemapViewIsPushedWithTheNextLayoutAndItsTransitionsAsync()
    {
        var region = await PushedAsync("Treemap", "recharts-rectangle", null);

        Assert.Contains("transition:d ", region, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "MATRIX-16")]
    public async Task TheSankeyViewIsPushedWithTheNextDataWithoutAMorphAsync() => await PushedAsync("Sankey", "recharts-sankey-link", null);

    [Fact]
    [Trait("AC", "MATRIX-17")]
    public async Task TheSunburstViewIsPushedWithTheNextDataWithoutAMorphAsync() => await PushedAsync("Sunburst chart", "recharts-sector", null);

    /// <summary>
    /// The region of the view titled <paramref name="title"/> in a push of the live charts' stream at a later tick than
    /// the first push carrying it (the family is due every fourth tick, GALLERY-06, so the test ticks the Gallery's clock
    /// and reads a push per tick until two carry it): its shapes of class <paramref name="shape"/> moved between them, and
    /// it declares <paramref name="morph"/> (or no morph at all). Other tests' ticks may add pushes in between, which only
    /// move it further.
    /// </summary>
    private async Task<string> PushedAsync(string title, string shape, string? morph)
    {
        var cancel = TestContext.Current.CancellationToken;
        await using var stream = await LiveStream.OpenAsync(gallery, "live-charts/live", cancel);
        var first = await CarriedAsync(stream, title, null, cancel);
        var second = await CarriedAsync(stream, title, Ticks().Match(first).Value, cancel);

        Assert.Matches(Ticks(), second);
        Assert.NotEqual(Shapes(first, shape), Shapes(second, shape));
        Assert.NotEmpty(Shapes(second, shape));
        if (morph is null)
        {
            Assert.DoesNotContain("data-aioros-morph-path", second, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains($"data-aioros-morph-path=\"{morph}\"", second, StringComparison.Ordinal);
        }

        return second;
    }

    /// <summary>Ticks and reads a push per tick until one carries the view titled <paramref name="title"/> at another tick than <paramref name="after"/>.</summary>
    private async Task<string> CarriedAsync(LiveStream stream, string title, string? after, CancellationToken cancel)
    {
        var bound = 3 * global::Gallery.Models.LiveSchedule.Interval;
        for (var tick = 0; tick < bound; tick++)
        {
            if (Region((await stream.TickedAsync(gallery, cancel)).Data, title) is { } region && Ticks().Match(region).Value != after)
            {
                return region;
            }
        }

        throw new Xunit.Sdk.XunitException($"{title} was not pushed in {bound} ticks");
    }

    private static string? Region(string push, string title)
    {
        var marker = $">{title}</";
        return push.Split("<div data-aioros-view=", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(region => region.Contains(marker, StringComparison.Ordinal));
    }

    private static string Shapes(string region, string shape) =>
        string.Join('\n', Shape().Matches(region).Where(match => match.Value.Contains(shape, StringComparison.Ordinal)).Select(match => match.Value));

    [GeneratedRegex(@"Tick (\d+) at \d\d:\d\d")]
    private static partial Regex Ticks();

    [GeneratedRegex(@"<[^>]*data-aioros-morph-duration=""\d+""[^>]*>")]
    private static partial Regex MorphTag();

    [GeneratedRegex(@"data-aioros-morph-duration=""(\d+)""")]
    private static partial Regex MorphDuration();

    [GeneratedRegex(@"data-aioros-morph-delay=""(\d+)""")]
    private static partial Regex MorphDelay();

    [GeneratedRegex(@"--pd:(\d+)ms")]
    private static partial Regex CssTiming();

    [GeneratedRegex(@"<(path|rect|polygon)\b[^>]*>")]
    private static partial Regex Shape();
}
