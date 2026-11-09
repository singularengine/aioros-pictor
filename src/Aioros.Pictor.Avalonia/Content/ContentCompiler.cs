using System.Collections.Immutable;
using Aioros.Pictor.Content;
using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Controls.Templates;

namespace Aioros.Pictor;

/// <summary>
/// Compiles a slot's XAML template to SVG with holes, once per template, at the time the spec is built (when Aioros
/// traces the page, on the desktop when the chart is laid out): the template is laid out by Avalonia with marked values
/// in its context, and what changes with each member is read back. Each number becomes an affine function of the
/// context's numbers, each color and text a member or a literal, each visibility a condition; what cannot be read so (a
/// converter with logic, code-behind, a position that depends on a text's width, a control that is not a shape) is
/// refused with its reason, and the template is then drawn by Avalonia (the desktop) or the page rendered by Avalonia
/// (the web's engine path). Precedents: Lit's template markers (holes found by rendering with unique values), Svelte's
/// and Marko's compiled holes, Aioros's own plans (TRC), and WPF's and Avalonia's data templates.
/// </summary>
internal static class ContentCompiler
{
    /// <summary>The compiled template, or a refused one with its reason.</summary>
    public static ContentSpec Compile(IDataTemplate template, ContentSlot slot, StyledElement? anchor, string property)
    {
        try
        {
            using var probes = new ContentProbes(template, slot, anchor);
            return Compile(probes);
        }
        catch (ContentRefusedException refusal)
        {
            return new ContentSpec { Refused = $"{anchor?.GetType().Name ?? "Chart"}.{property} cannot be written as SVG: {refusal.Message}" };
        }
    }

    private static ContentSpec Compile(ContentProbes probes)
    {
        var slot = probes.Slot;
        var origin = ProbePoint.Base(slot);
        var root = probes.Run(origin);
        for (var member = slot.Numbers.Length; member < probes.Count; member++)
        {
            origin.Set(member, ProbePoint.PayloadBase(member));
        }

        var count = Flatten(root).Length;
        var visibility = new ContentVisibility(probes, origin, count);
        var never = new bool[count];
        var rules = visibility.Rules(never);
        var props = new ImmutableArray<ContentProp>[count];
        var samples = new List<ProbePoint>();
        foreach (var group in Groups(root, rules, never))
        {
            var region = Region(probes, visibility, origin, group.Rules);
            samples.Add(region.Base);
            samples.AddRange(region.Checks);
            foreach (var node in group.Nodes)
            {
                Visible(region, node, root);
                props[node] = ContentFit.Props(region, node, slot);
            }
        }

        var index = 0;
        var nodes = Assemble(root, props, rules, never, samples, ref index);
        return new ContentSpec { Nodes = nodes is null ? [] : Defaults.Switches([nodes], samples), Keys = [.. probes.Keys] };
    }

    /// <summary>The nodes in document order (the order <see cref="Assemble"/> walks them).</summary>
    public static SceneNode[] Flatten(SceneNode root)
    {
        var nodes = new List<SceneNode>();
        Walk(root, nodes);
        return [.. nodes];

        static void Walk(SceneNode node, List<SceneNode> nodes)
        {
            nodes.Add(node);
            foreach (var child in node.Children)
            {
                Walk(child, nodes);
            }
        }
    }

    private sealed record Group(List<VisibilityRule> Rules, List<int> Nodes);

    /// <summary>The nodes grouped by the rules that show them (their own and their ancestors'), each group fitted in its own region.</summary>
    private static List<Group> Groups(SceneNode root, List<VisibilityRule>?[] rules, bool[] never)
    {
        var groups = new List<Group>();
        var index = 0;
        Collect(root, [], ref index);
        return groups;

        void Collect(SceneNode node, List<VisibilityRule> above, ref int position)
        {
            var at = position++;
            var path = rules[at] is { } own ? [.. above, .. own] : above;
            if (!never[at])
            {
                var group = groups.Find(known => known.Rules.Count == path.Count && known.Rules.SequenceEqual(path));
                if (group is null)
                {
                    group = new Group(path, []);
                    groups.Add(group);
                }

                group.Nodes.Add(at);
            }

            foreach (var child in node.Children)
            {
                Collect(child, path, ref position);
            }
        }
    }

