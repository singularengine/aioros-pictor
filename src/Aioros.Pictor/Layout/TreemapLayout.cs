using Aioros.Pictor.Data;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Layout;

/// <summary>A treemap node laid out: its rectangle, its value, its depth and index among its siblings, its children.</summary>
internal sealed class TreemapNode
{
    public string? Name { get; init; }

    /// <summary>The name the tooltip shows: the item's value at the treemap's <c>nameKey</c>, empty without one (Recharts' <c>Treemap</c> declares no default).</summary>
    public string TipName { get; init; } = string.Empty;

    public double Value { get; set; }

    public int Depth { get; init; }

    public int Index { get; init; }

    public TreemapNode[]? Children { get; init; }

    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public double Area { get; set; }

    /// <summary>Whether the node was drawn before an update of its chart at the same place with the same name (Recharts' node key), so it resizes instead of sliding in.</summary>
    public bool Kept { get; init; }
}

/// <summary>A box of a nest treemap's index (its breadcrumb): the node it zooms to (its path, the root's empty), its name, and whether it is on the zoomed node's trail.</summary>
internal readonly record struct NestBox(string Path, string Name, bool Shown);

/// <summary>A node of a treemap's whole data, in depth-first order: its path from the root, its name and whether it has children.</summary>
internal readonly record struct TreemapEntry(string Path, string? Name, bool Leaf, string Crumb);

/// <summary>
/// A treemap laid out as Recharts lays it out (<c>computeNode</c>, then <c>squarify</c>): every node's value is its
/// own or its children's sum, and each node's children are squarified into its rectangle in rows of whole pixels.
/// </summary>
internal sealed class TreemapPlot : IChartPlot
{
    private TreemapPlot(ChartSpec spec, TreemapSpec treemap, TreemapNode root, string zoom, TreemapEntry[] entries)
    {
        Spec = treemap.Nest ? spec with { Height = spec.Height - 30 } : spec;
        Treemap = treemap;
        Root = root;
        var hovered = new List<TreemapNode>();
        Shown(root, treemap.Nest, hovered);
        Hovered = [.. hovered];
        Colors = !treemap.ColorPanel.IsDefaultOrEmpty ? treemap.ColorPanel : Theming.Palettes.Of(spec.Colors);
        Zoom = zoom;
        Entries = entries;
        Trail = treemap.Nest ? [new NestBox(string.Empty, "root", true), .. entries.Where(entry => !entry.Leaf).Select(entry => new NestBox(entry.Path, entry.Crumb, OnTrail(entry.Path, zoom)))] : [];
    }

    private TreemapPlot(TreemapPlot source, TreemapNode root)
    {
        (Spec, Treemap, Root, Hovered, Colors) = (source.Spec, source.Treemap, root, source.Hovered, source.Colors);
        (Zoom, Entries, Trail) = (source.Zoom, source.Entries, source.Trail);
    }

    /// <summary>The zoomed node of a nest treemap (Recharts' <c>currentRoot</c>), as its path from the root; empty for the root.</summary>
    public string Zoom { get; }

    /// <summary>Every node of the whole data but the root, in depth-first order, whatever the zoom.</summary>
    public TreemapEntry[] Entries { get; }

    /// <summary>The boxes of a nest treemap's index: the root's, then one per node with children in depth-first order, shown on the zoomed node's trail (Recharts' <c>nestIndex</c>).</summary>
    public NestBox[] Trail { get; }

    /// <summary>The laid-out nodes by their path from the whole data's root, indexed when first asked for (by click targets).</summary>
    public Dictionary<string, TreemapNode> Paths
    {
        get
        {
            if (field is null)
            {
                field = [];
                Index(Root, Zoom, field);
            }

            return field;
        }
    }

    private static bool OnTrail(string path, string zoom) =>
        zoom.Length > 0 && (zoom == path || zoom.StartsWith(path + ".", StringComparison.Ordinal));

    private static void Index(TreemapNode node, string path, Dictionary<string, TreemapNode> paths)
    {
        if (node.Children is null)
        {
            return;
        }

        foreach (var child in node.Children)
        {
            var childPath = path.Length == 0 ? child.Index.ToString(System.Globalization.CultureInfo.InvariantCulture) : path + "." + child.Index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            paths[childPath] = child;
            Index(child, childPath, paths);
        }
    }

    /// <summary>The colors the nodes down to depth 1 take in turn: the treemap's own panel, or the theme's palette.</summary>
    public System.Collections.Immutable.ImmutableArray<string> Colors { get; }

    /// <summary>The chart's spec; a nest treemap's surface is 30 px shorter, as Recharts leaves room for its nest index.</summary>
    public ChartSpec Spec { get; }

    public TreemapSpec Treemap { get; }

    public TreemapNode Root { get; }

