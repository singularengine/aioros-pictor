using System.Collections.Immutable;
using System.Globalization;
using Aioros.Pictor.Content;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor;

/// <summary>
/// The probes of one region of the members (where the same nodes are visible): the base point, one point per number
/// member moved by one, and two points with every number moved, to check what the others found.
/// </summary>
internal sealed class ProbeRegion
{
    public required ProbePoint Base { get; init; }

    public required SceneNode[] BaseNodes { get; init; }

    /// <summary>The point that moves each number member, or null for a flag (flags only decide visibility).</summary>
    public required ProbePoint?[] Moved { get; init; }

    public required SceneNode[]?[] MovedNodes { get; init; }

    public required ProbePoint[] Checks { get; init; }

    public required SceneNode[][] CheckNodes { get; init; }

    public IEnumerable<(ProbePoint Point, SceneNode[] Nodes)> All()
    {
        yield return (Base, BaseNodes);
        for (var i = 0; i < Moved.Length; i++)
        {
            if (Moved[i] is { } point)
            {
                yield return (point, MovedNodes[i]!);
            }
        }

        for (var i = 0; i < Checks.Length; i++)
        {
            yield return (Checks[i], CheckNodes[i]);
        }
    }
}

/// <summary>
/// Finds what a node's props are made of from a region's probes: each number as an affine function of the number
/// members, each color and anchor as a member or a literal, each text as literals and members.
/// </summary>
internal static class ContentFit
{
    private const double Tolerance = 1e-6;

    public static ImmutableArray<ContentProp> Props(ProbeRegion region, int node, ContentSlot slot)
    {
        var props = ImmutableArray.CreateBuilder<ContentProp>();
        var baseNode = region.BaseNodes[node];
        for (var k = 0; k < baseNode.Props.Count; k++)
        {
            props.Add(Prop(region, node, k, slot));
        }

        return props.ToImmutable();
    }

    private static ContentProp Prop(ProbeRegion region, int node, int k, ContentSlot slot)
    {
        var prop = region.BaseNodes[node].Props[k];
        var values = new List<(ProbePoint Point, SceneProp Prop)>();
        foreach (var (point, nodes) in region.All())
        {
            var other = nodes[node].Props.ElementAtOrDefault(k);
            if (other is null || other.Name != prop.Name || other.Numbers.Length != prop.Numbers.Length || (prop.Name is ContentNames.Path or ContentNames.Transform && other.Text != prop.Text))
            {
                throw new ContentRefusedException($"the {prop.Name} of the template's {region.BaseNodes[node].Origin} changes its shape with the item");
            }

            values.Add((point, other));
        }

        var origin = region.BaseNodes[node].Origin;
        if (prop.Name == ContentNames.Content)
        {
            return Content(region, values, slot, origin);
        }

        if (prop.Numbers.Length > 0 || prop.Name is ContentNames.Path or ContentNames.Shape or ContentNames.Transform or ContentNames.Points)
        {
            return Numbers(region, values, slot, origin);
        }

        return prop.Name switch
        {
            "fill" or "stroke" or "background" or "border-color" or "color" => Member(values, slot, MemberKind.Color, origin) ?? Literal(values, origin),
            "text-anchor" => Member(values, slot, MemberKind.Anchor, origin) ?? Literal(values, origin),
            _ => Literal(values, origin),
        };
    }

    private static ContentProp Numbers(ProbeRegion region, List<(ProbePoint Point, SceneProp Prop)> values, ContentSlot slot, string origin)
    {
        var prop = values[0].Prop;
        if (prop.Name == ContentNames.Points && Points(values, slot) is { } member)
        {
            return new ContentProp { Name = prop.Name, Member = member };
        }

        var numbers = ImmutableArray.CreateBuilder<ContentNumber>(prop.Numbers.Length);
        for (var j = 0; j < prop.Numbers.Length; j++)
        {
            numbers.Add(Number(region, values, j, $"the {prop.Name} of the template's {origin}"));
        }

        _ = slot;
        return new ContentProp { Name = prop.Name, Numbers = numbers.MoveToImmutable(), Text = prop.Text };
    }

