using System.Collections.Immutable;
using Aioros.Pictor.BoundValues;
using Aioros.Pictor.Svg;
using Aioros.Runtime.Rendering;
using Avalonia;

namespace Aioros.Pictor;

/// <summary>An element whose items run a command when clicked (Recharts' <c>onClick</c> on a graphical item or the legend).</summary>
internal interface IClickSource
{
    /// <summary>The command property.</summary>
    AvaloniaProperty CommandProperty { get; }
}

/// <summary>
/// How Aioros writes a chart on the web: Pictor's renderer, the chart's spec at its size as the argument (with the holes
/// its bound child properties fill), and the parts it reads and writes: the bound properties the plan follows, the
/// brush's two range inputs (fields bound to <c>StartIndex</c> and <c>EndIndex</c>), and the commands its items' buttons
/// run. A command is declared only when it is set, so a chart without one writes no button. A chart whose holes read
/// Avalonia's values (a paint, a dash pattern, a number list, a thickness) is written by <see cref="BoundChartRenderer"/>.
/// </summary>
internal static class ChartSlot
{
    public const string BrushStart = "brush-start";

    public const string BrushEnd = "brush-end";

    public const string Click = "click";

    public const string ItemClick = "item";

    public const string Zoom = "zoom";

    public const string ZoomPath = "zoom-path";

    public static WrittenSlot Of(Chart chart)
    {
        var width = double.IsNaN(chart.Width) ? chart.Bounds.Width : chart.Width;
        var height = double.IsNaN(chart.Height) ? chart.Bounds.Height : chart.Height;
        var bound = BoundParts.Find(chart, () => chart.Spec(width, height), Chart.Members, Declare, Known);
        var parts = ImmutableArray.CreateBuilder<SlotPart>();
        parts.AddRange(bound.Select(part => part.Part));
        var spec = chart.Spec(width, height);
        if (chart.Elements.OfType<Brush>().LastOrDefault() is { } brush && spec.Brush is not null)
        {
            Add(parts, SlotPart.Number(BrushStart, brush, nameof(Brush.StartIndex), 0, int.MaxValue));
            Add(parts, SlotPart.Number(BrushEnd, brush, nameof(Brush.EndIndex), 0, int.MaxValue));
            var gap = spec.Brush.Gap;
            Add(parts, SlotPart.Composite(Svg.BrushFields.Key(Svg.BrushFields.Slide, gap), BrushStart, BrushEnd));
            if (gap > 1)
            {
                Add(parts, SlotPart.Composite(Svg.BrushFields.Key(Svg.BrushFields.Start, gap), BrushStart, BrushEnd));
                Add(parts, SlotPart.Composite(Svg.BrushFields.Key(Svg.BrushFields.End, gap), BrushStart, BrushEnd));
            }
            spec = spec with { Brush = spec.Brush with { Fields = true } };
        }

        Commands(chart, parts);
        spec = Refused(chart, spec, parts);
        spec = Formatted(chart, spec, parts);
        ImmutableArray<SpecHole> holes = [.. bound.SelectMany(part => part.Holes)];
        var argument = SpecHoles.Write(spec, holes);
        return holes.Any(static hole => hole.ComparedWritten)
            ? WrittenSlot.Rendered<BoundChartRenderer>(argument, nameof(Chart.Data), parts.ToImmutable())
            : WrittenSlot.Rendered<ChartRenderer>(argument, nameof(Chart.Data), parts.ToImmutable());
    }

    /// <summary>
    /// The templates of custom content the compiler could not write as SVG: each declared as a part the plan refuses with
    /// its reason, so the page is rendered by Avalonia (Aioros's engine path), whose spec then carries what Avalonia laid
    /// out from the template for each item.
    /// </summary>
    private static Specs.ChartSpec Refused(Chart chart, Specs.ChartSpec spec, ImmutableArray<SlotPart>.Builder parts)
    {
        var refused = Content.ContentWalk.Refused(spec).Select(content => content.Refused!).Distinct(StringComparer.Ordinal).ToList();
        if (refused.Count == 0)
        {
            return spec;
        }

        foreach (var reason in refused)
        {
            parts.Add(SlotPart.Read(reason, chart, RefusedPart));
        }

        var plot = Painting.Plots.Compute(spec, Data.ChartData.From(chart.Data, Chart.Members));
        return spec with { Resolved = ContentRecorder.Of(plot, chart.Templates) };
    }

