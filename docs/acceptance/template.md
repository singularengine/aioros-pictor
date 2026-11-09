# Template

`dotnet new pictor` (the `Aioros.Pictor.Templates` package): an Aioros app built with Pictor, out of the box. Its home
page is the Gallery's live analytics dashboard (see [dashboard.md](dashboard.md)): KPIs, traffic, conversion, devices and
an event feed for three dummy products, drawn by Pictor's charts and pushed by the server as a hosted ticker moves its
moment, under a header with the light and dark switch.

| AC | Criterion |
| --- | --- |
| TEMPLATE-01 | Given Pictor's packages and the template package packed to a local feed beside Aioros's and Graphs's, when `dotnet new pictor` creates an app from it in a private template hive (`--debug:custom-hive`) and the app is restored into a private package cache, built and run with `--precompile=<file>`, then the build reports 0 warnings and 0 errors, the run exits with 0 and reports every page precompiled (`Precompiled N of N pages`, N > 0), and nothing is written to the user's template hive or NuGet cache. Precedent: Aioros's own template test (Aioros CLI-06) and Cosmo's (Cosmo TEMPLATE-01). |
