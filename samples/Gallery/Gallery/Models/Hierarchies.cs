namespace Gallery.Models;

/// <summary>One stage of a funnel.</summary>
public sealed record Stage(string Name, double Value, string Fill);

/// <summary>A node of a treemap: a leaf's size, or a parent's children.</summary>
public sealed record Package(string Name, double Size, IReadOnlyList<Package>? Children = null);

/// <summary>A node of a Sankey.</summary>
public sealed record FlowNode(string Name);

/// <summary>A link of a Sankey, between two nodes by index.</summary>
public sealed record FlowLink(int Source, int Target, double Value);

/// <summary>A Sankey's nodes and links.</summary>
public sealed record Flow(IReadOnlyList<FlowNode> Nodes, IReadOnlyList<FlowLink> Links);

/// <summary>The data of Recharts' funnel, treemap and Sankey examples.</summary>
public sealed class Hierarchies
{
    /// <summary>The data of the page's motion demo, at the version its "Change data" button moves on.</summary>
    public Motion Motion { get; } = new();

    /// <summary>The nested treemap's zoomed node, which its nodes and nest index set through a choice field the plan binds.</summary>
    public string? Zoom { get; set; }

    public IReadOnlyList<Stage> Funnel { get; } =
    [
        new("Impression", 100, "#8884d8"),
        new("Click", 80, "#83a6ed"),
        new("Visit", 50, "#8dd1e1"),
        new("Consult", 40, "#82ca9d"),
        new("Order", 26, "#a4de6c"),
    ];

    public IReadOnlyList<Package> Flare { get; } =
    [
        new("axis", 0,
        [
            new("Axes", 1302),
            new("Axis", 24593),
            new("AxisGridLine", 652),
            new("AxisLabel", 636),
            new("CartesianAxes", 6703),
        ]),
        new("controls", 0,
        [
            new("AnchorControl", 2138),
            new("ClickControl", 3824),
            new("Control", 1353),
            new("ControlList", 4665),
            new("DragControl", 2649),
            new("ExpandControl", 2832),
            new("HoverControl", 4896),
            new("IControl", 763),
            new("PanZoomControl", 5222),
            new("SelectionControl", 7862),
            new("TooltipControl", 8435),
        ]),
        new("data", 0,
        [
            new("Data", 20544),
            new("DataList", 19788),
            new("DataSprite", 10349),
            new("EdgeSprite", 3301),
            new("NodeSprite", 19382),
            new("render", 0,
            [
                new("ArrowType", 698),
                new("EdgeRenderer", 5569),
                new("IRenderer", 353),
                new("ShapeRenderer", 2247),
            ]),
            new("ScaleBinding", 11275),
            new("Tree", 7147),
            new("TreeBuilder", 9930),
        ]),
        new("events", 0,
        [
            new("DataEvent", 7313),
            new("SelectionEvent", 6880),
            new("TooltipEvent", 3701),
            new("VisualizationEvent", 2117),
        ]),
        new("legend", 0,
        [
            new("Legend", 20859),
            new("LegendItem", 4614),
            new("LegendRange", 10530),
        ]),
        new("operator", 0,
        [
            new("distortion", 0,
            [
                new("BifocalDistortion", 4461),
                new("Distortion", 6314),
                new("FisheyeDistortion", 3444),
            ]),
            new("encoder", 0,
            [
                new("ColorEncoder", 3179),
                new("Encoder", 4060),
                new("PropertyEncoder", 4138),
                new("ShapeEncoder", 1690),
                new("SizeEncoder", 1830),
            ]),
            new("filter", 0,
            [
                new("FisheyeTreeFilter", 5219),
                new("GraphDistanceFilter", 3165),
                new("VisibilityFilter", 3509),
            ]),
            new("IOperator", 1286),
            new("label", 0,
            [
                new("Labeler", 9956),
                new("RadialLabeler", 3899),
                new("StackedAreaLabeler", 3202),
            ]),
            new("layout", 0,
            [
                new("AxisLayout", 6725),
                new("BundledEdgeRouter", 3727),
                new("CircleLayout", 9317),
                new("CirclePackingLayout", 12003),
                new("DendrogramLayout", 4853),
                new("ForceDirectedLayout", 8411),
                new("IcicleTreeLayout", 4864),
                new("IndentedTreeLayout", 3174),
                new("Layout", 7881),
                new("NodeLinkTreeLayout", 12870),
                new("PieLayout", 2728),
                new("RadialTreeLayout", 12348),
                new("RandomLayout", 870),
                new("StackedAreaLayout", 9121),
                new("TreeMapLayout", 9191),
            ]),
            new("Operator", 2490),
            new("OperatorList", 5248),
            new("OperatorSequence", 4190),
            new("OperatorSwitch", 2581),
            new("SortOperator", 2023),
        ]),
    ];

    public IReadOnlyList<Package> FlareTop => [.. Flare.Take(5)];

    public Flow Visits { get; } = new(
        [new("Visit"), new("Direct-Favourite"), new("Page-Click"), new("Detail-Favourite"), new("Lost")],
        [
            new(0, 1, 3728.3),
            new(0, 2, 354170),
            new(2, 3, 62429),
            new(2, 4, 291741),
        ]);

    public Flow Budget { get; } = new(
        [new("Salary"), new("Bonus"), new("Income"), new("Rent"), new("Food"), new("Savings"), new("Groceries"), new("Restaurants"), new("Stocks"), new("Bonds")],
        [
            new(0, 2, 4200),
            new(1, 2, 800),
            new(2, 3, 1500),
            new(2, 4, 900),
            new(2, 5, 2600),
            new(4, 6, 550),
            new(4, 7, 350),
            new(5, 8, 1700),
            new(5, 9, 900),
        ]);
}