    /// <summary>
    /// The probes of a region where every rule of a group holds: its base point, one point per member moved by one, two
    /// check points; a flag, or a whole number its rules hold at one value, is not moved (the nodes cannot depend on it there).
    /// </summary>
    private static ProbeRegion Region(ContentProbes probes, ContentVisibility visibility, ProbePoint origin, List<VisibilityRule> rules)
    {
        var slot = probes.Slot;
        var start = Satisfy(origin.Copy(slot), rules, slot) ?? throw new ContentRefusedException("the conditions that show the template's elements cannot hold together");
        var pinned = Pinned(rules, probes.Count);
        var moved = new ProbePoint?[probes.Count];
        var movedNodes = new SceneNode[]?[probes.Count];
        for (var i = 0; i < probes.Count; i++)
        {
            if (Kind(slot, i) != MemberKind.Flag && !pinned[i])
            {
                moved[i] = start.With(slot, i, start.Numbers[i] + 1);
                movedNodes[i] = visibility.Nodes(moved[i]!) ?? throw new ContentRefusedException("Avalonia cannot lay the template out with the probes' values");
            }
        }

        var checks = new[] { Check(start, slot, pinned, 1, rules), Check(start, slot, pinned, 2, rules) };
        return new ProbeRegion
        {
            Base = start,
            BaseNodes = visibility.Nodes(start) ?? throw new ContentRefusedException("Avalonia cannot lay the template out with the probes' values"),
            Moved = moved,
            MovedNodes = movedNodes,
            Checks = checks,
            CheckNodes = [.. checks.Select(check => visibility.Nodes(check) ?? throw new ContentRefusedException("Avalonia cannot lay the template out with the probes' values"))],
        };
    }

    /// <summary>The members a group's rules hold at one whole value: bounded below and above less than two apart.</summary>
    private static bool[] Pinned(List<VisibilityRule> rules, int count)
    {
        var pinned = new bool[count];
        foreach (var group in rules.Where(rule => rule.Single).GroupBy(rule => rule.Primary))
        {
            var low = group.Where(rule => rule.Slope > 0).Select(rule => -rule.Value.Constant / rule.Slope).DefaultIfEmpty(double.NegativeInfinity).Max();
            var high = group.Where(rule => rule.Slope < 0).Select(rule => -rule.Value.Constant / rule.Slope).DefaultIfEmpty(double.PositiveInfinity).Min();
            pinned[group.Key] = group.Key < count && high - low < 2;
        }

        return pinned;
    }

    private static MemberKind Kind(ContentSlot slot, int member) => member < slot.Numbers.Length ? slot.Numbers[member].Kind : MemberKind.Number;

    /// <summary>
    /// A point where every rule holds, moving each rule's primary member as far as it needs: a whole number to the middle
    /// of the range its rules leave, a flag to its value, a number 50 past its line; null when they cannot all hold.
    /// </summary>
    private static ProbePoint? Satisfy(ProbePoint point, List<VisibilityRule> rules, ContentSlot slot)
    {
        foreach (var group in rules.Where(rule => rule.Single && Kind(slot, rule.Primary) == MemberKind.Whole).GroupBy(rule => rule.Primary))
        {
            var low = group.Where(rule => rule.Slope > 0).Select(rule => -rule.Value.Constant / rule.Slope).DefaultIfEmpty(-1000).Max();
            var high = group.Where(rule => rule.Slope < 0).Select(rule => -rule.Value.Constant / rule.Slope).DefaultIfEmpty(low + 100).Min();
            point.Set(group.Key, Math.Round((low + high) / 2));
        }

        for (var round = 0; round < 3; round++)
        {
            var moved = false;
            foreach (var rule in rules.Where(rule => !Holds(rule, point, slot)))
            {
                var primary = rule.Primary;
                point.Set(primary, Kind(slot, primary) == MemberKind.Flag
                    ? (rule.Slope > 0 ? 1 : 0)
                    : point.Numbers[primary] + ((50 - rule.At(point)) / rule.Slope));
                moved = true;
            }

            if (!moved)
            {
                return point;
            }
        }

        return rules.TrueForAll(rule => rule.Holds(point)) ? point : null;
    }

    private static bool Holds(VisibilityRule rule, ProbePoint point, ContentSlot slot) =>
        Kind(slot, rule.Primary) is MemberKind.Flag || (rule.Single && Kind(slot, rule.Primary) == MemberKind.Whole) ? rule.Holds(point) : rule.At(point) >= 25;

    /// <summary>A check point: every number member that is not pinned moved by its own amount, the moves shrunk until the region's rules still hold.</summary>
    private static ProbePoint Check(ProbePoint start, ContentSlot slot, bool[] pinned, int seed, List<VisibilityRule> rules)
    {
        for (var scale = 1.0; scale > 1e-3; scale /= 4)
        {
            var point = start.Copy(slot);
            for (var i = 0; i < pinned.Length; i++)
            {
                var kind = Kind(slot, i);
                if (kind != MemberKind.Flag && !pinned[i])
                {
                    var step = (((i * 7) + (seed * 5)) % 11) - 5 + (seed * 0.5);
                    point.Set(i, start.Numbers[i] + (kind == MemberKind.Whole ? Math.Round(step * scale) : step * 3.7 * scale));
                }
            }

            if (rules.TrueForAll(rule => rule.Holds(point)))
            {
                return point;
            }
        }

        return start.Copy(slot);
    }

