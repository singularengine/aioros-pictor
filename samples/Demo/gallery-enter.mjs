// Compares the enter animations of charts served by the Gallery (GALLERY, e.g. http://127.0.0.1:3077) with
// Recharts' frames recorded by enter.mjs (<out>/enter/<name>-recharts-<time>.png), in Chromium and Firefox: each page is
// loaded once per time, every animation paused at that time, and the chart under the heading that names the example
// screenshotted. Usage: GALLERY=... node samples/Demo/gallery-enter.mjs <out> page:Example...
import {chromium, firefox} from "../../tools/recharts/node_modules/playwright-core/index.mjs";
import {readFileSync, writeFileSync} from "node:fs";
import {join, resolve} from "node:path";

const out = resolve(process.argv[2]);
const cases = process.argv.slice(3).map((arg) => arg.split(":"));
const gallery = process.env.GALLERY ?? "http://127.0.0.1:3077";
const times = [0, 96, 192, 304, 400, 496, 608, 704, 800, 992, 1200, 1392, 1600, 1792, 2000, 2208];
const compare = async (page, a, b) => page.evaluate(async ([left, right]) => {
  const load = (data) => new Promise((ok) => { const image = new Image(); image.onload = () => ok(image); image.src = `data:image/png;base64,${data}`; });
  const read = (image, w, h) => { const c = new OffscreenCanvas(w, h); const g = c.getContext("2d"); g.fillStyle = "#fff"; g.fillRect(0, 0, w, h); g.drawImage(image, 0, 0); return g.getImageData(0, 0, w, h).data; };
  const [i, j] = await Promise.all([load(left), load(right)]);
  const w = Math.min(i.width, j.width), h = Math.min(i.height, j.height);
  const [x, y] = [read(i, w, h), read(j, w, h)];
  let inked = 0, strong = 0;
  for (let k = 0; k < x.length; k += 4) {
    if (Math.min(x[k], x[k + 1], x[k + 2]) >= 250 && Math.min(y[k], y[k + 1], y[k + 2]) >= 250) continue;
    inked++;
    if (Math.max(Math.abs(x[k] - y[k]), Math.abs(x[k + 1] - y[k + 1]), Math.abs(x[k + 2] - y[k + 2])) > 64) strong++;
  }
  return inked ? Math.round(strong / inked * 1000) / 10 : 0;
}, [a, b]);
const results = {};
for (const [label, type, options] of [["chromium", chromium, {executablePath: "/usr/bin/chromium"}], ["firefox", firefox, {executablePath: process.env.FIREFOX ?? "/usr/bin/firefox", channel: "moz-firefox"}]]) {
  const browser = await type.launch(options);
  const page = await browser.newPage({viewport: {width: 1400, height: 1200}});
  for (const [path, name] of cases) {
    const row = [];
    for (const time of times) {
      await page.goto(gallery + "/" + path);
      await page.evaluate((t) => document.getAnimations().forEach((a) => { a.pause(); a.currentTime = t; }), time);
      const chart = page.locator(`xpath=//*[normalize-space(text())="${name}"]/following::div[contains(@class,"recharts-wrapper")][1]`);
      await chart.scrollIntoViewIfNeeded();
      await page.evaluate((t) => document.getAnimations().forEach((a) => { a.pause(); a.currentTime = t; }), time);
      const shot = (await chart.screenshot()).toString("base64");
      const reference = readFileSync(join(out, "enter", `${name}-recharts-${time}.png`)).toString("base64");
      row.push(await compare(page, reference, shot));
    }
    results[`${label} ${name}`] = row;
    console.log(`${label.padEnd(9)} ${name.padEnd(24)} ${row.map((v, i) => `${times[i]}:${v}`).join(" ")}`);
  }
  await browser.close();
}
writeFileSync(join(out, "gallery-enter.json"), JSON.stringify({gallery, times, results}, null, 1));
