import {node, off, pages} from "./common.mjs";

const grid = node("CartesianGrid", {strokeDasharray: "3 3"});
const tooltip = node("Tooltip");
const legend = node("Legend");
const standardMargin = {top: 5, right: 30, left: 20, bottom: 5};

const months = [
  {name: "January", uv: 4000, pv: 2400},
  {name: "February", uv: 3000, pv: 1398},
  {name: "March", uv: 2000, pv: 9800},
  {name: "April", uv: 2780, pv: 3908},
  {name: "May", uv: 1890, pv: 4800},
  {name: "June", uv: 2390, pv: 3800},
  {name: "July", uv: 3490, pv: 4300},
  {name: "August", uv: 3200, pv: 2900},
  {name: "September", uv: 2600, pv: 5100},
  {name: "October", uv: 3900, pv: 4100},
  {name: "November", uv: 2100, pv: 3600},
  {name: "December", uv: 4400, pv: 5300},
];

const days = Array.from({length: 40}, (_, index) => ({
  day: `Day ${index + 1}`,
  value: 300 + ((index * 137) % 500),
}));

const measures = [
  {x: 5, y: 12, z: 3},
  {x: 18, y: 45, z: 7},
  {x: 26, y: 31, z: 1},
  {x: 41, y: 77, z: 9},
  {x: 63, y: 58, z: 4},
  {x: 87, y: 96, z: 6},
];

const multiSeries = [
  {name: "Series 1", data: [{category: "A", value: 10}, {category: "B", value: 22}, {category: "C", value: 15}]},
  {name: "Series 2", data: [{category: "B", value: 31}, {category: "C", value: 18}, {category: "D", value: 25}]},
  {name: "Series 3", data: [{category: "C", value: 12}, {category: "D", value: 27}, {category: "E", value: 20}]},
];

const curveTypes = ["basis", "basisClosed", "basisOpen", "bumpX", "bumpY", "bump", "linear", "linearClosed", "natural",
  "monotoneX", "monotoneY", "monotone", "step", "stepBefore", "stepAfter"];

const palette = ["#8884d8", "#82ca9d", "#ffc658", "#ff7300", "#0088FE", "#00C49F", "#FF8042"];

const growth = [
  {name: "2016", users: 12},
  {name: "2017", users: 95},
  {name: "2018", users: 410},
  {name: "2019", users: 1800},
  {name: "2020", users: 5200},
  {name: "2021", users: 21000},
  {name: "2022", users: 64000},
];

const timeline = [
  {time: Date.UTC(2024, 0, 1), value: 30},
  {time: Date.UTC(2024, 0, 9), value: 42},
  {time: Date.UTC(2024, 0, 20), value: 28},
  {time: Date.UTC(2024, 1, 3), value: 51},
  {time: Date.UTC(2024, 1, 17), value: 47},
  {time: Date.UTC(2024, 2, 2), value: 63},
  {time: Date.UTC(2024, 2, 30), value: 58},
];

const regions = [
  {region: "North America", sales: 4200},
  {region: "South America", sales: 1800},
  {region: "Western Europe", sales: 3900},
  {region: "Asia Pacific", sales: 5100},
  {region: "Africa", sales: 900},
];

const sizedA = [
  {x: 100, y: 200, z: 200}, {x: 120, y: 100, z: 260}, {x: 170, y: 300, z: 400},
  {x: 140, y: 250, z: 280}, {x: 150, y: 400, z: 500}, {x: 110, y: 280, z: 200},
];

const sizedB = [
  {x: 200, y: 260, size: "S"}, {x: 240, y: 290, size: "L"}, {x: 190, y: 290, size: "M"},
  {x: 198, y: 250, size: "S"}, {x: 180, y: 280, size: "L"}, {x: 210, y: 220, size: "M"},
];

const signedPages = [
  {name: "Page A", uv: 4000, pv: 2400},
  {name: "Page B", uv: -3000, pv: 1398},
  {name: "Page C", uv: -2000, pv: -9800},
  {name: "Page D", uv: 2780, pv: 3908},
  {name: "Page E", uv: -1890, pv: 4800},
  {name: "Page F", uv: 2390, pv: -3800},
  {name: "Page G", uv: 3490, pv: 4300},
];

