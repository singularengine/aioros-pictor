using System.Globalization;
using System.Text.RegularExpressions;

namespace Aioros.Pictor.Demo;

/// <summary>An element of a chart's markup: its tag, attributes and text.</summary>
internal sealed record MarkupElement(string Tag, Dictionary<string, string> Attributes, string Text)
{
    public string Classes => Attributes.TryGetValue("class", out var classes) ? classes : string.Empty;
}

/// <summary>The deliberate divergences from Recharts' markup an example declares.</summary>
[Flags]
internal enum MarkupDivergence
{
    /// <summary>None: the markup matches Recharts' as it is.</summary>
    None = 0,

    /// <summary>
    /// A responsive web chart's surface fills its container (<c>width="100%" height="100%"</c>) and scales its view box,
    /// where Recharts' <c>ResponsiveContainer</c> writes the width it measured ("A responsive web chart scales its view box").
    /// </summary>
    ResponsiveSurface = 1,
}

/// <summary>What a comparison found: the elements compared and every difference.</summary>
internal sealed record MarkupVerdict(int Compared, int Recharts, int Pictor, IReadOnlyList<string> Differences)
{
    public bool Matches => Differences.Count == 0;
}

/// <summary>
/// Compares Pictor's markup of a chart with Recharts' own, element by element in document order: the same elements
/// with the same classes, and the same geometry (coordinates, path data after transforms) within 0.01 px, the same
/// colors, anchors, dashes and texts. What Recharts copies onto elements without effect (a line's <c>width</c>), its
/// hidden tooltip and Pictor's hover layers are left out; attributes equal to the SVG default are equal to missing ones,
/// and paints are compared on what draws, after SVG's inheritance (a stroke on a group or on its lines is the same).
/// </summary>
internal static partial class MarkupComparison
{
    private const double Tolerance = 0.01;

    private static readonly HashSet<string> Filled = new(StringComparer.Ordinal) { "path", "rect", "circle", "ellipse", "polygon", "polyline", "text", "tspan", "textpath" };
    private static readonly HashSet<string> Stroked = new(StringComparer.Ordinal) { "path", "rect", "circle", "ellipse", "polygon", "polyline", "line", "text", "tspan", "textpath" };
    private static readonly HashSet<string> Anchored = new(StringComparer.Ordinal) { "text", "tspan", "textpath" };
    private static readonly HashSet<string> Void = new(StringComparer.Ordinal) { "line", "path", "circle", "rect", "input" };
    private static readonly HashSet<string> Skipped = new(StringComparer.Ordinal) { "title", "desc", "defs", "clippath", "style" };

    public static MarkupVerdict Compare(string recharts, string pictor, MarkupDivergence divergences = MarkupDivergence.None)
    {
        if (recharts.Contains("recharts-zIndex-layer", StringComparison.Ordinal))
        {
            return CompareDrawn(Parse(recharts), Parse(pictor));
        }

        var expected = Parse(recharts);
        var actual = Parse(pictor);
        var differences = new List<string>();
        var count = Math.Min(expected.Count, actual.Count);
        var surface = expected.FindIndex(element => element.Tag == "svg");
        for (var i = 0; i < count; i++)
        {
            CompareElements(expected[i], actual[i], differences, i == surface ? divergences : MarkupDivergence.None);
            if (differences.Count > 50)
            {
                break;
            }
        }

        if (expected.Count != actual.Count)
        {
            differences.Add($"{expected.Count} elements in Recharts' markup, {actual.Count} in Pictor's; first extra: {Describe(expected.Count > actual.Count ? expected[count] : actual[count])}");
        }

        return new MarkupVerdict(count, expected.Count, actual.Count, differences);
    }

    /// <summary>
    /// Compares Pictor's markup with Recharts 3's render of an example Pictor follows Recharts 3 in: the drawn elements, every element but the groups Recharts 3 rearranges into its
    /// z-index layers, by tag and class (an <c>undefined</c> class token, the tooltip wrapper's side classes and the polar
    /// axes' renamed classes taken as Recharts 2 writes them), each class' elements in document order
    /// with the same geometry, text and presentation as <see cref="Compare"/> checks.
    /// </summary>
    /// <summary>The polar axes' classes Recharts 3 renamed, by their Recharts 2 names, which Pictor writes.</summary>
    private static readonly Dictionary<string, string> Renamed = new(StringComparer.Ordinal)
    {
        ["recharts-polar-angle-axis-line"] = "angleAxis",
        ["recharts-polar-angle-axis-tick-line"] = "angleAxis",
        ["recharts-polar-radius-axis-line"] = "radiusAxis",
        ["recharts-polar-radius-axis-tick-line"] = "radiusAxis",
    };

