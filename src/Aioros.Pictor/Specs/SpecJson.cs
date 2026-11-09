using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aioros.Pictor.Specs;

/// <summary>
/// A chart spec as the text a plan records at build time and the web writer reads back once per plan: the spec as JSON,
/// followed, when it carries a theme, by the theme as a second JSON text after a record separator (a JSON text
/// sequence, RFC 7464), each read by its own serializer.
/// </summary>
public static class SpecJson
{
    private const char Separator = '\u001e';

    /// <summary>The spec as compact JSON, leaving out what has its default value, and its theme after it when it has one.</summary>
    /// <param name="spec">The spec.</param>
    /// <returns>The JSON text.</returns>
    public static string Write(ChartSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var json = JsonSerializer.Serialize(spec, SpecJsonContext.Default.ChartSpec);
        return spec.Theme is { } theme ? json + Separator + JsonSerializer.Serialize(theme, ThemeJsonContext.Default.ChartTheme) : json;
    }

    /// <summary>The spec a JSON text holds, with its theme.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The spec.</returns>
    public static ChartSpec Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var split = json.IndexOf(Separator, StringComparison.Ordinal);
        var spec = JsonSerializer.Deserialize(split < 0 ? json : json[..split], SpecJsonContext.Default.ChartSpec) ?? throw new JsonException("The chart spec is empty.");
        return split < 0 ? spec : spec with { Theme = JsonSerializer.Deserialize(json.AsSpan(split + 1), ThemeJsonContext.Default.ChartTheme) };
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1506", Justification = "The generated serializer names every spec type, and the theme the spec carries; the coupling is the spec's, not the context's.")]
[JsonSerializable(typeof(ChartSpec))]
[JsonSerializable(typeof(List<GradientSpec>))]
[JsonSerializable(typeof(System.Collections.Immutable.ImmutableArray<double>))]
[JsonSourceGenerationOptions(UseStringEnumConverter = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class SpecJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(Theming.ChartTheme))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class ThemeJsonContext : JsonSerializerContext;
