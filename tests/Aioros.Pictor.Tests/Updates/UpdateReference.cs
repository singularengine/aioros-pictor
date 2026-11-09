using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aioros.Pictor.Data;
using Aioros.Pictor.Demo;
using Aioros.Pictor.Motion;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Updates;

/// <summary>
/// Recharts' update animation of a case recorded by <c>tools/recharts/motion.mjs</c>, compared frame by frame with
/// Pictor's: every frame after the update (rendered at the clock time of the first frame) is compared with Pictor's
/// update at the same time after it, Recharts' begin delay and its five frames of start included.
/// </summary>
internal static partial class UpdateReference
{
    private static readonly string[] Shapes = ["recharts-curve", "recharts-rectangle", "recharts-symbols", "recharts-sector", "recharts-polygon", "recharts-trapezoid"];

    /// <summary>The differences between Recharts' frames and Pictor's for a case, and how many frames moved.</summary>
    public static async Task<(List<string> Differences, int Moving)> CompareAsync(string name, double until = double.PositiveInfinity)
    {
        await using var stream = File.OpenRead(ReferenceFiles.Of(Reference.Root, Path.Combine("motion", name + ".json")));
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var root = document.RootElement;
        var from = Plot(root.GetProperty("from"));
        var to = Plot(root.GetProperty("to"));
        var morph = root.TryGetProperty("enter", out var enter) && enter.GetBoolean() ? PolarMorphs.Enter((Layout.PolarLayout)to) : Morph.Plan(from, to);
        var frames = root.GetProperty("frames").EnumerateArray().Select(frame => (Time: frame.GetProperty("time").GetDouble(), Shapes: frame.GetProperty("shapes").GetString()!)).ToList();
        var differences = new List<string>();
        if (morph is null)
        {
            return (["Pictor plans no update animation"], 0);
        }

        var geometry = frames.ConvertAll(frame => string.Join(' ', PathData().Matches(frame.Shapes).Select(match => match.Value)));
        var start = Enumerable.Range(2, frames.Count - 2).FirstOrDefault(k => geometry[k] != geometry[k - 1]) - 1;
        if (start < 1)
        {
            return (["Recharts' frames never move"], 0);
        }

        var moving = 0;
        for (var i = 1; i < frames.Count; i++)
        {
            var elapsed = frames[i].Time - frames[0].Time;
            if (elapsed > until)
            {
                break;
            }

            var found = Frame(MarkupComparison.Parse(frames[i].Shapes), morph, to, elapsed);
            differences.AddRange(found.Take(3).Select(difference => $"frame at {elapsed} ms: {difference}"));
            moving += geometry[i] != geometry[i - 1] ? 1 : 0;
            if (differences.Count > 12)
            {
                break;
            }
        }

        return (differences, moving);
    }

    /// <summary>The differences between a frame of Recharts and Pictor's at the same time.</summary>
    private static List<string> Frame(List<MarkupElement> expected, Morph morph, IChartPlot to, double elapsed)
    {
        var actual = Elements(Surface(Write(elapsed >= morph.End ? to : morph.Frame(elapsed))));
        if (elapsed >= morph.End && actual.Count != expected.Count)
        {
            actual = Elements(Write(to));
        }

        var found = new List<string>();
        if (expected.Count != actual.Count)
        {
            found.Add($"{expected.Count} shapes in Recharts' frame, {actual.Count} in Pictor's");
        }

        for (var k = 0; k < Math.Min(expected.Count, actual.Count); k++)
        {
            MarkupComparison.CompareElements(expected[k], actual[k], found);
        }

        return found;
    }

    /// <summary>
    /// The series' shapes of Pictor's markup. Recharts renders what waits for the end (dots, labels) two frames after
    /// its last frame (react-smooth calls <c>onAnimationEnd</c> through two more animation frames), so the frames at
    /// the end are compared with and without them.
    /// </summary>
    private static List<MarkupElement> Elements(string markup) =>
        [.. MarkupComparison.Parse(MotionClass().Replace(markup, string.Empty))
            .Where(element => element.Tag == "path" && Shapes.Any(shape => element.Classes.Split(' ').Contains(shape)))
            .Select(element => element with { Attributes = element.Attributes.Where(pair => pair.Key is "class" or "d" or "transform").ToDictionary(StringComparer.Ordinal) })];

    private static IChartPlot Plot(JsonElement description)
    {
        var example = RechartsExamples.Read(description);
        return Plots.Compute(ExampleAnimations.Animated(example.Spec), ChartData.From(example.Data, static (_, _) => null));
    }

    private static string Write(IChartPlot plot)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var canvas = new FrameCanvas(new SvgCanvas(buffer));
        Plots.Paint(ref canvas, plot, ChartPainter.NoTips);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>The chart's surface without the parts Recharts renders once its animation ends (Pictor's <c>pictor-late</c> groups).</summary>
    private static string Surface(string markup)
    {
        var start = markup.IndexOf("<svg", StringComparison.Ordinal);
        var svg = markup[start..(markup.IndexOf("</svg>", start, StringComparison.Ordinal) + 6)];
        var output = new StringBuilder();
        var (skipping, position) = (0, 0);
        foreach (Match tag in GroupTag().Matches(svg))
        {
            output.Append(skipping == 0 ? svg[position..tag.Index] : string.Empty);
            position = tag.Index + tag.Length;
            skipping += Depth(tag.Value, skipping);
            output.Append(skipping == 0 && !tag.Value.Contains("pictor-late", StringComparison.Ordinal) ? tag.Value : string.Empty);
        }

        return output.Append(svg[position..]).ToString();
    }

    /// <summary>How a group tag changes the depth of the late group being skipped: an opening one inside it or a late one opens, a closing one inside it closes.</summary>
    private static int Depth(string tag, int skipping) =>
        tag.EndsWith("/>", StringComparison.Ordinal) ? 0
        : tag.StartsWith("</", StringComparison.Ordinal) ? (skipping > 0 ? -1 : 0)
        : skipping > 0 || tag.Contains("pictor-late", StringComparison.Ordinal) ? 1 : 0;

    [GeneratedRegex(" d=\"[^\"]*\"")]
    private static partial Regex PathData();

    [GeneratedRegex(" pictor-[\\w-]+")]
    private static partial Regex MotionClass();

    [GeneratedRegex("</?g\\b[^>]*>")]
    private static partial Regex GroupTag();
}
