using System.Buffers;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Demo;

/// <summary>One example's results: the structure verdict, Pictor's server time and bytes.</summary>
internal sealed record ServerResult(string Name, int Differences, int Elements, double MedianUs, long Iterations, int Bytes, int Brotli, int RechartsBytes, int RechartsBrotli);

/// <summary>
/// Writes <c>out/index.html</c>: each Recharts example drawn by Recharts (its render after mount in Chromium), by
/// Pictor on the web (its markup, live: hover it) and by Pictor on the desktop (Skia), with the verdicts and the
/// timings of every side, as one self-contained page with no script.
/// </summary>
internal static class DemoPage
{
    private const string Style = """
        :root{color-scheme:light;--bg:#f4f4f2;--surface:#fcfcfb;--text:#0b0b0b;--muted:#52514e;--faint:#8a8984;--border:#dddcd7;--ok:#0a7a3d;--diff:#b3261e}
        @media (prefers-color-scheme:dark){:root{color-scheme:dark;--bg:#111110;--surface:#1a1a19;--text:#f0efec;--muted:#c3c2b7;--faint:#8f8e86;--border:#2f2f2d;--ok:#5cc98a;--diff:#f2867e}}
        *{box-sizing:border-box}
        body{margin:0;background:var(--bg);color:var(--text);font:15px/1.5 system-ui,-apple-system,"Segoe UI",Roboto,sans-serif}
        main{max-width:1640px;margin:0 auto;padding:24px 16px 48px}
        h1{font-size:28px;margin:0 0 4px;letter-spacing:-.01em}
        h2{font-size:18px;margin:0}
        p{margin:4px 0;color:var(--muted)}
        p.env{font-size:13px}
        code{font:13px ui-monospace,SFMono-Regular,Menlo,monospace}
        section{background:var(--surface);border:1px solid var(--border);border-radius:12px;padding:16px;margin-top:20px}
        .trio{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,500px),1fr));gap:16px;margin-top:12px}
        figure{margin:0;min-width:0}
        figcaption{font-size:12px;font-weight:600;text-transform:uppercase;letter-spacing:.06em;color:var(--faint);margin-bottom:4px}
        .canvas{background:#fff;color:#000;font:14px Inter;border-radius:8px;overflow-x:auto;line-height:normal}
        .canvas img{display:block}
        .verdict{font-size:14px;margin-top:8px}
        .ok{color:var(--ok);font-weight:600}.diff{color:var(--diff);font-weight:600}
        .table-wrap{overflow-x:auto;margin-top:8px}
        table{border-collapse:collapse;width:100%;font-size:14px;font-variant-numeric:tabular-nums}
        th,td{text-align:left;padding:6px 10px;border-bottom:1px solid var(--border);white-space:nowrap}
        th{color:var(--muted);font-weight:600}td.n,th.n{text-align:right}
        """;

