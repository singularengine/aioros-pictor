// Compares Pictor's web enter animations with Recharts' own, frame by frame. Recharts runs in Chromium with Playwright's
// clock driving its animation frames (mounted at clock 0, its frames every 16 ms); Pictor's markup (written animated by
// `dotnet Demo.dll animated`) is inserted in Chromium and in Firefox (WebDriver BiDi, `moz-firefox`) and its CSS
// animations are paused through the Web Animations API at the same times. At each time the share of inked pixels that
// differ strongly (more than 64 in a channel) from Recharts' frame is reported, with the frames as PNG files.
// Usage: node samples/Demo/enter.mjs <out> [Example...]
import {chromium, firefox} from "../../tools/recharts/node_modules/playwright-core/index.mjs";
import {mkdirSync, readFileSync, writeFileSync} from "node:fs";
import {dirname, join, resolve} from "node:path";
import {fileURLToPath} from "node:url";
import {open} from "../../tools/recharts/browser.mjs";
import {animated} from "../../tools/recharts/catalog/motion.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, "../..");
const out = resolve(process.argv[2] ?? join(here, "out"));
const names = process.argv.slice(3).length ? process.argv.slice(3) : ["SimpleLineChart", "SimpleAreaChart", "SimpleBarChart", "SimpleScatterChart", "TwoLevelPieChart", "StraightAnglePieChart", "SimpleRadarChart", "SimpleRadialBarChart", "FunnelChart", "SimpleTreemap"];
const times = [0, 96, 192, 304, 400, 496, 608, 704, 800, 992, 1200, 1392, 1600, 1792, 2000, 2208];
const shots = join(out, "enter");
mkdirSync(shots, {recursive: true});
const font = readFileSync(join(root, "tools/recharts/fonts/Inter-Regular-latin.woff2")).toString("base64");
const page = (body) => `<!doctype html><html><head><meta charset="utf-8"><style>@font-face{font-family:Inter;src:url(data:font/woff2;base64,${font}) format("woff2")}html,body{margin:0;background:#fff;font-family:Inter;font-size:14px}#host{display:inline-block}</style></head><body><div id="host">${body}</div></body></html>`;

async function pictorFrames(browser, label) {
  const tab = await browser.newPage({viewport: {width: 800, height: 600}});
  const frames = {};
  for (const name of names) {
    const example = JSON.parse(readFileSync(join(root, "tools/recharts/examples", `${name}.json`), "utf8"));
    await tab.setContent(page(readFileSync(join(out, "animated", `${name}.html`), "utf8")));
    await tab.evaluate(() => document.fonts.ready);
    frames[name] = [];
    for (const time of times) {
      await tab.evaluate((t) => document.getAnimations().forEach((a) => { a.pause(); a.currentTime = t; }), time);
      const path = join(shots, `${name}-${label}-${time}.png`);
      await tab.screenshot({path, clip: {x: 0, y: 0, width: example.width, height: example.height}});
      frames[name].push(readFileSync(path).toString("base64"));
    }
  }
  await tab.close();
  return frames;
}

const session = await open();
await session.page.clock.install({time: 0});
await session.page.clock.pauseAt(1000);
const recharts = {};
for (const name of names) {
  const example = animated(JSON.parse(readFileSync(join(root, "tools/recharts/examples", `${name}.json`), "utf8")));
  await session.page.evaluate((description) => window.pictor.motionMount(description), example);
  recharts[name] = [];
  let at = 0;
  for (const time of times) {
    await session.page.clock.runFor(time - at);
    at = time;
    const path = join(shots, `${name}-recharts-${time}.png`);
    await session.page.screenshot({path, clip: {x: 0, y: 0, width: example.width, height: example.height}});
    recharts[name].push(readFileSync(path).toString("base64"));
  }
  await session.page.evaluate(() => window.pictor.motionMount({...{kind: "LineChart", width: 1, height: 1, children: []}}));
}
const chromiumFrames = await pictorFrames(session.browser, "chromium");
const fox = await firefox.launch({executablePath: process.env.FIREFOX ?? "/usr/bin/firefox", channel: "moz-firefox"});
const firefoxFrames = await pictorFrames(fox, "firefox");
await fox.close();

const compare = async (a, b) => session.page.evaluate(async ([left, right]) => {
  const load = (data) => new Promise((ok) => { const image = new Image(); image.onload = () => ok(image); image.src = `data:image/png;base64,${data}`; });
  const read = (image) => { const c = new OffscreenCanvas(image.width, image.height); const g = c.getContext("2d"); g.drawImage(image, 0, 0); return g.getImageData(0, 0, image.width, image.height).data; };
  const [x, y] = (await Promise.all([load(left), load(right)])).map(read);
  let inked = 0, strong = 0;
  for (let i = 0; i < x.length; i += 4) {
    const ink = Math.min(x[i], x[i + 1], x[i + 2]) < 250 || Math.min(y[i], y[i + 1], y[i + 2]) < 250;
    if (!ink) continue;
    inked++;
    if (Math.max(Math.abs(x[i] - y[i]), Math.abs(x[i + 1] - y[i + 1]), Math.abs(x[i + 2] - y[i + 2])) > 64) strong++;
  }
  return inked ? Math.round(strong / inked * 1000) / 10 : 0;
}, [a, b]);

const results = {};
for (const name of names) {
  results[name] = [];
  for (let i = 0; i < times.length; i++) {
    results[name].push({time: times[i], chromium: await compare(recharts[name][i], chromiumFrames[name][i]), firefox: await compare(recharts[name][i], firefoxFrames[name][i])});
  }
  console.log(`${name.padEnd(24)} ${results[name].map((r) => `${r.time}:${r.chromium}/${r.firefox}`).join(" ")}`);
}
writeFileSync(join(out, "enter.json"), JSON.stringify({chromium: session.version, times, results}, null, 1));
await session.close();
