import {node, off, pages} from "./common.mjs";

const standardMargin = {top: 5, right: 30, left: 20, bottom: 5};
const grid = node("CartesianGrid", {strokeDasharray: "3 3"});
const tooltip = node("Tooltip");
const legend = node("Legend");

const signedPages = [
  {name: "Page A", uv: 4000, pv: 2400, amt: 2400},
  {name: "Page B", uv: -3000, pv: 1398, amt: 2210},
  {name: "Page C", uv: -2000, pv: -9800, amt: 2290},
  {name: "Page D", uv: 2780, pv: 3908, amt: 2000},
  {name: "Page E", uv: -1890, pv: 4800, amt: 2181},
  {name: "Page F", uv: 2390, pv: -3800, amt: 2500},
  {name: "Page G", uv: 3490, pv: 4300, amt: 2100},
];

const temperatures = [
  {day: "05-01", temperature: [-1, 10]},
  {day: "05-02", temperature: [2, 15]},
  {day: "05-03", temperature: [3, 12]},
  {day: "05-04", temperature: [4, 12]},
  {day: "05-05", temperature: [12, 16]},
  {day: "05-06", temperature: [5, 16]},
  {day: "05-07", temperature: [3, 12]},
  {day: "05-08", temperature: [0, 8]},
  {day: "05-09", temperature: [-3, 5]},
];

const overflowPages = pages.map((page, index) => ({...page, pvError: [300, 200, 900, 400, 500, 350, 450][index]}));

const pairPages = pages.map((page, index) => ({
  ...page,
  pvError: [[200, 600], [100, 300], [900, 400], 400, [0, 500], null, [450, 150]][index],
  uvError: [[400, 100], 250, [300, 600], [280, 0], [190, 400], [240, 240], [350, 100]][index],
}));

const stackedErrors = pages.map((page, index) => ({...page, uvError: [400, [250, 500], 300, 280, [190, 900], 240, 350][index]}));

const heights = [
  {x: 100, y: 200},
  {x: 120, y: 100},
  {x: 170, y: 300},
  {x: 140, y: 250},
  {x: 150, y: 400},
  {x: 110, y: 280},
  {x: 180, y: 320},
];

const spread = [
  {x: 10, y: 30},
  {x: 30, y: 200},
  {x: 45, y: 100},
  {x: 50, y: 400},
  {x: 70, y: 150},
  {x: 100, y: 250},
];

const trend = [
  {x: 30, y: 20},
  {x: 50, y: 180},
  {x: 75, y: 240},
  {x: 100, y: 100},
  {x: 120, y: 190},
];

const subjects = [
  {subject: "Math", range: [60, 120], fullMark: 150},
  {subject: "Chinese", range: [50, 98], fullMark: 150},
  {subject: "English", range: [70, 86], fullMark: 150},
  {subject: "Geography", range: [40, 99], fullMark: 150},
  {subject: "Physics", range: [55, 85], fullMark: 150},
  {subject: "History", range: [30, 65], fullMark: 150},
];

const ownPages = [
  {name: "Page A", pv: 1200},
  {name: "Page B", pv: 2400},
  {name: "Page C", pv: 900},
  {name: "Page D", pv: 1800},
];

const brushPages = Array.from({length: 30}, (_, index) => ({
  name: `Day ${index + 1}`,
  uv: [300, 145, 100, 8, 100, 9, 53, 252, 79, 294, 43, 74, 71, 117, 186, 16, 125, 222, 372, 182, 164, 316, 131, 291, 47, 415, 182, 93, 99, 52][index],
  pv: [456, 230, 345, 450, 321, 235, 267, 378, 210, 23, 45, 90, 130, 11, 107, 926, 653, 366, 486, 512, 302, 425, 467, 190, 194, 371, 376, 295, 322, 246][index],
}));

const visits = {
  nodes: [{name: "Visit"}, {name: "Direct-Favourite"}, {name: "Page-Click"}, {name: "Detail-Favourite"}, {name: "Lost"}],
  links: [
    {source: 0, target: 1, value: 3728.3},
    {source: 0, target: 2, value: 354170},
    {source: 2, target: 3, value: 62429},
    {source: 2, target: 4, value: 291741},
  ],
};

