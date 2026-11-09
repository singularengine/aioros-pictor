import {node, off, pages} from "./common.mjs";

const grid = node("CartesianGrid", {strokeDasharray: "3 3"});
const xName = node("XAxis", {dataKey: "name"});
const yAxis = node("YAxis");
const legend = node("Legend");
const margin = {top: 5, right: 30, left: 20, bottom: 5};
const gaps = pages.map((page, index) => index === 2 ? {...page, pv: null} : index === 4 ? {name: page.name, uv: page.uv, amt: page.amt} : page);
const fmt = (format) => ({$format: format});
const source = (what) => `Recharts' Tooltip API (recharts.org/en-US/api/Tooltip) on SimpleLineChart's or SimpleBarChart's data: ${what}`;

const lines = (tooltip, extra = {}) => [grid, xName, yAxis, node("Tooltip", tooltip), legend,
  node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...extra, ...off}),
  node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off})];

const bars = (tooltip) => [grid, xName, yAxis, node("Tooltip", tooltip), legend,
  node("Bar", {dataKey: "pv", fill: "#8884d8", ...off}),
  node("Bar", {dataKey: "uv", fill: "#82ca9d", ...off})];

const lineChart = (name, what, tooltip, interactions, data = pages, extra = {}) =>
  ({name, kind: "LineChart", width: 500, height: 300, margin, data, source: source(what), children: lines(tooltip, extra), interactions});

const barChart = (name, what, tooltip, interactions) =>
  ({name, kind: "BarChart", width: 500, height: 300, margin, data: pages, source: source(what), children: bars(tooltip), interactions});

export const examples = [
  barChart("TooltipItemBarChart", "shared={false}, a tooltip per bar", {shared: false}, [[{hover: [205, 200]}], [{hover: [398, 230]}]]),
  lineChart("TooltipSharedLineChart", "shared={false} in a line chart, which keeps the axis tooltip", {shared: false}, [[{hover: [210, 5]}]]),
  lineChart("TooltipGaps", "data with a null and a missing pv, filtered out by filterNull", {}, [[{hover: [210, 5]}], [{hover: [340, 5]}]], gaps),
  lineChart("TooltipKeepNulls", "filterNull={false} keeps the null and missing values", {filterNull: false}, [[{hover: [210, 5]}], [{hover: [340, 5]}]], gaps),
  lineChart("TooltipFormats", "a separator, a value formatter, a label formatter, itemSorter by value and the content, label and item styles",
    {separator: " = ", formatter: fmt("{0} kg"), labelFormatter: fmt("Page: {0}"), itemSorter: "value",
      contentStyle: {backgroundColor: "#f8f8f8", border: "2px solid #8884d8", borderRadius: 6, padding: 6}, labelStyle: {fontWeight: "bold"}, itemStyle: {color: "#333"}},
    [[{hover: [210, 5]}], [{hover: [80, 5]}]]),
  lineChart("TooltipLineCursor", "a cursor of its own stroke, width and dashes", {cursor: {stroke: "red", strokeWidth: 2, strokeDasharray: "5 5"}}, [[{hover: [210, 5]}]]),
  barChart("TooltipBarCursor", "a band cursor of its own fill and stroke", {cursor: {fill: "#f5f5f5", stroke: "#8884d8"}}, [[{hover: [205, 5]}]]),
  barChart("TooltipNoCursor", "cursor={false}", {cursor: false}, [[{hover: [205, 5]}]]),
  lineChart("TooltipDefaultIndex", "defaultIndex={3}, shown before any pointer event", {defaultIndex: 3}, [[{hover: [405, 5]}]]),
  {
    name: "TooltipDefaultIndexBar", kind: "BarChart", width: 500, height: 300, margin, data: pages,
    source: source("defaultIndex={1} and cursor={false} over stacked bars without a y axis, as shadcn/ui's tooltip examples set them"),
    children: [node("CartesianGrid", {vertical: false}), xName, node("Tooltip", {defaultIndex: 1, cursor: false}),
      node("Bar", {dataKey: "pv", stackId: "a", fill: "#8884d8", ...off}),
      node("Bar", {dataKey: "uv", stackId: "a", fill: "#82ca9d", ...off})],
    interactions: [[{hover: [405, 150]}]],
  },
  barChart("TooltipClick", "trigger=\"click\"", {trigger: "click"}, [[{click: [205, 5]}], [{click: [205, 5]}, {hover: [405, 5]}], [{click: [205, 5]}, {click: [405, 5]}]]),
  lineChart("TooltipPosition", "a fixed position", {position: {x: 300, y: 20}}, [[{hover: [210, 5]}]]),
  lineChart("TooltipOffset", "an offset of 40 and the direction reversed along x", {offset: 40, reverseDirection: {x: true}}, [[{hover: [275, 5]}], [{hover: [80, 5]}]]),
  lineChart("TooltipEscape", "allowEscapeViewBox along x, so that the box is not flipped at the right edge", {allowEscapeViewBox: {x: true}}, [[{hover: [470, 5]}]]),
  lineChart("TooltipActiveFalse", "active={false}: the active dots show, the box and the cursor do not", {active: false}, [[{hover: [210, 5]}]]),
  {
    name: "TooltipHiddenSeries", kind: "LineChart", width: 500, height: 300, margin, data: pages,
    source: source("includeHidden with a hidden line, a line with tooltipType=\"none\" and payloadUniqBy over two lines of the same data key"),
    children: [grid, xName, yAxis, node("Tooltip", {includeHidden: true, payloadUniqBy: true}), legend,
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", hide: true, ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off}),
      node("Line", {type: "monotone", dataKey: "uv", name: "uv again", stroke: "#ff7300", ...off}),
      node("Line", {type: "monotone", dataKey: "amt", stroke: "#ffc658", tooltipType: "none", ...off})],
    interactions: [[{hover: [210, 5]}]],
  },
];

