# Working on this repository

Guidance for every agent and person working on Aioros.Pictor, the chart library of Aioros: Recharts' charts as XAML,
drawn by the chart engine Aioros.Graphs. It follows Aioros's rules (`aioros/AGENTS.md`, `aioros/docs/principles.md`),
adapted to a library that ports Recharts. The rules are enforced by the architecture tests and the analyzers wherever
they can be.

## Before you change anything

1. Read the [README](README.md) and the [roadmap](docs/roadmap.md).
2. Build and test. Both must be green before and after your change. The machine may be shared: build with `-m:4`.
3. Do not reinvent and do not assume. The reference is Recharts' source and its output, at the version pinned in
   `tools/recharts` (2.15.4): read the component you port (`tools/recharts/node_modules/recharts/es6`), port its layout
   operation for operation, and compare the result with Recharts' own render of the same example
   (`tools/recharts/out`). note any divergence and its reason in its acceptance criterion.

```
dotnet build Aioros.Pictor.slnx -m:4
dotnet test --solution Aioros.Pictor.slnx
```

The SDK is Aioros's (`global.json`, 10.0.403): link Aioros's repository-local SDK with
`ln -s ../aioros/.dotnet .dotnet` or install it there with `eng/install-sdk.sh`. CI's `dependencies` workflow fails when that
SDK lags a security release of .NET or a package has a known advisory (`eng/check-dependencies.py`, as Aioros's). Aioros and Aioros.Graphs come from nuget.org.

## Writing tests

Tests come before the code they verify.

1. **Write the acceptance criteria first**, in the area's file in [docs/acceptance](docs/acceptance), as a table row
   `| AREA-NN | criterion |`. The identifier is never reused. Each criterion describes one observable behavior in
   Given / When / Then form, names the expected outcome precisely, and covers the rejected paths too.
2. **Check for duplicates** in the acceptance files and the tests; extend an existing test instead of adding one.
3. **Generate the golden cases from Recharts** when the behavior is Recharts': `tools/golden` (recharts-scale's
   ticks) and `tools/recharts` (whole examples rendered by Recharts on the server and in Chromium). Commit the JSON
   they write; compare exactly, and give any tolerance a reason in its acceptance criterion.
4. **One test per criterion**, carrying it: `[Trait("AC", "TICKS-01")]`. The architecture tests fail on a criterion
   without exactly one test and on a test without exactly one known criterion. The test name states the behavior in
   PascalCase, with the `Async` suffix for asynchronous tests.
5. Write the test first and watch it fail, then write the code.
6. No sleeps, no waits on time. Prove a new rule can fail: break it on purpose, watch the test fail, restore it.

## Architecture

- `src/Aioros.Pictor` is the web path: specs, data, layout, painting and the SVG writer Aioros calls at request time.
  It references Aioros.Graphs, Aioros.Graphs.Svg and Aioros.Runtime only: no Avalonia, no ASP.NET Core.
- `src/Aioros.Pictor.Avalonia` holds the XAML controls and the desktop drawing; only it, the tests, the benchmarks and
  the samples reference Avalonia (Aioros's version, 12.1.3).
- One layout and one painter for both targets: the painter is generic over a canvas struct (SVG bytes on the web,
  Avalonia's `DrawingContext` on the desktop), so the two renders cannot drift.
- **Composition over inheritance.** Every class and record class is `sealed` or `static`; benchmark classes are the
  exception BenchmarkDotNet forces.
- **No locks, no concurrent collections, no reflection** in `src/`. Data items are read through the accessors Aioros
  generates for the app's types.
- **Performance is the point.** Each chart is written straight to the response as UTF-8; no DOM, no strings for
  coordinates, no allocation per point on the hot paths. Measure with `bench/Aioros.Pictor.Benchmarks`
  (`[MemoryDiagnoser]`) and compare with Recharts (`tools/recharts/bench.mjs`).
- **Screaming architecture.** Folders and namespaces name what the code does: `Specs`, `Data`, `Ticks`, `Layout`,
  `Painting`, `Svg`.

## Code

- Modern C# 14 on .NET 10. The smallest code that states the behavior clearly.
- No comments. XML documentation on the public API only (required: the projects generate documentation).
- No type longer than 600 lines; Sonar's cognitive complexity (15), nesting (3) and method length (80) limits apply.
- Warnings are errors.

## Documentation

- Everything in the repository is written in English.
- Update the roadmap in the same change when something it says changes, and keep THIRD-PARTY-NOTICES.md in step with what is ported.
- **Branding.** Product surfaces (READMEs, Galleries, demos, page metadata, package and template descriptions) lead
  with the ecosystem's brands: Aioros ("The XAML framework for web and native apps"), Aioros.Cosmo, Aioros.Pictor,
  Aioros.Graphs. Avalonia is named once, as a compatibility statement where it helps a developer; shadcn/ui, Recharts
  and d3 appear as the reference on fidelity and compare pages and in the credits; comparisons say "native" or "desktop
  render". Internal technical documents, code and type names keep their names.
