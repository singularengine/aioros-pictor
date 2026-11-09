using System.Collections.Immutable;
using System.Text.Json;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Demo.ExampleJson;

namespace Aioros.Pictor.Demo;

/// <summary>Reads Recharts' cartesian examples (<c>LineChart</c>, <c>AreaChart</c>, <c>BarChart</c>, <c>ComposedChart</c>).</summary>
internal static class CartesianExamples
{
    public static ChartSpec Read(ChartSpec spec, string? kind, JsonElement root)
    {
        spec = spec with
        {
            Kind = kind switch
            {
                "LineChart" => ChartKind.Line,
                "AreaChart" => ChartKind.Area,
                "BarChart" => ChartKind.Bar,
                "ScatterChart" => ChartKind.Scatter,
                _ => ChartKind.Composed,
            },
            Layout = root.TryGetProperty("layout", out var layout) && layout.GetString() == "vertical" ? ChartLayoutKind.Vertical : ChartLayoutKind.Horizontal,
        };
        var chart = root.TryGetProperty("chartProps", out var props) ? props : default;
        spec = spec with
        {
            BaseValue = TextOf(chart, "baseValue"),
            StackOffset = Enum.TryParse<StackOffsetKind>(TextOf(chart, "stackOffset"), ignoreCase: true, out var offset) ? offset : StackOffsetKind.None,
            ReverseStackOrder = FlagOf(chart, "reverseStackOrder") ?? false,
            Compact = FlagOf(chart, "compact") ?? false,
            BarSize = TextOf(chart, "barSize"),
            BarGap = TextOf(chart, "barGap") ?? "4",
            BarCategoryGap = TextOf(chart, "barCategoryGap") ?? "10%",
            MaxBarSize = NumberOf(chart, "maxBarSize"),
        };
        var order = 0;
        foreach (var child in root.GetProperty("children").EnumerateArray())
        {
            spec = Child(spec, child, order++);
        }

        return spec;
    }

    private static ChartSpec Child(ChartSpec spec, JsonElement child, int order)
    {
        var props = child.GetProperty("props");
        return child.GetProperty("type").GetString() switch
        {
            "CartesianGrid" => spec with { Grid = Grid(props, order) },
            "XAxis" => spec with { XAxes = spec.XAxes.Add(Axis(child, props, isX: true, order)) },
            "YAxis" => spec with { YAxes = spec.YAxes.Add(Axis(child, props, isX: false, order)) },
            "Tooltip" => spec with { Tooltip = Tooltip(props) },
            "ZAxis" => spec with { ZAxes = spec.ZAxes.Add(ZAxis(props)) },
            "ReferenceArea" => spec with { ReferenceAreas = spec.ReferenceAreas.Add(Area(child, props, order)) },
            "ReferenceDot" => spec with { ReferenceDots = spec.ReferenceDots.Add(Dot(child, props, order)) },
            "Legend" => spec with { Legend = Legend(props) },
            "Brush" => spec with { Brush = Brush(child, props, order) },
            "ReferenceLine" => spec with { ReferenceLines = spec.ReferenceLines.Add(Reference(child, props, order)) },
            "Line" or "Area" or "Bar" or "Scatter" => spec with { Series = spec.Series.Add(Series(child, props, order)) },
            "defs" => spec with { Gradients = spec.Gradients.AddRange(ExampleJson.Children(child, "linearGradient").Select(Gradient)) },
            _ => spec,
        };
    }

    private static GradientSpec Gradient(JsonElement gradient)
    {
        var props = gradient.GetProperty("props");
        return new GradientSpec
        {
            Id = TextOf(props, "id") ?? string.Empty,
            X1 = Fraction(TextOf(props, "x1")) ?? 0,
            Y1 = Fraction(TextOf(props, "y1")) ?? 0,
            X2 = Fraction(TextOf(props, "x2")) ?? 1,
            Y2 = Fraction(TextOf(props, "y2")) ?? 0,
            Stops = [.. ExampleJson.Children(gradient, "stop").Select(stop => stop.GetProperty("props")).Select(stop =>
                new GradientStopSpec(Fraction(TextOf(stop, "offset")) ?? 0, TextOf(stop, "stopColor") ?? "#000", NumberOf(stop, "stopOpacity") ?? 1))],
        };
    }

