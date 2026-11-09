// Checks the final chart rows on a running Gallery's /extras page, reached at its address (GALLERY, e.g.
// http://127.0.0.1:3077), in Chromium and Firefox:
// - a click on an axis tick's label posts the axis' command with the tick's value (Page C, 5000);
// - the brush's slide dragged by the mouse, moved by the arrow keys and dragged by a finger posts one field and the
//   window comes back moved, its span kept;
// - a funnel item hovered shows its active shape in its own layer (the in-place group shown, the shape before it
//   hidden) under the label lists (the name is still what the pointer hits at its center);
// - the bars' activeIndex draws three active layers at rest.
// Usage: GALLERY=<url> node samples/Demo/final.mjs
import {chromium as playwright} from "../../tools/recharts/node_modules/playwright-core/index.mjs";
import {chromium, firefox, gallery} from "./browsers.mjs";

const pause = (ms = 300) => new Promise((ok) => setTimeout(ok, ms));
const results = [];
const check = (driver, name, ok, detail) => {
  results.push({browser: driver.name, name, ok});
  console.log(`${driver.name.padEnd(9)} ${ok ? "ok  " : "FAIL"} ${name}${detail ? `: ${detail}` : ""}`);
};

const window = (page) => page.evaluate(() => {
  const brush = document.querySelector(".pictor-bs").closest(".recharts-brush");
  brush.scrollIntoView({block: "center"});
  return [...brush.querySelectorAll(".pictor-b0,.pictor-b1")].map((input) => Number(input.value));
});

const slideCenter = (page) => page.evaluate(() => {
  const box = document.querySelector(".pictor-bs").closest(".recharts-brush").querySelector(".recharts-brush-slide").getBoundingClientRect();
  return {x: Math.round(box.x + box.width / 2), y: Math.round(box.y + box.height / 2)};
});

async function moved(driver, page, before) {
  await driver.wait(page, (start) => Number(document.querySelector(".pictor-b0")?.value) !== start, before[0], 10000).catch(() => {});
  await pause();
  return window(page);
}

async function run(driver) {
  const page = await driver.page();
  await driver.goto(page, gallery + "/extras");
  await pause(2500);

  await page.evaluate(() => document.querySelectorAll(".recharts-funnel-trapezoid")[1].scrollIntoView({block: "center", behavior: "instant"}));
  await pause();
  const funnel = await page.evaluate(() => {
    const item = document.querySelectorAll(".recharts-funnel-trapezoid")[1];
    const box = item.getBoundingClientRect();
    return {x: Math.round(box.x + (box.width * 0.25)), y: Math.round(box.y + (box.height / 2))};
  });
  await driver.move(page, funnel);
  await pause();
  const shown = await page.evaluate((point) => {
    const group = document.querySelector(".pictor-ak1");
    const before = group.previousElementSibling;
    const layers = [...group.closest(".recharts-trapezoids").children];
    const label = layers.findIndex((layer) => layer.matches(".recharts-label-list")) > layers.indexOf(group.parentElement);
    return {group: getComputedStyle(group).display, before: getComputedStyle(before).display, label, hovered: document.elementFromPoint(point.x, point.y)?.getAttribute("class")};
  }, funnel);
  check(driver, "a hovered funnel item draws its active shape in place under its label", shown.group !== "none" && shown.before === "none" && shown.label, JSON.stringify(shown));

  const active = await page.evaluate(() => document.querySelectorAll(".recharts-active-bar").length);
  check(driver, "the bars' activeIndex draws three active layers", active === 3, String(active));

  const tick = await page.evaluate(() => {
    const text = [...document.querySelectorAll(".recharts-xAxis .recharts-cartesian-axis-tick text")].find((t) => t.textContent === "Page C");
    text.scrollIntoView({block: "center"});
    const box = text.getBoundingClientRect();
    return {x: Math.round(box.x + box.width / 2), y: Math.round(box.y + box.height / 2)};
  });
  await driver.click(page, tick);
  const clicked = await driver.wait(page, () => document.body.innerText.includes("You clicked the tick Page C."), undefined, 10000).then(() => true, () => false);
  check(driver, "a tick's click posts its value", clicked);
  const yTick = await page.evaluate(() => {
    const text = [...document.querySelectorAll(".recharts-yAxis .recharts-cartesian-axis-tick text")].find((t) => t.textContent === "5000");
    const box = text.getBoundingClientRect();
    return {x: Math.round(box.x + box.width / 2), y: Math.round(box.y + box.height / 2)};
  });
  await driver.click(page, yTick);
  check(driver, "a number tick's click posts its value", await driver.wait(page, () => document.body.innerText.includes("You clicked the tick 5000."), undefined, 10000).then(() => true, () => false));

  let before = await window(page);
  let at = await slideCenter(page);
  await page.mouse.move(at.x, at.y);
  await page.mouse.down();
  for (let step = 1; step <= 6; step++) {
    await page.mouse.move(at.x + (step * 10), at.y);
  }
  await page.mouse.up();
  let after = await moved(driver, page, before);
  check(driver, "the slide dragged by the mouse moves the window", after[0] > before[0] && after[1] - after[0] === before[1] - before[0], `${before} to ${after}`);

  before = after;
  await page.focus(".pictor-bs");
  await driver.press(page, "ArrowLeft");
  after = await moved(driver, page, before);
  check(driver, "the slide's arrow key moves the window by one", after[0] === before[0] - 1 && after[1] === before[1] - 1, `${before} to ${after}`);

  before = after;
  at = await slideCenter(page);
  const finger = await driver.touches(page);
  await finger.start(at);
  for (let step = 1; step <= 6; step++) {
    await finger.move({x: at.x - (step * 10), y: at.y});
    await pause(30);
  }
  await finger.end();
  after = await moved(driver, page, before);
  check(driver, "the slide dragged by a finger moves the window", after[0] < before[0] && after[1] - after[0] === before[1] - before[0], `${before} to ${after}`);

  await driver.close(page);
}

const browser = await playwright.launch({executablePath: process.env.CHROMIUM ?? "/usr/bin/chromium"});
for (const make of [async () => chromium({browser, version: browser.version()}, {touch: true}), () => firefox({touch: true})]) {
  const driver = await make();
  console.log(`${driver.name} ${driver.version}`);
  try {
    await run(driver);
  } finally {
    await driver.quit();
  }
}

await browser.close();
const failed = results.filter((result) => !result.ok);
console.log(`${results.length - failed.length} of ${results.length} checks passed`);
process.exit(failed.length === 0 ? 0 : 1);
