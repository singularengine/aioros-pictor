using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aioros.Pictor.BoundValues;
using Aioros.Pictor.Specs;
using Aioros.Runtime.Rendering;
using Avalonia;
using Avalonia.Data;

namespace Aioros.Pictor;

/// <summary>A bound property of a chart or of one of its children, as the web follows it: the slot part the plan reads, and the holes of the spec its value fills (one per member it fills for a thickness; none when it does not change the spec).</summary>
internal sealed record BoundPart(SlotPart Part, ImmutableArray<SpecHole> Holes);

/// <summary>
/// Finds the bound properties of a chart and of the Avalonia objects it holds (its elements, their parts, three levels
/// deep as Aioros's plan walks them) and where each value lands in the chart's spec: the spec is built again with a
/// marker value set at animation priority (which leaves the binding in place and is removed after), the places where the
/// spec differs are the property's holes, and a hole is kept only when writing the property's current value into it
/// gives the traced spec back and the marker into it gives the marked one (the value is copied there, not transformed).
/// A property bound both ways by default is not marked (the marker would be written into the model): the chart names
/// its hole itself, or it is not followed. A property whose binding wrote the marker back anyway (an explicit two-way
/// binding) gets its value back and is not followed. A property typed <c>object</c> (a series' paint, a dash pattern, a
/// radius) is tried with a brush, then a list of numbers, then a text, and a value made of numbers (a thickness) with a
/// distinct number per member, each place it fills then a hole reading that member.
/// A property that passes is declared as a part the plan follows; one that does not is left undeclared, so the plan
/// still refuses it with its name rather than writing it frozen. Precedent: Lit's template markers, which find the
/// holes of a template once by rendering it with unique values.
/// </summary>
internal static class BoundParts
{
    private const string TextMarker = "\u0001pictor\u0001";
    private const double NumberMarker = 9.87654321e299;
    private const int WholeMarker = 1234567891;
    private static readonly double[] MemberMarkers = [9.87654321e299, 8.7654321e298, 7.654321e297, 6.54321e296];

    /// <summary>The bound parts of a chart, each declared by <paramref name="declare"/> (a value read, or a field the chart writes) from the key it is given.</summary>
    public static ImmutableArray<BoundPart> Find(Chart chart, Func<ChartSpec> build, Data.DataMembers members, Func<AvaloniaObject, AvaloniaProperty, string, SlotPart> declare, Func<AvaloniaObject, AvaloniaProperty, (HoleKind Kind, string Path)?> known)
    {
        var found = ImmutableArray.CreateBuilder<BoundPart>();
        string? traced = null;
        foreach (var (target, property) in Bound(chart))
        {
            traced ??= Bare(build());
            var part = declare(target, property, "p" + found.Count.ToString(CultureInfo.InvariantCulture));
            var holes = known(target, property) is { } place ? Known(part, place.Kind, place.Path, target.GetValue(property), traced, members) : Holes(target, property, part, traced, build, members);
            if (holes is { } bound)
            {
                found.Add(new BoundPart(part, bound));
            }
        }

        return found.ToImmutable();
    }

    /// <summary>The bound properties a chart's spec reads: the chart's own (those Pictor declares) and every property of the objects it holds.</summary>
    private static IEnumerable<(AvaloniaObject Target, AvaloniaProperty Property)> Bound(Chart chart)
    {
        foreach (var property in AvaloniaPropertyRegistry.Instance.GetRegistered(chart))
        {
            if (property != Chart.DataProperty && property.OwnerType.Namespace == typeof(Chart).Namespace && IsBound(chart, property))
            {
                yield return (chart, property);
            }
        }

        foreach (var element in chart.Elements.OfType<AvaloniaObject>())
        {
            foreach (var found in Held(element, 1))
            {
                yield return found;
            }
        }
    }

    private static IEnumerable<(AvaloniaObject Target, AvaloniaProperty Property)> Held(AvaloniaObject target, int depth)
    {
        foreach (var property in AvaloniaPropertyRegistry.Instance.GetRegistered(target))
        {
            if (IsBound(target, property))
            {
                yield return (target, property);
            }

            if (depth >= 3 || property.IsDirect)
            {
                continue;
            }

            foreach (var held in Children(target.GetValue(property)).SelectMany(held => Held(held, depth + 1)))
            {
                yield return held;
            }
        }
    }

    private static IEnumerable<AvaloniaObject> Children(object? value) => value switch
    {
        Visual => [],
        AvaloniaObject held => [held],
        Avalonia.Collections.IAvaloniaReadOnlyList<object> items => items.OfType<AvaloniaObject>().Where(item => item is not Visual),
        _ => [],
    };

