import {mkdirSync, writeFileSync} from "node:fs";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {examples, describe} from "./examples.mjs";
import {stableIds} from "./ids.mjs";
import {server} from "./bundle.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const out = join(here, "out");
mkdirSync(out, {recursive: true});
mkdirSync(join(here, "examples"), {recursive: true});

const {render} = await server();
const only = process.argv.slice(2);
for (const example of only.length ? examples.filter((example) => only.includes(example.name)) : examples) {
  writeFileSync(join(here, "examples", `${example.name}.json`), JSON.stringify(describe(example), null, 1) + "\n");
  const markup = stableIds(render(example));
  const svg = markup.match(/<svg[\s\S]*?<\/svg>/)?.[0];
  if (!svg && !example.container) throw new Error(`${example.name}: no <svg> in the server markup`);
  writeFileSync(join(out, `${example.name}.html`), markup + "\n");
  if (svg) writeFileSync(join(out, `${example.name}.svg`), svg.replace(/^<svg /, '<svg xmlns="http://www.w3.org/2000/svg" ') + "\n");
  console.log(`${example.name.padEnd(30)} ${String(svg?.length ?? 0).padStart(6)} B svg ${String(markup.length).padStart(6)} B markup`);
}
