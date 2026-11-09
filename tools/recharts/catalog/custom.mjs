import {createElement as h} from "react";
import {Layer, Rectangle, Sector} from "recharts";
import {node, off, pages} from "./common.mjs";
import {schoolA} from "./scatter.mjs";
import {ages, subjects} from "./polar.mjs";
import {funnel} from "./hierarchy.mjs";

const site = "recharts.org www/src/docs/exampleComponents";

// d3's schemeCategory10, which CustomShapeBarChart takes through scaleOrdinal(schemeCategory10).range().
const category10 = ["#1f77b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd", "#8c564b", "#e377c2", "#7f7f7f", "#bcbd22", "#17becf"];

const trianglePath = (x, y, width, height) => `M${x},${y + height}C${x + width / 3},${y + height} ${x + width / 2},${y + height / 3}
  ${x + width / 2}, ${y}
  C${x + width / 2},${y + height / 3} ${x + (2 * width) / 3},${y + height} ${x + width}, ${y + height}
  Z`;

function TriangleBar(props) {
  const {fill, x, y, width, height} = props;
  return h("path", {d: trianglePath(x, y, width, height), stroke: "none", fill});
}

const happy = "M512 1009.984c-274.912 0-497.76-222.848-497.76-497.76s222.848-497.76 497.76-497.76c274.912 0 497.76 222.848 497.76 497.76s-222.848 497.76-497.76 497.76zM340.768 295.936c-39.488 0-71.52 32.8-71.52 73.248s32.032 73.248 71.52 73.248c39.488 0 71.52-32.8 71.52-73.248s-32.032-73.248-71.52-73.248zM686.176 296.704c-39.488 0-71.52 32.8-71.52 73.248s32.032 73.248 71.52 73.248c39.488 0 71.52-32.8 71.52-73.248s-32.032-73.248-71.52-73.248zM772.928 555.392c-18.752-8.864-40.928-0.576-49.632 18.528-40.224 88.576-120.256 143.552-208.832 143.552-85.952 0-164.864-52.64-205.952-137.376-9.184-18.912-31.648-26.592-50.08-17.28-18.464 9.408-21.216 21.472-15.936 32.64 52.8 111.424 155.232 186.784 269.76 186.784 117.984 0 217.12-70.944 269.76-186.784 8.672-19.136 9.568-31.2-9.12-40.096z";
const sad = "M517.12 53.248q95.232 0 179.2 36.352t145.92 98.304 98.304 145.92 36.352 179.2-36.352 179.2-98.304 145.92-145.92 98.304-179.2 36.352-179.2-36.352-145.92-98.304-98.304-145.92-36.352-179.2 36.352-179.2 98.304-145.92 145.92-98.304 179.2-36.352zM663.552 261.12q-15.36 0-28.16 6.656t-23.04 18.432-15.872 27.648-5.632 33.28q0 35.84 21.504 61.44t51.2 25.6 51.2-25.6 21.504-61.44q0-17.408-5.632-33.28t-15.872-27.648-23.04-18.432-28.16-6.656zM373.76 261.12q-29.696 0-50.688 25.088t-20.992 60.928 20.992 61.44 50.688 25.6 50.176-25.6 20.48-61.44-20.48-60.928-50.176-25.088zM520.192 602.112q-51.2 0-97.28 9.728t-82.944 27.648-62.464 41.472-35.84 51.2q-1.024 1.024-1.024 2.048-1.024 3.072-1.024 8.704t2.56 11.776 7.168 11.264 12.8 6.144q25.6-27.648 62.464-50.176 31.744-19.456 79.36-35.328t114.176-15.872q67.584 0 116.736 15.872t81.92 35.328q37.888 22.528 63.488 50.176 17.408-5.12 19.968-18.944t0.512-18.944-3.072-7.168-1.024-3.072q-26.624-55.296-100.352-88.576t-176.128-33.28z";

