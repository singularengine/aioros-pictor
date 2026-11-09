import {createRoot} from "react-dom/client";
import {flushSync, render as legacyRender} from "react-dom";
import {examples, element} from "./examples.mjs";

const frame = () => new Promise((resolve) => requestAnimationFrame(() => resolve()));
const byName = new Map(examples.map((example) => [example.name, example]));

const shapeSelector = "path.recharts-curve, path.recharts-rectangle, path.recharts-symbols, path.recharts-sector, path.recharts-polygon, path.recharts-trapezoid";
let motionHost = null;

window.pictor = {
  names: examples.map((example) => example.name),

  /** Mounts a description with React's legacy root, which renders a state change synchronously (each animation frame is in the DOM when its callback returns). */
  motionMount(description) {
    const host = document.getElementById("host");
    host.replaceChildren();
    motionHost = document.createElement("div");
    host.append(motionHost);
    legacyRender(element(description), motionHost);
  },

  /** Renders the mounted chart again from another description: Recharts' update. */
  motionUpdate(description) {
    legacyRender(element(description), motionHost);
  },

  /** The series' shapes in the chart's surface, in document order, as markup of their class, path and transform. */
  shapes() {
    const surface = motionHost.querySelector(".recharts-wrapper > svg");
    return [...surface.querySelectorAll(shapeSelector)]
      .filter((shape) => !shape.closest(".recharts-tooltip-cursor, .recharts-active-dot, .recharts-legend-wrapper"))
      .map((shape) => `<path class="${shape.getAttribute("class")}" d="${shape.getAttribute("d") ?? ""}"${shape.hasAttribute("transform") ? ` transform="${shape.getAttribute("transform")}"` : ""}/>`).join("");
  },

  async mount(name) {
    const host = document.getElementById("host");
    host.replaceChildren();
    const container = document.createElement("div");
    host.append(container);
    const root = createRoot(container);
    flushSync(() => root.render(element(byName.get(name))));
    await frame();
    await frame();
    const wrapper = container.querySelector(".recharts-wrapper") ?? container.querySelector(".recharts-surface");
    const legend = wrapper.querySelector(".recharts-legend-wrapper");
    const box = legend?.getBoundingClientRect();
    const style = getComputedStyle(wrapper);
    return {
      markup: wrapper.outerHTML,
      font: {family: style.fontFamily, size: style.fontSize, lineHeight: style.lineHeight},
      legend: box ? {width: box.width, height: box.height} : null,
    };
  },

  async settle() {
    await frame();
    await frame();
    return (document.querySelector("#host .recharts-wrapper") ?? document.querySelector("#host .recharts-surface")).outerHTML;
  },

  async bench(name, warmup, samples) {
    const host = document.getElementById("host");
    const example = byName.get(name);
    const once = () => {
      const container = document.createElement("div");
      host.replaceChildren(container);
      const root = createRoot(container);
      const start = performance.now();
      flushSync(() => root.render(element(example)));
      const elapsed = performance.now() - start;
      root.unmount();
      return elapsed;
    };
    for (let i = 0; i < warmup; i++) once();
    await frame();
    const times = [];
    for (let i = 0; i < samples; i++) times.push(once());
    return times;
  },
};
