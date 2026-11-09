import {node, off} from "./common.mjs";

export const funnel = [
  {value: 100, name: "Impression", fill: "#8884d8"},
  {value: 80, name: "Click", fill: "#83a6ed"},
  {value: 50, name: "Visit", fill: "#8dd1e1"},
  {value: 40, name: "Consult", fill: "#82ca9d"},
  {value: 26, name: "Order", fill: "#a4de6c"},
];

export const flare = [
  {name: "axis", children: [
    {name: "Axes", size: 1302}, {name: "Axis", size: 24593}, {name: "AxisGridLine", size: 652},
    {name: "AxisLabel", size: 636}, {name: "CartesianAxes", size: 6703}]},
  {name: "controls", children: [
    {name: "AnchorControl", size: 2138}, {name: "ClickControl", size: 3824}, {name: "Control", size: 1353},
    {name: "ControlList", size: 4665}, {name: "DragControl", size: 2649}, {name: "ExpandControl", size: 2832},
    {name: "HoverControl", size: 4896}, {name: "IControl", size: 763}, {name: "PanZoomControl", size: 5222},
    {name: "SelectionControl", size: 7862}, {name: "TooltipControl", size: 8435}]},
  {name: "data", children: [
    {name: "Data", size: 20544}, {name: "DataList", size: 19788}, {name: "DataSprite", size: 10349},
    {name: "EdgeSprite", size: 3301}, {name: "NodeSprite", size: 19382},
    {name: "render", children: [
      {name: "ArrowType", size: 698}, {name: "EdgeRenderer", size: 5569}, {name: "IRenderer", size: 353},
      {name: "ShapeRenderer", size: 2247}]},
    {name: "ScaleBinding", size: 11275}, {name: "Tree", size: 7147}, {name: "TreeBuilder", size: 9930}]},
  {name: "events", children: [
    {name: "DataEvent", size: 7313}, {name: "SelectionEvent", size: 6880}, {name: "TooltipEvent", size: 3701},
    {name: "VisualizationEvent", size: 2117}]},
  {name: "legend", children: [
    {name: "Legend", size: 20859}, {name: "LegendItem", size: 4614}, {name: "LegendRange", size: 10530}]},
  {name: "operator", children: [
    {name: "distortion", children: [
      {name: "BifocalDistortion", size: 4461}, {name: "Distortion", size: 6314}, {name: "FisheyeDistortion", size: 3444}]},
    {name: "encoder", children: [
      {name: "ColorEncoder", size: 3179}, {name: "Encoder", size: 4060}, {name: "PropertyEncoder", size: 4138},
      {name: "ShapeEncoder", size: 1690}, {name: "SizeEncoder", size: 1830}]},
    {name: "filter", children: [
      {name: "FisheyeTreeFilter", size: 5219}, {name: "GraphDistanceFilter", size: 3165},
      {name: "VisibilityFilter", size: 3509}]},
    {name: "IOperator", size: 1286},
    {name: "label", children: [
      {name: "Labeler", size: 9956}, {name: "RadialLabeler", size: 3899}, {name: "StackedAreaLabeler", size: 3202}]},
    {name: "layout", children: [
      {name: "AxisLayout", size: 6725}, {name: "BundledEdgeRouter", size: 3727}, {name: "CircleLayout", size: 9317},
      {name: "CirclePackingLayout", size: 12003}, {name: "DendrogramLayout", size: 4853},
      {name: "ForceDirectedLayout", size: 8411}, {name: "IcicleTreeLayout", size: 4864},
      {name: "IndentedTreeLayout", size: 3174}, {name: "Layout", size: 7881}, {name: "NodeLinkTreeLayout", size: 12870},
      {name: "PieLayout", size: 2728}, {name: "RadialTreeLayout", size: 12348}, {name: "RandomLayout", size: 870},
      {name: "StackedAreaLayout", size: 9121}, {name: "TreeMapLayout", size: 9191}]},
    {name: "Operator", size: 2490}, {name: "OperatorList", size: 5248}, {name: "OperatorSequence", size: 4190},
    {name: "OperatorSwitch", size: 2581}, {name: "SortOperator", size: 2023}]},
];