    /// <summary>The nodes that show a tooltip, in the order they are drawn: the leaves, or a nest treemap's shown nodes.</summary>
    public TreemapNode[] Hovered { get; }

    public int HoverCount => Spec.Tooltip is null ? 0 : Hovered.Length;

    /// <summary>The same chart drawing another tree of nodes (a frame of an update animation).</summary>
    public TreemapPlot With(TreemapNode root) => new(this, root);

    public static TreemapPlot Compute(ChartSpec spec, ChartData data)
    {
        var treemap = spec.Treemap ?? new TreemapSpec();
        var entries = new List<TreemapEntry>();
        var leaves = spec.ClickCommand is not null || spec.ItemClickCommand is not null;
        if (treemap.Nest || leaves)
        {
            Walk(data, treemap, string.Empty, entries, leaves);
        }

        var (shown, zoom, name) = treemap.Nest ? Zoomed(data, treemap) : (data, string.Empty, null);
        var root = new TreemapNode
        {
            Name = name,
            Depth = 0,
            Index = 0,
            Children = shown.Count > 0 ? Children(shown, treemap, 1) : null,
            Width = spec.Width,
            Height = spec.Height,
        };
        root.Value = root.Children?.Sum(child => child.Value) ?? 0;
        Squarify(root, treemap.AspectRatio);
        return new TreemapPlot(spec, treemap, root, zoom, [.. entries]);
    }

    /// <summary>The children a nest treemap shows: the zoomed node's (Recharts lays the clicked node out as the root), as far as its path is valid.</summary>
    private static (ChartData Shown, string Path, string? Name) Zoomed(ChartData data, TreemapSpec treemap)
    {
        var (shown, path, name) = (data, string.Empty, (string?)null);
        foreach (var part in (treemap.Zoom ?? string.Empty).Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var key = shown.Resolve("children");
            if (!int.TryParse(part, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var index) || index >= shown.Count || shown.Nested(index, key).Count == 0)
            {
                break;
            }

            name = shown.Texts(shown.Resolve("name"))[index];
            shown = shown.Nested(index, key);
            path = path.Length == 0 ? part : path + "." + part;
        }

        return (shown, path, name);
    }

    /// <summary>The whole data's nodes in depth-first order, the leaves only for click targets (the nest index names the nodes with children).</summary>
    private static void Walk(ChartData data, TreemapSpec treemap, string path, List<TreemapEntry> entries, bool leaves)
    {
        var names = data.Texts(data.Resolve("name"));
        var crumbs = treemap.NameKey is { } nameKey ? data.Texts(data.Resolve(nameKey)) : null;
        var key = data.Resolve("children");
        for (var i = 0; i < data.Count; i++)
        {
            var nested = data.Nested(i, key);
            if (nested.Count == 0 && !leaves)
            {
                continue;
            }

            var at = path.Length == 0 ? i.ToString(System.Globalization.CultureInfo.InvariantCulture) : path + "." + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            entries.Add(new TreemapEntry(at, names[i], nested.Count == 0, crumbs?[i] ?? "root"));
            if (nested.Count > 0)
            {
                Walk(nested, treemap, at, entries, leaves);
            }
        }
    }

    /// <summary>The nodes that show a tooltip in depth-first order: the leaves, or a nest treemap's nodes down to depth 1.</summary>
    private static void Shown(TreemapNode node, bool nest, List<TreemapNode> shown)
    {
        if (nest ? node.Depth <= 1 : node.Children is null)
        {
            shown.Add(node);
        }

        if (node.Children is null || (nest && node.Depth >= 1))
        {
            return;
        }

        foreach (var child in node.Children)
        {
            Shown(child, nest, shown);
        }
    }

    private static TreemapNode[] Children(ChartData data, TreemapSpec treemap, int depth)
    {
        var names = data.Texts(data.Resolve("name"));
        var tips = treemap.NameKey is { } nameKey ? data.Texts(data.Resolve(nameKey)) : null;
        var values = data.Numbers(data.Resolve(treemap.DataKey));
        var key = data.Resolve("children");
        var nodes = new TreemapNode[data.Count];
        for (var i = 0; i < nodes.Length; i++)
        {
            var nested = data.Nested(i, key);
            var children = nested.Count > 0 ? Children(nested, treemap, depth + 1) : null;
            nodes[i] = new TreemapNode
            {
                Name = names[i],
                TipName = tips?[i] ?? string.Empty,
                Depth = depth,
                Index = i,
                Children = children,
                Value = children is not null ? children.Sum(child => child.Value) : double.IsNaN(values[i]) || values[i] <= 0 ? 0 : values[i],
            };
        }

        return nodes;
    }

