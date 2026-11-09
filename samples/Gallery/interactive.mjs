// Usage: [BROWSER=firefox] node samples/Gallery/interactive.mjs [baseUrl] [light|dark]
// Drives the Gallery's /interactive page in Chromium (playwright-core from tools/recharts, /usr/bin/chromium or
// CHROMIUM) or Firefox (/usr/bin/firefox or FIREFOX) and checks each interaction on the web: a bar's button runs the
// command and the region comes back, the brush moves by the keyboard (rapid presses included) and by dragging its
// traveller, the treemap zooms in and out, synced charts show the same category, and the live chart is pushed by the
// server. Prints each check, the round trip of each post (from the input to the morphed answer) and the bytes of each
// answer.
import { chromium, firefox } from '../../tools/recharts/node_modules/playwright-core/index.mjs';

const base = process.argv[2] ?? 'http://127.0.0.1:3011';
const theme = process.argv[3] ?? 'light';
const browser = process.env.BROWSER === 'firefox'
  ? await firefox.launch({ executablePath: process.env.FIREFOX ?? '/usr/bin/firefox', channel: 'moz-firefox' })
  : await chromium.launch({ executablePath: process.env.CHROMIUM ?? '/usr/bin/chromium', args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 1700, height: 1400 } });
const results = [];
const check = (name, ok, detail = '') => { results.push({ name, ok, detail }); console.log(`${ok ? 'ok  ' : 'FAIL'} ${name}${detail ? ' — ' + detail : ''}`); };

async function posted(action, settle) {
  const response = page.waitForResponse(r => r.request().method() === 'POST');
  const start = performance.now();
  await action();
  const answer = await response;
  const body = await answer.body();
  await page.waitForFunction(settle.fn, settle.arg, { timeout: 5000 });
  const sizes = await answer.request().sizes();
  const timing = answer.request().timing();
  return { ms: Math.round(performance.now() - start), server: Math.round(timing.responseStart - timing.requestStart), bytes: body.length, wire: sizes.responseBodySize, status: answer.status() };
}

const median = values => [...values].sort((a, b) => a - b)[Math.floor(values.length / 2)];

let pendingPosts = 0;
page.on('request', request => { if (request.method() === 'POST') pendingPosts++; });
for (const done of ['requestfinished', 'requestfailed']) page.on(done, request => { if (request.method() === 'POST') pendingPosts--; });
/** Returns once no post is in flight (the live stream stays open, so the network never idles). */
async function postsSettled() { while (pendingPosts > 0) await page.waitForEvent('requestfinished'); }

/** The chart's pixels at rest, then without the markup an interaction adds: equal when the buttons and fields change nothing at rest. */
async function sameAtRest(index, selector) {
  const wrapper = page.locator('.recharts-wrapper').nth(index);
  await page.mouse.move(1, 1);
  const with_ = await wrapper.screenshot();
  await wrapper.evaluate((w, s) => w.querySelectorAll(s).forEach(e => { e.dataset.removed = e.style.display; e.style.display = 'none'; }), selector);
  const without = await wrapper.screenshot();
  await wrapper.evaluate((w, s) => w.querySelectorAll(s).forEach(e => { e.style.display = e.dataset.removed; }), selector);
  return with_.equals(without);
}

await page.goto(base + '/theme/' + theme);
await page.goto(base + '/interactive');
await page.waitForFunction(() => document.getAnimations().every(a => a.playState === 'finished'));
const loaded = await page.evaluate(() => performance.getEntriesByType('navigation')[0].transferSize);
await page.evaluate(() => { window.__loaded = performance.timeOrigin; });

const tick = await page.locator('.recharts-wrapper').nth(0).locator('.recharts-cartesian-axis-tick-value').first().getAttribute('fill');
const nestBox = await page.locator('.recharts-treemap-nest-index-box').filter({ visible: true }).first().evaluate(e => getComputedStyle(e).backgroundColor);
check(`the page is drawn in the ${theme} theme`, theme === 'dark' ? tick !== '#666' && nestBox !== 'rgb(0, 0, 0)' : tick === '#666' && nestBox === 'rgb(0, 0, 0)', `tick ${tick}, nest index ${nestBox}`);
check('the bar chart looks the same with and without its buttons at rest', await sameAtRest(0, '.pictor-cmds'));
check('the brush looks the same with and without its range inputs at rest', await sameAtRest(2, '.pictor-brush-fields'));
const wrappers = page.locator('.recharts-wrapper');
const bar = wrappers.nth(0);
const firstButton = bar.locator('.pictor-cmds button').nth(2);
let r = await posted(() => firstButton.click(), { fn: () => document.body.innerText.includes('You clicked Page C'), arg: null });
check('a click on the third pv bar runs Pick("2") and the text comes back', true, `${r.ms} ms, ${r.bytes} B answer`);
check('no reload after the click', await page.evaluate(() => window.__loaded === performance.timeOrigin));
await page.mouse.click(5, 300);
let tabbed = false;
for (let i = 0; i < 60 && !tabbed; i++) {
  await page.keyboard.press('Tab');
  tabbed = await page.evaluate(() => document.activeElement?.closest('.pictor-cmds') !== null && document.activeElement?.tagName === 'BUTTON');
}
const focusedTip = tabbed && await page.locator('.recharts-wrapper').nth(0).locator('.pictor-hover>[class^=pictor-tk]').first().evaluate(e => getComputedStyle(e).display !== 'none');
check('Tab reaches the first bar button, which shows its category\'s tooltip', focusedTip);
const clicks = [];
for (let i = 0; i < 9; i++) {
  const target = page.locator('.recharts-wrapper').nth(0).locator('.pictor-cmds button').nth(i % 7);
  const name = ['Page A', 'Page B', 'Page C', 'Page D', 'Page E', 'Page F', 'Page G'][i % 7];
  clicks.push(await posted(() => target.click(), { fn: n => document.body.innerText.includes('You clicked ' + n + ':'), arg: name }));
}
check('a bar click\'s round trip', true, `median ${median(clicks.map(c => c.ms))} ms (server ${median(clicks.map(c => c.server))} ms) over ${clicks.length}, answer ${median(clicks.map(c => c.bytes))} B (${median(clicks.map(c => c.wire))} B on the wire)`);

