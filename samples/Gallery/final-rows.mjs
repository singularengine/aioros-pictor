// Checks the Gallery's last demo rows on a running Gallery reached at its address (GALLERY, e.g.
// http://127.0.0.1:3077), in Chromium and Firefox, and takes the screenshots of the comparison page and of the theme
// switch (artifacts/):
// - the theme switch's System choice follows the device's color scheme (emulated: Chromium's colorScheme, Firefox's
//   layout.css.prefers-color-scheme.content-override), through the theme-system cookie the framework's script keeps
//   (a LAN address is not a secure context, so no browser sends the Sec-CH-Prefers-Color-Scheme hint there), and is kept
//   on the next page; Light and Dark switch back;
// - /live-charts: a push morphs the charts (an element carries data-aioros-morphing);
// - the dashboard: Relay chosen by a client navigation stays Relay over two later pushes (Aioros's SSR-179);
// - the dashboard's main chart brush moved in the Live range is kept over two later pushes (the window in the address).
// Usage: GALLERY=<url> node samples/Gallery/final-rows.mjs
import {mkdirSync, writeFileSync} from "node:fs";
import {dirname, join} from "node:path";
import {fileURLToPath} from "node:url";
import {chromium as playwright} from "../../tools/recharts/node_modules/playwright-core/index.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const out = join(here, "..", "..", "artifacts");
mkdirSync(out, {recursive: true});
const gallery = process.env.GALLERY ?? "http://127.0.0.1:3077";
const puppeteerPath = process.env.PUPPETEER ?? "../../artifacts/firefox-tool/node_modules/puppeteer-core/lib/esm/puppeteer/puppeteer-core.js";

const background = (page) => page.evaluate(() => getComputedStyle(document.body).backgroundColor);
const dark = "rgb(9, 9, 11)";
const light = "rgb(247, 247, 248)";

/** Waits until fn(arg) is true in the page, polling every 100 ms, for up to 20 s. */
async function until(page, fn, arg) {
  for (let i = 0; i < 200; i++) {
    if (await page.evaluate(fn, arg)) return true;
    await new Promise((ok) => setTimeout(ok, 100));
  }
  return false;
}

const viewText = (text) => [...document.querySelectorAll("[data-aioros-view]")].find((v) => v.innerText.includes(text))?.innerText ?? "";

/** The text of the view holding `text` after each of two later pushes. */
async function twoPushes(page, text) {
  const seen = [];
  let before = await page.evaluate(viewText, text);
  for (let i = 0; i < 2; i++) {
    await until(page, ([t, b]) => ([...document.querySelectorAll("[data-aioros-view]")].find((v) => v.innerText.includes(t))?.innerText ?? b) !== b, [text, before]);
    before = await page.evaluate(viewText, text);
    seen.push(before);
  }
  return seen;
}

