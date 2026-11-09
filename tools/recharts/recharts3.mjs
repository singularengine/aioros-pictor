import {mkdirSync, writeFileSync} from "node:fs";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {examples} from "./examples.mjs";
import {stableIds} from "./ids.mjs";
import {font, open} from "./browser.mjs";

// Renders the examples whose look Pictor takes from Recharts 3.8.0 in
// Chromium with Recharts 3, as screenshot.mjs renders the others with Recharts 2: every radial bar chart. Writes
// out/recharts3/<name>.client.html, .png and .client.json and
// each interaction's out/recharts3/<name>.<i>.client.html (and .clicks.json for an example with click handlers). Recharts 3 renders nothing on the server (README,
// "Versions"), so its client render is the whole reference; its z-index layers are the one structure Pictor does not
// write, which MarkupComparison leaves out of the comparison of these files.

const here = dirname(fileURLToPath(import.meta.url));
const out = join(here, "out", "recharts3");
mkdirSync(out, {recursive: true});

export const followed = (example) => example.kind === "RadialBarChart";

const only = process.argv.slice(2);
const chosen = examples.filter((example) => followed(example) && (only.length === 0 || only.includes(example.name)));
const {page, version, close} = await open(true);
console.log(`Chromium ${version}, Recharts 3.8.0`);
try {
  for (const example of chosen) {
    await page.setViewportSize({width: Math.max(800, example.width), height: Math.max(600, example.height)});
    await page.mouse.move(799, 599);
    const mounted = await page.evaluate((name) => window.pictor.mount(name), example.name);
    const markup = stableIds(mounted.markup);
    writeFileSync(join(out, `${example.name}.client.html`), markup + "\n");
    writeFileSync(join(out, `${example.name}.client.json`), JSON.stringify({recharts: "3.8.0", chromium: version, font: {...font, computed: mounted.font}, legend: mounted.legend}, null, 1) + "\n");
    await page.screenshot({path: join(out, `${example.name}.png`), clip: {x: 0, y: 0, width: example.width, height: example.height}, animations: "disabled"});
    console.log(`${example.name.padEnd(36)} ${example.width}x${example.height} png, ${markup.length} B client markup`);
    for (const [index, steps] of (example.interactions ?? []).entries()) {
      await page.mouse.move(799, 599);
      await page.evaluate((name) => window.pictor.mount(name), example.name);
      await page.evaluate(() => { window.pictorClicks = []; });
      for (const step of steps) {
        if (step.hover) await page.mouse.move(step.hover[0], step.hover[1]);
        if (step.click) await page.mouse.click(step.click[0], step.click[1]);
        if (step.focus) await page.evaluate(() => document.querySelector("#host .recharts-surface").focus());
        if (step.key) await page.keyboard.press(step.key);
        await page.evaluate(() => window.pictor.settle());
      }
      writeFileSync(join(out, `${example.name}.${index}.client.html`), stableIds(await page.evaluate(() => window.pictor.settle())) + "\n");
      if (JSON.stringify(example).includes('"$click"')) {
        writeFileSync(join(out, `${example.name}.${index}.clicks.json`), JSON.stringify(await page.evaluate(() => window.pictorClicks), null, 1) + "\n");
      }
      console.log(`${"".padEnd(36)} interaction ${index}: ${JSON.stringify(steps)}`);
    }
  }
} finally {
  await close();
}
