// What touch.mjs, keyboard.mjs and gallery-focus.mjs share: a Chromium driver (Playwright, CDP touch events) and a
// Firefox driver (puppeteer-core over WebDriver BiDi, touch events enabled), the framework's script read from a running
// Gallery (GALLERY, its LAN address), and the tooltip each chart shows.
import {existsSync} from "node:fs";

const puppeteerPath = process.env.PUPPETEER ?? "../../artifacts/firefox-tool/node_modules/puppeteer-core/lib/esm/puppeteer/puppeteer-core.js";
export const gallery = process.env.GALLERY ?? "http://127.0.0.1:3077";

/** The framework's inline script, as the Gallery serves it (the touch follow and the roving focus among its handlers). */
export async function frameworkScript() {
  const html = await (await fetch(gallery + "/")).text();
  const script = [...html.matchAll(/<script>([\s\S]*?)<\/script>/g)].map((m) => m[1]).find((s) => s.includes("data-aioros-follow"));
  if (!script) throw new Error(`no framework script with the touch follow at ${gallery}`);
  return script;
}

const clean = (text) => (text ?? "").replace(/\s+/g, " ").trim();

/** Pictor's shown tips (read a quarter of a second after the input, as Recharts' throttled handlers settle): how many
 * show a box, their texts, whether a cursor shows and how many active dots. */
export async function pictorTip(page, root = "body") {
  const tip = await page.evaluate(async (scope) => {
    await new Promise((ok) => setTimeout(ok, 250));
    await new Promise((ok) => requestAnimationFrame(() => requestAnimationFrame(ok)));
    const host = document.querySelector(scope);
    const shown = [...host.querySelectorAll(".pictor-hover > [class^=pictor-tk]")].filter((t) => getComputedStyle(t).display !== "none");
    const boxes = shown.map((t) => t.querySelector("foreignObject")).filter((f) => f && getComputedStyle(f).display !== "none");
    const cursor = [...host.querySelectorAll(".pictor-cursors > g")].some((c) => getComputedStyle(c).display !== "none") || [...host.querySelectorAll(".pictor-keys .recharts-tooltip-cursor")].some((c) => getComputedStyle(c).visibility === "visible");
    const dots = shown.reduce((n, t) => n + [...t.querySelectorAll("circle")].filter((c) => getComputedStyle(c).display !== "none").length, 0);
    return {count: boxes.length, text: boxes.map((f) => f.querySelector(".recharts-default-tooltip")?.textContent ?? "").join(" + "), cursor, dots};
  }, root);
  return {count: tip.count, text: clean(tip.text), cursor: tip.cursor, dots: tip.dots};
}

/** Recharts' visible default tooltip's text, empty when hidden. */
export async function rechartsTip(page) {
  return (await rechartsState(page)).text;
}

/** Recharts' visible default tooltip's text, whether its cursor draws anything and how many active dots. */
export async function rechartsState(page) {
  const state = await page.evaluate(async () => {
    await new Promise((ok) => setTimeout(ok, 250));
    await new Promise((ok) => requestAnimationFrame(() => requestAnimationFrame(ok)));
    const wrapper = [...document.querySelectorAll("#host .recharts-tooltip-wrapper")].find((w) => !w.closest("svg"));
    const text = wrapper && getComputedStyle(wrapper).visibility !== "hidden" ? wrapper.querySelector(".recharts-default-tooltip")?.textContent ?? "" : "";
    return {text, cursor: [...document.querySelectorAll("#host .recharts-tooltip-cursor")].some((c) => { const b = c.getBBox(); return b.width > 0 || b.height > 0; }), dots: document.querySelectorAll("#host .recharts-active-dot circle").length};
  });
  return {...state, text: clean(state.text)};
}

export function chromium(session, {touch = false} = {}) {
  return {
    name: "chromium",
    version: session.version,
    async page() {
      const context = await session.browser.newContext({hasTouch: touch, viewport: {width: 900, height: 700}, deviceScaleFactor: 1});
      return context.newPage();
    },
    close: (page) => page.context().close(),
    quit: async () => {},
    goto: (page, target) => page.goto(target),
    front: (page) => page.bringToFront(),
    wait: (page, fn, arg, timeout = 10000) => page.waitForFunction(fn, arg, {timeout, polling: 100}),
    tap: (page, p) => page.touchscreen.tap(p.x, p.y),
    async touches(page) {
      const cdp = await page.context().newCDPSession(page);
      const send = (type, p) => cdp.send("Input.dispatchTouchEvent", {type, touchPoints: p ? [{x: p.x, y: p.y}] : []});
      return {start: (p) => send("touchStart", p), move: (p) => send("touchMove", p), end: async () => { await send("touchEnd"); await cdp.detach(); }};
    },
    click: (page, p) => page.mouse.click(p.x, p.y),
    move: (page, p) => page.mouse.move(p.x, p.y),
    press: (page, key) => page.keyboard.press(key),
  };
}

export async function firefox({touch = false} = {}) {
  if (!existsSync(puppeteerPath)) throw new Error(`puppeteer-core not found at ${puppeteerPath}: set PUPPETEER`);
  const {default: puppeteer} = await import(puppeteerPath);
  const prefs = touch ? {"dom.w3c_touch_events.enabled": 1, "dom.w3c_touch_events.legacy_apis.enabled": true} : {};
  const browser = await puppeteer.launch({browser: "firefox", executablePath: process.env.FIREFOX ?? "/usr/bin/firefox", headless: true, extraPrefsFirefox: prefs});
  return {
    name: "firefox",
    version: await browser.version(),
    async page() {
      const p = await browser.newPage();
      await p.setViewport({width: 900, height: 700, hasTouch: touch});
      return p;
    },
    close: (p) => p.close(),
    quit: () => browser.close(),
    goto: (p, target) => p.goto(target),
    front: (p) => p.bringToFront(),
    wait: (p, fn, arg, timeout = 10000) => p.waitForFunction(fn, {timeout, polling: 100}, arg),
    tap: (p, at) => p.touchscreen.tap(at.x, at.y),
    async touches(p) {
      return {start: (at) => p.touchscreen.touchStart(at.x, at.y), move: (at) => p.touchscreen.touchMove(at.x, at.y), end: () => p.touchscreen.touchEnd()};
    },
    click: (p, at) => p.mouse.click(at.x, at.y),
    move: (p, at) => p.mouse.move(at.x, at.y),
    press: (p, key) => p.keyboard.press(key),
  };
}

