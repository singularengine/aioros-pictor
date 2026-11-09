import {mkdirSync, writeFileSync} from "node:fs";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {examples} from "./examples.mjs";
import {open} from "./browser.mjs";
import {environment, summarize} from "./environment.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const output = process.argv[2] ?? join(here, "bench-results", "recharts-client.json");
const warmup = 100, samples = 500;

const header = environment();
const {page, version, isolated, close} = await open();
const results = [];
try {
  for (const example of examples.filter(e => !process.env.PICTOR_EXAMPLES || process.env.PICTOR_EXAMPLES.split(",").includes(e.name))) {
    const times = await page.evaluate(([name, w, s]) => window.pictor.bench(name, w, s), [example.name, warmup, samples]);
    const result = {name: example.name, warmup, ...summarize(times.map((ms) => ms * 1e3))};
    results.push(result);
    console.log(`${example.name.padEnd(30)} median ${result.medianUs.toFixed(1).padStart(9)} us  p95 ${result.p95Us.toFixed(1).padStart(9)} us  ${result.opsPerSec.toFixed(0).padStart(6)} ops/s`);
  }
} finally {
  await close();
}

mkdirSync(dirname(output), {recursive: true});
writeFileSync(output, JSON.stringify({
  benchmark: "Recharts client render in Chromium: createRoot on a fresh container, then flushSync(root.render(chart)) timed with performance.now; includes the synchronous componentDidMount work (legend measurement and its re-render, axis font reads); unmount is not timed",
  ...header,
  chromium: version,
  crossOriginIsolated: isolated,
  timerResolution: isolated ? "5 us (cross-origin isolated)" : "100 us (not isolated)",
  bundle: "esbuild, React production build, minified",
  methodology: `${warmup} warm-up renders, then ${samples} timed renders per example; opsPerSec = 1e6 / median`,
  results,
}, null, 1) + "\n");
console.log(`written to ${output}`);