    /// <summary>
    /// A chart with a formatter function (a converter on an axis' <c>TickFormatter</c> or a tooltip's <c>Formatter</c> or
    /// <c>LabelFormatter</c>): the spec names each converter the app's generated converters rebuild, which the web's
    /// writer runs, and records the texts the functions write for this data; a converter it cannot name is a part the
    /// plan refuses, since the plan records the spec at build time without the functions, so the page is rendered by
    /// Avalonia's engine path, whose spec then carries the texts for its data.
    /// </summary>
    private static Specs.ChartSpec Formatted(Chart chart, Specs.ChartSpec spec, ImmutableArray<SlotPart>.Builder parts)
    {
        if (!Text.FormatterRecording.Has(spec))
        {
            return spec;
        }

        if (Text.FormatterRecording.Unrebuilt(spec))
        {
            parts.Add(SlotPart.Read(FormatterReason, chart, RefusedPart));
        }

        return Text.FormatterRecording.Record(spec, chart.Data, Chart.Members);
    }

    /// <summary>Why a chart with a formatter function is rendered by Avalonia.</summary>
    public const string FormatterReason = "a formatter function, which runs where Avalonia renders the page";

    /// <summary>The property a refused template's part names, which no chart has, so that the plan refuses the part with the template's reason.</summary>
    public const string RefusedPart = "(a template the web cannot write as SVG)";

    /// <summary>The key of an element's command: its place among the chart's children.</summary>
    public static string CommandKey(int order) => "c" + order.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>A bound property as a part: the brush's indexes as the fields they are, any other as a value read.</summary>
    private static SlotPart Declare(AvaloniaObject target, AvaloniaProperty property, string key) => (target, property.Name) switch
    {
        (Brush, nameof(Brush.StartIndex)) => SlotPart.Number(BrushStart, target, property.Name, 0, int.MaxValue),
        (Brush, nameof(Brush.EndIndex)) => SlotPart.Number(BrushEnd, target, property.Name, 0, int.MaxValue),
        (Treemap treemap, nameof(Treemap.Zoom)) => ZoomsByChoice(treemap) ? SlotPart.Choice(ZoomPath, target, property.Name) : SlotPart.Text(ZoomPath, target, property.Name),
        _ => SlotPart.Read(key, target, property.Name),
    };

    /// <summary>The holes of the properties bound both ways, which no marker may reach: the brush's indexes.</summary>
    private static (HoleKind Kind, string Path)? Known(AvaloniaObject target, AvaloniaProperty property) => (target, property.Name) switch
    {
        (Brush, nameof(Brush.StartIndex)) => (HoleKind.Whole, "/Brush/StartIndex"),
        (Brush, nameof(Brush.EndIndex)) => (HoleKind.Whole, "/Brush/EndIndex"),
        (Treemap, nameof(Treemap.Zoom)) => (HoleKind.Text, "/Treemap/Zoom"),
        _ => null,
    };

    /// <summary>The commands the chart's buttons run: the chart's, its items', a nest treemap's zoom (with its zoom as a field the buttons' posts carry), and each child's that is set.</summary>
    private static void Commands(Chart chart, ImmutableArray<SlotPart>.Builder parts)
    {
        if (chart.IsSet(Chart.ClickCommandProperty))
        {
            parts.Add(SlotPart.Command(Click, chart, nameof(Chart.ClickCommand)));
        }

        if (chart.IsSet(Chart.ItemClickCommandProperty))
        {
            parts.Add(SlotPart.Command(ItemClick, chart, nameof(Chart.ItemClickCommand)));
        }

        if (chart is Treemap { IsNest: true } treemap && !ZoomsByChoice(treemap))
        {
            Add(parts, SlotPart.Text(ZoomPath, treemap, nameof(Treemap.Zoom)));
            parts.Add(SlotPart.Command(Zoom, treemap, nameof(Treemap.ZoomCommand)));
        }

        var order = 0;
        foreach (var element in chart.Elements)
        {
            if (element is IClickSource source and AvaloniaObject target && target.IsSet(source.CommandProperty))
            {
                parts.Add(SlotPart.Command(CommandKey(order), target, source.CommandProperty.Name));
            }

            order++;
        }
    }

    /// <summary>
    /// Whether a nest treemap zooms on the web through a choice field: its zoom bound to the model and its own zoom command
    /// (none set by the app), so a node's or a box's option binds the zoom on the plan, where a command's button would
    /// post to Aioros's engine.
    /// </summary>
    public static bool ZoomsByChoice(Treemap treemap) =>
        treemap.IsNest && treemap.ZoomCommand is Zooming && Avalonia.Data.BindingOperations.GetBindingExpressionBase(treemap, Treemap.ZoomProperty) is not null;

    private static void Add(ImmutableArray<SlotPart>.Builder parts, SlotPart part)
    {
        if (!parts.Any(existing => existing.Key == part.Key))
        {
            parts.Add(part);
        }
    }
}
