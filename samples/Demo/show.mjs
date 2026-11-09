// Times how long Chromium and Firefox take to show Pictor's markup of reference examples (the page of
// tools/recharts/browser.mjs: Inter 14px; innerHTML, then a forced style and layout, median of 60), for one or more
// output folders of `dotnet Demo.dll render` (to compare two states of the writer), with Recharts' mount in Chromium
// (React's flushSync, median of 200 after 50 warm-ups) beside them, and the markup's bytes.
// Usage: node samples/Demo/show.mjs <out>[,<out>...] [names]
import {readFileSync} from "node:fs";
import {join} from "node:path";
import {open, font} from "../../tools/recharts/browser.mjs";
import {firefox} from "./browsers.mjs";

const outs = process.argv[2].split(",");
const names = process.argv.slice(3).length ? process.argv.slice(3) : ["ColorPanelTreemap", "BudgetSankey", "SimpleSankey", "SimpleTreemap", "SimpleLineChart", "BrushBarChart"];
const session = await open();
const time = (page, html) => page.evaluate(async (markup) => {
  const host = document.getElementById("host");
  const times = [];
  for (let i = 0; i < 60; i++) {
    host.innerHTML = "";
    document.body.offsetHeight;
    const start = performance.now();
    host.innerHTML = markup;
    host.getBoundingClientRect();
    document.body.offsetHeight;
    times.push(performance.now() - start);
  }
  times.sort((a, b) => a - b);
  host.innerHTML = "";
  return +times[times.length >> 1].toFixed(3);
}, html);
const ff = await firefox();
const ffPage = await ff.page();
await ffPage.goto(session.page.url());
await ffPage.waitForFunction(() => window.pictor !== undefined);
await ffPage.evaluate(async () => { await document.fonts.load("400 14px Inter"); await document.fonts.ready; });
for (const name of names) {
  const recharts = await session.page.evaluate(([n]) => window.pictor.bench(n, 50, 200), [name]).then((t) => { const s = [...t].sort((a, b) => a - b); return +s[s.length >> 1].toFixed(3); }).catch(() => null);
  const cells = [];
  for (const out of outs) {
    const html = readFileSync(join(out, "pictor", `${name}.html`), "utf8");
    cells.push(`${Buffer.byteLength(html)} B, Chromium ${await time(session.page, html)} ms, Firefox ${await time(ffPage, html)} ms`);
  }
  console.log(`${name.padEnd(20)} Recharts mount ${recharts} ms | ${cells.join(" | ")}`);
}
await ff.quit();
await session.close();
