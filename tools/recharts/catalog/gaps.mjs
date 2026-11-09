import {node, off, pages} from "./common.mjs";

const grid = node("CartesianGrid", {strokeDasharray: "3 3"});
const tooltip = node("Tooltip");
const legend = node("Legend");
const margin = {top: 5, right: 30, left: 20, bottom: 5};
const source = (what) => `Pictor's gaps catalog: ${what}`;

const gappedPages = pages.map((page, index) => ({
  ...page,
  uv: index === 2 ? null : page.uv,
  pv: index === 4 ? null : page.pv,
  amt: index === 3 ? null : page.amt,
}));

const smallPages = [
  {name: "Page A", uv: 4000, pv: 2400, amt: 2400, small: 0},
  {name: "Page B", uv: 3000, pv: 1398, amt: 2210, small: 40},
  {name: "Page C", uv: 2000, pv: 9800, amt: 2290, small: 2500},
  {name: "Page D", uv: 2780, pv: 3908, amt: 2000, small: 15},
  {name: "Page E", uv: 1890, pv: 4800, amt: 2181, small: 1800},
  {name: "Page F", uv: 2390, pv: 3800, amt: 2500, small: 900},
  {name: "Page G", uv: 3490, pv: 4300, amt: 2100, small: 3000},
];

const points = [
  {x: 100, y: 200, z: 200},
  {x: 120, y: 100, z: 260},
  {x: 170, y: 300, z: 400},
  {x: 140, y: 250, z: 280},
  {x: 150, y: 400, z: 500},
  {x: 110, y: 280, z: 200},
];

const otherPoints = [
  {x: 200, y: 260, z: 240},
  {x: 240, y: 290, z: 220},
  {x: 190, y: 290, z: 250},
  {x: 198, y: 250, z: 210},
  {x: 180, y: 280, z: 260},
  {x: 210, y: 220, z: 230},
];

const scaled = pages.map((page, index) => ({...page, px: [40, 180, 90, 150, 60, 200, 120][index]}));

const sizedPoints = points.map((point, index) => ({...point, size: ["S", "M", "L"][index % 3]}));

const zeroPoints = otherPoints.map((point, index) => ({...point, z: index === 0 ? 0 : point.z}));

const errorItems = pages.slice(0, 5).map((page, index) => ({...page, pvError: [300, 200, 900, 400, 500][index], uvError: [400, 250, 300, 280, 190][index]}));

const subjects = [
  {subject: "Math", A: 120, B: 110, fullMark: 150},
  {subject: "Chinese", A: 98, B: 130, fullMark: 150},
  {subject: "English", A: 86, B: 130, fullMark: 150},
  {subject: "Geography", A: 99, B: 100, fullMark: 150},
  {subject: "Physics", A: 85, B: 90, fullMark: 150},
  {subject: "History", A: 65, B: 85, fullMark: 150},
];

const scaledSubjects = subjects.map((subject, index) => ({...subject, code: subject.subject.slice(0, 2).toUpperCase(), hours: [12, 30, 22, 8, 17, 26][index]}));

const ages = [
  {age: "18-24", uv: 31.47, pv: 2400, small: 12, fill: "#8884d8"},
  {age: "25-29", uv: 26.69, pv: 4567, small: 20, fill: "#83a6ed"},
  {age: "30-34", uv: 15.69, pv: 1398, small: 9, fill: "#8dd1e1"},
  {age: "35-39", uv: 8.22, pv: 9800, small: 14, fill: "#82ca9d"},
  {age: "40-49", uv: 0.4, pv: 3908, small: 6, fill: "#a4de6c"},
  {age: "50+", uv: 2.63, pv: 4800, small: 3, fill: "#d0ed57"},
];

const signedAges = ages.map((age, index) => ({...age, uv: index % 2 ? -age.uv : age.uv, small: [5, -8, 12, -3, 6, 9][index]}));

