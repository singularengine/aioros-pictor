// Draws each Pictor chart (samples/Demo/out/pictor/<name>.html) in Chromium on the page Recharts' reference was drawn
// on (tools/recharts/browser.mjs: Inter 14px, device scale 1), screenshots it, compares it pixel by pixel with Recharts'
// own render (tools/recharts/out/<name>.png, or Recharts 3's out/recharts3/<name>.png for the examples Pictor takes from it) and times how long Chromium takes to show the server's markup.
// Usage: node samples/Demo/compare.mjs [out]
import {existsSync, readFileSync, writeFileSync, readdirSync} from "node:fs";
import {dirname, join, resolve} from "node:path";
import {fileURLToPath} from "node:url";
import {open} from "../../tools/recharts/browser.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, "../..");
const out = resolve(process.argv[2] ?? join(here, "out"));
const examples = readdirSync(join(root, "tools/recharts/examples")).filter((f) => f.endsWith(".json")).sort()
  .map((f) => JSON.parse(readFileSync(join(root, "tools/recharts/examples", f), "utf8")))
  .filter((e) => !process.env.PICTOR_EXAMPLES || process.env.PICTOR_EXAMPLES.split(",").includes(e.name));
const session = await open();
const {page} = session;
const results = [];
for (const example of examples) {
  const markup = readFileSync(join(out, "pictor", `${example.name}.html`), "utf8");
  const paint = await page.evaluate(async (html) => {
    const host = document.getElementById("host");
    const times = [];
    for (let i = 0; i < 60; i++) {
      host.innerHTML = "";
      const start = performance.now();
      host.innerHTML = html;
      host.getBoundingClientRect();
      document.body.offsetHeight;
      times.push(performance.now() - start);
    }
    times.sort((a, b) => a - b);
    await new Promise((r) => requestAnimationFrame(() => requestAnimationFrame(r)));
    return {medianMs: times[times.length >> 1], p95Ms: times[Math.floor(times.length * 0.95)]};
  }, markup);
  const png = join(out, "pictor", `${example.name}.png`);
  await page.screenshot({path: png, clip: {x: 0, y: 0, width: example.width, height: example.height}, animations: "disabled"});
  const followed = join(root, "tools/recharts/out/recharts3", `${example.name}.png`);
  const reference = readFileSync(existsSync(followed) ? followed : join(root, "tools/recharts/out", `${example.name}.png`)).toString("base64");
  const ours = readFileSync(png).toString("base64");
  const diff = await page.evaluate(async ([a, b]) => {
    const load = (data) => new Promise((ok) => { const image = new Image(); image.onload = () => ok(image); image.src = `data:image/png;base64,${data}`; });
    const [ia, ib] = await Promise.all([load(a), load(b)]);
    const w = Math.max(ia.width, ib.width), h = Math.max(ia.height, ib.height);
    const read = (image) => { const c = new OffscreenCanvas(w, h); const g = c.getContext("2d"); g.fillStyle = "#fff"; g.fillRect(0, 0, w, h); g.drawImage(image, 0, 0); return g.getImageData(0, 0, w, h).data; };
    const da = read(ia), db = read(ib);
    let differ = 0, strong = 0, inked = 0;
    for (let i = 0; i < da.length; i += 4) {
      const d = Math.max(Math.abs(da[i] - db[i]), Math.abs(da[i + 1] - db[i + 1]), Math.abs(da[i + 2] - db[i + 2]));
      if (da[i] + da[i + 1] + da[i + 2] < 750 || db[i] + db[i + 1] + db[i + 2] < 750) inked++;
      if (d > 0) differ++;
      if (d > 64) strong++;
    }
    return {pixels: w * h, differ, strong, inked};
  }, [reference, ours]);
  results.push({name: example.name, ...diff, strongOfInked: diff.inked ? diff.strong / diff.inked : 0, paint});
  console.log(`${example.name}: ${(100 * diff.strong / diff.inked).toFixed(2)}% of inked pixels differ strongly, ${diff.differ} differ at all; shown in ${paint.medianMs.toFixed(3)} ms`);
}
writeFileSync(join(out, "compare.json"), JSON.stringify({chromium: session.version, results}, null, 1));
await session.close();