function CustomizedDot(props) {
  const {cx, cy, value} = props;
  if (value > 2500) {
    return h("svg", {x: cx - 10, y: cy - 10, width: 20, height: 20, fill: "red", viewBox: "0 0 1024 1024"}, h("path", {d: happy}));
  }

  return h("svg", {x: cx - 10, y: cy - 10, width: 20, height: 20, fill: "green", viewBox: "0 0 1024 1024"}, h("path", {d: sad}));
}

function CustomizedLabel(props) {
  const {x, y, stroke, value} = props;
  return h("text", {x, y, dy: -4, fill: stroke, fontSize: 10, textAnchor: "middle"}, value);
}

function CustomizedAxisTick(props) {
  const {x, y, payload} = props;
  return h("g", {transform: `translate(${x},${y})`},
    h("text", {x: 0, y: 0, dy: 16, textAnchor: "end", fill: "#666", transform: "rotate(-35)"}, payload.value));
}

function renderActiveShape(props) {
  const RADIAN = Math.PI / 180;
  const {cx, cy, midAngle, innerRadius, outerRadius, startAngle, endAngle, fill, payload, percent, value} = props;
  const sin = Math.sin(-RADIAN * midAngle);
  const cos = Math.cos(-RADIAN * midAngle);
  const sx = cx + (outerRadius + 10) * cos;
  const sy = cy + (outerRadius + 10) * sin;
  const mx = cx + (outerRadius + 30) * cos;
  const my = cy + (outerRadius + 30) * sin;
  const ex = mx + (cos >= 0 ? 1 : -1) * 22;
  const ey = my;
  const textAnchor = cos >= 0 ? "start" : "end";
  return h("g", null,
    h("text", {x: cx, y: cy, dy: 8, textAnchor: "middle", fill}, payload.name),
    h(Sector, {cx, cy, innerRadius, outerRadius, startAngle, endAngle, fill}),
    h(Sector, {cx, cy, startAngle, endAngle, innerRadius: outerRadius + 6, outerRadius: outerRadius + 10, fill}),
    h("path", {d: `M${sx},${sy}L${mx},${my}L${ex},${ey}`, stroke: fill, fill: "none"}),
    h("circle", {cx: ex, cy: ey, r: 2, fill, stroke: "none"}),
    h("text", {x: ex + (cos >= 0 ? 1 : -1) * 12, y: ey, textAnchor, fill: "#333"}, `PV ${value}`),
    h("text", {x: ex + (cos >= 0 ? 1 : -1) * 12, y: ey, dy: 18, textAnchor, fill: "#999"}, `(Rate ${(percent * 100).toFixed(2)}%)`));
}

const treemapColors = ["#8889DD", "#9597E4", "#8DC77B", "#A5D297", "#E2CF45", "#F8C12D"];

function CustomizedContent(props) {
  const {root, depth, x, y, width, height, index, colors, name} = props;
  return h("g", null,
    h("rect", {x, y, width, height, style: {fill: depth < 2 ? colors[Math.floor((index / root.children.length) * 6)] : "#ffffff00", stroke: "#fff", strokeWidth: 2 / (depth + 1e-10), strokeOpacity: 1 / (depth + 1e-10)}}),
    depth === 1 ? h("text", {x: x + width / 2, y: y + height / 2 + 7, textAnchor: "middle", fill: "#fff", fontSize: 14}, name) : null,
    depth === 1 ? h("text", {x: x + 4, y: y + 18, fill: "#fff", fontSize: 16, fillOpacity: 0.9}, index + 1) : null);
}