const shares = [
  {label: "Desktop", amount: 400},
  {label: "Mobile", amount: 300},
  {label: "Tablet", amount: 300},
  {label: "Other", amount: 200},
];

const stages = [
  {stage: "Visits", value: 100, fill: "#8884d8"},
  {stage: "Clicks", value: 80, fill: "#83a6ed"},
  {stage: "Carts", value: 50, fill: "#8dd1e1"},
  {stage: "Orders", value: 40, fill: "#82ca9d"},
  {stage: "Repeats", value: 26, fill: "#a4de6c"},
];

export const examples = [
  {
    name: "GapsTitledComposedChart", kind: "ComposedChart", width: 500, height: 300, margin, data: gappedPages,
    chartProps: {title: "Visits per page", desc: "Views, visitors and amounts with gaps"},
    source: source("a chart title and description, null values bridged by connectNulls on a line and an area, units in the tooltip, a legend of a fixed height"),
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip, node("Legend", {height: 60}),
      node("Area", {type: "monotone", dataKey: "amt", fill: "#ffc658", stroke: "#ffc658", connectNulls: true, unit: " $", ...off}),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", connectNulls: true, unit: " views", ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", unit: " visitors", ...off})],
    interactions: [[{hover: [250, 5]}], [{hover: [330, 5]}]],
  },
  {
    name: "GapsBarOptionsChart", kind: "BarChart", width: 500, height: 300, margin, data: smallPages,
    source: source("bars on named axes with a name, a unit, a stroke width, a fill opacity, a radius, a largest size and a smallest size, a hidden bar, a label list formatter, references on the named axes"),
    children: [grid, node("XAxis", {dataKey: "name", xAxisId: "pages"}),
      node("YAxis", {yAxisId: "views"}), node("YAxis", {yAxisId: "small", orientation: "right"}), tooltip, legend,
      node("ReferenceArea", {xAxisId: "pages", yAxisId: "small", x1: "Page B", x2: "Page C", y1: 0, y2: 1000, fill: "#ff7300", fillOpacity: 0.2}),
      node("ReferenceLine", {xAxisId: "pages", yAxisId: "small", y: 2000, stroke: "red", strokeDasharray: "4 4"}),
      node("ReferenceDot", {xAxisId: "pages", yAxisId: "views", x: "Page E", y: 8000, r: 6, fill: "#8884d8", stroke: "none"}),
      node("Bar", {xAxisId: "pages", yAxisId: "views", dataKey: "pv", name: "Page views", unit: " views", fill: "#8884d8", stroke: "#333",
        strokeWidth: 2, fillOpacity: 0.6, radius: [8, 8, 0, 0], maxBarSize: 18, ...off}),
      node("Bar", {xAxisId: "pages", yAxisId: "small", dataKey: "small", name: "Small", fill: "#82ca9d", minPointSize: 12, ...off},
        [node("LabelList", {dataKey: "small", position: "top", formatter: {$format: "{0} u"}})]),
      node("Bar", {xAxisId: "pages", yAxisId: "views", dataKey: "uv", fill: "#ffc658", hide: true, ...off})],
    interactions: [[{hover: [212, 5]}]],
  },
  {
    name: "GapsAxisHideStrokeChart", kind: "LineChart", width: 500, height: 300, margin, data: pages,
    source: source("an axis stroke, a hidden y-axis, a stroked right y-axis and a top x-axis without ticks"),
    children: [grid, node("XAxis", {dataKey: "name", stroke: "#e33"}), node("XAxis", {dataKey: "name", xAxisId: "top", orientation: "top", tick: false}),
      node("YAxis", {yAxisId: "hidden", hide: true}), node("YAxis", {yAxisId: "right", orientation: "right", stroke: "#3a3"}),
      node("Line", {yAxisId: "hidden", dataKey: "pv", stroke: "#8884d8", ...off}),
      node("Line", {yAxisId: "right", xAxisId: "top", dataKey: "uv", stroke: "#82ca9d", ...off})],
  },
  {
    name: "GapsScatterAxesChart", kind: "ScatterChart", width: 500, height: 400, margin: {top: 20, right: 20, bottom: 20, left: 20}, data: null,
    source: source("scatters on their own x- and y-axes, legend types and a hidden scatter"),
    children: [grid,
      node("XAxis", {type: "number", dataKey: "x", xAxisId: "a", name: "stature", unit: "cm"}),
      node("XAxis", {type: "number", dataKey: "x", xAxisId: "b", orientation: "top"}),
      node("YAxis", {type: "number", dataKey: "y", yAxisId: "l", name: "weight", unit: "kg"}),
      node("YAxis", {type: "number", dataKey: "y", yAxisId: "r", orientation: "right"}),
      tooltip, legend,
      node("Scatter", {name: "A school", data: points, xAxisId: "a", yAxisId: "l", fill: "#8884d8", legendType: "star", ...off}),
      node("Scatter", {name: "B school", data: otherPoints, xAxisId: "b", yAxisId: "r", fill: "#82ca9d", legendType: "triangle", ...off}),
      node("Scatter", {name: "C school", data: points, xAxisId: "a", yAxisId: "l", fill: "#ff7300", hide: true, ...off})],
  },
  {
    name: "GapsRadialGaugeChart", kind: "RadialBarChart", width: 500, height: 300, margin, data: ages,
    chartProps: {cx: "50%", cy: "80%", innerRadius: "20%", outerRadius: "100%", startAngle: 180, endAngle: 0, barGap: 2, barCategoryGap: "30%"},
    source: source("a half-circle radial bar chart (startAngle and endAngle on the chart) with its bar gaps, a radial bar's name, barSize, minPointSize, unit and legend type (its legend reads each item's name), a hidden radial bar"),
    children: [
      node("RadialBar", {dataKey: "uv", name: "Share", barSize: 10, minPointSize: 8, unit: "%", legendType: "circle", background: true, ...off}),
      node("RadialBar", {dataKey: "pv", hide: true, ...off}),
      node("Legend"), tooltip],
    interactions: [[{hover: [150, 239.9]}]],
  },
  {
    name: "GapsPieOptionsChart", kind: "PieChart", width: 400, height: 400, margin: null, data: [],
    chartProps: {title: "Devices", desc: "Visits by device"},
    source: source("a pie's nameKey, corner radius, stroke and legend type, and a hidden pie"),
    children: [
      node("Pie", {data: shares, dataKey: "amount", nameKey: "label", cx: "50%", cy: "50%", innerRadius: 60, outerRadius: 120, paddingAngle: 4,
        cornerRadius: 8, stroke: "#222", fill: "#8884d8", legendType: "diamond", ...off}),
      node("Pie", {data: shares, dataKey: "amount", nameKey: "label", outerRadius: 40, fill: "#82ca9d", hide: true, ...off}),
      node("Legend"), tooltip],
    interactions: [[{hover: [290, 140]}]],
  },
  {
    name: "GapsRadarOptionsChart", kind: "RadarChart", width: 500, height: 400, margin, data: subjects,
    chartProps: {cx: "50%", cy: "50%", outerRadius: "70%", startAngle: 0, endAngle: 360},
    source: source("a radar's stroke width, unit and legend type, a hidden radar, a circular angle axis line with a tick format, a stroked radius axis with a format, no decimals and an overflowing domain"),
    children: [node("PolarGrid"),
      node("PolarAngleAxis", {dataKey: "subject", axisLineType: "circle", tickFormatter: "{0}:"}),
      node("PolarRadiusAxis", {angle: 30, stroke: "#e33", domain: [0, 100], allowDataOverflow: true, allowDecimals: false, tickCount: 4, tickFormatter: "{0} pts"}),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, strokeWidth: 3, unit: " pts", legendType: "diamond", ...off}),
      node("Radar", {name: "Lily", dataKey: "B", stroke: "#82ca9d", fill: "#82ca9d", fillOpacity: 0.6, hide: true, ...off}),
      legend, tooltip],
    interactions: [[{hover: [282.64, 143.47]}]],
  },
  {
    name: "GapsHiddenPolarAxesChart", kind: "RadarChart", width: 500, height: 400, margin, data: subjects,
    source: source("a hidden angle axis and a hidden radius axis"),
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "subject", hide: true}), node("PolarRadiusAxis", {hide: true}),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, ...off})],
  },
  {
    name: "GapsPolarAxisScalesChart", kind: "RadialBarChart", width: 500, height: 400, margin, data: ages,
    chartProps: {innerRadius: "15%", outerRadius: "85%"},
    source: source("a numeric angle axis with a linear scale, an overflowing domain and a tick format; a category radius axis without its line, with a stroke and a tick format"),
    children: [
      node("PolarAngleAxis", {type: "number", domain: [0, 20], allowDataOverflow: true, scale: "linear", tickFormatter: "{0}%"}),
      node("PolarRadiusAxis", {type: "category", dataKey: "age", axisLine: false, stroke: "#3a3", tickFormatter: "<{0}>"}),
      node("RadialBar", {dataKey: "uv", background: true, ...off})],
  },
  {
    name: "GapsFunnelOptionsChart", kind: "FunnelChart", width: 600, height: 400, margin: {right: 30}, data: null,
    source: source("a funnel's nameKey, fill, stroke, legend type and last shape, its cells, and a hidden funnel"),
    children: [tooltip, node("Legend"),
      node("Funnel", {dataKey: "value", nameKey: "stage", data: stages, fill: "#8884d8", stroke: "#333", legendType: "circle", lastShapeType: "rectangle", ...off},
        [node("Cell", {fill: "#ff7300"}), node("Cell", {fill: "#413ea0"}), node("Cell", {stroke: "#e33"}),
          node("LabelList", {position: "right", fill: "#000", stroke: "none", dataKey: "stage"})]),
      node("Funnel", {dataKey: "value", data: stages, hide: true, ...off})],
    interactions: [[{hover: [280, 120]}]],
  },
  {
    name: "GapsStackedLabelsChart", kind: "ComposedChart", width: 500, height: 300, margin, data: pages,
    source: source("label lists of stacked bars and stacked areas, which show the stack's top (LabelList's last(value))"),
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"),
      node("Area", {type: "monotone", dataKey: "amt", stackId: "a", fill: "#ffc658", stroke: "#ffc658", label: true, ...off}),
      node("Area", {type: "monotone", dataKey: "uv", stackId: "a", fill: "#82ca9d", stroke: "#82ca9d", ...off}, [node("LabelList", {position: "top", fill: "#2a2"})]),
      node("Bar", {dataKey: "pv", stackId: "b", fill: "#8884d8", ...off}, [node("LabelList", {position: "insideTop", fill: "#fff", fontSize: 10})]),
      node("Bar", {dataKey: "uv", stackId: "b", fill: "#413ea0", ...off}, [node("LabelList", {position: "top", fontSize: 10}), node("LabelList", {dataKey: "name", position: "center", fill: "#fff", fontSize: 8})])],
  },
  {
    name: "GapsGridValuesChart", kind: "LineChart", width: 500, height: 300, margin, data: pages,
    source: source("CartesianGrid horizontalValues and verticalValues, kept as their axes keep ticks"),
    children: [node("CartesianGrid", {stroke: "#ccc", horizontalValues: [1000, 2500, 2600, 7000, 12000], verticalValues: ["Page B", "Page C", "Page F", "Page Z"]}),
      node("XAxis", {dataKey: "name"}), node("YAxis"),
      node("Line", {dataKey: "pv", stroke: "#8884d8", ...off})],
  },
  {
    name: "GapsTooltipTypePieChart", kind: "PieChart", width: 400, height: 400, margin: null, data: [],
    source: source("tooltipType none on a pie: its sectors' tooltips list nothing"),
    children: [
      node("Pie", {data: shares, dataKey: "amount", nameKey: "label", outerRadius: 80, fill: "#8884d8", tooltipType: "none", ...off}),
      node("Pie", {data: shares, dataKey: "amount", nameKey: "label", innerRadius: 100, outerRadius: 130, fill: "#82ca9d", ...off}),
      tooltip],
    interactions: [[{hover: [230, 160]}], [{hover: [310, 140]}]],
  },
  {
    name: "GapsTooltipTypeRadarChart", kind: "RadarChart", width: 500, height: 400, margin, data: subjects,
    source: source("tooltipType none on a radar: the tooltip lists the other radar only"),
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "subject"}),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, tooltipType: "none", ...off}),
      node("Radar", {name: "Lily", dataKey: "B", stroke: "#82ca9d", fill: "#82ca9d", fillOpacity: 0.6, ...off}),
      tooltip],
    interactions: [[{hover: [250, 122]}]],
  },
  {
    name: "GapsTooltipTypeRadialChart", kind: "RadialBarChart", width: 500, height: 300, margin, data: ages,
    chartProps: {innerRadius: "20%", outerRadius: "90%"},
    source: source("tooltipType none on a radial bar: the tooltip lists the other radial bar only"),
    children: [
      node("RadialBar", {dataKey: "uv", fill: "#8884d8", tooltipType: "none", ...off}),
      node("RadialBar", {dataKey: "pv", fill: "#82ca9d", ...off}),
      tooltip],
    interactions: [[{hover: [330, 150]}]],
  },
  {
    name: "GapsTooltipTypeFunnelChart", kind: "FunnelChart", width: 600, height: 400, margin: {right: 30}, data: null,
    source: source("tooltipType none on a funnel: its tooltip lists nothing"),
    children: [tooltip, node("Funnel", {dataKey: "value", nameKey: "stage", data: stages, tooltipType: "none", ...off})],
    interactions: [[{hover: [280, 120]}]],
  },
  {
    name: "GapsTooltipTypeScatterChart", kind: "ScatterChart", width: 500, height: 400, margin: {top: 20, right: 20, bottom: 20, left: 20}, data: null,
    source: source("tooltipType none on a scatter: its points' tooltips list nothing"),
    children: [grid, node("XAxis", {type: "number", dataKey: "x", name: "stature", unit: "cm"}), node("YAxis", {type: "number", dataKey: "y", name: "weight", unit: "kg"}),
      tooltip, node("Scatter", {name: "A school", data: points, fill: "#8884d8", tooltipType: "none", ...off})],
    interactions: [[{hover: [302, 185]}]],
  },
  {
    name: "GapsBrushScatterChart", kind: "ScatterChart", width: 500, height: 400, margin: {top: 20, right: 20, bottom: 20, left: 20}, data: points,
    source: source("a Brush in a scatter chart whose scatter reads the chart's data: the window slices the points"),
    children: [grid, node("XAxis", {type: "number", dataKey: "x"}), node("YAxis", {type: "number", dataKey: "y"}),
      node("Scatter", {fill: "#8884d8", ...off}), node("Brush", {startIndex: 1, endIndex: 4, height: 30, stroke: "#8884d8", ariaLabel: "points shown"})],
  },
  {
    name: "GapsDiscreteScalesChart", kind: "LineChart", width: 600, height: 300, margin, data: scaled,
    source: source("value axes with d3's quantile, quantize and threshold scales (Recharts' parseScale: scale + the name)"),
    children: [grid, node("XAxis", {dataKey: "name"}),
      node("YAxis", {yAxisId: "quantile", scale: "quantile", type: "number"}),
      node("YAxis", {yAxisId: "quantize", scale: "quantize", type: "number", orientation: "right"}),
      node("YAxis", {yAxisId: "threshold", scale: "threshold", type: "number", orientation: "right", domain: [1000, 5000]}),
      node("Line", {yAxisId: "quantile", dataKey: "pv", stroke: "#8884d8", ...off}),
      node("Line", {yAxisId: "quantize", dataKey: "uv", stroke: "#82ca9d", ...off}),
      node("Line", {yAxisId: "threshold", dataKey: "amt", stroke: "#ff7300", ...off})],
  },
  {
    name: "GapsOtherScalesChart", kind: "LineChart", width: 600, height: 300, margin, data: scaled,
    source: source("value axes with d3's identity and sequential scales"),
    children: [grid, node("XAxis", {dataKey: "name"}),
      node("YAxis", {yAxisId: "identity", scale: "identity", type: "number", dataKey: "px"}),
      node("YAxis", {yAxisId: "sequential", scale: "sequential", type: "number", orientation: "right"}),
      node("Line", {yAxisId: "identity", dataKey: "px", stroke: "#8884d8", ...off}),
      node("Line", {yAxisId: "sequential", dataKey: "uv", stroke: "#82ca9d", ...off}),
      node("Line", {yAxisId: "sequential", dataKey: "amt", stroke: "#ff7300", ...off})],
  },
  {
    name: "GapsZTooltipChart", kind: "ScatterChart", width: 500, height: 400, margin: {top: 20, right: 20, bottom: 20, left: 20}, data: null,
    source: source("tooltips of points on a category z-axis (its text value) and on a numeric z-axis with a zero value"),
    children: [grid, node("XAxis", {type: "number", dataKey: "x", name: "stature", unit: "cm"}), node("YAxis", {type: "number", dataKey: "y", name: "weight", unit: "kg"}),
      node("ZAxis", {zAxisId: "size", type: "category", dataKey: "size", range: [50, 500], name: "size"}),
      node("ZAxis", {type: "number", dataKey: "z", range: [60, 400], name: "score", unit: "pt"}),
      tooltip,
      node("Scatter", {name: "A school", data: sizedPoints, zAxisId: "size", fill: "#8884d8", ...off}),
      node("Scatter", {name: "B school", data: zeroPoints, fill: "#82ca9d", ...off})],
    interactions: [[{hover: [246.67, 185]}], [{hover: [415, 132]}]],
  },
  {
    name: "GapsCentricRadialChart", kind: "RadialBarChart", layout: "centric", width: 500, height: 400, margin, data: ages,
    chartProps: {innerRadius: "10%", outerRadius: "80%"},
    source: source("a radial bar chart with layout centric: categories around the angle axis, values along the radius axis, two radial bars side by side"),
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "age"}), node("PolarRadiusAxis", {angle: 90}),
      node("RadialBar", {dataKey: "uv", name: "share", fill: "#8884d8", minPointSize: 4, ...off}),
      node("RadialBar", {dataKey: "small", name: "small", fill: "#82ca9d", ...off}),
      legend, tooltip],
    interactions: [[{hover: [287.3, 135.4]}]],
  },
  {
    name: "GapsExpandRadialChart", kind: "RadialBarChart", width: 500, height: 300, margin, data: ages,
    chartProps: {innerRadius: "20%", outerRadius: "90%", stackOffset: "expand"},
    source: source("stacked radial bars with stackOffset expand"),
    children: [node("RadialBar", {dataKey: "uv", stackId: "a", fill: "#8884d8", ...off}), node("RadialBar", {dataKey: "small", stackId: "a", fill: "#82ca9d", ...off})],
  },
  {
    name: "GapsSignRadialChart", kind: "RadialBarChart", width: 500, height: 300, margin, data: signedAges,
    chartProps: {innerRadius: "20%", outerRadius: "90%", stackOffset: "sign"},
    source: source("stacked radial bars with stackOffset sign over negative values"),
    children: [node("PolarAngleAxis", {type: "number"}), node("RadialBar", {dataKey: "uv", stackId: "a", fill: "#8884d8", ...off}), node("RadialBar", {dataKey: "small", stackId: "a", fill: "#82ca9d", ...off})],
  },
  {
    name: "GapsLabelsChart", kind: "LineChart", width: 500, height: 320, margin: {top: 20, right: 40, bottom: 30, left: 30}, data: pages,
    source: source("several labels per element: an axis' label prop then its Label children, {x, y} positions, formatters on a text and a number, labels on references"),
    children: [grid,
      node("XAxis", {dataKey: "name", label: {value: "pages", position: "insideBottomRight", offset: 0}},
        [node("Label", {value: "left", position: "insideBottomLeft", offset: 0, fill: "#c00"}), node("Label", {value: "at", position: {x: "50%", y: 28}, fontSize: 11})]),
      node("YAxis", {label: {value: "views", angle: -90, position: "insideLeft"}},
        [node("Label", {value: "unit", position: "insideTopLeft", formatter: {$format: "[{0}]"}}), node("Label", {value: 42, position: {x: 10, y: 0}, formatter: {$format: "{0} kg"}})]),
      node("ReferenceLine", {y: 5000, stroke: "red", label: "max"},
        [node("Label", {value: "start", position: "insideTopLeft"}), node("Label", {value: "end", position: "insideBottomRight", fill: "#00f"})]),
      node("ReferenceArea", {x1: "Page B", x2: "Page D", fill: "#82ca9d", fillOpacity: 0.2},
        [node("Label", {value: "zone", position: "insideTop"}), node("Label", {value: "below", position: "bottom", angle: 20})]),
      node("ReferenceDot", {x: "Page F", y: 3800, r: 8, fill: "#ff7300", stroke: "none"},
        [node("Label", {value: "dot", position: "top"}), node("Label", {value: 3800, position: "right", formatter: {$format: "~{0}"}})]),
      node("Line", {dataKey: "pv", stroke: "#8884d8", ...off})],
  },
  {
    name: "GapsRadiusLabelsChart", kind: "RadarChart", width: 500, height: 400, margin, data: subjects,
    source: source("a polar radius axis with its label prop and two Label children"),
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "subject"}),
      node("PolarRadiusAxis", {angle: 30, label: {value: "score", position: "insideEnd"}},
        [node("Label", {value: "low", position: "insideStart"}), node("Label", {value: 150, position: "end", formatter: {$format: "max {0}"}})]),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, ...off})],
  },
  {
    name: "GapsResponsiveContainerChart", kind: "LineChart", width: 500, height: 250, margin, data: pages,
    container: {width: 500, aspect: 2, id: "sales", className: "chart-box", style: {background: "#fafafa", padding: "4px"}},
    source: source("ResponsiveContainer's id, className and style on the container"),
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off})],
  },
  {
    name: "GapsBarBackgroundChart", kind: "BarChart", width: 500, height: 300, margin, data: pages,
    source: source("bar backgrounds: true on a rounded bar (the background takes none of the bar's props) and an object with fill, stroke, stroke width, fill opacity and radius"),
    children: [node("XAxis", {dataKey: "name"}), node("YAxis"),
      node("Bar", {dataKey: "pv", fill: "#8884d8", radius: [10, 10, 0, 0], background: true, ...off}),
      node("Bar", {dataKey: "uv", fill: "#82ca9d", background: {fill: "#f5f5f5", stroke: "#999", strokeWidth: 2, fillOpacity: 0.8, radius: 6}, ...off})],
  },
  {
    name: "GapsOwnDataErrorBarChart", kind: "BarChart", width: 500, height: 300, margin, data: pages,
    source: source("error bars on bars that carry their own data over a category axis, in a chart with data of its own (without it Recharts draws no bar)"),
    children: [grid, node("XAxis", {dataKey: "name", allowDuplicatedCategory: false}), node("YAxis"),
      node("Bar", {dataKey: "pv", data: errorItems, fill: "#8884d8", ...off}, [node("ErrorBar", {dataKey: "pvError", width: 4, strokeWidth: 2, stroke: "green", direction: "y"})]),
      node("Bar", {dataKey: "uv", data: errorItems, fill: "#82ca9d", ...off}, [node("ErrorBar", {dataKey: "uvError", width: 4, stroke: "#333", direction: "y"})])],
  },
  {
    name: "GapsRadarTwoScalesChart", kind: "RadarChart", width: 500, height: 400, margin, data: scaledSubjects,
    source: source("two radius axes by radiusAxisId, each over its own radar's values, and a second angle axis by angleAxisId"),
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "subject"}),
      node("PolarAngleAxis", {angleAxisId: "codes", dataKey: "code", tickSize: 24, stroke: "#999"}),
      node("PolarRadiusAxis", {radiusAxisId: "scores", angle: 30}),
      node("PolarRadiusAxis", {radiusAxisId: "hours", angle: 120, stroke: "#82ca9d"}),
      node("Radar", {name: "score", dataKey: "A", radiusAxisId: "scores", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.5, ...off}),
      node("Radar", {name: "hours", dataKey: "hours", radiusAxisId: "hours", angleAxisId: "codes", stroke: "#82ca9d", fill: "#82ca9d", fillOpacity: 0.5, ...off})],
  },
  {
    name: "GapsRadialTwoAnglesChart", kind: "RadialBarChart", width: 500, height: 400, margin, data: ages,
    chartProps: {innerRadius: "15%", outerRadius: "85%"},
    source: source("two numeric angle axes by angleAxisId, each over its own radial bar's values"),
    children: [node("PolarAngleAxis", {angleAxisId: "share", type: "number", tickCount: 5}),
      node("PolarAngleAxis", {angleAxisId: "people", type: "number", tickCount: 3, stroke: "#82ca9d"}),
      node("RadialBar", {dataKey: "uv", angleAxisId: "share", fill: "#8884d8", ...off}),
      node("RadialBar", {dataKey: "pv", angleAxisId: "people", fill: "#82ca9d", ...off})],
  },
  {
    name: "GapsImplicitPolarAxesChart", kind: "RadarChart", width: 500, height: 400, margin, data: scaledSubjects,
    source: source("radars on radius axis ids no axis declares: each gets an implicit axis over its own values"),
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "subject"}),
      node("Radar", {name: "score", dataKey: "A", radiusAxisId: "p", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.5, ...off}),
      node("Radar", {name: "hours", dataKey: "hours", radiusAxisId: "q", stroke: "#82ca9d", fill: "#82ca9d", fillOpacity: 0.5, ...off})],
  },
  {
    name: "GapsCompactChart", kind: "LineChart", width: 600, height: 160, margin, data: pages,
    chartProps: {compact: true},
    source: source("a compact chart: the surface alone, without the wrapper, the legend or the tooltip box"),
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip, legend,
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off})],
  },
  {
    name: "GapsAreaLegendBrushChart", kind: "AreaChart", width: 500, height: 320, margin, data: pages,
    source: source("a stacked area chart with a legend and a brush window"),
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip, legend,
      node("Brush", {dataKey: "name", height: 24, stroke: "#8884d8", startIndex: 1, endIndex: 5}),
      node("Area", {type: "monotone", dataKey: "uv", stackId: "1", stroke: "#8884d8", fill: "#8884d8", ...off}),
      node("Area", {type: "monotone", dataKey: "pv", stackId: "1", stroke: "#82ca9d", fill: "#82ca9d", legendType: "circle", ...off})],
  },
  {
    name: "GapsComposedBrushChart", kind: "ComposedChart", width: 500, height: 320, margin, data: pages,
    source: source("a composed chart (bars, a line and an area) with a brush window"),
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip, legend,
      node("Area", {type: "monotone", dataKey: "amt", fill: "#ffc658", stroke: "#ffc658", ...off}),
      node("Bar", {dataKey: "pv", fill: "#8884d8", barSize: 16, ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#ff7300", ...off}),
      node("Brush", {dataKey: "name", height: 24, stroke: "#8884d8", startIndex: 2, endIndex: 6})],
  },
];
