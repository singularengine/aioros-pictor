import {createElement} from "react";
import * as Recharts from "recharts";
import {node, off, pages} from "./catalog/common.mjs";
import {examples as polar} from "./catalog/polar.mjs";
import {examples as scatter} from "./catalog/scatter.mjs";
import {examples as hierarchy} from "./catalog/hierarchy.mjs";
import {examples as features} from "./catalog/features.mjs";
import {examples as axes} from "./catalog/axes.mjs";
import {examples as ranges} from "./catalog/ranges.mjs";
import {examples as sunburst} from "./catalog/sunburst.mjs";
import {examples as tips} from "./catalog/tips.mjs";
import {examples as gaps} from "./catalog/gaps.mjs";
import {examples as custom, contents as customContents} from "./catalog/custom.mjs";
import {examples as details} from "./catalog/details.mjs";
import {examples as final, contents as finalContents} from "./catalog/final.mjs";
import {matrix} from "./catalog/matrix.mjs";

const signedPages = [
  {name: "Page A", uv: 4000, pv: 2400, amt: 2400},
  {name: "Page B", uv: -3000, pv: 1398, amt: 2210},
  {name: "Page C", uv: -2000, pv: -9800, amt: 2290},
  {name: "Page D", uv: 2780, pv: 3908, amt: 2000},
  {name: "Page E", uv: -1890, pv: 4800, amt: 2181},
  {name: "Page F", uv: 2390, pv: -3800, amt: 2500},
  {name: "Page G", uv: 3490, pv: 4300, amt: 2100},
];

const composedPages = [
  {name: "Page A", uv: 590, pv: 800, amt: 1400, cnt: 490},
  {name: "Page B", uv: 868, pv: 967, amt: 1506, cnt: 590},
  {name: "Page C", uv: 1397, pv: 1098, amt: 989, cnt: 350},
  {name: "Page D", uv: 1480, pv: 1200, amt: 1228, cnt: 480},
  {name: "Page E", uv: 1520, pv: 1108, amt: 1100, cnt: 460},
  {name: "Page F", uv: 1400, pv: 680, amt: 1700, cnt: 380},
];

const cellColors = ["#0088FE", "#00C49F", "#FFBB28", "#FF8042", "red", "pink"];

const grid = node("CartesianGrid", {strokeDasharray: "3 3"});
const xName = node("XAxis", {dataKey: "name"});
const yAxis = node("YAxis");
const tooltip = node("Tooltip");
const legend = node("Legend");
const standardMargin = {top: 5, right: 30, left: 20, bottom: 5};

