using Aioros.Pictor.Specs;
using Avalonia;

namespace Aioros.Pictor;

/// <summary>Recharts' <c>Sankey</c>: its data holds the nodes and the links (members <c>nodes</c> and <c>links</c>, or <c>Nodes</c> and <c>Links</c>).</summary>
public sealed class Sankey : Chart
{
    /// <summary>Defines <see cref="NameKey"/>.</summary>
    public static readonly StyledProperty<string> NameKeyProperty = AvaloniaProperty.Register<Sankey, string>(nameof(NameKey), "name");

    /// <summary>Defines <see cref="NodeTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> NodeTemplateProperty = AvaloniaProperty.Register<Sankey, Avalonia.Controls.Templates.IDataTemplate?>(nameof(NodeTemplate));

    /// <summary>Defines <see cref="LinkTemplate"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> LinkTemplateProperty = AvaloniaProperty.Register<Sankey, Avalonia.Controls.Templates.IDataTemplate?>(nameof(LinkTemplate));

    /// <summary>The custom content drawn for each node (Recharts' <c>node</c> element): a template whose data context is the node's <see cref="SankeyNodeContext"/>.</summary>
    public Avalonia.Controls.Templates.IDataTemplate? NodeTemplate { get => GetValue(NodeTemplateProperty); set => SetValue(NodeTemplateProperty, value); }

    /// <summary>The custom content drawn for each link (Recharts' <c>link</c> element): a template whose data context is the link's <see cref="SankeyLinkContext"/>.</summary>
    public Avalonia.Controls.Templates.IDataTemplate? LinkTemplate { get => GetValue(LinkTemplateProperty); set => SetValue(LinkTemplateProperty, value); }

    /// <summary>Defines <see cref="NodePadding"/>.</summary>
    public static readonly StyledProperty<double> NodePaddingProperty = AvaloniaProperty.Register<Sankey, double>(nameof(NodePadding), 10);

    /// <summary>Defines <see cref="NodeWidth"/>.</summary>
    public static readonly StyledProperty<double> NodeWidthProperty = AvaloniaProperty.Register<Sankey, double>(nameof(NodeWidth), 10);

    /// <summary>Defines <see cref="LinkCurvature"/>.</summary>
    public static readonly StyledProperty<double> LinkCurvatureProperty = AvaloniaProperty.Register<Sankey, double>(nameof(LinkCurvature), 0.5);

    /// <summary>Defines <see cref="Iterations"/>.</summary>
    public static readonly StyledProperty<int> IterationsProperty = AvaloniaProperty.Register<Sankey, int>(nameof(Iterations), 32);

    /// <summary>Defines <see cref="Sort"/>.</summary>
    public static readonly StyledProperty<bool> SortProperty = AvaloniaProperty.Register<Sankey, bool>(nameof(Sort), true);

    /// <summary>Defines <see cref="NodeFill"/>.</summary>
    public static readonly StyledProperty<string?> NodeFillProperty = AvaloniaProperty.Register<Sankey, string?>(nameof(NodeFill));

    /// <summary>Defines <see cref="NodeFillOpacity"/>.</summary>
    public static readonly StyledProperty<double> NodeFillOpacityProperty = AvaloniaProperty.Register<Sankey, double>(nameof(NodeFillOpacity), 0.8);

    /// <summary>Defines <see cref="NodeStroke"/>.</summary>
    public static readonly StyledProperty<string?> NodeStrokeProperty = AvaloniaProperty.Register<Sankey, string?>(nameof(NodeStroke));

    /// <summary>Defines <see cref="LinkStroke"/>.</summary>
    public static readonly StyledProperty<string?> LinkStrokeProperty = AvaloniaProperty.Register<Sankey, string?>(nameof(LinkStroke));

    /// <summary>Defines <see cref="LinkStrokeOpacity"/>.</summary>
    public static readonly StyledProperty<double> LinkStrokeOpacityProperty = AvaloniaProperty.Register<Sankey, double>(nameof(LinkStrokeOpacity), 0.2);

    /// <summary>Defines <see cref="NodeStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> NodeStrokeWidthProperty = AvaloniaProperty.Register<Sankey, double?>(nameof(NodeStrokeWidth));

    /// <summary>Defines <see cref="NodeStrokeOpacity"/>.</summary>
    public static readonly StyledProperty<double?> NodeStrokeOpacityProperty = AvaloniaProperty.Register<Sankey, double?>(nameof(NodeStrokeOpacity));

    /// <summary>Defines <see cref="NodeStrokeDashArray"/>.</summary>
    public static readonly StyledProperty<object?> NodeStrokeDashArrayProperty = AvaloniaProperty.Register<Sankey, object?>(nameof(NodeStrokeDashArray));

    /// <summary>Defines <see cref="LinkStrokeWidth"/>.</summary>
    public static readonly StyledProperty<double?> LinkStrokeWidthProperty = AvaloniaProperty.Register<Sankey, double?>(nameof(LinkStrokeWidth));

    /// <summary>Defines <see cref="LinkFill"/>.</summary>
    public static readonly StyledProperty<string?> LinkFillProperty = AvaloniaProperty.Register<Sankey, string?>(nameof(LinkFill));

