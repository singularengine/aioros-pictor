import {node, off, pages} from "./common.mjs";

const composedPages = [
  {name: "Page A", uv: 590, pv: 800, amt: 1400},
  {name: "Page B", uv: 868, pv: 967, amt: 1506},
  {name: "Page C", uv: 1397, pv: 1098, amt: 989},
  {name: "Page D", uv: 1480, pv: 1200, amt: 1228},
  {name: "Page E", uv: 1520, pv: 1108, amt: 1100},
  {name: "Page F", uv: 1400, pv: 680, amt: 1700},
];

const errorPages = pages.map((page, index) => ({...page, pvError: [300, 200, 900, 400, 500, 350, 450][index], uvError: [400, 250, 300, 280, 190, 240, 350][index]}));

const brushPages = [
  {name: "1", uv: 300, pv: 456},
  {name: "2", uv: -145, pv: 230},
  {name: "3", uv: -100, pv: 345},
  {name: "4", uv: -8, pv: 450},
  {name: "5", uv: 100, pv: 321},
  {name: "6", uv: 9, pv: 235},
  {name: "7", uv: 53, pv: 267},
  {name: "8", uv: 252, pv: -378},
  {name: "9", uv: 79, pv: -210},
  {name: "10", uv: 294, pv: -23},
  {name: "12", uv: 43, pv: 45},
  {name: "13", uv: -74, pv: 90},
  {name: "14", uv: -71, pv: 130},
  {name: "15", uv: -117, pv: 11},
  {name: "16", uv: -186, pv: 107},
  {name: "17", uv: -16, pv: 926},
  {name: "18", uv: -125, pv: 653},
  {name: "19", uv: 222, pv: 366},
  {name: "20", uv: 372, pv: 486},
  {name: "21", uv: 182, pv: 512},
  {name: "22", uv: 164, pv: 302},
  {name: "23", uv: 316, pv: 425},
  {name: "24", uv: 131, pv: 467},
  {name: "25", uv: 291, pv: -190},
  {name: "26", uv: -47, pv: 194},
  {name: "27", uv: -415, pv: 371},
  {name: "28", uv: -182, pv: 376},
  {name: "29", uv: -93, pv: 295},
  {name: "30", uv: -99, pv: 322},
  {name: "31", uv: -52, pv: 246},
  {name: "32", uv: 154, pv: 33},
  {name: "33", uv: 205, pv: 354},
  {name: "34", uv: 70, pv: 258},
  {name: "35", uv: -25, pv: 359},
  {name: "36", uv: -59, pv: 192},
  {name: "37", uv: -63, pv: 464},
  {name: "38", uv: -91, pv: -2},
  {name: "39", uv: -66, pv: 154},
  {name: "40", uv: -50, pv: 186},
];

const grid = node("CartesianGrid", {strokeDasharray: "3 3"});
const tooltip = node("Tooltip");
const legend = node("Legend");
const standardMargin = {top: 5, right: 30, left: 20, bottom: 5};

