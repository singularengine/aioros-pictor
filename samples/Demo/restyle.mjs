// Measures what the hover markup's selectors cost a page of the Gallery (GALLERY, its LAN address) in Chromium and
// Firefox: the page's bytes and its `:has(` count, a forced restyle of the whole document (40 times), 40 focus moves
// among the charts' hit groups (each with a style flush: the focus rules' invalidation), a mouse sweep over every chart
// (Chromium: the restyle time it caused) and, on a live page, 10 s at rest while the server pushes (Chromium: restyle
// time and count). Usage: GALLERY=<url> node samples/Demo/restyle.mjs [paths]
import {chromium, firefox} from "../../tools/recharts/node_modules/playwright-core/index.mjs";

const gallery = process.env.GALLERY ?? "http://127.0.0.1:3077";
const paths = process.argv.slice(2).length ? process.argv.slice(2) : ["/", "/tips"];
const idle = Number(process.env.IDLE ?? 10000);
const results = {};
for (const [label, type, options] of [["chromium", chromium, {executablePath: "/usr/bin/chromium"}], ["firefox", firefox, {executablePath: "/usr/bin/firefox", channel: "moz-firefox"}]]) {
  if (process.env.BROWSER && process.env.BROWSER !== label) continue;
  const browser = await type.launch({...options, headless: true});
  const page = await browser.newPage({viewport: {width: 1400, height: 1000}});
  const cdp = label === "chromium" ? await page.context().newCDPSession(page) : null;
  if (cdp) await cdp.send("Performance.enable");
  const metrics = async () => cdp ? Object.fromEntries((await cdp.send("Performance.getMetrics")).metrics.map((x) => [x.name, x.value])) : null;
  for (const path of paths) {
    const html = await (await fetch(gallery + path)).text();
    await page.goto(gallery + path, {waitUntil: "load"});
    await page.waitForTimeout(2500);
    const probe = await page.evaluate(() => {
      const all = document.querySelectorAll("*"); const last = all[all.length - 1];
      let s = performance.now();
      for (let i = 0; i < 40; i++) { document.documentElement.style.setProperty("--probe", String(i)); getComputedStyle(last).getPropertyValue("--probe"); document.body.offsetHeight; }
      const recalc40 = performance.now() - s;
      const groups = [...document.querySelectorAll(".pictor-hits > g[tabindex], .pictor-hover > g[tabindex], .pictor-keys > g[tabindex]")];
      s = performance.now();
      for (let i = 0; i < 40 && groups.length; i++) { groups[(i * 7) % groups.length].focus({preventScroll: true, focusVisible: true}); document.body.offsetHeight; }
      const focus40 = performance.now() - s;
      document.activeElement?.blur();
      return {recalc40: +recalc40.toFixed(1), focus40: +focus40.toFixed(1), groups: groups.length, nodes: all.length};
    });
    const m0 = await metrics();
    const boxes = await page.evaluate(() => [...document.querySelectorAll(".recharts-surface:has(>.pictor-hover,>.pictor-hits)")].map((h) => { const r = h.getBoundingClientRect(); return {x: r.x + scrollX, y: r.y + scrollY, w: r.width, h: r.height}; }));
    let sweepStart = Date.now();
    for (const b of boxes) {
      await page.evaluate((y) => scrollTo(0, Math.max(0, y - 300)), b.y);
      const top = await page.evaluate(() => scrollY);
      for (let k = 0; k <= 16; k++) await page.mouse.move(b.x + (b.w * k) / 16, b.y - top + b.h / 2);
    }
    const sweepMs = Date.now() - sweepStart;
    const m1 = await metrics();
    await page.mouse.move(1399, 999);
    await page.evaluate(() => scrollTo(0, 0));
    let live = null;
    if (path === "/" && idle > 0) {
      const a = await metrics();
      await page.waitForTimeout(idle);
      const b = await metrics();
      live = a ? {recalcMs: +((b.RecalcStyleDuration - a.RecalcStyleDuration) * 1000).toFixed(0), recalcCount: b.RecalcStyleCount - a.RecalcStyleCount} : null;
    }
    const row = {bytes: html.length, has: (html.match(/:has\(/g) ?? []).length, ...probe, sweepMs, sweepRecalcMs: m0 ? +((m1.RecalcStyleDuration - m0.RecalcStyleDuration) * 1000).toFixed(0) : null, live};
    (results[label] ??= {})[path] = row;
    console.log(label.padEnd(9), path.padEnd(8), JSON.stringify(row));
  }
  await browser.close();
}