    /// <summary>The points member when a polyline's points are the context's points in every probe.</summary>
    private static int? Points(List<(ProbePoint Point, SceneProp Prop)> values, ContentSlot slot)
    {
        if (!slot.Points)
        {
            return null;
        }

        foreach (var (point, prop) in values)
        {
            double[] expected = [point.Numbers[0], point.Numbers[1], point.Numbers[0] + 40, point.Numbers[1] + 10, point.Numbers[0] + 20, point.Numbers[1] + 60];
            if (!expected.AsSpan().SequenceEqual(prop.Numbers))
            {
                return null;
            }
        }

        return 0;
    }

    /// <summary>One number of a prop as an affine function of the number members, checked on the region's check points.</summary>
    public static ContentNumber Number(ProbeRegion region, List<(ProbePoint Point, SceneProp Prop)> values, int j, string what)
    {
        var at = values.ToDictionary(value => value.Point.Id, value => value.Prop.Numbers[j]);
        var origin = at[region.Base.Id];
        if (double.IsNaN(origin))
        {
            return values.TrueForAll(value => double.IsNaN(value.Prop.Numbers[j])) ? new ContentNumber { Constant = double.NaN } : throw new ContentRefusedException($"{what} is not a number for every item");
        }

        var members = ImmutableArray.CreateBuilder<int>();
        var coefficients = ImmutableArray.CreateBuilder<double>();
        var constant = origin;
        for (var i = 0; i < region.Moved.Length; i++)
        {
            if (region.Moved[i] is not { } moved)
            {
                continue;
            }

            var delta = moved.Numbers[i] - region.Base.Numbers[i];
            var coefficient = Math.Round((at[moved.Id] - origin) / delta, 12);
            if (Math.Abs(coefficient) > 1e-9)
            {
                members.Add(i);
                coefficients.Add(coefficient);
                constant -= coefficient * region.Base.Numbers[i];
            }
        }

        var divisor = Divisor(coefficients);
        var number = new ContentNumber
        {
            Constant = Math.Round(constant, 9),
            Members = members.ToImmutable(),
            Coefficients = divisor == 1 ? coefficients.ToImmutable() : [.. coefficients.Select(coefficient => Math.Round(coefficient * divisor, 9))],
            Divisor = divisor,
        };
        foreach (var check in region.Checks)
        {
            var expected = Evaluate(number, check);
            var actual = at[check.Id];
            if (!(Math.Abs(expected - actual) <= Tolerance * (1 + Math.Abs(actual))))
            {
                throw new ContentRefusedException($"{what} is not a sum of the context's members times numbers ({actual.ToString(CultureInfo.InvariantCulture)} where such a sum gives {expected.ToString(CultureInfo.InvariantCulture)}): a converter with logic, or a position that depends on a text's width");
            }
        }

        return number;
    }

    public static double Evaluate(ContentNumber number, ProbePoint point)
    {
        var sum = 0.0;
        for (var i = 0; i < number.Members.Length; i++)
        {
            sum += number.Coefficients[i] * point.Numbers[number.Members[i]];
        }

        return number.Constant + (sum / number.Divisor);
    }

    /// <summary>
    /// The whole number the multiples were divided by, when one of them has no short binary form (a third, a thousandth)
    /// and every multiple times that number is whole or a short binary fraction: 1 otherwise.
    /// </summary>
    private static double Divisor(ImmutableArray<double>.Builder coefficients)
    {
        if (coefficients.All(static coefficient => Binary(coefficient)))
        {
            return 1;
        }

        return Divisors.FirstOrDefault(divisor => coefficients.All(coefficient => Binary(Math.Round(coefficient * divisor, 9))), 1);
    }