const subjects = [
  {subject: "Math", A: 120, B: 110, fullMark: 150},
  {subject: "Chinese", A: 98, B: 130, fullMark: 150},
  {subject: "English", A: 86, B: 130, fullMark: 150},
  {subject: "Geography", A: 99, B: 100, fullMark: 150},
  {subject: "Physics", A: 85, B: 90, fullMark: 150},
  {subject: "History", A: 65, B: 85, fullMark: 150},
];

const ages = [
  {name: "18-24", uv: 31.47, fill: "#8884d8"},
  {name: "25-29", uv: 26.69, fill: "#83a6ed"},
  {name: "30-34", uv: 15.69, fill: "#8dd1e1"},
  {name: "35-39", uv: 8.22, fill: "#82ca9d"},
  {name: "40-49", uv: 8.63, fill: "#a4de6c"},
  {name: "50+", uv: 2.63, fill: "#d0ed57"},
];

export const examples = [
  {
    name: "AxisLabelLineChart", kind: "LineChart", width: 500, height: 300,
    margin: {top: 5, right: 30, left: 20, bottom: 20}, data: pages,
    source: "www/src/docs/api/Label.ts's example (XAxis with a Label child, YAxis with a label object), plus reference lines with label objects",
    children: [grid,
      node("XAxis", {dataKey: "name"}, [node("Label", {value: "Pages of my website", offset: 0, position: "insideBottom"})]),
      node("YAxis", {label: {value: "pv of page", angle: -90, position: "insideLeft"}}),
      tooltip,
      node("ReferenceLine", {y: 4000, stroke: "red", label: {value: "Target", position: "insideTopRight", fill: "red", fontSize: 12, offset: 8}}),
      node("ReferenceLine", {x: "Page E", stroke: "green", label: {value: "Launch", position: "insideTopLeft", angle: -30, fill: "green"}}),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off})],
  },
  {
    name: "ReferenceLabelsChart", kind: "LineChart", width: 500, height: 300, margin: standardMargin, data: pages,
    source: "Recharts' ReferenceArea and ReferenceDot API pages, with label objects (position, offset, fill, fontSize, angle)",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip,
      node("ReferenceArea", {x1: "Page B", x2: "Page D", y1: 1000, y2: 6000, stroke: "red", strokeOpacity: 0.3,
        label: {value: "Campaign", position: "insideBottomRight", offset: 12, fill: "#b00", fontSize: 16}}),
      node("ReferenceDot", {x: "Page C", y: 9800, r: 8, fill: "red", stroke: "none",
        label: {value: "Peak", position: "right", offset: 14, fill: "#333", angle: 15}}),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off})],
  },
  {
    name: "RotatedTickBarChart", kind: "BarChart", width: 500, height: 300,
    margin: {top: 5, right: 30, left: 20, bottom: 5}, data: months,
    source: "a bar chart of twelve months with rotated tick labels (XAxis angle and textAnchor), the usual shape of Recharts' rotated-label recipes",
    children: [grid,
      node("XAxis", {dataKey: "name", angle: -45, textAnchor: "end", height: 70}),
      node("YAxis"), tooltip,
      node("Bar", {dataKey: "pv", fill: "#8884d8", ...off})],
  },
  {
    name: "SteepTickLineChart", kind: "LineChart", width: 500, height: 300,
    margin: {top: 5, right: 30, left: 20, bottom: 5}, data: months,
    source: "the twelve months with ticks at -90 degrees (the angled width is the label's height) and at 30 degrees on a top axis",
    children: [grid,
      node("XAxis", {dataKey: "name", angle: -90, textAnchor: "end", height: 80}),
      node("XAxis", {dataKey: "name", xAxisId: "top", orientation: "top", angle: 30, textAnchor: "start", height: 50}),
      node("YAxis"), tooltip,
      node("Line", {dataKey: "uv", stroke: "#82ca9d", ...off}),
      node("Line", {dataKey: "pv", xAxisId: "top", stroke: "#8884d8", ...off})],
  },
  {
    name: "StyledAxesChart", kind: "LineChart", width: 500, height: 300, margin: standardMargin, data: pages,
    source: "the tick, axisLine and tickLine props objects of XAxis and YAxis, with tickSize and tickMargin",
    children: [grid,
      node("XAxis", {dataKey: "name", tick: {fill: "#8884d8", fontSize: 12}, axisLine: {stroke: "#8884d8", strokeWidth: 2},
        tickLine: {stroke: "red"}, tickSize: 10, tickMargin: 6}),
      node("YAxis", {tick: {fontSize: 10, fill: "#82ca9d"}, axisLine: false, tickLine: {stroke: "#82ca9d", strokeWidth: 3}, tickSize: 4}),
      tooltip,
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off})],
  },
  {
    name: "AxisOptionsChart", kind: "LineChart", width: 500, height: 300, margin: {top: 20, right: 20, left: 20, bottom: 5}, data: pages,
    source: "XAxis on top with padding, a mirrored and reversed YAxis on the right with a unit, allowDecimals and tickCount",
    children: [grid,
      node("XAxis", {dataKey: "name", orientation: "top", padding: {left: 30, right: 30}, minTickGap: 20}),
      node("YAxis", {orientation: "right", mirror: true, reversed: true, unit: "kg", tickCount: 8, allowDecimals: false}),
      tooltip,
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off})],
  },
  {
    name: "ExplicitTicksChart", kind: "LineChart", width: 500, height: 300, margin: standardMargin, data: measures,
    source: "a numeric XAxis with an explicit ticks list and domain, a YAxis with an explicit ticks list",
    children: [grid,
      node("XAxis", {dataKey: "x", type: "number", domain: [0, 100], ticks: [0, 25, 50, 75, 100]}),
      node("YAxis", {ticks: [0, 30, 60, 90, 120]}),
      tooltip,
      node("Line", {dataKey: "y", stroke: "#8884d8", ...off})],
  },
  {
    name: "EquidistantTicksChart", kind: "LineChart", width: 500, height: 300, margin: standardMargin, data: days,
    source: "forty categories on an XAxis with interval equidistantPreserveStart, against preserveStartEnd on a second axis",
    children: [grid,
      node("XAxis", {dataKey: "day", interval: "equidistantPreserveStart"}),
      node("XAxis", {dataKey: "day", xAxisId: "top", orientation: "top", interval: "preserveStartEnd"}),
      node("YAxis"), tooltip,
      node("Line", {dataKey: "value", stroke: "#8884d8", dot: false, ...off}),
      node("Line", {dataKey: "value", xAxisId: "top", stroke: "#82ca9d", dot: false, ...off})],
  },
  {
    name: "MultiSeriesLineChart", kind: "LineChart", width: 500, height: 300, margin: standardMargin, data: null,
    source: "www/src/docs/exampleComponents/LineChart/LineChartHasMultiSeries.tsx (fixed values instead of random ones)",
    children: [grid,
      node("XAxis", {dataKey: "category", type: "category", allowDuplicatedCategory: false}),
      node("YAxis", {dataKey: "value"}), tooltip, legend,
      ...multiSeries.map((series) => node("Line", {dataKey: "value", data: series.data, name: series.name, key: series.name, ...off}))],
  },
  {
    name: "DomainOptionsChart", kind: "LineChart", width: 500, height: 300, margin: standardMargin, data: pages,
    source: "a category XAxis whose domain lists one more category than the data, a YAxis with includeHidden and a 'dataMin - 500' to 'dataMax + 500' domain over a hidden line",
    children: [grid,
      node("XAxis", {dataKey: "name", domain: ["Page A", "Page B", "Page C", "Page D", "Page E", "Page F", "Page G", "Page H"]}),
      node("YAxis", {includeHidden: true, domain: ["dataMin - 500", "dataMax + 500"]}),
      tooltip, legend,
      node("Line", {dataKey: "uv", stroke: "#82ca9d", ...off}),
      node("Line", {dataKey: "pv", stroke: "#8884d8", hide: true, ...off})],
  },
  {
    name: "GapPaddingChart", kind: "LineChart", width: 500, height: 300, margin: {top: 20, right: 30, left: 20, bottom: 5}, data: measures,
    source: "two numeric XAxis over the same values, padded with 'gap' (bottom) and 'no-gap' (top)",
    children: [grid,
      node("XAxis", {dataKey: "x", type: "number", padding: "gap"}),
      node("XAxis", {dataKey: "x", type: "number", xAxisId: "top", orientation: "top", padding: "no-gap"}),
      node("YAxis"), tooltip,
      node("Line", {dataKey: "y", stroke: "#8884d8", ...off}),
      node("Line", {dataKey: "z", xAxisId: "top", stroke: "#82ca9d", ...off})],
  },
  {
    name: "CurveTypesChart", kind: "ComposedChart", width: 500, height: 400, margin: standardMargin, data: pages,
    source: "every curve type Recharts names, on lines and on two areas",
    children: [node("XAxis", {dataKey: "name"}), node("YAxis"),
      ...curveTypes.map((type, index) => node("Line", {type, dataKey: index % 2 ? "uv" : "pv", name: type, stroke: palette[index % palette.length], dot: false, ...off})),
      node("Area", {type: "bumpX", dataKey: "amt", stroke: "#8884d8", fill: "#8884d8", ...off}),
      node("Area", {type: "natural", dataKey: "uv", stroke: "#82ca9d", fill: "#82ca9d", ...off})],
  },
  {
    name: "VerticalBumpChart", kind: "LineChart", layout: "vertical", width: 500, height: 400, margin: standardMargin, data: pages,
    source: "a vertical line chart whose 'bump' and 'monotone' lines are d3's bumpY and monotoneY",
    children: [grid, node("XAxis", {type: "number"}), node("YAxis", {dataKey: "name", type: "category"}), tooltip,
      node("Line", {type: "bump", dataKey: "pv", stroke: "#8884d8", ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off})],
  },
  {
    name: "LogScaleChart", kind: "LineChart", width: 500, height: 300, margin: standardMargin, data: growth,
    source: "a YAxis with scale log and domain ['auto', 'auto'] (d3's log ticks, no nice ticks) beside a YAxis with scale linear (d3's linear ticks over the unrounded domain)",
    children: [grid, node("XAxis", {dataKey: "name"}),
      node("YAxis", {scale: "log", domain: ["auto", "auto"]}),
      node("YAxis", {yAxisId: "linear", orientation: "right", scale: "linear"}),
      tooltip,
      node("Line", {dataKey: "users", stroke: "#8884d8", ...off}),
      node("Line", {dataKey: "users", yAxisId: "linear", stroke: "#82ca9d", ...off})],
  },
  {
    name: "PowerScalesChart", kind: "LineChart", width: 500, height: 300, margin: standardMargin, data: growth,
    source: "a YAxis with scale sqrt and one with scale pow (exponent 1), each with d3's ticks",
    children: [grid, node("XAxis", {dataKey: "name"}),
      node("YAxis", {scale: "sqrt"}),
      node("YAxis", {yAxisId: "pow", orientation: "right", scale: "pow", tickCount: 8}),
      tooltip,
      node("Line", {dataKey: "users", stroke: "#8884d8", ...off}),
      node("Line", {dataKey: "users", yAxisId: "pow", stroke: "#82ca9d", ...off})],
  },
  {
    name: "TimeScaleChart", kind: "LineChart", width: 500, height: 300, margin: {top: 20, right: 30, left: 20, bottom: 5}, data: timeline,
    source: "a numeric XAxis of timestamps with scale utc and an ISO date tick formatter, and a top XAxis with scale time and Recharts' default tick text (Date.toString, in UTC)",
    children: [grid,
      node("XAxis", {dataKey: "time", type: "number", scale: "utc", domain: ["auto", "auto"], tickFormatter: "isoDate"}),
      node("XAxis", {dataKey: "time", type: "number", scale: "time", domain: ["dataMin", "dataMax"], xAxisId: "top", orientation: "top", tickCount: 2}),
      node("YAxis"), tooltip,
      node("Line", {dataKey: "value", stroke: "#8884d8", ...off}),
      node("Line", {dataKey: "value", xAxisId: "top", stroke: "#82ca9d", ...off})],
  },
  {
    name: "VerticalTimeChart", kind: "LineChart", layout: "vertical", width: 500, height: 300, margin: {top: 5, right: 40, left: 20, bottom: 5}, data: timeline,
    source: "a vertical chart whose numeric XAxis has scale utc: its ticks are d3's dates, written with Date.toString (in UTC)",
    children: [grid,
      node("XAxis", {dataKey: "time", type: "number", scale: "utc", domain: ["dataMin", "dataMax"], tickCount: 3}),
      node("YAxis", {dataKey: "value", type: "category"}), tooltip,
      node("Line", {dataKey: "time", stroke: "#8884d8", ...off})],
  },
  {
    name: "WrappedTicksChart", kind: "BarChart", layout: "vertical", width: 500, height: 300, margin: standardMargin, data: regions,
    source: "a vertical bar chart whose category names are wider than the YAxis (60 px): Recharts' Text wraps them at the axis' width",
    children: [grid, node("XAxis", {type: "number"}), node("YAxis", {dataKey: "region", type: "category"}), tooltip,
      node("Bar", {dataKey: "sales", fill: "#8884d8", ...off})],
  },
  {
    name: "ZAxisOptionsChart", kind: "ScatterChart", width: 500, height: 400, margin: {top: 20, right: 20, bottom: 20, left: 20}, data: null,
    source: "ThreeDimScatterChart's schools with a z-axis of scale sqrt over the domain [0, 1000], and a category z-axis of sizes S, M and L",
    children: [node("CartesianGrid"), node("XAxis", {type: "number", dataKey: "x", name: "stature", unit: "cm"}),
      node("YAxis", {type: "number", dataKey: "y", name: "weight", unit: "kg"}),
      node("ZAxis", {type: "number", dataKey: "z", range: [60, 400], scale: "sqrt", domain: [0, 1000], name: "score"}),
      node("ZAxis", {zAxisId: "size", type: "category", dataKey: "size", range: [50, 500], name: "size"}),
      node("Tooltip", {cursor: {strokeDasharray: "3 3"}}),
      node("Scatter", {name: "A school", data: sizedA, fill: "#8884d8", ...off}),
      node("Scatter", {name: "B school", data: sizedB, zAxisId: "size", fill: "#82ca9d", shape: "square", ...off})],
  },
  {
    name: "PercentAreaChart", kind: "AreaChart", width: 500, height: 400, margin: {top: 10, right: 30, left: 0, bottom: 0}, data: pages,
    chartProps: {stackOffset: "expand"},
    source: "www/src/docs/exampleComponents/AreaChart/PercentAreaChart.tsx (stackOffset expand; without its percent tick formatter)",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip,
      node("Area", {type: "monotone", dataKey: "uv", stackId: "1", stroke: "#8884d8", fill: "#8884d8", ...off}),
      node("Area", {type: "monotone", dataKey: "pv", stackId: "1", stroke: "#82ca9d", fill: "#82ca9d", ...off}),
      node("Area", {type: "monotone", dataKey: "amt", stackId: "1", stroke: "#ffc658", fill: "#ffc658", ...off})],
  },
  {
    name: "SilhouetteAreaChart", kind: "AreaChart", width: 500, height: 400, margin: {top: 10, right: 30, left: 0, bottom: 0}, data: pages,
    chartProps: {stackOffset: "silhouette"},
    source: "StackedAreaChart with stackOffset silhouette (a streamgraph centered on zero)",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip,
      node("Area", {type: "monotone", dataKey: "uv", stackId: "1", stroke: "#8884d8", fill: "#8884d8", ...off}),
      node("Area", {type: "monotone", dataKey: "pv", stackId: "1", stroke: "#82ca9d", fill: "#82ca9d", ...off}),
      node("Area", {type: "monotone", dataKey: "amt", stackId: "1", stroke: "#ffc658", fill: "#ffc658", ...off})],
  },
  {
    name: "WiggleAreaChart", kind: "AreaChart", width: 500, height: 400, margin: {top: 10, right: 30, left: 0, bottom: 0}, data: pages,
    chartProps: {stackOffset: "wiggle"},
    source: "StackedAreaChart with stackOffset wiggle (d3's stackOffsetWiggle)",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip,
      node("Area", {type: "monotone", dataKey: "uv", stackId: "1", stroke: "#8884d8", fill: "#8884d8", ...off}),
      node("Area", {type: "monotone", dataKey: "pv", stackId: "1", stroke: "#82ca9d", fill: "#82ca9d", ...off}),
      node("Area", {type: "monotone", dataKey: "amt", stackId: "1", stroke: "#ffc658", fill: "#ffc658", ...off})],
  },
  {
    name: "SignStackedBarChart", kind: "BarChart", width: 500, height: 300, margin: standardMargin, data: signedPages,
    chartProps: {stackOffset: "sign"},
    source: "www/src/docs/exampleComponents/BarChart/BarChartStackedBySign.tsx",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip, legend,
      node("ReferenceLine", {y: 0, stroke: "#000"}),
      node("Bar", {dataKey: "pv", stackId: "stack", fill: "#8884d8", ...off}),
      node("Bar", {dataKey: "uv", stackId: "stack", fill: "#82ca9d", ...off})],
  },
  {
    name: "PositiveStackedBarChart", kind: "BarChart", width: 500, height: 300, margin: standardMargin, data: signedPages,
    chartProps: {stackOffset: "positive"},
    source: "BarChartStackedBySign with stackOffset positive (negative values stack as zero)",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip, legend,
      node("ReferenceLine", {y: 0, stroke: "#000"}),
      node("Bar", {dataKey: "pv", stackId: "stack", fill: "#8884d8", ...off}),
      node("Bar", {dataKey: "uv", stackId: "stack", fill: "#82ca9d", ...off})],
  },
  {
    name: "ReverseStackBarChart", kind: "BarChart", width: 500, height: 300, margin: standardMargin, data: pages,
    chartProps: {reverseStackOrder: true, barSize: "8%", barGap: "20%"},
    source: "MixBarChart with reverseStackOrder (the stack, the bar groups, the legend and the tooltip in reverse order), a percent barSize and a percent barGap",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip, legend,
      node("Bar", {dataKey: "pv", stackId: "a", fill: "#8884d8", ...off}),
      node("Bar", {dataKey: "amt", stackId: "a", fill: "#82ca9d", ...off}),
      node("Bar", {dataKey: "uv", fill: "#ffc658", ...off})],
  },
  {
    name: "BarGapsChart", kind: "BarChart", width: 500, height: 300, margin: standardMargin, data: pages,
    chartProps: {barCategoryGap: 12, barGap: 1, maxBarSize: 18},
    source: "SimpleBarChart with a numeric barCategoryGap and barGap and a maxBarSize",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip, legend,
      node("Bar", {dataKey: "pv", fill: "#8884d8", ...off}),
      node("Bar", {dataKey: "uv", fill: "#82ca9d", ...off})],
  },
  {
    name: "OwnBarSizesChart", kind: "BarChart", width: 500, height: 300, margin: standardMargin, data: pages,
    source: "two bars with their own barSize, a percent of the axis' length and a number",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip, legend,
      node("Bar", {dataKey: "pv", barSize: "4%", fill: "#8884d8", ...off}),
      node("Bar", {dataKey: "uv", barSize: 10, fill: "#82ca9d", ...off})],
  },
  {
    name: "RadarAxesChart", kind: "RadarChart", width: 500, height: 400, margin: null, data: subjects,
    chartProps: {cx: "50%", cy: "50%", outerRadius: "70%"},
    source: "SpecifiedDomainRadarChart with angle axis props (tick, axisLine, tickSize, orientation inner) and a radius axis at 60 degrees, on the left, reversed, with an explicit ticks list and its label outside",
    children: [node("PolarGrid"),
      node("PolarAngleAxis", {dataKey: "subject", tick: {fill: "#8884d8", fontSize: 12}, axisLine: {stroke: "#999"}, tickSize: 12, orientation: "inner"}),
      node("PolarRadiusAxis", {angle: 60, orientation: "left", reversed: true, domain: [0, 150], ticks: [0, 50, 100, 150], tick: {fontSize: 10},
        label: {value: "score", position: "outside", offset: 10, fill: "#333"}}),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, ...off})],
  },
  {
    name: "RadarCategoryAngleChart", kind: "RadarChart", width: 500, height: 400, margin: null, data: [...subjects, {subject: "Math", A: 70, B: 60, fullMark: 150}],
    chartProps: {cx: "50%", cy: "50%", outerRadius: "70%"},
    source: "a radar whose angle axis drops a duplicated category, shows an explicit ticks list, runs reversed, and whose radius axis has a sqrt scale, a tick count of 3 and its label at insideEnd",
    children: [node("PolarGrid"),
      node("PolarAngleAxis", {dataKey: "subject", allowDuplicatedCategory: false, reversed: true, ticks: ["Math", "English", "Physics"]}),
      node("PolarRadiusAxis", {scale: "sqrt", tickCount: 3, label: {value: "score", position: "insideEnd"}}),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, ...off})],
  },
  {
    name: "RadialAngleAxisChart", kind: "RadialBarChart", width: 500, height: 300, margin: null, data: ages,
    chartProps: {cx: "50%", cy: "50%", innerRadius: "10%", outerRadius: "80%", barSize: 10},
    source: "SimpleRadialBarChart with a numeric angle axis (domain [0, 40], 5 ticks, no decimals, tick line props) and a reversed category radius axis with its label at centerTop",
    children: [
      node("PolarAngleAxis", {type: "number", domain: [0, 40], tickCount: 5, allowDecimals: false, tickLine: {stroke: "#c00"}, tick: {fontSize: 11}}),
      node("PolarRadiusAxis", {type: "category", dataKey: "name", reversed: true, label: {value: "ages", position: "centerTop"}}),
      node("RadialBar", {background: true, dataKey: "uv", ...off})],
  },
];
