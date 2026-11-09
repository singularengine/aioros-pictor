using System.Collections.Immutable;
using Aioros.Pictor.Content;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor;

/// <summary>When a node of a template is visible: while an affine function of the number members is above zero (or at least zero).</summary>
internal sealed record VisibilityRule(ContentNumber Value, bool Inclusive, int Primary)
{
    public double At(ProbePoint point) => ContentFit.Evaluate(Value, point);

    public bool Holds(ProbePoint point) => At(point) is var value && (Inclusive ? value >= 0 : value > 0);

    /// <summary>How much the value moves when the primary member moves by one.</summary>
    public double Slope => Value.Coefficients[Value.Members.IndexOf(Primary)];

    /// <summary>Whether the rule reads its primary member alone.</summary>
    public bool Single => Value.Members.Length == 1;

    public ContentCondition Condition => new() { Value = Value, Inclusive = Inclusive };
}

/// <summary>
/// Finds when each node of a template is visible: its own <c>IsVisible</c> probed at both ends of every member (a flag
/// at its other value, a whole number at each value from -1 to 12), then, for a node that turns on or off, the rules
/// that show it: the line in the members where it turns (by bisection along each member that moves it), so that a
/// visibility bound through a converter (<c>Value &gt; 2500</c>, <c>X + Width + 6 &gt; ContainerWidth</c>) compiles to a
/// condition; a flag; or the range of a whole number it is shown in (<c>Depth == 1</c>).
/// </summary>
internal sealed class ContentVisibility(ContentProbes probes, ProbePoint origin, int count)
{
    private const double Far = 10000;
    private const int Steps = 72;
    private const int Lowest = -1;
    private const int Highest = 12;
    private SceneNode[]? _origin;

    /// <summary>The rules of each node (all hold while it is shown): null when always shown; a node never shown is marked in <paramref name="never"/>.</summary>
    public List<VisibilityRule>?[] Rules(bool[] never)
    {
        var rules = new List<VisibilityRule>?[count];
        var ends = Ends();
        for (var node = 0; node < count; node++)
        {
            var visibleAtOrigin = Visible(origin, node);
            if (visibleAtOrigin && ends.TrueForAll(end => end.Nodes[node].Visible))
            {
                continue;
            }

            if (!visibleAtOrigin && ends.TrueForAll(end => !end.Nodes[node].Visible))
            {
                never[node] = true;
                continue;
            }

            rules[node] = Rule(node, ends, visibleAtOrigin);
        }

        return rules;
    }

    private sealed record End(int Member, double Value, SceneNode[] Nodes);

    /// <summary>The template laid out with each member at both ends: a number at -10000 and 10000, a flag at its other value, a whole number at each value from -1 to 12 as well.</summary>
    private List<End> Ends()
    {
        var ends = new List<End>();
        for (var i = 0; i < probes.Count; i++)
        {
            double[] values = Kind(i) switch
            {
                MemberKind.Flag => [1 - origin.Numbers[i]],
                MemberKind.Whole => [-Far, Far, .. Enumerable.Range(Lowest, Highest - Lowest + 1).Select(value => (double)value)],
                _ => [-Far, Far],
            };
            foreach (var value in values)
            {
                if (Nodes(origin.With(probes.Slot, i, value)) is { } nodes)
                {
                    ends.Add(new End(i, value, nodes));
                }
            }
        }

        return ends;
    }

    private MemberKind Kind(int member) => member < probes.Slot.Numbers.Length ? probes.Slot.Numbers[member].Kind : MemberKind.Number;

    private List<VisibilityRule> Rule(int node, List<End> ends, bool atOrigin)
    {
        var rules = new List<VisibilityRule>();
        var flags = new List<int>();
        var lines = new List<int>();
        foreach (var group in ends.GroupBy(end => end.Member))
        {
            var far = group.Where(end => Math.Abs(end.Value) >= Far).Select(end => end.Nodes[node].Visible).Distinct().Count() > 1;
            if (Kind(group.Key) == MemberKind.Flag && group.Any(end => end.Nodes[node].Visible != atOrigin))
            {
                flags.Add(group.Key);
            }
            else if (far)
            {
                lines.Add(group.Key);
            }
            else if (Kind(group.Key) == MemberKind.Whole && Range(node, group) is { } range)
            {
                rules.AddRange(range);
            }
        }

        if (lines.Count > 0)
        {
            rules.Add(Line(node, [.. lines, .. flags]));
        }
        else
        {
            rules.AddRange(flags.Select(flag => Flag(node, flag)));
        }

        if (rules.Count == 0)
        {
            throw new ContentRefusedException("the visibility of one of the template's elements changes in a way Pictor cannot follow");
        }

        return Reproduced(node, rules, ends) ? rules : throw new ContentRefusedException("the visibility of one of the template's elements is not one condition on its members (shown over two ranges of a value, as Depth == 1 || Depth > 100)");
    }

