using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Aioros.Pictor.Gallery.Tests;

public sealed partial class TemplateAppTests
{
    private const string PictorVersion = "0.1.0-preview.1";

    [Fact]
    [Trait("AC", "TEMPLATE-01")]
    public async Task TheTemplateCreatesAPictorAppThatBuildsAndPrecompilesAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var repository = Root(AppContext.BaseDirectory);
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var userCache = Environment.GetEnvironmentVariable("NUGET_PACKAGES") is { Length: > 0 } configured ? configured : Path.Combine(home, ".nuget", "packages");
        var userHive = Path.Combine(home, ".templateengine");
        var before = (Stamp(userCache), Stamp(userHive));
        var aioros = AiorosVersion().Match(await File.ReadAllTextAsync(Path.Combine(repository, "Directory.Packages.props"), cancellationToken)).Groups["version"].Value;
        var work = Directory.CreateTempSubdirectory("pictor-template-");
        try
        {
            var feed = Path.Combine(work.FullName, "feed");
            var hive = Path.Combine(work.FullName, "hive");
            var cache = Path.Combine(work.FullName, "packages");
            var plans = Path.Combine(work.FullName, "plans.json");
            await File.WriteAllTextAsync(Path.Combine(work.FullName, "NuGet.config"), $"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <config><add key="globalPackagesFolder" value="{cache}" /></config>
                  <packageSources>
                    <clear />
                    <add key="pictor" value="{feed}" />
                    <add key="cache" value="{userCache}" />
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                  </packageSources>
                  <packageSourceMapping>
                    <packageSource key="pictor"><package pattern="Aioros.Pictor" /><package pattern="Aioros.Pictor.*" /></packageSource>
                    <packageSource key="cache"><package pattern="*" /></packageSource>
                    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
                  </packageSourceMapping>
                </configuration>
                """, cancellationToken);
            var isolated = new Dictionary<string, string> { ["NUGET_PACKAGES"] = cache };
            var repositoryCache = new Dictionary<string, string>();

            var library = await DotnetAsync(repository, repositoryCache, "pack", Path.Combine(repository, "src", "Aioros.Pictor", "Aioros.Pictor.csproj"), "-c", "Release", "-m:4", "-p:PackageOutputPath=" + feed, "-p:Version=" + PictorVersion);
            var charts = await DotnetAsync(repository, repositoryCache, "pack", Path.Combine(repository, "src", "Aioros.Pictor.Avalonia", "Aioros.Pictor.Avalonia.csproj"), "-c", "Release", "-m:4", "-p:PackageOutputPath=" + feed, "-p:Version=" + PictorVersion);
            var templates = await DotnetAsync(repository, repositoryCache, "pack", Path.Combine(repository, "templates", "Aioros.Pictor.Templates.csproj"), "-p:PackageOutputPath=" + feed);
            Assert.True(library.Exit == 0, library.Output);
            Assert.True(charts.Exit == 0, charts.Output);
            Assert.True(templates.Exit == 0, templates.Output);
            var package = Assert.Single(Directory.GetFiles(feed, "Aioros.Pictor.Templates.*.nupkg"));
            var installed = await DotnetAsync(work.FullName, isolated, "new", "install", package, "--debug:custom-hive", hive);
            var created = await DotnetAsync(work.FullName, isolated, "new", "pictor", "-n", "Dashboard", "-o", "Dashboard", "--AiorosVersion", aioros, "--PictorVersion", PictorVersion, "--debug:custom-hive", hive);
            var project = Path.Combine(work.FullName, "Dashboard", "Dashboard", "Dashboard.csproj");
            var built = await DotnetAsync(work.FullName, isolated, "build", project, "-tl:off", "--disable-build-servers", "-m:4");
            var ran = await DotnetAsync(work.FullName, isolated, "run", "--no-build", "--no-launch-profile", "--project", project, "--", "--precompile=" + plans);
            var precompiled = Precompiled().Match(ran.Output);

            Assert.True(installed.Exit == 0, installed.Output);
            Assert.True(created.Exit == 0, created.Output);
            Assert.True(built.Exit == 0, built.Output);
            Assert.Matches(@"\b0 Warning\(s\)", built.Output);
            Assert.True(ran.Exit == 0, ran.Output);
            Assert.True(precompiled.Success, ran.Output);
            Assert.Equal(precompiled.Groups["planned"].Value, precompiled.Groups["pages"].Value);
            Assert.NotEqual("0", precompiled.Groups["pages"].Value);
            Assert.True(File.Exists(plans));
            Assert.Equal(before, (Stamp(userCache), Stamp(userHive)));
        }
        finally
        {
            work.Delete(recursive: true);
        }
    }

    [GeneratedRegex(@"Precompiled (?<planned>\d+) of (?<pages>\d+) pages")]
    private static partial Regex Precompiled();

    [GeneratedRegex(@"<AiorosVersion>(?<version>[^<]+)</AiorosVersion>")]
    private static partial Regex AiorosVersion();

    private static DateTime? Stamp(string directory) => Directory.Exists(directory) ? Directory.GetLastWriteTimeUtc(directory) : null;

    private static string Root(string directory) =>
        File.Exists(Path.Combine(directory, "Aioros.Pictor.slnx"))
            ? directory
            : Root(Directory.GetParent(directory)?.FullName ?? throw new DirectoryNotFoundException("Aioros.Pictor.slnx not found"));

    private static async Task<(int Exit, string Output)> DotnetAsync(string directory, IReadOnlyDictionary<string, string> environment, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        foreach (var inherited in start.Environment.Keys.Where(key => key.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase) || key.StartsWith("DOTNET_HOST", StringComparison.Ordinal)).ToList())
        {
            start.Environment.Remove(inherited);
        }

        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_NOLOGO"] = "1";
        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        foreach (var (key, value) in environment)
        {
            start.Environment[key] = value;
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("dotnet did not start.");
        var output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var error = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        return (process.ExitCode, $"dotnet {string.Join(' ', arguments)}\n{await output}{await error}");
    }
}
