// Checks the framework's touch follow, roving focus and theme switch on a running Gallery, reached at its address
// (GALLERY, e.g. http://127.0.0.1:3077), in Chromium and Firefox:
// - /interactive's accessible line chart: a finger dragged across three categories moves its tooltip and lifting keeps
//   it, a tap outside hides it; its one tab stop takes the focus and ArrowRight, ArrowRight, ArrowLeft, Home and Escape
//   move and hide the tooltip;
// - /features' vertical line chart (data-aioros-follow="capture", touch-action:none): a vertical drag moves its tooltip
//   and does not scroll the page;
// - the theme switched by the Gallery's link (/theme/dark and /theme/light, or /theme/toggle) on every chart page and
//   back: every chart SVG and every element in it is
//   kept, and no CSS animation runs on a kept element afterwards (document.getAnimations()).
// Usage: GALLERY=<url> node samples/Demo/gallery-focus.mjs
import {chromium as playwright} from "../../tools/recharts/node_modules/playwright-core/index.mjs";
import {chromium, firefox, gallery} from "./browsers.mjs";

const pages = ["/", "/line", "/area", "/bar", "/composed", "/scatter", "/pie", "/radar", "/radial-bar", "/funnel", "/treemap", "/sankey", "/sunburst", "/interactive", "/interactions", "/tips", "/features"];
const pause = (ms = 250) => new Promise((ok) => setTimeout(ok, ms));
const settled = (driver, page) => driver.wait(page, () => document.getAnimations().every((a) => a.playState !== "running" || a.effect?.getComputedTiming().iterations === Infinity), undefined, 15000).catch(() => {});

/** The shown tip's index in the chart whose hit layer matches the selector, -1 when none, -2 when more than one. */
const shown = (page, layer) => page.evaluate(async (selector) => {
  await new Promise((ok) => requestAnimationFrame(() => requestAnimationFrame(ok)));
  const wrapper = document.querySelector(selector).closest(".recharts-wrapper");
  const tips = [...wrapper.querySelectorAll(".pictor-hover > [class^=pictor-tk]")];
  const visible = tips.map((tip, i) => (getComputedStyle(tip).display !== "none" ? i : -1)).filter((i) => i >= 0);
  return visible.length > 1 ? -2 : visible[0] ?? -1;
}, layer);

const centers = (page, layer) => page.evaluate((selector) => {
  const hits = document.querySelector(selector);
  hits.closest(".recharts-wrapper").scrollIntoView({block: "center"});
  return [...hits.children].filter((g) => !g.matches("[class^=pictor-tk]")).map((g) => { const b = g.querySelector(".pictor-hit").getBoundingClientRect(); return {x: Math.round(b.x + b.width / 2), y: Math.round(b.y + b.height / 2)}; });
}, layer);

async function drag(driver, page, layer, indices) {
  const at = await centers(page, layer);
  const seen = [];
  const finger = await driver.touches(page);
  await finger.start(at[indices[0]]);
  await pause();
  seen.push(await shown(page, layer));
  for (const i of indices.slice(1)) {
    await finger.move(at[i]);
    await pause();
    seen.push(await shown(page, layer));
  }
  await finger.end();
  seen.push(await shown(page, layer));
  return seen;
}

