# Getting started

How to add a chart to an Aioros app, size it, feed it data and make it respond. If you do not have an app yet, follow the [Aioros getting started guide](https://github.com/singularengine/aioros/blob/main/docs/getting-started.md) first.

## Add the package and the theme

```bash
dotnet add package Aioros.Pictor.Avalonia --version 0.1.0-preview.1
```

In `App.axaml`, declare the namespace and add the theme after your own:

```xml
<Application xmlns="https://github.com/avaloniaui" xmlns:p="https://aioros.dev/pictor">
  <Application.Styles>
    <FluentTheme />
    <p:PictorTheme />
  </Application.Styles>
</Application>
```

## Draw a chart

A chart takes its rows from `Data`, and each series and axis names the member it reads with `DataKey`:

```xml
<p:BarChart Data="{Binding Sales}" Height="300">
  <p:XAxis DataKey="Month" />
  <p:YAxis />
  <p:Tooltip />
  <p:Bar DataKey="Revenue" Fill="#8884d8" />
</p:BarChart>
```

`Data` accepts any list of objects. There is nothing else to register, and the same XAML works on the web and on the desktop.

## Size it

A chart fills the width of its parent:

| Set | Result |
| --- | --- |
| `Height` | Fills the width, at that height |
| `Aspect` | Fills the width, with the height following from the ratio |
| `Aspect` and `MinHeight` | As above, but never shorter than `MinHeight` |
| `ResponsiveWidths` | On the web, lays the chart out at each of those widths |

On the web there is no script to measure the client's exact width, so a chart is laid out when the page is written and scales from there. List the widths you care about in `ResponsiveWidths`, for example `320,640,1000`, so that a phone gets a layout of its own with readable text.

## Respond to the user

A click on an item runs a command:

```xml
<p:Bar DataKey="Revenue" ClickCommand="{Binding SelectCommand}" />
```

To make an item a real link, name the member that holds its address with `HrefKey`:

```xml
<p:Bar DataKey="Revenue" HrefKey="Link" />
```

To style each item from its data, use `FillKey` and `OpacityKey`. They are available on bars, scatter points, pie sectors, radial bars and funnel segments.

## Format values

`TickFormat` on an axis takes a composite format string such as `{}{0:MMM}`, which follows the current culture. For anything more specific, `TickFormatter` takes a value converter.

## Theme it

`PictorTheme` provides light and dark looks, and its resource keys can be overridden per app, per page or per chart. Chart elements take ordinary styles and control themes. Custom content, such as a dot, a tick or a tooltip, is a `DataTemplate`.

## Live charts

Put the chart on a page marked `a:Web.Live="True"`. When the data changes, the server pushes the new chart, and it animates from the previous data.

## Learn more

The Gallery in `samples/Gallery` shows every chart family and feature, and [Coverage](coverage.md) lists everything that is supported.
