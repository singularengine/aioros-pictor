// Writes recharts-scale's tick values for a grid of cases to tests/Aioros.Pictor.Tests/Golden/ticks.json.
// Run from tools/recharts's installed packages: node tools/golden/ticks.mjs
import { createRequire } from 'node:module';
import { writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const require = createRequire(join(here, '../recharts/package.json'));
const load = () => {
  for (const key of Object.keys(require.cache)) {
    if (key.includes('decimal.js-light') || key.includes('recharts-scale')) delete require.cache[key];
  }
  return require('recharts-scale');
};

const domains = [
  [0, 9800], [0, 10000], [0, 4000], [-3000, 9800], [-9800, -100], [0, 1], [0, 0.37], [12, 87], [0.001, 0.0093],
  [-1, 1], [5, 5], [0, 0], [0.5, 0.5], [-2.5, -2.5], [1398, 9800], [0, 123456789], [-0.2, 37.5], [100, 101], [0, 3],
  [9800, 0], [0, 15.5], [3.3, 3.31], [-17, 4],
];
const counts = [2, 3, 5, 6, 7, 10];
const cases = [];
for (const [min, max] of domains) {
  for (const count of counts) {
    for (const allowDecimals of [true, false]) {
      const attempt = (f) => { try { return f(load()); } catch { return null; } };
      cases.push({
        min, max, count, allowDecimals,
        nice: attempt((m) => m.getNiceTickValues([min, max], count, allowDecimals)),
        fixed: attempt((m) => m.getTickValuesFixedDomain([min, max], count, allowDecimals)),
      });
    }
  }
}
const out = join(here, '../../tests/Aioros.Pictor.Tests/Golden');
mkdirSync(out, { recursive: true });
writeFileSync(join(out, 'ticks.json'), JSON.stringify(cases, null, 1) + '\n');
console.log(`${cases.length} cases`);
