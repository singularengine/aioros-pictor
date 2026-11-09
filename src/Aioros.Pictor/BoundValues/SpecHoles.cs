using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Aioros.Pictor.Data;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;
using Aioros.Runtime.Rendering;

namespace Aioros.Pictor.BoundValues;

/// <summary>How a bound value is written into a hole of a chart's spec.</summary>
public enum HoleKind
{
    /// <summary>A text (a color, a name, a format).</summary>
    Text,

    /// <summary>A number.</summary>
    Number,

    /// <summary>A whole number (a brush's index).</summary>
    Whole,

    /// <summary>A boolean.</summary>
    Boolean,

    /// <summary>An enumeration's member, by its name.</summary>
    Name,

    /// <summary>A series' own items, read into a <see cref="DataTable"/> of the hole's keys.</summary>
    Table,

    /// <summary>
    /// A paint: a color text as it is, or a color or a brush as the renderer's bound values give it (a color text, or a
    /// <see cref="GradientSpec"/> named as <c>url(#id)</c> and defined in the spec's gradients).
    /// </summary>
    Paint,

    /// <summary>A dash pattern: a text as it is, or a list of numbers separated by spaces (<see cref="BoundLists.Dashes"/>).</summary>
    Dashes,

    /// <summary>A number list: a text of numbers, a number or a list of numbers, or a value the renderer's bound values give as one (<see cref="BoundLists.Numbers(object)"/>).</summary>
    Numbers,
}

/// <summary>
/// How a renderer reads a bound value of a type the web path does not know (an Avalonia color, brush or thickness) as
/// one it does: a color text, a <see cref="GradientSpec"/>, a number list, or the number of one of its members (a side of
/// a thickness); any other value is given back as it is.
/// </summary>
/// <param name="value">The bound value.</param>
/// <param name="member">The member a hole reads, or null for the whole value.</param>
/// <returns>The plain value.</returns>
public delegate object? PlainValue(object? value, string? member);

/// <summary>A place in a chart's spec that a bound property of the chart or of one of its children fills: the slot part's key, how the value is written, and where.</summary>
/// <param name="Key">The key of the slot part that reads the value.</param>
/// <param name="Kind">How the value is written.</param>
/// <param name="Paths">The JSON pointers of the places the value fills (<c>/Series/0/Stroke</c>).</param>
public sealed record SpecHole(string Key, HoleKind Kind, ImmutableArray<string> Paths)
{
    /// <summary>The keys a table hole reads from the items.</summary>
    public ImmutableArray<string> Keys { get; init; } = [];

    /// <summary>The member of the bound value the hole holds (a side of a thickness, <c>Left</c>), or null for the whole value.</summary>
    public string? Member { get; init; }

    /// <summary>Whether the hole's value is compared by what it writes rather than by itself (a brush may change in place, a list is compared by its items).</summary>
    internal bool ComparedWritten => Kind is HoleKind.Paint or HoleKind.Dashes or HoleKind.Numbers || Member is not null;
}

/// <summary>A chart's spec with the holes its bound child properties fill, as a slot renderer's argument carries them.</summary>
public sealed record SpecArgument
{
    /// <summary>The holes.</summary>
    public ImmutableArray<SpecHole> Holes { get; init; } = [];

    /// <summary>The spec as the trace wrote it, with the traced values in its holes.</summary>
    public JsonObject? Spec { get; init; }

    /// <summary>The theme the spec is drawn with, carried beside it as the spec's JSON text sequence carries it.</summary>
    public Theming.ChartTheme? Theme { get; init; }
}

/// <summary>
/// The spec of a chart whose children have bound properties (a series' color, a pie's own data, a brush's indexes): a
/// template recorded when the page was traced, with holes where those values go. At request time the renderer reads
/// each bound value from the plan (<see cref="SlotOutput.TryRead"/>), fills the holes and writes the chart from the
/// spec they give; the spec and its output are kept while the values stay the same, so a request whose bound values did
/// not change costs one comparison per hole. Precedent: Lit's templates, whose holes are found once by marker values
/// and filled per render.
/// </summary>
public sealed class SpecHoles
{
    private readonly JsonObject? _template;
    private readonly ImmutableArray<SpecHole> _holes;
    private readonly DataMembers _members;
    private readonly PlainValue? _plain;
    private Filled? _filled;