function DemoSankeyNode({x, y, width, height, index, payload, containerWidth}) {
  const isOut = x + width + 6 > containerWidth;
  return h(Layer, {key: `CustomNode${index}`},
    h(Rectangle, {x, y, width, height, fill: "#5192ca", fillOpacity: "1"}),
    h("text", {textAnchor: isOut ? "end" : "start", x: isOut ? x - 6 : x + width + 6, y: y + height / 2, fontSize: "14", stroke: "#333"}, payload.name),
    h("text", {textAnchor: isOut ? "end" : "start", x: isOut ? x - 6 : x + width + 6, y: y + height / 2 + 13, fontSize: "12", stroke: "#333", strokeOpacity: "0.5"}, `${payload.value}k`));
}

function CustomTooltip({active, payload, label}) {
  if (active && payload && payload.length) {
    return h("div", {className: "custom-tooltip"},
      h("p", {className: "label"}, `${label} : ${payload[0].value}`),
      h("p", {className: "intro"}, payload[0].payload.intro),
      h("p", {className: "desc"}, "Anything you want can be displayed here."));
  }

  return null;
}

function CustomTraveller({x, y, width, height, stroke}) {
  return h("g", null,
    h("rect", {x, y, width, height, fill: stroke, rx: 3}),
    h("line", {x1: x + width / 2, y1: y + 8, x2: x + width / 2, y2: y + height - 8, stroke: "#fff", strokeWidth: 2}));
}

function DiamondShape({cx, cy, fill}) {
  return h("path", {d: `M${cx},${cy - 8}L${cx + 8},${cy}L${cx},${cy + 8}L${cx - 8},${cy}Z`, fill});
}

function ActiveDiamond({cx, cy, fill}) {
  return h("path", {d: `M${cx},${cy - 12}L${cx + 12},${cy}L${cx},${cy + 12}L${cx - 12},${cy}Z`, fill, stroke: "#333", strokeWidth: 2});
}

function RoundedTrapezoid({x, y, upperWidth, height, fill}) {
  return h("rect", {x, y, width: upperWidth, height, fill, rx: 4});
}

function RadarPolygon({points, fill}) {
  return h("polygon", {points: points.map((point) => `${point.x},${point.y}`).join(" "), fill, fillOpacity: 0.3, stroke: fill});
}

function RoundedSector({cx, cy, innerRadius, outerRadius, startAngle, endAngle, fill}) {
  return h(Sector, {cx, cy, innerRadius: innerRadius + 1, outerRadius: outerRadius - 1, startAngle, endAngle, fill, cornerRadius: 4});
}

function BandLink({sourceX, sourceY, sourceControlX, targetX, targetY, targetControlX, linkWidth}) {
  const top = (y) => y - linkWidth / 2;
  const bottom = (y) => y + linkWidth / 2;
  return h("path", {
    d: `M${sourceX},${top(sourceY)}C${sourceControlX},${top(sourceY)} ${targetControlX},${top(targetY)} ${targetX},${top(targetY)}L${targetX},${bottom(targetY)}C${targetControlX},${bottom(targetY)} ${sourceControlX},${bottom(sourceY)} ${sourceX},${bottom(sourceY)}Z`,
    fill: "#77c878", fillOpacity: 0.3, stroke: "none",
  });
}

function ArrowLine({x1, y1, x2, y2, stroke}) {
  return h("g", null, h("line", {x1, y1, x2, y2, stroke, strokeWidth: 2}), h("circle", {cx: x2, cy: y2, r: 4, fill: stroke}));
}

function HatchedArea({x, y, width, height}) {
  return h("rect", {x, y, width, height, fill: "#ffc658", fillOpacity: 0.3, stroke: "#ffc658", strokeDasharray: "4 2"});
}

function StarDot({cx, cy, fill}) {
  return h("path", {d: `M${cx},${cy - 10}L${cx + 3},${cy - 3}L${cx + 10},${cy}L${cx + 3},${cy + 3}L${cx},${cy + 10}L${cx - 3},${cy + 3}L${cx - 10},${cy}L${cx - 3},${cy - 3}Z`, fill});
}

