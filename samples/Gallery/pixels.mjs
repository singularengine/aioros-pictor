// Records the comparison pages' pixel verdicts: reads what samples/Demo/compare.mjs measured (<out>/compare.json) and
// the Pictor markup it measured (<out>/pictor/<name>.html), and writes Reference/pixels.json with, per example, the
// share of inked pixels that differ strongly and the hash of the markup measured, beside the Chromium version, the
// commit and the date of the measurement. The Gallery shows a share only while Pictor still writes that markup.
// Usage: samples/Demo/run.sh (or `Demo render <out>` then `node samples/Demo/compare.mjs <out>`), then
//        node samples/Gallery/pixels.mjs [out]
import {createHash} from "node:crypto";
import {execFileSync} from "node:child_process";
import {readFileSync, writeFileSync} from "node:fs";
import {dirname, join, resolve} from "node:path";
import {fileURLToPath} from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const out = resolve(process.argv[2] ?? join(here, "../Demo/out"));
const measured = JSON.parse(readFileSync(join(out, "compare.json"), "utf8"));
const commit = execFileSync("git", ["-C", here, "rev-parse", "--short", "HEAD"], {encoding: "utf8"}).trim();
const results = {};
for (const result of [...measured.results].sort((a, b) => (a.name < b.name ? -1 : a.name > b.name ? 1 : 0))) {
  const markup = readFileSync(join(out, "pictor", `${result.name}.html`));
  results[result.name] = {
    share: Math.round(result.strongOfInked * 1e6) / 1e6,
    markup: createHash("sha256").update(markup).digest("hex").slice(0, 16),
  };
}
const file = join(here, "Gallery", "Reference", "pixels.json");
writeFileSync(file, JSON.stringify({chromium: measured.chromium, commit, date: new Date().toISOString().slice(0, 10), results}, null, 1) + "\n");
console.log(`${Object.keys(results).length} examples recorded in ${file} (Chromium ${measured.chromium}, ${commit})`);
