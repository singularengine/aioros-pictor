using System.Buffers;
using System.Text;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;
using Aioros.Pictor.Tests.Desktop;
using Avalonia;

namespace Aioros.Pictor.Tests.Custom;

/// <summary>A custom content example: its chart built from the example's description, its templates set from <see cref="CustomTemplates"/>.</summary>
internal static class CustomReference
{
    /// <summary>The chart of an example, with its templates set, laid out at its size.</summary>
    public static Chart Chart(RechartsExample example, Action<Chart, CustomTemplates> templates)
    {
        var chart = SpecControls.Create(example);
        templates(chart, new CustomTemplates());
        chart.Measure(new Size(example.Spec.Width, example.Spec.Height));
        chart.Arrange(new Rect(0, 0, example.Spec.Width, example.Spec.Height));
        return chart;
    }

    /// <summary>The spec the chart's XAML elements and templates build, as Aioros receives it (kept in the folder <c>PICTOR_KEEP_SPEC</c> names, for the benchmarks).</summary>
    public static async Task<ChartSpec> SpecAsync(DesktopSession desktop, string name, Action<Chart, CustomTemplates> templates)
    {
        var example = await Reference.ExampleAsync(name);
        var json = await desktop.Session.Dispatch(() => Chart(example, templates).WrittenSlot!.Argument, TestContext.Current.CancellationToken);
        if (Environment.GetEnvironmentVariable("PICTOR_KEEP_SPEC") is { Length: > 0 } folder)
        {
            await File.WriteAllTextAsync(Path.Combine(folder, name + ".json"), json, TestContext.Current.CancellationToken);
        }

        return SpecJson.Read(json);
    }

    /// <summary>The markup the web writes from that spec.</summary>
    public static async Task<string> MarkupAsync(DesktopSession desktop, string name, Action<Chart, CustomTemplates> templates)
    {
        var example = await Reference.ExampleAsync(name);
        var spec = await SpecAsync(desktop, name, templates);
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, example.Data, static (_, _) => null);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>How that markup compares with Recharts' render of the example.</summary>
    public static async Task<MarkupVerdict> CompareAsync(DesktopSession desktop, string name, Action<Chart, CustomTemplates> templates) =>
        MarkupComparison.Compare(await Reference.RechartsAsync(name), await MarkupAsync(desktop, name, templates));

    /// <summary>
    /// The differences between Recharts' active shape after an example's interaction (the elements inside its
    /// <c>recharts-active-shape</c> layer) and the one Pictor's markup draws in the hover state under the same pointer.
    /// </summary>
    public static async Task<List<string>> ActiveDifferencesAsync(DesktopSession desktop, string name, Action<Chart, CustomTemplates> templates, int interaction, string layer = "recharts-active-shape", bool all = false)
    {
        var example = await Reference.ExampleAsync(name);
        var spec = await SpecAsync(desktop, name, templates);
        var hover = await Reference.HoverAsync(name, interaction);
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, example.Data, static (_, _) => null);
        var expected = Active(hover.Recharts[..(hover.Recharts.IndexOf("</svg>", StringComparison.Ordinal) + 6)], layer, all);
        var state = HoverComparison.Group(Encoding.UTF8.GetString(buffer.WrittenSpan), "pictor-tips", hover.State);
        var actual = layer == "recharts-active-dot" ? Unwrapped(state) : Active(state, layer, all);
        var differences = new List<string>();
        if (expected.Count == 0 || expected.Count != actual.Count)
        {
            differences.Add($"{expected.Count} elements in Recharts' active shape, {actual.Count} in Pictor's");
        }

        for (var i = 0; i < Math.Min(expected.Count, actual.Count); i++)
        {
            MarkupComparison.CompareElements(expected[i], actual[i], differences);
        }

