using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Aioros.Pictor.Motion;
using Aioros.Runtime.Rendering;

namespace Aioros.Pictor.Svg;

/// <summary>
/// A button that plays the enter animations of a page's charts again, without script: a label holding a checkbox,
/// whose every change switches each movement to a copy of its keyframes under another name (a new animation name
/// restarts a CSS animation), as remounting a Recharts chart (a new React <c>key</c>) plays its animations again.
/// </summary>
internal static partial class ReplayMarkup
{
    private static readonly byte[] Rules = Encoding.UTF8.GetBytes(Alternates());

    /// <summary>Writes the button with its label text.</summary>
    public static void Write(MarkupBuffer output, string label)
    {
        Utf8Markup.Raw(output, "<label class=\"pictor-replay\" style=\"position:relative;display:inline-flex;align-items:center;gap:6px;padding:5px 11px;border:1px solid #8888;border-radius:4px;cursor:pointer;user-select:none\"><input type=\"checkbox\" style=\"position:absolute;left:0;top:0;opacity:0;width:1px;height:1px;margin:0\">"u8);
        Utf8Markup.Text(output, label);
        Utf8Markup.Raw(output, "</label>"u8);
        Utf8Markup.Raw(output, Rules);
    }

    /// <summary>Every movement's keyframes copied under a name ending in <c>-r</c>, each class switched to them while the box is checked.</summary>
    private static string Alternates()
    {
        var rules = new StringBuilder("<style>@media (prefers-reduced-motion:no-preference){.pictor-replay:has(:focus-visible){outline:2px solid}");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var kind in Enum.GetValues<StepKind>())
        {
            Add(rules, seen, Encoding.UTF8.GetString(SvgMotion.Rules(kind)));
        }

        Add(rules, seen, Encoding.UTF8.GetString(SvgMotion.Base));
        return rules.Append("}</style>").ToString();
    }

    private static void Add(StringBuilder rules, HashSet<string> seen, string css)
    {
        foreach (var groups in Keyframes().Matches(css).Select(match => match.Groups).Where(groups => seen.Add("@" + groups[1].Value)))
        {
            rules.Append("@keyframes ").Append(groups[1].Value).Append("-r{").Append(groups[2].Value).Append('}');
        }

        var classes = Animated().Matches(css).Select(match => match.Groups).SelectMany(groups => groups[1].Value.Split(',').Select(name => (Name: name, Keyframes: groups[2].Value)));
        foreach (var (name, keyframes) in classes.Where(pair => seen.Add(pair.Name)))
        {
            rules.Append(":root:has(.pictor-replay :checked) ").Append(name).Append("{animation-name:").Append(keyframes).Append("-r}");
        }
    }

    [GeneratedRegex(@"@keyframes ([\w-]+)\{((?:[^{}]|\{[^{}]*\})*)\}")]
    private static partial Regex Keyframes();

    [GeneratedRegex(@"((?:\.[\w-]+,?)+)\{[^}]*?animation:([\w-]+) ")]
    private static partial Regex Animated();
}

/// <summary>The writer Aioros calls for a <c>ReplayButton</c>: the label it records, written as the scriptless button.</summary>
public sealed class ReplaySlotWriter : ISlotWriter
{
    /// <inheritdoc />
    public static SlotWrite Create(string argument, MemberLookup members) => (_, output) =>
    {
        var markup = MarkupBuffer.Over(output);
        ReplayMarkup.Write(markup, argument);
        markup.Flush();
    };
}
