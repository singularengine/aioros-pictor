import {createElement as h} from "react";
import {DefaultLegendContent} from "recharts";
import {node, off, pages} from "./common.mjs";
import {schoolA} from "./scatter.mjs";
import {ages, subjects} from "./polar.mjs";
import {funnel, flare, visits} from "./hierarchy.mjs";

const content = (name) => ({$content: name});
const RADIAN = Math.PI / 180;

/** Recharts' polarToCartesian. */
const polar = (cx, cy, radius, angle) => [cx + Math.cos(-RADIAN * angle) * radius, cy + Math.sin(-RADIAN * angle) * radius];

/** A value at the middle of its sector (the ring's middle radius, the sector's middle angle), as a pie's or a radial bar's label list content. */
function SectorValueLabel({viewBox, value}) {
  const {cx, cy, innerRadius, outerRadius, startAngle, endAngle} = viewBox;
  const [x, y] = polar(cx, cy, (innerRadius + outerRadius) / 2, (startAngle + endAngle) / 2);
  return h("text", {x, y, dy: 4, fill: "#fff", fontSize: 12, textAnchor: "middle"}, value);
}

/** A small dark value at the middle of a radial bar. */
function BarValueLabel({viewBox, value}) {
  const {cx, cy, innerRadius, outerRadius, startAngle, endAngle} = viewBox;
  const [x, y] = polar(cx, cy, (innerRadius + outerRadius) / 2, (startAngle + endAngle) / 2);
  return h("text", {x, y, dy: 3, fill: "#333", fontSize: 8, textAnchor: "middle"}, value);
}

/** A radar point's value in a dot above it. */
function RadarPointLabel({x, y, value}) {
  return h("g", null,
    h("circle", {cx: x, cy: y - 12, r: 10, fill: "#8884d8"}),
    h("text", {x, y: y - 12, dy: 4, fill: "#fff", fontSize: 10, textAnchor: "middle"}, value));
}

/** A funnel item's value in a white badge at its label box's center. */
function FunnelBadge({viewBox, value}) {
  const {x, y, width, height} = viewBox;
  return h("g", null,
    h("rect", {x: x + width / 2 - 30, y: y + height / 2 - 10, width: 60, height: 20, rx: 10, fill: "#fff", fillOpacity: 0.8}),
    h("text", {x: x + width / 2, y: y + height / 2, dy: 4, fill: "#333", fontSize: 12, textAnchor: "middle"}, value));
}

/** Recharts' docs' renderPolarAngleAxis: each angle tick's name pushed out from the center by a tenth of its distance. */
function AngleTick({x, y, cx, cy, payload}) {
  return h("text", {x: x + (x - cx) / 10, y: y + (y - cy) / 10, dy: 4, textAnchor: "middle", fill: "#8884d8", fontSize: 12}, payload.value);
}

/** A radius tick's value in a group at its point, 4 px past it. */
function RadiusTick({x, y, payload}) {
  return h("g", {transform: `translate(${x},${y})`}, h("text", {x: 0, y: 0, dx: 4, fill: "#999", fontSize: 10}, payload.value));
}

/** A radius axis' title 8 px above the middle of the axis, from the label's polar viewBox. */
function RadiusTitle({viewBox, value}) {
  const {cx, cy, innerRadius, outerRadius, startAngle} = viewBox;
  const [x, y] = polar(cx, cy, (innerRadius + outerRadius) / 2, startAngle);
  return h("text", {x, y, dy: -8, textAnchor: "middle", fill: "#666", fontSize: 11}, value);
}

/** A line's active dot as a ring around a dot of its color. */
function ActiveRing({cx, cy, fill}) {
  return h("g", null, h("circle", {cx, cy, r: 10, fill: "none", stroke: fill, strokeWidth: 2}), h("circle", {cx, cy, r: 4, fill}));
}