function PlotFrame({offset, width, height}) {
  return h("g", null,
    h("rect", {x: offset.left, y: offset.top, width: offset.width, height: offset.height, fill: "none", stroke: "#8884d8", strokeDasharray: "2 2"}),
    h("text", {x: width / 2, y: height / 2, textAnchor: "middle", fill: "#ccc", fontSize: 24}, "Draft"));
}

function renderLegend(props) {
  const {payload} = props;
  return h("ul", null, payload.map((entry, index) => h("li", {key: `item-${index}`}, entry.value)));
}

function OutlinedTriangleBar(props) {
  const {fill, x, y, width, height} = props;
  return h("path", {d: trianglePath(x, y, width, height), stroke: "#333", strokeWidth: 2, fill});
}

function OutlinedTrapezoid({x, y, upperWidth, height, fill}) {
  return h("rect", {x, y, width: upperWidth, height, fill, rx: 4, stroke: "#333", strokeWidth: 2});
}

function GrownSector({cx, cy, innerRadius, outerRadius, startAngle, endAngle, fill}) {
  return h(Sector, {cx, cy, innerRadius: innerRadius - 2, outerRadius: outerRadius + 2, startAngle, endAngle, fill, stroke: "#333"});
}

function renderCustomizedLabel({x, y, width, value}) {
  const radius = 10;
  return h("g", null,
    h("circle", {cx: x + width / 2, cy: y - radius, r: radius, fill: "#8884d8"}),
    h("text", {x: x + width / 2, y: y - radius, dy: 4, fill: "#fff", fontSize: 10, textAnchor: "middle"}, value / 1000));
}

function TargetLabel({viewBox, value}) {
  const {x, y, width} = viewBox;
  return h("g", null,
    h("rect", {x: x + width - 50, y: y - 20, width: 50, height: 18, rx: 3, fill: "red"}),
    h("text", {x: x + width - 25, y: y - 7, fill: "#fff", fontSize: 12, textAnchor: "middle"}, value));
}

function AxisTitle({viewBox, value}) {
  return h("text", {x: viewBox.x + 4, y: viewBox.y - 6, fill: "#666", fontSize: 11}, value);
}

const nestIndexContent = (item, i) => h("span", {style: {fontWeight: "bold"}}, `${i}: ${item.name ?? "root"}`);

/** The custom components the examples name with {$content}, as Recharts receives them: an element or a render function. */
export const contents = {
  TriangleBar: h(TriangleBar),
  CustomizedDot: h(CustomizedDot),
  CustomizedLabel: h(CustomizedLabel),
  CustomizedAxisTick: h(CustomizedAxisTick),
  renderActiveShape,
  CustomizedContent: h(CustomizedContent, {colors: treemapColors}),
  DemoSankeyNode: h(DemoSankeyNode, {containerWidth: 960}),
  CustomTooltip: h(CustomTooltip),
  CustomTraveller: h(CustomTraveller),
  DiamondShape: h(DiamondShape),
  ActiveDiamond: h(ActiveDiamond),
  RoundedTrapezoid: h(RoundedTrapezoid),
  RadarPolygon: h(RadarPolygon),
  RoundedSector: h(RoundedSector),
  BandLink: h(BandLink),
  ArrowLine: h(ArrowLine),
  HatchedArea: h(HatchedArea),
  StarDot: h(StarDot),
  PlotFrame: h(PlotFrame),
  renderLegend,
  OutlinedTriangleBar: h(OutlinedTriangleBar),
  nestIndexContent,
  OutlinedTrapezoid: h(OutlinedTrapezoid),
  GrownSector: h(GrownSector),
  renderCustomizedLabel,
  TargetLabel: h(TargetLabel),
  AxisTitle: h(AxisTitle),
};

const content = (name) => ({$content: name});

const groups = [
  {name: "Group A", value: 400},
  {name: "Group B", value: 300},
  {name: "Group C", value: 300},
  {name: "Group D", value: 200},
];

