import {node, off} from "./common.mjs";

const groups = [
  {name: "Group A", value: 400},
  {name: "Group B", value: 300},
  {name: "Group C", value: 300},
  {name: "Group D", value: 200},
];

const sixGroups = [...groups, {name: "Group E", value: 278}, {name: "Group F", value: 189}];

const twoLevelOuter = [
  {name: "A1", value: 100},
  {name: "A2", value: 300},
  {name: "B1", value: 100},
  {name: "B2", value: 80},
  {name: "B3", value: 40},
  {name: "B4", value: 30},
  {name: "B5", value: 50},
  {name: "C1", value: 100},
  {name: "C2", value: 200},
  {name: "D1", value: 150},
  {name: "D2", value: 50},
];

const bigGroups = [
  {name: "Group A", value: 2400},
  {name: "Group B", value: 4567},
  {name: "Group C", value: 1398},
  {name: "Group D", value: 9800},
  {name: "Group E", value: 3908},
  {name: "Group F", value: 4800},
];

const activeGroups = [
  {name: "Group A", value: 400},
  {name: "Group B", value: 300},
  {name: "Group C", value: 100},
  {name: "Group D", value: 200},
];

const colors = ["#0088FE", "#00C49F", "#FFBB28", "#FF8042"];

export const subjects = [
  {subject: "Math", A: 120, B: 110, fullMark: 150},
  {subject: "Chinese", A: 98, B: 130, fullMark: 150},
  {subject: "English", A: 86, B: 130, fullMark: 150},
  {subject: "Geography", A: 99, B: 100, fullMark: 150},
  {subject: "Physics", A: 85, B: 90, fullMark: 150},
  {subject: "History", A: 65, B: 85, fullMark: 150},
];

export const ages = [
  {name: "18-24", uv: 31.47, pv: 2400, fill: "#8884d8"},
  {name: "25-29", uv: 26.69, pv: 4567, fill: "#83a6ed"},
  {name: "30-34", uv: 15.69, pv: 1398, fill: "#8dd1e1"},
  {name: "35-39", uv: 8.22, pv: 9800, fill: "#82ca9d"},
  {name: "40-49", uv: 8.63, pv: 3908, fill: "#a4de6c"},
  {name: "50+", uv: 2.63, pv: 4800, fill: "#d0ed57"},
  {name: "unknow", uv: 6.67, pv: 4800, fill: "#ffc658"},
];

const site = "recharts.org's src/docs/exampleComponents (Recharts 2.x site)";

export const examples = [
  {
    name: "TwoLevelPieChart", kind: "PieChart", width: 400, height: 400, margin: null, data: [],
    source: `${site}: PieChart/TwoLevelPieChart.tsx`,
    children: [
      node("Pie", {data: groups, dataKey: "value", cx: "50%", cy: "50%", outerRadius: 60, fill: "#8884d8", ...off}),
      node("Pie", {data: twoLevelOuter, dataKey: "value", cx: "50%", cy: "50%", innerRadius: 70, outerRadius: 90, fill: "#82ca9d", label: true, ...off})],
  },
  {
    name: "StraightAnglePieChart", kind: "PieChart", width: 400, height: 400, margin: null, data: [],
    source: `${site}: PieChart/StraightAnglePieChart.tsx`,
    children: [
      node("Pie", {dataKey: "value", startAngle: 180, endAngle: 0, data: sixGroups, cx: "50%", cy: "50%", outerRadius: 80, fill: "#8884d8", label: true, ...off})],
  },
  {
    name: "TwoSimplePieChart", kind: "PieChart", width: 800, height: 400, margin: null, data: [],
    source: `${site}: PieChart/TwoSimplePieChart.tsx (800 wide and the first pie at cx 200, so that the second pie, at cx 500, is inside the chart)`,
    children: [
      node("Pie", {dataKey: "value", data: sixGroups, cx: 200, cy: "50%", outerRadius: 80, fill: "#8884d8", label: true, ...off}),
      node("Pie", {dataKey: "value", data: bigGroups, cx: 500, cy: 200, innerRadius: 40, outerRadius: 80, fill: "#82ca9d", ...off}),
      node("Tooltip")],
  },
  {
    name: "PieChartWithPaddingAngle", kind: "PieChart", width: 800, height: 400, margin: null, data: [],
    source: `${site}: PieChart/PieChartWithPaddingAngle.tsx`,
    children: [
      node("Pie", {data: groups, cx: 120, cy: 200, innerRadius: 60, outerRadius: 80, fill: "#8884d8", paddingAngle: 5, dataKey: "value", ...off},
        groups.map((_, index) => node("Cell", {fill: colors[index % colors.length]}))),
      node("Pie", {data: groups, cx: 420, cy: 200, startAngle: 180, endAngle: 0, innerRadius: 60, outerRadius: 80, fill: "#8884d8", paddingAngle: 5, dataKey: "value", ...off},
        groups.map((_, index) => node("Cell", {fill: colors[index % colors.length]})))],
  },
  {
    name: "ActiveShapePieChart", kind: "PieChart", width: 400, height: 400, margin: null, data: [],
    source: `${site}: PieChart/CustomActiveShapePieChart.tsx (the custom active shape function replaced by an activeShape object that Recharts merges into the sector, a fixed activeIndex, and the default tooltip)`,
    children: [
      node("Pie", {activeIndex: 1, activeShape: {outerRadius: 90, fill: "#ff7300"}, data: activeGroups, cx: "50%", cy: "50%", innerRadius: 60, outerRadius: 80, fill: "#8884d8", dataKey: "value", ...off}),
      node("Tooltip")],
  },
  {
    name: "SimpleRadarChart", kind: "RadarChart", width: 500, height: 400, margin: null, data: subjects,
    source: `${site}: RadarChart/SimpleRadarChart.tsx (500 by 400 instead of a responsive container)`,
    chartProps: {cx: "50%", cy: "50%", outerRadius: "80%"},
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "subject"}), node("PolarRadiusAxis"),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, ...off})],
  },
  {
    name: "SpecifiedDomainRadarChart", kind: "RadarChart", width: 500, height: 400, margin: null, data: subjects,
    source: `${site}: RadarChart/SpecifiedDomainRadarChart.tsx (500 by 400 instead of a responsive container)`,
    chartProps: {cx: "50%", cy: "50%", outerRadius: "80%"},
    children: [node("PolarGrid"), node("PolarAngleAxis", {dataKey: "subject"}), node("PolarRadiusAxis", {angle: 30, domain: [0, 150]}),
      node("Radar", {name: "Mike", dataKey: "A", stroke: "#8884d8", fill: "#8884d8", fillOpacity: 0.6, ...off}),
      node("Radar", {name: "Lily", dataKey: "B", stroke: "#82ca9d", fill: "#82ca9d", fillOpacity: 0.6, ...off}),
      node("Legend"), node("Tooltip")],
  },
  {
    name: "SimpleRadialBarChart", kind: "RadialBarChart", width: 500, height: 300, margin: null, data: ages,
    source: `${site}: RadialBarChart/SimpleRadialBarChart.tsx (500 by 300 instead of a responsive container; the legend's wrapperStyle replaced by align right, which places it the same way)`,
    chartProps: {cx: "50%", cy: "50%", innerRadius: "10%", outerRadius: "80%", barSize: 10},
    children: [
      node("RadialBar", {minAngle: 15, label: {position: "insideStart", fill: "#fff"}, background: true, clockWise: true, dataKey: "uv", ...off}),
      node("Legend", {iconSize: 10, layout: "vertical", verticalAlign: "middle", align: "right"}),
      node("Tooltip")],
  },
];
