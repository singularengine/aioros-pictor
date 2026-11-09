using System.Buffers;
using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Svg;

namespace Gallery.Compare;

/// <summary>A chart family of the comparison pages: its route, its title and the kind of Recharts' examples it holds.</summary>
public sealed record Family(string Route, string Title, string Kind);

/// <summary>
/// Recharts' reference examples (<c>tools/recharts/examples</c>, copied beside the app with Recharts' renders), read
/// once into Pictor's specs, with each example's verdict: Pictor's markup compared element by element with Recharts'
/// client render by the comparison the tests use (<c>MarkupComparison</c>), and the share of inked pixels that differ
/// strongly from Recharts' screenshot, measured by <c>samples/Demo/compare.mjs</c> and recorded by
/// <c>samples/Gallery/pixels.mjs</c> in <c>Reference/pixels.json</c> with the hash of the markup measured: a share is
/// shown only while Pictor still writes that markup, and an example measured on another markup, or never, counts as
/// not measured.
/// </summary>
public static class ReferenceExamples
{
    public static readonly ImmutableArray<Family> Families =
    [
        new("line", "Line charts", "LineChart"),
        new("area", "Area charts", "AreaChart"),
        new("bar", "Bar charts", "BarChart"),
        new("composed", "Composed charts", "ComposedChart"),
        new("scatter", "Scatter charts", "ScatterChart"),
        new("pie", "Pie charts", "PieChart"),
        new("radar", "Radar charts", "RadarChart"),
        new("radial-bar", "Radial bar charts", "RadialBarChart"),
        new("funnel", "Funnel charts", "FunnelChart"),
        new("treemap", "Treemaps", "Treemap"),
        new("sankey", "Sankey diagrams", "Sankey"),
        new("sunburst", "Sunburst charts", "SunburstChart"),
    ];

    private static Catalog? _loaded;

    /// <summary>The folder the reference files are copied to beside the app.</summary>
    public static string Folder { get; } = Path.Combine(AppContext.BaseDirectory, "Reference");

    /// <summary>The Chromium version, the commit and the date the recorded pixel differences were measured with.</summary>
    public static string PixelsMeasured => Loaded.Measured;

    internal static RechartsExample? Find(string? name) => name is not null && Loaded.Examples.TryGetValue(name, out var example) ? example.Example : null;

    /// <summary>The compared examples of a family, in name order; custom content is left to the custom content page, whose templates the reference needs.</summary>
    public static ImmutableArray<ComparedExample> Of(string kind) => Loaded.ByKind.GetValueOrDefault(kind, []);

    /// <summary>The markup Pictor's writer writes now for a reference example, by name, as its comparison measures it.</summary>
    public static string? MarkupOf(string name) => Find(name) is { } example ? Markup(example) : null;

    /// <summary>Recharts' screenshot of an example, as committed in <c>tools/recharts/out</c> (Recharts 3's of <c>out/recharts3</c> where Pictor follows it).</summary>
    public static string Png(string name) => Followed(Path.Combine(Folder, "png3", name + ".png"), Path.Combine(Folder, "png", name + ".png"));

    private static string Followed(string recharts3, string recharts2) => File.Exists(recharts3) ? recharts3 : recharts2;

    private static Catalog Loaded => Volatile.Read(ref _loaded) ?? throw new InvalidOperationException("The reference examples are read when the Gallery starts (LoadAsync).");