/** An area's active dot as a square of its color outlined in white. */
function ActiveSquare({cx, cy, fill}) {
  return h("rect", {x: cx - 6, y: cy - 6, width: 12, height: 12, fill, stroke: "#fff", strokeWidth: 2});
}

/** An item tooltip's content: its first entry's name and value in a paragraph. */
function ItemTooltip({active, payload}) {
  if (active && payload && payload.length) {
    return h("div", {className: "custom-tooltip"}, h("p", {className: "label"}, `${payload[0].name} : ${payload[0].value}`));
  }

  return null;
}

/** An axis tooltip's content: the category's label, then its first entry's name and value. */
function AxisTooltip({active, payload, label}) {
  if (active && payload && payload.length) {
    return h("div", {className: "custom-tooltip"}, h("p", {className: "label"}, label), h("p", {className: "value"}, `${payload[0].name} : ${payload[0].value}`));
  }

  return null;
}

/** A legend content that is not one item per entry: a bordered box holding a column with a bold title above Recharts' default list. */
function FramedLegend(props) {
  return h("div", {className: "legend-frame", style: {display: "flex", flexDirection: "column", padding: "4px 0", border: "1px solid #ccc"}},
    h("div", {style: {display: "flex", flexDirection: "column"}},
      h("p", {className: "legend-title", style: {margin: 0, whiteSpace: "nowrap", lineHeight: "normal", fontWeight: 700}}, `${props.payload.length} series`),
      h(DefaultLegendContent, props)));
}

/** An axis' onClick: what Recharts gives it (the tick's payload and index) kept on the chart's wrapper for the C# side. */
function RecordTick(payload, index) {
  document.querySelector("#host .recharts-wrapper").setAttribute("data-tick", JSON.stringify({value: payload.value, index}));
}

/** The custom components of these examples, as Recharts receives them. */
export const contents = {
  SectorValueLabel: h(SectorValueLabel),
  BarValueLabel: h(BarValueLabel),
  RadarPointLabel: h(RadarPointLabel),
  FunnelBadge: h(FunnelBadge),
  AngleTick: h(AngleTick),
  RadiusTick: h(RadiusTick),
  RadiusTitle: h(RadiusTitle),
  ActiveRing: h(ActiveRing),
  ActiveSquare: h(ActiveSquare),
  ItemTooltip: h(ItemTooltip),
  AxisTooltip: h(AxisTooltip),
  FramedLegend,
  RecordTick,
};

const groups = [
  {name: "Group A", value: 400},
  {name: "Group B", value: 300},
  {name: "Group C", value: 300},
  {name: "Group D", value: 200},
];