    private static MarkupVerdict CompareDrawn(List<MarkupElement> recharts, List<MarkupElement> pictor)
    {
        static MarkupElement Normalized(MarkupElement element)
        {
            var classes = string.Join(' ', element.Classes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(static token => Renamed.GetValueOrDefault(token, token)).Where(static token => token.Length > 0 && token != "undefined" && !token.StartsWith("recharts-tooltip-wrapper-", StringComparison.Ordinal)));
            return element.Attributes.ContainsKey("class") ? element with { Attributes = new Dictionary<string, string>(element.Attributes, StringComparer.Ordinal) { ["class"] = classes } } : element;
        }

        static string Key(MarkupElement element) => element.Tag + "." + element.Classes + (element.Tag == "svg" ? "@" + element.Attributes.GetValueOrDefault("width") + "x" + element.Attributes.GetValueOrDefault("height") : string.Empty);
        static ILookup<string, MarkupElement> Drawn(List<MarkupElement> elements) => elements.Where(static element => element.Tag != "g").Select(Normalized).ToLookup(Key, StringComparer.Ordinal);
        var expected = Drawn(recharts);
        var actual = Drawn(pictor);
        var differences = new List<string>();
        var compared = 0;
        foreach (var key in expected.Select(static group => group.Key).Union(actual.Select(static group => group.Key), StringComparer.Ordinal))
        {
            var (theirs, ours) = (expected[key].ToList(), actual[key].ToList());
            if (theirs.Count != ours.Count)
            {
                differences.Add($"{theirs.Count} {key} in Recharts 3's markup, {ours.Count} in Pictor's");
            }

            for (var i = 0; i < Math.Min(theirs.Count, ours.Count) && differences.Count <= 50; i++)
            {
                CompareElements(theirs[i], ours[i], differences, MarkupDivergence.None);
                compared++;
            }
        }

        return new MarkupVerdict(compared, recharts.Count, pictor.Count, differences);
    }

    public static List<MarkupElement> Parse(string markup)
    {
        var state = new ParseState();
        var position = 0;
        foreach (Match tag in Tag().Matches(markup))
        {
            state.AddText(markup[position..tag.Index]);
            position = tag.Index + tag.Length;
            var name = tag.Groups[2].Value.ToLowerInvariant();
            if (tag.Groups[1].Value == "/")
            {
                state.Close(name);
            }
            else
            {
                state.Open(name, Attributes(tag.Groups[3].Value), tag.Groups[4].Value == "/");
            }
        }

        Resolve(state);
        return state.Elements;
    }

    /// <summary>Replaces each reference to a clip or a gradient with what it defines, so two renders compare by what they draw.</summary>
    private static void Resolve(ParseState state)
    {
        foreach (var attributes in state.Elements.Select(element => element.Attributes))
        {
            if (attributes.TryGetValue("clip-path", out var reference) && ClipReference().Match(reference) is { Success: true } match)
            {
                attributes["clip"] = state.Clips.TryGetValue(match.Groups[1].Value, out var rect) ? rect : "-1";
            }

            if ((attributes.GetValueOrDefault("xlink:href") ?? attributes.GetValueOrDefault("href")) is ['#', .. var path])
            {
                attributes["path"] = state.Paths.GetValueOrDefault(path) ?? "missing path";
            }

            ResolveGradient(state, attributes, "fill");
            ResolveGradient(state, attributes, "stroke");
        }
    }

    private static void ResolveGradient(ParseState state, Dictionary<string, string> attributes, string paint)
    {
        if (attributes.TryGetValue(paint, out var color) && GradientReference().Match(color) is { Success: true } gradient)
        {
            attributes[paint] = state.Gradients.TryGetValue(gradient.Groups[1].Value, out var definition) ? definition : "missing gradient";
        }
    }

    private sealed class ParseState
    {
        private static readonly string[] Inherited = ["fill", "stroke", "stroke-width", "stroke-dasharray", "stroke-opacity", "fill-opacity", "stroke-linecap", "stroke-linejoin", "stroke-miterlimit", "font-weight", "font-style", "text-anchor"];