    public static async Task WriteAsync(IReadOnlyList<RechartsExample> examples, string root, string output, string commit)
    {
        var server = new List<ServerResult>();
        var warm = new ArrayBufferWriter<byte>(64 * 1024);
        for (var round = 0; round < 200; round++)
        {
            foreach (var example in examples)
            {
                warm.ResetWrittenCount();
                ChartSvg.Write(warm, example.Spec, example.Data, static (_, _) => null);
            }
        }

        foreach (var example in examples)
        {
            server.Add(await ServerAsync(example, root));
        }

        var recharts = await ReadAsync(Path.Combine(root, "tools", "recharts", "bench-results", "recharts-ssr.json"));
        var client = await ReadAsync(Path.Combine(root, "tools", "recharts", "bench-results", "recharts-client.json"));
        var compare = await ReadAsync(Path.Combine(output, "compare.json"));
        var desktop = await ReadAsync(Path.Combine(output, "desktop.json"));
        var environment = await MachineAsync(commit);
        var results = new { environment, server, compare, desktop, recharts, client };
        await File.WriteAllTextAsync(Path.Combine(output, "results.json"), JsonSerializer.Serialize(results, Json.Options));
        var font = Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(root, "tools", "recharts", "fonts", "Inter-Regular-latin.woff2")));
        var html = new StringBuilder();
        html.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<title>Pictor demo</title>\n<style>\n")
            .Append("@font-face{font-family:Inter;font-weight:400;src:url(data:font/woff2;base64,").Append(font).Append(") format(\"woff2\")}\n")
            .Append(Style).Append("\n</style>\n</head>\n<body>\n<main>\n");
        Intro(html, environment, recharts, client);
        Summary(html, server, recharts, client, compare, desktop);
        foreach (var example in examples)
        {
            await SectionAsync(html, example, root, output, server.Single(item => item.Name == example.Name), compare, desktop);
        }

        html.Append("</main>\n</body>\n</html>\n");
        await File.WriteAllTextAsync(Path.Combine(output, "index.html"), html.ToString());
        await Console.Out.WriteLineAsync($"wrote {Path.Combine(output, "index.html")} ({html.Length / 1024} KB)");
    }

    private static async Task<ServerResult> ServerAsync(RechartsExample example, string root)
    {
        var buffer = new ArrayBufferWriter<byte>(64 * 1024);
        var (ns, iterations) = Timing.Measure(() =>
        {
            buffer.ResetWrittenCount();
            ChartSvg.Write(buffer, example.Spec, example.Data, static (_, _) => null);
        });
        var markup = buffer.WrittenSpan.ToArray();
        var reference = await File.ReadAllBytesAsync(Path.Combine(root, "tools", "recharts", "out", example.Name + ".html"));
        var verdict = MarkupComparison.Compare(await File.ReadAllTextAsync(ReferenceFiles.Of(root, example.Name + ".client.html")), Encoding.UTF8.GetString(markup));
        return new ServerResult(example.Name, verdict.Differences.Count, verdict.Compared, ns / 1000, iterations, markup.Length, Brotli(markup), reference.Length, Brotli(reference));
    }

    private static int Brotli(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            brotli.Write(bytes);
        }

        return (int)output.Length;
    }

    private static async Task<JsonElement> ReadAsync(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        await using var stream = File.OpenRead(path);
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    private static async Task<Dictionary<string, string>> MachineAsync(string commit)
    {
        var cpu = File.Exists("/proc/cpuinfo") ? (await File.ReadAllLinesAsync("/proc/cpuinfo")).FirstOrDefault(line => line.StartsWith("model name", StringComparison.Ordinal))?.Split(':', 2)[1].Trim() : null;
        var load = File.Exists("/proc/loadavg") ? (await File.ReadAllTextAsync("/proc/loadavg")).Split(' ')[0] : "?";
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["commit"] = commit,
            ["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture),
            ["cpu"] = $"{cpu ?? "unknown CPU"}, {Environment.ProcessorCount} threads",
            ["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            ["load"] = load,
        };
    }

    private static void Intro(StringBuilder html, Dictionary<string, string> environment, JsonElement recharts, JsonElement client) =>
        html.Append("<h1>Pictor demo</h1>\n<p>Fifteen of Recharts' own examples (recharts.org), each drawn three times from the same data: by Recharts ")
            .Append(Text(recharts, "recharts")).Append(" in Chromium after mount (the reference), by Pictor on the web (the markup Aioros writes per request, live on this page: hover a chart) and by Pictor on the desktop (Avalonia and Skia). ")
            .Append("Text is Inter at 14&nbsp;px on every side. No script runs on this page.</p>\n<p class=\"env\">Pictor: ")
            .Append(environment["runtime"]).Append(", commit <code>").Append(environment["commit"]).Append("</code>, ").Append(environment["date"]).Append(", ")
            .Append(environment["cpu"]).Append(", load ").Append(environment["load"]).Append(". Recharts: Node ").Append(Text(recharts, "node")).Append(", React ")
            .Append(Text(recharts, "react")).Append(", ").Append(Text(recharts, "machine")).Append("; Chromium ").Append(Text(client, "chromium"))
            .Append(". A run on a loaded workstation shows trends; the lab runner's runs are in <code>docs/benchmarks.md</code>.</p>\n");

    private static void Summary(StringBuilder html, List<ServerResult> server, JsonElement recharts, JsonElement client, JsonElement compare, JsonElement desktop)
    {
        html.Append("<section>\n<h2>Summary</h2>\n<p>Structure: Pictor's markup against Recharts', element by element (no difference above 0.01&nbsp;px is a match). Pixels: share of inked pixels that differ by more than 64 in a channel from Recharts' screenshot. Server: Pictor writing the chart (layout and markup) against Recharts' <code>renderToStaticMarkup</code>. Chromium: showing Pictor's markup against React mounting Recharts. Bytes: the chart's markup, raw and brotli; Recharts' figure is its server markup, and its interactivity also needs its script.</p>\n<div class=\"table-wrap\"><table>\n<tr><th>Example</th><th>Structure</th><th class=\"n\">Web pixels</th><th class=\"n\">Desktop pixels</th><th class=\"n\">Pictor server</th><th class=\"n\">Recharts SSR</th><th class=\"n\">Faster</th><th class=\"n\">Pictor in Chromium</th><th class=\"n\">Recharts in Chromium</th><th class=\"n\">Desktop frame / redraw</th><th class=\"n\">Pictor bytes</th><th class=\"n\">Recharts bytes</th></tr>\n");
        foreach (var result in server)
        {
            var shown = Row(compare, result.Name);
            var drawn = Row(desktop, result.Name);
            var rechartsUs = Number(Row(recharts, result.Name), "medianUs");
            html.Append("<tr><td>").Append(result.Name).Append("</td><td>").Append(Verdict(result))
                .Append("</td><td class=\"n\">").Append(Percent(Number(shown, "strong"), Number(shown, "inked")))
                .Append("</td><td class=\"n\">").Append(Percent(Number(drawn, "strong"), Number(drawn, "inked")))
                .Append("</td><td class=\"n\">").Append(Us(result.MedianUs)).Append("</td><td class=\"n\">").Append(Us(rechartsUs))
                .Append("</td><td class=\"n\">").Append(double.IsNaN(rechartsUs) ? "–" : (rechartsUs / result.MedianUs).ToString("0", CultureInfo.InvariantCulture) + "×")
                .Append("</td><td class=\"n\">").Append(Us(1000 * Number(shown.ValueKind == JsonValueKind.Object && shown.TryGetProperty("paint", out var paint) ? paint : default, "medianMs")))
                .Append("</td><td class=\"n\">").Append(Us(Number(Row(client, result.Name), "medianUs")))
                .Append("</td><td class=\"n\">").Append(Us(Number(drawn, "firstFrameUs"))).Append(" / ").Append(Us(Number(drawn, "redrawUs")))
                .Append("</td><td class=\"n\">").Append(Kb(result.Bytes)).Append(" / ").Append(Kb(result.Brotli))
                .Append("</td><td class=\"n\">").Append(Kb(result.RechartsBytes)).Append(" / ").Append(Kb(result.RechartsBrotli)).Append("</td></tr>\n");
        }

        html.Append("</table></div>\n</section>\n");
    }

    private static string Verdict(ServerResult result) =>
        result.Differences == 0 ? "<span class=\"ok\">match</span>" : $"<span class=\"diff\">{result.Differences} differences</span>";

    private static async Task SectionAsync(StringBuilder html, RechartsExample example, string root, string output, ServerResult result, JsonElement compare, JsonElement desktop)
    {
        var reference = await File.ReadAllTextAsync(ReferenceFiles.Of(root, example.Name + ".client.html"));
        var pictor = await File.ReadAllTextAsync(Path.Combine(output, "pictor", example.Name + ".html"));
        var png = Path.Combine(output, "desktop", example.Name + ".png");
        html.Append("<section>\n<h2>").Append(example.Name).Append("</h2>\n<p class=\"verdict\">Structure: ").Append(Verdict(result))
            .Append(" (").Append(result.Elements).Append(" elements) · web pixels ").Append(Percent(Number(Row(compare, example.Name), "strong"), Number(Row(compare, example.Name), "inked")))
            .Append(" · desktop pixels ").Append(Percent(Number(Row(desktop, example.Name), "strong"), Number(Row(desktop, example.Name), "inked")))
            .Append(" · server ").Append(Us(result.MedianUs)).Append("</p>\n<div class=\"trio\">\n<figure><figcaption>Recharts</figcaption><div class=\"canvas\">")
            .Append(reference).Append("</div></figure>\n<figure><figcaption>Pictor, web</figcaption><div class=\"canvas\">").Append(pictor).Append("</div></figure>\n<figure><figcaption>Pictor, desktop</figcaption><div class=\"canvas\">");
        if (File.Exists(png))
        {
            html.Append("<img alt=\"").Append(example.Name).Append(" drawn by Pictor on the desktop\" width=\"").Append(example.Spec.Width.ToString(CultureInfo.InvariantCulture))
                .Append("\" height=\"").Append(example.Spec.Height.ToString(CultureInfo.InvariantCulture)).Append("\" src=\"data:image/png;base64,")
                .Append(Convert.ToBase64String(await File.ReadAllBytesAsync(png))).Append("\">");
        }

        html.Append("</div></figure>\n</div>\n</section>\n");
    }

    private static JsonElement Row(JsonElement file, string name)
    {
        if (file.ValueKind != JsonValueKind.Object || !(file.TryGetProperty("results", out var rows) || file.TryGetProperty("Results", out rows)))
        {
            return default;
        }

        return rows.EnumerateArray().FirstOrDefault(row => (row.TryGetProperty("name", out var n) || row.TryGetProperty("Name", out n)) && n.GetString() == name);
    }

    private static double Number(JsonElement row, string name) =>
        row.ValueKind == JsonValueKind.Object && row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : double.NaN;

    private static string Text(JsonElement file, string name) =>
        file.ValueKind == JsonValueKind.Object && file.TryGetProperty(name, out var value) ? value.ToString() : "?";

    private static string Percent(double part, double whole) => double.IsNaN(part) || double.IsNaN(whole) || whole == 0 ? "–" : (100 * part / whole).ToString("0.00", CultureInfo.InvariantCulture) + "%";

    private static string Us(double us) => double.IsNaN(us) ? "–" : us >= 1000 ? (us / 1000).ToString("0.00", CultureInfo.InvariantCulture) + "&nbsp;ms" : us.ToString("0.0", CultureInfo.InvariantCulture) + "&nbsp;µs";

    private static string Kb(int bytes) => (bytes / 1024.0).ToString("0.0", CultureInfo.InvariantCulture) + "&nbsp;KB";
}
