// Drives Recharts and Pictor's web charts with the same touch and mouse input, in Chromium (touch emulation, CDP touch
// events) and Firefox (WebDriver BiDi touch actions through puppeteer-core, touch events enabled): a tap on a hover
// state, a drag from it across the next two states and lifting the finger, a tap outside the chart; then, in the
// charts whose tooltip follows the axis, a mouse click on the first state, a move to the third and out of the chart.
// After each step it reads the tooltip each shows (Recharts' visible default tooltip, Pictor's shown tips) and compares
// their texts; Pictor must show one tip at most. The finger rests a quarter of a second at each point of a drag
// (Chromium holds back a touch move that follows the last one by less than 200 ms once the page may scroll). Each
// chart plays the sequence in a page of its own (a page that loses the window's focus loses its focused element's
// :focus, and a mouse moved earlier would leave the chart after a tap). Pictor's static markup is put in Recharts'
// reference page in place of Recharts' chart, with the framework's script of a running Gallery (its touch follow and
// roving focus), read from GALLERY (the Gallery's LAN address).
// Usage: GALLERY=<url> node samples/Demo/touch.mjs <out> [names]
// (Pictor's markup in <out>/pictor, from `dotnet Demo.dll render`)
import {readFileSync, writeFileSync} from "node:fs";
import {dirname, join, resolve} from "node:path";
import {fileURLToPath} from "node:url";
import {open} from "../../tools/recharts/browser.mjs";
import {chromium, firefox, frameworkScript, pictorTip, rechartsTip} from "./browsers.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const out = resolve(process.argv[2] ?? join(here, "out"));
const names = process.argv.slice(3).length ? process.argv.slice(3) : ["SimpleLineChart", "SimpleAreaChart", "SimpleBarChart", "LineBarAreaComposedChart", "VerticalBarChart", "SimpleScatterChart", "TwoSimplePieChart", "SpecifiedDomainRadarChart", "SimpleRadialBarChart", "FunnelChart", "ColorPanelTreemap", "SimpleSankey", "SimpleSunburst"];
const script = await frameworkScript();
const session = await open();
const url = session.page.url();
const locate = (page) => page.evaluate(() => {
  const layer = document.querySelector(".pictor-hover");
  if (!layer) return null;
  const hits = [...layer.children].filter((g) => !g.matches("[class^=pictor-tk]"));
  const found = new Map();
  const box = document.querySelector(".recharts-wrapper").getBoundingClientRect();
  for (let y = box.top + 2; y < box.bottom; y += 3) for (let x = box.left + 2; x < box.right; x += 3) {
    const hit = document.elementFromPoint(x, y)?.closest(".pictor-hover > *");
    const index = hits.indexOf(hit);
    if (index >= 0) found.set(index, [...(found.get(index) ?? []), {x, y}]);
  }
  const states = [...found.keys()].sort((a, b) => a - b);
  if (!states.length) return null;
  const at = (k) => states[Math.min(Math.floor(states.length / 2) + k, states.length - 1)];
  const inside = (state) => {
    const all = found.get(state);
    const cx = all.reduce((sum, p) => sum + p.x, 0) / all.length;
    const cy = all.reduce((sum, p) => sum + p.y, 0) / all.length;
    return all.reduce((best, p) => (Math.hypot(p.x - cx, p.y - cy) < Math.hypot(best.x - cx, best.y - cy) ? p : best));
  };
  return {follows: layer.hasAttribute("data-aioros-follow"), path: [0, 1, 2].map((k) => ({state: at(k), ...inside(at(k))})), outside: {x: box.right + 5, y: box.bottom + 5}};
});

/** Pictor's chart in Recharts' reference page in place of Recharts' host, with the framework's script: the same document, styles and fonts as the Recharts it is compared with. */
async function host(driver, page, markup) {
  await driver.goto(page, url);
  await page.waitForFunction(() => window.pictor !== undefined);
  await page.evaluate((html) => { document.getElementById("host").innerHTML = html; }, markup);
  await page.addScriptTag({content: script});
}

const pause = () => new Promise((ok) => setTimeout(ok, 250));

async function play(driver, page, points, read) {
  const [first, second, third] = points.path;
  const seen = [];
  await driver.front(page);
  const record = async (step) => seen.push({step, ...(await read())});
  await driver.tap(page, first);
  await record("tap");
  const finger = await driver.touches(page);
  await finger.start(first);
  let from = first;
  for (const [step, to] of [["drag to the next", second], ["drag to the one after", third]]) {
    for (const at of [{x: (from.x + to.x) / 2, y: (from.y + to.y) / 2}, to]) {
      await finger.move(at);
      await pause();
    }
    from = to;
    await record(step);
  }
  await finger.end();
  await record("lift");
  await driver.tap(page, points.outside);
  await record("tap outside");
  if (points.follows) {
    await driver.click(page, first);
    await record("mouse click");
    await driver.move(page, third);
    await record("mouse move");
    await driver.move(page, points.outside);
    await record("mouse out");
  }
  return seen;
}

const results = {};
for (const driver of [chromium(session, {touch: true}), await firefox({touch: true})]) {
  const rows = [];
  for (const name of names) {
    const pictor = await driver.page();
    await host(driver, pictor, readFileSync(join(out, "pictor", `${name}.html`), "utf8"));
    const points = await locate(pictor);
    if (!points) {
      await driver.close(pictor);
      console.log(`${driver.name.padEnd(9)} ${name.padEnd(26)} no hover states (no tooltip)`);
      continue;
    }
    const p = await play(driver, pictor, points, () => pictorTip(pictor));
    await driver.close(pictor);
    const recharts = await driver.page();
    await driver.goto(recharts, url);
    await recharts.waitForFunction(() => window.pictor !== undefined);
    await recharts.evaluate((n) => window.pictor.mount(n), name);
    const r = await play(driver, recharts, points, async () => ({text: await rechartsTip(recharts)}));
    await driver.close(recharts);
    const steps = p.map((s, i) => ({step: s.step, pictor: s.text, count: s.count, recharts: r[i].text, same: s.text === r[i].text && s.count <= 1}));
    rows.push({name, points, steps});
    console.log(`${driver.name.padEnd(9)} ${name.padEnd(26)} ${steps.every((s) => s.same) ? `same at all ${steps.length} steps` : steps.filter((s) => !s.same).map((s) => `${s.step}: Pictor "${s.pictor}" (${s.count} shown) / Recharts "${s.recharts}"`).join(" | ")}`);
  }
  results[driver.name] = {version: driver.version, rows};
  await driver.quit();
}
writeFileSync(join(out, "touch.json"), JSON.stringify(results, null, 1));
await session.close();