    /// <summary>An SVG length as a fraction: <c>5%</c> is 0.05, <c>0.2</c> is 0.2.</summary>
    private static double? Fraction(string? text) =>
        text is null ? null
        : text.EndsWith('%') ? double.Parse(text[..^1], System.Globalization.CultureInfo.InvariantCulture) / 100
        : double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

    private static GridSpec Grid(JsonElement props, int order) => new()
    {
        Presentation = ExampleJson.Presentation(props),
        ClassName = TextOf(props, "className"),
        Stroke = TextOf(props, "stroke"),
        StrokeDashArray = TextOf(props, "strokeDasharray"),
        Horizontal = FlagOf(props, "horizontal") ?? true,
        Vertical = FlagOf(props, "vertical") ?? true,
        Fill = TextOf(props, "fill"),
        FillOpacity = NumberOf(props, "fillOpacity"),
        StrokeWidth = NumberOf(props, "strokeWidth"),
        StrokeOpacity = NumberOf(props, "strokeOpacity"),
        HorizontalFill = ListOf(props, "horizontalFill", item => item.GetString()!),
        VerticalFill = ListOf(props, "verticalFill", item => item.GetString()!),
        HorizontalPoints = ListOf(props, "horizontalPoints", item => item.GetDouble()),
        VerticalPoints = ListOf(props, "verticalPoints", item => item.GetDouble()),
        SyncWithTicks = FlagOf(props, "syncWithTicks") ?? false,
        HorizontalValues = ListOf(props, "horizontalValues", DomainText),
        VerticalValues = ListOf(props, "verticalValues", DomainText),
        X = NumberOf(props, "x"),
        Y = NumberOf(props, "y"),
        Width = NumberOf(props, "width"),
        Height = NumberOf(props, "height"),
        Order = order,
    };

    private static AxisSpec Axis(JsonElement child, JsonElement props, bool isX, int order)
    {
        var type = TextOf(props, "type") is { } name ? name == "number" ? AxisType.Number : AxisType.Category : isX ? AxisType.Category : AxisType.Number;
        var axis = new AxisSpec
        {
            Presentation = ExampleJson.Presentation(props),
        ClassName = TextOf(props, "className"),
            Id = TextOf(props, isX ? "xAxisId" : "yAxisId") ?? "0",
            DataKey = TextOf(props, "dataKey"),
            Type = type,
            Orientation = TextOf(props, "orientation") switch
            {
                "top" => AxisOrientation.Top,
                "right" => AxisOrientation.Right,
                "left" => AxisOrientation.Left,
                "bottom" => AxisOrientation.Bottom,
                _ => isX ? AxisOrientation.Bottom : AxisOrientation.Left,
            },
            Size = NumberOf(props, isX ? "height" : "width") ?? 0,
            Scale = TextOf(props, "scale"),
            Hide = FlagOf(props, "hide") ?? false,
            Name = TextOf(props, "name"),
            Unit = TextOf(props, "unit"),
            Label = Label(props),
            Labels = Labels(child),
            TickFormat = TextOf(props, "tickFormatter") switch
            {
                "isoDate" => "{0:yyyy-MM-dd}",
                _ => null,
            },
            Order = order,
        };
        return AxisStyle(props, AxisTicks(props, AxisDomain(props, axis, isX)));
    }

    private static AxisSpec AxisDomain(JsonElement props, AxisSpec axis, bool isX)
    {
        var domain = props.TryGetProperty("domain", out var bounds) && bounds.ValueKind == JsonValueKind.Array ? bounds.EnumerateArray().Select(DomainText).ToArray() : [];
        var padding = props.TryGetProperty("padding", out var pad) ? pad : default;
        var numeric = axis.Type == AxisType.Number;
        return axis with
        {
            AllowDataOverflow = FlagOf(props, "allowDataOverflow") ?? false,
            DomainMin = numeric && domain.Length > 0 ? domain[0] : null,
            DomainMax = numeric && domain.Length > 1 ? domain[1] : null,
            CategoryDomain = numeric ? [] : [.. domain],
            PaddingStart = NumberOf(padding, isX ? "left" : "bottom") ?? 0,
            PaddingEnd = NumberOf(padding, isX ? "right" : "top") ?? 0,
            PaddingKind = padding.ValueKind != JsonValueKind.String ? AxisPaddingKind.Fixed : padding.GetString() == "gap" ? AxisPaddingKind.Gap : AxisPaddingKind.NoGap,
            Reversed = FlagOf(props, "reversed") ?? false,
            AllowDuplicatedCategory = FlagOf(props, "allowDuplicatedCategory") ?? true,
            IncludeHidden = FlagOf(props, "includeHidden") ?? false,
        };
    }

