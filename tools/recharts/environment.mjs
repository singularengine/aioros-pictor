import {readFileSync} from "node:fs";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {cpus, loadavg} from "node:os";

const here = dirname(fileURLToPath(import.meta.url));

export function version(module) {
  return JSON.parse(readFileSync(join(here, "node_modules", module, "package.json"), "utf8")).version;
}

export function environment() {
  return {
    node: process.version,
    recharts: version("recharts"),
    react: version("react"),
    reactDom: version("react-dom"),
    machine: `${cpus()[0].model}, ${cpus().length} threads`,
    date: new Date().toISOString(),
    loadAverage: loadavg()[0],
  };
}

export function summarize(timesUs) {
  const sorted = [...timesUs].sort((a, b) => a - b);
  const at = (q) => sorted[Math.min(sorted.length - 1, Math.floor(q * sorted.length))];
  const mean = sorted.reduce((s, t) => s + t, 0) / sorted.length;
  return {
    samples: sorted.length,
    medianUs: round(at(0.5)),
    p95Us: round(at(0.95)),
    minUs: round(sorted[0]),
    meanUs: round(mean),
    opsPerSec: round(1e6 / at(0.5)),
  };
}

const round = (value) => Math.round(value * 100) / 100;
