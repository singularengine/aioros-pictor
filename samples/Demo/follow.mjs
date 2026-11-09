// Checks the tooltip following the pointer on a running Gallery (GALLERY, its LAN address) in Chromium and Firefox:
// the /tips page's charts "The box follows the pointer" and "Following the pointer, reversed" (Recharts'
// SimpleLineChart, as DetailsFollowLineChart and DetailsFollowReversedChart) are hovered at the reference examples'
// mouse positions, and the shown box's position in the chart is compared with Recharts' box after the same moves
// (tools/recharts/out/<name>.<i>.client.html), within a pixel (Pictor measures its box with Inter's metrics, the
// browser lays it out). Usage: GALLERY=<url> node samples/Demo/follow.mjs
import {readFileSync} from "node:fs";
import {dirname, join, resolve} from "node:path";
import {fileURLToPath} from "node:url";
import {chromium as playwright} from "../../tools/recharts/node_modules/playwright-core/index.mjs";
import {firefox, gallery} from "./browsers.mjs";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const charts = [["/tips", "The box follows the pointer", "DetailsFollowLineChart"], ["/tips", "Following the pointer, reversed", "DetailsFollowReversedChart"], ["/radar", "SpecifiedDomainRadarChart", "DetailsFollowRadarChart"], ["/radial-bar", "SimpleRadialBarChart", "DetailsFollowRadialBarChart"]];
const recharts = (name, i) => {
  const html = readFileSync(join(root, "tools/recharts/out", `${name}.${i}.client.html`), "utf8");
  const m = html.match(/recharts-tooltip-wrapper[^"]*" style="([^"]*)translate\(([-\d.]+)px, ([-\d.]+)px\)/);
  return m[1].includes("visibility: hidden") ? null : {x: +m[2], y: +m[3]};
};
const pointsOf = (name) => JSON.parse(readFileSync(join(root, "tools/recharts/examples", `${name}.json`), "utf8")).interactions.map((steps) => steps.at(-1).hover);

let failures = 0;
const browserA = await playwright.launch({executablePath: "/usr/bin/chromium"});
const drivers = [{name: "chromium", page: async () => (await browserA.newContext({viewport: {width: 1400, height: 1000}})).newPage(), quit: () => browserA.close()}];
const ff = await firefox();
drivers.push({name: "firefox", page: ff.page, quit: ff.quit});
for (const driver of drivers) {
  const page = await driver.page();
  for (const [path, title, name] of charts) {
    if (page.url() !== gallery + path) await page.goto(gallery + path);
    const box = await page.evaluate((t) => {
      const heading = [...document.querySelectorAll("*")].find((e) => e.childElementCount === 0 && e.textContent.trim() === t);
      const svg = heading.parentElement.querySelector("svg.recharts-surface");
      svg.scrollIntoView({block: "center"});
      const r = svg.getBoundingClientRect();
      return {x: r.x, y: r.y};
    }, title);
    for (const [i, [x, y]] of pointsOf(name).entries()) {
      await page.mouse.move(box.x + x, box.y + y);
      await page.evaluate(() => new Promise((ok) => requestAnimationFrame(() => requestAnimationFrame(ok))));
      const shown = await page.evaluate(({t}) => {
        const heading = [...document.querySelectorAll("*")].find((e) => e.childElementCount === 0 && e.textContent.trim() === t);
        const svg = heading.parentElement.querySelector("svg.recharts-surface");
        const s = svg.getBoundingClientRect();
        const tips = [...svg.querySelectorAll(".pictor-hover > [class^=pictor-tk]")].filter((g) => getComputedStyle(g).display !== "none");
        return tips.map((g) => { const r = g.querySelector(".recharts-default-tooltip").getBoundingClientRect(); return {x: +(r.x - s.x).toFixed(2), y: +(r.y - s.y).toFixed(2)}; });
      }, {t: title});
      const expected = recharts(name, i);
      const same = expected === null ? shown.length === 0 : shown.length === 1 && Math.abs(shown[0].x - expected.x) <= 1 && Math.abs(shown[0].y - expected.y) <= 1;
      failures += same ? 0 : 1;
      console.log(`${driver.name.padEnd(9)} ${name.padEnd(28)} at (${x}, ${y}): Pictor ${JSON.stringify(shown)} / Recharts ${JSON.stringify(expected)} ${same ? "same" : "DIFFERENT"}`);
    }
  }
  await driver.quit();
}
process.exit(failures ? 1 : 0);
