using System.Globalization;
using Aioros.Pictor.Specs;
using Avalonia;

namespace Aioros.Pictor.Demo;

/// <summary>
/// Builds the XAML elements of a Recharts example in code (what its Gallery page declares), so the desktop control can
/// be drawn and measured outside an app; each family builds its own.
/// </summary>
internal static class SpecControls
{
    public static Chart Create(RechartsExample example) => example.Spec.Kind switch
    {
        ChartKind.Pie or ChartKind.Radar or ChartKind.RadialBar => PolarControls.Create(example),
        ChartKind.Funnel or ChartKind.Treemap or ChartKind.Sankey or ChartKind.Sunburst => StandaloneControls.Create(example),
        _ => CartesianControls.Create(example),
    };

    /// <summary>An element's animation props, as its spec gives them.</summary>
    public static T Animated<T>(T element, AnimationSpec? spec)
        where T : global::Avalonia.AvaloniaObject
    {
        if (spec is not null)
        {
            element.SetValue(Series.IsAnimationActiveProperty, spec.Active);
            element.SetValue(Series.AnimationBeginProperty, spec.Begin);
            element.SetValue(Series.AnimationDurationProperty, spec.Duration);
            element.SetValue(Series.AnimationEasingProperty, spec.Easing);
        }

        return element;
    }

    /// <summary>A tooltip element with every prop of its spec.</summary>
    public static Tooltip TooltipOf(TooltipSpec spec) => new()
    {
        Separator = spec.Separator,
        Cursor = spec.Cursor,
        CursorStroke = spec.CursorStroke,
        CursorFill = spec.CursorFill,
        CursorStrokeWidth = spec.CursorStrokeWidth,
        CursorStrokeDashArray = spec.CursorStrokeDashArray,
        CursorFillOpacity = spec.CursorFillOpacity,
        CursorStrokeOpacity = spec.CursorStrokeOpacity,
        Format = spec.Format,
        LabelFormat = spec.LabelFormat,
        FilterNull = spec.FilterNull,
        IncludeHidden = spec.IncludeHidden,
        ItemSorter = spec.ItemSorter switch { "dataKey" => TooltipSorter.DataKey, "value" => TooltipSorter.Value, "name" => TooltipSorter.Name, _ => TooltipSorter.None },
        PayloadUniqBy = spec.PayloadUniqBy,
        Shared = spec.Shared,
        Trigger = spec.ClickTrigger ? TooltipTrigger.Click : TooltipTrigger.Hover,
        DefaultIndex = spec.DefaultIndex,
        Active = spec.Active,
        Offset = spec.Offset,
        AllowEscapeViewBoxX = spec.AllowEscapeX,
        AllowEscapeViewBoxY = spec.AllowEscapeY,
        ReverseDirectionX = spec.ReverseX,
        ReverseDirectionY = spec.ReverseY,
        PositionX = spec.PositionX,
        PositionY = spec.PositionY,
        WrapperStyle = spec.WrapperStyle,
        ContentStyle = spec.ContentStyle,
        LabelStyle = spec.LabelStyle,
        ItemStyle = spec.ItemStyle,
        WrapperClassName = spec.WrapperClassName,
        LabelClassName = spec.LabelClassName,
        AccessibilityLayer = spec.AccessibilityLayer,
    };

    /// <summary>A cell element with every prop of its spec.</summary>
    public static Cell CellOf(CellSpec spec) => new() { Fill = spec.Fill, Stroke = spec.Stroke, StrokeWidth = spec.StrokeWidth, FillOpacity = spec.FillOpacity, Opacity = spec.Opacity, StrokeDashArray = spec.StrokeDashArray };