export const examples = [
  {
    name: "BrushBarChart", kind: "BarChart", width: 500, height: 300, margin: standardMargin, data: brushPages,
    source: "recharts.org src/docs/exampleComponents/BarChart/BrushBarChart.js",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip,
      node("Legend", {verticalAlign: "top", wrapperStyle: {lineHeight: "40px"}}),
      node("ReferenceLine", {y: 0, stroke: "#000"}),
      node("Brush", {dataKey: "name", height: 30, stroke: "#8884d8"}),
      node("Bar", {dataKey: "pv", fill: "#8884d8", ...off}),
      node("Bar", {dataKey: "uv", fill: "#82ca9d", ...off})],
  },
  {
    name: "BrushWindowBarChart", kind: "BarChart", width: 500, height: 300, margin: standardMargin, data: brushPages,
    source: "recharts.org src/docs/exampleComponents/BarChart/BrushBarChart.js, with the brush's startIndex 5 and endIndex 15 and no legend",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip,
      node("ReferenceLine", {y: 0, stroke: "#000"}),
      node("Brush", {dataKey: "name", height: 30, stroke: "#8884d8", startIndex: 5, endIndex: 15}),
      node("Bar", {dataKey: "pv", fill: "#8884d8", ...off}),
      node("Bar", {dataKey: "uv", fill: "#82ca9d", ...off})],
  },
  {
    name: "VerticalLineChart", kind: "LineChart", layout: "vertical", width: 500, height: 300,
    margin: {top: 20, right: 30, left: 20, bottom: 5}, data: pages,
    source: "recharts.org src/docs/exampleComponents/LineChart/VerticalLineChart.tsx",
    children: [grid, node("XAxis", {type: "number"}), node("YAxis", {dataKey: "name", type: "category"}), tooltip, legend,
      node("Line", {dataKey: "pv", stroke: "#8884d8", ...off}),
      node("Line", {dataKey: "uv", stroke: "#82ca9d", ...off})],
  },
  {
    name: "VerticalComposedChart", kind: "ComposedChart", layout: "vertical", width: 500, height: 400,
    margin: {top: 20, right: 20, bottom: 20, left: 20}, data: composedPages,
    source: "recharts.org src/docs/exampleComponents/ComposedChart/VerticalComposedChart.js",
    children: [node("CartesianGrid", {stroke: "#f5f5f5"}), node("XAxis", {type: "number"}),
      node("YAxis", {dataKey: "name", type: "category", scale: "band"}), tooltip, legend,
      node("Area", {dataKey: "amt", fill: "#8884d8", stroke: "#8884d8", ...off}),
      node("Bar", {dataKey: "pv", barSize: 20, fill: "#413ea0", ...off}),
      node("Line", {dataKey: "uv", stroke: "#ff7300", ...off})],
  },
  {
    name: "VerticalBarChart", kind: "BarChart", layout: "vertical", width: 500, height: 300, margin: standardMargin, data: pages,
    source: "recharts.org src/docs/exampleComponents/BarChart/SimpleBarChart.js with layout=\"vertical\" (a number x-axis and the category y-axis, as VerticalLineChart sets them); the 2.x site has no vertical bar example",
    children: [grid, node("XAxis", {type: "number"}), node("YAxis", {dataKey: "name", type: "category"}), tooltip, legend,
      node("Bar", {dataKey: "pv", fill: "#8884d8", activeBar: {fill: "pink", stroke: "blue"}, ...off}),
      node("Bar", {dataKey: "uv", fill: "#82ca9d", activeBar: {fill: "gold", stroke: "purple"}, ...off})],
  },
  {
    name: "ComposedChartWithErrorBars", kind: "ComposedChart", width: 500, height: 300, margin: standardMargin, data: errorPages,
    source: "Recharts' ErrorBar API example (recharts.org/en-US/api/ErrorBar, direction y) on a bar and a line of SimpleBarChart's data, with an error per point added",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip, legend,
      node("Bar", {dataKey: "pv", fill: "#8884d8", ...off}, [node("ErrorBar", {dataKey: "pvError", width: 4, strokeWidth: 2, stroke: "green", direction: "y"})]),
      node("Line", {dataKey: "uv", stroke: "#ff7300", activeDot: {r: 6, fill: "#ff7300", stroke: "#fff", strokeWidth: 3}, ...off}, [node("ErrorBar", {dataKey: "uvError", width: 6, stroke: "#ff7300", direction: "y"})])],
  },
  {
    name: "LineChartWithReferenceAreas", kind: "LineChart", width: 500, height: 300,
    margin: {top: 20, right: 30, left: 20, bottom: 5}, data: pages,
    source: "recharts.org src/docs/exampleComponents/LineChart/SimpleLineChart.js with Recharts' ReferenceArea and ReferenceDot API examples added (an area between two categories, a band between two values, a dot at the maximum, a dot outside the domain that is discarded), and activeDot={false} on the second line",
    children: [grid, node("XAxis", {dataKey: "name"}), node("YAxis"), tooltip, legend,
      node("ReferenceArea", {x1: "Page B", x2: "Page D", fill: "#8884d8", fillOpacity: 0.2, label: "Peak"}),
      node("ReferenceArea", {y1: 2000, y2: 3000, stroke: "red", strokeOpacity: 0.3}),
      node("ReferenceDot", {x: "Page C", y: 9800, r: 10, fill: "red", stroke: "none", label: "Max"}),
      node("ReferenceDot", {x: "Page E", y: 20000, r: 6, fill: "blue"}),
      node("Line", {type: "monotone", dataKey: "pv", stroke: "#8884d8", activeDot: {r: 8}, ...off}),
      node("Line", {type: "monotone", dataKey: "uv", stroke: "#82ca9d", activeDot: false, ...off})],
  },
  {
    name: "AreaChartGradient", kind: "AreaChart", width: 730, height: 250, margin: {top: 10, right: 30, left: 0, bottom: 0}, data: pages,
    source: "recharts.org api/Area (the AreaChart with two linearGradient fills in its defs)",
    children: [
      node("defs", {}, [
        node("linearGradient", {id: "colorUv", x1: "0", y1: "0", x2: "0", y2: "1"}, [
          node("stop", {offset: "5%", stopColor: "#8884d8", stopOpacity: 0.8}),
          node("stop", {offset: "95%", stopColor: "#8884d8", stopOpacity: 0})]),
        node("linearGradient", {id: "colorPv", x1: "0", y1: "0", x2: "0", y2: "1"}, [
          node("stop", {offset: "5%", stopColor: "#82ca9d", stopOpacity: 0.8}),
          node("stop", {offset: "95%", stopColor: "#82ca9d", stopOpacity: 0})])]),
      node("XAxis", {dataKey: "name"}), node("YAxis"), node("CartesianGrid", {strokeDasharray: "3 3"}), node("Tooltip"),
      node("Area", {type: "monotone", dataKey: "uv", stroke: "#8884d8", fillOpacity: 1, fill: "url(#colorUv)", ...off}),
      node("Area", {type: "monotone", dataKey: "pv", stroke: "#82ca9d", fillOpacity: 1, fill: "url(#colorPv)", ...off})],
  },
];