        private readonly Stack<bool> _hidden = new();
        private readonly Stack<Dictionary<string, string>> _inherited = new();
        private int _skip;
        private string? _clip;
        private string? _gradient;

        public List<MarkupElement> Elements { get; } = [];

        /// <summary>The rectangle of every clip path, by id, as "x y width height".</summary>
        public Dictionary<string, string> Clips { get; } = new(StringComparer.Ordinal);

        /// <summary>Every linear gradient, by id, as its line and stops with numbers normalized, so two gradients compare by what they paint.</summary>
        public Dictionary<string, string> Gradients { get; } = new(StringComparer.Ordinal);

        /// <summary>The data of every path with an id (the paths a <c>textPath</c> runs along), by id.</summary>
        public Dictionary<string, string> Paths { get; } = new(StringComparer.Ordinal);

        private void Gradient(string name, Dictionary<string, string> attributes)
        {
            if (name == "lineargradient" && attributes.GetValueOrDefault("id") is { } id)
            {
                _gradient = id;
                Gradients[id] = "gradient " + string.Join(' ', ((string[])["x1", "y1", "x2", "y2"]).Select(key => Normalized(attributes.GetValueOrDefault(key) ?? (key == "x2" ? "100%" : "0"))))
                    + (attributes.GetValueOrDefault("gradientUnits") is { } units ? " " + units : string.Empty);
            }
            else if (name == "stop" && _gradient is { } owner)
            {
                Gradients[owner] += $" | {Normalized(attributes.GetValueOrDefault("offset") ?? "0")} {attributes.GetValueOrDefault("stop-color")} {Normalized(attributes.GetValueOrDefault("stop-opacity") ?? "1")}";
            }
        }

        private static string Normalized(string length) =>
            length.EndsWith('%') && double.TryParse(length[..^1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var percent)
                ? (percent / 100).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)
                : double.TryParse(length, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) : length;

        public void AddText(string text)
        {
            if (_skip == 0 && Elements.Count > 0 && text.Trim().Length > 0)
            {
                Elements[^1] = Elements[^1] with { Text = Elements[^1].Text + text.Trim() };
            }
        }

        public void Close(string name)
        {
            if (!Void.Contains(name) && _hidden.Count > 0)
            {
                _inherited.Pop();
                if (_hidden.Pop())
                {
                    _skip--;
                }
            }
        }

        /// <summary>The paints an element's <c>style</c> sets, which win over its attributes as CSS does (Recharts' treemap content and Pictor's cursors write them so).</summary>
        private static void Styled(Dictionary<string, string> attributes)
        {
            if (!attributes.TryGetValue("style", out var style))
            {
                return;
            }

            foreach (var declaration in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = declaration.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && (Inherited.Contains(parts[0]) || parts[0] == "opacity"))
                {
                    attributes[parts[0]] = parts[1];
                }
            }
        }

        private void NamedPath(string name, Dictionary<string, string> attributes)
        {
            if (name == "path" && attributes.GetValueOrDefault("id") is { } id)
            {
                Paths[id] = attributes.GetValueOrDefault("d") ?? string.Empty;
            }
        }

        private void Clip(string name, Dictionary<string, string> attributes)
        {
            if (name == "clippath")
            {
                _clip = attributes.GetValueOrDefault("id");
            }
            else if (name == "rect" && _clip is { } id)
            {
                Clips[id] = string.Join(' ', ((string[])["x", "y", "width", "height"]).Select(key => attributes.GetValueOrDefault(key) ?? "0"));
                _clip = null;
            }
        }

