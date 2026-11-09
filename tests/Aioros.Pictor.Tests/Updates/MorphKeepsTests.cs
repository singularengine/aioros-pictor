using System.Text.RegularExpressions;
using Aioros.Pictor.Demo;

namespace Aioros.Pictor.Tests.Updates;

public sealed partial class MorphKeepsTests
{
    [Fact]
    [Trait("AC", "UPDATE-17")]
    public async Task AReRenderedChartKeepsItsElementsSoNoEnterAnimationRestartsAsync()
    {
        var example = await Reference.ExampleAsync("BrushWindowBarChart");
        var spec = ExampleAnimations.Animated(example.Spec);
        var before = Reference.Pictor(example with { Spec = spec });
        var after = Reference.Pictor(example with { Spec = spec with { Brush = spec.Brush! with { EndIndex = 16 } } });

        var (replaced, added, restarted) = Morph(Tree(before), Tree(after));

        Assert.Equal(0, replaced);
        Assert.Equal(0, restarted);
        var moving = added.Where(element => Moves(element).Length > 0).Select(Shape).ToList();
        Assert.Equal(2, moving.Count);
        Assert.All(moving, shape => Assert.Matches("^path pictor-grow-[bt]$", shape));
    }

    [Fact]
    [Trait("AC", "UPDATE-25")]
    public async Task ASwitchedThemeKeepsEveryElementOfTheChartAsync()
    {
        var examples = await RechartsExamples.LoadAsync(Path.Combine(Reference.Root, "tools", "recharts", "examples"), TestContext.Current.CancellationToken);
        var failures = new List<string>();
        foreach (var example in examples.Where(example => example.Spec.Tooltip is not null))
        {
            var spec = ExampleAnimations.Animated(example.Spec);
            var light = Reference.Pictor(example with { Spec = spec });
            var dark = Reference.Pictor(example with { Spec = spec with { Theme = Theming.ChartTheme.Dark } });
            var (replaced, added, restarted) = Morph(Tree(light), Tree(dark));
            var classes = Classes().Matches(light).Select(match => match.Value).SequenceEqual(Classes().Matches(dark).Select(match => match.Value));
            if (replaced > 0 || added.Count > 0 || restarted > 0 || !classes)
            {
                failures.Add($"{example.Name}: {replaced} replaced, {added.Count} added, {restarted} restarted, same classes {classes}");
            }
        }

        Assert.Empty(failures);
    }

    /// <summary>
    /// Aioros's morph (the framework script's <c>m</c> and <c>c</c>): an element keeps its place when its tag matches,
    /// its attributes are updated, children are compared by position, and an extra child is appended. Counts the
    /// elements replaced, lists the ones added, and counts the kept elements whose animation class changes (which would
    /// restart a CSS animation).
    /// </summary>
    private static (int Replaced, List<string> Added, int Restarted) Morph(Node a, Node b)
    {
        var (replaced, restarted) = (0, 0);
        var added = new List<string>();
        void Walk(Node x, Node y)
        {
            if (x.Name != y.Name)
            {
                replaced++;
                return;
            }

            restarted += Moves(x.Open) != Moves(y.Open) ? 1 : 0;
            for (var i = 0; i < y.Children.Count; i++)
            {
                if (i < x.Children.Count)
                {
                    Walk(x.Children[i], y.Children[i]);
                }
                else
                {
                    Flatten(y.Children[i], added);
                }
            }
        }

        Walk(a, b);
        return (replaced, added, restarted);
    }

    private static void Flatten(Node node, List<string> into)
    {
        into.Add(node.Open);
        foreach (var child in node.Children)
        {
            Flatten(child, into);
        }
    }

    private static string Moves(string open) => string.Join(' ', Move().Matches(open).Select(match => match.Value));

    private static string Shape(string open) => Tag().Match(open).Groups[1].Value + " " + Moves(open);

    private static Node Tree(string markup)
    {
        var root = new Node("#root", string.Empty);
        var stack = new Stack<Node>([root]);
        foreach (Match tag in Element().Matches(markup))
        {
            if (tag.Groups[1].Value == "/")
            {
                stack.Pop();
                continue;
            }

            var node = new Node(tag.Groups[2].Value, tag.Value);
            stack.Peek().Children.Add(node);
            if (!tag.Value.EndsWith("/>", StringComparison.Ordinal) && tag.Groups[2].Value is not ("input" or "br"))
            {
                stack.Push(node);
            }
        }

        return root;
    }

    private sealed record Node(string Name, string Open)
    {
        public List<Node> Children { get; } = [];
    }

    [GeneratedRegex("<(/?)([a-zA-Z][\\w-]*)[^>]*>")]
    private static partial Regex Element();

    [GeneratedRegex("<([a-zA-Z][\\w-]*)")]
    private static partial Regex Tag();

    [GeneratedRegex(" class=\"[^\"]*\"")]
    private static partial Regex Classes();

    [GeneratedRegex("pictor-(?:draw|reveal|reveal-down|grow-[btlr]|scale|scale-area|center|slide|late|shown|converge|arc|edge)\\b")]
    private static partial Regex Move();
}