    /// <summary>A list as XAML writes it, separated by spaces.</summary>
    public static string? Join<T>(System.Collections.Immutable.ImmutableArray<T> values) =>
        values.IsDefaultOrEmpty ? null : string.Join(' ', values.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture)));

    /// <summary>A label list element with every prop of its spec.</summary>
    public static LabelList LabelListOf(LabelSpec spec) => new()
    {
        DataKey = spec.DataKey,
        Position = spec.Position,
        PositionX = spec.PositionX,
        PositionY = spec.PositionY,
        Offset = spec.Offset,
        Fill = spec.Fill,
        Stroke = spec.Stroke,
        StrokeWidth = spec.StrokeWidth,
        FontSize = spec.FontSize,
        FontWeight = spec.FontWeight,
        Angle = spec.Angle,
        ClockWise = spec.ClockWise,
        TextBreakAll = spec.BreakAll,
        Format = spec.Format,
    };

    /// <summary>Adds an element's label lists to its children.</summary>
    public static void AddLabelLists(global::Avalonia.Collections.AvaloniaList<ISeriesPart> parts, LabelSpec? first, System.Collections.Immutable.ImmutableArray<LabelSpec> others)
    {
        if (first is not null)
        {
            parts.Add(LabelListOf(first));
        }

        foreach (var list in others)
        {
            parts.Add(LabelListOf(list));
        }
    }

    /// <summary>Adds an element's <c>Label</c> children.</summary>
    public static void AddLabels(global::Avalonia.Collections.AvaloniaList<Label> target, System.Collections.Immutable.ImmutableArray<LabelSpec> labels)
    {
        foreach (var label in labels)
        {
            target.Add(new Label { Value = label.Value, Position = label.Position, PositionX = label.PositionX, PositionY = label.PositionY, Offset = label.Offset, Angle = label.Angle, Fill = label.Fill, FontSize = label.FontSize, Format = label.Format });
        }
    }

    /// <summary>A legend element with every prop of its spec.</summary>
    public static Legend LegendOf(LegendSpec spec)
    {
        var legend = new Legend
        {
            IconSize = spec.IconSize,
            Align = spec.Align,
            VerticalAlign = spec.VerticalAlign,
            IconType = spec.IconType,
            Layout = spec.Vertical ? "vertical" : "horizontal",
            Toggles = spec.Toggles,
            LineHeight = spec.LineHeight,
            InactiveColor = spec.InactiveColor,
            Format = spec.Format,
            Width = spec.Width,
            Height = spec.Height,
            PayloadUniqBy = spec.PayloadUniqBy,
            WrapperStyle = spec.WrapperStyle,
        };
        foreach (var entry in spec.Payload)
        {
            legend.Payload.Add(new LegendPayload { Value = entry.Value, Color = entry.Color, Type = entry.Type, Inactive = entry.Inactive });
        }

        return legend;
    }

    /// <summary>The chart's size, inner margin and data, as the example sets them.</summary>
    public static T Sized<T>(T chart, RechartsExample example)
        where T : Chart
    {
        var spec = example.Spec;
        chart.Width = spec.Width;
        chart.Height = spec.Height;
        chart.Padding = new Thickness(spec.Margin.Left, spec.Margin.Top, spec.Margin.Right, spec.Margin.Bottom);
        chart.Data = example.Data;
        chart.Title = spec.Title;
        chart.Desc = spec.Desc;
        return chart;
    }
}

/// <summary>Builds the elements of a cartesian example.</summary>
internal static class CartesianControls
{
    public static CartesianChart Create(RechartsExample example)
    {
        var spec = example.Spec;
        CartesianChart chart = spec.Kind switch
        {
            ChartKind.Line => new LineChart(),
            ChartKind.Area => new AreaChart(),
            ChartKind.Bar => new BarChart(),
            ChartKind.Scatter => new ScatterChart(),
            _ => new ComposedChart(),
        };
        SpecControls.Sized(chart, example);
        chart.Layout = spec.Layout;
        chart.BaseValue = spec.BaseValue;
        chart.StackOffset = spec.StackOffset;
        chart.ReverseStackOrder = spec.ReverseStackOrder;
        chart.Compact = spec.Compact;
        chart.BarSize = spec.BarSize;
        chart.BarGap = spec.BarGap;
        chart.BarCategoryGap = spec.BarCategoryGap;
        chart.MaxBarSize = spec.MaxBarSize;
        foreach (var element in Elements(spec).OrderBy(pair => pair.Order).Select(pair => pair.Element))
        {
            chart.Elements.Add(element);
        }

        return chart;
    }

