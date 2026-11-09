import {node, off} from "./common.mjs";

export const schoolA = [
  {x: 100, y: 200, z: 200},
  {x: 120, y: 100, z: 260},
  {x: 170, y: 300, z: 400},
  {x: 140, y: 250, z: 280},
  {x: 150, y: 400, z: 500},
  {x: 110, y: 280, z: 200},
];

const schoolB = [
  {x: 200, y: 260, z: 240},
  {x: 240, y: 290, z: 220},
  {x: 190, y: 290, z: 250},
  {x: 198, y: 250, z: 210},
  {x: 180, y: 280, z: 260},
  {x: 210, y: 220, z: 230},
];

const jointA = [
  {x: 10, y: 30},
  {x: 30, y: 200},
  {x: 45, y: 100},
  {x: 50, y: 400},
  {x: 70, y: 150},
  {x: 100, y: 250},
];

const jointB = [
  {x: 30, y: 20},
  {x: 50, y: 180},
  {x: 75, y: 240},
  {x: 100, y: 100},
  {x: 120, y: 190},
];

const measured = [
  {x: 100, y: 200, errorX: 10, errorY: 30},
  {x: 120, y: 100, errorX: 5, errorY: 20},
  {x: 170, y: 300, errorX: 15, errorY: 50},
  {x: 140, y: 250, errorX: 8, errorY: 40},
  {x: 150, y: 400, errorX: 12, errorY: 60},
  {x: 110, y: 280, errorX: 6, errorY: 25},
];

const margin = {top: 20, right: 20, bottom: 20, left: 20};
const xStature = node("XAxis", {type: "number", dataKey: "x", name: "stature", unit: "cm"});
const yWeight = node("YAxis", {type: "number", dataKey: "y", name: "weight", unit: "kg"});
const scatterTooltip = node("Tooltip", {cursor: {strokeDasharray: "3 3"}});

export const examples = [
  {
    name: "SimpleScatterChart", kind: "ScatterChart", width: 500, height: 400, margin, data: null,
    source: "recharts.org src/docs/exampleComponents/ScatterChart/SimpleScatterChart.js (ResponsiveContainer replaced by a width of 500)",
    children: [node("CartesianGrid"), xStature, yWeight, scatterTooltip,
      node("Scatter", {name: "A school", data: schoolA, fill: "#8884d8", ...off})],
  },
  {
    name: "ThreeDimScatterChart", kind: "ScatterChart", width: 500, height: 400, margin, data: null,
    source: "recharts.org src/docs/exampleComponents/ScatterChart/ThreeDimScatterChart.js (ResponsiveContainer replaced by a width of 500)",
    children: [node("CartesianGrid"), xStature, yWeight,
      node("ZAxis", {type: "number", dataKey: "z", range: [60, 400], name: "score", unit: "km"}), scatterTooltip, node("Legend"),
      node("Scatter", {name: "A school", data: schoolA, fill: "#8884d8", shape: "star", ...off}),
      node("Scatter", {name: "B school", data: schoolB, fill: "#82ca9d", shape: "triangle", ...off})],
  },
  {
    name: "JointLineScatterChart", kind: "ScatterChart", width: 500, height: 400, margin, data: null,
    source: "recharts.org src/docs/exampleComponents/ScatterChart/JointLineScatterChart.js (ResponsiveContainer replaced by a width of 500)",
    children: [node("CartesianGrid"), xStature, yWeight, node("ZAxis", {type: "number", range: [100]}), scatterTooltip, node("Legend"),
      node("Scatter", {name: "A school", data: jointA, fill: "#8884d8", line: true, shape: "cross", ...off}),
      node("Scatter", {name: "B school", data: jointB, fill: "#82ca9d", line: true, shape: "diamond", ...off})],
  },
  {
    name: "ScatterChartWithLabels", kind: "ScatterChart", width: 500, height: 400, margin, data: null,
    source: "recharts.org src/docs/exampleComponents/ScatterChart/ScatterChartWithLabels.js (ResponsiveContainer replaced by a width of 500)",
    children: [node("CartesianGrid"), xStature, yWeight, scatterTooltip,
      node("Scatter", {name: "A school", data: schoolA, fill: "#8884d8", ...off}, [node("LabelList", {dataKey: "x"})])],
  },
  {
    name: "ScatterChartWithErrorBars", kind: "ScatterChart", width: 500, height: 400, margin, data: null,
    source: "Recharts' ErrorBar API example (recharts.org/en-US/api/ErrorBar) on SimpleScatterChart's data with errorX and errorY added, and its ReferenceArea API example (x1 150 to 180, y1 200 to 300)",
    children: [node("CartesianGrid"), xStature, yWeight, scatterTooltip,
      node("ReferenceArea", {x1: 150, x2: 180, y1: 200, y2: 300, stroke: "red", strokeOpacity: 0.3}),
      node("Scatter", {name: "A school", data: measured, fill: "#8884d8", ...off}, [
        node("ErrorBar", {dataKey: "errorY", width: 4, strokeWidth: 2, stroke: "green", direction: "y"}),
        node("ErrorBar", {dataKey: "errorX", width: 0, strokeWidth: 1, stroke: "blue", direction: "x"})])],
  },
];
