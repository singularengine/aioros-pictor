using System.Diagnostics;
using System.Text.RegularExpressions;

[assembly: AssemblyFixture(typeof(Aioros.Pictor.Gallery.Tests.GalleryHost))]

namespace Aioros.Pictor.Gallery.Tests;

/// <summary>
/// The Gallery as a user reaches it: its built application started on a port Kestrel picks (the address read from its
/// "Now listening on" line), every page answered once before the tests run (<see cref="WarmAsync"/>), its clock standing
/// still until a test ticks it (<see cref="TickAsync"/>), and stopped after the tests. A Gallery that exits or stays silent past <see cref="Startup"/> fails the fixture with its output.
/// </summary>
public sealed partial class GalleryHost : IAsyncLifetime
{
    /// <summary>The moment the Gallery's clock starts at: two seconds before midnight, so the live views cross a day.</summary>
    public const string ClockStart = "2026-10-14T23:59:58Z";

    /// <summary>The pages a visitor reaches from the Gallery's navigation.</summary>
    public static readonly string[] Routes =
    [
        "/dashboard", "/about", "/line", "/area", "/bar", "/composed", "/scatter", "/pie", "/radar", "/radial-bar", "/funnel", "/treemap",
        "/sankey", "/sunburst", "/axes", "/tips", "/ranges", "/custom", "/interactions", "/interactive", "/live-charts", "/families", "/compare", "/binding", "/themes", "/features", "/more",
    ];

    /// <summary>How long the Gallery may take to listen.</summary>
    public static readonly TimeSpan Startup = TimeSpan.FromSeconds(60);

    private readonly System.Collections.Concurrent.ConcurrentQueue<string> _errors = new();

    private Process? _process;

    private Task? _drain;

    public HttpClient Client { get; } = new();

    public Uri Address { get; private set; } = new("http://127.0.0.1/");

    /// <summary>The Gallery project's folder, where it runs as <c>dotnet run</c> runs it.</summary>
    public static string ProjectFolder { get; } = Path.Combine(Root(), "samples", "Gallery", "Gallery");

    /// <summary>The Gallery's built application, in the configuration the tests were built in.</summary>
    public static string Application { get; } = Path.Combine(ProjectFolder, "bin", Configuration(), "net10.0", "Gallery.dll");

    public async ValueTask InitializeAsync()
    {
        await global::Gallery.Compare.ReferenceExamples.LoadAsync(CancellationToken.None);
        _process = Start(_errors, Application, "--urls", "http://127.0.0.1:0");
        using var startup = new CancellationTokenSource(Startup, TimeProvider.System);
        try
        {
            while (await _process.StandardOutput.ReadLineAsync(startup.Token) is { } line)
            {
                if (Listening().Match(line) is { Success: true } listening)
                {
                    Address = new Uri(listening.Groups[1].Value + "/");
                    Client.BaseAddress = Address;
                    _drain = _process.StandardOutput.ReadToEndAsync();
                    await WarmAsync();
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (startup.IsCancellationRequested)
        {
            throw new InvalidOperationException($"The Gallery did not listen within {Startup.TotalSeconds} s: {string.Join('\n', _errors)}");
        }

        await _process.WaitForExitAsync();
        throw new InvalidOperationException($"The Gallery exited with code {_process.ExitCode} before listening: {string.Join('\n', _errors)}");
    }

    /// <summary>
    /// Answers every page once, one after another, before any test runs: the Gallery compiles a page's plan the first time it
    /// answers it, on the one thread its engine renders on, beside its own warm-up of every page, so the tests' first
    /// requests and live pushes, started together, would otherwise queue behind all of those compilations on a slow runner.
    /// Each page's answer is the signal that its plan is compiled.
    /// </summary>
    private async Task WarmAsync()
    {
        foreach (var page in (string[])[.. Routes, "/extras", "/updates", "/compare/line"])
        {
            using var bound = new Patience(CancellationToken.None);
            using var answer = await Client.GetAsync(page.TrimStart('/'), bound.Token);
            answer.EnsureSuccessStatusCode();
        }
    }

    /// <summary>Ticks the Gallery's clock once (the live views' one-second tick) and returns once the tick has run and its keys are invalidated.</summary>
    public async Task TickAsync(CancellationToken cancellationToken)
    {
        using var ticked = await Client.PostAsync("test/tick", null, cancellationToken);
        ticked.EnsureSuccessStatusCode();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
        }

        if (_drain is not null)
        {
            await _drain;
        }

        _process?.Dispose();
    }

    /// <summary>Starts the Gallery's application with <paramref name="arguments"/>, its output redirected, on the tests' clock.</summary>
    public static Process Start(params string[] arguments) => Start(new System.Collections.Concurrent.ConcurrentQueue<string>(), arguments);

    private static Process Start(System.Collections.Concurrent.ConcurrentQueue<string> errors, params string[] arguments)
    {
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            WorkingDirectory = ProjectFolder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        start.Environment["DOTNET_NOLOGO"] = "1";
        start.Environment["GALLERY_CLOCK"] = ClockStart;
        var process = Process.Start(start) ?? throw new InvalidOperationException("The Gallery did not start.");
        process.ErrorDataReceived += (_, line) =>
        {
            if (line.Data is { } data)
            {
                errors.Enqueue(data);
            }
        };
        process.BeginErrorReadLine();
        return process;
    }

    private static string Configuration() =>
        AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar).Contains("Release", StringComparer.Ordinal) ? "Release" : "Debug";

    private static string Root()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "Aioros.Pictor.slnx")))
        {
            folder = folder.Parent;
        }

        return folder?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }

    [GeneratedRegex(@"Now listening on: (http://[^\s]+)")]
    private static partial Regex Listening();
}