    /// <summary>
    /// Whether a property is bound to data as Aioros's plan sees it, through Avalonia's public API only: a binding
    /// expression holding the property's value at the local priority (a template binding holds it at the template's, a
    /// style's binding at the style's), which is not a multi binding, an indexer binding or a dynamic resource. Avalonia's
    /// expression types are internal; their public descriptions (<see cref="IDescription"/>) are the only public trace of
    /// their kind.
    /// </summary>
    private static bool IsBound(AvaloniaObject target, AvaloniaProperty property) =>
        BindingOperations.GetBindingExpressionBase(target, property) is IDescription expression
        && Avalonia.Diagnostics.AvaloniaObjectExtensions.GetDiagnostic(target, property).Priority == BindingPriority.LocalValue
        && !Array.Exists(NotData, kind => expression.Description?.StartsWith(kind, StringComparison.Ordinal) == true);

    /// <summary>The descriptions Avalonia gives the binding expressions that do not read data.</summary>
    private static readonly string[] NotData = ["MultiBinding", "IndexerBinding", "DynamicResource", "TemplateBinding"];

    private static ImmutableArray<SpecHole>? Holes(AvaloniaObject target, AvaloniaProperty property, SlotPart part, string traced, Func<ChartSpec> build, Data.DataMembers members)
    {
        var current = target.GetValue(property);
        var probes = Probes(property.PropertyType, current);
        if (probes.Count == 0 || property.GetMetadata(target.GetType()).DefaultBindingMode is BindingMode.TwoWay or BindingMode.OneWayToSource)
        {
            return null;
        }

        var original = JsonNode.Parse(traced)!.AsObject();
        var read = false;
        foreach (var probe in probes)
        {
            var (marked, wroteBack) = Mark(target, property, current, probe.Marker, build);
            if (wroteBack)
            {
                return null;
            }

            if (marked is not null && Differences(original, marked) is { Count: > 0 } paths)
            {
                read = true;
                if (Followed(part.Key, probe, original, traced, marked, paths, current, members) is { } holes)
                {
                    return holes;
                }
            }
        }

        return !read && Unread(target, property, probes[0], current, traced, build) ? [] : null;
    }

    /// <summary>The places where a marked spec differs from the traced one.</summary>
    private static List<string> Differences(JsonObject original, string marked)
    {
        var paths = new List<string>();
        Diff(original, JsonNode.Parse(marked)!.AsObject(), string.Empty, paths);
        return paths;
    }

    /// <summary>The holes of the first of a probe's kinds that writes the current value and the marker back where the specs differ.</summary>
    private static ImmutableArray<SpecHole>? Followed(string key, Probe probe, JsonObject original, string traced, string marked, List<string> paths, object? current, Data.DataMembers members)
    {
        var changed = JsonNode.Parse(marked)!.AsObject();
        foreach (var kind in probe.Kinds)
        {
            if (Placed(key, kind, probe, original, changed, paths) is { } holes
                && Reproduces(holes, original, traced, current, members) && Reproduces(holes, original, marked, probe.Marker, members))
            {
                return holes;
            }
        }

        return null;
    }

    /// <summary>The spec built with a marker bound over the property, and whether its binding wrote the marker back (an explicit two-way binding); none when the spec cannot be built with it.</summary>
    private static (string? Marked, bool WroteBack) Mark(AvaloniaObject target, AvaloniaProperty property, object? current, object marker, Func<ChartSpec> build)
    {
        string? marked;
        using (target.Bind(property, new Marking(marker), BindingPriority.Animation))
        {
            try
            {
                marked = Bare(build());
            }
            catch (FormatException)
            {
                marked = null;
            }
        }

        if (!Equals(target.GetValue(property), current))
        {
            target.SetCurrentValue(property, current);
            return (null, true);
        }

        return (marked, false);
    }

    /// <summary>
    /// The holes a marker's differences give for a kind: a table's (the nearest object holding <c>Columns</c>, with the keys
    /// both specs read), one per member for a value made of numbers (each place holding one member's marker), or one hole
    /// over every place; none when a place holds no member's marker.
    /// </summary>
    private static ImmutableArray<SpecHole>? Placed(string key, HoleKind kind, Probe probe, JsonObject original, JsonObject changed, List<string> paths)
    {
        if (probe.Members.IsDefaultOrEmpty)
        {
            if (kind != HoleKind.Table)
            {
                return [new SpecHole(key, kind, [.. paths])];
            }

            var tables = paths.Select(path => TableOf(original, changed, path)).Distinct(StringComparer.Ordinal).ToList();
            var keys = tables.SelectMany(path => Columns(original, path).Concat(Columns(changed, path))).Distinct(StringComparer.Ordinal);
            return [new SpecHole(key, kind, [.. tables]) { Keys = [.. keys] }];
        }

        var placed = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            var index = At(changed, path) is JsonValue value && value.TryGetValue<double>(out var number) ? Array.IndexOf(MemberMarkers, number) : -1;
            if (index < 0 || index >= probe.Members.Length)
            {
                return null;
            }

            if (!placed.TryGetValue(probe.Members[index], out var list))
            {
                list = [];
                placed[probe.Members[index]] = list;
            }

            list.Add(path);
        }

