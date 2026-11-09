using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Aioros.Pictor.Demo;

/// <summary>A tooltip as it shows: whether it is visible, where its box starts, its label and its lines (text and color).</summary>
internal sealed record TipState(bool Visible, double X, double Y, string Label, IReadOnlyList<(string Text, string Color)> Items, string? Style);

/// <summary>What a hover state draws besides its tooltip: the cursor, the active dots and the active bars.</summary>
internal sealed record ActiveMarks(IReadOnlyList<MarkupElement> Cursors, IReadOnlyList<MarkupElement> Dots, IReadOnlyList<MarkupElement> Bars);

/// <summary>
/// Compares a hover state of Pictor's web chart (the K-th group of its <c>pictor-hits</c> and <c>pictor-tips</c>
/// layers) with Recharts' render of the same chart after the same pointer events in Chromium: the tooltip's visibility,
/// position, label and lines, the cursor, the active dots and the active bars, element by element.
/// </summary>
internal static partial class HoverComparison
{
    public static TipState RechartsTip(string markup)
    {
        var wrapper = Wrapper().Match(markup);
        if (!wrapper.Success)
        {
            return new TipState(false, 0, 0, string.Empty, [], null);
        }

        var style = wrapper.Groups[1].Value;
        var translate = Translate().Match(style);
        var content = wrapper.Groups[2].Value;
        var box = Box().Match(content);
        var items = Item().Matches(content).Select(item => (Plain(item.Groups[2].Value), Color(Declaration(item.Groups[1].Value, "color")))).ToList();
        return new TipState(
            !style.Contains("visibility: hidden", StringComparison.Ordinal),
            translate.Success ? Number(translate.Groups[1].Value) : 0,
            translate.Success ? Number(translate.Groups[2].Value) : 0,
            Plain(Label().Match(content).Groups[1].Value),
            items,
            box.Success ? box.Groups[1].Value : null);
    }

    /// <summary>
    /// A hover state's tooltip in Pictor's markup. A box that follows the pointer (its hover layer's <c>pictor-fy</c> or
    /// <c>pictor-fx</c> rule) is placed as that rule places it in the browser, at <paramref name="pointer"/> (or without
    /// one at the view box's start), with the box's extent its <c>foreignObject</c> carries.
    /// </summary>
    public static TipState PictorTip(string markup, int state, (double X, double Y)? pointer = null)
    {
        var tip = Group(markup, "pictor-tips", state);
        var foreign = Foreign().Match(tip);
        if (!foreign.Success)
        {
            return new TipState(false, 0, 0, string.Empty, [], null);
        }

        var items = PictorItem().Matches(tip).Select(item => (Plain(item.Groups[2].Value), Color(Declaration(item.Groups[1].Value, "color")))).ToList();
        var box = PictorBox().Match(tip);
        var (x, y) = (Number(foreign.Groups[1].Value), Number(foreign.Groups[2].Value));
        if (Follow().Match(markup) is { Success: true } follow)
        {
            var alongX = follow.Groups[1].Value == "x";
            var at = pointer is { } p ? Math.Floor((alongX ? p.X : p.Y) + 0.5) : Number(follow.Groups[7].Value);
            var placed = Followed(at, Number(alongX ? foreign.Groups[3].Value : foreign.Groups[4].Value), Number(follow.Groups[4].Value), Number(follow.Groups[5].Value), Number(follow.Groups[6].Value), follow.Groups[2].Success, follow.Groups[3].Success);
            (x, y) = alongX ? (placed, y) : (x, placed);
        }

        else if (PolarFollow().Match(markup) is { Success: true } polar)
        {
            (x, y) = PolarPlaced(polar, tip, foreign, pointer);
        }

        return new TipState(true, x, y, Plain(PictorLabel().Match(tip).Groups[1].Value), items, BoxStyle(markup, box.Success ? box.Groups[1].Value : string.Empty));
    }