    private static AxisSpec AxisTicks(JsonElement props, AxisSpec axis)
    {
        var (interval, count) = Interval(props);
        return axis with
        {
            TickCount = (int)(NumberOf(props, "tickCount") ?? 5),
            AllowDecimals = FlagOf(props, "allowDecimals") ?? true,
            Interval = interval,
            IntervalCount = count,
            MinTickGap = NumberOf(props, "minTickGap") ?? 5,
            TickSize = NumberOf(props, "tickSize") ?? 6,
            TickMargin = NumberOf(props, "tickMargin") ?? 2,
            Ticks = props.TryGetProperty("ticks", out var ticks) ? [.. ticks.EnumerateArray().Select(DomainText)] : [],
            Mirror = FlagOf(props, "mirror") ?? false,
        };
    }

    private static AxisSpec AxisStyle(JsonElement props, AxisSpec axis)
    {
        var tick = props.TryGetProperty("tick", out var value) ? value : default;
        return axis with
        {
            TickLine = FlagOf(props, "tickLine") ?? true,
            AxisLine = FlagOf(props, "axisLine") ?? true,
            Tick = FlagOf(props, "tick") ?? true,
            Stroke = TextOf(props, "stroke"),
            Angle = NumberOf(props, "angle") ?? 0,
            TextAnchor = Anchor(TextOf(props, "textAnchor")),
            TickFill = TextOf(tick, "fill"),
            TickFontSize = NumberOf(tick, "fontSize"),
            AxisLineStyle = LineStyleOf(props, "axisLine"),
            TickLineStyle = LineStyleOf(props, "tickLine"),
        };
    }

    private static string DomainText(JsonElement value) => value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText();

    private static (TickInterval Interval, int Count) Interval(JsonElement props) => props.TryGetProperty("interval", out var interval)
        ? interval.ValueKind == JsonValueKind.Number
            ? (TickInterval.Fixed, interval.GetInt32())
            : interval.GetString() switch
            {
                "preserveStart" => (TickInterval.PreserveStart, 0),
                "preserveStartEnd" => (TickInterval.PreserveStartEnd, 0),
                "equidistantPreserveStart" => (TickInterval.EquidistantPreserveStart, 0),
                _ => (TickInterval.PreserveEnd, 0),
            }
        : (TickInterval.PreserveEnd, 0);

    public static TextAnchorKind? Anchor(string? anchor) => anchor switch
    {
        "start" => TextAnchorKind.Start,
        "middle" => TextAnchorKind.Middle,
        "end" => TextAnchorKind.End,
        _ => null,
    };

    private static LineStyle? LineStyleOf(JsonElement props, string name) =>
        props.TryGetProperty(name, out var line) && line.ValueKind == JsonValueKind.Object
            ? new LineStyle(TextOf(line, "stroke"), NumberOf(line, "strokeWidth"), TextOf(line, "strokeDasharray"))
            : null;

    /// <summary>An element's <c>Label</c> children, in order (Recharts draws them after its <c>label</c> prop).</summary>
    public static ImmutableArray<LabelSpec> Labels(JsonElement child) =>
        child.ValueKind == JsonValueKind.Object ? [.. Children(child, "Label").Select(element => LabelObject(element.GetProperty("props")))] : [];

    /// <summary>An element's label prop: a text or an object.</summary>
    public static LabelSpec? Label(JsonElement props)
    {
        if (!props.TryGetProperty("label", out var label))
        {
            return null;
        }

        return label.ValueKind switch
        {
            JsonValueKind.String => new LabelSpec { Value = label.GetString() },
            JsonValueKind.Number => new LabelSpec { Value = label.GetRawText() },
            JsonValueKind.Object => LabelObject(label),
            _ => null,
        };
    }