const cartesian = [
  {
    name: "SimpleLineChart", kind: "LineChart", width: 500, height: 300, margin: standardMargin, data: pages,
    source: "www/src/docs/exampleComponents/LineChart/SimpleLineChart.tsx",
    children: [grid, xName, yAxis, tooltip, legend,
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", activeDot: {r: 8}, ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off})],
  },
  {
    name: "DashedLineChart", kind: "LineChart", width: 500, height: 300, margin: standardMargin, data: pages,
    source: "www/src/docs/exampleComponents/LineChart/DashedLineChart.tsx",
    children: [grid, xName, yAxis, tooltip, legend,
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", strokeDasharray: "5 5", ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", strokeDasharray: "3 4 5 2", ...off})],
  },
  {
    name: "LineChartWithReferenceLines", kind: "LineChart", width: 500, height: 300,
    margin: {top: 20, right: 50, left: 20, bottom: 5}, data: pages,
    source: "www/src/docs/exampleComponents/LineChart/LineChartWithReferenceLines.tsx",
    children: [grid, xName, yAxis, tooltip, legend,
      node("ReferenceLine", {x: "Page C", stroke: "red", label: "Max PV PAGE"}),
      node("ReferenceLine", {y: 9800, label: "Max", stroke: "red"}),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off})],
  },
  {
    name: "CustomizedLabelLineChart", kind: "LineChart", width: 500, height: 300,
    margin: {top: 20, right: 30, left: 20, bottom: 10}, data: pages,
    source: "www/src/docs/exampleComponents/LineChart/CustomizedLabelLineChart.tsx (custom label and tick components replaced by LabelList and the label prop)",
    children: [grid, xName, yAxis, tooltip, legend,
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off},
        [node("LabelList", {dataKey: "pv", position: "top"})]),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", label: true, ...off})],
  },
  {
    name: "SimpleAreaChart", kind: "AreaChart", width: 500, height: 400,
    margin: {top: 10, right: 30, left: 0, bottom: 0}, data: pages,
    source: "www/src/docs/exampleComponents/AreaChart/SimpleAreaChart.tsx",
    children: [grid, xName, yAxis, tooltip,
      node("Area", {type: "monotone", dataKey: "uv", stroke: "#8884d8", fill: "#8884d8", ...off})],
  },
  {
    name: "StackedAreaChart", kind: "AreaChart", width: 500, height: 400,
    margin: {top: 10, right: 30, left: 0, bottom: 0}, data: pages,
    source: "www/src/docs/exampleComponents/AreaChart/StackedAreaChart.tsx",
    children: [grid, xName, yAxis, tooltip,
      node("Area", {type: "monotone", dataKey: "uv", stackId: "1", stroke: "#8884d8", fill: "#8884d8", ...off}),
      node("Area", {type: "monotone", dataKey: "pv", stackId: "1", stroke: "#82ca9d", fill: "#82ca9d", ...off}),
      node("Area", {type: "monotone", dataKey: "amt", stackId: "1", stroke: "#ffc658", fill: "#ffc658", ...off})],
  },
  {
    name: "SimpleBarChart", kind: "BarChart", width: 500, height: 300, margin: standardMargin, data: pages,
    source: "www/src/docs/exampleComponents/BarChart/SimpleBarChart.tsx",
    children: [grid, xName, yAxis, tooltip, legend,
      node("Bar", {dataKey: "pv", fill: "#8884d8", activeBar: {fill: "pink", stroke: "blue"}, ...off}),
      node("Bar", {dataKey: "uv", fill: "#82ca9d", activeBar: {fill: "gold", stroke: "purple"}, ...off})],
  },
  {
    name: "StackedBarChart", kind: "BarChart", width: 500, height: 300, margin: standardMargin, data: pages,
    source: "www/src/docs/exampleComponents/BarChart/StackedBarChart.tsx",
    children: [grid, xName, yAxis, tooltip, legend,
      node("Bar", {dataKey: "pv", stackId: "a", fill: "#8884d8", ...off}),
      node("Bar", {dataKey: "uv", stackId: "a", fill: "#82ca9d", ...off})],
  },
  {
    name: "MixBarChart", kind: "BarChart", width: 500, height: 300,
    margin: {top: 20, right: 30, left: 20, bottom: 5}, data: pages,
    source: "www/src/docs/exampleComponents/BarChart/MixBarChart.tsx",
    children: [grid, xName, yAxis, tooltip, legend,
      node("Bar", {dataKey: "pv", stackId: "a", fill: "#8884d8", ...off}),
      node("Bar", {dataKey: "amt", stackId: "a", fill: "#82ca9d", ...off}),
      node("Bar", {dataKey: "uv", fill: "#ffc658", ...off})],
  },
  {
    name: "PositiveAndNegativeBarChart", kind: "BarChart", width: 500, height: 300, margin: standardMargin,
    data: signedPages,
    source: "www/src/docs/exampleComponents/BarChart/PositiveAndNegativeBarChart.tsx",
    children: [grid, xName, yAxis, tooltip, legend,
      node("ReferenceLine", {y: 0, stroke: "#000"}),
      node("Bar", {dataKey: "pv", fill: "#8884d8", ...off}),
      node("Bar", {dataKey: "uv", fill: "#82ca9d", ...off})],
  },
  {
    name: "BarChartWithCell", kind: "BarChart", width: 500, height: 300,
    margin: {top: 20, right: 30, left: 20, bottom: 5}, data: pages,
    source: "www/src/docs/exampleComponents/BarChart/CustomShapeBarChart.tsx (default rectangle shape instead of the custom triangle; colors cycled with index % colors.length)",
    children: [grid, xName, yAxis,
      node("Bar", {dataKey: "uv", fill: "#8884d8", label: {position: "top"}, ...off},
        pages.map((_, index) => node("Cell", {fill: cellColors[index % cellColors.length]})))],
  },
  {
    name: "BarChartHasBackground", kind: "BarChart", width: 500, height: 300, margin: standardMargin, data: pages,
    source: "www/src/docs/exampleComponents/BarChart/BarChartHasBackground.tsx",
    children: [grid, xName, yAxis, tooltip, legend,
      node("Bar", {dataKey: "pv", fill: "#8884d8", background: {fill: "#eee"}, ...off}),
      node("Bar", {dataKey: "uv", fill: "#82ca9d", ...off})],
  },
  {
    name: "LineBarAreaComposedChart", kind: "ComposedChart", width: 500, height: 400,
    margin: {top: 20, right: 20, bottom: 20, left: 20}, data: composedPages,
    source: "www/src/docs/exampleComponents/ComposedChart/LineBarAreaComposedChart.tsx",
    children: [node("CartesianGrid", {stroke: "#f5f5f5"}), node("XAxis", {dataKey: "name", scale: "band"}), yAxis,
      tooltip, legend,
      node("Area", {type: "monotone", dataKey: "amt", fill: "#8884d8", stroke: "#8884d8", ...off}),
      node("Bar", {dataKey: "pv", barSize: 20, fill: "#413ea0", ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#ff7300", ...off}),
      node("Scatter", {dataKey: "cnt", fill: "red", ...off})],
    interactions: [[{hover: [110, 20]}], [{hover: [116, 20]}], [{hover: [470, 20]}]],
  },
  {
    name: "BiaxialLineChart", kind: "LineChart", width: 500, height: 300, margin: standardMargin, data: pages,
    source: "www/src/docs/exampleComponents/LineChart/BiaxialLineChart.tsx",
    children: [grid, xName, node("YAxis", {yAxisId: "left"}), node("YAxis", {yAxisId: "right", orientation: "right"}),
      tooltip, legend,
      node("Line", {yAxisId: "left", type: "monotone", dataKey: "pv", stroke: "#8884d8", activeDot: {r: 8}, ...off}),
      node("Line", {yAxisId: "right", type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off})],
  },
  {
    name: "TinyLineChart", kind: "LineChart", width: 300, height: 100, margin: null, data: pages,
    source: "www/src/docs/exampleComponents/LineChart/TinyLineChart.tsx",
    children: [node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", strokeWidth: 2, ...off})],
  },
];

const contents = {...customContents, ...finalContents};

const catalog = [...cartesian, ...features, ...polar, ...scatter, ...hierarchy, ...axes, ...ranges, ...sunburst, ...tips, ...gaps, ...details, ...custom, ...final];

export const examples = [...catalog, ...matrix((name) => catalog.find((example) => example.name === name))];

const formatters = {
  isoDate: (value) => new Date(value).toISOString().slice(0, 10),
};

const format = (pattern) => (value) => pattern.replace("{0}", value);

const withFormatters = (props) => typeof props?.tickFormatter === "string"
  ? {...props, tickFormatter: formatters[props.tickFormatter] ?? format(props.tickFormatter)}
  : props;

/** A .NET composite format given where Recharts takes a formatter, as the function Recharts calls: `{0}` is the value. */
const formatter = (pattern) => (value) => pattern.replaceAll("{0}", String(value));

/** What a click handler received, kept for the C# side: the index Recharts passed (an item's second argument, a dot's or the chart's state's index) and its label. */
function clicked(args) {
  const [first, second] = args;
  const index = typeof second === "number" ? second : first?.activeTooltipIndex ?? first?.index;
  const label = first?.activeLabel ?? first?.payload?.name ?? first?.name;
  return {index: index ?? null, ...(label === undefined ? {} : {label: String(label)}), ...(first?.dataKey === undefined ? {} : {dataKey: String(first.dataKey)})};
}

/** A `{$click}` marker as the handler Recharts calls: it records the handler's name and what it received in `window.pictorClicks`. */
const click = (name) => (...args) => {
  if (typeof window === "undefined") return;
  (window.pictorClicks ??= []).push({handler: name, ...clicked(args)});
};

/** Props as Recharts takes them: `{$format}` markers become formatter functions, `{$content}` markers the custom content they name, `{$click}` markers recording handlers. */
function revive(value) {
  if (Array.isArray(value)) return value.map(revive);
  if (value === null || typeof value !== "object") return value;
  if (typeof value.$format === "string") return formatter(value.$format);
  if (typeof value.$click === "string") return click(value.$click);
  if (typeof value.$content === "string") return contents[value.$content];
  return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, revive(item)]));
}

export function element(example) {
  const make = (description, key) => createElement(Recharts[description.type] ?? description.type, {key, ...withFormatters(revive(description.props))},
    ...description.children.map(make));
  const props = {width: example.width, height: example.height, ...revive(example.chartProps), ...revive(example.props)};
  if (example.data) props.data = example.data;
  if (example.margin) props.margin = example.margin;
  if (example.layout) props.layout = example.layout;
  if (example.container) {
    delete props.width;
    delete props.height;
    return createElement(Recharts.ResponsiveContainer, example.container, createElement(Recharts[example.kind], props, ...example.children.map(make)));
  }
  return createElement(Recharts[example.kind], props, ...example.children.map(make));
}

export function describe(example) {
  return {
    name: example.name,
    kind: example.kind,
    ...(example.layout ? {layout: example.layout} : {}),
    width: example.width,
    height: example.height,
    margin: example.margin,
    ...(example.chartProps ? {chartProps: example.chartProps} : {}),
    source: example.source,
    data: example.data,
    props: example.props,
    children: example.children,
    ...(example.interactions ? {interactions: example.interactions} : {}),
    ...(example.container ? {container: example.container} : {}),
  };
}

