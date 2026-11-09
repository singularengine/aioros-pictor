# Benchmarks

How Aioros.Pictor compares with Recharts 2.15.4 when both draw the same charts, and the latest results.

## What is measured

Both sides draw the same 42 Recharts reference examples from the same data, at the same size. Pictor's markup is compared with Recharts' element by element, so the work is the same.

- **Pictor on the server** is everything a request does for a chart: read the data, lay it out and write the markup.
- **Recharts on the server** is `renderToStaticMarkup` of the chart element, on a production build of React.

Pictor's numbers come from BenchmarkDotNet in Release mode on .NET 10. Both runtimes are pinned to the same four CPU cores.

## Latest results

Measured on 2026-10-15, on a self-hosted machine. A representative sample of the 42 examples:

| Example | Pictor | Allocated | Recharts | Times faster |
| --- | --- | --- | --- | --- |
| Simple line chart | 30.8 µs | 6,992 B | 3.36 ms | 109× |
| Simple bar chart | 34.3 µs | 8,056 B | 4.51 ms | 132× |
| Simple area chart | 27.3 µs | 5,264 B | 2.27 ms | 83× |
| Stacked bar chart | 30.8 µs | 8,464 B | 4.08 ms | 132× |
| Two-level pie chart | 32.2 µs | 6,304 B | 3.44 ms | 107× |
| Simple radar chart | 14.4 µs | 4,640 B | 1.80 ms | 125× |
| Simple treemap | 47.7 µs | 39,400 B | 6.53 ms | 137× |
| Simple Sankey diagram | 19.0 µs | 5,512 B | 501.8 µs | 26× |
| **All 42, geometric mean** | | | | **96×** (24× to 181×) |

When the data has not changed, a compiled page writes the chart from its kept markup, which takes about 1 µs or less for most of these examples.

Recharts also runs in the browser, where Pictor needs no script at all. That comparison is not included here.

## Running it

```bash
dotnet run -c Release --project bench/Aioros.Pictor.Benchmarks -- --filter '*'
samples/Demo/run.sh     # the comparison page, in samples/Demo/out/index.html
```

`samples/Demo/run.sh` installs Recharts' Node packages when they are missing. On a machine doing other work, compare ratios and not absolute times.
