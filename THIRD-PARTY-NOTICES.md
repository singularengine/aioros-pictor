# Third-party notices

Aioros.Pictor ports layout code and default styles from the following projects. Their notices are kept here as their
licenses require.

| Component | Where it is used | License |
|---|---|---|
| [Recharts](https://github.com/recharts/recharts) 2.15.4 | `src/Aioros.Pictor` (chart layout, polar layout and sector paths, the funnel, treemap, Sankey and sunburst layouts, axis ticks, bar positions, the brush, stacking offsets, legend and tooltip markup, default styles and class names) | MIT, Copyright (c) 2015-present recharts |
| [recharts-scale](https://github.com/recharts/recharts-scale) 0.4.5 | `src/Aioros.Pictor/Ticks/NiceTicks.cs` (nice tick values) | MIT, Copyright (c) 2015 Sen Yang |
| [Inter](https://rsms.me/inter/) 3.019 | `src/Aioros.Pictor/Text/InterMetrics.cs` (advance widths, through Aioros, and the kerning pairs of the reference font `tools/recharts/fonts/Inter-Regular-latin.woff2`) | SIL Open Font License 1.1, Copyright (c) 2016-2020 The Inter Project Authors (`tools/recharts/fonts/Inter-LICENSE.txt`) |
| [recharts.org](https://github.com/recharts/recharts.org)'s examples | Not shipped: the reference examples' data and markup (`tools/recharts/examples`, `tools/recharts/catalog`, each naming its `source`), used by the tests, the Gallery and `samples/Demo` | MIT, Copyright (c) 2016 recharts |
| [Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia) | Not shipped: a test dependency (`tests/Aioros.Pictor.Tests`, its tokens loaded from the package); `src/Aioros.Pictor.Avalonia/Theming/HostThemes.cs` names its resource keys, no code or value is ported | MIT |
| Recharts, React, esbuild, Playwright, puppeteer-core | Not shipped: `tools/recharts`, `samples/Demo` and the Gallery tests run them to render the reference examples, measure Recharts and drive browsers | MIT, Apache-2.0 |

## Recharts and recharts-scale (MIT)

```
The MIT License (MIT)

Copyright (c) 2015-present recharts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

recharts-scale carries the same license text with "Copyright (c) 2015 Sen Yang".