        return [.. probe.Members.Where(placed.ContainsKey).Select(member => new SpecHole(key, kind, [.. placed[member]]) { Member = member })];
    }

    /// <summary>
    /// Whether a property the markers left the spec unchanged with is truly not read: a second, ordinary value (a small
    /// number, a short whole number, a plain text, another member of an enumeration, another color, other numbers)
    /// leaves it unchanged too. A property clamped to a range (which the far marker hits at its bound) or read through a
    /// step changes the spec there, so it is left undeclared and the plan refuses it by name rather than writing it frozen.
    /// </summary>
    private static bool Unread(AvaloniaObject target, AvaloniaProperty property, Probe probe, object? current, string traced, Func<ChartSpec> build)
    {
        if (probe.Alternate is not { } alternate || Equals(alternate, current))
        {
            return true;
        }

        var (probed, wroteBack) = Mark(target, property, current, alternate, build);
        return !wroteBack && probed == traced;
    }

    /// <summary>A hole whose place the chart knows (a field bound both ways, which a marker would write into the model), kept when the current value written there gives the traced spec back.</summary>
    private static ImmutableArray<SpecHole>? Known(SlotPart part, HoleKind kind, string path, object? current, string traced, Data.DataMembers members)
    {
        ImmutableArray<SpecHole> holes = [new SpecHole(part.Key, kind, [path])];
        return Reproduces(holes, JsonNode.Parse(traced)!.AsObject(), traced, current, members) ? holes : null;
    }

    /// <summary>The spec's own JSON, without the theme the spec's text carries after it (the holes are in the spec).</summary>
    private static string Bare(ChartSpec spec) => SpecJson.Write(spec with { Theme = null });

    /// <summary>
    /// The markers a property's type is tried with, each with the kinds of hole its differences may be and an ordinary
    /// value that tells a property not read from one the markers miss; none for a type the web cannot follow.
    /// </summary>
    private static List<Probe> Probes(Type type, object? current)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(object))
        {
            List<Probe> probes = [new(BrushMarker, [HoleKind.Paint], OtherBrush), new(ListMarker(current), [HoleKind.Dashes, HoleKind.Numbers], OtherList)];
            return current is null or string ? [.. probes, new(TextMarker, [HoleKind.Text], "pictor")] : probes;
        }

        return underlying switch
        {
            _ when underlying == typeof(string) => [new(TextMarker, [HoleKind.Text], "pictor")],
            _ when underlying == typeof(double) => [new(NumberMarker, [HoleKind.Number], 0.123456789)],
            _ when underlying == typeof(int) => [new(WholeMarker, [HoleKind.Whole], 3)],
            _ when underlying == typeof(bool) => [new(current is not true, [HoleKind.Boolean], null)],
            _ when underlying == typeof(Thickness) => [MemberProbe(new Thickness(MemberMarkers[0], MemberMarkers[1], MemberMarkers[2], MemberMarkers[3]), new Thickness(1.5, 2.5, 3.5, 4.5), nameof(Thickness.Left), nameof(Thickness.Top), nameof(Thickness.Right), nameof(Thickness.Bottom))],
            _ when underlying == typeof(CornerRadius) => [MemberProbe(new CornerRadius(MemberMarkers[0], MemberMarkers[1], MemberMarkers[2], MemberMarkers[3]), new CornerRadius(1.5, 2.5, 3.5, 4.5), nameof(CornerRadius.TopLeft), nameof(CornerRadius.TopRight), nameof(CornerRadius.BottomRight), nameof(CornerRadius.BottomLeft))],
            _ when underlying == typeof(Point) => [MemberProbe(new Point(MemberMarkers[0], MemberMarkers[1]), new Point(1.5, 2.5), nameof(Point.X), nameof(Point.Y))],
            _ when underlying == typeof(Avalonia.Media.Color) => [new(BrushMarker.Color, [HoleKind.Paint], OtherBrush.Color)],
            _ when underlying.IsAssignableTo(typeof(Avalonia.Media.IBrush)) => [new(BrushMarker, [HoleKind.Paint], OtherBrush)],
            _ when underlying.IsEnum => Enumeration(underlying, current),
            _ when underlying.IsAssignableTo(typeof(IEnumerable)) => [new(current is IEnumerable items && items.Cast<object?>().Any() ? Array.Empty<object>() : new object[] { new() }, [HoleKind.Table], null)],
            _ => [],
        };
    }

    private static List<Probe> Enumeration(Type type, object? current)
    {
        var others = Enum.GetValuesAsUnderlyingType(type).Cast<object>().Select(value => Enum.ToObject(type, value)).Where(value => !value.Equals(current)).ToList();
        return others.Count == 0 ? [] : [new(others[0], [HoleKind.Name], others.Count > 1 ? others[1] : null)];
    }

    private static Probe MemberProbe(object marker, object alternate, params string[] members) => new(marker, [HoleKind.Number], alternate) { Members = [.. members] };

    /// <summary>A brush no chart paints with, and an ordinary one.</summary>
    private static readonly Avalonia.Media.Immutable.ImmutableSolidColorBrush BrushMarker = new(Avalonia.Media.Color.FromArgb(2, 254, 1, 253));

    private static readonly Avalonia.Media.Immutable.ImmutableSolidColorBrush OtherBrush = new(Avalonia.Media.Color.FromRgb(1, 2, 3));

    /// <summary>An ordinary list of numbers.</summary>
    private static readonly double[] OtherList = [2.5, 3.5];

    /// <summary>A list of numbers one longer than the current value's, so a number list's whole array differs and a dash pattern's text.</summary>
    private static double[] ListMarker(object? current)
    {
        int count;
        try
        {
            count = BoundLists.Numbers(AvaloniaValues.Plain(current, null))?.Length ?? 0;
        }
        catch (FormatException)
        {
            count = 0;
        }

        return [NumberMarker, .. Enumerable.Range(1, Math.Max(count, 1)).Select(i => i + 0.5)];
    }

    /// <summary>A marker bound over a property, the kinds of hole its differences may be, an ordinary value (or none), and the members it marks one by one.</summary>
    private sealed record Probe(object Marker, HoleKind[] Kinds, object? Alternate)
    {
        public ImmutableArray<string> Members { get; init; }
    }

    private static void Diff(JsonNode? left, JsonNode? right, string path, List<string> paths)
    {
        if (JsonNode.DeepEquals(left, right))
        {
            return;
        }

        if (left is JsonObject leftObject && right is JsonObject rightObject)
        {
            foreach (var name in leftObject.Select(pair => pair.Key).Union(rightObject.Select(pair => pair.Key)))
            {
                Diff(leftObject[name], rightObject[name], Pointer(path, name), paths);
            }

            return;
        }

        if (left is JsonArray leftArray && right is JsonArray rightArray && leftArray.Count == rightArray.Count)
        {
            for (var i = 0; i < leftArray.Count; i++)
            {
                Diff(leftArray[i], rightArray[i], Pointer(path, i.ToString(CultureInfo.InvariantCulture)), paths);
            }

            return;
        }

        paths.Add(path);
    }

    /// <summary>A JSON pointer one segment deeper.</summary>
    private static string Pointer(string path, string segment) => string.Concat(path, "/", segment);

    /// <summary>The path of the table a difference lies in: the nearest object holding <c>Columns</c>, in either spec.</summary>
    private static string TableOf(JsonObject original, JsonObject changed, string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var length = 1; length <= segments.Length; length++)
        {
            var prefix = "/" + string.Join('/', segments[..length]);
            if (At(original, prefix) is JsonObject { } left && left.ContainsKey("Columns") || At(changed, prefix) is JsonObject { } right && right.ContainsKey("Columns"))
            {
                return prefix;
            }
        }

        return path;
    }

    private static IEnumerable<string> Columns(JsonObject spec, string path) =>
        At(spec, path)?["Columns"] is JsonObject columns ? columns.Select(pair => pair.Key) : [];

    private static JsonNode? At(JsonNode node, string path)
    {
        var current = (JsonNode?)node;
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = current switch
            {
                JsonArray array when int.TryParse(segment, CultureInfo.InvariantCulture, out var index) && index < array.Count => array[index],
                JsonObject item => item[segment],
                _ => null,
            };
        }

        return current;
    }

    /// <summary>Whether writing a value into the holes of the traced spec gives the expected spec's JSON.</summary>
    private static bool Reproduces(ImmutableArray<SpecHole> holes, JsonObject original, string expected, object? value, Data.DataMembers members)
    {
        try
        {
            return Bare(SpecHoles.Fill(original, holes, Enumerable.Repeat(value, holes.Length).ToArray(), members, AvaloniaValues.Plain)) == expected;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (InvalidCastException)
        {
            return false;
        }
    }

    /// <summary>A marker value bound at animation priority: it wins over the property's binding while it is held, and the binding's value comes back when it is disposed.</summary>
    private sealed class Marking(object marker) : IObservable<object?>
    {
        public IDisposable Subscribe(IObserver<object?> observer)
        {
            observer.OnNext(marker);
            return Released.Instance;
        }
    }

    private sealed class Released : IDisposable
    {
        public static Released Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