const legendSource = (what) => `Recharts' Legend API (recharts.org/en-US/api/Legend) on SimpleLineChart's or SimpleBarChart's data: ${what}`;
const icons = ["line", "plainline", "square", "rect", "circle", "cross", "diamond", "star", "triangle", "wye", "none"];
const iconColors = ["#8884d8", "#82ca9d", "#ffc658", "#ff7300", "#0088FE", "#00C49F", "#FFBB28", "#FF8042", "red", "purple", "black"];

const legendChart = (name, what, legendProps, series = [], kind = "LineChart") => ({
  name, kind, width: 500, height: 300, margin, data: pages, source: legendSource(what),
  children: [grid, xName, yAxis, node("Legend", legendProps), ...(series.length ? series : [
    node(kind === "BarChart" ? "Bar" : "Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", fill: "#8884d8", ...off}),
    node(kind === "BarChart" ? "Bar" : "Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", fill: "#82ca9d", ...off})])],
});

examples.push(
  legendChart("LegendPayload", "a custom payload listing every icon type", {payload: icons.map((type, index) => ({value: type, type, color: iconColors[index], id: type, payload: {}}))}, [], "BarChart"),
  legendChart("LegendTypes", "each series' legendType: cross, diamond and triangle", {}, [
    node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", legendType: "cross", ...off}),
    node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", legendType: "diamond", ...off}),
    node("Line", {type: "monotone", dataKey: "amt", stroke: "#ffc658", legendType: "triangle", ...off})]),
  legendChart("LegendIconType", "iconType=\"wye\" for every item and an icon size of 20", {iconType: "wye", iconSize: 20}),
  legendChart("LegendStarSquare", "star and square icons on bars", {}, [
    node("Bar", {dataKey: "pv", fill: "#8884d8", legendType: "star", ...off}),
    node("Bar", {dataKey: "uv", fill: "#82ca9d", legendType: "square", ...off})], "BarChart"),
  legendChart("LegendInactive", "inactiveColor=\"#999\" on the item of a hidden series", {inactiveColor: "#999"}, [
    node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", hide: true, ...off}),
    node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off})]),
  legendChart("LegendFormatter", "a formatter and the legend at the top right", {formatter: fmt("{0} (k)"), align: "right", verticalAlign: "top"}),
  legendChart("LegendVerticalLeft", "a vertical legend at the top left", {layout: "vertical", align: "left", verticalAlign: "top"}),
  legendChart("LegendMiddle", "a horizontal legend in the middle, which takes no space from the plot", {verticalAlign: "middle"}),
  legendChart("LegendVerticalCenter", "a vertical legend centered at the bottom", {layout: "vertical", align: "center", verticalAlign: "bottom"}),
  legendChart("LegendWidth", "a legend 200 px wide, aligned left, whose items wrap", {width: 200, align: "left"}, [
    node("Line", {type: "monotone", dataKey: "pv", name: "page views", stroke: "#8884d8", ...off}),
    node("Line", {type: "monotone", dataKey: "uv", name: "unique visitors", stroke: "#82ca9d", ...off}),
    node("Line", {type: "monotone", dataKey: "amt", name: "amount", stroke: "#ffc658", ...off})]),
  legendChart("LegendDashed", "a plainline icon that takes its series' dashes, and payloadUniqBy over two series of one name", {payloadUniqBy: true}, [
    node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", strokeDasharray: "5 5", legendType: "plainline", ...off}),
    node("Line", {type: "monotone", dataKey: "uv", name: "pv", stroke: "#82ca9d", ...off})]),
);

