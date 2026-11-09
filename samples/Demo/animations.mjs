// Checks Pictor's web enter animations in Chromium (no script on the page: the test harness pauses the CSS animations
// through the Web Animations API). For every example written animated (samples/Demo/out/animated/<name>.html, by
// `dotnet Demo.dll animated`): a screenshot at a paused time in the middle of the animation, one after it ends and one
// with prefers-reduced-motion; the last two must equal Recharts' static screenshot (tools/recharts/out/<name>.png).
// Usage: node samples/Demo/animations.mjs [out] [milliseconds]
import {mkdirSync, readFileSync, readdirSync, writeFileSync} from "node:fs";
import {dirname, join, resolve} from "node:path";
import {fileURLToPath} from "node:url";
import {open} from "../../tools/recharts/browser.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, "../..");
const out = resolve(process.argv[2] ?? join(here, "out"));
const middle = Number(process.argv[3] ?? 600);
const names = readdirSync(join(out, "animated")).filter((f) => f.endsWith(".html")).map((f) => f.slice(0, -5)).sort();
mkdirSync(join(out, "animated", "shots"), {recursive: true});
const session = await open();
const {page} = session;
const results = [];
for (const name of names) {
  const example = JSON.parse(readFileSync(join(root, "tools/recharts/examples", `${name}.json`), "utf8"));
  const clip = {x: 0, y: 0, width: example.width, height: example.height};
  const shot = async (motion, time, file) => {
    await page.emulateMedia({reducedMotion: motion});
    await page.evaluate((html) => { document.getElementById("host").innerHTML = html; }, readFileSync(join(out, "animated", `${name}.html`), "utf8"));
    const count = await page.evaluate((t) => { const all = document.getAnimations(); all.forEach((a) => { a.pause(); a.currentTime = t; }); return all.length; }, time);
    const path = join(out, "animated", "shots", file);
    await page.screenshot({path, clip});
    return {count, png: readFileSync(path).toString("base64")};
  };
  const moving = await shot("no-preference", middle, `${name}-${middle}.png`);
  const ended = await shot("no-preference", 100000, `${name}-end.png`);
  const reduced = await shot("reduce", 0, `${name}-reduced.png`);
  const reference = readFileSync(join(root, "tools/recharts/out", `${name}.png`)).toString("base64");
  const differ = await page.evaluate(async (images) => {
    const load = (data) => new Promise((ok) => { const image = new Image(); image.onload = () => ok(image); image.src = `data:image/png;base64,${data}`; });
    const all = await Promise.all(images.map(load));
    const read = (image) => { const c = new OffscreenCanvas(image.width, image.height); const g = c.getContext("2d"); g.drawImage(image, 0, 0); return g.getImageData(0, 0, image.width, image.height).data; };
    const [ref, ...others] = all.map(read);
    return others.map((data) => { let strong = 0; for (let i = 0; i < data.length; i += 4) { if (Math.max(Math.abs(data[i] - ref[i]), Math.abs(data[i + 1] - ref[i + 1]), Math.abs(data[i + 2] - ref[i + 2])) > 64) strong++; } return strong; });
  }, [reference, ended.png, reduced.png, moving.png]);
  results.push({name, animations: moving.count, reducedAnimations: reduced.count, endDiffers: differ[0], reducedDiffers: differ[1], middleDiffers: differ[2]});
  console.log(`${name}: ${moving.count} animations; after the end ${differ[0]} and reduced ${differ[1]} pixels differ from Recharts' static render; at ${middle} ms ${differ[2]}`);
}
writeFileSync(join(out, "animations.json"), JSON.stringify({chromium: session.version, middle, results}, null, 1));
await session.close();
