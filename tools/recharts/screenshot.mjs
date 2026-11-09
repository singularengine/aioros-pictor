import {mkdirSync, writeFileSync} from "node:fs";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {examples} from "./examples.mjs";
import {stableIds} from "./ids.mjs";
import {font, open} from "./browser.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const out = join(here, "out");
mkdirSync(out, {recursive: true});

const only = process.argv.slice(2);
const chosen = only.length ? examples.filter((example) => only.includes(example.name)) : examples;
const {page, version, close} = await open();
console.log(`Chromium ${version}`);
try {
  for (const example of chosen) {
    await page.setViewportSize({width: Math.max(800, example.width), height: Math.max(600, example.height)});
    await page.mouse.move(799, 599);
    const mounted = await page.evaluate((name) => window.pictor.mount(name), example.name);
    const markup = stableIds(mounted.markup);
    writeFileSync(join(out, `${example.name}.client.html`), markup + "\n");
    writeFileSync(join(out, `${example.name}.client.json`), JSON.stringify({chromium: version, font: {...font, computed: mounted.font}, legend: mounted.legend}, null, 1) + "\n");
    await page.screenshot({path: join(out, `${example.name}.png`), clip: {x: 0, y: 0, width: example.width, height: example.height}, animations: "disabled"});
    console.log(`${example.name.padEnd(30)} ${example.width}x${example.height} png, ${markup.length} B client markup`);
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
      const acted = stableIds(await page.evaluate(() => window.pictor.settle()));
      writeFileSync(join(out, `${example.name}.${index}.client.html`), acted + "\n");
      if (JSON.stringify(example).includes('"$click"')) {
        writeFileSync(join(out, `${example.name}.${index}.clicks.json`), JSON.stringify(await page.evaluate(() => window.pictorClicks), null, 1) + "\n");
      }
      console.log(`${"".padEnd(30)} interaction ${index}: ${JSON.stringify(steps)}`);
    }
  }
} finally {
  await close();
}
