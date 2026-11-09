# Recharts reference

Pictor is a port of Recharts to Avalonia XAML. This folder renders the official Recharts examples with Recharts itself, so
the C# side can compare its output with the original markup and pixels, and it measures how fast Recharts renders them.

```
cd tools/recharts
npm ci
node render.mjs          # examples/<name>.json, out/<name>.html, out/<name>.svg (server render)
node screenshot.mjs      # out/<name>.client.html, out/<name>.client.json, out/<name>.png (client render in Chromium)
node bench.mjs           # bench-results/recharts-ssr.json
node bench-client.mjs    # bench-results/recharts-client.json
```

`screenshot.mjs` and `bench-client.mjs` need a Chromium: `/usr/bin/chromium` by default, or the path in `CHROMIUM`.
`npm ci` downloads no browser (`playwright-core` drives the installed one). The outputs are committed, so the C# build
needs no Node; run the scripts again only when an example or a pinned version changes.

## Versions

Recharts is pinned to 2.15.4, the last 2.x release. Recharts 3 (3.10.1 checked) renders an empty
`<div class="recharts-wrapper">` with `renderToStaticMarkup`: its layout lives in a Redux store filled by effects, which
never run on the server. Recharts 2 lays the chart out during render, so the server markup is complete. React and
React DOM are pinned to 18.3.1, the line Recharts 2 was built and tested against.

Recharts 3.8.0 is installed beside it as `recharts3` (`npm:recharts@3.8.0`), the version shadcn/ui's charts pin. Pictor
takes the radial bar charts from it: `node recharts3.mjs [Name...]`
renders every `RadialBarChart` example in Chromium with Recharts 3 (bundled into `build3/` with `recharts` aliased to
it) and writes `out/recharts3/<name>.client.html`, `.client.json`, `.png`, each interaction's
`out/recharts3/<name>.<i>.client.html` and `.clicks.json`; `node motion.mjs --recharts3 Case...` records the radial bar
motion cases into `out/recharts3/motion/`. The C# side reads a file of `out/recharts3` in place of its Recharts 2 twin
(`ReferenceFiles`). Recharts 3 wraps what it draws in `g.recharts-zIndex-layer_*` groups ordered by z-index, so
`MarkupComparison` compares such a render class by class (each class' elements in document order), leaving the groups
out, and reads the polar axes' renamed classes (`recharts-polar-angle-axis-line`, `-tick-line`,
`recharts-polar-radius-axis-line`) as the Recharts 2 names Pictor writes.

Both renders go through an esbuild bundle (`bundle.mjs`) that resolves the packages' `module` entry, as an application
bundler does. This matters: Recharts' ES build draws curves with `d3-shape` 3.2, which rounds path coordinates to 3
decimals, while its CommonJS build (what plain `node` loads) uses a vendored older `d3-shape` that prints full doubles.
The reference is the bundled output that browsers show.

## Files

- `examples.mjs` holds the examples as plain descriptions (chart kind, size, margin, data, children with their props) and
  turns them into React elements; `render.mjs` writes each description to `examples/<name>.json` for the C# side.
- `out/<name>.html` is the server markup: the `recharts-wrapper` div with the `<svg>`, the legend and the tooltip.
  `out/<name>.svg` is the `<svg>` element alone, with an `xmlns` added so it opens as a file.
- `out/<name>.client.html` is the wrapper's `outerHTML` after mounting in Chromium, `out/<name>.png` its screenshot at
  device scale 1, and `out/<name>.client.json` the font and the measured legend box. The page uses Inter 400 at 14px
  (`fonts/`, OFL, the font Aioros pages use) and waits for it before mounting.
- An example may list `interactions`: sequences of pointer and keyboard steps (`{hover: [x, y]}`, `{click: [x, y]}`, `{focus: true}` focusing the surface, `{key: "ArrowRight"}` pressing a key) that
  `screenshot.mjs` plays in Chromium on a fresh mount, writing the wrapper's `outerHTML` afterwards to
  `out/<name>.<i>.client.html` (Recharts' visible tooltip, cursor and active shapes, which the C# side compares with
  Pictor's hover states). Formatters are written `{"$format": "{0} kg"}`: a .NET composite format on the C# side, a
  function replacing `{0}` with the value on Recharts'. `node render.mjs Name...` and `node screenshot.mjs Name...`
  render only the named examples.
- A click handler is written `{"$click": "name"}`: Recharts receives a handler that records its name, the index it
  received (an item handler's second argument, or the `activeTooltipIndex` or `index` of its first) and the label in
  `window.pictorClicks`; after each interaction of an example holding one, `screenshot.mjs` writes what the handlers
  received, in the order they ran, to `out/<name>.<i>.clicks.json` (the fidelity matrix's click criteria read them).
- Custom content is written `{"$content": "Name"}`: Recharts receives the element or render function `catalog/custom.mjs`
  exports under that name (`contents`), the C# side leaves the slot to the template its test sets (the templates of
  `tests/Aioros.Pictor.Tests/Custom/CustomTemplates.axaml`).
- An example may set `container` (ResponsiveContainer's props, such as `{width: 500, aspect: 2}`): the chart is then
  wrapped in Recharts' `ResponsiveContainer`, which renders nothing on the server (it measures its parent after
  mount), so only the client render is a reference.
- `node ticks.mjs Name...` records where Recharts' cartesian tick texts take the pointer in Chromium (each text's first
  character's start, advance and box height) as `out/<name>.ticks.json`, which the tick buttons are compared with.
  `node strokes.mjs` records the inline styles react-smooth gives a treemap's rectangles when its
  `isUpdateAnimationActive` is off (a CSS transition, which Playwright's clock does not drive, so the time it is set is
  what is recorded) as `out/motion/<case>.styles.json`. An example's handler written as `{$content: "RecordTick"}`
  (`catalog/final.mjs`) keeps what Recharts passed it on the chart's wrapper (`data-tick`).
- Recharts numbers its ids from a global counter (`recharts1-clip`, `recharts-line-3`, ...). `ids.mjs` renumbers each
  family from 1 in order of appearance, so every output is deterministic.

Every series sets `isAnimationActive={false}`. Recharts' default is `!Global.isSsr`: animations are off on the server
and on in the browser (Line and Area 1500 ms, Bar and Scatter 400 ms, starting at 0 ms).

The server render and the client render differ: the legend's size is measured after mount, so on the client the plot
area shrinks by the legend's height; the client also hides overlapping ticks (on the server every tick is kept) and
positions the hidden tooltip. Compare pixels with the client render.

## Benchmarks

`bench.mjs` times `renderToStaticMarkup` of each example in Node with React's production build: one second of warm-up,
then each render timed with `process.hrtime.bigint` for three seconds (500 to 20 000 samples), reporting the median,
p95, minimum, mean, operations per second (from the median) and the bytes of the `<svg>` and of the whole markup.

`bench-client.mjs` times the mount in Chromium: a fresh `createRoot`, then `flushSync(() => root.render(chart))` timed
with `performance.now`, which includes the synchronous mount work (the legend measurement and the re-render it
triggers). The page is cross-origin isolated, so the timer resolution is 5 us. 100 warm-up renders, then 500 timed ones.
