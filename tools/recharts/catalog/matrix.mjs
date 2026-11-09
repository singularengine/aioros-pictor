const source = (what) => `Pictor's fidelity matrix: ${what}`;
const on = (name) => ({$click: name});

/** Adds props to the children of an example whose type is `type` (and, given, whose dataKey is `key`). */
const withProps = (children, type, props, key) => children.map((child) =>
  child.type === type && (key === undefined || child.props.dataKey === key) ? {...child, props: {...child.props, ...props}} : child);

/** The click examples of the fidelity matrix: reference examples with Recharts' click handlers recorded (`{$click}`), clicked where an item or the chart's zone is. */
export function matrix(byName) {
  const area = byName("SimpleAreaChart");
  const composed = byName("LineBarAreaComposedChart");
  const scatter = byName("SimpleScatterChart");
  const radar = byName("SpecifiedDomainRadarChart");
  const radial = byName("SimpleRadialBarChart");
  return [
    {
      ...area, name: "MatrixClickAreaChart", chartProps: {onClick: on("chart")},
      source: source("SimpleAreaChart with the chart's onClick and the area's activeDot onClick, clicked on the active dot and in a category's zone away from it"),
      children: withProps(area.children, "Area", {activeDot: {onClick: on("dot")}}),
      interactions: [[{hover: [196.667, 190]}, {click: [196.667, 190]}], [{hover: [333.333, 300]}, {click: [333.333, 300]}]],
    },
    {
      ...composed, name: "MatrixClickComposedChart", chartProps: {onClick: on("chart")},
      source: source("LineBarAreaComposedChart with the chart's onClick, the bar's and the scatter's onClick and the line's and area's activeDot onClick, clicked on a bar, on the line's active dot, on a scatter symbol and in a zone above everything"),
      children: withProps(withProps(withProps(withProps(composed.children, "Bar", {onClick: on("bar")}), "Scatter", {onClick: on("scatter")}), "Line", {activeDot: {onClick: on("line")}}), "Area", {activeDot: {onClick: on("area")}}),
      interactions: [[{hover: [246, 300]}, {click: [246, 300]}], [{hover: [180, 182.064]}, {click: [180, 182.064]}], [{hover: [113.333, 247.794]}, {click: [113.333, 247.794]}], [{hover: [390, 30]}, {click: [390, 30]}]],
    },
    {
      ...scatter, name: "MatrixClickScatterChart",
      source: source("SimpleScatterChart with the scatter's onClick, clicked on two symbols and beside them"),
      children: withProps(scatter.children, "Scatter", {onClick: on("scatter")}),
      interactions: [[{hover: [302.222, 185]}, {click: [302.222, 185]}], [{hover: [457.778, 102.5]}, {click: [457.778, 102.5]}], [{hover: [450, 330]}, {click: [450, 330]}]],
    },
    {
      ...radar, name: "MatrixClickRadarChart", chartProps: {...radar.chartProps, onClick: on("chart")},
      source: source("SpecifiedDomainRadarChart with the chart's onClick and both radars' activeDot onClick, clicked on Mike's and Lily's active dots and in an angle's sector away from the dots"),
      children: withProps(withProps(radar.children, "Radar", {activeDot: {onClick: on("mike")}}, "A"), "Radar", {activeDot: {onClick: on("lily")}}, "B"),
      interactions: [[{hover: [250, 80.64]}, {click: [250, 80.64]}], [{hover: [361.983, 135.347]}, {click: [361.983, 135.347]}], [{hover: [280, 230]}, {click: [280, 230]}]],
    },
    {
      ...radial, name: "MatrixClickRadialBarChart", chartProps: {...radial.chartProps, onClick: on("chart")},
      source: source("SimpleRadialBarChart with the chart's onClick and the radial bar's onClick, clicked on a bar and on a band's background"),
      children: withProps(radial.children, "RadialBar", {onClick: on("bar")}),
      interactions: [[{hover: [180, 150]}, {click: [180, 150]}], [{hover: [250, 60]}, {click: [250, 60]}]],
    },
  ];
}