const labelSource = (what) => `Recharts' LabelList API (recharts.org/en-US/api/LabelList): ${what}`;
const groups = [
  {name: "Group A", value: 400},
  {name: "Group B", value: 300},
  {name: "Group C", value: 300},
  {name: "Group D", value: 200},
];
const pieColors = ["#0088FE", "#00C49F", "#FFBB28", "#FF8042"];
const ages = [
  {name: "18-24", uv: 31.47, fill: "#8884d8"},
  {name: "25-29", uv: 26.69, fill: "#83a6ed"},
  {name: "30-34", uv: 15.69, fill: "#8dd1e1"},
  {name: "35-39", uv: 8.22, fill: "#82ca9d"},
];
const funnelData = [
  {value: 100, name: "Impression", fill: "#8884d8"},
  {value: 80, name: "Click", fill: "#83a6ed"},
  {value: 50, name: "Visit", fill: "#8dd1e1"},
  {value: 40, name: "Consult", fill: "#82ca9d"},
  {value: 26, name: "Order", fill: "#a4de6c"},
];

examples.push(
  {
    name: "LabelListsBarChart", kind: "BarChart", width: 500, height: 300, margin: {top: 20, right: 30, left: 20, bottom: 5}, data: pages,
    source: labelSource("a bar's label prop and three LabelLists: rotated names inside the bottom in a size and weight of their own, values at an {x, y} offset, and centerTop (which is center on a cartesian label)"),
    children: [grid, xName, yAxis,
      node("Bar", {dataKey: "pv", fill: "#8884d8", label: {position: "top", fill: "#333"}, ...off}, [
        node("LabelList", {dataKey: "name", position: "insideBottom", angle: -90, offset: 30, fill: "#fff", fontSize: 10, fontWeight: "bold"}),
        node("LabelList", {dataKey: "uv", position: {x: 10, y: 20}, fill: "#000", stroke: "#fff", strokeWidth: 0.5}),
        node("LabelList", {dataKey: "amt", position: "centerTop", fill: "#ff7300"})])],
  },
  {
    name: "LabelListsLineChart", kind: "LineChart", width: 500, height: 300, margin: {top: 20, right: 30, left: 20, bottom: 10}, data: pages,
    source: labelSource("two LabelLists on a line, above and below its points"),
    children: [grid, xName, yAxis,
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off}, [
        node("LabelList", {position: "top", offset: 10, fill: "#8884d8"}),
        node("LabelList", {dataKey: "name", position: "bottom", fontSize: 9})])],
  },
  {
    name: "PieLabelLists", kind: "PieChart", width: 400, height: 400, margin: null, data: [],
    source: labelSource("LabelLists on a donut: names outside, values at the ring's middle, and centerTop and centerBottom at the center"),
    children: [node("Pie", {data: groups, dataKey: "value", cx: "50%", cy: "50%", innerRadius: 60, outerRadius: 100, fill: "#8884d8", ...off}, [
      ...groups.map((_, index) => node("Cell", {fill: pieColors[index]})),
      node("LabelList", {dataKey: "name", position: "outside", offset: 12}),
      node("LabelList", {position: "inside", fill: "#fff", stroke: "none"}),
      node("LabelList", {dataKey: "name", position: "centerTop", fill: "#333", stroke: "none"}),
      node("LabelList", {position: "centerBottom", fill: "#999", stroke: "none"})])],
  },
  {
    name: "RadialBarLabelLists", kind: "RadialBarChart", width: 500, height: 300, margin: null, data: ages,
    chartProps: {cx: "50%", cy: "50%", innerRadius: "20%", outerRadius: "90%", barSize: 14},
    source: labelSource("LabelLists on radial bars: values inside the end, names inside the start clockwise, and outside"),
    children: [node("RadialBar", {dataKey: "uv", background: true, ...off}, [
      node("LabelList", {position: "insideEnd", fill: "#fff", fontSize: 10}),
      node("LabelList", {dataKey: "name", position: "insideStart", clockWise: true, fill: "#333"}),
      node("LabelList", {position: "outside", fill: "#666"})])],
  },
  {
    name: "FunnelLabelLists", kind: "FunnelChart", width: 700, height: 432, margin: {right: 30}, data: null,
    source: labelSource("two LabelLists on a funnel: names at the right, values at the center"),
    children: [node("Funnel", {dataKey: "value", data: funnelData, ...off}, [
      node("LabelList", {position: "right", fill: "#000", stroke: "none", dataKey: "name"}),
      node("LabelList", {position: "center", fill: "#fff", stroke: "none"})])],
  },
);

