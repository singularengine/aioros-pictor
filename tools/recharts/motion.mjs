// Records Recharts' update animations frame by frame in Chromium, with Playwright's clock driving the animation frames
// (every 16 ms of clock time): for each case of catalog/motion.mjs, the example mounted animated, its enter animation
// run to its end, the new data rendered, then the series' shapes after every frame until the update has ended.
// Writes out/motion/<case>.json: the two descriptions and the frames ({time, shapes}), the first frame before the update,
// the last six the same as the one before them (the update has ended).
// With --recharts3 the cases render with Recharts 3.8.0 (the radial bar cases, which Pictor takes from it) into out/recharts3/motion/<case>.json.
// Usage: node motion.mjs [--recharts3] [Case...]
import {mkdirSync, readFileSync, writeFileSync} from "node:fs";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {animated, motionCases} from "./catalog/motion.mjs";
import {open} from "./browser.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const recharts3 = process.argv.includes("--recharts3");
const out = join(here, "out", ...(recharts3 ? ["recharts3"] : []), "motion");
mkdirSync(out, {recursive: true});
const only = process.argv.slice(2).filter((arg) => arg !== "--recharts3");
const {page, version, close} = await open(recharts3);
await page.clock.install({time: 0});
await page.clock.pauseAt(1000);
console.log(`Chromium ${version}`);
try {
  for (const motion of motionCases.filter((item) => !only.length || only.includes(item.name))) {
    const example = animated(JSON.parse(readFileSync(join(here, "examples", `${motion.example}.json`), "utf8")));
    const from = motion.from ? motion.from(example) : example;
    const to = motion.to(example);
    let frames;
    if (motion.enter) {
      await page.clock.runFor(16);
      frames = [{time: await page.evaluate(() => performance.now()), shapes: ""}];
      await page.evaluate((description) => window.pictor.motionMount(description), to);
    } else {
      await page.evaluate((description) => window.pictor.motionMount(description), from);
      await page.clock.runFor(4000);
      frames = [{time: await page.evaluate(() => performance.now()), shapes: await page.evaluate(() => window.pictor.shapes())}];
      await page.evaluate((description) => window.pictor.motionUpdate(description), to);
    }
    for (let step = 0, rest = 0, moved = 0; step < 2600 / 16 && rest < 6; step++) {
      await page.clock.runFor(16);
      frames.push({time: await page.evaluate(() => performance.now()), shapes: await page.evaluate(() => window.pictor.shapes())});
      const same = frames.at(-1).shapes === frames.at(-2).shapes;
      moved += same || frames.length < 3 ? 0 : 1;
      rest = moved > 1 && same ? rest + 1 : 0;
    }
    const changed = frames.filter((frame, i) => i > 0 && frame.shapes !== frames[i - 1].shapes).length;
    writeFileSync(join(out, `${motion.name}.json`), JSON.stringify({chromium: version, example: motion.example, enter: Boolean(motion.enter), from, to, frames}) + "\n");
    console.log(`${motion.name.padEnd(20)} ${frames.length} frames, ${changed} of them changed`);
  }
} finally {
  await close();
}
