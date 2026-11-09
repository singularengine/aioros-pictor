// Checks Pictor's web update animations against Recharts' in Chromium and Firefox, through a real post and morph. For
// each update case recorded by tools/recharts/motion.mjs: Recharts mounted in Chromium with the old data, its enter
// animation run, then rendered with the new data (Playwright's clock driving its frames); the Gallery's /updates page
// (run it first, its URL as the second argument, over a LAN address) loaded with the same old data, its enter
// animations finished, then "Change data" clicked: the post's answer is morphed by Aioros's framework script, whose
// morph behaviors move every declared shape frame by frame (Playwright's clock paused on a 16 ms frame, as Recharts').
// Reports the share of inked pixels that differ strongly at each time, per case and browser.
// Usage: node samples/Demo/updates.mjs <out> <gallery-url> [Case...]
import {chromium, firefox} from "../../tools/recharts/node_modules/playwright-core/index.mjs";
import {mkdirSync, readFileSync, writeFileSync} from "node:fs";
import {dirname, join, resolve} from "node:path";
import {fileURLToPath} from "node:url";
import {open} from "../../tools/recharts/browser.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, "../..");
const out = resolve(process.argv[2] ?? join(here, "out"));
const gallery = process.argv[3] ?? "http://127.0.0.1:3077";
const motion = join(root, "tools/recharts/out/motion");
const cases = ["LineValues", "LineMore", "LineFewer", "LineFromEmpty", "AreaValues", "StackedAreaMore", "BarMore", "VerticalBarMore", "ScatterMore", "PieValues", "RadarValues", "RadialBarValues", "FunnelMore"];
const names = process.argv.slice(4).length ? process.argv.slice(4) : cases;
const times = [0, 96, 192, 304, 400, 496, 608, 800, 992, 1200, 1392, 1600, 1792, 2000, 2208];
const shots = join(out, "updates", "shots");
mkdirSync(shots, {recursive: true});

async function pictorFrames(browser, label) {
  const page = await browser.newPage({viewport: {width: 1400, height: 1000}});
  await page.clock.install({time: 0});
  await page.goto(gallery + "/theme/light");
  const module = page.waitForResponse((response) => response.url().includes("/_aioros/morph/"));
  await page.goto(gallery + "/updates");
  await module;
  await page.clock.pauseAt(await page.evaluate(() => Date.now()) + 1000);
  await page.evaluate(async () => {
    await document.fonts.ready;
    document.getAnimations().forEach((animation) => animation.finish());
  });
  await page.mouse.move(1, 1);
  const now = await page.evaluate(() => performance.now());
  await page.clock.runFor((16 - (now % 16)) % 16);
  const posted = page.waitForResponse((response) => response.request().method() === "POST");
  await page.getByRole("button", {name: "Change data"}).click();
  await posted;
  await page.waitForFunction(() => document.querySelector("[data-aioros-morphing]") !== null, null, {polling: 10});
  const frames = Object.fromEntries(names.map((name) => [name, []]));
  let at = 0;
  for (const time of times) {
    await page.clock.runFor(time - at);
    at = time;
    for (const name of names) {
      const wrapper = page.locator(".recharts-wrapper").nth(cases.indexOf(name));
      const path = join(shots, `${name}-${label}-${time}.png`);
      await wrapper.screenshot({path, animations: "allow"});
      frames[name].push(readFileSync(path).toString("base64"));
    }
  }

  await page.close();
  return frames;
}

const session = await open();
await session.page.clock.install({time: 0});
await session.page.clock.pauseAt(1000);
const recharts = {};
for (const name of names) {
  const {from, to} = JSON.parse(readFileSync(join(motion, `${name}.json`), "utf8"));
  await session.page.evaluate((description) => window.pictor.motionMount(description), from);
  await session.page.clock.runFor(4000);
  await session.page.evaluate((description) => window.pictor.motionUpdate(description), to);
  recharts[name] = [];
  let at = 0;
  for (const time of times) {
    await session.page.clock.runFor(time - at);
    at = time;
    const path = join(shots, `${name}-recharts-${time}.png`);
    await session.page.screenshot({path, clip: {x: 0, y: 0, width: to.width, height: to.height}});
    recharts[name].push(readFileSync(path).toString("base64"));
  }
}

const chromiumBrowser = await chromium.launch({executablePath: process.env.CHROMIUM ?? "/usr/bin/chromium"});
const chromiumFrames = await pictorFrames(chromiumBrowser, "chromium");
await chromiumBrowser.close();
const fox = await firefox.launch({executablePath: process.env.FIREFOX ?? "/usr/bin/firefox", channel: "moz-firefox"});
const firefoxFrames = await pictorFrames(fox, "firefox");
await fox.close();

const compare = async (a, b) => session.page.evaluate(async ([left, right]) => {
  const load = (data) => new Promise((ok) => { const image = new Image(); image.onload = () => ok(image); image.src = `data:image/png;base64,${data}`; });
  const [x, y] = await Promise.all([load(left), load(right)]);
  const width = Math.min(x.width, y.width), height = Math.min(x.height, y.height);
  const read = (image) => { const c = new OffscreenCanvas(width, height); const g = c.getContext("2d"); g.drawImage(image, 0, 0); return g.getImageData(0, 0, width, height).data; };
  const [p, q] = [read(x), read(y)];
  let inked = 0, strong = 0;
  for (let i = 0; i < p.length; i += 4) {
    if (Math.min(p[i], p[i + 1], p[i + 2]) >= 250 && Math.min(q[i], q[i + 1], q[i + 2]) >= 250) continue;
    inked++;
    if (Math.max(Math.abs(p[i] - q[i]), Math.abs(p[i + 1] - q[i + 1]), Math.abs(p[i + 2] - q[i + 2])) > 64) strong++;
  }
  return inked ? Math.round(strong / inked * 1000) / 10 : 0;
}, [a, b]);

const results = {};
for (const name of names) {
  results[name] = [];
  for (let i = 0; i < times.length; i++) {
    results[name].push({time: times[i], chromium: await compare(recharts[name][i], chromiumFrames[name][i]), firefox: await compare(recharts[name][i], firefoxFrames[name][i])});
  }
  const worst = (browser) => Math.max(...results[name].map((r) => r[browser]));
  console.log(`${name.padEnd(18)} worst ${worst("chromium")}/${worst("firefox")}  ${results[name].map((r) => `${r.time}:${r.chromium}/${r.firefox}`).join(" ")}`);
}
writeFileSync(join(out, "updates.json"), JSON.stringify({chromium: session.version, gallery, times, results}, null, 1));
await session.close();