    /// <summary>Reads the reference examples and their recorded measurements once, before the Gallery answers a request.</summary>
    public static async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _loaded) is null)
        {
            Volatile.Write(ref _loaded, await ReadAsync(cancellationToken));
        }
    }

    private static async Task<Catalog> ReadAsync(CancellationToken cancellationToken)
    {
        var pixels = await PixelsAsync(cancellationToken);
        var examples = new Dictionary<string, (RechartsExample Example, ComparedExample Compared)>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(Folder, "examples"), "*.json").Order(StringComparer.Ordinal))
        {
            using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(file, cancellationToken));
            var root = document.RootElement;
            if (root.GetRawText().Contains("\"$content\"", StringComparison.Ordinal))
            {
                continue;
            }

            var example = RechartsExamples.Read(root);
            var source = root.TryGetProperty("source", out var described) ? described.GetString() ?? string.Empty : string.Empty;
            var recharts = await File.ReadAllTextAsync(Followed(Path.Combine(Folder, "recharts3", example.Name + ".client.html"), Path.Combine(Folder, "recharts", example.Name + ".client.html")), cancellationToken);
            examples[example.Name] = (example, Compare(example, root.GetProperty("kind").GetString()!, source, recharts, pixels.Results));
        }

        var byKind = examples.Values.GroupBy(entry => entry.Compared.Kind, StringComparer.Ordinal)
            .ToImmutableDictionary(group => group.Key, group => group.Select(entry => entry.Compared).ToImmutableArray(), StringComparer.Ordinal);
        return new Catalog(examples.ToImmutableDictionary(StringComparer.Ordinal), byKind, pixels.Measured);
    }

    private static ComparedExample Compare(RechartsExample example, string kind, string source, string recharts, Dictionary<string, Measurement> pixels)
    {
        var markup = Markup(example);
        var verdict = MarkupComparison.Compare(recharts, markup);
        var structure = verdict.Matches
            ? $"Elements: all {verdict.Compared} match"
            : $"Elements: {verdict.Differences.Count} of {verdict.Compared} differ";
        var measured = pixels.TryGetValue(example.Name, out var measurement) && measurement.MarkupHash == Hash(markup);
        var pixel = measured
            ? "Pixels: " + (measurement!.Share * 100).ToString("0.00", CultureInfo.InvariantCulture) + "% of inked pixels differ strongly"
            : measurement is null ? "Pixels: not measured" : "Pixels: not measured on this markup (measured on an earlier one)";
        return new ComparedExample(example.Name, kind, source, example.Spec.Width, example.Spec.Height, structure, pixel, verdict.Matches, measured ? measurement!.Share : null);
    }

    /// <summary>The hash <c>pixels.mjs</c> records of the markup measured: the first 16 hexadecimal digits of its UTF-8 SHA-256.</summary>
    public static string Hash(string markup) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(markup)))[..16];

    private static string Markup(RechartsExample example)
    {
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, example.Spec, example.Data, static (_, _) => null);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static async Task<(string Measured, Dictionary<string, Measurement> Results)> PixelsAsync(CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(Path.Combine(Folder, "pixels.json"), cancellationToken));
        var root = document.RootElement;
        var results = root.GetProperty("results").EnumerateObject().ToDictionary(
            entry => entry.Name,
            entry => new Measurement(entry.Value.GetProperty("share").GetDouble(), entry.Value.GetProperty("markup").GetString()!),
            StringComparer.Ordinal);
        return ($"Chromium {root.GetProperty("chromium").GetString()}, Pictor {root.GetProperty("commit").GetString()}, on {root.GetProperty("date").GetString()}", results);
    }

    private sealed record Measurement(double Share, string MarkupHash);

    private sealed record Catalog(
        ImmutableDictionary<string, (RechartsExample Example, ComparedExample Compared)> Examples,
        ImmutableDictionary<string, ImmutableArray<ComparedExample>> ByKind,
        string Measured);
}

/// <summary>A reference example and its verdict against Recharts' render: its elements, and its pixels when they were measured on the markup Pictor writes now.</summary>
public sealed record ComparedExample(string Name, string Kind, string Source, double Width, double Height, string Structure, string Pixels, bool ElementsMatch, double? Share)
{
    /// <summary>Whether its pixels were measured on the markup Pictor writes now.</summary>
    public bool Measured => Share is not null;

    /// <summary>Whether its elements match and its measured pixels hold none that differs strongly.</summary>
    public bool Matches => ElementsMatch && Share < 0.001;

    public string Verdict => !ElementsMatch ? "differs" : Share is null ? "not measured" : Matches ? "match" : "differs";

    /// <summary>A family's headline: the matching examples among those measured, and how many were not measured.</summary>
    public static string Headline(IReadOnlyCollection<ComparedExample> examples, string noun)
    {
        var measured = examples.Count(example => example.Measured);
        var unmeasured = examples.Count - measured;
        var head = $"{examples.Count(example => example.Matches)} of {measured} measured {noun}";
        return unmeasured == 0 ? head : $"{head}; {unmeasured} not measured on Pictor's current markup ({examples.Count(example => !example.Measured && example.ElementsMatch)} of them match element for element)";
    }
}

/// <summary>A family's line on the comparison's index.</summary>
public sealed record FamilyVerdict(string Title, string Href, string Summary);

/// <summary>The comparison's index: every family with its summary.</summary>
public sealed class ComparisonIndex
{
    public IReadOnlyList<FamilyVerdict> Families { get; } = [.. ReferenceExamples.Families.Select(family =>
    {
        var examples = ReferenceExamples.Of(family.Kind);
        return new FamilyVerdict(family.Title, "/compare/" + family.Route, ComparedExample.Headline(examples, "examples match Recharts' render"));
    })];

    public string Measured { get; } = ReferenceExamples.PixelsMeasured;
}

/// <summary>A family's comparison page: its reference examples, each with its verdict.</summary>
public sealed class FamilyComparison(Family family)
{
    public string Title { get; } = family.Title + " compared with Recharts";

    public IReadOnlyList<ComparedExample> Examples { get; } = ReferenceExamples.Of(family.Kind);

    public string Summary => ComparedExample.Headline([.. Examples], "reference examples match Recharts' render element for element, without a pixel that differs strongly") + ".";

    public string FamilyHref { get; } = "/" + family.Route;

    public string Measured { get; } = ReferenceExamples.PixelsMeasured;
}