        public void Open(string name, Dictionary<string, string> attributes, bool selfClosing)
        {
            var classes = attributes.TryGetValue("class", out var value) ? value : string.Empty;
            Styled(attributes);
            Clip(name, attributes);
            Gradient(name, attributes);
            NamedPath(name, attributes);

            var hides = classes.StartsWith("pictor-hk", StringComparison.Ordinal) || classes.StartsWith("pictor-ck", StringComparison.Ordinal) || classes.StartsWith("pictor-tk", StringComparison.Ordinal) || classes.StartsWith("pictor-ak", StringComparison.Ordinal) || classes.StartsWith("pictor-hover", StringComparison.Ordinal) || classes is "pictor-keys" or "pictor-cursors" or "pictor-brush-fields" or "pictor-cmds" or "pictor-zoom" || classes == "pictor-off" || classes.EndsWith(" pictor-off", StringComparison.Ordinal) || classes.Contains("recharts-tooltip-wrapper", StringComparison.Ordinal) || Skipped.Contains(name);
            var parent = _inherited.Count > 0 ? _inherited.Peek() : [];
            foreach (var (key, inherited) in parent.Where(pair => !attributes.ContainsKey(pair.Key)))
            {
                attributes[key] = inherited;
            }

            if (_skip == 0 && !hides)
            {
                Elements.Add(new MarkupElement(name, attributes, string.Empty));
            }

            if (!selfClosing && !Void.Contains(name))
            {
                _hidden.Push(hides);
                _inherited.Push(Inherited.Where(attributes.ContainsKey).ToDictionary(key => key, key => attributes[key], StringComparer.Ordinal));
                _skip += hides ? 1 : 0;
            }
        }
    }

    private static Dictionary<string, string> Attributes(string text)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var groups in Attribute().Matches(text).Select(attribute => attribute.Groups))
        {
            attributes[groups[1].Value] = System.Net.WebUtility.HtmlDecode(groups[2].Value);
        }

        return attributes;
    }

    /// <summary>The SVG presentation attributes Recharts spread onto the element (inherited ones resolved), each equal in Pictor's; one Recharts leaves out must be left out (or at its default) in Pictor's too.</summary>
    private static void ComparePresentation(MarkupElement expected, MarkupElement actual, List<string> differences)
    {
        string[] names = expected.Tag switch
        {
            "g" => ["opacity"],
            "text" => ["opacity", "font-weight", "font-style"],
            "tspan" => [],
            _ => ["opacity", "stroke-opacity", "stroke-linecap", "stroke-linejoin", "stroke-miterlimit"],
        };
        foreach (var name in names)
        {
            var left = expected.Attributes.GetValueOrDefault(name) ?? Default(name);
            var right = actual.Attributes.GetValueOrDefault(name) ?? Default(name);
            var same = double.TryParse(left, NumberStyles.Float, CultureInfo.InvariantCulture, out var a) && double.TryParse(right, NumberStyles.Float, CultureInfo.InvariantCulture, out var b) ? Math.Abs(a - b) < 1e-9 : string.Equals(left, right, StringComparison.Ordinal);
            if (!same)
            {
                differences.Add($"{Describe(expected)} {name}: {left} in Recharts, {right} in Pictor");
            }
        }

        static string Default(string name) => name switch
        {
            "opacity" or "stroke-opacity" => "1",
            "stroke-linecap" => "butt",
            "stroke-linejoin" => "miter",
            "stroke-miterlimit" => "4",
            "font-weight" => "normal",
            _ => "normal",
        };
    }

    public static void CompareElements(MarkupElement expected, MarkupElement actual, List<string> differences, MarkupDivergence divergences = MarkupDivergence.None)
    {
        if (expected.Tag != actual.Tag || expected.Classes != actual.Classes)
        {
            differences.Add($"{Describe(expected)} in Recharts, {Describe(actual)} in Pictor");
            return;
        }

        string[] geometry = expected.Tag switch
        {
            "line" => ["x1", "y1", "x2", "y2"],
            "circle" => ["cx", "cy", "r"],
            "rect" => ["x", "y", "width", "height"],
            "text" or "tspan" => ["x", "y", "dy"],
            _ => [],
        };
        foreach (var name in geometry)
        {
            CompareNumbers(expected, actual, name, Numbers(expected, name), Numbers(actual, name), differences);
        }

        if (expected.Tag == "text")
        {
            CompareText(expected, actual, differences);
        }

        if (expected.Tag == "path")
        {
            CompareNumbers(expected, actual, "d", PathNumbers(expected), PathNumbers(actual), differences);
        }

        CompareNumbers(expected, actual, "clip", Numbers(expected, "clip"), Numbers(actual, "clip"), differences);

        ComparePresentation(expected, actual, differences);
        CompareShape(expected, actual, differences, divergences);
        if (expected.Tag is "g" or "svg")
        {
            return;
        }

        ComparePaints(expected, actual, differences);

        if ((Fills(expected) && Math.Abs(Alpha(expected, "fill") - Alpha(actual, "fill")) > Tolerance) || Math.Abs(Opacity(expected, "stroke-width") - Opacity(actual, "stroke-width")) > Tolerance)
        {
            differences.Add($"{Describe(expected)} opacity or stroke width differ");
        }

        if (expected.Text != actual.Text)
        {
            differences.Add($"{Describe(expected)} text: \"{expected.Text}\" in Recharts, \"{actual.Text}\" in Pictor");
        }
    }

    /// <summary>
    /// The paints that draw: fill, stroke, dashes and anchor, each the attribute after SVG's inheritance or, when
    /// neither side's element nor its ancestors carry it, SVG's initial value (<c>black</c>, <c>none</c>, <c>none</c>,
    /// <c>start</c>), so a paint left out on one side counts unless the other side writes the default.
    /// </summary>
    private static void ComparePaints(MarkupElement expected, MarkupElement actual, List<string> differences)
    {
        if (Fills(expected))
        {
            ComparePaint(expected, actual, "fill", "black", differences);
        }

        if (!Stroked.Contains(expected.Tag))
        {
            return;
        }

        var stroked = ComparePaint(expected, actual, "stroke", "none", differences);
        if (stroked && expected.Tag is not ("text" or "tspan" or "textpath") && !Dashes(expected).SequenceEqual(Dashes(actual)))
        {
            differences.Add($"{Describe(expected)} stroke-dasharray: {expected.Attributes.GetValueOrDefault("stroke-dasharray") ?? "none"} in Recharts, {actual.Attributes.GetValueOrDefault("stroke-dasharray") ?? "none"} in Pictor");
        }

        if (Anchored.Contains(expected.Tag) && (expected.Attributes.GetValueOrDefault("text-anchor") ?? "start") is var left && (actual.Attributes.GetValueOrDefault("text-anchor") ?? "start") is var right && left != right)
        {
            differences.Add($"{Describe(expected)} text-anchor: {left} in Recharts, {right} in Pictor");
        }
    }

    /// <summary>Compares one paint with its initial value standing for a missing one; whether either side draws with it.</summary>
    private static bool ComparePaint(MarkupElement expected, MarkupElement actual, string name, string initial, List<string> differences)
    {
        var left = expected.Attributes.GetValueOrDefault(name) ?? initial;
        var right = actual.Attributes.GetValueOrDefault(name) ?? initial;
        var leftOpacity = Opacity(expected, name + "-opacity");
        var rightOpacity = Opacity(actual, name + "-opacity");
        if (left != right && !SameColor(left, right, leftOpacity, rightOpacity))
        {
            differences.Add($"{Describe(expected)} {name}: {left} in Recharts, {right} in Pictor");
        }

        return Color(left) is not { } a || a.A * leftOpacity > 0 || Color(right) is not { } b || b.A * rightOpacity > 0;
    }

    private static bool Fills(MarkupElement element) => Filled.Contains(element.Tag) && !(element.Tag == "path" && Flat(element));

    /// <summary>Whether a path encloses no area, so its fill draws nothing: a cursor's line or cross, a sector whose radii are equal (Recharts' radial cursor).</summary>
    private static bool Flat(MarkupElement element)
    {
        var path = Path(element);
        path.Close();
        return !path.Curved && Math.Abs(path.Area) < Tolerance;
    }

    /// <summary>A dash pattern's lengths, none for a solid stroke (<c>none</c>, or only zeros).</summary>
    private static double[] Dashes(MarkupElement element)
    {
        var numbers = Numbers(element, "stroke-dasharray");
        return numbers.All(number => number == 0) ? [] : [.. numbers.Select(number => Math.Round(number, 2))];
    }

    /// <summary>
    /// What places and sizes an element beyond its own coordinates: a group's transform (no transform and
    /// <c>translate(0, 0)</c> alike), an svg's width, height and view box, a polygon's points and the path a
    /// <c>textPath</c> runs along.
    /// </summary>
    private static void CompareShape(MarkupElement expected, MarkupElement actual, List<string> differences, MarkupDivergence divergences)
    {
        switch (expected.Tag)
        {
            case "svg" when divergences.HasFlag(MarkupDivergence.ResponsiveSurface):
                CompareNumbers(expected, actual, "viewBox", Numbers(expected, "viewBox"), Numbers(actual, "viewBox"), differences);
                if (actual.Attributes.GetValueOrDefault("width") != "100%" || actual.Attributes.GetValueOrDefault("height") != "100%")
                {
                    differences.Add($"{Describe(expected)} a responsive surface is 100% wide and high in Pictor, not {actual.Attributes.GetValueOrDefault("width")} by {actual.Attributes.GetValueOrDefault("height")}");
                }

                break;
            case "g":
                CompareNumbers(expected, actual, "transform", Moved(expected), Moved(actual), differences);
                break;
            case "svg":
                foreach (var name in (string[])["width", "height", "viewBox"])
                {
                    CompareNumbers(expected, actual, name, Numbers(expected, name), Numbers(actual, name), differences);
                }

                break;
            case "polygon" or "polyline":
                CompareNumbers(expected, actual, "points", Numbers(expected, "points"), Numbers(actual, "points"), differences);
                break;
            case "textpath":
                CompareNumbers(expected, actual, "path", PathNumbers(expected, "path"), PathNumbers(actual, "path"), differences);
                CompareNumbers(expected, actual, "startOffset", Numbers(expected, "startOffset"), Numbers(actual, "startOffset"), differences);
                break;
        }
    }

    private static double[] Moved(MarkupElement element)
    {
        var numbers = Numbers(element, "transform");
        return numbers.All(number => number == 0) ? [] : numbers;
    }

    /// <summary>A text's rotation and its own font size, which move and size it.</summary>
    private static void CompareText(MarkupElement expected, MarkupElement actual, List<string> differences)
    {
        CompareNumbers(expected, actual, "transform", Rotation(expected), Rotation(actual), differences);
        if (expected.Attributes.GetValueOrDefault("font-size") != actual.Attributes.GetValueOrDefault("font-size"))
        {
            differences.Add($"{Describe(expected)} font-size: {expected.Attributes.GetValueOrDefault("font-size")} in Recharts, {actual.Attributes.GetValueOrDefault("font-size")} in Pictor");
        }
    }

    /// <summary>
    /// A text's transform numbers, none for a rotation by 0 degrees (no effect, which Recharts writes and Pictor leaves
    /// out), a rotation around the origin as its angle alone (<c>rotate(-35, 0, 0)</c> is <c>rotate(-35)</c>).
    /// </summary>
    private static double[] Rotation(MarkupElement element)
    {
        var numbers = Numbers(element, "transform");
        var rotation = numbers.Length > 0 && element.Attributes["transform"].TrimStart().StartsWith("rotate", StringComparison.Ordinal);
        return rotation && numbers[0] == 0 ? [] : rotation && numbers.Length == 3 && numbers[1] == 0 && numbers[2] == 0 ? [numbers[0]] : numbers;
    }

    /// <summary>
    /// Whether two paints are the same color however they are written (<c>red</c>, <c>#ff0000</c>, <c>#f00</c>,
    /// <c>rgb(255, 0, 0)</c>), their translucency (a color's alpha times the paint's opacity) aside; any two fully
    /// transparent paints are the same.
    /// </summary>
    private static bool SameColor(string left, string right, double leftOpacity, double rightOpacity) =>
        Color(left) is { } a && Color(right) is { } b && (a.A * leftOpacity == 0 && b.A * rightOpacity == 0 || (a.R == b.R && a.G == b.G && a.B == b.B));

    /// <summary>What a paint lets through: its color's alpha times its opacity attribute (1 when it has neither).</summary>
    private static double Alpha(MarkupElement element, string paint) =>
        (element.Attributes.TryGetValue(paint, out var value) && Color(value) is { } color ? color.A / 255.0 : 1) * Opacity(element, paint + "-opacity");

    /// <summary>A CSS color: a name, a hex color, or <c>rgb()</c> and <c>rgba()</c>.</summary>
    private static global::Avalonia.Media.Color? Color(string text)
    {
        var trimmed = text.Trim();
        if (trimmed is "none" or "transparent")
        {
            return global::Avalonia.Media.Colors.Transparent;
        }

        if (trimmed.StartsWith("rgb", StringComparison.Ordinal) && trimmed.IndexOf('(') is var open and > 0 && trimmed.EndsWith(')'))
        {
            var parts = trimmed[(open + 1)..^1].Split(',', StringSplitOptions.TrimEntries);
            var alpha = parts.Length > 3 ? double.Parse(parts[3], CultureInfo.InvariantCulture) : 1;
            return global::Avalonia.Media.Color.FromArgb((byte)Math.Round(alpha * 255), byte.Parse(parts[0], CultureInfo.InvariantCulture), byte.Parse(parts[1], CultureInfo.InvariantCulture), byte.Parse(parts[2], CultureInfo.InvariantCulture));
        }

        return trimmed.Length == 9 && trimmed[0] == '#' && uint.TryParse(trimmed[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgba)
            ? global::Avalonia.Media.Color.FromArgb((byte)(rgba & 0xff), (byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8))
            : global::Avalonia.Media.Color.TryParse(trimmed, out var parsed) ? parsed : null;
    }

    private static void CompareNumbers(MarkupElement expected, MarkupElement actual, string name, double[] left, double[] right, List<string> differences)
    {
        if (left.Length != right.Length || left.Zip(right).Any(pair => Math.Abs(pair.First - pair.Second) > Tolerance))
        {
            differences.Add($"{Describe(expected)} {name}: [{string.Join(", ", left.Take(8).Select(Text))}] in Recharts, [{string.Join(", ", right.Take(8).Select(Text))}] in Pictor");
        }

        _ = actual;
    }

    private static double Opacity(MarkupElement element, string name) =>
        element.Attributes.TryGetValue(name, out var value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : 1;

    private static double[] Numbers(MarkupElement element, string name) =>
        element.Attributes.TryGetValue(name, out var value) ? [.. Number().Matches(value).Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture))] : [];

    /// <summary>
    /// A path's numbers, translated by its transform, every command made absolute from the current point (lines as
    /// <c>L</c>, smooth curves with their reflected control points as <c>C</c> and <c>Q</c>), so two paths compare by
    /// what they draw however they were written.
    /// </summary>
    private static double[] PathNumbers(MarkupElement element, string name = "d")
    {
        var (dx, dy) = Translation(element);
        var path = Path(element, name);
        return [.. path.Values.Select((value, i) => value + (path.Kinds[i] switch { 'x' => dx, 'y' => dy, _ => 0 }))];
    }

    private static PathState Path(MarkupElement element, string name = "d")
    {
        var path = new PathState();
        foreach (var groups in PathCommand().Matches(element.Attributes.GetValueOrDefault(name) ?? string.Empty).Select(command => command.Groups))
        {
            var letter = groups[1].Value[0];
            var values = Number().Matches(groups[2].Value).Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
            path.Command(letter, values);
        }

        return path;
    }

    /// <summary>A path read command by command into absolute numbers, each marked as an x, a y or neither (an arc's radii, rotation and flags).</summary>
    private sealed class PathState
    {
        private double _x, _y, _startX, _startY, _controlX, _controlY;
        private char _last;

        public List<double> Values { get; } = [];

        public List<char> Kinds { get; } = [];

        /// <summary>The area the path encloses, each subpath closed, arcs with their circular segments (positive clockwise on the screen).</summary>
        public double Area { get; private set; }

        /// <summary>Whether the path holds a Bézier curve, whose area <see cref="Area"/> leaves out.</summary>
        public bool Curved { get; private set; }

        public void Command(char letter, double[] values)
        {
            var upper = char.ToUpperInvariant(letter);
            var relative = letter != upper;
            var arity = upper switch { 'M' or 'L' or 'T' => 2, 'H' or 'V' => 1, 'C' => 6, 'S' or 'Q' => 4, 'A' => 7, _ => 0 };
            if (arity == 0)
            {
                Close();
                (_x, _y, _last) = (_startX, _startY, 'Z');
                return;
            }

            for (var i = 0; i + arity <= values.Length; i += arity)
            {
                var command = upper == 'M' && i > 0 ? 'L' : upper;
                Step(command, values.AsSpan(i, arity), relative);
            }
        }

        private void Step(char command, ReadOnlySpan<double> v, bool relative)
        {
            var (ox, oy) = relative ? (_x, _y) : (0, 0);
            switch (command)
            {
                case 'M':
                    Close();
                    Add(v[0] + ox, 'x');
                    Add(v[1] + oy, 'y');
                    (_x, _y) = (v[0] + ox, v[1] + oy);
                    (_startX, _startY) = (_x, _y);
                    break;
                case 'L':
                    Point(v[0] + ox, v[1] + oy);
                    break;
                case 'H':
                    Point(v[0] + (relative ? _x : 0), _y);
                    break;
                case 'V':
                    Point(_x, v[0] + (relative ? _y : 0));
                    break;
                case 'C':
                    Cubic(v[0] + ox, v[1] + oy, v[2] + ox, v[3] + oy, v[4] + ox, v[5] + oy);
                    break;
                case 'S':
                    var (sx, sy) = _last is 'C' or 'S' ? ((2 * _x) - _controlX, (2 * _y) - _controlY) : (_x, _y);
                    Cubic(sx, sy, v[0] + ox, v[1] + oy, v[2] + ox, v[3] + oy);
                    break;
                case 'Q':
                    Quadratic(v[0] + ox, v[1] + oy, v[2] + ox, v[3] + oy);
                    break;
                case 'T':
                    var (tx, ty) = _last is 'Q' or 'T' ? ((2 * _x) - _controlX, (2 * _y) - _controlY) : (_x, _y);
                    Quadratic(tx, ty, v[0] + ox, v[1] + oy);
                    break;
                default:
                    Segment(v[0], v[1], v[3] != 0, v[4] != 0, v[5] + ox, v[6] + oy);
                    Add(v[0], 'r');
                    Add(v[1], 'r');
                    Add(v[2], 'r');
                    Add(v[3], 'r');
                    Add(v[4], 'r');
                    Point(v[5] + ox, v[6] + oy);
                    break;
            }

            _last = command;
        }

        /// <summary>Closes the current subpath's area back to its start.</summary>
        public void Close() => Chord(_startX, _startY);

        private void Chord(double x, double y) => Area += ((_x * y) - (x * _y)) / 2;

        private void Segment(double rx, double ry, bool large, bool sweep, double x, double y)
        {
            var chord = Math.Sqrt(((x - _x) * (x - _x)) + ((y - _y) * (y - _y)));
            Curved |= Math.Abs(rx - ry) > Tolerance;
            var radius = Math.Max(Math.Abs(rx), chord / 2);
            if (radius == 0)
            {
                return;
            }

            var small = 2 * Math.Asin(Math.Min(1, chord / (2 * radius)));
            var angle = large ? (2 * Math.PI) - small : small;
            Area += (sweep ? 1 : -1) * radius * radius / 2 * (angle - Math.Sin(angle));
        }

        private void Cubic(double x1, double y1, double x2, double y2, double x, double y)
        {
            Curved = true;
            Add(x1, 'x');
            Add(y1, 'y');
            Add(x2, 'x');
            Add(y2, 'y');
            (_controlX, _controlY) = (x2, y2);
            Point(x, y);
        }

        private void Quadratic(double x1, double y1, double x, double y)
        {
            Curved = true;
            Add(x1, 'x');
            Add(y1, 'y');
            (_controlX, _controlY) = (x1, y1);
            Point(x, y);
        }

        private void Point(double x, double y)
        {
            Chord(x, y);
            Add(x, 'x');
            Add(y, 'y');
            (_x, _y) = (x, y);
        }

        private void Add(double value, char kind)
        {
            Values.Add(value);
            Kinds.Add(kind);
        }
    }

    private static (double X, double Y) Translation(MarkupElement element)
    {
        if (element.Attributes.TryGetValue("transform", out var transform) && Translate().Match(transform) is { Success: true } match)
        {
            return (double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
        }

        return (0, 0);
    }

    private static string Text(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Describe(MarkupElement element) => $"<{element.Tag} class=\"{element.Classes}\">";

    [GeneratedRegex(@"<(/?)([a-zA-Z][a-zA-Z0-9]*)((?:\s+[^\s=>/]+(?:=""[^""]*"")?)*)\s*(/?)>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"([^\s=]+)=""([^""]*)""")]
    private static partial Regex Attribute();

    [GeneratedRegex(@"-?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")]
    private static partial Regex Number();

    [GeneratedRegex(@"([MLHVCSQTAZmlhvcsqtaz])([^MLHVCSQTAZmlhvcsqtaz]*)")]
    private static partial Regex PathCommand();

    [GeneratedRegex(@"url\(#([^)]+)\)")]
    private static partial Regex ClipReference();

    [GeneratedRegex(@"^url\(#([^)]+)\)$")]
    private static partial Regex GradientReference();

    [GeneratedRegex(@"translate\(\s*(-?[\d.]+)[ ,]+(-?[\d.]+)\s*\)")]
    private static partial Regex Translate();
}