const flare = [
  {name: "axis", children: [{name: "Axes", size: 1302}, {name: "Axis", size: 24593}, {name: "AxisGridLine", size: 652}, {name: "AxisLabel", size: 636}, {name: "CartesianAxes", size: 6703}]},
  {name: "controls", children: [{name: "AnchorControl", size: 2138}, {name: "ClickControl", size: 3824}, {name: "Control", size: 1353}, {name: "ControlList", size: 4665}, {name: "DragControl", size: 2649}, {name: "ExpandControl", size: 2832}, {name: "HoverControl", size: 4896}, {name: "IControl", size: 763}, {name: "PanZoomControl", size: 5222}, {name: "SelectionControl", size: 7862}, {name: "TooltipControl", size: 8435}]},
  {name: "data", children: [{name: "Data", size: 20544}, {name: "DataList", size: 19788}, {name: "DataSprite", size: 10349}, {name: "EdgeSprite", size: 3301}, {name: "NodeSprite", size: 19382}, {name: "render", children: [{name: "ArrowType", size: 698}, {name: "EdgeRenderer", size: 5569}, {name: "IRenderer", size: 353}, {name: "ShapeRenderer", size: 2247}]}, {name: "ScaleBinding", size: 11275}, {name: "Tree", size: 7147}, {name: "TreeBuilder", size: 9930}]},
  {name: "events", children: [{name: "DataEvent", size: 7313}, {name: "SelectionEvent", size: 6880}, {name: "TooltipEvent", size: 3701}, {name: "VisualizationEvent", size: 2117}]},
  {name: "legend", children: [{name: "Legend", size: 20859}, {name: "LegendItem", size: 4614}, {name: "LegendRange", size: 10530}]},
  {name: "operator", children: [{name: "distortion", children: [{name: "BifocalDistortion", size: 4461}, {name: "Distortion", size: 6314}, {name: "FisheyeDistortion", size: 3444}]}, {name: "encoder", children: [{name: "ColorEncoder", size: 3179}, {name: "Encoder", size: 4060}, {name: "PropertyEncoder", size: 4138}, {name: "ShapeEncoder", size: 1690}, {name: "SizeEncoder", size: 1830}]}, {name: "filter", children: [{name: "FisheyeTreeFilter", size: 5219}, {name: "GraphDistanceFilter", size: 3165}, {name: "VisibilityFilter", size: 3509}]}, {name: "IOperator", size: 1286}, {name: "label", children: [{name: "Labeler", size: 9956}, {name: "RadialLabeler", size: 3899}, {name: "StackedAreaLabeler", size: 3202}]}, {name: "layout", children: [{name: "AxisLayout", size: 6725}, {name: "BundledEdgeRouter", size: 3727}, {name: "CircleLayout", size: 9317}, {name: "CirclePackingLayout", size: 12003}, {name: "DendrogramLayout", size: 4853}, {name: "ForceDirectedLayout", size: 8411}, {name: "IcicleTreeLayout", size: 4864}, {name: "IndentedTreeLayout", size: 3174}, {name: "Layout", size: 7881}, {name: "NodeLinkTreeLayout", size: 12870}, {name: "PieLayout", size: 2728}, {name: "RadialTreeLayout", size: 12348}, {name: "RandomLayout", size: 870}, {name: "StackedAreaLayout", size: 9121}, {name: "TreeMapLayout", size: 9191}]}, {name: "Operator", size: 2490}, {name: "OperatorList", size: 5248}, {name: "OperatorSequence", size: 4190}, {name: "OperatorSwitch", size: 2581}, {name: "SortOperator", size: 2023}]},
];

const sankey = {
  nodes: [{name: "Visit"}, {name: "Direct-Favourite"}, {name: "Page-Click"}, {name: "Detail-Favourite"}, {name: "Lost"}],
  links: [{source: 0, target: 1, value: 3728.3}, {source: 0, target: 2, value: 354170}, {source: 2, target: 3, value: 291741}, {source: 2, target: 4, value: 62429}],
};