    private static IEnumerable<(int Order, IChartElement Element)> Elements(ChartSpec spec)
    {
        if (spec.Grid is { } grid)
        {
            yield return (grid.Order, PresentationControls.Of(Grid(grid), grid.Presentation));
        }

        foreach (var axis in spec.XAxes)
        {
            yield return (axis.Order, AxisControls.Of(new XAxis { XAxisId = axis.Id }, axis));
        }

        foreach (var axis in spec.YAxes)
        {
            yield return (axis.Order, AxisControls.Of(new YAxis { YAxisId = axis.Id }, axis));
        }

        foreach (var reference in References(spec))
        {
            yield return reference;
        }

        foreach (var series in spec.Series)
        {
            yield return (series.Order, Series(series, spec.Gradients));
        }

        foreach (var axis in spec.ZAxes)
        {
            yield return (-1, new ZAxis { ZAxisId = axis.Id, DataKey = axis.DataKey, Range = string.Create(CultureInfo.InvariantCulture, $"{axis.RangeMin},{axis.RangeMax}"), Name = axis.Name, Unit = axis.Unit, Type = axis.Type, Scale = axis.Scale, Domain = axis.DomainMin is null ? null : $"{axis.DomainMin}, {axis.DomainMax ?? "auto"}" });
        }

        if (spec.Brush is { } brush)
        {
            yield return (brush.Order, new Brush
            {
                DataKey = brush.DataKey,
                Height = brush.Height,
                Stroke = brush.Stroke,
                Fill = brush.Fill,
                TravellerWidth = brush.TravellerWidth,
                Gap = brush.Gap,
                StartIndex = brush.StartIndex,
                EndIndex = brush.EndIndex,
                TickFormat = brush.TickFormat,
                AlwaysShowText = brush.AlwaysShowText,
                AriaLabel = brush.AriaLabel,
                X = brush.X,
                Y = brush.Y,
                Width = brush.Width,
                Padding = new Thickness(brush.Padding.Left, brush.Padding.Top, brush.Padding.Right, brush.Padding.Bottom),
                Panorama = brush.Panorama is { } panorama ? Create(new RechartsExample(string.Empty, panorama, null)) : null,
            });
        }

        if (spec.Legend is { } legend)
        {
            yield return (int.MaxValue - 1, SpecControls.LegendOf(legend));
        }

        if (spec.Tooltip is not null)
        {
            yield return (int.MaxValue, SpecControls.TooltipOf(spec.Tooltip));
        }
    }

    private static CartesianGrid Grid(GridSpec grid) => new()
    {
        Stroke = grid.Stroke,
        StrokeDashArray = grid.StrokeDashArray,
        Horizontal = grid.Horizontal,
        Vertical = grid.Vertical,
        Fill = grid.Fill,
        FillOpacity = grid.FillOpacity,
        StrokeWidth = grid.StrokeWidth,
        StrokeOpacity = grid.StrokeOpacity,
        HorizontalFill = SpecControls.Join(grid.HorizontalFill),
        VerticalFill = SpecControls.Join(grid.VerticalFill),
        HorizontalPoints = SpecControls.Join(grid.HorizontalPoints),
        VerticalPoints = SpecControls.Join(grid.VerticalPoints),
        SyncWithTicks = grid.SyncWithTicks,
        HorizontalValues = grid.HorizontalValues.IsDefaultOrEmpty ? null : string.Join(", ", grid.HorizontalValues),
        VerticalValues = grid.VerticalValues.IsDefaultOrEmpty ? null : string.Join(", ", grid.VerticalValues),
        X = grid.X,
        Y = grid.Y,
        GridWidth = grid.Width,
        GridHeight = grid.Height,
    };

