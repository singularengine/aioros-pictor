import {writeFileSync} from "node:fs";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {examples} from "./examples.mjs";
import {open} from "./browser.mjs";

// Records, for the named examples, the box each cartesian axis tick's text takes the pointer on in Chromium (its glyph
// cells: from its first character's start, as long as its advances, as tall as its box), as out/<name>.ticks.json,
// which Pictor's tick buttons are compared with: `node ticks.mjs Name...`. The advance, not getBBox's width, which
// reaches past it by a glyph's ink (a final A).
const here = dirname(fileURLToPath(import.meta.url));
const only = process.argv.slice(2);
const {page, version, close} = await open();
console.log(`Chromium ${version}`);
try {
  for (const example of examples.filter((example) => only.includes(example.name))) {
    await page.evaluate((name) => window.pictor.mount(name), example.name);
    const ticks = await page.evaluate(() => [...document.querySelectorAll("#host .recharts-cartesian-axis")].map((axis) => ({
      axis: axis.getAttribute("class"),
      ticks: [...axis.querySelectorAll(".recharts-cartesian-axis-tick")].map((tick) => {
        const node = tick.querySelector("text");
        const box = node.getBBox();
        return {text: tick.textContent, x: node.getStartPositionOfChar(0).x, y: box.y, width: node.getComputedTextLength(), height: box.height};
      }),
    })));
    writeFileSync(join(here, "out", `${example.name}.ticks.json`), JSON.stringify(ticks, null, 1) + "\n");
    console.log(`${example.name.padEnd(30)} ${ticks.map((axis) => axis.ticks.length).join(" + ")} ticks`);
  }
} finally {
  await close();
}
