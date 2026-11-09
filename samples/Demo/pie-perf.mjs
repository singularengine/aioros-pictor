// Measures the cost of the pies' enter animations in the Gallery (GALLERY, e.g. http://127.0.0.1:3077) in Chromium
// and Firefox: each page's frame times while /pie and /radial-bar animate in (loaded, then reached by client
// navigation), what document.getAnimations() still holds at rest, a forced restyle's cost (40 restyles of the whole
// document), and whether pages visited after /pie are slower than before (frames, restyle, animations, elements, the
// browser's content processes' RSS; Chromium's Performance metrics). HEADED=1 shows the windows (Firefox's paint cost
// only shows headed), BROWSER picks one browser, LOOPS adds visits to /pie and /radial-bar, CYCLES navigation loops
// over every route. Usage: GALLERY=... node samples/Demo/pie-perf.mjs
import {chromium, firefox} from "../../tools/recharts/node_modules/playwright-core/index.mjs";
import {execSync} from "node:child_process";

const gallery = process.env.GALLERY ?? "http://127.0.0.1:3077";
const only = process.env.BROWSER;
const loops = Number(process.env.LOOPS ?? 0);
const others = (process.env.OTHERS ?? "/line,/bar,/area,/radar,/funnel,/scatter").split(",");

const frames = (page, ms) => page.evaluate((ms) => new Promise((ok) => {
  const t = []; const s = performance.now(); let last = s;
  const step = (now) => { t.push(now - last); last = now; if (now - s < ms) requestAnimationFrame(step); else ok(t); };
  requestAnimationFrame(step);
}), ms);
const stats = (t) => {
  const s = [...t].sort((a, b) => a - b);
  const p = (q) => s[Math.min(s.length - 1, Math.floor(q * s.length))];
  return {n: t.length, mean: +(t.reduce((a, b) => a + b, 0) / t.length).toFixed(1), p50: +p(.5).toFixed(1), p95: +p(.95).toFixed(1), max: +s[s.length - 1].toFixed(1), over33: t.filter((x) => x > 33).length};
};
const probe = (page) => page.evaluate(() => {
  const s = performance.now();
  const leaf = document.querySelectorAll("*"); const last = leaf[leaf.length - 1];
  for (let i = 0; i < 40; i++) { document.documentElement.style.setProperty("--probe", String(i)); getComputedStyle(last).getPropertyValue("--probe"); document.body.offsetHeight; }
  const recalc = performance.now() - s;
  const a = document.getAnimations();
  const states = {};
  for (const x of a) { const k = `${x.animationName ?? "?"}:${x.playState}:${x.effect?.getTiming().fill}`; states[k] = (states[k] ?? 0) + 1; }
  return {recalc40: +recalc.toFixed(1), animations: a.length, states, nodes: document.getElementsByTagName("*").length, styles: document.querySelectorAll("style").length, sheets: document.styleSheets.length};
});
const nav = async (page, path) => {
  await page.evaluate((p) => window.aioros.go(new URL(p, location).href, true), path);
  await page.waitForFunction((p) => location.pathname === p, path);
  await page.evaluate(() => new Promise((ok) => requestAnimationFrame(() => requestAnimationFrame(ok))));
};
const rss = (label) => {
  try {
    const main = execSync(`pgrep -n -f -- 'playwright_${label === "firefox" ? "firefox" : "chromium"}dev_profile'`).toString().trim().split("\n").pop();
    const rows = execSync("ps -eo pid=,ppid=,rss=,args=").toString().trim().split("\n").map((l) => { const m = l.trim().match(/^(\d+)\s+(\d+)\s+(\d+)\s+(.*)$/); return {pid: m[1], ppid: m[2], rss: Number(m[3]), args: m[4]}; });
    const tree = new Set([main]);
    for (let grew = true; grew;) { grew = false; for (const r of rows) if (!tree.has(r.pid) && tree.has(r.ppid)) { tree.add(r.pid); grew = true; } }
    const mine = rows.filter((r) => tree.has(r.pid));
    const content = mine.filter((r) => r.args.includes("isForBrowser") || r.args.includes("--type=renderer")).reduce((a, r) => a + r.rss, 0);
    const all = mine.reduce((a, r) => a + r.rss, 0);
    return `content ${Math.round(content / 1024)} MB, all ${Math.round(all / 1024)} MB`;
  } catch (e) { return String(e).slice(0, 80); }
};