        return differences;
    }

    /// <summary>
    /// The differences between Recharts' custom tooltip after an example's interaction (its wrapper's x and the elements
    /// of its content) and Pictor's hover state under the same pointer (its foreign object's x and the HTML inside).
    /// </summary>
    public static async Task<List<string>> TooltipDifferencesAsync(DesktopSession desktop, string name, Action<Chart, CustomTemplates> templates, int interaction)
    {
        var example = await Reference.ExampleAsync(name);
        var spec = await SpecAsync(desktop, name, templates);
        var hover = await Reference.HoverAsync(name, interaction);
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, example.Data, static (_, _) => null);
        var recharts = hover.Recharts[hover.Recharts.IndexOf("recharts-tooltip-wrapper", StringComparison.Ordinal)..];
        var pictor = HoverComparison.Group(Encoding.UTF8.GetString(buffer.WrittenSpan), "pictor-tips", hover.State);
        var differences = new List<string>();
        var expectedX = Number(System.Text.RegularExpressions.Regex.Match(recharts, @"translate\(([-\d.]+)px").Groups[1].Value);
        var placed = HoverComparison.PictorTip(Encoding.UTF8.GetString(buffer.WrittenSpan), hover.State, hover.Point);
        var actualX = placed.Visible ? placed.X : double.NaN;
        if (!(Math.Abs(expectedX - actualX) <= 0.5))
        {
            differences.Add($"tooltip x: {expectedX} in Recharts, {actualX} in Pictor");
        }

        var expected = MarkupComparison.Parse(recharts[(recharts.IndexOf('>') + 1)..recharts.IndexOf("</div></div>", StringComparison.Ordinal)]);
        var start = pictor.IndexOf("class=\"pictor-content\"", StringComparison.Ordinal);
        var actual = start < 0 ? [] : MarkupComparison.Parse(pictor[(pictor.IndexOf('>', start) + 1)..pictor.IndexOf("</div></foreignObject>", StringComparison.Ordinal)]);
        if (expected.Count != actual.Count)
        {
            differences.Add($"{expected.Count} elements in Recharts' tooltip, {actual.Count} in Pictor's");
        }

        foreach (var (left, right) in expected.Zip(actual))
        {
            if (left.Tag != right.Tag || left.Classes != right.Classes || left.Text != right.Text)
            {
                differences.Add($"<{left.Tag} class=\"{left.Classes}\">{left.Text} in Recharts, <{right.Tag} class=\"{right.Classes}\">{right.Text} in Pictor");
            }
        }

        return differences;
    }

    private static double Number(string text) => double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// The differences between the elements Recharts draws with a stroke after an example's interaction (an active
    /// shape outlined in that color, drawn where its item was) and the ones Pictor's hover state draws under the same pointer.
    /// </summary>
    public static async Task<List<string>> OutlinedDifferencesAsync(DesktopSession desktop, string name, Action<Chart, CustomTemplates> templates, int interaction, string stroke)
    {
        var example = await Reference.ExampleAsync(name);
        var spec = await SpecAsync(desktop, name, templates);
        var hover = await Reference.HoverAsync(name, interaction);
        var buffer = new ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, spec, example.Data, static (_, _) => null);
        bool Outlined(MarkupElement element) => element.Attributes.TryGetValue("stroke", out var value) && Hex(value) == Hex(stroke);
        var expected = MarkupComparison.Parse(hover.Recharts[..(hover.Recharts.IndexOf("</svg>", StringComparison.Ordinal) + 6)]).Where(Outlined).ToList();
        var markup = Encoding.UTF8.GetString(buffer.WrittenSpan);
        var state = HoverComparison.Group(markup, "pictor-tips", hover.State) + InPlace(markup, hover.State);
        var actual = MarkupComparison.Parse(state[(state.IndexOf('>', StringComparison.Ordinal) + 1)..]).Where(Outlined).ToList();
        var differences = new List<string>();
        if (expected.Count == 0 || expected.Count != actual.Count)
        {
            differences.Add($"{expected.Count} elements stroked {stroke} in Recharts' hover, {actual.Count} in Pictor's");
        }

        for (var i = 0; i < Math.Min(expected.Count, actual.Count); i++)
        {
            MarkupComparison.CompareElements(expected[i], actual[i], differences);
        }

        return differences;
    }

    /// <summary>The elements inside the first active layer (<c>recharts-active-shape</c>, <c>recharts-active-bar</c>) of a markup.</summary>
    private static string Hex(string color) => color is ['#', var r, var g, var b] ? $"#{r}{r}{g}{g}{b}{b}".ToLowerInvariant() : color.ToLowerInvariant();

    private static List<MarkupElement> Active(string markup, string layer, bool all = false)
    {
        var elements = new List<MarkupElement>();
        for (var start = markup.IndexOf("<g class=\"recharts-layer " + layer + "\"", StringComparison.Ordinal); start >= 0; start = all ? markup.IndexOf("<g class=\"recharts-layer " + layer + "\"", start + 1, StringComparison.Ordinal) : -1)
        {
            elements.AddRange(Inside(markup, start));
        }

        return elements;
    }

    /// <summary>What a hover state shows in place of an item's shape (the content of its <c>pictor-akK</c> group), or an empty text.</summary>
    public static string InPlace(string markup, int state)
    {
        var open = "<g class=\"pictor-ak" + state + "\">";
        var start = markup.IndexOf(open, StringComparison.Ordinal);
        return start < 0 ? string.Empty : markup[(start + open.Length)..(End(markup, start) - 4)];
    }

    /// <summary>The markup as the browser shows a hover state: the shape before its in-place group (<c>pictor-akK</c>) left out and the group's content in its place.</summary>
    public static string Applied(string markup, int state)
    {
        var open = "<g class=\"pictor-ak" + state + "\">";
        var start = markup.IndexOf(open, StringComparison.Ordinal);
        if (start < 0)
        {
            return markup;
        }

        var end = End(markup, start);
        var before = markup[..start];
        var shape = before.EndsWith("</g>", StringComparison.Ordinal) ? Opening(before) : before.LastIndexOf('<');
        return before[..shape] + markup[(start + open.Length)..(end - 4)] + markup[end..];
    }

    /// <summary>Where the group the text before an index ends with opens.</summary>
    private static int Opening(string before)
    {
        var depth = 0;
        for (var i = before.Length - 4; i >= 0; i--)
        {
            depth += string.CompareOrdinal(before, i, "</g>", 0, 4) == 0 ? 1 : string.CompareOrdinal(before, i, "<g", 0, 2) == 0 && before[i + 2] is ' ' or '>' ? -1 : 0;
            if (depth == 0)
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>Where the group that opens at an index of a markup ends (past its closing tag).</summary>
    private static int End(string markup, int start)
    {
        var depth = 0;
        for (var i = start; i < markup.Length; i++)
        {
            depth += string.CompareOrdinal(markup, i, "<g", 0, 2) == 0 && markup[i + 2] is ' ' or '>' ? 1 : string.CompareOrdinal(markup, i, "</g>", 0, 4) == 0 ? -1 : 0;
            if (depth == 0)
            {
                return i + 4;
            }
        }

        return markup.Length;
    }

    /// <summary>The elements of a hover state's tip group besides its tooltip: its active dots, whose layers the hover markup leaves out (decision "Smaller hover markup").</summary>
    private static List<MarkupElement> Unwrapped(string state)
    {
        var inner = state[(state.IndexOf('>', StringComparison.Ordinal) + 1)..];
        var foreign = inner.IndexOf("<foreignObject", StringComparison.Ordinal);
        return MarkupComparison.Parse(foreign < 0 ? inner : inner[..foreign]);
    }

    /// <summary>The elements inside the group that starts at an index of a markup.</summary>
    private static IEnumerable<MarkupElement> Inside(string markup, int start)
    {
        var depth = 0;
        for (var i = start; i < markup.Length; i++)
        {
            depth += string.CompareOrdinal(markup, i, "<g", 0, 2) == 0 && markup[i + 2] is ' ' or '>' ? 1 : string.CompareOrdinal(markup, i, "</g>", 0, 4) == 0 ? -1 : 0;
            if (depth == 0)
            {
                return MarkupComparison.Parse(markup[start..(i + 4)]).Skip(1);
            }
        }

        return [];
    }

    /// <summary>How the desktop chart's pixels compare with Recharts' screenshot.</summary>
    public static async Task<DesktopResult> MeasureAsync(DesktopSession desktop, string name, Action<Chart, CustomTemplates> templates)
    {
        var example = await Reference.ExampleAsync(name);
        var output = Path.Combine(Path.GetTempPath(), "pictor-custom-" + Guid.NewGuid().ToString("N") + ".png");
        var result = await desktop.Session.Dispatch(
            () => DesktopRender.Compare(example.Name, Chart(example, templates), ReferenceFiles.Of(Reference.Root, name + ".png"), output),
            TestContext.Current.CancellationToken);
        File.Delete(output);
        return result;
    }
}