examples.push({
  name: "LabelListsBreakAll", kind: "BarChart", width: 500, height: 300, margin: {top: 20, right: 30, left: 20, bottom: 5}, data: pages,
  source: labelSource("textBreakAll on labels wrapped to their bar's width"),
  children: [grid, xName, yAxis,
    node("Bar", {dataKey: "pv", fill: "#8884d8", ...off}, [
      node("LabelList", {dataKey: "name", position: "insideTop", textBreakAll: true, fill: "#fff"})])],
});

const shapeSource = (what) => `Recharts' API (recharts.org/en-US/api): ${what}`;
const subjects = [
  {subject: "Math", A: 120, B: 110},
  {subject: "Chinese", A: 98, B: null},
  {subject: "English", A: 86, B: 130},
  {subject: "Geography", A: 99, B: 100},
  {subject: "Physics", A: 85, B: 90},
  {subject: "History", A: 65, B: 85},
];

examples.push(
  {
    name: "DotProps", kind: "LineChart", width: 500, height: 300, margin, data: pages,
    source: shapeSource("Line's dot and activeDot given as objects"),
    children: [grid, xName, yAxis, node("Tooltip"), node("Legend"),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", dot: {r: 5, fill: "#8884d8", stroke: "#fff", strokeWidth: 2}, activeDot: {r: 8, fill: "red", stroke: "#000", strokeWidth: 1}, ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", dot: {r: 4, fillOpacity: 0.5, strokeDasharray: "2 2"}, ...off})],
    interactions: [[{hover: [210, 5]}]],
  },
  {
    name: "AreaDotProps", kind: "AreaChart", width: 500, height: 300, margin, data: pages,
    source: shapeSource("Area's dots, which take the area's fill and fill opacity, and a dot object"),
    children: [grid, xName, yAxis,
      node("Area", {type: "monotone", dataKey: "uv", stroke: "#8884d8", fill: "#8884d8", dot: true, ...off}),
      node("Area", {type: "monotone", dataKey: "pv", stroke: "#82ca9d", fill: "#82ca9d", dot: {r: 6, stroke: "#333"}, ...off})],
  },
  {
    name: "CellProps", kind: "BarChart", width: 500, height: 300, margin, data: pages,
    source: shapeSource("Cell's stroke, stroke width, fill opacity, opacity and dashes"),
    children: [grid, xName, yAxis,
      node("Bar", {dataKey: "uv", fill: "#8884d8", ...off}, pages.map((_, index) => node("Cell", {
        fill: pieColors[index % pieColors.length], stroke: "#333", strokeWidth: index % 3, fillOpacity: index % 2 ? 0.5 : 1, opacity: index === 6 ? 0.4 : 1, strokeDasharray: index === 3 ? "4 2" : undefined}))),
    ],
  },
  {
    name: "GridStripes", kind: "LineChart", width: 500, height: 300, margin, data: pages,
    source: shapeSource("CartesianGrid's horizontal and vertical stripes, fill opacity, stroke width and a background"),
    children: [node("CartesianGrid", {stroke: "#999", strokeWidth: 2, fill: "#fafafa", horizontalFill: ["#ccc", "#fff"], verticalFill: ["#eef", "#fee", "#efe"], fillOpacity: 0.3}), xName, yAxis,
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off})],
  },
  {
    name: "GridPoints", kind: "LineChart", width: 500, height: 300, margin, data: pages,
    source: shapeSource("CartesianGrid's own horizontal and vertical points"),
    children: [node("CartesianGrid", {strokeDasharray: "3 3", horizontalPoints: [50, 100, 150], verticalPoints: [100, 200, 300]}), xName, yAxis,
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off})],
  },
  {
    name: "GridSyncWithTicks", kind: "BarChart", width: 500, height: 300, margin, data: pages,
    source: shapeSource("CartesianGrid's syncWithTicks, its lines at the ticks only"),
    children: [node("CartesianGrid", {syncWithTicks: true, strokeOpacity: 0.5}), xName, yAxis,
      node("Bar", {dataKey: "pv", fill: "#8884d8", ...off})],
  },
  {
    name: "PolarGridProps", kind: "RadarChart", width: 500, height: 400, margin: null, data: subjects,
    chartProps: {cx: "50%", cy: "50%", outerRadius: "80%"},
    source: shapeSource("PolarGrid as circles with dashes, a width and a fill, without radial lines; and a second radar chart grid's own angles and radii are in PolarGridAngles"),
    children: [node("PolarGrid", {gridType: "circle", radialLines: false, strokeDasharray: "3 3", stroke: "#999", strokeWidth: 2, fill: "#eee"}), node("PolarAngleAxis", {dataKey: "subject"}), node("PolarRadiusAxis"),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, ...off})],
  },
  {
    name: "PolarGridAngles", kind: "RadarChart", width: 500, height: 400, margin: null, data: subjects,
    chartProps: {cx: "50%", cy: "50%", outerRadius: "80%"},
    source: shapeSource("PolarGrid's own polarAngles and polarRadius"),
    children: [node("PolarGrid", {polarAngles: [0, 45, 90, 135, 180, 225, 270, 315], polarRadius: [40, 80, 120]}), node("PolarAngleAxis", {dataKey: "subject"}),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, ...off})],
  },
  {
    name: "RadarOptions", kind: "RadarChart", width: 500, height: 400, margin: null, data: subjects,
    chartProps: {cx: "50%", cy: "50%", outerRadius: "80%"},
    source: shapeSource("Radar's dot and activeDot objects, connectNulls over a missing value, a label and a LabelList"),
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "subject"}), node("PolarRadiusAxis"), node("Tooltip"),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, dot: {r: 4, fill: "red"}, activeDot: {r: 6, fill: "#000", stroke: "#fff", strokeWidth: 3}, label: {position: "top", fill: "#333"}, ...off}),
      node("Radar", {name: "Lily", dataKey: "B", stroke: "#82ca9d", fill: "#82ca9d", fillOpacity: 0.6, connectNulls: true, dot: true, ...off}, [node("LabelList", {position: "right", fill: "#82ca9d"})])],
    interactions: [[{hover: [250, 120]}]],
  },
);

