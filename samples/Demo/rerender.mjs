// Checks that a chart the framework re-renders (a post, a live push) never plays its enter animation again, in Chromium
// and Firefox, on the Gallery's /interactive page (run the Gallery first; its URL in GALLERY, http://127.0.0.1:3077 by
// default). After the page's enter animations end it moves the brush with the keyboard (a post), zooms the treemap (a
// post) and waits for the live chart's push, then samples every 40 ms for a second: the CSS animations running on
// elements that were on the page before (an enter animation replayed; Recharts' update grows only items that are new),
// the animations of new elements, and how many bars are drawn with no height.
// Usage: node samples/Demo/rerender.mjs
import {chromium, firefox} from "../../tools/recharts/node_modules/playwright-core/index.mjs";

const url = (process.env.GALLERY ?? "http://127.0.0.1:3077") + "/interactive";
const mark = (page) => page.evaluate(() => { window.__kept = new Set(document.querySelectorAll("*")); });
const sample = (page) => page.evaluate(async () => {
  const frames = [];
  for (let i = 0; i < 25; i++) {
    await new Promise((ok) => setTimeout(ok, 40));
    const animations = document.getAnimations().filter((a) => a.playState === "running" && a instanceof CSSAnimation);
    const running = animations.filter((a) => window.__kept.has(a.effect.target)).map((a) => a.animationName);
    const added = animations.filter((a) => !window.__kept.has(a.effect.target)).map((a) => a.animationName);
    const bars = [...document.querySelectorAll(".recharts-bar-rectangle path")].map((p) => p.getBoundingClientRect().height);
    frames.push({running: [...new Set(running)], added: [...new Set(added)], hidden: bars.filter((h) => h === 0).length, bars: bars.length});
  }
  return frames;
});
const summary = (frames) => ({replayedOnKept: [...new Set(frames.flatMap((f) => f.running))], framesReplaying: frames.filter((f) => f.running.length).length, onNewElements: [...new Set(frames.flatMap((f) => f.added))], maxHiddenBars: Math.max(...frames.map((f) => f.hidden))});
for (const [name, type, options] of [["Chromium", chromium, {executablePath: "/usr/bin/chromium"}], ["Firefox", firefox, {executablePath: process.env.FIREFOX ?? "/usr/bin/firefox", channel: "moz-firefox"}]]) {
  const browser = await type.launch(options);
  const page = await browser.newPage({viewport: {width: 1300, height: 1000}});
  await page.goto(url);
  await page.waitForTimeout(3500);
  const results = {};
  const range = page.locator("input[type=range]").last();
  await range.focus();
  await mark(page);
  await page.keyboard.press("ArrowRight");
  results.brush = summary(await sample(page));
  const node = page.locator(".pictor-hits button, .recharts-wrapper button").first();
  if (await node.count()) {
    await mark(page);
    await node.click({force: true}).catch(() => {});
    results.zoom = summary(await sample(page));
  }
  await mark(page);
  await page.waitForTimeout(1500);
  results.live = summary(await sample(page));
  console.log(name, JSON.stringify(results));
  await browser.close();
}
