// Records, for each case of catalog/motion.mjs' strokeCases, the inline style Recharts gives each rectangle of a treemap
// mounted animated with `isUpdateAnimationActive={false}`, after every 16 ms of Playwright's clock: react-smooth sets
// the stroke's dash to `0px L` at once and, some frames later, to `Lpx 0px` with a CSS transition, which runs on the
// browser's own clock (Playwright's does not drive it), so the time the transition is set is what Pictor ports.
// Writes out/motion/<case>.styles.json. Usage: node strokes.mjs
import {readFileSync, writeFileSync} from "node:fs";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {animated, strokeCases} from "./catalog/motion.mjs";
import {open} from "./browser.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const {page, version, close} = await open();
await page.clock.install({time: 0});
await page.clock.pauseAt(1000);
try {
  for (const stroke of strokeCases) {
    const example = stroke.to(animated(JSON.parse(readFileSync(join(here, "examples", `${stroke.example}.json`), "utf8"))));
    await page.clock.runFor(16);
    const mounted = await page.evaluate(() => performance.now());
    await page.evaluate((description) => window.pictor.motionMount(description), example);
    const frames = [];
    for (let step = 0; step < 12; step++) {
      frames.push({time: await page.evaluate(() => performance.now()) - mounted, styles: await page.evaluate(() => [...document.querySelectorAll("#host path.recharts-rectangle")].map((path) => path.getAttribute("style")))});
      await page.clock.runFor(16);
    }
    writeFileSync(join(here, "out", "motion", `${stroke.name}.styles.json`), JSON.stringify({chromium: version, example: stroke.example, frames}, null, 1) + "\n");
    console.log(`${stroke.name.padEnd(24)} ${frames.length} frames`);
  }
} finally {
  await close();
}