    private static readonly double[] Divisors = [.. Enumerable.Range(2, 99).Select(static value => (double)value), 1000, 10000, 100000, 1000000];

    private static bool Binary(double value) => Math.Abs((value * 1024) - Math.Round(value * 1024)) < 1e-6;

    /// <summary>A prop read whole from one text member of a kind in every probe, or null.</summary>
    private static ContentProp? Member(List<(ProbePoint Point, SceneProp Prop)> values, ContentSlot slot, MemberKind kind, string origin)
    {
        _ = origin;
        for (var j = 0; j < slot.Texts.Length; j++)
        {
            if (slot.Texts[j].Kind == kind && values.TrueForAll(value => string.Equals(value.Prop.Text, value.Point.Texts[j], StringComparison.OrdinalIgnoreCase)))
            {
                return new ContentProp { Name = values[0].Prop.Name, Member = j };
            }
        }

        return null;
    }

    private static ContentProp Literal(List<(ProbePoint Point, SceneProp Prop)> values, string origin)
    {
        var text = values[0].Prop.Text;
        return values.TrueForAll(value => value.Prop.Text == text)
            ? new ContentProp { Name = values[0].Prop.Name, Text = text }
            : throw new ContentRefusedException($"the {values[0].Prop.Name} of the template's {origin} changes with the item but is none of the context's members");
    }

    /// <summary>A text's content: a number through its format, or literals and members found in the base probe and checked in every other.</summary>
    private static ContentProp Content(ProbeRegion region, List<(ProbePoint Point, SceneProp Prop)> values, ContentSlot slot, string origin)
    {
        var prop = values[0].Prop;
        if (prop.Numbers.Length == 1)
        {
            var number = Number(region, values, 0, $"the value of the template's {origin}");
            return new ContentProp { Name = prop.Name, Parts = [new ContentPart { Kind = prop.Format is null ? ContentPartKind.Number : ContentPartKind.Formatted, Literal = prop.Format, Value = number }] };
        }

        var parts = Followed(TextParts.Parts(prop.Text ?? string.Empty, region.Base, slot, numbers: true), values, slot)
            ?? Followed(TextParts.Parts(prop.Text ?? string.Empty, region.Base, slot, numbers: false), values, slot)
            ?? throw new ContentRefusedException($"the text of the template's {origin} changes with the item in a way Pictor cannot follow: bind it to a text member, or use SvgText's Value and Format for a number");
        return parts.Length == 1 && parts[0].Kind == ContentPartKind.Literal ? new ContentProp { Name = prop.Name, Text = parts[0].Literal } : new ContentProp { Name = prop.Name, Parts = parts };
    }

    /// <summary>The parts when they give every probe's text, or null.</summary>
    private static ImmutableArray<ContentPart>? Followed(ImmutableArray<ContentPart> parts, List<(ProbePoint Point, SceneProp Prop)> values, ContentSlot slot) =>
        values.TrueForAll(value => TextParts.Expected(parts, value.Point, slot) == (value.Prop.Text ?? string.Empty)) ? parts : null;
}

/// <summary>A text of a template as literals and holes: the marks of text members, or a number member shown whole.</summary>
internal static class TextParts
{
    public static ImmutableArray<ContentPart> Parts(string text, ProbePoint point, ContentSlot slot, bool numbers)
    {
        if (numbers && WholeNumber(text, point, slot) is { } whole)
        {
            return [whole];
        }

        var parts = ImmutableArray.CreateBuilder<ContentPart>();
        var position = 0;
        while (position < text.Length)
        {
            var (at, length, part) = NextMark(text, position, point, slot, numbers);
            if (at < 0)
            {
                parts.Add(new ContentPart { Kind = ContentPartKind.Literal, Literal = text[position..] });
                break;
            }

            if (at > position)
            {
                parts.Add(new ContentPart { Kind = ContentPartKind.Literal, Literal = text[position..at] });
            }

            parts.Add(part!);
            position = at + length;
        }

        return parts.Count == 0 ? [new ContentPart { Kind = ContentPartKind.Literal, Literal = string.Empty }] : parts.ToImmutable();
    }