export const examples = [
  {
    name: "DataOverflowComposedChart", kind: "ComposedChart", width: 500, height: 300, margin: standardMargin, data: overflowPages,
    source: "Recharts' YAxis allowDataOverflow API example (recharts.org/en-US/api/YAxis: a specified domain that cuts the data) on SimpleLineChart's data, with an area, bars, a line with error bars, a line whose dots are not clipped (dot clipDot false) and a scatter",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis", {domain: [0, 5000], allowDataOverflow: true}), tooltip, legend,
      node("Area", {type: "monotone", dataKey: "amt", fill: "#8884d8", stroke: "#8884d8", ...off}),
      node("Bar", {dataKey: "uv", barSize: 20, fill: "#413ea0", ...off}),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#ff7300", ...off}, [node("ErrorBar", {dataKey: "pvError", width: 4, stroke: "green", direction: "y"})]),
      node("Line", {type: "monotone", dataKey: "amt", stroke: "#82ca9d", dot: {clipDot: false}, ...off}),
      node("Scatter", {dataKey: "pv", fill: "red", ...off})],
  },
  {
    name: "DataOverflowScatterChart", kind: "ScatterChart", width: 400, height: 400, margin: {top: 20, right: 20, bottom: 20, left: 20}, data: null,
    source: "Recharts' XAxis allowDataOverflow API example (recharts.org/en-US/api/XAxis) on a scatter chart: the x domain cut to [110, 170], the joint line and the symbols clipped to the plot's width",
    children: [node("CartesianGrid"), node("XAxis", {type: "number", dataKey: "x", name: "stature", unit: "cm", domain: [110, 170], allowDataOverflow: true}),
      node("YAxis", {type: "number", dataKey: "y", name: "weight", unit: "kg"}), node("Tooltip", {cursor: {strokeDasharray: "3 3"}}),
      node("Scatter", {name: "A school", data: heights, fill: "#8884d8", line: true, ...off})],
  },
  {
    name: "ReferenceOverflowLineChart", kind: "LineChart", width: 500, height: 300, margin: {top: 20, right: 50, left: 20, bottom: 5}, data: pages,
    source: "Recharts' ifOverflow API examples (recharts.org/en-US/api/ReferenceLine, ReferenceArea, ReferenceDot) on SimpleLineChart's data: lines, areas and dots beyond the y domain, visible (drawn where they fall, one area with rounded corners), hidden (clipped to the plot) and discarded (dropped)",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip, legend,
      node("ReferenceLine", {y: 11000, stroke: "red", ifOverflow: "visible", label: "visible"}),
      node("ReferenceLine", {y: 10500, stroke: "green", ifOverflow: "hidden", label: "hidden"}),
      node("ReferenceLine", {y: 12000, stroke: "black", label: "discarded"}),
      node("ReferenceArea", {y1: 8000, y2: 13000, fill: "red", fillOpacity: 0.2, ifOverflow: "hidden", label: "clipped"}),
      node("ReferenceArea", {x1: "Page E", x2: "Page G", y1: 9000, y2: 12000, fill: "blue", fillOpacity: 0.2, ifOverflow: "visible", radius: 10}),
      node("ReferenceArea", {y1: 9000, y2: 11000, fill: "black"}),
      node("ReferenceDot", {x: "Page B", y: 10100, r: 8, fill: "purple", stroke: "none", ifOverflow: "hidden"}),
      node("ReferenceDot", {x: "Page D", y: 10600, r: 8, fill: "orange", stroke: "none", ifOverflow: "visible"}),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off})],
  },
  {
    name: "ReferenceSegmentBarChart", kind: "BarChart", width: 500, height: 300, margin: {top: 20, right: 30, left: 20, bottom: 5}, data: pages,
    source: "Recharts' ReferenceLine API example (recharts.org/en-US/api/ReferenceLine: segment, position, isFront, ifOverflow extendDomain) on SimpleBarChart's data with the x-axis on top and the y-axis on the right; a segment beyond the domain is discarded",
    children: [grid, node("XAxis", {dataKey: "name", orientation: "top"}), node("YAxis", {orientation: "right"}), tooltip,
      node("ReferenceLine", {x: "Page B", position: "start", stroke: "red", isFront: true, label: "start"}),
      node("ReferenceLine", {x: "Page D", position: "end", stroke: "green"}),
      node("ReferenceLine", {y: 12000, stroke: "blue", ifOverflow: "extendDomain", label: "goal"}),
      node("ReferenceLine", {segment: [{x: "Page A", y: 1000}, {x: "Page F", y: 9000}], stroke: "purple", strokeWidth: 2, label: "trend"}),
      node("ReferenceLine", {segment: [{x: "Page B", y: 20000}, {x: "Page C", y: 0}], stroke: "black"}),
      node("Bar", {dataKey: "pv", fill: "#8884d8", ...off}),
      node("Bar", {dataKey: "uv", fill: "#82ca9d", ...off})],
  },
  {
    name: "RangeAreaChart", kind: "AreaChart", width: 500, height: 400, margin: {top: 20, right: 20, bottom: 20, left: 20}, data: temperatures,
    source: "recharts.org src/docs/exampleComponents/AreaChart/RangeAreaChart.js (the data key holds [low, high]), with dots",
    children: [node("XAxis", {dataKey: "day"}), node("YAxis"), tooltip,
      node("Area", {dataKey: "temperature", stroke: "#8884d8", fill: "#8884d8", dot: true, ...off})],
  },
  {
    name: "VerticalRangeAreaChart", kind: "AreaChart", layout: "vertical", width: 400, height: 500, margin: {top: 20, right: 20, bottom: 20, left: 20}, data: temperatures,
    source: "recharts.org src/docs/exampleComponents/AreaChart/RangeAreaChart.js in a vertical layout (a number x-axis and the category y-axis)",
    children: [node("XAxis", {type: "number"}), node("YAxis", {dataKey: "day", type: "category"}), tooltip,
      node("Area", {type: "monotone", dataKey: "temperature", stroke: "#82ca9d", fill: "#82ca9d", ...off})],
  },
  {
    name: "RangeBarChart", kind: "BarChart", width: 500, height: 400, margin: {top: 20, right: 20, bottom: 20, left: 20}, data: temperatures,
    source: "recharts.org src/docs/exampleComponents/BarChart/RangeBarChart.js (floating bars from [low, high]), with labels",
    children: [node("XAxis", {dataKey: "day"}), node("YAxis"), tooltip,
      node("Bar", {dataKey: "temperature", fill: "#8884d8", label: {position: "top"}, ...off})],
  },
  {
    name: "AreaBaseValueChart", kind: "AreaChart", width: 500, height: 400, margin: {top: 10, right: 30, left: 0, bottom: 0}, data: signedPages,
    source: "Recharts' Area baseValue API example (recharts.org/en-US/api/Area: dataMin, dataMax, a number; the chart's baseValue as the default) on PositiveAndNegativeBarChart's data",
    chartProps: {baseValue: "dataMin"},
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip,
      node("Area", {dataKey: "uv", baseValue: "dataMax", stroke: "#8884d8", fill: "#8884d8", ...off}),
      node("Area", {dataKey: "pv", baseValue: 1000, stroke: "#82ca9d", fill: "#82ca9d", ...off}),
      node("Area", {dataKey: "amt", stroke: "#ffc658", fill: "#ffc658", ...off})],
  },
  {
    name: "ErrorBarPairsChart", kind: "ComposedChart", width: 500, height: 300, margin: standardMargin, data: pairPages,
    source: "Recharts' ErrorBar API example (recharts.org/en-US/api/ErrorBar) with errors given as [low, high] pairs (and a plain value and a missing one) on a bar and a line",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip, legend,
      node("Bar", {dataKey: "pv", fill: "#8884d8", ...off}, [node("ErrorBar", {dataKey: "pvError", width: 4, strokeWidth: 2, stroke: "green", direction: "y"})]),
      node("Line", {dataKey: "uv", stroke: "#ff7300", ...off}, [node("ErrorBar", {dataKey: "uvError", width: 6, stroke: "#ff7300", direction: "y"})])],
  },
  {
    name: "StackedErrorBarChart", kind: "BarChart", width: 500, height: 300, margin: standardMargin, data: stackedErrors,
    source: "recharts.org src/docs/exampleComponents/BarChart/StackedBarChart.tsx with an ErrorBar on the top bar of the stack (a value and [low, high] pairs)",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip, legend,
      node("Bar", {dataKey: "pv", stackId: "a", fill: "#8884d8", ...off}),
      node("Bar", {dataKey: "uv", stackId: "a", fill: "#82ca9d", ...off}, [node("ErrorBar", {dataKey: "uvError", width: 4, stroke: "red", direction: "y"})])],
  },
  {
    name: "StackedOwnDataAreaChart", kind: "AreaChart", width: 500, height: 400, margin: {top: 10, right: 30, left: 0, bottom: 0}, data: pages,
    source: "recharts.org src/docs/exampleComponents/AreaChart/StackedAreaChart.tsx where the second area carries its own data (Recharts stacks the chart's data and reads the categories from the series' own items)",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip,
      node("Area", {type: "monotone", dataKey: "uv", stackId: "1", stroke: "#8884d8", fill: "#8884d8", ...off}),
      node("Area", {type: "monotone", dataKey: "pv", stackId: "1", stroke: "#82ca9d", fill: "#82ca9d", data: ownPages, ...off})],
  },
  {
    name: "RangeRadarChart", kind: "RadarChart", width: 500, height: 400, margin: null, data: subjects,
    source: "recharts.org src/docs/exampleComponents/RadarChart/SimpleRadarChart.tsx with a [low, high] data key (Recharts' radar range, Polygon's baseLinePoints) and dots",
    chartProps: {cx: "50%", cy: "50%", outerRadius: "80%"},
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "subject"}), node("PolarRadiusAxis"),
      node("Radar", {name: "Mike", dataKey: "range", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, dot: true, ...off})],
  },
  {
    name: "FittingLineScatterChart", kind: "ScatterChart", width: 400, height: 400, margin: {top: 20, right: 20, bottom: 20, left: 20}, data: null,
    source: "recharts.org src/docs/exampleComponents/ScatterChart/JointLineScatterChart.tsx with lineType fitting (Recharts' linear regression) and line props on both scatters",
    children: [node("CartesianGrid"), node("XAxis", {type: "number", dataKey: "x", name: "stature", unit: "cm"}),
      node("YAxis", {type: "number", dataKey: "y", name: "weight", unit: "kg"}), node("Tooltip", {cursor: {strokeDasharray: "3 3"}}), legend,
      node("Scatter", {name: "A school", data: spread, fill: "#8884d8", line: {stroke: "red", strokeWidth: 2, strokeDasharray: "5 5"}, lineType: "fitting", ...off}),
      node("Scatter", {name: "B school", data: trend, fill: "#82ca9d", line: {strokeWidth: 3}, lineJointType: "monotoneX", shape: "diamond", ...off})],
  },
  {
    name: "BrushPanoramaChart", kind: "LineChart", width: 500, height: 300, margin: standardMargin, data: brushPages,
    source: "recharts.org src/docs/exampleComponents/LineChart/SynchronizedLineChart.tsx's brush with a panorama (an area chart inside the brush), its texts always shown (alwaysShowText) through a tickFormatter, and a window from 5 to 20",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip,
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", ...off}),
      node("Brush", {dataKey: "name", height: 40, stroke: "#8884d8", startIndex: 5, endIndex: 20, alwaysShowText: true, tickFormatter: "on {0}"},
        [node("AreaChart", {}, [node("Area", {type: "monotone", dataKey: "pv", stroke: "#8884d8", fill: "#8884d8", ...off})])])],
  },
  {
    name: "BrushOptionsBarChart", kind: "BarChart", width: 500, height: 300, margin: standardMargin, data: brushPages,
    source: "recharts.org src/docs/exampleComponents/BarChart/BrushBarChart.js with the brush's travellerWidth, fill, stroke, x, y, width and padding set, its texts always shown and a line chart panorama with its own padding",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip,
      node("Bar", {dataKey: "pv", fill: "#8884d8", ...off}),
      node("Brush", {dataKey: "name", height: 30, travellerWidth: 12, gap: 5, fill: "#eee", stroke: "#82ca9d", x: 120, y: 262, width: 300, startIndex: 3, endIndex: 17, alwaysShowText: true, padding: {top: 5, right: 10, bottom: 5, left: 10}},
        [node("LineChart", {}, [node("Line", {dataKey: "uv", stroke: "#413ea0", dot: false, ...off})])])],
  },
  {
    name: "SankeyOptionsChart", kind: "Sankey", width: 800, height: 400, margin: {top: 10, right: 10, bottom: 10, left: 10}, data: visits,
    source: "Recharts' Sankey API example (recharts.org/en-US/api/Sankey: node and link props, nodePadding, nodeWidth, linkCurvature, iterations, sort and dataKey, which 2.15.4 does not read) on SankeyCustomNodeExample's data",
    children: [node("Tooltip")],
    props: {dataKey: "other", nodePadding: 30, nodeWidth: 20, linkCurvature: 0.3, iterations: 4, sort: false,
      node: {fill: "#8884d8", fillOpacity: 1, stroke: "#333", strokeWidth: 2, strokeOpacity: 0.5, strokeDasharray: "4 2"},
      link: {stroke: "#82ca9d", strokeOpacity: 0.5, strokeDasharray: "8 4", fill: "#eee"}},
  },
  {
    name: "SankeyLinkWidthChart", kind: "Sankey", width: 600, height: 300, margin: null, data: visits,
    source: "Recharts' Sankey API example with link={{strokeWidth: 4}} (a fixed width overrides every link's own, as Recharts spreads the link props last)",
    children: [],
    props: {link: {strokeWidth: 4, stroke: "#413ea0", strokeOpacity: 0.6}},
  },
];
