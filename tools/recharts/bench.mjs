import {mkdirSync, writeFileSync} from "node:fs";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {loadavg} from "node:os";
import {stableIds} from "./ids.mjs";
import {server} from "./bundle.mjs";
import {environment, summarize} from "./environment.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const output = process.argv[2] ?? join(here, "bench-results", "recharts-ssr.json");
const warmupNs = 1e9, measureNs = 3e9, minSamples = 500, maxSamples = 20_000;

const {examples, render} = await server();
const header = environment();
const results = [];
for (const example of examples.filter(e => !process.env.PICTOR_EXAMPLES || process.env.PICTOR_EXAMPLES.split(",").includes(e.name))) {
  const markup = stableIds(render(example));
  const svg = markup.match(/<svg[\s\S]*?<\/svg>/)[0];
  let warmup = 0;
  for (const begin = process.hrtime.bigint(); Number(process.hrtime.bigint() - begin) < warmupNs || warmup < 100; warmup++) render(example);
  const times = [];
  for (const begin = process.hrtime.bigint(); (Number(process.hrtime.bigint() - begin) < measureNs || times.length < minSamples) && times.length < maxSamples;) {
    const start = process.hrtime.bigint();
    render(example);
    times.push(Number(process.hrtime.bigint() - start) / 1e3);
  }
  const result = {name: example.name, warmup, ...summarize(times), svgBytes: Buffer.byteLength(svg), markupBytes: Buffer.byteLength(markup)};
  results.push(result);
  console.log(`${example.name.padEnd(30)} median ${result.medianUs.toFixed(1).padStart(9)} us  p95 ${result.p95Us.toFixed(1).padStart(9)} us  ${result.opsPerSec.toFixed(0).padStart(6)} ops/s  ${result.samples} samples`);
}

mkdirSync(dirname(output), {recursive: true});
writeFileSync(output, JSON.stringify({
  benchmark: "Recharts server render: renderToStaticMarkup of each example, React production build, one render per sample",
  ...header,
  nodeEnv: "production",
  loadAverage: [header.loadAverage, loadavg()[0]],
  methodology: `warm up for ${warmupNs / 1e9} s (at least 100 renders), then time each render with process.hrtime.bigint for ${measureNs / 1e9} s (${minSamples} to ${maxSamples} samples); opsPerSec = 1e6 / median`,
  results,
}, null, 1) + "\n");
console.log(`written to ${output}`);
