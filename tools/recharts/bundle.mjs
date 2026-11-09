import {build} from "esbuild";
import {mkdirSync} from "node:fs";
import {fileURLToPath, pathToFileURL} from "node:url";
import {dirname, join} from "node:path";

const here = dirname(fileURLToPath(import.meta.url));
export const directory = join(here, "build");

const common = {
  bundle: true,
  mainFields: ["module", "main"],
  define: {"process.env.NODE_ENV": '"production"'},
  logLevel: "warning",
};

export async function server() {
  mkdirSync(directory, {recursive: true});
  const outfile = join(directory, "server.mjs");
  await build({...common, entryPoints: [join(here, "server.mjs")], platform: "node", format: "esm", target: "node22", external: ["react", "react-dom", "react/*", "react-dom/*"], outfile});
  return import(pathToFileURL(outfile).href);
}

export async function client(into = directory, alias = undefined) {
  mkdirSync(into, {recursive: true});
  const outfile = join(into, "client.js");
  await build({...common, entryPoints: [join(here, "client.mjs")], platform: "browser", format: "iife", target: "es2022", minify: true, outfile, alias});
  return outfile;
}
