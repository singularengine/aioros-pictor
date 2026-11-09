using System.Collections.Immutable;
using System.Globalization;
using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Collections;

namespace Aioros.Pictor;

/// <summary>A child of a chart (an axis, a grid, a series, a reference, the legend or the tooltip), as Recharts' children.</summary>
public interface IChartElement
{
    /// <summary>Raised when one of the element's properties, or one of its own children, changes.</summary>
    event EventHandler? Changed;
}

/// <summary>A child of a series (a <see cref="LabelList"/>, a <see cref="Cell"/>, an <see cref="ErrorBar"/>) or of a legend (a <see cref="LegendPayload"/>).</summary>
public interface ISeriesPart
{
    /// <summary>Raised when one of the part's properties changes.</summary>
    event EventHandler? Changed;
}

/// <summary>Collects a chart's spec from its elements, in the order the XAML declares them.</summary>
internal sealed class SpecBuilder(Data.DataMembers members)
{
    /// <summary>How the items of a series' own data are read.</summary>
    public Data.DataMembers Members { get; } = members;

    /// <summary>The chart's theme, for the defaults its elements resolve themselves (the tooltip's separator).</summary>
    public Theming.ChartTheme Theme { get; init; } = Theming.ChartTheme.Light;

    /// <summary>The chart's compiled templates of custom content.</summary>
    public ContentTemplates? Templates { get; init; }

    /// <summary>A slot's template compiled to SVG with holes (or refused with its reason), once per template; null when the slot has none.</summary>
    public ContentSpec? Content(Avalonia.Controls.Templates.IDataTemplate? template, Content.ContentSlot slot, StyledElement owner, string property) =>
        template is null ? null : Templates is { } templates ? templates.Compile(template, slot, owner, property) : ContentCompiler.Compile(template, slot, owner, property);

    /// <summary>The theme the spec carries: none for Recharts' defaults, so a light chart's spec is written as before.</summary>
    public Theming.ChartTheme? SpecTheme => ReferenceEquals(Theme, Theming.ChartTheme.Light) ? null : Theme;

    public GridSpec? Grid { get; set; }

    public List<AxisSpec> XAxes { get; } = [];

    public List<AxisSpec> YAxes { get; } = [];

    public List<SeriesSpec> Series { get; } = [];

    public List<ReferenceLineSpec> References { get; } = [];

    public LegendSpec? Legend { get; set; }

    public TooltipSpec? Tooltip { get; set; }

    /// <summary>A polar chart's grid, axes, pies, radars and radial bars.</summary>
    public PolarSpec Polar { get; set; } = new();
    public List<FunnelSpec> Funnels { get; } = [];

    public List<ZAxisSpec> ZAxes { get; } = [];

    public List<ReferenceAreaSpec> ReferenceAreas { get; } = [];

    public List<ReferenceDotSpec> ReferenceDots { get; } = [];

    public BrushSpec? Brush { get; set; }

    /// <summary>The chart's <c>Customized</c> children.</summary>
    public List<CustomizedSpec> Customized { get; } = [];

    /// <summary>The own items of series, by their index in <see cref="Series"/>, read into tables once every key is known.</summary>
    public Dictionary<int, System.Collections.IEnumerable> SeriesData { get; } = [];

    /// <summary>The gradients the elements' brushes define, each once.</summary>
    public List<GradientSpec> Gradients { get; } = [];

    /// <summary>
    /// A paint as the spec writes it: a color text as it is, a solid brush as its color, a linear gradient brush as a
    /// reference to the gradient it adds; null for nothing or a brush the charts cannot paint.
    /// </summary>
    public string? Paint(object? value) => AvaloniaValues.Plain(value, null) switch
    {
        string text => text,
        GradientSpec gradient => Reference(gradient),
        _ => null,
    };

    private string Reference(GradientSpec gradient)
    {
        if (!Gradients.Exists(known => known.Id == gradient.Id))
        {
            Gradients.Add(gradient);
        }

        return "url(#" + gradient.Id + ")";
    }
}

/// <summary>An element with items of its own (a series', a pie's or a funnel's <c>Data</c>), which the chart follows while it is in the visual tree.</summary>
internal interface IOwnItems
{
    System.Collections.IEnumerable? OwnItems { get; }
}

internal interface ISpecSource
{
    void Apply(SpecBuilder builder, int order);
}

/// <summary>Parses the text forms XAML gives to Recharts' props.</summary>
internal static class SpecText
{
    public static ImmutableArray<double> Numbers(string? text) => BoundValues.BoundLists.Numbers(text);