    /// <summary>
    /// The style a Pictor tooltip box shows with: its page rule's declarations (each <c>var()</c> the value the chart's
    /// wrapper gives the variable, or its fallback), then the box's own <c>style</c>, which wins.
    /// </summary>
    private static string BoxStyle(string markup, string own)
    {
        var rule = BoxRule().Match(markup);
        var declarations = rule.Success ? rule.Groups[1].Value : string.Empty;
        var resolved = Variable().Replace(declarations, match => VariableValue(markup, match.Groups[1].Value) ?? match.Groups[2].Value.Trim());
        return resolved + ";" + own;
    }

    private static string? VariableValue(string markup, string name)
    {
        var start = markup.IndexOf("class=\"recharts-wrapper", StringComparison.Ordinal);
        var end = start < 0 ? -1 : markup.IndexOf('>', start);
        var wrapper = end < 0 ? string.Empty : markup[start..end];
        var found = Regex.Match(wrapper, "--" + Regex.Escape(name) + ":([^;\"]+)", RegexOptions.None, TimeSpan.FromSeconds(1));
        return found.Success ? found.Groups[1].Value.Trim() : null;
    }

    /// <summary>A box style's declarations, each value with its colors as <c>#rrggbb</c> and its zero lengths as <c>0</c>, the last declaration of a property winning.</summary>
    public static Dictionary<string, string> Declarations(string? style)
    {
        var declarations = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var declaration in (style ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = declaration.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && parts[0].Length > 0)
            {
                declarations[parts[0].ToLowerInvariant()] = string.Join(' ', Tokens().Matches(parts[1]).Select(token => token.Value is "0px" ? "0" : Color(token.Value)));
            }
        }