const intros = {
  "Page A": "Page A is about men's clothing",
  "Page B": "Page B is about women's dress",
  "Page C": "Page C is about women's bag",
  "Page D": "Page D is about household goods",
  "Page E": "Page E is about food",
  "Page F": "Page F is about baby food",
};

export const examples = [
  {
    name: "CustomShapeBarChart", kind: "BarChart", width: 500, height: 300, margin: {top: 20, right: 30, left: 20, bottom: 5}, data: pages,
    source: `${site}/BarChart/CustomShapeBarChart.tsx`,
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name"}), node("YAxis"),
      node("Bar", {dataKey: "uv", fill: "#8884d8", shape: content("TriangleBar"), label: {position: "top"}, ...off},
        pages.map((_, index) => node("Cell", {fill: category10[index % 20]})))],
  },
  {
    name: "CustomizedDotLineChart", kind: "LineChart", width: 500, height: 300, margin: {top: 5, right: 30, left: 20, bottom: 5}, data: pages,
    source: `${site}/LineChart/CustomizedDotLineChart.tsx`,
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name"}), node("YAxis"), node("Tooltip"), node("Legend"),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", dot: content("CustomizedDot"), ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off})],
  },
  {
    name: "CustomizedLabelTickLineChart", kind: "LineChart", width: 500, height: 300, margin: {top: 20, right: 30, left: 20, bottom: 10}, data: pages,
    source: `${site}/LineChart/CustomizedLabelLineChart.tsx (its own CustomizedLabel and CustomizedAxisTick)`,
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name", height: 60, tick: content("CustomizedAxisTick")}), node("YAxis"), node("Tooltip"), node("Legend"),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", label: content("CustomizedLabel"), ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off})],
  },
  {
    name: "CustomActiveShapePieChart", kind: "PieChart", width: 400, height: 400, margin: null, data: [],
    source: `${site}/PieChart/CustomActiveShapePieChart.tsx (its activeIndex state at its initial 0)`,
    children: [node("Pie", {activeIndex: 0, activeShape: content("renderActiveShape"), data: groups, cx: "50%", cy: "50%", innerRadius: 60, outerRadius: 80, fill: "#8884d8", dataKey: "value", ...off})],
  },
  {
    name: "CustomContentTreemap", kind: "Treemap", width: 400, height: 200, margin: null, data: flare,
    source: `${site}/Treemap/CustomContentTreemap.tsx (400 by 200 instead of a responsive container)`,
    props: {dataKey: "size", stroke: "#fff", fill: "#8884d8", content: content("CustomizedContent"), ...off},
    children: [],
  },
  {
    name: "CustomNodeSankey", kind: "Sankey", width: 960, height: 500, margin: {top: 20, bottom: 20, left: 20, right: 150}, data: sankey,
    source: "recharts demo/component/DemoSankeyNode.tsx and Sankey's docs example (a custom node with its name and value beside it)",
    props: {nodePadding: 50, link: {stroke: "#77c878"}, node: content("DemoSankeyNode")},
    children: [],
  },
  {
    name: "CustomContentOfTooltip", kind: "BarChart", width: 500, height: 300, margin: {top: 5, right: 30, left: 20, bottom: 5},
    data: pages.slice(0, 6).map((page) => ({...page, intro: intros[page.name]})),
    source: `${site}/Tooltip/CustomContentOfTooltip.tsx (the page's intro read from its item rather than a switch)`,
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name"}), node("YAxis"),
      node("Tooltip", {content: content("CustomTooltip")}), node("Legend"),
      node("Bar", {dataKey: "pv", barSize: 20, fill: "#8884d8", ...off})],
    interactions: [[{hover: [150, 150]}], [{hover: [437, 150]}]],
  },
  {
    name: "CustomTravellerBrush", kind: "BarChart", width: 500, height: 300, margin: {top: 5, right: 30, left: 20, bottom: 5}, data: pages,
    source: "Brush's traveller prop (an element receiving the traveller's x, y, width, height and stroke)",
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name"}), node("YAxis"),
      node("Bar", {dataKey: "pv", fill: "#8884d8", ...off}),
      node("Brush", {dataKey: "name", height: 30, stroke: "#8884d8", travellerWidth: 10, traveller: content("CustomTraveller")})],
  },
  {
    name: "CustomShapeScatterChart", kind: "ScatterChart", width: 500, height: 400, margin: {top: 20, right: 20, bottom: 20, left: 20}, data: null,
    source: "SimpleScatterChart with a scatter's shape and activeShape elements (a diamond at each point, a bigger outlined one on the hovered point)",
    children: [node("CartesianGrid"), node("XAxis", {type: "number", dataKey: "x", name: "stature", unit: "cm"}), node("YAxis", {type: "number", dataKey: "y", name: "weight", unit: "kg"}),
      node("Tooltip", {cursor: {strokeDasharray: "3 3"}}),
      node("Scatter", {name: "A school", data: schoolA, fill: "#8884d8", shape: content("DiamondShape"), activeShape: content("ActiveDiamond"), ...off})],
    interactions: [[{hover: [302, 185]}]],
  },
  {
    name: "CustomShapeFunnelChart", kind: "FunnelChart", width: 700, height: 432, margin: {right: 30}, data: null,
    source: "FunnelChartExample with a funnel's shape element (each item's upper edge as a rounded rectangle)",
    children: [node("Funnel", {dataKey: "value", data: funnel, shape: content("RoundedTrapezoid"), ...off},
      [node("LabelList", {position: "right", fill: "#000", stroke: "none", dataKey: "name"})])],
  },
  {
    name: "CustomShapeRadarChart", kind: "RadarChart", width: 500, height: 400, margin: null, data: subjects,
    source: "SimpleRadarChart with a radar's shape element (a polygon element filled at 0.3 and stroked)",
    chartProps: {cx: "50%", cy: "50%", outerRadius: "80%"},
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "subject"}), node("PolarRadiusAxis"),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", shape: content("RadarPolygon"), ...off})],
  },
  {
    name: "CustomShapeRadialBarChart", kind: "RadialBarChart", width: 500, height: 300, margin: null, data: ages,
    source: "SimpleRadialBarChart with a radial bar's shape element (each bar a sector 1 px thinner on both sides with rounded corners)",
    chartProps: {cx: "50%", cy: "50%", innerRadius: "10%", outerRadius: "80%", barSize: 10},
    children: [node("RadialBar", {minAngle: 15, background: true, clockWise: true, dataKey: "uv", shape: content("RoundedSector"), ...off})],
  },
  {
    name: "CustomLinkSankey", kind: "Sankey", width: 960, height: 500, margin: {top: 20, bottom: 20, left: 20, right: 150}, data: sankey,
    source: "the Sankey of CustomNodeSankey with a link element (each link a filled band between its two curves, Recharts' link props)",
    props: {nodePadding: 50, link: content("BandLink")},
    children: [],
  },
  {
    name: "CustomReferenceShapes", kind: "LineChart", width: 500, height: 300, margin: {top: 20, right: 30, left: 20, bottom: 5}, data: pages,
    source: "SimpleLineChart's data with a reference line, area and dot, each with a shape element (a line ending in a dot, a dashed translucent area, a star)",
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name"}), node("YAxis"),
      node("ReferenceArea", {x1: "Page B", x2: "Page D", shape: content("HatchedArea")}),
      node("ReferenceLine", {y: 4000, stroke: "red", shape: content("ArrowLine")}),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off}),
      node("ReferenceDot", {x: "Page C", y: 9800, r: 10, fill: "#8884d8", stroke: "none", shape: content("StarDot")})],
  },
  {
    name: "CustomizedChart", kind: "LineChart", width: 500, height: 300, margin: {top: 20, right: 30, left: 20, bottom: 5}, data: pages,
    source: "Recharts' Customized with a component drawn from the chart's offset and size (a dashed frame around the plot and a word at the chart's center)",
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name"}), node("YAxis"),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off}),
      node("Customized", {component: content("PlotFrame")})],
  },
  {
    name: "CustomContentLegend", kind: "LineChart", width: 500, height: 300, margin: {top: 5, right: 30, left: 20, bottom: 5}, data: pages,
    source: "Recharts' Legend API example (recharts.org/en-US/api/Legend): renderLegend, a list of the entries' values, on SimpleLineChart",
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name"}), node("YAxis"), node("Legend", {content: content("renderLegend")}),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off})],
  },
  {
    name: "CustomActiveBarChart", kind: "BarChart", width: 500, height: 300, margin: {top: 20, right: 30, left: 20, bottom: 5}, data: pages,
    source: "CustomShapeBarChart's triangles with a tooltip and an activeBar element (the hovered triangle outlined)",
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name"}), node("YAxis"), node("Tooltip"),
      node("Bar", {dataKey: "uv", fill: "#8884d8", shape: content("TriangleBar"), activeBar: content("OutlinedTriangleBar"), ...off})],
    interactions: [[{hover: [163, 200]}]],
  },
  {
    name: "CustomNestIndexTreemap", kind: "Treemap", width: 500, height: 375, margin: null, data: flare.slice(0, 5),
    source: "NestedTreemap with a nestIndexContent function (each trail box's index and name in bold)",
    children: [node("Tooltip")],
    props: {dataKey: "size", nameKey: "name", aspectRatio: 4 / 3, type: "nest", nestIndexContent: content("nestIndexContent"), ...off},
  },
  {
    name: "CustomActiveFunnelChart", kind: "FunnelChart", width: 700, height: 432, margin: {right: 30}, data: null,
    source: "Funnel's shape and activeShape props (a rounded rectangle per item, outlined while hovered)",
    children: [node("Tooltip"), node("Funnel", {dataKey: "value", data: funnel, shape: content("RoundedTrapezoid"), activeShape: content("OutlinedTrapezoid"), ...off})],
    interactions: [[{hover: [330, 130]}]],
  },
  {
    name: "CustomActiveRadialBarChart", kind: "RadialBarChart", width: 500, height: 300, margin: null, data: ages,
    source: "RadialBar's activeShape prop (the hovered bar grown by 2 px on both sides and outlined)",
    chartProps: {cx: "50%", cy: "50%", innerRadius: "10%", outerRadius: "80%", barSize: 10},
    children: [node("Tooltip"), node("RadialBar", {minAngle: 15, background: true, clockWise: true, dataKey: "uv", activeShape: content("GrownSector"), ...off})],
    interactions: [[{hover: [330, 150]}]],
  },
  {
    name: "CustomizedBarLabel", kind: "BarChart", width: 500, height: 300, margin: {top: 25, right: 30, left: 20, bottom: 5}, data: pages,
    source: "BarChartWithMinHeight's renderCustomizedLabel, given to the bar's label prop (a circle above each bar with the value in thousands)",
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name"}), node("YAxis"),
      node("Bar", {dataKey: "uv", fill: "#82ca9d", label: content("renderCustomizedLabel"), ...off})],
  },
  {
    name: "CustomElementLabels", kind: "LineChart", width: 500, height: 300, margin: {top: 20, right: 30, left: 20, bottom: 5}, data: pages,
    source: "Label's content prop on a reference line's and an axis' Label child (a badge at the line's right end, a title above the axis)",
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name"}),
      node("YAxis", {}, [node("Label", {value: "pv", content: content("AxisTitle")})]),
      node("ReferenceLine", {y: 4000, stroke: "red"}, [node("Label", {value: "Target", content: content("TargetLabel")})]),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off})],
  },
];
