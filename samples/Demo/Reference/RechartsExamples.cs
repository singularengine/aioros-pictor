using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Demo;

/// <summary>A Recharts example as <c>tools/recharts</c> describes it: the chart's props, its data and its children.</summary>
internal sealed record RechartsExample(string Name, ChartSpec Spec, object? Data);

/// <summary>Reads the example descriptions of <c>tools/recharts/examples</c> into Pictor's specs, prop for prop, by chart family.</summary>
internal static class RechartsExamples
{
    public static async Task<IReadOnlyList<RechartsExample>> LoadAsync(string folder, CancellationToken cancellationToken)
    {
        var examples = new List<RechartsExample>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.json").Order(StringComparer.Ordinal))
        {
            await using var stream = File.OpenRead(file);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            examples.Add(Read(document.RootElement));
        }

        return examples;
    }

    public static RechartsExample Read(JsonElement root)
    {
        var name = root.GetProperty("name").GetString()!;
        var spec = new ChartSpec
        {
            Width = root.GetProperty("width").GetDouble(),
            Height = root.GetProperty("height").GetDouble(),
            Margin = ExampleJson.Margin(root.GetProperty("margin")),
            Title = root.TryGetProperty("chartProps", out var chart) ? ExampleJson.TextOf(chart, "title") : null,
            Desc = root.TryGetProperty("chartProps", out var described) ? ExampleJson.TextOf(described, "desc") : null,
            AccessibilityLayer = root.TryGetProperty("chartProps", out var accessible) && ExampleJson.FlagOf(accessible, "accessibilityLayer") == true,
            ClassName = root.TryGetProperty("chartProps", out var classed) ? ExampleJson.TextOf(classed, "className") : null,
            Role = root.TryGetProperty("chartProps", out var role) ? ExampleJson.TextOf(role, "role") : null,
            TabIndex = root.TryGetProperty("chartProps", out var tab) && ExampleJson.NumberOf(tab, "tabIndex") is { } index ? (int)index : null,
            WrapperStyle = root.TryGetProperty("chartProps", out var styled) ? ExampleJson.StyleOf(styled, "style") : null,
        };
        var data = ExampleJson.Value(root.GetProperty("data"));
        spec = root.GetProperty("kind").GetString() switch
        {
            var kind when kind is "PieChart" or "RadarChart" or "RadialBarChart" => PolarExamples.Read(spec, kind, root),
            var kind when kind is "FunnelChart" or "Treemap" or "Sankey" or "SunburstChart" => StandaloneExamples.Read(spec, kind, root),
            var kind => CartesianExamples.Read(spec, kind, root),
        };
        return new RechartsExample(name, spec, data);
    }

    public static string Describe(double value) => value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Reads the props of an example's JSON description: numbers, texts, flags, positions, the data as .NET values.</summary>
internal static class ExampleJson
{
    /// <summary>A JSON value as the .NET value an app would bind: objects as dictionaries, arrays as lists.</summary>
    public static object? Value(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => value.EnumerateArray().Select(Value).ToList(),
        JsonValueKind.Object => (IReadOnlyDictionary<string, object?>)value.EnumerateObject().ToDictionary(property => property.Name, property => Value(property.Value), StringComparer.Ordinal),
        _ => null,
    };

    public static Edges Margin(JsonElement margin) => margin.ValueKind != JsonValueKind.Object
        ? Edges.Default
        : new Edges(NumberOf(margin, "top") ?? 0, NumberOf(margin, "right") ?? 0, NumberOf(margin, "bottom") ?? 0, NumberOf(margin, "left") ?? 0);