export const examples = [
  {
    name: "TickClickChart", kind: "LineChart", width: 500, height: 300, margin: {top: 5, right: 30, left: 20, bottom: 5}, data: pages,
    source: "SimpleLineChart whose axes take onClick: a click on a tick's label gives the handler the tick's payload and index",
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name", onClick: content("RecordTick")}), node("YAxis", {onClick: content("RecordTick")}),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off})],
    interactions: [[{click: [210, 285]}], [{click: [50, 140]}]],
  },
  {
    name: "CustomFramedLegend", kind: "LineChart", width: 500, height: 300, margin: {top: 5, right: 30, left: 20, bottom: 5}, data: pages,
    source: "a Legend content function that is not one item per entry: a bordered column holding a title with the entries' count above DefaultLegendContent",
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name"}), node("YAxis"), node("Legend", {content: content("FramedLegend")}),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off})],
  },
  {
    name: "PieTooltipContent", kind: "PieChart", width: 400, height: 400, margin: null, data: [],
    source: "a pie's item tooltip with a content element (the sector's name and value)",
    children: [node("Tooltip", {content: content("ItemTooltip")}), node("Pie", {data: groups, dataKey: "value", cx: "50%", cy: "50%", outerRadius: 120, fill: "#8884d8", ...off})],
    interactions: [[{hover: [230, 130]}]],
  },
  {
    name: "RadarTooltipContent", kind: "RadarChart", width: 500, height: 400, margin: null, data: subjects,
    source: "a radar's axis tooltip with a content element (the category, then the first entry's name and value)",
    chartProps: {cx: "50%", cy: "50%", outerRadius: "80%"},
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "subject"}), node("PolarRadiusAxis"), node("Tooltip", {content: content("AxisTooltip")}),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, ...off})],
    interactions: [[{hover: [250, 120]}]],
  },
  {
    name: "RadialBarTooltipContent", kind: "RadialBarChart", width: 500, height: 300, margin: null, data: ages,
    source: "radial bars' axis tooltip with a content element (the category, then the first entry's name and value)",
    chartProps: {cx: "50%", cy: "50%", innerRadius: "10%", outerRadius: "80%", barSize: 10},
    children: [node("Tooltip", {content: content("AxisTooltip")}), node("RadialBar", {minAngle: 15, background: true, clockWise: true, dataKey: "uv", ...off})],
    interactions: [[{hover: [330, 150]}]],
  },
  {
    name: "FunnelTooltipContent", kind: "FunnelChart", width: 700, height: 432, margin: {right: 30}, data: null,
    source: "a funnel's item tooltip with a content element (the item's name and value)",
    children: [node("Tooltip", {content: content("ItemTooltip")}), node("Funnel", {dataKey: "value", data: funnel, ...off})],
    interactions: [[{hover: [330, 130]}]],
  },
  {
    name: "TreemapTooltipContent", kind: "Treemap", width: 500, height: 375, margin: null, data: flare,
    source: "a treemap's item tooltip with a content element (the leaf's name and value)",
    children: [node("Tooltip", {content: content("ItemTooltip")})],
    props: {dataKey: "size", nameKey: "name", aspectRatio: 4 / 3, stroke: "#fff", fill: "#8884d8", ...off},
    interactions: [[{hover: [50, 50]}]],
  },
  {
    name: "SankeyTooltipContent", kind: "Sankey", width: 800, height: 400, margin: {bottom: 30}, data: visits,
    source: "a Sankey's item tooltip with a content element (the link's name and value)",
    children: [node("Tooltip", {content: content("ItemTooltip")})],
    props: {nodePadding: 50, link: {stroke: "#77c878"}},
    interactions: [[{hover: [400, 200]}]],
  },
  {
    name: "CustomActiveFunnelLabels", kind: "FunnelChart", width: 700, height: 432, margin: {right: 30}, data: null,
    source: "CustomActiveFunnelChart with its names centered on the items: the hovered item's outlined rectangle drawn in its own layer, under the label list",
    children: [node("Tooltip"), node("Funnel", {dataKey: "value", data: funnel, shape: content("RoundedTrapezoid"), activeShape: content("OutlinedTrapezoid"), ...off},
      [node("LabelList", {position: "center", fill: "#fff", stroke: "none", dataKey: "name"})])],
    interactions: [[{hover: [150, 120]}]],
  },
  {
    name: "CustomActiveDotChart", kind: "ComposedChart", width: 500, height: 300, margin: {top: 20, right: 30, left: 20, bottom: 5}, data: pages,
    source: "an area's and a line's activeDot elements (a white-outlined square, a ring around a dot) under the tooltip",
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name"}), node("YAxis"), node("Tooltip"),
      node("Area", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", fill: "#82ca9d", activeDot: content("ActiveSquare"), ...off}),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", activeDot: content("ActiveRing"), ...off})],
    interactions: [[{hover: [212, 100]}]],
  },
  {
    name: "PolarTickContent", kind: "RadarChart", width: 500, height: 400, margin: null, data: subjects,
    source: "SimpleRadarChart with Recharts' docs' renderPolarAngleAxis as the angle axis' tick and a radius axis' tick element",
    chartProps: {cx: "50%", cy: "50%", outerRadius: "70%"},
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "subject", tick: content("AngleTick")}),
      node("PolarRadiusAxis", {angle: 30, domain: [0, 150], tick: content("RadiusTick")}),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, ...off})],
  },
  {
    name: "PolarRadiusLabelContent", kind: "RadarChart", width: 500, height: 400, margin: null, data: subjects,
    source: "SimpleRadarChart with a Label child of the radius axis with a content element (a title above the axis' middle, from the label's polar viewBox)",
    chartProps: {cx: "50%", cy: "50%", outerRadius: "70%"},
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "subject"}),
      node("PolarRadiusAxis", {angle: 30, domain: [0, 150]}, [node("Label", {value: "Score", content: content("RadiusTitle")})]),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, ...off})],
  },
  {
    name: "PieLabelListContent", kind: "PieChart", width: 400, height: 400, margin: null, data: [],
    source: "a donut's LabelList with a content element (each value at its sector's middle, from the label's polar viewBox)",
    children: [node("Pie", {data: groups, dataKey: "value", cx: "50%", cy: "50%", innerRadius: 60, outerRadius: 100, fill: "#8884d8", ...off}, [
      node("LabelList", {content: content("SectorValueLabel")})])],
  },
  {
    name: "RadialBarLabelListContent", kind: "RadialBarChart", width: 500, height: 300, margin: null, data: ages,
    source: "a radial bar's LabelList with a content element (each value at its bar's middle)",
    chartProps: {cx: "50%", cy: "50%", innerRadius: "20%", outerRadius: "90%", barSize: 14},
    children: [node("RadialBar", {dataKey: "uv", background: true, ...off}, [node("LabelList", {content: content("BarValueLabel")})])],
  },
  {
    name: "RadarLabelListContent", kind: "RadarChart", width: 500, height: 400, margin: null, data: subjects,
    source: "a radar's LabelList with a content element (each value in a dot above its point)",
    chartProps: {cx: "50%", cy: "50%", outerRadius: "80%"},
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "subject"}), node("PolarRadiusAxis"),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, ...off}, [node("LabelList", {content: content("RadarPointLabel")})])],
  },
  {
    name: "FunnelLabelListContent", kind: "FunnelChart", width: 700, height: 432, margin: {right: 30}, data: null,
    source: "a funnel's LabelList with a content element (each value in a white badge at its label box's center) beside its names",
    children: [node("Funnel", {dataKey: "value", data: funnel, ...off}, [
      node("LabelList", {position: "right", fill: "#000", stroke: "none", dataKey: "name"}),
      node("LabelList", {content: content("FunnelBadge")})])],
  },
  {
    name: "ActiveIndexBarChart", kind: "BarChart", width: 500, height: 300, margin: {top: 5, right: 30, left: 20, bottom: 5}, data: pages,
    source: "SimpleBarChart with a static activeIndex on both bars: the first with an activeBar object and a background object, the second with Recharts' default activeBar (false)",
    children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name"}), node("YAxis"), node("Tooltip"),
      node("Bar", {dataKey: "pv", fill: "#8884d8", activeIndex: 2, activeBar: {fill: "pink", stroke: "blue"}, background: {fill: "#eee"}, ...off}),
      node("Bar", {dataKey: "uv", fill: "#82ca9d", activeIndex: 4, ...off})],
    interactions: [[{hover: [100, 150]}]],
  },
  {
    name: "ActiveIndexScatterChart", kind: "ScatterChart", width: 500, height: 400, margin: {top: 20, right: 20, bottom: 20, left: 20}, data: null,
    source: "SimpleScatterChart with a static activeIndex and Recharts' default activeShape (a circle in the active layer)",
    children: [node("CartesianGrid"), node("XAxis", {type: "number", dataKey: "x", name: "stature", unit: "cm"}), node("YAxis", {type: "number", dataKey: "y", name: "weight", unit: "kg"}),
      node("Tooltip", {cursor: {strokeDasharray: "3 3"}}),
      node("Scatter", {name: "A school", data: schoolA, fill: "#8884d8", activeIndex: 2, ...off})],
    interactions: [[{hover: [302, 185]}]],
  },
  {
    name: "CustomActiveIndexScatterChart", kind: "ScatterChart", width: 500, height: 400, margin: {top: 20, right: 20, bottom: 20, left: 20}, data: null,
    source: "CustomShapeScatterChart with a static activeIndex: the active diamond drawn in place of the third point, kept while another point is hovered",
    children: [node("CartesianGrid"), node("XAxis", {type: "number", dataKey: "x", name: "stature", unit: "cm"}), node("YAxis", {type: "number", dataKey: "y", name: "weight", unit: "kg"}),
      node("Tooltip", {cursor: {strokeDasharray: "3 3"}}),
      node("Scatter", {name: "A school", data: schoolA, fill: "#8884d8", shape: content("DiamondShape"), activeShape: content("ActiveDiamond"), activeIndex: 2, ...off})],
    interactions: [[{hover: [302, 185]}]],
  },
  {
    name: "ActiveIndexFunnelChart", kind: "FunnelChart", width: 700, height: 432, margin: {right: 30}, data: null,
    source: "FunnelChartExample with activeIndex as an array and Recharts' default activeShape (each active trapezoid in the active layer)",
    children: [node("Tooltip"),
      node("Funnel", {dataKey: "value", data: funnel, activeIndex: [0, 2], ...off},
        [node("LabelList", {position: "right", fill: "#000", stroke: "none", dataKey: "name"})])],
    interactions: [[{hover: [330, 130]}]],
  },
  {
    name: "CustomActiveIndexFunnelChart", kind: "FunnelChart", width: 700, height: 432, margin: {right: 30}, data: null,
    source: "CustomActiveFunnelChart with a static activeIndex: the outlined rectangle drawn in place of the fourth item, under its label",
    children: [node("Tooltip"), node("Funnel", {dataKey: "value", data: funnel, shape: content("RoundedTrapezoid"), activeShape: content("OutlinedTrapezoid"), activeIndex: 3, ...off},
      [node("LabelList", {position: "center", fill: "#fff", stroke: "none", dataKey: "name"})])],
    interactions: [[{hover: [330, 130]}]],
  },
  {
    name: "ActiveIndexRadialBarChart", kind: "RadialBarChart", width: 500, height: 300, margin: null, data: ages,
    source: "CustomActiveRadialBarChart with a static activeIndex: the grown sector drawn in place of the fourth bar, kept while another ring is hovered",
    chartProps: {cx: "50%", cy: "50%", innerRadius: "10%", outerRadius: "80%", barSize: 10},
    children: [node("Tooltip"), node("RadialBar", {minAngle: 15, background: true, clockWise: true, dataKey: "uv", activeShape: content("GrownSector"), activeIndex: 3, ...off})],
    interactions: [[{hover: [330, 150]}]],
  },
  {
    name: "DefaultActiveIndexRadialBarChart", kind: "RadialBarChart", width: 500, height: 300, margin: null, data: ages,
    source: "SimpleRadialBarChart with a static activeIndex and Recharts' default activeShape (the bar's own sector in the active layer)",
    chartProps: {cx: "50%", cy: "50%", innerRadius: "10%", outerRadius: "80%", barSize: 10},
    children: [node("RadialBar", {minAngle: 15, background: true, clockWise: true, dataKey: "uv", activeIndex: 1, ...off})],
  },
  {
    name: "ActiveIndexPieChart", kind: "PieChart", width: 400, height: 400, margin: null, data: [],
    source: "PieActiveShapes' activeIndex and activeShape object with a tooltip: hovering another sector keeps the static active one",
    children: [node("Tooltip"), node("Pie", {data: groups, dataKey: "value", cx: "50%", cy: "50%", innerRadius: 50, outerRadius: 80, fill: "#8884d8", activeIndex: 1, activeShape: {outerRadius: 90, fill: "#ff7300"}, ...off})],
    interactions: [[{hover: [200, 130]}]],
  },
];