const brushChart = wrappers.nth(2);
const end = brushChart.locator('.pictor-b1');
await end.focus();
const before = await brushChart.locator('.recharts-xAxis .recharts-cartesian-axis-tick-value').allTextContents();
r = await posted(() => page.keyboard.press('ArrowLeft'), { fn: () => document.querySelectorAll('.recharts-wrapper')[2].querySelector('.pictor-b1').getAttribute('value') === '19', arg: null });
const moves = [];
for (let i = 0; i < 8; i++) {
  const key = i % 2 === 0 ? 'ArrowLeft' : 'ArrowRight';
  const expected = i % 2 === 0 ? '18' : '19';
  moves.push(await posted(() => page.keyboard.press(key), { fn: v => document.querySelectorAll('.recharts-wrapper')[2].querySelector('.pictor-b1').getAttribute('value') === v, arg: expected }));
}
const after = await page.locator('.recharts-wrapper').nth(2).locator('.recharts-xAxis .recharts-cartesian-axis-tick-value').allTextContents();
const brushWindow = () => page.evaluate(() => { const w = document.querySelectorAll('.recharts-wrapper')[2]; return w.querySelector('.pictor-b0').getAttribute('value') + '..' + w.querySelector('.pictor-b1').getAttribute('value'); });
check('ArrowLeft on the end traveller moves the window from 5..20 to 5..19', after.at(-1) !== before.at(-1) && await brushWindow() === '5..19', `${before.at(-1)} -> ${after.at(-1)}, window ${await brushWindow()}; ${r.ms} ms, ${r.bytes} B`);
check('the end traveller keeps the focus after the answer', await page.evaluate(() => document.activeElement?.classList.contains('pictor-b1') ?? false));
check('a brush move\'s round trip', true, `median ${median(moves.map(c => c.ms))} ms (server ${median(moves.map(c => c.server))} ms) over ${moves.length}, answer ${median(moves.map(c => c.bytes))} B (${median(moves.map(c => c.wire))} B on the wire)`);

const strip = await page.locator('.recharts-wrapper').nth(2).locator('.pictor-brush-fields').boundingBox();
const count = 39;
const step = (strip.width - 5) / (count - 1);
const startX = strip.x + 5 * step + 2.5;
const y = strip.y + strip.height / 2;
r = await posted(async () => { await page.mouse.move(startX, y); await page.mouse.down(); await page.mouse.move(startX + 3 * step, y, { steps: 5 }); await page.mouse.up(); },
  { fn: () => document.querySelectorAll('.recharts-wrapper')[2].querySelector('.pictor-b0').getAttribute('value') === '8', arg: null });
