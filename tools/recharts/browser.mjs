import {chromium} from "playwright-core";
import {copyFileSync, existsSync, mkdirSync, writeFileSync} from "node:fs";
import {readFile} from "node:fs/promises";
import {createServer} from "node:http";
import {fileURLToPath} from "node:url";
import {dirname, extname, join, normalize} from "node:path";
import {client, directory as defaultDirectory} from "./bundle.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const candidates = [process.env.CHROMIUM, "/usr/bin/chromium", "/usr/bin/chromium-browser", "/usr/bin/google-chrome"];
const types = {".html": "text/html; charset=utf-8", ".js": "text/javascript", ".woff2": "font/woff2"};

export const executable = candidates.find((path) => path && existsSync(path));
export const font = {family: "Inter", weight: 400, size: "14px", file: "fonts/Inter-Regular-latin.woff2"};

function serve(directory) {
  const server = createServer(async (request, response) => {
    const path = normalize(join(directory, new URL(request.url, "http://localhost").pathname));
    try {
      const body = await readFile(path.startsWith(directory) && !path.endsWith("/") ? path : join(directory, "index.html"));
      response.writeHead(200, {
        "Content-Type": types[extname(path)] ?? "text/html; charset=utf-8",
        "Cross-Origin-Opener-Policy": "same-origin",
        "Cross-Origin-Embedder-Policy": "require-corp",
      });
      response.end(body);
    } catch {
      response.writeHead(404).end();
    }
  });
  return new Promise((resolve) => server.listen(0, "127.0.0.1", () => resolve(server)));
}

/**
 * Opens the reference app in Chromium; with <paramref name="recharts3"/> the bundle resolves `recharts` to Recharts 3.8.0
 * (the `recharts3` alias), whose client render is the reference where Pictor follows Recharts 3.
 */
export async function open(recharts3 = false) {
  if (!executable) throw new Error("No Chromium found: set CHROMIUM to its executable.");
  const directory = recharts3 ? join(dirname(fileURLToPath(import.meta.url)), "build3") : defaultDirectory;
  await client(directory, recharts3 ? {recharts: join(dirname(fileURLToPath(import.meta.url)), "node_modules", "recharts3")} : undefined);
  mkdirSync(join(directory, "fonts"), {recursive: true});
  copyFileSync(join(here, font.file), join(directory, font.file));
  writeFileSync(join(directory, "index.html"), `<!doctype html>
<html><head><meta charset="utf-8"><style>
@font-face{font-family:Inter;font-weight:400;font-style:normal;src:url("fonts/Inter-Regular-latin.woff2") format("woff2")}
html{background:#fff}
body{font-family:Inter;font-size:14px;margin:0;background:#fff}
#host{display:inline-block}
</style></head>
<body><div id="host"></div><script src="client.js"></script></body></html>
`);
  const server = await serve(directory);
  const browser = await chromium.launch({executablePath: executable, args: ["--font-render-hinting=none"]});
  const page = await browser.newPage({viewport: {width: 800, height: 600}, deviceScaleFactor: 1, timezoneId: "UTC"});
  await page.goto(`http://127.0.0.1:${server.address().port}/index.html`);
  await page.waitForFunction(() => window.pictor !== undefined);
  const ready = await page.evaluate(async () => {
    await document.fonts.load("400 14px Inter");
    await document.fonts.ready;
    return {font: document.fonts.check("400 14px Inter"), isolated: self.crossOriginIsolated};
  });
  if (!ready.font) throw new Error("Inter did not load");
  const close = async () => {
    await browser.close();
    server.close();
  };
  return {browser, page, version: browser.version(), isolated: ready.isolated, close};
}