    private static LabelSpec LabelObject(JsonElement label) => new()
    {
        Value = TextOf(label, "value"),
        Position = Position(TextOf(label, "position")),
        PositionX = label.TryGetProperty("position", out var at) && at.ValueKind == JsonValueKind.Object ? TextOf(at, "x") : null,
        PositionY = label.TryGetProperty("position", out var to) && to.ValueKind == JsonValueKind.Object ? TextOf(to, "y") : null,
        Format = FormatOf(label, "formatter"),
        Offset = NumberOf(label, "offset") ?? 5,
        Angle = NumberOf(label, "angle") ?? 0,
        Fill = TextOf(label, "fill"),
        FontSize = NumberOf(label, "fontSize"),
    };

    private static ZAxisSpec ZAxis(JsonElement props)
    {
        var range = props.TryGetProperty("range", out var values) ? values.EnumerateArray().Select(value => value.GetDouble()).ToArray() : [64, 64];
        return new ZAxisSpec
        {
            Id = TextOf(props, "zAxisId") ?? "0",
            DataKey = TextOf(props, "dataKey"),
            RangeMin = range[0],
            RangeMax = range.Length > 1 ? range[1] : range[0],
            Name = TextOf(props, "name"),
            Unit = TextOf(props, "unit"),
            Type = TextOf(props, "type") == "category" ? AxisType.Category : AxisType.Number,
            Scale = TextOf(props, "scale"),
            DomainMin = props.TryGetProperty("domain", out var domain) ? DomainText(domain[0]) : null,
            DomainMax = props.TryGetProperty("domain", out var bounds) && bounds.GetArrayLength() > 1 ? DomainText(bounds[1]) : null,
        };
    }

    private static OverflowKind Overflow(JsonElement props) => TextOf(props, "ifOverflow") switch
    {
        "visible" => OverflowKind.Visible,
        "hidden" => OverflowKind.Hidden,
        "extendDomain" => OverflowKind.ExtendDomain,
        _ => OverflowKind.Discard,
    };

    private static ReferenceAreaSpec Area(JsonElement child, JsonElement props, int order) => new()
    {
        X1 = TextOf(props, "x1"),
        X2 = TextOf(props, "x2"),
        Y1 = TextOf(props, "y1"),
        Y2 = TextOf(props, "y2"),
        Fill = TextOf(props, "fill"),
        FillOpacity = NumberOf(props, "fillOpacity") ?? 0.5,
        Stroke = TextOf(props, "stroke") ?? "none",
        StrokeOpacity = NumberOf(props, "strokeOpacity"),
        StrokeWidth = NumberOf(props, "strokeWidth") ?? 1,
        Label = Label(props),
        Labels = Labels(child),
        XAxisId = TextOf(props, "xAxisId") ?? "0",
        YAxisId = TextOf(props, "yAxisId") ?? "0",
        IfOverflow = Overflow(props),
        Radius = NumberOf(props, "radius") is { } radius ? [radius] : [],
        Order = order,
    };

    private static ReferenceDotSpec Dot(JsonElement child, JsonElement props, int order) => new()
    {
        X = TextOf(props, "x"),
        Y = TextOf(props, "y"),
        R = NumberOf(props, "r") ?? 10,
        Fill = TextOf(props, "fill"),
        FillOpacity = NumberOf(props, "fillOpacity") ?? 1,
        Stroke = TextOf(props, "stroke"),
        StrokeWidth = NumberOf(props, "strokeWidth") ?? 1,
        Label = Label(props),
        Labels = Labels(child),
        XAxisId = TextOf(props, "xAxisId") ?? "0",
        YAxisId = TextOf(props, "yAxisId") ?? "0",
        IfOverflow = Overflow(props),
        Order = order,
    };