export const visits = {
  nodes: [{name: "Visit"}, {name: "Direct-Favourite"}, {name: "Page-Click"}, {name: "Detail-Favourite"}, {name: "Lost"}],
  links: [
    {source: 0, target: 1, value: 3728.3},
    {source: 0, target: 2, value: 354170},
    {source: 2, target: 3, value: 62429},
    {source: 2, target: 4, value: 291741},
  ],
};

const budget = {
  nodes: [{name: "Salary"}, {name: "Bonus"}, {name: "Income"}, {name: "Rent"}, {name: "Food"}, {name: "Savings"},
    {name: "Groceries"}, {name: "Restaurants"}, {name: "Stocks"}, {name: "Bonds"}],
  links: [
    {source: 0, target: 2, value: 4200}, {source: 1, target: 2, value: 800},
    {source: 2, target: 3, value: 1500}, {source: 2, target: 4, value: 900}, {source: 2, target: 5, value: 2600},
    {source: 4, target: 6, value: 550}, {source: 4, target: 7, value: 350},
    {source: 5, target: 8, value: 1700}, {source: 5, target: 9, value: 900},
  ],
};

const treemap = {kind: "Treemap", width: 500, height: 375, margin: null, data: flare};

export const examples = [
  {
    name: "FunnelChart", kind: "FunnelChart", width: 700, height: 432, margin: {right: 30}, data: null,
    source: "www/src/docs/exampleComponents/FunnelChart/FunnelChartExample.tsx (a fixed 700 x 432 instead of the responsive aspect ratio 1.618)",
    children: [node("Tooltip"),
      node("Funnel", {dataKey: "value", data: funnel, ...off},
        [node("LabelList", {position: "right", fill: "#000", stroke: "none", dataKey: "name"})])],
  },
  {
    name: "ReversedFunnelChart", kind: "FunnelChart", width: 700, height: 432, margin: {right: 30}, data: null,
    source: "www/src/docs/exampleComponents/Funnel/FunnelExample.tsx (reversed, labels at an offset of 20; the custom active shape left out, a legend added)",
    children: [node("Tooltip"), node("Legend"),
      node("Funnel", {dataKey: "value", data: funnel, reversed: true, ...off},
        [node("LabelList", {position: "right", fill: "#000", stroke: "none", dataKey: "name", offset: 20})])],
  },
  {
    name: "SimpleTreemap", ...treemap,
    source: "www/src/docs/exampleComponents/TreeMap/SimpleTreemap.tsx (a fixed 500 x 375; stroke and fill as recharts.org's 2.x example sets them)",
    children: [],
    props: {dataKey: "size", aspectRatio: 4 / 3, stroke: "#fff", fill: "#8884d8", ...off},
  },
  {
    name: "ColorPanelTreemap", ...treemap,
    source: "www/src/docs/exampleComponents/TreeMap/CustomContentTreemap.tsx (the custom content replaced by Recharts' default content with the example's six colors as colorPanel; a tooltip)",
    children: [node("Tooltip")],
    props: {dataKey: "size", aspectRatio: 4 / 3, colorPanel: ["#8889DD", "#9597E4", "#8DC77B", "#A5D297", "#E2CF45", "#F8C12D"], ...off},
    interactions: [[{hover: [50, 50]}]],
  },
  {
    name: "NestedTreemap", ...treemap, data: flare.slice(0, 5),
    source: "www/src/docs/exampleComponents/TreeMap/NestedTreemap.tsx (a fixed 500 x 375)",
    children: [node("Tooltip")],
    props: {dataKey: "size", nameKey: "name", aspectRatio: 4 / 3, type: "nest", ...off},
    interactions: [[{click: [80, 60]}]],
  },
  {
    name: "SimpleSankey", kind: "Sankey", width: 800, height: 400, margin: {bottom: 30}, data: visits,
    source: "www/src/docs/exampleComponents/Sankey/SankeyCustomNodeExample.tsx (a fixed 800 x 400; the custom node replaced by Recharts' default node)",
    children: [node("Tooltip")],
    props: {nodePadding: 50, link: {stroke: "#77c878"}},
  },
  {
    name: "BudgetSankey", kind: "Sankey", width: 700, height: 400, margin: null, data: budget,
    source: "A three-level flow written for Pictor (no Recharts example has one): wider nodes, curvature 0.6, no sorting",
    children: [node("Tooltip")],
    props: {nodeWidth: 15, nodePadding: 20, linkCurvature: 0.6, sort: false, node: {fill: "#8884d8"}},
  },
];
