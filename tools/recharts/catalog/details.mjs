import {examples as scatter} from "./scatter.mjs";
import {examples as hierarchy} from "./hierarchy.mjs";
import {examples as features} from "./features.mjs";
import {examples as polar} from "./polar.mjs";
import {node, off, pages} from "./common.mjs";

const source = (what) => `Pictor's details catalog: ${what}`;
const byName = (name) => [...scatter, ...hierarchy, ...features, ...polar].find((example) => example.name === name);
const lineChart = (name, tooltip, interactions, what) => ({
  name, kind: "LineChart", width: 500, height: 300, margin: {top: 5, right: 30, left: 20, bottom: 5}, data: pages,
  source: source(`SimpleLineChart, its tooltip ${what}, the mouse resting at points inside categories`),
  children: [node("CartesianGrid", {strokeDasharray: "3 3"}), node("XAxis", {dataKey: "name"}), node("YAxis"), node("Tooltip", tooltip), node("Legend"),
    node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", activeDot: {r: 8}, ...off}),
    node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off})],
  interactions,
});
const rest = (...points) => points.map((point) => [{hover: point}]);
const keys = (...pressed) => [{focus: true}, ...pressed.map((key) => ({key}))];

export const examples = [
  {
    ...byName("SimpleScatterChart"), name: "DetailsKeyboardScatterChart", chartProps: {accessibilityLayer: true},
    source: source("SimpleScatterChart with accessibilityLayer, focused and moved by the arrow keys (Recharts' AccessibilityManager spoofs the pointer at each point's x)"),
    interactions: [keys(), keys("ArrowRight", "ArrowRight"), keys("ArrowRight", "ArrowRight", "ArrowLeft"), keys(...Array(7).fill("ArrowRight")), keys("ArrowLeft", "ArrowDown", "Escape")],
  },
  {
    ...byName("ThreeDimScatterChart"), name: "DetailsKeyboardTwoScatterChart", chartProps: {accessibilityLayer: true},
    source: source("ThreeDimScatterChart with accessibilityLayer: the arrow keys walk both scatters' points in the order of their data"),
    interactions: [keys(...Array(6).fill("ArrowRight")), keys(...Array(13).fill("ArrowRight"))],
  },
  {
    ...byName("NestedTreemap"), name: "DetailsNestTreemapWithoutNameKey", props: {...byName("NestedTreemap").props, nameKey: undefined},
    source: source("NestedTreemap without nameKey (Recharts' Treemap has no default): a zoomed node's nest index boxes read 'root' and its tooltips show no name"),
    interactions: [[{click: [80, 60]}], [{click: [80, 60]}, {hover: [200, 100]}]],
  },
  lineChart("DetailsFollowLineChart", {}, rest([150, 40], [160, 120], [150, 200], [300, 240], [420, 10]), "following the pointer"),
  lineChart("DetailsFollowReversedChart", {reverseDirection: {x: true, y: true}}, rest([150, 40], [160, 120], [300, 240], [100, 30]), "following the pointer with reverseDirection"),
  lineChart("DetailsFollowEscapeChart", {allowEscapeViewBox: {x: true, y: true}}, rest([150, 40], [300, 240], [440, 200]), "following the pointer with allowEscapeViewBox"),
  lineChart("DetailsFollowFixedChart", {position: {y: 60}}, rest([150, 40], [300, 240]), "with position.y fixed"),
  {
    ...byName("VerticalLineChart"), name: "DetailsFollowVerticalChart",
    source: source("VerticalLineChart with the mouse resting at points inside categories: the box follows the pointer's x"),
    interactions: rest([150, 60], [300, 60], [440, 150], [100, 260]),
  },
  {
    ...byName("SpecifiedDomainRadarChart"), name: "DetailsFollowRadarChart",
    source: source("SpecifiedDomainRadarChart with the mouse resting at points of a category's sector: the box stands at the pointer's distance from the center along the category's angle"),
    interactions: rest([250, 60], [250, 150], [330, 215], [160, 300], [380, 120]),
  },
  {
    ...byName("SimpleRadialBarChart"), name: "DetailsFollowRadialBarChart",
    source: source("SimpleRadialBarChart with the mouse resting at points of a band: the box stands at the pointer's angle on the band's radius"),
    interactions: rest([150, 40], [300, 150], [250, 260], [150, 200]),
  },
  {
    name: "DetailsPresentationChart", kind: "ComposedChart", width: 500, height: 300, margin: {top: 10, right: 30, left: 20, bottom: 5},
    data: pages.map((page) => ({...page, error: [200, 400]})),
    source: source("SVG presentation attributes Recharts spreads onto a grid's lines, an axis' line, tick lines and texts, a line's curve and dots, an area's curves and an error bar's lines"),
    children: [
      node("CartesianGrid", {strokeDasharray: "3 3", strokeLinecap: "round", opacity: 0.8}),
      node("XAxis", {dataKey: "name", opacity: 0.7, strokeOpacity: 0.5, fontWeight: "bold", fontStyle: "italic", strokeLinecap: "square"}),
      node("YAxis", {strokeLinejoin: "bevel", fontWeight: 600, opacity: 0.9}),
      node("Area", {type: "monotone", dataKey: "amt", fill: "#8884d8", stroke: "#8884d8", strokeOpacity: 0.4, strokeLinejoin: "round", opacity: 0.6, ...off}),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#ff7300", strokeWidth: 3, strokeLinecap: "round", strokeLinejoin: "round", strokeMiterlimit: 2, strokeOpacity: 0.8, opacity: 0.9, ...off},
        [node("ErrorBar", {dataKey: "error", stroke: "red", strokeOpacity: 0.5, strokeLinecap: "round", opacity: 0.7})]),
    ],
  },
  {
    name: "DetailsClassedChart", kind: "ComposedChart", width: 500, height: 300, margin: {top: 5, right: 30, left: 20, bottom: 5}, data: pages,
    chartProps: {className: "sales", style: {background: "#fafafa", border: "1px solid #eee"}},
    source: source("className on the chart, its grid, axes and series, and a style on the chart's wrapper"),
    children: [node("CartesianGrid", {strokeDasharray: "3 3", className: "grid"}), node("XAxis", {dataKey: "name", className: "pages"}), node("YAxis", {className: "values"}),
      node("Tooltip"), node("Legend"),
      node("Area", {type: "monotone", dataKey: "amt", fill: "#8884d8", stroke: "#8884d8", className: "amounts", ...off}),
      node("Bar", {dataKey: "uv", fill: "#82ca9d", className: "visitors", ...off}),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#ff7300", className: "views", ...off})],
  },
  {
    name: "DetailsClassedPieChart", kind: "PieChart", width: 400, height: 300, margin: null, data: null,
    chartProps: {className: "shares", style: {background: "#fafafa"}},
    source: source("className on a pie chart and its pie"),
    children: [node("Pie", {data: pages.slice(0, 4).map((page) => ({name: page.name, value: page.uv})), dataKey: "value", cx: "50%", cy: "50%", outerRadius: 80, fill: "#8884d8", className: "pages", ...off}), node("Tooltip")],
  },
  {
    ...byName("SimpleRadialBarChart"), name: "DetailsRoleRadialBarChart", chartProps: {...byName("SimpleRadialBarChart").chartProps, accessibilityLayer: true, role: "img", tabIndex: 2},
    source: source("SimpleRadialBarChart with accessibilityLayer, its surface's role and tabIndex overridden"),
  },
];
