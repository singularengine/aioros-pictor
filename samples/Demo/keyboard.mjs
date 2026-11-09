// Drives Recharts' accessibilityLayer and Pictor's web chart with the keyboard, in Chromium (Playwright) and Firefox
// (puppeteer-core over WebDriver BiDi). Recharts: focus on the surface, then its AccessibilityManager's keys. Pictor:
// Tab from a button before the chart (the chart's one tab stop), then the same keys, moved by the framework's roving
// focus. The keys: ArrowRight, ArrowRight, ArrowLeft, four more ArrowLeft (clamped at the first category), ArrowDown
// (nothing) and Escape (hides the tooltip's box; the cursor and active dots stay). Each step compares the shown
// tooltip's text, whether a cursor shows and how many active dots; Pictor must show one box at most. Pictor's markup is put in Recharts' reference page in place of Recharts' chart, with the framework's script of
// a running Gallery, read from GALLERY (the Gallery's LAN address).
// Usage: GALLERY=<url> node samples/Demo/keyboard.mjs <out> [names]
// (Pictor's markup in <out>/accessible, from `dotnet Demo.dll accessible`)
import {readFileSync, writeFileSync} from "node:fs";
import {dirname, join, resolve} from "node:path";
import {fileURLToPath} from "node:url";
import {open} from "../../tools/recharts/browser.mjs";
import {chromium, firefox, frameworkScript, pictorTip, rechartsState} from "./browsers.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, "../..");
const out = resolve(process.argv[2] ?? join(here, "out"));
const names = process.argv.slice(3).length ? process.argv.slice(3) : ["SimpleLineChart", "SimpleAreaChart", "SimpleBarChart", "LineBarAreaComposedChart", "TooltipDefaultIndex", "VerticalBarChart", "SimpleScatterChart", "TwoSimplePieChart", "SpecifiedDomainRadarChart", "SimpleRadialBarChart", "FunnelChart"];
const keys = ["ArrowRight", "ArrowRight", "ArrowLeft", "ArrowLeft", "ArrowLeft", "ArrowLeft", "ArrowLeft", "ArrowDown", "Escape"];
const script = await frameworkScript();
const session = await open();
const url = session.page.url();

const results = {};
for (const driver of [chromium(session), await firefox()]) {
  const rows = [];
  for (const name of names) {
    const description = JSON.parse(readFileSync(join(root, "tools/recharts/examples", `${name}.json`), "utf8"));
    description.chartProps = {...(description.chartProps ?? {}), accessibilityLayer: true};
    const recharts = await driver.page();
    await driver.goto(recharts, url);
    await recharts.waitForFunction(() => window.pictor !== undefined);
    await recharts.evaluate((d) => window.pictor.motionMount(d), description);
    await driver.front(recharts);
    await recharts.evaluate(() => document.querySelector("#host .recharts-surface").focus());
    const r = [await rechartsState(recharts)];
    for (const key of keys) {
      await driver.press(recharts, key);
      r.push(await rechartsState(recharts));
    }
    await driver.close(recharts);

    const pictor = await driver.page();
    await driver.goto(pictor, url);
    await pictor.waitForFunction(() => window.pictor !== undefined);
    await pictor.evaluate((html) => { document.getElementById("host").outerHTML = `<button id="start">start</button><div id="host">${html}</div>`; }, readFileSync(join(out, "accessible", `${name}.html`), "utf8"));
    await pictor.addScriptTag({content: script});
    await driver.front(pictor);
    await pictor.evaluate(() => document.getElementById("start").focus());
    await driver.press(pictor, "Tab");
    const p = [await pictorTip(pictor)];
    for (const key of keys) {
      await driver.press(pictor, key);
      p.push(await pictorTip(pictor));
    }
    const stops = await pictor.evaluate(() => document.querySelectorAll("#host [tabindex='0'], #host [tabindex='00']").length);
    await driver.close(pictor);

    const steps = ["focus", ...keys].map((step, i) => ({step, pictor: p[i], recharts: r[i], same: p[i].text === r[i].text && p[i].count <= 1 && p[i].cursor === r[i].cursor && p[i].dots === r[i].dots}));
    rows.push({name, stops, steps});
    console.log(`${driver.name.padEnd(9)} ${name.padEnd(26)} ${stops} tab stop  ${steps.every((s) => s.same) ? `same at all ${steps.length} steps` : steps.filter((s) => !s.same).map((s) => `${s.step}: Pictor ${JSON.stringify(s.pictor)} / Recharts ${JSON.stringify(s.recharts)}`).join(" | ")}`);
  }
  results[driver.name] = {version: driver.version, rows};
  await driver.quit();
}
writeFileSync(join(out, "keyboard.json"), JSON.stringify(results, null, 1));
await session.close();