    private static DataTable? Table(JsonElement props)
    {
        if (!props.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var items = (List<object?>)Value(data)!;
        var keys = items.OfType<IReadOnlyDictionary<string, object?>>().SelectMany(item => item.Keys).Distinct().ToList();
        return DataTable.Read(items, keys, static (_, _) => null);
    }

    private static ActiveStyle? Active(JsonElement props, string name) =>
        props.TryGetProperty(name, out var active) && active.ValueKind == JsonValueKind.Object && (TextOf(active, "fill") is not null || TextOf(active, "stroke") is not null || NumberOf(active, "strokeWidth") is not null)
            ? new ActiveStyle(TextOf(active, "fill"), TextOf(active, "stroke"), NumberOf(active, "strokeWidth"))
            : null;

    private static ErrorBarSpec ErrorBar(JsonElement props) => new()
    {
        Presentation = ExampleJson.Presentation(props),
        DataKey = TextOf(props, "dataKey") ?? string.Empty,
        Width = NumberOf(props, "width") ?? 5,
        StrokeWidth = NumberOf(props, "strokeWidth") ?? 1.5,
        Stroke = TextOf(props, "stroke"),
        AlongX = TextOf(props, "direction") == "x",
    };

    private static ScatterShape Shape(string? shape) =>
        shape is not null && Enum.TryParse<ScatterShape>(shape, ignoreCase: true, out var parsed) ? parsed : ScatterShape.Circle;

    private static ReferenceLineSpec Reference(JsonElement child, JsonElement props, int order) => new()
    {
        X = TextOf(props, "x"),
        Y = TextOf(props, "y"),
        Stroke = TextOf(props, "stroke"),
        StrokeDashArray = TextOf(props, "strokeDasharray"),
        Label = Label(props),
        Labels = Labels(child),
        XAxisId = TextOf(props, "xAxisId") ?? "0",
        YAxisId = TextOf(props, "yAxisId") ?? "0",
        StrokeWidth = NumberOf(props, "strokeWidth") ?? 1,
        IfOverflow = Overflow(props),
        Position = TextOf(props, "position") switch
        {
            "start" => BandPosition.Start,
            "end" => BandPosition.End,
            _ => BandPosition.Middle,
        },
        Segment = props.TryGetProperty("segment", out var segment) && segment.ValueKind == JsonValueKind.Array && segment.GetArrayLength() == 2
            ? new ReferenceSegment(TextOf(segment[0], "x"), TextOf(segment[0], "y"), TextOf(segment[1], "x"), TextOf(segment[1], "y"))
            : null,
        Order = order,
    };

    private static SeriesSpec Series(JsonElement child, JsonElement props, int order)
    {
        var kind = child.GetProperty("type").GetString() switch
        {
            "Line" => SeriesKind.Line,
            "Area" => SeriesKind.Area,
            "Scatter" => SeriesKind.Scatter,
            _ => SeriesKind.Bar,
        };
        var cells = Children(child, "Cell").Select(item => CellOf(item.GetProperty("props"))).ToImmutableArray();
        var (labels, others) = LabelListsOf(child);
        return BarProps(props, ScatterLine(props, new SeriesSpec
        {
            Presentation = kind is SeriesKind.Line or SeriesKind.Area ? ExampleJson.Presentation(props) : null,
            ClassName = TextOf(props, "className"),
            Kind = kind,
            Animation = Animation(props),
            DataKey = TextOf(props, "dataKey") ?? string.Empty,
            Name = TextOf(props, "name"),
            Curve = Curve(TextOf(props, "type")),
            Stroke = TextOf(props, "stroke"),
            StrokeWidth = NumberOf(props, "strokeWidth") ?? 1,
            Fill = TextOf(props, "fill"),
            FillOpacity = NumberOf(props, "fillOpacity"),
            StrokeDashArray = TextOf(props, "strokeDasharray"),
            Dot = FlagOf(props, "dot") ?? (DotOf(props) is not null || kind == SeriesKind.Line),
            DotProps = DotOf(props),
            ClipDot = !props.TryGetProperty("dot", out var dotProps) || (FlagOf(dotProps, "clipDot") ?? true),
            ActiveDot = FlagOf(props, "activeDot") ?? true,
            ActiveDotRadius = props.TryGetProperty("activeDot", out var active) ? NumberOf(active, "r") ?? 4 : 4,
            ActiveDotStyle = Active(props, "activeDot"),
            ActiveBar = Active(props, "activeBar"),
            ActiveIndex = (int?)NumberOf(props, "activeIndex"),
            Data = Table(props),
            ZAxisId = TextOf(props, "zAxisId") ?? "0",
            Shape = Shape(TextOf(props, "shape")),
            LineJointType = Curve(TextOf(props, "lineJointType")),
            ErrorBars = [.. Children(child, "ErrorBar").Select(item => ErrorBar(item.GetProperty("props")))],
            StackId = TextOf(props, "stackId"),
            BaseValue = TextOf(props, "baseValue"),
            Hide = FlagOf(props, "hide") ?? false,
            XAxisId = TextOf(props, "xAxisId") ?? "0",
            YAxisId = TextOf(props, "yAxisId") ?? "0",
            Cells = cells,
            Labels = labels,
            LabelLists = others,
            Unit = TextOf(props, "unit"),
            TooltipNone = TextOf(props, "tooltipType") == "none",
            LegendType = Icon(TextOf(props, "legendType")),
            Order = order,
        }));
    }

    /// <summary>A bar's sizes, corners, smallest size and background, and a curve's connectNulls.</summary>
    private static SeriesSpec BarProps(JsonElement props, SeriesSpec spec)
    {
        var background = props.TryGetProperty("background", out var given) ? given : default;
        return spec with
        {
            BarSize = TextOf(props, "barSize"),
            MaxBarSize = NumberOf(props, "maxBarSize"),
            Radius = props.TryGetProperty("radius", out var radius) ? radius.ValueKind == JsonValueKind.Array ? [.. radius.EnumerateArray().Select(value => value.GetDouble())] : [radius.GetDouble()] : [],
            MinPointSize = NumberOf(props, "minPointSize") ?? 0,
            ConnectNulls = FlagOf(props, "connectNulls") ?? false,
            Background = background.ValueKind == JsonValueKind.Undefined ? null : TextOf(background, "fill") ?? "true",
            BackgroundProps = background.ValueKind == JsonValueKind.Object && (TextOf(background, "stroke") is not null || NumberOf(background, "strokeWidth") is not null || NumberOf(background, "fillOpacity") is not null) ? CellOf(background) with { Fill = null } : null,
            BackgroundRadius = background.ValueKind == JsonValueKind.Object && background.TryGetProperty("radius", out var corner) ? corner.ValueKind == JsonValueKind.Array ? [.. corner.EnumerateArray().Select(value => value.GetDouble())] : [corner.GetDouble()] : [],
        };
    }

    private static SeriesSpec ScatterLine(JsonElement props, SeriesSpec spec)
    {
        var line = props.TryGetProperty("line", out var value) ? value : default;
        return spec with
        {
            JointLine = FlagOf(props, "line") ?? line.ValueKind == JsonValueKind.Object,
            LineType = TextOf(props, "lineType") == "fitting" ? ScatterLineType.Fitting : ScatterLineType.Joint,
            LineStroke = TextOf(line, "stroke"),
            LineStrokeWidth = NumberOf(line, "strokeWidth"),
            LineStrokeDashArray = TextOf(line, "strokeDasharray"),
        };
    }

    private static BrushSpec Brush(JsonElement child, JsonElement props, int order) => new()
    {
        TickFormat = TextOf(props, "tickFormatter"),
        AlwaysShowText = FlagOf(props, "alwaysShowText") ?? false,
        AriaLabel = TextOf(props, "ariaLabel"),
        X = NumberOf(props, "x"),
        Y = NumberOf(props, "y"),
        Width = NumberOf(props, "width"),
        Padding = props.TryGetProperty("padding", out var padding) ? ExampleJson.Margin(padding) : new Edges(1, 1, 1, 1),
        Panorama = child.GetProperty("children").EnumerateArray().FirstOrDefault() is { ValueKind: JsonValueKind.Object } chart ? Read(new ChartSpec(), chart.GetProperty("type").GetString(), chart) : null,
        DataKey = TextOf(props, "dataKey"),
        Height = NumberOf(props, "height") ?? 40,
        Stroke = TextOf(props, "stroke"),
        Fill = TextOf(props, "fill"),
        TravellerWidth = NumberOf(props, "travellerWidth") ?? 5,
        Gap = (int)(NumberOf(props, "gap") ?? 1),
        StartIndex = NumberOf(props, "startIndex") is { } start ? (int)start : null,
        EndIndex = NumberOf(props, "endIndex") is { } end ? (int)end : null,
        Order = order,
    };
}
