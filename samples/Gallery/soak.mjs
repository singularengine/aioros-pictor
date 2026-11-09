// Usage: node samples/Gallery/soak.mjs [baseUrl] [seconds] [out.json] [live=8] [loops=2] [paced=1]
// Soaks a Gallery running in Release (`dotnet samples/Gallery/Gallery/bin/Release/net10.0/Gallery.dll`, Production):
// live connections to the dashboard (`/live`, some with the plan, three products and ranges) reconnecting every 20 to
// 90 s, and loops over every route: document GETs, client-navigation GETs (half answered as the page inside its kept
// layout, half as dynamics), posts of each page's form with one of its buttons, and the framework's static files.
// Paced (the default), the loops send 60 documents, 60 navigations, 40 posts and 40 static files a second whatever
// the server's speed, so two builds are compared at the same load; with paced=0 they run closed. Writes each request
// kind's count, rate, mean size and p50/p99 latency, and what the live connections received. Read the server's
// allocation and collections beside it with `dotnet-counters collect --counters System.Runtime` (decisions, "Pooled
// render buffers").
import { writeFileSync } from 'node:fs';

const [base = 'http://127.0.0.1:3311', seconds = '600', out = 'soak.json', sseCount = '8', loopCount = '2', paced = '1'] = process.argv.slice(2);
const rates = { document: 30, partial: 30, post: 20, static: 40 };
const pacer = kind => { const start = performance.now(); let i = 0; return async () => { if (paced !== '1') return; const due = start + (i++ * 1000) / rates[kind]; const wait = due - performance.now(); if (wait > 0) await new Promise(r => setTimeout(r, wait)); }; };
const end = Date.now() + Number(seconds) * 1000;
let seed = 2026;
const rnd = () => ((seed = (seed * 1103515245 + 12345) & 0x7fffffff) / 0x7fffffff);
const AE = 'gzip, deflate, br, zstd';
const routes = ['/', '/?app=api&range=live', '/?app=mobile&range=live', '/?app=web&range=24h', '/?app=web&range=7d', '/?app=web&range=30d',
  '/about', '/area', '/axes', '/bar', '/binding', '/composed', '/features', '/funnel', '/interactions', '/interactive', '/line', '/more',
  '/pie', '/radar', '/radial-bar', '/ranges', '/sankey', '/scatter', '/sunburst', '/themes', '/tips', '/treemap', '/updates'];
const stats = {};
const record = (kind, ms, bytes, status) => {
  const s = (stats[kind] ??= { n: 0, bytes: 0, errors: 0, lat: [] });
  s.n++; s.bytes += bytes; s.lat.push(ms);
  if (status >= 400 || status === 0) s.errors++;
};
const meta = (html, name) => (html.match(new RegExp(`<meta name="${name}" content="([^"]*)"`)) || [])[1];
const pages = new Map();

async function timed(kind, url, init = {}) {
  const t = performance.now();
  try {
    const r = await fetch(base + url, { ...init, headers: { 'accept-encoding': AE, 'sec-fetch-site': 'same-origin', ...(init.headers || {}) } });
    const text = await r.text();
    record(kind, performance.now() - t, text.length, r.status);
    return { r, text };
  } catch (e) {
    record(kind, performance.now() - t, 0, 0);
    return { r: null, text: '' };
  }
}

function learn(route, html) {
  const fields = [...html.matchAll(/<input[^>]*>/g)].map(m => m[0])
    .map(i => [(i.match(/name="([^"]*)"/) || [])[1], (i.match(/value="([^"]*)"/) || [, ''])[1]]).filter(([n]) => n);
  const buttons = [...html.matchAll(/<button[^>]*name="_btn"[^>]*value="([^"]*)"/g)].map(m => m[1].replaceAll('&amp;', '&'));
  const statics = [...html.matchAll(/(?:href|src)="(\/_aioros\/[^"]*)"/g)].map(m => m[1]);
  pages.set(route, { plan: meta(html, 'aioros-plan'), layout: meta(html, 'aioros-layout'), slots: meta(html, 'aioros-slots'), fields, buttons, statics });
}

async function documentLoop() {
  const pace = pacer('document');
  while (Date.now() < end) {
    for (const route of routes) {
      if (Date.now() >= end) return;
      await pace();
      const { text } = await timed('document', route);
      if (text) learn(route, text);
    }
  }
}