    /// <summary>Refuses a region where a node of its group is hidden in one of its probes: its visibility was not understood.</summary>
    private static void Visible(ProbeRegion region, int node, SceneNode root)
    {
        _ = root;
        foreach (var (_, nodes) in region.All())
        {
            if (!nodes[node].Visible)
            {
                throw new ContentRefusedException($"the visibility of the template's {nodes[node].Origin} changes in a way Pictor cannot follow");
            }
        }
    }

    /// <summary>The content node of a laid-out node and the nodes below it, with its fitted props, its condition, and SVG's defaults left out.</summary>
    private static ContentNode? Assemble(SceneNode scene, ImmutableArray<ContentProp>[] props, List<VisibilityRule>?[] rules, bool[] never, List<ProbePoint> samples, ref int index)
    {
        var at = index++;
        var children = new List<ContentNode>();
        foreach (var child in scene.Children)
        {
            if (Assemble(child, props, rules, never, samples, ref index) is { } node)
            {
                children.Add(node);
            }
        }

        if (never[at])
        {
            return null;
        }

        return new ContentNode
        {
            Element = scene.Element,
            Class = scene.Class,
            Attributes = Defaults.Leave(scene.Element, props[at]),
            Children = Defaults.Switches([.. children], samples),
            When = rules[at] is { } own ? [.. own.Select(rule => rule.Condition)] : [],
        };
    }
}

/// <summary>What the compiler leaves out of compiled content: props at SVG's defaults, and groups that only choose one of their children.</summary>
internal static class Defaults
{
    public static ImmutableArray<ContentProp> Leave(ContentElement element, ImmutableArray<ContentProp> props)
    {
        var kept = ImmutableArray.CreateBuilder<ContentProp>(props.Length);
        foreach (var prop in props)
        {
            if (prop.Name == ContentNames.Transform)
            {
                if (Transform(prop) is { } transform)
                {
                    kept.Add(transform);
                }

                continue;
            }

            if (!IsDefault(element, prop))
            {
                kept.Add(prop);
            }
        }

        return kept.ToImmutable();
    }

    private static bool IsDefault(ContentElement element, ContentProp prop) => prop.Name switch
    {
        "opacity" or "fill-opacity" or "stroke-opacity" => Constant(prop, 1),
        "dx" or "dy" or "rx" or "ry" => Constant(prop, 0),
        "stroke-width" => element is not ContentElement.Sector and not ContentElement.Dot and not ContentElement.Rectangle and not ContentElement.Symbols && Constant(prop, 1),
        _ => false,
    };

    private static bool Constant(ContentProp prop, double value) =>
        prop.Numbers.Length == 1 && prop.Numbers[0].Members.IsDefaultOrEmpty && prop.Numbers[0].Constant == value;

    /// <summary>A transform without its operations that do nothing (a translation by 0, a rotation by 0), or null when none is left.</summary>
    private static ContentProp? Transform(ContentProp prop)
    {
        var operations = new System.Text.StringBuilder();
        var numbers = ImmutableArray.CreateBuilder<ContentNumber>();
        var n = 0;
        foreach (var operation in prop.Text ?? string.Empty)
        {
            var arity = ContentNames.TransformArity(operation);
            var part = prop.Numbers.Skip(n).Take(arity).ToList();
            n += arity;
            var idle = operation switch
            {
                't' => part.TrueForAll(number => number.Members.IsDefaultOrEmpty && number.Constant == 0),
                'r' => part[0].Members.IsDefaultOrEmpty && part[0].Constant == 0,
                _ => false,
            };
            if (!idle)
            {
                operations.Append(operation);
                numbers.AddRange(part);
            }
        }

        return operations.Length == 0 ? null : prop with { Text = operations.ToString(), Numbers = numbers.ToImmutable() };
    }

    /// <summary>
    /// The children of a group, a group that only chooses one of its children (each shown on its own conditions, never
    /// two at once at any of the probes' points) replaced by them, as the alternatives it chooses between.
    /// </summary>
    public static ImmutableArray<ContentNode> Switches(ImmutableArray<ContentNode> children, List<ProbePoint> samples)
    {
        var flat = ImmutableArray.CreateBuilder<ContentNode>(children.Length);
        foreach (var child in children)
        {
            if (child is { Element: ContentElement.Group, Class: null, When.IsEmpty: true, Attributes.IsEmpty: true, Children.IsEmpty: false }
                && child.Children.All(inner => !inner.When.IsEmpty)
                && samples.TrueForAll(point => child.Children.Count(inner => inner.When.All(when => Holds(when, point))) <= 1))
            {
                flat.AddRange(child.Children);
            }
            else
            {
                flat.Add(child);
            }
        }

        return flat.ToImmutable();
    }

    private static bool Holds(ContentCondition when, ProbePoint point) =>
        ContentFit.Evaluate(when.Value, point) is var value && (when.Inclusive ? value >= 0 : value > 0);
}