    /// <summary>Whether the rules show the node exactly where it was shown at every probed end, so a condition that holds over two ranges is refused rather than compiled to one.</summary>
    private bool Reproduced(int node, List<VisibilityRule> rules, List<End> ends)
    {
        foreach (var end in ends)
        {
            var point = origin.With(probes.Slot, end.Member, end.Value);
            if (rules.TrueForAll(rule => rule.Holds(point)) != end.Nodes[node].Visible)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>A flag's rule: shown while the flag is set, or while it is not.</summary>
    private VisibilityRule Flag(int node, int flag)
    {
        var sign = Visible(origin.With(probes.Slot, flag, 1), node) ? 1 : -1;
        return new VisibilityRule(new ContentNumber { Constant = -0.5 * sign, Members = [flag], Coefficients = [sign] }, false, flag);
    }

    /// <summary>The rules of a whole number a node is shown in one range of (from -1 to 12): above its lowest value less a half, below its highest plus a half; null when it is not one range.</summary>
    private static List<VisibilityRule>? Range(int node, IGrouping<int, End> group)
    {
        var shown = group.Where(end => Math.Abs(end.Value) < Far && end.Nodes[node].Visible).Select(end => (int)end.Value).Order().ToList();
        if (shown.Count == 0 || shown[^1] - shown[0] + 1 != shown.Count || group.Count(end => Math.Abs(end.Value) < Far) == shown.Count)
        {
            return null;
        }

        var member = group.Key;
        var rules = new List<VisibilityRule>();
        if (shown[0] > Lowest)
        {
            rules.Add(new VisibilityRule(new ContentNumber { Constant = -(shown[0] - 0.5), Members = [member], Coefficients = [1] }, false, member));
        }

        if (shown[^1] < Highest)
        {
            rules.Add(new VisibilityRule(new ContentNumber { Constant = shown[^1] + 0.5, Members = [member], Coefficients = [-1] }, false, member));
        }

        return rules;
    }

    /// <summary>The line where a node turns on or off, from its threshold along the first member that moves it and how the others shift that threshold.</summary>
    private VisibilityRule Line(int node, List<int> moving)
    {
        var primary = moving[0];
        var threshold = Threshold(node, primary, origin);
        var members = ImmutableArray.CreateBuilder<int>();
        var coefficients = ImmutableArray.CreateBuilder<double>();
        members.Add(primary);
        coefficients.Add(1);
        var constant = -threshold;
        foreach (var other in moving.Skip(1))
        {
            var shift = Kind(other) == MemberKind.Flag ? 1 - origin.Numbers[other] : 10;
            var shifted = Threshold(node, primary, origin.With(probes.Slot, other, origin.Numbers[other] + shift));
            var coefficient = Math.Round((threshold - shifted) / shift, 10);
            members.Add(other);
            coefficients.Add(coefficient);
            constant -= coefficient * origin.Numbers[other];
        }

        var rounded = double.Parse(threshold.ToString("G10", System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture);
        var sign = Visible(origin.With(probes.Slot, primary, threshold + 1), node) ? 1 : -1;
        var inclusive = Visible(origin.With(probes.Slot, primary, rounded), node);
        var value = new ContentNumber
        {
            Constant = Math.Round(sign * (constant + (threshold - rounded)), 9),
            Members = members.ToImmutable(),
            Coefficients = [.. coefficients.Select(coefficient => sign * coefficient)],
        };
        return new VisibilityRule(value, inclusive, primary);
    }

    /// <summary>The value of a member where a node turns on or off, the others as at a point.</summary>
    private double Threshold(int node, int member, ProbePoint at)
    {
        var (low, high) = (-Far, Far);
        var lowVisible = Visible(at.With(probes.Slot, member, low), node);
        if (Visible(at.With(probes.Slot, member, high), node) == lowVisible)
        {
            throw new ContentRefusedException("the visibility of a template's element depends on two members together in a way Pictor cannot follow");
        }

        for (var step = 0; step < Steps; step++)
        {
            var middle = (low + high) / 2;
            if (Visible(at.With(probes.Slot, member, middle), node) == lowVisible)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return (low + high) / 2;
    }

    private bool Visible(ProbePoint point, int node) => (ReferenceEquals(point, origin) ? _origin ??= Nodes(point) : Nodes(point)) is { } nodes && nodes[node].Visible;

    /// <summary>The template's elements in order for a probe, or null when Avalonia cannot lay it out with those values.</summary>
    public SceneNode[]? Nodes(ProbePoint point)
    {
        SceneNode root;
        try
        {
            root = probes.Run(point);
        }
        catch (ContentRefusedException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            return null;
        }

        var nodes = ContentCompiler.Flatten(root);
        return nodes.Length == count ? nodes : throw new ContentRefusedException("the template's elements change with the item (an items control or a template selector inside it)");
    }
}
