namespace Aioros.Pictor.Demo;

/// <summary>
/// Where an example's Recharts reference lies: Recharts 3's render (<c>tools/recharts/out/recharts3</c>) for the examples
/// Pictor takes from Recharts 3, Recharts 2's otherwise.
/// </summary>
internal static class ReferenceFiles
{
    /// <summary>The path of a reference file of <c>tools/recharts/out</c> (such as <c>Name.png</c> or <c>Name.0.client.html</c>), Recharts 3's when it has one.</summary>
    public static string Of(string root, string file)
    {
        var folder = Path.Combine(root, "tools", "recharts", "out");
        var followed = Path.Combine(folder, "recharts3", file);
        return File.Exists(followed) ? followed : Path.Combine(folder, file);
    }
}