async function touch(driver) {
  const page = await driver.page();
  await driver.goto(page, gallery + "/interactive");
  await settled(driver, page);
  const roving = "[data-aioros-roving]";
  const followed = await drag(driver, page, roving, [2, 3, 4]);
  await driver.tap(page, {x: 5, y: 5});
  const outside = await shown(page, roving);
  const buttons = await page.evaluate(() => {
    const surface = [...document.querySelectorAll("svg.recharts-surface[data-aioros-follow]")].find((svg) => svg.querySelector(".pictor-cmds button[tabindex]"));
    surface.closest(".recharts-wrapper").scrollIntoView({block: "center"});
    return [2, 3, 4].map((k) => { const b = surface.querySelector(`.pictor-h${k}`).getBoundingClientRect(); return {x: Math.round(b.x + b.width / 2), y: Math.round(b.y + b.height / 2)}; });
  });
  const commandsLayer = "svg.recharts-surface[data-aioros-follow] .pictor-hover";
  await driver.tap(page, buttons[0]);
  const overButtons = [await shown(page, commandsLayer)];
  const finger = await driver.touches(page);
  await finger.start(buttons[0]);
  await pause();
  for (const at of buttons.slice(1)) {
    await finger.move(at);
    await pause();
    overButtons.push(await shown(page, commandsLayer));
  }
  await finger.end();
  await driver.tap(page, {x: 5, y: 5});
  const synced = {};
  for (const id of ["pages", "values"]) {
    const layers = await page.evaluate((c) => [...document.querySelectorAll(`.${c} .pictor-hover`)].length, `pictor-y-${id}`);
    await page.evaluate((c) => { document.querySelectorAll(`.${c} .pictor-hover`).forEach((l, i) => l.setAttribute("data-check", `${c}-${i}`)); }, `pictor-y-${id}`);
    const dragged = await drag(driver, page, `[data-check=pictor-y-${id}-0]`, [1, 2, 3]);
    const other = await shown(page, `[data-check=pictor-y-${id}-1]`);
    await driver.tap(page, {x: 5, y: 5});
    synced[id] = {layers, dragged, other};
  }
  await driver.close(page);

  const features = await driver.page();
  await driver.goto(features, gallery + "/features");
  await settled(driver, features);
  const capture = "[data-aioros-follow=capture]";
  const action = await features.evaluate((s) => getComputedStyle(document.querySelector(s)).touchAction, capture);
  await centers(features, capture);
  const before = await features.evaluate(() => scrollY);
  const vertical = await drag(driver, features, capture, [1, 3, 5]);
  const scrolled = (await features.evaluate(() => scrollY)) - before;
  await driver.close(features);
  return {followed, outside, overButtons, synced, capture: {touchAction: action, vertical, scrolled}};
}

async function keyboard(driver) {
  const page = await driver.page();
  await driver.goto(page, gallery + "/interactive");
  await settled(driver, page);
  const roving = "[data-aioros-roving]";
  const stops = await page.evaluate((s) => {
    const wrapper = document.querySelector(s).closest(".recharts-wrapper");
    return {stops: wrapper.querySelectorAll("[tabindex='00'],[tabindex='0']").length, surface: wrapper.querySelector(".recharts-surface").getAttribute("tabindex")};
  }, roving);
  await driver.front(page);
  await page.evaluate((s) => document.querySelector(`${s} [tabindex='00']`).focus(), roving);
  const seen = [await shown(page, roving)];
  for (const key of ["ArrowRight", "ArrowRight", "ArrowLeft", "Home", "Escape"]) {
    await driver.press(page, key);
    seen.push(await shown(page, roving));
  }
  const escaped = await page.evaluate((s) => {
    const wrapper = document.querySelector(s).closest(".recharts-wrapper");
    const tips = [...wrapper.querySelectorAll(".pictor-hover > [class^=pictor-tk]")].filter((t) => getComputedStyle(t).display !== "none");
    return {box: tips.some((t) => getComputedStyle(t.querySelector("foreignObject")).display !== "none"), cursor: [...wrapper.querySelectorAll(".pictor-cursors > g")].some((c) => getComputedStyle(c).display !== "none"), dots: tips.reduce((n, t) => n + t.querySelectorAll("circle").length, 0)};
  }, roving);
  await driver.close(page);
  return {...stops, seen, escaped};
}