    private static void Squarify(TreemapNode node, double aspectRatio)
    {
        if (node.Children is not { Length: > 0 } children)
        {
            return;
        }

        var rect = (node.X, node.Y, node.Width, node.Height);
        var ratio = Math.Max(rect.Width * rect.Height / node.Value, 0);
        foreach (var child in children)
        {
            var area = child.Value * ratio;
            child.Area = double.IsNaN(area) || area <= 0 ? 0 : area;
        }

        var row = new List<TreemapNode>(children.Length);
        var rowArea = 0.0;
        var best = double.PositiveInfinity;
        var size = Math.Min(rect.Width, rect.Height);
        var next = 0;
        while (next < children.Length)
        {
            var child = children[next];
            row.Add(child);
            rowArea += child.Area;
            var score = WorstScore(row, rowArea, size, aspectRatio);
            if (score <= best || row.Count == 1)
            {
                next++;
                best = score;
            }
            else
            {
                rowArea -= child.Area;
                row.RemoveAt(row.Count - 1);
                rect = Position(row, rowArea, size, rect, flush: false);
                size = Math.Min(rect.Width, rect.Height);
                row.Clear();
                rowArea = 0;
                best = double.PositiveInfinity;
            }
        }

        if (row.Count > 0)
        {
            Position(row, rowArea, size, rect, flush: true);
        }

        foreach (var child in children)
        {
            Squarify(child, aspectRatio);
        }
    }

    private static double WorstScore(List<TreemapNode> row, double area, double parentSize, double aspectRatio)
    {
        var parentArea = parentSize * parentSize;
        var rowArea = area * area;
        var min = double.PositiveInfinity;
        var max = 0.0;
        for (var i = 0; i < row.Count; i++)
        {
            min = Math.Min(min, row[i].Area);
            max = Math.Max(max, row[i].Area);
        }

        return rowArea != 0 && !double.IsNaN(rowArea) ? Math.Max(parentArea * max * aspectRatio / rowArea, rowArea / (parentArea * min * aspectRatio)) : double.PositiveInfinity;
    }

    private static (double X, double Y, double Width, double Height) Position(List<TreemapNode> row, double area, double parentSize, (double X, double Y, double Width, double Height) rect, bool flush)
    {
        if (row.Count == 0)
        {
            return rect;
        }

        return parentSize == rect.Width ? Horizontal(row, area, parentSize, rect, flush) : Vertical(row, area, parentSize, rect, flush);
    }

    private static (double X, double Y, double Width, double Height) Horizontal(List<TreemapNode> row, double area, double parentSize, (double X, double Y, double Width, double Height) rect, bool flush)
    {
        var rowHeight = parentSize != 0 ? JsRound(area / parentSize) : 0;
        if (flush || rowHeight > rect.Height)
        {
            rowHeight = rect.Height;
        }

        var x = rect.X;
        foreach (var child in row)
        {
            child.X = x;
            child.Y = rect.Y;
            child.Height = rowHeight;
            child.Width = Math.Min(rowHeight != 0 ? JsRound(child.Area / rowHeight) : 0, rect.X + rect.Width - x);
            x += child.Width;
        }

        row[^1].Width += rect.X + rect.Width - x;
        return (rect.X, rect.Y + rowHeight, rect.Width, rect.Height - rowHeight);
    }

    private static (double X, double Y, double Width, double Height) Vertical(List<TreemapNode> row, double area, double parentSize, (double X, double Y, double Width, double Height) rect, bool flush)
    {
        var rowWidth = parentSize != 0 ? JsRound(area / parentSize) : 0;
        if (flush || rowWidth > rect.Width)
        {
            rowWidth = rect.Width;
        }

        var y = rect.Y;
        foreach (var child in row)
        {
            child.X = rect.X;
            child.Y = y;
            child.Width = rowWidth;
            child.Height = Math.Min(rowWidth != 0 ? JsRound(child.Area / rowWidth) : 0, rect.Y + rect.Height - y);
            y += child.Height;
        }

        row[^1].Height += rect.Y + rect.Height - y;
        return (rect.X + rowWidth, rect.Y, rect.Width - rowWidth, rect.Height);
    }

    private static double JsRound(double value) => Math.Floor(value + 0.5);
}

/// <summary>Where a nest treemap's index places its shown boxes: inline, centered, each its text plus 7 px each side and 3 px apart (Recharts' styles).</summary>
internal static class NestBoxes
{
    public static List<(NestBox Box, double Left, double Width)> Place(NestBox[] trail, double fontSize, double width, Func<string, double> measure)
    {
        var placed = new List<(NestBox Box, double Left, double Width)>();
        var total = 0.0;
        foreach (var box in trail)
        {
            if (box.Shown)
            {
                var size = measure(box.Name) + 14;
                placed.Add((box, total, size));
                total += size + 3;
            }
        }

        var left = (width - total) / 2;
        for (var i = 0; i < placed.Count; i++)
        {
            placed[i] = (placed[i].Box, placed[i].Left + left, placed[i].Width);
        }

        return placed;
    }
}