    private SpecHoles(ChartSpec spec, JsonObject? template, ImmutableArray<SpecHole> holes, DataMembers members, PlainValue? plain = null)
    {
        Spec = spec;
        _template = template;
        _holes = holes;
        _members = members;
        _plain = plain;
    }

    /// <summary>The spec as traced.</summary>
    public ChartSpec Spec { get; }

    /// <summary>Whether no bound value fills the spec.</summary>
    public bool IsEmpty => _holes.IsEmpty;

    /// <summary>How many specs were built from new bound values.</summary>
    internal int Fills => Volatile.Read(ref _fills);

    private int _fills;

    /// <summary>The spec and holes an argument holds: a plain spec's JSON, or a <see cref="SpecArgument"/>.</summary>
    /// <param name="argument">The argument the chart recorded.</param>
    /// <param name="members">How items are read.</param>
    /// <returns>The holes.</returns>
    public static SpecHoles Read(string argument, DataMembers members) => Read(argument, members, null);

    /// <summary>The spec and holes an argument holds, its bound values read through <paramref name="plain"/>.</summary>
    internal static SpecHoles Read(string argument, DataMembers members, PlainValue? plain)
    {
        ArgumentNullException.ThrowIfNull(argument);
        if (!argument.StartsWith("{\"Holes\"", StringComparison.Ordinal))
        {
            return new SpecHoles(SpecJson.Read(argument), null, [], members);
        }

        var parsed = JsonSerializer.Deserialize(argument, SpecArgumentJson.Default.SpecArgument) ?? throw new JsonException("The chart argument is empty.");
        var template = parsed.Spec ?? throw new JsonException("The chart argument has no spec.");
        return new SpecHoles(template.Deserialize(SpecJsonContext.Default.ChartSpec)! with { Theme = parsed.Theme }, template, parsed.Holes, members, plain);
    }

    /// <summary>The argument of a spec and its holes.</summary>
    /// <param name="spec">The spec as traced.</param>
    /// <param name="holes">The holes.</param>
    /// <returns>The argument's text.</returns>
    public static string Write(ChartSpec spec, ImmutableArray<SpecHole> holes) => holes.IsDefaultOrEmpty
        ? SpecJson.Write(spec)
        : JsonSerializer.Serialize(new SpecArgument { Holes = holes, Spec = JsonSerializer.SerializeToNode(spec, SpecJsonContext.Default.ChartSpec)!.AsObject(), Theme = spec.Theme }, SpecArgumentJson.Default.SpecArgument);