    public static double? NumberOf(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;

    public static string? TextOf(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        } : null;

    /// <summary>The SVG presentation attributes of an element's props that Pictor takes, or null when there are none.</summary>
    public static SvgPresentation? Presentation(JsonElement props)
    {
        var presentation = new SvgPresentation
        {
            Opacity = NumberOf(props, "opacity"),
            StrokeOpacity = NumberOf(props, "strokeOpacity"),
            StrokeLinecap = TextOf(props, "strokeLinecap"),
            StrokeLinejoin = TextOf(props, "strokeLinejoin"),
            StrokeMiterlimit = NumberOf(props, "strokeMiterlimit"),
            FontWeight = props.ValueKind == JsonValueKind.Object && props.TryGetProperty("fontWeight", out var weight) ? weight.ToString() : null,
            FontStyle = TextOf(props, "fontStyle"),
        };
        return presentation.IsEmpty ? null : presentation;
    }

    public static bool? FlagOf(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;

    public static IEnumerable<JsonElement> Children(JsonElement node, string type) =>
        node.GetProperty("children").EnumerateArray().Where(item => item.GetProperty("type").GetString() == type);

    /// <summary>Recharts' animation props: off when <c>isAnimationActive</c> is false, null when every prop has its default.</summary>
    public static AnimationSpec? Animation(JsonElement props)
    {
        if (FlagOf(props, "isAnimationActive") == false)
        {
            return AnimationSpec.Off;
        }

        var easing = TextOf(props, "animationEasing") is { } name && Enum.TryParse<AnimationEasing>(name.Replace("-", string.Empty, StringComparison.Ordinal), ignoreCase: true, out var parsed) ? parsed : (AnimationEasing?)null;
        var begin = NumberOf(props, "animationBegin");
        var duration = NumberOf(props, "animationDuration");
        return begin is null && duration is null && easing is null ? null : new AnimationSpec { Begin = begin, Duration = duration, Easing = easing };
    }

    public static LabelPosition? Position(string? position) =>
        position is null ? null : Enum.TryParse<LabelPosition>(position, ignoreCase: true, out var parsed) ? parsed : null;

    /// <summary>A formatter given as a .NET composite format (<c>{"$format": "{0} kg"}</c>, which the reference tooling turns into a function).</summary>
    public static string? FormatOf(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object && value.TryGetProperty("$format", out var format) ? format.GetString() : null;

    /// <summary>A React style object as CSS declarations: camelCase names in kebab case, numbers in pixels except unitless ones.</summary>
    public static string? StyleOf(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var style) || style.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var declarations = style.EnumerateObject().Select(property =>
        {
            var css = string.Concat(property.Name.Select(c => char.IsUpper(c) ? "-" + char.ToLowerInvariant(c) : c.ToString()));
            var value = property.Value.ValueKind == JsonValueKind.Number && css is not ("font-weight" or "opacity" or "z-index" or "line-height" or "flex")
                ? property.Value.GetRawText() + "px"
                : property.Value.ValueKind == JsonValueKind.Number ? property.Value.GetRawText() : property.Value.GetString();
            return $"{css}:{value}";
        });
        return string.Join(';', declarations);
    }

    /// <summary>A <c>LabelList</c>'s or a <c>label</c> object's props.</summary>
    public static LabelSpec LabelOf(JsonElement props)
    {
        var position = props.ValueKind == JsonValueKind.Object && props.TryGetProperty("position", out var at) ? at : default;
        return new LabelSpec
        {
            DataKey = TextOf(props, "dataKey"),
            Position = position.ValueKind == JsonValueKind.String ? Position(position.GetString()) : null,
            PositionX = TextOf(position, "x"),
            PositionY = TextOf(position, "y"),
            Offset = NumberOf(props, "offset") ?? 5,
            Fill = TextOf(props, "fill"),
            Stroke = TextOf(props, "stroke"),
            StrokeWidth = NumberOf(props, "strokeWidth"),
            FontSize = NumberOf(props, "fontSize"),
            FontWeight = TextOf(props, "fontWeight"),
            Angle = NumberOf(props, "angle") ?? 0,
            ClockWise = FlagOf(props, "clockWise") ?? false,
            BreakAll = FlagOf(props, "textBreakAll") ?? false,
            Format = FormatOf(props, "formatter"),
        };
    }

    /// <summary>An element's label lists in Recharts' order: its <c>label</c> prop's (true or an object), then its <c>LabelList</c> children.</summary>
    public static (LabelSpec? First, ImmutableArray<LabelSpec> Others) LabelListsOf(JsonElement child, bool implicitLabel = true)
    {
        var props = child.GetProperty("props");
        var lists = new List<LabelSpec>();
        if (implicitLabel && props.TryGetProperty("label", out var label))
        {
            if (label.ValueKind == JsonValueKind.True)
            {
                lists.Add(new LabelSpec());
            }
            else if (label.ValueKind == JsonValueKind.Object)
            {
                lists.Add(LabelOf(label));
            }
        }

        lists.AddRange(Children(child, "LabelList").Select(item => LabelOf(item.GetProperty("props"))));
        return lists.Count == 0 ? (null, []) : (lists[0], [.. lists.Skip(1)]);
    }

    /// <summary>A <c>dot</c> given as an object, or null.</summary>
    public static ActiveStyle? DotOf(JsonElement props) =>
        props.TryGetProperty("dot", out var dot) && dot.ValueKind == JsonValueKind.Object
            ? new ActiveStyle { R = NumberOf(dot, "r"), Fill = TextOf(dot, "fill"), Stroke = TextOf(dot, "stroke"), StrokeWidth = NumberOf(dot, "strokeWidth"), FillOpacity = NumberOf(dot, "fillOpacity"), StrokeDashArray = TextOf(dot, "strokeDasharray") }
            : null;

    /// <summary>A <c>Cell</c>'s props.</summary>
    public static CellSpec CellOf(JsonElement props) => new(TextOf(props, "fill"), TextOf(props, "stroke"))
    {
        StrokeWidth = NumberOf(props, "strokeWidth"),
        FillOpacity = NumberOf(props, "fillOpacity"),
        Opacity = NumberOf(props, "opacity"),
        StrokeDashArray = TextOf(props, "strokeDasharray"),
    };

    /// <summary>A list of numbers or texts given as a JSON array.</summary>
    public static ImmutableArray<T> ListOf<T>(JsonElement props, string name, Func<JsonElement, T> read) =>
        props.TryGetProperty(name, out var list) && list.ValueKind == JsonValueKind.Array ? [.. list.EnumerateArray().Select(read)] : [];

    /// <summary>A legend icon by Recharts' name (<c>plainline</c>, <c>wye</c>).</summary>
    public static LegendIcon? Icon(string? type) => type is not null && Enum.TryParse<LegendIcon>(type, ignoreCase: true, out var icon) ? icon : null;

    /// <summary>Recharts' <c>Legend</c> props.</summary>
    public static LegendSpec Legend(JsonElement props) => new()
    {
        Align = TextOf(props, "align") ?? "center",
        VerticalAlign = TextOf(props, "verticalAlign") ?? "bottom",
        Vertical = TextOf(props, "layout") == "vertical",
        IconSize = NumberOf(props, "iconSize") ?? 14,
        IconType = Icon(TextOf(props, "iconType")),
        LineHeight = props.TryGetProperty("wrapperStyle", out var wrapper) && TextOf(wrapper, "lineHeight") is { } line
            ? double.Parse(line.Replace("px", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture)
            : null,
        InactiveColor = TextOf(props, "inactiveColor"),
        WrapperStyle = props.TryGetProperty("wrapperStyle", out var style) && style.EnumerateObject().Any(property => property.Name != "lineHeight")
            ? string.Join(';', StyleOf(props, "wrapperStyle")!.Split(';').Where(declaration => !declaration.StartsWith("line-height:", StringComparison.Ordinal)))
            : null,
        Format = FormatOf(props, "formatter"),
        Width = NumberOf(props, "width"),
        Height = NumberOf(props, "height"),
        PayloadUniqBy = FlagOf(props, "payloadUniqBy") ?? false,
        Payload = props.TryGetProperty("payload", out var payload) && payload.ValueKind == JsonValueKind.Array
            ? [.. payload.EnumerateArray().Select(entry => new LegendPayloadSpec { Value = TextOf(entry, "value") ?? string.Empty, Color = TextOf(entry, "color"), Type = Icon(TextOf(entry, "type")), Inactive = FlagOf(entry, "inactive") ?? false })]
            : [],
    };

    /// <summary>Recharts' <c>Tooltip</c> props.</summary>
    public static TooltipSpec Tooltip(JsonElement props)
    {
        var cursor = props.TryGetProperty("cursor", out var value) ? value : default;
        var escape = props.TryGetProperty("allowEscapeViewBox", out var allow) ? allow : default;
        var reverse = props.TryGetProperty("reverseDirection", out var direction) ? direction : default;
        var position = props.TryGetProperty("position", out var at) ? at : default;
        return new TooltipSpec
        {
            Separator = TextOf(props, "separator") ?? " : ",
            Cursor = cursor.ValueKind != JsonValueKind.False,
            CursorStroke = TextOf(cursor, "stroke"),
            CursorFill = TextOf(cursor, "fill"),
            CursorStrokeWidth = NumberOf(cursor, "strokeWidth"),
            CursorStrokeDashArray = TextOf(cursor, "strokeDasharray"),
            CursorFillOpacity = NumberOf(cursor, "fillOpacity"),
            CursorStrokeOpacity = NumberOf(cursor, "strokeOpacity"),
            Format = FormatOf(props, "formatter"),
            LabelFormat = FormatOf(props, "labelFormatter"),
            FilterNull = FlagOf(props, "filterNull") ?? true,
            IncludeHidden = FlagOf(props, "includeHidden") ?? false,
            PayloadUniqBy = FlagOf(props, "payloadUniqBy") ?? false,
            ItemSorter = TextOf(props, "itemSorter"),
            Shared = FlagOf(props, "shared"),
            ClickTrigger = TextOf(props, "trigger") == "click",
            DefaultIndex = NumberOf(props, "defaultIndex") is { } index ? (int)index : null,
            Active = FlagOf(props, "active"),
            Offset = NumberOf(props, "offset") ?? 10,
            AllowEscapeX = FlagOf(escape, "x") ?? false,
            AllowEscapeY = FlagOf(escape, "y") ?? false,
            ReverseX = FlagOf(reverse, "x") ?? false,
            ReverseY = FlagOf(reverse, "y") ?? false,
            PositionX = NumberOf(position, "x"),
            PositionY = NumberOf(position, "y"),
            WrapperStyle = StyleOf(props, "wrapperStyle"),
            ContentStyle = StyleOf(props, "contentStyle"),
            LabelStyle = StyleOf(props, "labelStyle"),
            ItemStyle = StyleOf(props, "itemStyle"),
            WrapperClassName = TextOf(props, "wrapperClassName"),
            LabelClassName = TextOf(props, "labelClassName"),
            AccessibilityLayer = FlagOf(props, "accessibilityLayer") ?? false,
        };
    }

    public static CurveType Curve(string? type) =>
        type is not null && Enum.TryParse<CurveType>(type, ignoreCase: true, out var curve) ? curve : CurveType.Linear;
}
