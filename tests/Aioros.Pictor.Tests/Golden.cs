using System.Text.Json;

namespace Aioros.Pictor.Tests;

internal static class Golden
{
    public static async Task<IReadOnlyList<JsonElement>> CasesAsync(string name)
    {
        await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Golden", name));
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        return [.. document.RootElement.EnumerateArray().Select(item => item.Clone())];
    }
}
