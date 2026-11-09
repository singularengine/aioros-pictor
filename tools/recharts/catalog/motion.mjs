// Recharts' update animations: each case mounts a reference example with its animations on (Recharts' defaults), lets
// the enter animation end, then renders it again with new data and records every frame of the update animation; an
// `enter` case records the enter animation itself, from the mount.
const copy = (value) => JSON.parse(JSON.stringify(value));

/** The example with every `isAnimationActive: false` removed, so each series animates with Recharts' defaults. */
export function animated(description) {
  const strip = (node) => {
    if (node.props) delete node.props.isAnimationActive;
    (node.children ?? []).forEach(strip);
  };
  const result = copy(description);
  if (result.props) delete result.props.isAnimationActive;
  result.children.forEach(strip);
  return result;
}

/** Each row's values of the keys taken from the next row (the last from the first), so every value changes and the count stays. */
const rotate = (rows, keys) => rows.map((row, i) => ({...row, ...Object.fromEntries(keys.map((key) => [key, rows[(i + 1) % rows.length][key]]))}));

const chartData = (change) => (description) => ({...description, data: change(description.data)});

/** A change of the own data of the series of a type (a pie's, a scatter's, a funnel's). */
const ownData = (type, change) => (description) => ({
  ...description,
  children: description.children.map((child, index) => child.type === type ? {...child, props: {...child.props, data: change(child.props.data, index)}} : child),
});

const extraPages = [
  {name: "Page H", uv: 2600, pv: 5200, amt: 2300},
  {name: "Page I", uv: 3100, pv: 2900, amt: 2400},
];

const scaleSizes = (nodes, factor) => nodes.map((node, i) => node.children
  ? {...node, children: scaleSizes(node.children, factor)}
  : {...node, size: Math.round(node.size * (i % 2 === 0 ? factor : 1 / factor))});

/** A treemap with `isUpdateAnimationActive={false}`, which Recharts passes negated to each node's `Rectangle`. */
const rectanglesAnimate = (description) => ({...description, props: {...description.props, isUpdateAnimationActive: false}});

/** The cases whose shapes' inline styles strokes.mjs records (a style Recharts animates by a CSS transition). */
export const strokeCases = [{name: "TreemapRectanglesEnter", example: "SimpleTreemap", to: rectanglesAnimate}];

export const motionCases = [
  {name: "LineValues", example: "SimpleLineChart", to: chartData((rows) => rotate(rows, ["pv", "uv"]))},
  {name: "LineMore", example: "SimpleLineChart", to: chartData((rows) => [...rows, ...extraPages])},
  {name: "LineFewer", example: "SimpleLineChart", to: chartData((rows) => rows.slice(0, 5))},
  {name: "LineFromEmpty", example: "SimpleLineChart", from: chartData(() => []), to: chartData((rows) => rows)},
  {name: "AreaValues", example: "SimpleAreaChart", to: chartData((rows) => rotate(rows, ["uv"]))},
  {name: "StackedAreaMore", example: "StackedAreaChart", to: chartData((rows) => [...rotate(rows, ["uv", "pv", "amt"]), ...extraPages])},
  {name: "BarMore", example: "SimpleBarChart", to: chartData((rows) => [...rotate(rows, ["pv", "uv"]), extraPages[0]])},
  {name: "VerticalBarMore", example: "VerticalBarChart", to: chartData((rows) => [...rotate(rows, ["pv", "uv"]), extraPages[0]])},
  {name: "ScatterMore", example: "SimpleScatterChart", to: ownData("Scatter", (rows) => [...rows.map((row, i) => ({...row, y: rows[(i + 1) % rows.length].y})), {x: 130, y: 350, z: 300}, {x: 160, y: 220, z: 200}])},
  {name: "PieValues", example: "TwoSimplePieChart", to: ownData("Pie", (rows, index) => index === 0 ? [...rows.map((row, i) => ({...row, value: rows[(i + 1) % rows.length].value})), {name: "Group G", value: 250}] : rows.slice(0, 4))},
  {name: "RadarValues", example: "SimpleRadarChart", to: chartData((rows) => rotate(rows, ["A", "B"]))},
  {name: "RadialBarValues", example: "SimpleRadialBarChart", to: chartData((rows) => rotate(rows, ["uv", "pv"]))},
  {name: "FunnelMore", example: "FunnelChart", to: ownData("Funnel", (rows) => [...rows.map((row) => ({...row, value: Math.round(row.value * 0.8)})), {value: 10, name: "Retained", fill: "#a4de6c"}])},
  {name: "TreemapValues", example: "SimpleTreemap", to: chartData((rows) => scaleSizes(rows, 1.6))},
  {name: "TreemapRectanglesValues", example: "SimpleTreemap", from: rectanglesAnimate, to: (description) => rectanglesAnimate(chartData((rows) => scaleSizes(rows, 1.6))(description))},
  {name: "PieEnter", example: "TwoLevelPieChart", enter: true, to: (description) => description},
  {name: "StraightPieEnter", example: "StraightAnglePieChart", enter: true, to: (description) => description},
  {name: "RadarEnter", example: "SimpleRadarChart", enter: true, to: (description) => description},
  {name: "RadialBarEnter", example: "SimpleRadialBarChart", enter: true, to: (description) => description},
];
