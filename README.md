<img src="assets/icon/pictor.svg" alt="Pictor" width="72" height="72" align="right">

# Aioros.Pictor

**Charts in pure XAML, on the web and natively, for the [Aioros](https://github.com/singularengine/aioros) ecosystem.**

> **Public preview.** APIs can change between versions and it is not ready for production yet.

Aioros.Pictor gives you the charts of [Recharts](https://recharts.org) as XAML controls. You declare a chart the way you declare any other control, bind it to your view model, and it works on both targets:

- **On the web**, the chart is written as SVG straight into the response, from the page's compiled plan, with no JavaScript of your own.
- **On the desktop**, the same control draws itself natively.

## Install

Pictor needs an [Aioros](https://github.com/singularengine/aioros) app. Add the package to it:

```bash
dotnet add package Aioros.Pictor.Avalonia --version 0.1.0-preview.1
```

Then add the theme to `App.axaml`:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:p="https://aioros.dev/pictor">
  <Application.Styles>
    <FluentTheme />
    <p:PictorTheme />
  </Application.Styles>
</Application>
```

## Example

A page with a line chart:

```xml
<UserControl xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:p="https://aioros.dev/pictor" xmlns:m="using:Metrics.Models" x:DataType="m:SalesModel">
  <p:LineChart Data="{Binding Sales}" Height="300">
    <p:CartesianGrid StrokeDashArray="3 3" />
    <p:XAxis DataKey="Month" />
    <p:YAxis />
    <p:Tooltip />
    <p:Legend />
    <p:Line Type="Monotone" DataKey="Revenue" Stroke="#8884d8" />
  </p:LineChart>
</UserControl>
```

And its view model:

```csharp
namespace Metrics.Models;

public sealed record Sale(string Month, int Revenue);

public sealed class SalesModel
{
    public IReadOnlyList<Sale> Sales { get; } =
    [
        new("Jan", 4000),
        new("Feb", 3000),
        new("Mar", 5000),
    ];
}
```

`docs/getting-started.md` covers sizing, data, interaction and theming.

## What is included

**Charts.** Line, area, bar, composed, scatter, pie, radar, radial bar, funnel, treemap, Sankey and sunburst, with their axes, grids, legends, tooltips, labels and reference lines.

**Interaction on the web.** Tooltips and cursors on hover, touch, the keyboard, legend toggles, the brush and live charts pushed by the server. Clicks on items run commands, and an item can be a real link.

**Animation.** Enter animations, and update animations from the previous data to the new, on both targets. Reduced motion is honored.

**A native XAML control.** Themes, styles, resources and data binding work on every property, and custom content such as dots, ticks and tooltips is a `DataTemplate`.

The complete list of what is covered, row by row, is in [Coverage](docs/coverage.md).

## How fast is it

Writing a chart on the server is 96 times faster than Recharts' server rendering on average, across 42 reference examples. Details are in [Benchmarks](docs/benchmarks.md).

## Projects

| Project | Role |
| --- | --- |
| `src/Aioros.Pictor` | Data, layout, painting and the SVG writer, with no UI framework |
| `src/Aioros.Pictor.Avalonia` | The XAML controls and the desktop canvas |
| `samples/Gallery` | The demo app: a live dashboard, every chart family and comparison pages |
| `samples/Demo` | Each reference example drawn by Pictor next to Recharts' own render |
| `tools/recharts` | Renders the reference examples with Recharts |

## Building from source

```bash
dotnet build Aioros.Pictor.slnx
dotnet test --solution Aioros.Pictor.slnx
dotnet run --project samples/Gallery/Gallery              # the Gallery, at http://localhost:3010
dotnet run --project samples/Gallery/Gallery -- --desktop  # the same pages in a native window
```

The SDK is pinned in `global.json`. See [AGENTS.md](AGENTS.md) for how changes are made, and the [roadmap](docs/roadmap.md) for what comes next.

## License

MIT, as is the whole Aioros ecosystem: see [LICENSE](LICENSE).

Aioros.Pictor is an independent port and is not affiliated with or endorsed by the Recharts project. Recharts and recharts-scale are MIT-licensed, and text is measured with Inter's metrics (SIL OFL 1.1). Their notices are in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) and ship with every package.