    /// <summary>The JSON node a value takes in a hole of a kind, or null for none.</summary>
    /// <param name="kind">The hole's kind.</param>
    /// <param name="value">The bound value.</param>
    /// <param name="keys">A table's keys.</param>
    /// <param name="members">How items are read.</param>
    /// <returns>The node.</returns>
    public static JsonNode? Encode(HoleKind kind, object? value, ImmutableArray<string> keys, DataMembers members) => value is null ? null : kind switch
    {
        HoleKind.Paint => value switch
        {
            string text => JsonValue.Create(text),
            GradientSpec gradient => JsonValue.Create("url(#" + gradient.Id + ")"),
            _ => null,
        },
        HoleKind.Dashes => BoundLists.Dashes(value) is { } dashes ? JsonValue.Create(dashes) : null,
        HoleKind.Numbers => JsonSerializer.SerializeToNode(BoundLists.Numbers(value) ?? [], SpecJsonContext.Default.ImmutableArrayDouble),
        HoleKind.Number => JsonValue.Create(Convert.ToDouble(value, CultureInfo.InvariantCulture)),
        HoleKind.Whole => JsonValue.Create((int)Math.Round(Convert.ToDouble(value, CultureInfo.InvariantCulture))),
        HoleKind.Boolean => JsonValue.Create(value is true),
        HoleKind.Table => JsonSerializer.SerializeToNode(value as DataTable ?? DataTable.Read(value as IEnumerable, keys, members), SpecJsonContext.Default.DataTable),
        _ => JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture)),
    };

    /// <summary>The spec a template gives with values in its holes.</summary>
    /// <param name="template">The traced spec as JSON.</param>
    /// <param name="holes">The holes.</param>
    /// <param name="values">One value per hole.</param>
    /// <param name="members">How items are read.</param>
    /// <returns>The spec.</returns>
    public static ChartSpec Fill(JsonObject template, ImmutableArray<SpecHole> holes, ReadOnlySpan<object?> values, DataMembers members) =>
        Fill(template, holes, values, members, null);

    /// <summary>
    /// The spec a template gives with values in its holes, read through <paramref name="plain"/>: a paint hole's gradient
    /// is defined in the spec's gradients, and a gradient its traced value defined is left out once no paint names it.
    /// </summary>
    internal static ChartSpec Fill(JsonObject template, ImmutableArray<SpecHole> holes, ReadOnlySpan<object?> values, DataMembers members, PlainValue? plain)
    {
        ArgumentNullException.ThrowIfNull(template);
        var spec = template.DeepClone().AsObject();
        var painted = holes.Any(static hole => hole.Kind == HoleKind.Paint) ? new PaintedGradients(spec, holes) : null;
        for (var i = 0; i < holes.Length; i++)
        {
            if (values[i] is Traced)
            {
                continue;
            }

            var value = Plain(holes[i], values[i], plain);
            painted?.Add(value);
            foreach (var path in holes[i].Paths)
            {
                Set(spec, path, Encode(holes[i].Kind, value, holes[i].Keys, members));
            }
        }

        painted?.Define(spec);
        return spec.Deserialize(SpecJsonContext.Default.ChartSpec)!;
    }

    /// <summary>A hole's value as the web path reads it: through the renderer's bound values for a member or a paint, a dash pattern or a number list.</summary>
    private static object? Plain(SpecHole hole, object? value, PlainValue? plain) =>
        plain is not null && (hole.ComparedWritten || hole.Kind == HoleKind.Number) ? plain(value, hole.Member) : value;

    /// <summary>Writes the chart for the bound values of this render, through the output kept for them.</summary>
    /// <param name="output">The renderer's output.</param>
    public void Write(ref SlotOutput output)
    {
        var filled = Volatile.Read(ref _filled);
        if (filled is null || !filled.Holds(ref output, _holes, this))
        {
            var values = new object?[_holes.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = Value(ref output, _holes[i], _members);
            }

            Interlocked.Increment(ref _fills);
            filled = new Filled(values, Written(values), new ChartOutput(Fill(_template!, _holes, values, _members, _plain) with { Theme = Spec.Theme }, _members));
            Interlocked.Exchange(ref _filled, filled);
        }

        filled.Output.Write(ref output);
    }

    /// <summary>What each hole compared by what it writes writes for its value, null for the others.</summary>
    private string?[] Written(object?[] values)
    {
        var written = new string?[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            written[i] = _holes[i].ComparedWritten ? Writes(_holes[i], values[i]) : null;
        }

        return written;
    }

    /// <summary>What a hole writes for a value, as JSON text (a traced value, which leaves the hole alone, as none).</summary>
    private string? Writes(SpecHole hole, object? value)
    {
        if (value is Traced)
        {
            return null;
        }

        try
        {
            var plain = Plain(hole, value, _plain);
            return plain is GradientSpec gradient ? JsonSerializer.Serialize(gradient, SpecJsonContext.Default.GradientSpec) : Encode(hole.Kind, plain, hole.Keys, _members)?.ToJsonString() ?? "null";
        }
        catch (FormatException)
        {
            return "\u0001";
        }
    }

    /// <summary>A hole's value for this render: the bound value (a table hole's items read into their table, so a list changed in place is seen), or none when the plan does not read it.</summary>
    private static object? Value(ref SlotOutput output, SpecHole hole, DataMembers members)
    {
        if (!output.TryRead(hole.Key, out var value))
        {
            return Traced.Instance;
        }

        return hole.Kind == HoleKind.Table && value is not null ? DataTable.Read(value as IEnumerable, hole.Keys, members) : value;
    }

    private static void Set(JsonObject spec, string path, JsonNode? value)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        JsonNode node = spec;
        for (var i = 0; i < segments.Length - 1; i++)
        {
            node = node is JsonArray array ? array[int.Parse(segments[i], CultureInfo.InvariantCulture)]! : node[segments[i]]!;
        }

        var last = segments[^1];
        if (node is JsonArray items)
        {
            items[int.Parse(last, CultureInfo.InvariantCulture)] = value;
        }
        else if (value is null)
        {
            node.AsObject().Remove(last);
        }
        else
        {
            node[last] = value;
        }
    }

    /// <summary>A hole left with its traced value: the plan does not read it.</summary>
    private sealed class Traced
    {
        public static Traced Instance { get; } = new();
    }

    /// <summary>The values the holes were last filled with, what the holes compared by what they write wrote, and the output of the spec they gave.</summary>
    private sealed record Filled(object?[] Values, string?[] Written, ChartOutput Output)
    {
        public bool Holds(ref SlotOutput output, ImmutableArray<SpecHole> holes, SpecHoles owner)
        {
            for (var i = 0; i < holes.Length; i++)
            {
                if (!Holds(ref output, holes[i], i, owner))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Whether a hole's bound value is the one kept: a table hole's items compared with the kept table on the live items,
        /// without reading them into a new table; a paint, a dash pattern, a number list or a member by what they write.
        /// </summary>
        private bool Holds(ref SlotOutput output, SpecHole hole, int index, SpecHoles owner)
        {
            if (hole.ComparedWritten)
            {
                return Value(ref output, hole, owner._members) is var value && (value is Traced ? Values[index] is Traced : Written[index] == owner.Writes(hole, value));
            }

            if (hole.Kind != HoleKind.Table)
            {
                return Equals(Value(ref output, hole, owner._members), Values[index]);
            }

            if (!output.TryRead(hole.Key, out var items))
            {
                return Values[index] is Traced;
            }

            return items is null ? Values[index] is null : Values[index] is DataTable table && table.Matches(items as System.Collections.IEnumerable, hole.Keys, owner._members);
        }
    }

    /// <summary>
    /// The gradients a spec's paint holes define: the traced values' gradients are left out once no paint names them, a
    /// value's gradient is added, and the definitions are ordered as the paints first name them, as the chart's elements
    /// add them.
    /// </summary>
    private sealed class PaintedGradients
    {
        private readonly List<GradientSpec> _known;
        private readonly HashSet<string> _traced = new(StringComparer.Ordinal);

        public PaintedGradients(JsonObject spec, ImmutableArray<SpecHole> holes)
        {
            _known = spec["Gradients"] is JsonArray gradients ? gradients.Deserialize(SpecJsonContext.Default.ListGradientSpec) ?? [] : [];
            foreach (var path in holes.Where(static hole => hole.Kind == HoleKind.Paint).SelectMany(static hole => hole.Paths))
            {
                if (At(spec, path) is JsonValue value && value.TryGetValue<string>(out var paint) && Named(paint) is { } id)
                {
                    _traced.Add(id);
                }
            }
        }

        public void Add(object? value)
        {
            if (value is GradientSpec gradient && !_known.Exists(known => known.Id == gradient.Id))
            {
                _known.Add(gradient);
            }
        }

        public void Define(JsonObject spec)
        {
            var listed = spec.Remove("Gradients");
            var text = spec.ToJsonString();
            var named = _known.Select(gradient => (Gradient: gradient, At: text.IndexOf("\"url(#" + gradient.Id + ")\"", StringComparison.Ordinal))).ToList();
            var defined = named.Where(pair => pair.At >= 0).OrderBy(pair => pair.At).Select(pair => pair.Gradient)
                .Concat(named.Where(pair => pair.At < 0 && !_traced.Contains(pair.Gradient.Id)).Select(pair => pair.Gradient)).ToList();
            if (listed || defined.Count > 0)
            {
                spec["Gradients"] = JsonSerializer.SerializeToNode(defined, SpecJsonContext.Default.ListGradientSpec);
            }
        }

        private static string? Named(string paint) => paint.StartsWith("url(#", StringComparison.Ordinal) && paint.EndsWith(')') ? paint[5..^1] : null;

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
    }
}

[JsonSerializable(typeof(SpecArgument))]
[JsonSourceGenerationOptions(UseStringEnumConverter = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class SpecArgumentJson : JsonSerializerContext;