        return declarations;
    }

    /// <summary>What a polar box's rule computes: the pointer's distance along the box's direction (<c>pictor-fp</c>) or its angle on the box's radius (<c>pictor-fa</c>), then the placement along each coordinate.</summary>
    private static (double X, double Y) PolarPlaced(Match polar, string tip, Match foreign, (double X, double Y)? pointer)
    {
        double Var(string text, string name) => Number(Regex.Match(text, "--pictor-" + name + ":([-\\d.e]+)", RegexOptions.None, TimeSpan.FromSeconds(1)).Groups[1].Value);
        var layer = polar.Value;
        var (cx, cy, offset) = (Var(layer, "cx"), Var(layer, "cy"), Var(layer, "fo"));
        var (dx, dy) = pointer is { } p ? (Math.Floor(p.X + 0.5) - cx, Math.Floor(p.Y + 0.5) - cy) : (double.NaN, double.NaN);
        double px, py;
        if (polar.Groups[1].Value == "p")
        {
            var d = double.IsNaN(dx) ? Var(layer, "rest") : Math.Sqrt((dx * dx) + (dy * dy));
            (px, py) = (cx + (d * Var(tip, "u")), cy + (d * Var(tip, "v")));
        }
        else
        {
            var angle = double.IsNaN(dx) ? Math.Atan2(Var(layer, "v0"), Var(layer, "u0")) : Math.Atan2(dy, dx);
            var r = Var(tip, "r");
            (px, py) = (cx + (r * Math.Cos(angle)), cy + (r * Math.Sin(angle)));
        }

        return (Followed(px, Number(foreign.Groups[3].Value), Var(layer, "xs"), Var(layer, "xe"), offset, false, false),
            Followed(py, Number(foreign.Groups[4].Value), Var(layer, "ys"), Var(layer, "ye"), offset, false, false));
    }

    /// <summary>What a following box's rule computes (Recharts' <c>getTooltipTranslateXY</c> at the pointer): <paramref name="at"/> the pointer, a box of <paramref name="size"/> in the view box from <paramref name="start"/> to <paramref name="end"/>.</summary>
    private static double Followed(double at, double size, double start, double end, double offset, bool escape, bool reverse) => (escape, reverse) switch
    {
        (true, false) => at + offset,
        (true, true) => at - offset - size,
        (false, true) => at - offset - size < start ? Math.Max(at + offset, start) : Math.Max(at - offset - size, start),
        _ => at + offset + size > end ? Math.Max(at - offset - size, start) : Math.Max(at + offset, start),
    };

    public static ActiveMarks RechartsMarks(string markup)
    {
        var svg = markup[..(markup.LastIndexOf("</svg>", StringComparison.Ordinal) + 6)];
        var elements = MarkupComparison.Parse(svg);
        var cursors = elements.Where(element => element.Classes.Contains("recharts-tooltip-cursor", StringComparison.Ordinal)).ToList();
        var dots = new List<MarkupElement>();
        var bars = new List<MarkupElement>();
        for (var i = 1; i < elements.Count; i++)
        {
            var parent = elements[i - 1];
            if (parent.Tag == "g" && parent.Classes.Contains("recharts-active-dot", StringComparison.Ordinal) && elements[i].Tag == "circle")
            {
                dots.Add(elements[i]);
            }
            else if (parent.Tag == "g" && parent.Classes.Contains("recharts-active-bar", StringComparison.Ordinal) && elements[i].Tag == "path")
            {
                bars.Add(elements[i]);
            }
        }

        return new ActiveMarks(cursors, dots, bars);
    }

    public static ActiveMarks PictorMarks(string markup, int state)
    {
        var hits = MarkupComparison.Parse(Inner(Group(markup, "pictor-cursors", state)));
        var tips = MarkupComparison.Parse(Foreign().Replace(Inner(Group(markup, "pictor-tips", state)), string.Empty));
        var cursors = hits.Concat(tips).Where(element => element.Classes.Contains("recharts-tooltip-cursor", StringComparison.Ordinal)).Select(Cursor).ToList();
        var dots = tips.Where(element => element.Tag == "circle").Select(Dot).ToList();
        var bars = tips.Where(element => element.Tag == "path" && element.Classes == "recharts-rectangle").ToList();
        return new ActiveMarks(cursors, dots, bars);
    }

    /// <summary>The differences between two tooltips; positions within <paramref name="tolerance"/> pixels (Recharts measures its box in the DOM, Pictor with Inter's metrics).</summary>
    public static List<string> Compare(TipState expected, TipState actual, double tolerance = 0.01, bool position = true)
    {
        var differences = new List<string>();
        if (expected.Visible != actual.Visible)
        {
            differences.Add($"tooltip visible: {expected.Visible} in Recharts, {actual.Visible} in Pictor");
            return differences;
        }

        if (!expected.Visible)
        {
            return differences;
        }

        if (position && (Math.Abs(expected.X - actual.X) > tolerance || Math.Abs(expected.Y - actual.Y) > tolerance))
        {
            differences.Add($"tooltip at ({Text(expected.X)}, {Text(expected.Y)}) in Recharts, ({Text(actual.X)}, {Text(actual.Y)}) in Pictor");
        }

        if (expected.Label != actual.Label)
        {
            differences.Add($"label \"{expected.Label}\" in Recharts, \"{actual.Label}\" in Pictor");
        }

        if (!expected.Items.SequenceEqual(actual.Items))
        {
            differences.Add($"items [{string.Join(", ", expected.Items)}] in Recharts, [{string.Join(", ", actual.Items)}] in Pictor");
        }

        if (expected.Style is not null)
        {
            var shown = Declarations(actual.Style);
            foreach (var (name, value) in Declarations(expected.Style).Where(pair => shown.GetValueOrDefault(pair.Key) != pair.Value))
            {
                differences.Add($"box {name}: {value} in Recharts, {shown.GetValueOrDefault(name) ?? "nothing"} in Pictor");
            }
        }

        return differences;
    }

    public static List<string> Compare(ActiveMarks expected, ActiveMarks actual)
    {
        var differences = new List<string>();
        Compare("cursor", expected.Cursors, actual.Cursors, differences);
        Compare("active dot", expected.Dots, actual.Dots, differences);
        Compare("active bar", expected.Bars, actual.Bars, differences);
        return differences;
    }

    private static void Compare(string what, IReadOnlyList<MarkupElement> expected, IReadOnlyList<MarkupElement> actual, List<string> differences)
    {
        if (expected.Count != actual.Count)
        {
            differences.Add($"{expected.Count} {what}s in Recharts, {actual.Count} in Pictor");
            return;
        }

        for (var i = 0; i < expected.Count; i++)
        {
            MarkupComparison.CompareElements(expected[i] with { Attributes = new(expected[i].Attributes) { ["class"] = what } }, actual[i] with { Attributes = new(actual[i].Attributes) { ["class"] = what } }, differences);
        }
    }

    private static MarkupElement Cursor(MarkupElement element)
    {
        var attributes = new Dictionary<string, string>(element.Attributes, StringComparer.Ordinal);
        if (attributes.TryGetValue("style", out var style))
        {
            foreach (var declaration in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = declaration.Split(':', 2);
                attributes[parts[0].Trim()] = parts[1].Trim();
            }
        }
        else if (element.Classes.StartsWith("recharts-rectangle", StringComparison.Ordinal))
        {
            attributes["stroke"] = "none";
            attributes["fill"] = "#ccc";
        }
        else
        {
            attributes["stroke"] = "#ccc";
            attributes["fill"] = "none";
        }

        return element with { Attributes = attributes };
    }

    private static MarkupElement Dot(MarkupElement element)
    {
        if (element.Classes != "pictor-dot")
        {
            return element;
        }

        var attributes = new Dictionary<string, string>(element.Attributes, StringComparer.Ordinal) { ["class"] = "recharts-dot", ["stroke"] = "#fff", ["stroke-width"] = "2" };
        return element with { Attributes = attributes };
    }

    /// <summary>The K-th group of a layer of Pictor's markup.</summary>
    /// <summary>
    /// The markup of a hover state's group in a layer (<c>pictor-hits</c>, <c>pictor-clicks</c> or <c>pictor-tips</c>):
    /// the surface's child classed <c>pictor-hkK</c>, <c>pictor-ckK</c> or <c>pictor-tkK</c>, to its closing tag.
    /// </summary>
    public static string Group(string markup, string layer, int index)
    {
        var prefix = layer switch { "pictor-tips" => "pictor-tk", "pictor-clicks" => "pictor-ck", "pictor-cursors" => "pictor-c", _ => "pictor-hk" };
        var start = -1;
        foreach (var end in (string[])["\"", " "])
        {
            var found = markup.IndexOf($"class=\"{prefix}{index}{end}", StringComparison.Ordinal);
            start = found >= 0 && (start < 0 || found < start) ? found : start;
        }

        if (start < 0)
        {
            return string.Empty;
        }

        start = markup.LastIndexOf("<g", start, StringComparison.Ordinal);
        var depth = 0;
        for (var i = start; i < markup.Length; i++)
        {
            depth += Step(markup, i);
            if (depth == 0 && i > start)
            {
                return markup[start..(i + 4)];
            }
        }

        return string.Empty;
    }

    /// <summary>A group's children: its markup without its own tags (a state's group is left out of comparisons by its class), or a lone hit shape itself.</summary>
    private static string Inner(string group) =>
        group.StartsWith("<g", StringComparison.Ordinal) && group.EndsWith("</g>", StringComparison.Ordinal) ? group[(group.IndexOf('>', StringComparison.Ordinal) + 1)..^4] : group;

    private static int Step(string markup, int i) =>
        string.CompareOrdinal(markup, i, "<g", 0, 2) == 0 && (markup[i + 2] == '>' || markup[i + 2] == ' ') ? 1 : string.CompareOrdinal(markup, i, "</g>", 0, 4) == 0 ? -1 : 0;

    private static string Declaration(string style, string name)
    {
        var found = string.Empty;
        foreach (var declaration in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = declaration.Split(':', 2);
            if (parts.Length == 2 && parts[0].Trim() == name)
            {
                found = parts[1].Trim();
            }
        }

        return found;
    }

    private static string Color(string text)
    {
        var rgb = Rgb().Match(text);
        if (rgb.Success)
        {
            return string.Create(CultureInfo.InvariantCulture, $"#{int.Parse(rgb.Groups[1].Value, CultureInfo.InvariantCulture):x2}{int.Parse(rgb.Groups[2].Value, CultureInfo.InvariantCulture):x2}{int.Parse(rgb.Groups[3].Value, CultureInfo.InvariantCulture):x2}");
        }

        return Avalonia.Media.Color.TryParse(text, out var color) ? string.Create(CultureInfo.InvariantCulture, $"#{color.R:x2}{color.G:x2}{color.B:x2}") : text;
    }

    private static string Plain(string html) => WebUtility.HtmlDecode(Tags().Replace(html, string.Empty));

    private static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    private static string Text(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    [GeneratedRegex(@"<div (?:xmlns=""[^""]*"" )?tabindex=""-1"" class=""recharts-tooltip-wrapper[^""]*"" style=""([^""]*)"">(.*?)</div></div>", RegexOptions.Singleline)]
    private static partial Regex Wrapper();

    [GeneratedRegex(@"translate\(([-\d.e]+)px, ([-\d.e]+)px\)")]
    private static partial Regex Translate();

    [GeneratedRegex(@"<div class=""recharts-default-tooltip[^""]*""(?: role=""status"" aria-live=""assertive"")? style=""([^""]*)""")]
    private static partial Regex Box();

    [GeneratedRegex(@"<p class=""recharts-tooltip-label[^""]*""[^>]*>(.*?)</p>", RegexOptions.Singleline)]
    private static partial Regex Label();

    [GeneratedRegex(@"<li class=""recharts-tooltip-item"" style=""([^""]*)"">(.*?)</li>", RegexOptions.Singleline)]
    private static partial Regex Item();

    [GeneratedRegex(@"<foreignObject x=""([-\d.e]+)"" y=""([-\d.e]+)"" width=""([-\d.e]+)"" height=""([-\d.e]+)"".*?</foreignObject>", RegexOptions.Singleline)]
    private static partial Regex Foreign();

    [GeneratedRegex(@"class=""pictor-hover pictor-f([xy])(e)?(r)?"" style=""--pictor-fs:([-\d.e]+)px;--pictor-fe:([-\d.e]+)px;--pictor-fo:([-\d.e]+)px;--pictor-fr:([-\d.e]+)px""")]
    private static partial Regex Follow();

    [GeneratedRegex(@"class=""pictor-hover pictor-f([pa])"" style=""[^""]*""")]
    private static partial Regex PolarFollow();

    [GeneratedRegex(@"<li(?: class=""[^""]*"")? style=""([^""]*)"">(.*?)</li>", RegexOptions.Singleline)]
    private static partial Regex PictorItem();

    [GeneratedRegex(@"<div class=""recharts-default-tooltip[^""]*""(?: style=""([^""]*)"")?")]
    private static partial Regex PictorBox();

    [GeneratedRegex(@"<p[^>]*>(.*?)</p>", RegexOptions.Singleline)]
    private static partial Regex PictorLabel();

    [GeneratedRegex(@"\.recharts-default-tooltip\{(display:inline-block;[^}]*)\}")]
    private static partial Regex BoxRule();

    [GeneratedRegex(@"var\(--([\w-]+),([^()]*(?:\([^()]*\))?[^()]*)\)")]
    private static partial Regex Variable();

    [GeneratedRegex(@"[^\s(]+(?:\([^)]*\))?")]
    private static partial Regex Tokens();

    [GeneratedRegex(@"rgb\((\d+), (\d+), (\d+)\)")]
    private static partial Regex Rgb();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tags();
}