    private static IEnumerable<(int Order, IChartElement Element)> References(ChartSpec spec)
    {
        foreach (var reference in spec.ReferenceLines)
        {
            yield return (reference.Order, Children(Labelled(new ReferenceLine
            {
                X = reference.X,
                Y = reference.Y,
                Stroke = reference.Stroke,
                StrokeWidth = reference.StrokeWidth,
                StrokeDashArray = reference.StrokeDashArray,
                Label = reference.Label?.Value,
                LabelPosition = reference.Label?.Position,
                IfOverflow = Overflow(reference.IfOverflow),
                Position = reference.Position,
                X1 = reference.Segment?.X1,
                Y1 = reference.Segment?.Y1,
                X2 = reference.Segment?.X2,
                Y2 = reference.Segment?.Y2,
                XAxisId = reference.XAxisId,
                YAxisId = reference.YAxisId,
            }, reference.Label), reference.Labels));
        }

        foreach (var area in spec.ReferenceAreas)
        {
            yield return (area.Order, Children(Labelled(new ReferenceArea { X1 = area.X1, X2 = area.X2, Y1 = area.Y1, Y2 = area.Y2, Fill = area.Fill, FillOpacity = area.FillOpacity, Stroke = area.Stroke, StrokeOpacity = area.StrokeOpacity, StrokeWidth = area.StrokeWidth, Label = area.Label?.Value, LabelPosition = area.Label?.Position, IfOverflow = Overflow(area.IfOverflow), XAxisId = area.XAxisId, YAxisId = area.YAxisId, Radius = area.Radius.IsDefaultOrEmpty ? null : string.Join(',', area.Radius.Select(radius => radius.ToString(CultureInfo.InvariantCulture))) }, area.Label), area.Labels));
        }

        foreach (var dot in spec.ReferenceDots)
        {
            yield return (dot.Order, Children(Labelled(new ReferenceDot { X = dot.X, Y = dot.Y, R = dot.R, Fill = dot.Fill, FillOpacity = dot.FillOpacity, Stroke = dot.Stroke, StrokeWidth = dot.StrokeWidth, Label = dot.Label?.Value, LabelPosition = dot.Label?.Position, IfOverflow = Overflow(dot.IfOverflow), XAxisId = dot.XAxisId, YAxisId = dot.YAxisId }, dot.Label), dot.Labels));
        }
    }