for (const [label, type, options] of [["chromium", chromium, {executablePath: "/usr/bin/chromium"}], ["firefox", firefox, {executablePath: "/usr/bin/firefox", channel: "moz-firefox"}]]) {
  if (only && only !== label) continue;
  const browser = await type.launch({...options, headless: process.env.HEADED ? false : true});
  const page = await browser.newPage({viewport: {width: 1400, height: 1000}});
  const cdp = label === "chromium" ? await page.context().newCDPSession(page) : null;
  if (cdp) await cdp.send("Performance.enable");
  const metrics = async () => { if (!cdp) return null; const m = Object.fromEntries((await cdp.send("Performance.getMetrics")).metrics.map((x) => [x.name, x.value])); return {TaskMs: +(m.TaskDuration * 1000).toFixed(0), RecalcStyleCount: m.RecalcStyleCount, RecalcStyleMs: +(m.RecalcStyleDuration * 1000).toFixed(0), LayoutMs: +(m.LayoutDuration * 1000).toFixed(0), LayoutCount: m.LayoutCount, Nodes: m.Nodes, JSHeapMB: +(m.JSHeapUsedSize / 1048576).toFixed(1)}; };
  console.log(`== ${label}`);
  for (const start of ["/pie", "/radial-bar"]) {
    const t0 = Date.now();
    await page.goto(gallery + start, {waitUntil: "load"});
    const load = Date.now() - t0;
    const f = await frames(page, 2600);
    const fcp = await page.evaluate(() => performance.getEntriesByType("paint").find((e) => e.name === "first-contentful-paint")?.startTime ?? -1);
    const end = await page.evaluate(() => Math.round(performance.now()));
    console.log(`fresh ${start}: load ${load} ms, fcp ${Math.round(fcp)} ms, frames 0..2.6s`, JSON.stringify(stats(f)), "at", end, "ms", JSON.stringify(await probe(page)));
  }
  await page.goto(gallery + "/line", {waitUntil: "load"});
  const round = async (tag) => {
    const rows = [];
    for (const p of others) { await nav(page, p); const f = await frames(page, 800); rows.push({p, ...stats(f), ...(await probe(page))}); }
    const recalc = rows.reduce((a, r) => a + r.recalc40, 0) / rows.length;
    const mean = rows.reduce((a, r) => a + r.mean, 0) / rows.length;
    console.log(`${tag}: mean frame ${mean.toFixed(1)} ms, probe recalc40 ${recalc.toFixed(1)} ms, anims ${rows.map((r) => r.animations).join(",")}, nodes ${rows.map((r) => r.nodes).join(",")}`, process.env.STATES ? JSON.stringify(rows.map((r) => r.states)) : "", JSON.stringify(await metrics()), "rss", rss(label));
    return rows;
  };
  await round("before pie");
  for (const p of ["/pie", "/radial-bar"]) {
    const m0 = await metrics();
    await nav(page, p);
    const pf = await frames(page, 2600);
    const m1 = await metrics();
    const d = m0 ? Object.fromEntries(Object.keys(m1).map((k) => [k, +(m1[k] - m0[k]).toFixed(1)])) : null;
    console.log(`client nav ${p} enter frames`, JSON.stringify(stats(pf)), JSON.stringify(await probe(page)), "delta", JSON.stringify(d));
    await nav(page, "/line");
  }
  await round("after pie");
  const cycles = Number(process.env.CYCLES ?? 0);
  const routes = ["/", "/line", "/area", "/bar", "/composed", "/pie", "/radar", "/radial-bar", "/funnel", "/treemap", "/sankey", "/sunburst", "/scatter", "/features", "/axes", "/interactions", "/ranges", "/binding", "/tips", "/more", "/themes"];
  for (let i = 0; i < cycles; i++) {
    const t0 = Date.now();
    for (const r of routes) { await nav(page, r); await frames(page, 200); }
    const p = await probe(page);
    console.log(`cycle ${i + 1}: ${Date.now() - t0} ms, recalc40 ${p.recalc40} ms, rss ${rss(label)}`, JSON.stringify(await metrics()));
  }
  for (let i = 0; i < loops; i++) {
    await nav(page, "/pie"); await frames(page, 300); await nav(page, "/radial-bar"); await frames(page, 300);
    if (i % 10 === 9) await round(`after ${i + 1} pie loops`);
  }
  await browser.close();
}