    /// <summary>A number list from a text, a number, a list of numbers or a <see cref="Avalonia.CornerRadius"/> (Recharts' <c>[topLeft, topRight, bottomRight, bottomLeft]</c>); none for any other value.</summary>
    public static ImmutableArray<double> Numbers(object? value) => BoundValues.BoundLists.Numbers(AvaloniaValues.Plain(value, null)) ?? [];

    /// <summary>A dash pattern from a text as it is or a list of numbers (Avalonia's <c>AvaloniaList&lt;double&gt;</c>, as <c>Shape.StrokeDashArray</c> takes).</summary>
    public static string? Dashes(object? value) => BoundValues.BoundLists.Dashes(value);

    public static ImmutableArray<string> Words(string? text) =>
        string.IsNullOrWhiteSpace(text) ? [] : [.. text.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)];

    public static (string? Min, string? Max) Domain(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (null, null);
        }

        var parts = text.Split(',', 2, StringSplitOptions.TrimEntries);
        return (parts[0], parts.Length > 1 ? parts[1] : null);
    }

    /// <summary>Texts separated by commas, trimmed: an explicit ticks list or a category domain.</summary>
    public static ImmutableArray<string> Texts(string? text) =>
        string.IsNullOrWhiteSpace(text) ? [] : [.. text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];

    /// <summary>A line's own SVG props, or null when none is set.</summary>
    public static LineStyle? Line(string? stroke, double? width, string? dash) =>
        stroke is null && width is null && dash is null ? null : new LineStyle(stroke, width, dash);

    /// <summary>An element's label from its text and its label props (shared by axes and references).</summary>
    public static LabelSpec? Label(string? value, LabelPosition? position, AvaloniaObject owner) => value is null ? null : new LabelSpec
    {
        Value = value,
        Position = position,
        Angle = owner.GetValue(Axis.LabelAngleProperty),
        Offset = owner.GetValue(Axis.LabelOffsetProperty),
        Fill = owner.GetValue(Axis.LabelFillProperty),
        FontSize = owner.GetValue(Axis.LabelFontSizeProperty),
    };

    public static OverflowKind Overflow(string? text) => text switch
    {
        "visible" or "Visible" => OverflowKind.Visible,
        "hidden" or "Hidden" => OverflowKind.Hidden,
        "extendDomain" or "ExtendDomain" => OverflowKind.ExtendDomain,
        _ => OverflowKind.Discard,
    };

    public static (TickInterval Interval, int Count) Interval(string? text) => text switch
    {
        null or "preserveEnd" or "PreserveEnd" => (TickInterval.PreserveEnd, 0),
        "preserveStart" or "PreserveStart" => (TickInterval.PreserveStart, 0),
        "preserveStartEnd" or "PreserveStartEnd" => (TickInterval.PreserveStartEnd, 0),
        "equidistantPreserveStart" or "EquidistantPreserveStart" => (TickInterval.EquidistantPreserveStart, 0),
        _ => (TickInterval.Fixed, int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ? count : 0),
    };
}

/// <summary>Raises <see cref="Changed"/> for an element's property changes and its children's.</summary>
internal sealed class ChangeRelay
{
    public event EventHandler? Changed;

    public void Raise(object? sender) => Changed?.Invoke(sender, EventArgs.Empty);

    /// <summary>An element's own classes as Recharts' <c>className</c> (its pseudo-classes left out), or null.</summary>
    public static string? ClassName(StyledElement element)
    {
        var names = element.Classes.Where(static name => !name.StartsWith(':')).ToList();
        return names.Count == 0 ? null : string.Join(' ', names);
    }

    /// <summary>Follows an element's classes: a change of its own (not of a pseudo-class) is its change.</summary>
    public void FollowClasses(StyledElement element)
    {
        var last = ClassName(element);
        element.Classes.CollectionChanged += (_, _) =>
        {
            var now = ClassName(element);
            if (now != last)
            {
                last = now;
                Raise(element);
            }
        };
    }

    /// <summary>Follows an element's children: their changes are the element's, and they are its logical children, so styles, resources and the data context reach them.</summary>
    public void Follow<T>(AvaloniaList<T> children, object owner, IList<Avalonia.LogicalTree.ILogical> logical)
        where T : class
    {
        EventHandler changed = (_, _) => Raise(owner);
        var followed = new ChildChanges<ISeriesPart>(part => part.Changed += changed, part => part.Changed -= changed);
        children.CollectionChanged += (_, args) =>
        {
            foreach (var removed in args.OldItems?.OfType<Avalonia.LogicalTree.ILogical>() ?? [])
            {
                logical.Remove(removed);
            }

            foreach (var added in (args.NewItems?.OfType<Avalonia.LogicalTree.ILogical>() ?? []).Where(added => !logical.Contains(added)))
            {
                logical.Add(added);
            }

            followed.Follow(children.OfType<ISeriesPart>());
            Raise(owner);
        };
    }
}