    /// <summary>A table's items as the dictionaries an app's data would be read like: numbers as numbers, texts as texts.</summary>
    public static List<IReadOnlyDictionary<string, object?>> Rows(DataTable table)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(table.Count);
        for (var i = 0; i < table.Count; i++)
        {
            var row = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var (key, column) in table.Columns)
            {
                var text = column[i];
                row[key] = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : text;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static string Overflow(OverflowKind kind) => kind switch
    {
        OverflowKind.Visible => "visible",
        OverflowKind.Hidden => "hidden",
        OverflowKind.ExtendDomain => "extendDomain",
        _ => "discard",
    };

    private static T Children<T>(T element, System.Collections.Immutable.ImmutableArray<LabelSpec> labels)
        where T : IChartElement
    {
        switch (element)
        {
            case ReferenceLine line:
                SpecControls.AddLabels(line.Labels, labels);
                break;
            case ReferenceArea area:
                SpecControls.AddLabels(area.Labels, labels);
                break;
            case ReferenceDot dot:
                SpecControls.AddLabels(dot.Labels, labels);
                break;
        }

        return element;
    }

    private static PointSeries Dotted(PointSeries series, ActiveStyle? dot, SvgPresentation? presentation = null)
    {
        PresentationControls.Of(series, presentation);
        if (dot is not null)
        {
            (series.DotRadius, series.DotFill, series.DotStroke) = (dot.R, dot.Fill, dot.Stroke);
            (series.DotStrokeWidth, series.DotFillOpacity, series.DotStrokeDashArray) = (dot.StrokeWidth, dot.FillOpacity, dot.StrokeDashArray);
        }

        return series;
    }

    /// <summary>An element's label props (shared by axes and references), as its spec's label gives them.</summary>
    public static T Labelled<T>(T element, LabelSpec? label)
        where T : AvaloniaObject
    {
        if (label is not null)
        {
            element.SetValue(Pictor.Axis.LabelAngleProperty, label.Angle);
            element.SetValue(Pictor.Axis.LabelOffsetProperty, label.Offset);
            element.SetValue(Pictor.Axis.LabelFillProperty, label.Fill);
            element.SetValue(Pictor.Axis.LabelFontSizeProperty, label.FontSize);
        }

        return element;
    }

    private static Bar Bar(SeriesSpec spec) => new()
    {
        StackId = spec.StackId,
        BarSize = spec.BarSize,
        MaxBarSize = spec.MaxBarSize,
        MinPointSize = spec.MinPointSize,
        Background = spec.Background,
        BackgroundStroke = spec.BackgroundProps?.Stroke,
        BackgroundStrokeWidth = spec.BackgroundProps?.StrokeWidth,
        BackgroundFillOpacity = spec.BackgroundProps?.FillOpacity,
        BackgroundRadius = spec.BackgroundRadius.IsDefaultOrEmpty ? null : string.Join(',', spec.BackgroundRadius.Select(radius => radius.ToString(CultureInfo.InvariantCulture))),
        Radius = spec.Radius.IsDefaultOrEmpty ? null : string.Join(',', spec.Radius.Select(radius => radius.ToString(CultureInfo.InvariantCulture))),
        ActiveFill = spec.ActiveBar?.Fill,
        ActiveStroke = spec.ActiveBar?.Stroke,
        ActiveStrokeWidth = spec.ActiveBar?.StrokeWidth,
        ActiveIndex = spec.ActiveIndex,
    };

    private static Series Series(SeriesSpec spec, System.Collections.Immutable.ImmutableArray<GradientSpec> gradients)
    {
        Series series = spec.Kind switch
        {
            SeriesKind.Line => Dotted(new Line { Type = spec.Curve, Dot = spec.Dot, ClipDot = spec.ClipDot, ConnectNulls = spec.ConnectNulls, ActiveDot = spec.ActiveDot ? spec.ActiveDotRadius : 0, StackId = spec.StackId, ActiveDotFill = spec.ActiveDotStyle?.Fill, ActiveDotStroke = spec.ActiveDotStyle?.Stroke, ActiveDotStrokeWidth = spec.ActiveDotStyle?.StrokeWidth }, spec.DotProps, spec.Presentation),
            SeriesKind.Area => Dotted(new Area { BaseValue = spec.BaseValue, Type = spec.Curve, Dot = spec.Dot, ClipDot = spec.ClipDot, ConnectNulls = spec.ConnectNulls, ActiveDot = spec.ActiveDot ? spec.ActiveDotRadius : 0, StackId = spec.StackId, ActiveDotFill = spec.ActiveDotStyle?.Fill, ActiveDotStroke = spec.ActiveDotStyle?.Stroke, ActiveDotStrokeWidth = spec.ActiveDotStyle?.StrokeWidth }, spec.DotProps, spec.Presentation),
            SeriesKind.Scatter => new Scatter { Shape = spec.Shape, Line = spec.JointLine, LineJointType = spec.LineJointType, LineType = spec.LineType, LineStroke = spec.LineStroke, LineStrokeWidth = spec.LineStrokeWidth, LineStrokeDashArray = spec.LineStrokeDashArray, ZAxisId = spec.ZAxisId, ActiveIndex = spec.ActiveIndex },
            _ => Bar(spec),
        };
        series.Data = spec.Data is { } table ? Rows(table) : null;
        SpecControls.Animated(series, spec.Animation);
        series.DataKey = spec.DataKey;
        series.Name = spec.Name;
        series.Stroke = ExampleBrushes.Brush(spec.Stroke, gradients);
        series.StrokeWidth = spec.StrokeWidth;
        series.Fill = ExampleBrushes.Brush(spec.Fill, gradients);
        series.FillOpacity = spec.FillOpacity;
        series.StrokeDashArray = spec.StrokeDashArray;
        series.XAxisId = spec.XAxisId;
        series.YAxisId = spec.YAxisId;
        series.Unit = spec.Unit;
        series.Hide = spec.Hide;
        series.TooltipType = spec.TooltipNone ? "none" : null;
        series.LegendType = spec.LegendType;
        SpecControls.AddLabelLists(series.Parts, spec.Labels, spec.LabelLists);

        foreach (var cell in spec.Cells)
        {
            series.Parts.Add(SpecControls.CellOf(cell));
        }

        foreach (var bar in spec.ErrorBars)
        {
            series.Parts.Add(PresentationControls.Of(new ErrorBar { DataKey = bar.DataKey, Width = bar.Width, StrokeWidth = bar.StrokeWidth, Stroke = bar.Stroke, Direction = bar.AlongX ? "x" : null }, bar.Presentation));
        }

        return series;
    }
}