const polarSource = (what) => `Recharts' RadialBar and Pie API (recharts.org/en-US/api): ${what}`;
const bands = [
  {name: "18-24", uv: 31.47, amt: 12, fill: "#8884d8"},
  {name: "25-29", uv: 26.69, amt: 20, fill: "#83a6ed"},
  {name: "30-34", uv: 15.69, amt: 8, fill: "#8dd1e1"},
  {name: "35-39", uv: 8.22, amt: 30, fill: "#82ca9d"},
  {name: "40-49", uv: 0.6, amt: 5, fill: "#a4de6c"},
];
const sixGroups = [...groups, {name: "Group E", value: 6}, {name: "Group F", value: 189}];

examples.push(
  {
    name: "RadialBarOptions", kind: "RadialBarChart", width: 500, height: 300, margin: null, data: bands,
    chartProps: {cx: "50%", cy: "50%", innerRadius: "10%", outerRadius: "90%"},
    source: polarSource("maxBarSize, cornerRadius with forceCornerRadius and cornerIsExternal, and a background object"),
    children: [node("RadialBar", {dataKey: "uv", maxBarSize: 14, cornerRadius: 10, forceCornerRadius: true, cornerIsExternal: true, background: {fill: "#ddd", stroke: "#bbb", strokeWidth: 1}, ...off})],
  },
  {
    name: "StackedRadialBarChart", kind: "RadialBarChart", width: 500, height: 300, margin: null, data: bands,
    chartProps: {cx: "50%", cy: "50%", innerRadius: "10%", outerRadius: "90%", barSize: 12},
    source: polarSource("two RadialBars of one stackId"),
    children: [node("RadialBar", {dataKey: "uv", stackId: "a", fill: "#8884d8", ...off}), node("RadialBar", {dataKey: "amt", stackId: "a", fill: "#82ca9d", ...off}), node("Legend"), node("Tooltip")],
  },
  {
    name: "RadialBarItemTooltip", kind: "RadialBarChart", width: 500, height: 300, margin: null, data: bands,
    chartProps: {cx: "50%", cy: "50%", innerRadius: "10%", outerRadius: "90%", barSize: 14},
    source: polarSource("shared={false}: a tooltip per radial bar"),
    children: [node("RadialBar", {dataKey: "uv", background: true, ...off}), node("Tooltip", {shared: false})],
    interactions: [[{hover: [262, 61]}]],
  },
  {
    name: "PieActiveShapes", kind: "PieChart", width: 400, height: 400, margin: null, data: [],
    source: polarSource("activeIndex as an array, an activeShape and an inactiveShape object, and blendStroke"),
    children: [node("Pie", {data: groups, dataKey: "value", cx: "50%", cy: "50%", innerRadius: 50, outerRadius: 80, activeIndex: [0, 2], activeShape: {outerRadius: 90, fill: "#ff7300"}, inactiveShape: {fillOpacity: 0.5}, blendStroke: true, ...off},
      groups.map((_, index) => node("Cell", {fill: pieColors[index]})))],
  },
  {
    name: "PieLabelProps", kind: "PieChart", width: 400, height: 400, margin: null, data: [],
    source: polarSource("a label object (offsetRadius, fill, font size and weight) and a labelLine object"),
    children: [node("Pie", {data: groups, dataKey: "value", cx: "50%", cy: "50%", outerRadius: 100, fill: "#8884d8", label: {offsetRadius: 30, fill: "#333", fontSize: 12, fontWeight: "bold"}, labelLine: {stroke: "#999", strokeWidth: 2, strokeDasharray: "3 3"}, ...off})],
  },
  {
    name: "PieAngles", kind: "PieChart", width: 400, height: 400, margin: null, data: [],
    source: polarSource("a clockwise pie from 90° to -270° with a padding angle and a minimum angle that widens a small sector"),
    children: [node("Pie", {data: sixGroups, dataKey: "value", cx: "50%", cy: "50%", innerRadius: 60, outerRadius: 100, startAngle: 90, endAngle: -270, paddingAngle: 3, minAngle: 10, fill: "#82ca9d", label: true, ...off})],
  },
);

examples.push(
  {
    name: "ResponsiveAspect", kind: "LineChart", width: 500, height: 250, margin, data: pages,
    container: {width: 500, aspect: 2},
    source: "Recharts' ResponsiveContainer API (recharts.org/en-US/api/ResponsiveContainer): a width of 500 and an aspect of 2",
    children: [grid, xName, yAxis, node("Tooltip"),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off})],
  },
  {
    name: "ResponsiveMaxHeight", kind: "BarChart", width: 500, height: 300, margin, data: pages,
    container: {width: 500, aspect: 1, maxHeight: 300},
    source: "Recharts' ResponsiveContainer API: an aspect of 1 capped by a maxHeight of 300",
    children: [grid, xName, yAxis, node("Bar", {dataKey: "pv", fill: "#8884d8", ...off})],
  },
);

examples.push(legendChart("LegendWrapperStyle", "a wrapperStyle that moves the legend to the top left and sets its line height and background", {wrapperStyle: {top: 0, left: 100, lineHeight: "30px", backgroundColor: "#f5f5f5"}}));