    /// <summary>A text that is a number member as .NET writes it: a data member's text (Recharts' value as text), or a slot's number.</summary>
    private static ContentPart? WholeNumber(string text, ProbePoint point, ContentSlot slot)
    {
        for (var i = point.Numbers.Count - 1; i >= 0; i--)
        {
            if (text != point.Numbers[i].ToString(CultureInfo.CurrentCulture))
            {
                continue;
            }

            return i >= slot.Numbers.Length
                ? new ContentPart { Kind = ContentPartKind.Text, Member = slot.Texts.Length + (i - slot.Numbers.Length) }
                : new ContentPart { Kind = ContentPartKind.Number, Value = new ContentNumber { Members = [i], Coefficients = [1] } };
        }

        return null;
    }

    /// <summary>
    /// The next mark in a text: a text member's token, or (with <paramref name="numbers"/>) a whole number member as
    /// .NET writes it (an index in a formatted text), whichever comes first; its place, length and part.
    /// </summary>
    private static (int At, int Length, ContentPart? Part) NextMark(string text, int position, ProbePoint point, ContentSlot slot, bool numbers)
    {
        var next = (At: -1, Length: 0, Part: (ContentPart?)null);
        for (var j = 0; j < slot.Texts.Length; j++)
        {
            if (slot.Texts[j].Kind != MemberKind.Anchor && point.Texts[j] is { Length: > 0 } mark)
            {
                next = Earlier(next, text.IndexOf(mark, position, StringComparison.Ordinal), mark.Length, () => new ContentPart { Kind = ContentPartKind.Text, Member = j });
            }
        }

        for (var i = 0; numbers && i < point.Numbers.Count; i++)
        {
            var value = point.Numbers[i];
            var mark = value.ToString(CultureInfo.CurrentCulture);
            if (value == Math.Floor(value) && mark.Length >= 3)
            {
                var member = i;
                next = Earlier(next, text.IndexOf(mark, position, StringComparison.Ordinal), mark.Length, () => NumberPart(member, slot));
            }
        }

        return next;
    }

    private static (int At, int Length, ContentPart? Part) Earlier((int At, int Length, ContentPart? Part) next, int at, int length, Func<ContentPart> part) =>
        at >= 0 && (next.At < 0 || at < next.At) ? (at, length, part()) : next;

    /// <summary>A number member shown in a text: a data member's text, or a slot's number as JavaScript writes it.</summary>
    private static ContentPart NumberPart(int member, ContentSlot slot) => member >= slot.Numbers.Length
        ? new ContentPart { Kind = ContentPartKind.Text, Member = slot.Texts.Length + (member - slot.Numbers.Length) }
        : new ContentPart { Kind = ContentPartKind.Number, Value = new ContentNumber { Members = [member], Coefficients = [1] } };

    /// <summary>The text parts give for a probe, as Avalonia would show it.</summary>
    public static string Expected(ImmutableArray<ContentPart> parts, ProbePoint point, ContentSlot slot)
    {
        var text = new System.Text.StringBuilder();
        foreach (var part in parts)
        {
            text.Append(part.Kind switch
            {
                ContentPartKind.Text when part.Member >= slot.Texts.Length => Payload(point, slot, part.Member - slot.Texts.Length),
                ContentPartKind.Text => point.Texts[part.Member],
                ContentPartKind.Number => ContentFit.Evaluate(part.Value!, point).ToString(CultureInfo.CurrentCulture),
                _ => part.Literal,
            });
        }

        return text.ToString();
    }

    private static string Payload(ProbePoint point, ContentSlot slot, int k)
    {
        var member = slot.Numbers.Length + k;
        return (member < point.Numbers.Count ? point.Numbers[member] : ProbePoint.PayloadBase(member)).ToString(CultureInfo.CurrentCulture);
    }
}