async function check(name, open) {
  const result = {browser: name};
  {
    const {page, close, scheme} = await open("dark");
    await page.goto(gallery + "/line");
    result.themeBefore = await background(page);
    await page.click("a[href='/theme/system']");
    result.systemFollowsDark = await until(page, (c) => getComputedStyle(document.body).backgroundColor === c, dark);
    result.systemCookie = await page.evaluate(() => document.cookie);
    await page.screenshot({path: join(out, `theme-system-dark-${name}.png`)});
    await page.click("a[href='/bar']");
    await until(page, () => location.pathname === "/bar");
    result.keptOnNextPage = await until(page, (c) => getComputedStyle(document.body).backgroundColor === c, dark);
    if (scheme) {
      await scheme(page, "light");
      result.systemFollowsLight = await until(page, (c) => getComputedStyle(document.body).backgroundColor === c, light);
      await page.screenshot({path: join(out, `theme-system-light-${name}.png`)});
    }
    await page.click("a[href='/theme/dark']");
    result.darkChoice = await until(page, (c) => getComputedStyle(document.body).backgroundColor === c, dark);
    await page.screenshot({path: join(out, `theme-dark-${name}.png`)});
    await page.click("a[href='/theme/light']");
    result.lightChoice = await until(page, (c) => getComputedStyle(document.body).backgroundColor === c, light);
    await page.screenshot({path: join(out, `theme-light-${name}.png`)});
    await close();
  }
  {
    const {page, close} = await open("light");
    await page.goto(gallery + "/compare/pie");
    await page.evaluate(() => [...document.images].forEach((image) => { image.loading = "eager"; }));
    await until(page, () => [...document.images].filter((i) => i.src.includes("/reference/")).every((i) => i.complete && i.naturalWidth > 0));
    result.compareImages = await page.evaluate(() => [...document.images].filter((i) => i.src.includes("/reference/") && i.naturalWidth > 0).length);
    await page.screenshot({path: join(out, `compare-pie-${name}.png`), fullPage: false});
    await page.goto(gallery + "/compare");
    await page.screenshot({path: join(out, `compare-index-${name}.png`)});

    await page.goto(gallery + "/live-charts");
    result.liveMorph = await until(page, () => document.querySelector("[data-aioros-morphing]") !== null);
    await page.screenshot({path: join(out, `live-charts-${name}.png`)});

    await page.goto(gallery + "/?app=web&range=live");
    await page.click("a[aria-label='Relay, the payments API']");
    await until(page, () => document.body.innerText.includes("Requests per second"));
    const pushes = await twoPushes(page, "Requests per second");
    result.productKept = pushes.length === 2 && pushes.every((text) => text.includes("Requests per second")) && !(await page.evaluate(() => document.body.innerText.includes("Active users now")));

    await page.goto(gallery + "/?app=api&range=live");
    await page.evaluate(() => { const input = [...document.querySelectorAll("input[aria-label='Start of the window']")].find((i) => i.offsetParent !== null); input.value = "5"; input.dispatchEvent(new Event("input", {bubbles: true})); input.dispatchEvent(new Event("change", {bubbles: true})); });
    await until(page, () => location.search.includes("from=5"));
    result.brushAddress = await page.evaluate(() => location.search);
    const ticks = await twoPushes(page, "Per 2 minutes");
    result.brushKept = await page.evaluate(() => [...document.querySelectorAll("input[type=range]")].filter((i) => i.offsetParent !== null).map((i) => i.value).join(","));
    result.brushPushes = ticks.length;
    await close();
  }
  return result;
}

const results = [];
const chromium = await playwright.launch({executablePath: process.env.CHROMIUM ?? "/usr/bin/chromium"});
results.push({version: chromium.version(), ...(await check("chromium", async (scheme) => {
  const context = await chromium.newContext({colorScheme: scheme, viewport: {width: 1280, height: 900}, deviceScaleFactor: 1});
  const page = await context.newPage();
  return {page, close: () => context.close(), scheme: (p, next) => p.emulateMedia({colorScheme: next})};
}))});
await chromium.close();

const {default: puppeteer} = await import(puppeteerPath);
for (const scheme of ["dark", "light"]) {
  const firefox = await puppeteer.launch({browser: "firefox", executablePath: process.env.FIREFOX ?? "/usr/bin/firefox", headless: true,
    extraPrefsFirefox: {"layout.css.prefers-color-scheme.content-override": scheme === "dark" ? 0 : 1}});
  if (scheme === "dark") {
    results.push({version: await firefox.version(), ...(await check("firefox", async () => {
      const page = await firefox.newPage();
      await page.setViewport({width: 1280, height: 900});
      return {page, close: () => page.close(), scheme: null};
    }))});
  } else {
    const page = await firefox.newPage();
    await page.setViewport({width: 1280, height: 900});
    await page.goto(gallery + "/line");
    await page.click("a[href='/theme/system']");
    const followsLight = await until(page, (c) => getComputedStyle(document.body).backgroundColor === c, light);
    await page.screenshot({path: join(out, "theme-system-light-firefox.png")});
    results.find((r) => r.browser === "firefox").systemFollowsLight = followsLight;
  }
  await firefox.close();
}

writeFileSync(join(out, "final-rows.json"), JSON.stringify({gallery, results}, null, 1) + "\n");
console.log(JSON.stringify(results, null, 1));
