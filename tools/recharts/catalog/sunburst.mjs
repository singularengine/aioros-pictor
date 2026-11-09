import {node} from "./common.mjs";

const tree = {
  name: "Root", value: 100, children: [
    {name: "Child1", fill: "#264653", value: 30, children: [
      {name: "third child", value: 10},
      {name: "another child", value: 5},
      {name: "next child", value: 15, children: [
        {name: "third level child", value: 5},
        {name: "third level child", value: 5},
        {name: "third level child", value: 5, children: [{name: "level 4", value: 2}]},
      ]},
    ]},
    {name: "Child2", fill: "#2a9d8f", value: 20, children: [
      {name: "another child", value: 10},
      {name: "next child", value: 10, children: [
        {name: "level 3 of child 2", value: 5},
        {name: "level 3 of child 2", value: 3},
        {name: "level 3 of child 2", value: 2},
      ]},
    ]},
    {name: "Child3", fill: "#e9c46a", value: 20},
    {name: "Child4", fill: "#F4A261", value: 10, children: [
      {name: "child4 child", value: 5},
      {name: "child4 child", value: 5},
    ]},
    {name: "Child5", fill: "#e76f51", value: 20, children: [
      {name: "child5 child", value: 10, children: [
        {name: "level 3 child of child 5", value: 5},
        {name: "level 3 child of child 5", value: 5},
      ]},
      {name: "child5 child", value: 5},
      {name: "child5 child", value: 5},
    ]},
  ],
};

const budget = {
  name: "Budget", size: 1000, children: [
    {name: "Housing", size: 450, children: [{name: "Rent", size: 350}, {name: "Utilities", size: 100}]},
    {name: "Food", size: 250, fill: "#82ca9d", children: [{name: "Groceries", size: 180}, {name: "Restaurants", size: 70}]},
    {name: "Savings", size: 300, fill: "#ffc658"},
  ],
};

export const examples = [
  {
    name: "SimpleSunburst", kind: "SunburstChart", width: 500, height: 500, margin: null, data: tree,
    source: "storybook/stories/API/chart/SunburstChart.stories.tsx (Recharts 2's storybook data; a tooltip)",
    children: [node("Tooltip")],
    props: {},
  },
  {
    name: "CustomSunburst", kind: "SunburstChart", width: 600, height: 400, margin: null, data: budget,
    source: "A partial sunburst written for Pictor: dataKey, center, radii, ring padding, angles, colors and text options",
    children: [node("Tooltip")],
    props: {
      dataKey: "size", cx: 250, cy: 220, innerRadius: 30, outerRadius: 170, ringPadding: 6, padding: 1,
      startAngle: 30, endAngle: 300, fill: "#8884d8", stroke: "#eee",
      textOptions: {fontSize: "11px", fontWeight: "normal", fill: "#fff", stroke: "none", fontFamily: "Inter"},
    },
  },
];
