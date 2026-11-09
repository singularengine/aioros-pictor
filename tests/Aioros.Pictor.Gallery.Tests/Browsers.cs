namespace Aioros.Pictor.Gallery.Tests;

/// <summary>The browsers the Gallery's browser tests drive: the installed Chromium (the path in <c>CHROMIUM</c>, <c>/usr/bin/chromium</c> by default, as CI installs it).</summary>
internal static class Browsers
{
    public static string Chromium { get; } = Environment.GetEnvironmentVariable("CHROMIUM") is { Length: > 0 } path ? path : "/usr/bin/chromium";

    /// <summary>Fails with the path to set when no Chromium is installed where the tests look.</summary>
    public static void Require() => Assert.True(File.Exists(Chromium), $"No Chromium at {Chromium}: install it or set CHROMIUM to its executable.");
}