async function themes(driver) {
  const page = await driver.page();
  const rows = [];
  for (const path of pages) {
    await driver.goto(page, gallery + path);
    await settled(driver, page);
    for (const theme of ["dark", "light"]) {
      await page.evaluate(() => {
        window.__svgs = [...document.querySelectorAll("svg.recharts-surface")];
        window.__kept = new Set(window.__svgs.flatMap((svg) => [svg, ...svg.querySelectorAll("*")]));
      });
      await page.evaluate((t) => (document.querySelector(`a[href="/theme/${t}"]`) ?? document.querySelector('a[href="/theme/toggle"]')).click(), theme);
      await driver.wait(page, (t) => getComputedStyle(document.documentElement).colorScheme.includes(t), theme);
      await pause(400);
      rows.push(await page.evaluate(([p, t]) => {
        const svgs = [...document.querySelectorAll("svg.recharts-surface")];
        const kept = [...window.__kept].filter((e) => e.isConnected).length;
        const running = document.getAnimations().filter((a) => a.playState === "running" && window.__kept.has(a.effect?.target));
        return {path: p, theme: t, charts: window.__svgs.length, chartsKept: svgs.filter((s) => window.__svgs.includes(s)).length, elements: window.__kept.size, elementsKept: kept, animationsOnKept: running.filter((a) => a instanceof CSSAnimation).map((a) => a.animationName), transitionsOnKept: running.filter((a) => a instanceof CSSTransition).length};
      }, [path, theme]));
    }
  }
  await driver.close(page);
  return rows;
}

const browser = await playwright.launch({executablePath: process.env.CHROMIUM ?? "/usr/bin/chromium"});
const report = {};
for (const make of [async () => chromium({browser, version: browser.version()}, {touch: true}), () => firefox({touch: true})]) {
  const driver = await make();
  const result = {version: driver.version, touch: await touch(driver), keyboard: await keyboard(driver), themes: await themes(driver)};
  report[driver.name] = result;
  const t = result.touch;
  console.log(`${driver.name} ${driver.version}`);
  console.log(`  touch on /interactive: tooltip at ${t.followed.join(" -> ")} (dragged over 2, 3, 4, then lifted), tap outside: ${t.outside}`);
  console.log(`  command buttons ("Click a bar" on /interactive): tap then drag over the bars of categories 2, 3, 4: tooltip at ${t.overButtons.join(" -> ")}`);
  for (const [id, r] of Object.entries(t.synced)) console.log(`  synced (${id}) on /interactive: dragged chart at ${r.dragged.join(" -> ")}, the other chart at ${r.other} after the drag`);
  console.log(`  capture on /features: touch-action ${t.capture.touchAction}, tooltip at ${t.capture.vertical.join(" -> ")} (dragged over 1, 3, 5), page scrolled ${t.capture.scrolled}px`);
  console.log(`  keyboard on /interactive: ${result.keyboard.stops} tab stop, surface tabindex ${result.keyboard.surface}, tooltip at ${result.keyboard.seen.join(" -> ")} (focus, ArrowRight, ArrowRight, ArrowLeft, Home, Escape); after Escape box ${result.keyboard.escaped.box}, cursor ${result.keyboard.escaped.cursor}, dots ${result.keyboard.escaped.dots}`);
  const bad = result.themes.filter((r) => r.chartsKept !== r.charts || r.elementsKept !== r.elements || r.animationsOnKept.length);
  console.log(`  theme switch: ${result.themes.length} switches on ${pages.length} pages, ${result.themes.reduce((s, r) => s + r.chartsKept, 0)} of ${result.themes.reduce((s, r) => s + r.charts, 0)} chart SVGs and ${result.themes.reduce((s, r) => s + r.elementsKept, 0)} of ${result.themes.reduce((s, r) => s + r.elements, 0)} elements kept, enter animations replayed on kept elements: ${result.themes.reduce((s, r) => s + r.animationsOnKept.length, 0)}${bad.length ? `; not kept: ${JSON.stringify(bad)}` : ""}`);
  await driver.quit();
}
await browser.close();
console.log(JSON.stringify(report));
