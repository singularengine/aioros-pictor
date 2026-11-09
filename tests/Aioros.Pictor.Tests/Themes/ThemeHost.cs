using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;
using Aioros.Pictor.Tests.Desktop;
using Aioros.Pictor.Theming;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Aioros.Pictor.Tests.Themes;

/// <summary>Charts in a window that includes <see cref="PictorTheme"/>, in a theme variant, as an app shows them.</summary>
internal static partial class ThemeHost
{
    /// <summary>A window holding a chart, with Pictor's theme in its styles and the variant asked for.</summary>
    public static Window Window(Control content, ThemeVariant variant, bool theme = true)
    {
        var window = new Window { Width = 800, Height = 600, RequestedThemeVariant = variant, Content = content };
        if (theme)
        {
            window.Styles.Add(new PictorTheme());
        }

        window.Show();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        return window;
    }

    /// <summary>The spec a chart gives Aioros at its size (what the web plan records).</summary>
    public static ChartSpec Spec(Chart chart) => SpecJson.Read(chart.WrittenSlot!.Argument);

    /// <summary>The markup of a reference example built from its XAML elements in a window of the given variant.</summary>
    public static async Task<string> MarkupAsync(DesktopSession desktop, string name, ThemeVariant variant)
    {
        var example = await Reference.ExampleAsync(name);
        var spec = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var window = Window(chart, variant);
                var written = Spec(chart);
                window.Close();
                return written;
            },
            TestContext.Current.CancellationToken);
        return Write(spec, example.Data);
    }

    public static string Write(ChartSpec spec, object? data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, data, static (_, _) => null);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>How the example's light markup compares with Recharts' render.</summary>
    public static async Task<IReadOnlyList<string>> LightDifferencesAsync(DesktopSession desktop, string name) =>
        MarkupComparison.Compare(await Reference.RechartsAsync(name), await MarkupAsync(desktop, name, ThemeVariant.Light)).Differences;

    /// <summary>
    /// What differs between the example's light and dark markup beyond its themed colors: the texts between colors must be
    /// the same (once the dark wrapper's custom properties are set aside), and every color must be the same or the dark
    /// value of the light one. Also counts the colors the theme changed, so a dark render equal to the light one is caught.
    /// </summary>
    public static async Task<(IReadOnlyList<string> Differences, int Themed)> DarkDifferencesAsync(DesktopSession desktop, string name)
    {
        var light = await MarkupAsync(desktop, name, ThemeVariant.Light);
        var dark = await MarkupAsync(desktop, name, ThemeVariant.Dark);
        var json = await File.ReadAllTextAsync(Path.Combine(Reference.Root, "tools", "recharts", "examples", name + ".json"), TestContext.Current.CancellationToken);
        var map = await desktop.Session.Dispatch(DarkOf, TestContext.Current.CancellationToken);
        return Substitution(map, light, dark, Colors().Matches(json).Select(match => match.Value).ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The differences of a dark markup from a light one beyond the theme's colors (<paramref name="map"/>, from <see cref="DarkOf"/>); a themed color may stay as it is only
    /// where the example sets it itself (<paramref name="explicitColors"/>).
    /// </summary>
    public static (IReadOnlyList<string> Differences, int Themed) Substitution(Dictionary<string, string> map, string light, string dark, IReadOnlySet<string> explicitColors)
    {
        var lightParts = Colors().Split(Fallbacks().Replace(light, "$1"));
        var darkParts = Colors().Split(Fallbacks().Replace(Variables().Replace(dark, string.Empty), "$1"));
        var differences = new List<string>();
        if (lightParts.Length != darkParts.Length)
        {
            differences.Add($"{lightParts.Length} parts in the light markup, {darkParts.Length} in the dark one");
            return (differences, 0);
        }

        var themed = 0;
        for (var i = 0; i < lightParts.Length && differences.Count < 20; i++)
        {
            var difference = i % 2 == 0 ? TextDifference(i, lightParts[i], darkParts[i]) : ColorDifference(map, explicitColors, lightParts[i], darkParts[i], ref themed);
            if (difference is not null)
            {
                differences.Add(difference);
            }
        }

        return (differences, themed);
    }

    private static string? TextDifference(int index, string light, string dark) =>
        light == dark ? null : $"text {index / 2}: '{Clip(light)}' became '{Clip(dark)}'";

    private static string? ColorDifference(Dictionary<string, string> map, IReadOnlySet<string> explicitColors, string light, string dark, ref int themed)
    {
        if (string.Equals(light, dark, StringComparison.OrdinalIgnoreCase))
        {
            return map.TryGetValue(light, out var themedValue) && !string.Equals(themedValue, light, StringComparison.OrdinalIgnoreCase) && !explicitColors.Contains(light)
                ? $"color {light} kept its light value, not {themedValue}"
                : null;
        }

        if (map.TryGetValue(light, out var expected) && expected.Split('|').Contains(dark, StringComparer.OrdinalIgnoreCase))
        {
            themed++;
            return null;
        }

        return $"color {light} became {dark}, not {expected ?? "itself"}";
    }

    /// <summary>Each light value of the theme and its dark value, from the keys of <see cref="PictorTheme"/>'s two dictionaries (Avalonia objects, so read inside the session's dispatch).</summary>
    public static Dictionary<string, string> DarkOf()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var light = PictorKeys.Dictionary(ChartTheme.Light);
        var dark = PictorKeys.Dictionary(ChartTheme.Dark);
        foreach (var key in light.Keys)
        {
            if (light[key] is string from && dark[key] is string to && Colors().Match(from).Value == from)
            {
                map[from] = map.TryGetValue(from, out var existing) && !existing.Split('|').Contains(to, StringComparer.OrdinalIgnoreCase) ? existing + "|" + to : existing ?? to;
            }
        }

        return map;
    }

    private static string Clip(string text) => text.Length > 80 ? text[..80] + "…" : text;

    [GeneratedRegex(@"(#[0-9a-fA-F]{3,8}\b|\bblack\b)")]
    private static partial Regex Colors();

    [GeneratedRegex(@"(var\(--pictor-[a-z-]+),[^)]*")]
    private static partial Regex Fallbacks();

    [GeneratedRegex(@"(?<=class=""recharts-wrapper[^""]*"" style=""[^""]*);--pictor-[a-z-]+:[^;""]*")]
    private static partial Regex Variables();
}