    /// <summary>Defines <see cref="LinkStrokeDashArray"/>.</summary>
    public static readonly StyledProperty<object?> LinkStrokeDashArrayProperty = AvaloniaProperty.Register<Sankey, object?>(nameof(LinkStrokeDashArray));

    /// <summary>The nodes' stroke width.</summary>
    public double? NodeStrokeWidth { get => GetValue(NodeStrokeWidthProperty); set => SetValue(NodeStrokeWidthProperty, value); }

    /// <summary>The nodes' stroke opacity.</summary>
    public double? NodeStrokeOpacity { get => GetValue(NodeStrokeOpacityProperty); set => SetValue(NodeStrokeOpacityProperty, value); }

    /// <summary>The nodes' stroke dash pattern.</summary>
    public object? NodeStrokeDashArray { get => GetValue(NodeStrokeDashArrayProperty); set => SetValue(NodeStrokeDashArrayProperty, value); }

    /// <summary>One stroke width for every link instead of its value's width (Recharts spreads its <c>link</c> props over it).</summary>
    public double? LinkStrokeWidth { get => GetValue(LinkStrokeWidthProperty); set => SetValue(LinkStrokeWidthProperty, value); }

    /// <summary>The links' fill; none by default.</summary>
    public string? LinkFill { get => GetValue(LinkFillProperty); set => SetValue(LinkFillProperty, value); }

    /// <summary>The links' stroke dash pattern.</summary>
    public object? LinkStrokeDashArray { get => GetValue(LinkStrokeDashArrayProperty); set => SetValue(LinkStrokeDashArrayProperty, value); }

    /// <summary>The data key of the nodes' names.</summary>
    public string NameKey { get => GetValue(NameKeyProperty); set => SetValue(NameKeyProperty, value); }

    /// <summary>The vertical gap between nodes of one column.</summary>
    public double NodePadding { get => GetValue(NodePaddingProperty); set => SetValue(NodePaddingProperty, value); }

    /// <summary>The nodes' width.</summary>
    public double NodeWidth { get => GetValue(NodeWidthProperty); set => SetValue(NodeWidthProperty, value); }

    /// <summary>Where a link's control points sit, as a share of its length.</summary>
    public double LinkCurvature { get => GetValue(LinkCurvatureProperty); set => SetValue(LinkCurvatureProperty, value); }

    /// <summary>How many relaxation passes place the nodes.</summary>
    public int Iterations { get => GetValue(IterationsProperty); set => SetValue(IterationsProperty, value); }

    /// <summary>Whether a column's nodes are sorted by position at every pass.</summary>
    public bool Sort { get => GetValue(SortProperty); set => SetValue(SortProperty, value); }

    /// <summary>The nodes' fill (Recharts' <c>node</c> fill).</summary>
    public string? NodeFill { get => GetValue(NodeFillProperty); set => SetValue(NodeFillProperty, value); }

    /// <summary>The nodes' fill opacity.</summary>
    public double NodeFillOpacity { get => GetValue(NodeFillOpacityProperty); set => SetValue(NodeFillOpacityProperty, value); }

    /// <summary>The nodes' stroke, if any.</summary>
    public string? NodeStroke { get => GetValue(NodeStrokeProperty); set => SetValue(NodeStrokeProperty, value); }

    /// <summary>The links' stroke (Recharts' <c>link</c> stroke).</summary>
    public string? LinkStroke { get => GetValue(LinkStrokeProperty); set => SetValue(LinkStrokeProperty, value); }

    /// <summary>The links' stroke opacity.</summary>
    public double LinkStrokeOpacity { get => GetValue(LinkStrokeOpacityProperty); set => SetValue(LinkStrokeOpacityProperty, value); }

    private protected override ChartKind Kind => ChartKind.Sankey;

    private protected override ChartSpec Complete(ChartSpec spec, SpecBuilder builder) => spec with
    {
        Sankey = new SankeySpec
        {
            NodeContent = builder.Content(NodeTemplate, Content.ContentSlots.SankeyNode, this, nameof(NodeTemplate)),
            LinkContent = builder.Content(LinkTemplate, Content.ContentSlots.SankeyLink, this, nameof(LinkTemplate)),
            NameKey = NameKey,
            NodePadding = NodePadding,
            NodeWidth = NodeWidth,
            LinkCurvature = LinkCurvature,
            Iterations = Iterations,
            Sort = Sort,
            NodeFill = NodeFill,
            NodeFillOpacity = NodeFillOpacity,
            NodeStroke = NodeStroke,
            NodeStrokeWidth = NodeStrokeWidth,
            NodeStrokeOpacity = NodeStrokeOpacity,
            NodeStrokeDashArray = SpecText.Dashes(NodeStrokeDashArray),
            LinkStrokeWidth = LinkStrokeWidth,
            LinkFill = LinkFill,
            LinkStrokeDashArray = SpecText.Dashes(LinkStrokeDashArray),
            LinkStroke = LinkStroke,
            LinkStrokeOpacity = LinkStrokeOpacity,
        },
    };
}