check('dragging the start traveller three steps right moves the window to 8..19, the end kept', await brushWindow() === '8..19', `window ${await brushWindow()}; ${r.ms} ms, ${r.bytes} B`);
await brushChart.locator('.pictor-b0').focus();
r = await posted(() => page.keyboard.press('ArrowRight'), { fn: () => document.querySelectorAll('.recharts-wrapper')[2].querySelector('.pictor-b0').getAttribute('value') === '9', arg: null });
check('ArrowRight on the start traveller moves the start alone (9..19) and keeps the focus', await brushWindow() === '9..19' && await page.evaluate(() => document.activeElement?.classList.contains('pictor-b0') ?? false), `window ${await brushWindow()}; focus ${await page.evaluate(() => document.activeElement?.className)}`);
await brushChart.locator('.pictor-bs').focus();
r = await posted(() => page.keyboard.press('ArrowLeft'), { fn: () => document.querySelectorAll('.recharts-wrapper')[2].querySelector('.pictor-b0').getAttribute('value') === '8', arg: null });
check('ArrowLeft on the slide moves both ends (8..18) and keeps the focus', await brushWindow() === '8..18' && await page.evaluate(() => document.activeElement?.classList.contains('pictor-bs') ?? false), `window ${await brushWindow()}`);
await brushChart.locator('.pictor-b1').focus();
const rapidPosts = [];
const counting = request => { if (request.method() === 'POST') rapidPosts.push(request); };
page.on('request', counting);
for (let i = 0; i < 5; i++) await page.keyboard.press('ArrowLeft');
await page.waitForFunction(() => document.querySelectorAll('.recharts-wrapper')[2].querySelector('.pictor-b1').getAttribute('value') === '13', null, { timeout: 10000 }).catch(() => {});
await postsSettled();
page.off('request', counting);
const rapidTicks = await page.locator('.recharts-wrapper').nth(2).locator('.recharts-xAxis .recharts-cartesian-axis-tick-value').allTextContents();
check('five rapid ArrowLeft presses on the end traveller move the window to 8..13, the server\'s answer agreeing', await brushWindow() === '8..13' && await page.evaluate(() => document.querySelectorAll('.recharts-wrapper')[2].querySelector('.pictor-b1').value === '13'), `window ${await brushWindow()}, ${rapidPosts.length} posts, last tick ${rapidTicks.at(-1)}`);
r = await posted(() => page.keyboard.press('ArrowRight'), { fn: () => document.querySelectorAll('.recharts-wrapper')[2].querySelector('.pictor-b1').getAttribute('value') === '14', arg: null });
for (let i = 0; i < 4; i++) await page.keyboard.press('ArrowRight');
await page.waitForFunction(() => document.querySelectorAll('.recharts-wrapper')[2].querySelector('.pictor-b1').getAttribute('value') === '18', null, { timeout: 10000 }).catch(() => {});
await postsSettled();
check('five more presses back (one awaited, four rapid) restore 8..18', await brushWindow() === '8..18', `window ${await brushWindow()}`);

const treemap = page.locator('.recharts-wrapper').nth(3);
const node = await treemap.locator('.pictor-cmds :is(button, input):not([hidden])').first().boundingBox();
r = await posted(() => page.mouse.click(node.x + node.width / 2, node.y + node.height / 2), { fn: () => [...document.querySelectorAll('.recharts-treemap-nest-index-box')].filter(b => b.checkVisibility()).length === 2, arg: null });
const trail = await page.locator('.recharts-treemap-nest-index-box').filter({ visible: true }).allTextContents();
check('a click on a treemap node zooms into it, the trail naming the node', trail.length === 2 && trail[0] === 'root' && trail[1] !== 'root', `${trail.join(' > ')}; ${r.ms} ms, ${r.bytes} B`);
r = await posted(() => page.locator('.recharts-treemap-nest-index-box').filter({ visible: true }).first().click(), { fn: () => [...document.querySelectorAll('.recharts-treemap-nest-index-box')].filter(b => b.checkVisibility()).length === 1, arg: null });
check('a click on the root box zooms back out', true, `${r.ms} ms, ${r.bytes} B`);
const brushKept = await brushWindow();
check('the brush window is kept across the treemap posts (its fields are posted)', brushKept === '8..18', `window ${brushKept}`);

const line = page.locator('.recharts-wrapper').nth(4);
const box = await line.locator('.pictor-hover>[class*=pictor-hk]').nth(2).boundingBox();
await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
const visible = async (i, k) => page.locator('.recharts-wrapper').nth(i).locator('.pictor-hover>[class^=pictor-tk]').nth(k).evaluate(e => getComputedStyle(e).display !== 'none');
check('hovering Page C in the first synced chart shows Page C in the second and the third (index)', await visible(5, 2) && await visible(4, 2) && await visible(6, 2));
const valueBox = await page.locator('.recharts-wrapper').nth(7).locator('.pictor-hover>[class*=pictor-hk]').nth(1).boundingBox();
await page.mouse.move(valueBox.x + valueBox.width / 2, valueBox.y + valueBox.height / 2);
check('hovering Page B in the value-synced chart shows Page B in the reversed one (its 6th category)', await visible(8, 5));

const tickAfter = await page.locator('.recharts-wrapper').nth(2).locator('.recharts-cartesian-axis-tick-value').first().getAttribute('fill');
const travellerAfter = await page.locator('.recharts-wrapper').nth(2).locator('.pictor-brush-fields').getAttribute('style');
check(`the answers to the posts keep the ${theme} theme`, theme === 'dark' ? tickAfter !== '#666' : tickAfter === '#666', `tick ${tickAfter}; brush fields ${travellerAfter}`);
const liveText = async () => page.locator('.recharts-wrapper').nth(9).locator('.recharts-line-curve').getAttribute('d');
const d0 = await liveText();
const t0 = Date.now();
await page.waitForFunction(d => document.querySelectorAll('.recharts-wrapper')[9].querySelector('.recharts-line-curve').getAttribute('d') !== d, d0, { timeout: 5000 });
check('the live chart is pushed by the server without a reload', await page.evaluate(() => window.__loaded === performance.timeOrigin), `new data after ${Date.now() - t0} ms`);

console.log(JSON.stringify({ page: loaded, results }, null, 1).split('\n').length > 0 ? `page ${loaded} B transferred` : '');
await browser.close();
process.exit(results.every(result => result.ok) ? 0 : 1);