async function navigationLoop() {
  const pace = pacer('partial');
  while (Date.now() < end) {
    for (const route of routes) {
      if (Date.now() >= end) return;
      const p = pages.get(route);
      if (!p) continue;
      await pace();
      const headers = { 'x-aioros-navigation': '1', 'x-aioros-layout': p.layout ?? '' };
      if (rnd() < 0.5 || !p.plan) {
        await timed('partial', route, { headers });
      } else {
        await timed('dynamics', route, { headers: { ...headers, 'x-aioros-plan': p.plan } });
      }
    }
  }
}

async function postLoop() {
  const pace = pacer('post');
  while (Date.now() < end) {
    for (const route of routes) {
      if (Date.now() >= end) return;
      const p = pages.get(route);
      if (!p || (p.buttons.length === 0 && p.fields.length === 0)) continue;
      await pace();
      const body = new URLSearchParams(p.fields);
      if (p.buttons.length) body.set('_btn', p.buttons[Math.floor(rnd() * p.buttons.length)]);
      const headers = { 'x-aioros-navigation': '1', 'x-aioros-layout': p.layout ?? '', 'content-type': 'application/x-www-form-urlencoded', origin: base };
      if (p.plan && rnd() < 0.5) headers['x-aioros-plan'] = p.plan;
      if (p.slots) headers['x-aioros-slots'] = p.slots;
      await timed('post', route, { method: 'POST', body: body.toString(), headers, redirect: 'manual' });
    }
  }
}

async function staticLoop() {
  const pace = pacer('static');
  while (Date.now() < end) {
    const all = [...new Set([...pages.values()].flatMap(p => p.statics.concat(p.plan ? [`/_aioros/plan/${p.plan}`] : [])))];
    if (all.length === 0) { await new Promise(r => setTimeout(r, 200)); continue; }
    for (const url of all) {
      if (Date.now() >= end) return;
      await pace();
      await timed('static', url);
    }
  }
}

const live = { connects: 0, events: 0, views: 0, bytes: 0, errors: 0 };
async function sseClient(index) {
  while (Date.now() < end) {
    const p = pages.get('/');
    const q = new URLSearchParams(index % 4 === 1 ? 'app=api&range=live' : index % 4 === 2 ? 'app=web&range=7d' : '');
    if (p?.plan && index % 2 === 0) q.set('plan', p.plan);
    const ac = new AbortController();
    const life = Math.min(20000 + rnd() * 70000, end - Date.now());
    const timer = setTimeout(() => ac.abort(), life);
    live.connects++;
    try {
      const r = await fetch(`${base}/live${q.size ? '?' + q : ''}`, { signal: ac.signal, headers: { accept: 'text/event-stream', 'accept-encoding': AE } });
      const decoder = new TextDecoder();
      for await (const chunk of r.body) {
        const s = decoder.decode(chunk, { stream: true });
        live.bytes += chunk.length;
        live.events += (s.match(/\n\n/g) || []).length;
        live.views += (s.match(/event: views/g) || []).length;
      }
    } catch (e) {
      if (e.name !== 'AbortError') { live.errors++; await new Promise(r => setTimeout(r, 1000)); }
    } finally { clearTimeout(timer); }
  }
}

const loops = Number(loopCount);
const started = Date.now();
await timed('document', '/').then(({ text }) => learn('/', text));
await Promise.all([
  ...Array.from({ length: Number(sseCount) }, (_, i) => sseClient(i)),
  ...Array.from({ length: loops }, documentLoop),
  ...Array.from({ length: loops }, navigationLoop),
  ...Array.from({ length: loops }, postLoop),
  staticLoop(),
]);
const elapsed = (Date.now() - started) / 1000;
const q = (a, f) => { const s = [...a].sort((x, y) => x - y); return s[Math.min(s.length - 1, Math.floor(f * s.length))] ?? 0; };
const summary = { elapsed, live, kinds: {} };
for (const [k, s] of Object.entries(stats)) {
  summary.kinds[k] = { n: s.n, rps: +(s.n / elapsed).toFixed(1), errors: s.errors, avgKB: +(s.bytes / s.n / 1024).toFixed(1), p50: +q(s.lat, 0.5).toFixed(2), p99: +q(s.lat, 0.99).toFixed(2) };
}
writeFileSync(out, JSON.stringify(summary, null, 2));
console.log(JSON.stringify(summary, null, 2));
