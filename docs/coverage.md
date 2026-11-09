# Recharts 2.15.4 coverage

Counts: done 416 · partial 0 · missing 0 · n/a 45 · fidelity matrix 120 of 120 cells filled

Finished: true

This page lists every export, prop and behavior of Recharts 2.15.4 and where Pictor stands on each. A row is `done` when Pictor matches Recharts for every chart that supports it, in XAML, in render and under the mouse, touch and the keyboard, on the web and on the desktop. A `done` row names the acceptance criteria that verify it, and an `n/a` row gives its reason. The [fidelity matrix](#fidelity-matrix) checks every chart family against Recharts in each aspect.

## 1. Exports

From `types/index.d.ts`, every runtime export:

| Group | Exports |
|---|---|
| Containers | `Surface`, `Layer` |
| Components | `Legend`, `DefaultLegendContent`, `Tooltip`, `DefaultTooltipContent`, `ResponsiveContainer`, `Cell`, `Text`, `Label`, `LabelList`, `Customized` |
| Shapes | `Sector`, `Curve`, `Rectangle`, `Polygon`, `Dot`, `Cross`, `Symbols`, `Trapezoid` |
| Polar | `PolarGrid`, `PolarRadiusAxis`, `PolarAngleAxis`, `Pie`, `Radar`, `RadialBar` |
| Cartesian | `Brush`, `ReferenceLine`, `ReferenceDot`, `ReferenceArea`, `CartesianAxis`, `CartesianGrid`, `Line`, `Area`, `Bar`, `Scatter`, `XAxis`, `YAxis`, `ZAxis`, `ErrorBar` |
| Charts | `LineChart`, `BarChart`, `PieChart`, `Treemap`, `Sankey`, `RadarChart`, `ScatterChart`, `AreaChart`, `RadialBarChart`, `ComposedChart`, `SunburstChart`, `FunnelChart` |
| Other | `Funnel`, `Global` (config: `isSsr`); type `LegendType` |

Pictor's XAML elements (`A/`): `LineChart`, `AreaChart`, `BarChart`, `ScatterChart`, `ComposedChart`
(`A/Charts/CartesianChart.cs`), `PieChart`, `RadarChart`, `RadialBarChart` (`A/Charts/PolarCharts.cs`), `FunnelChart`
and `Funnel` (`A/Charts/FunnelChart.cs`), `Treemap` (`A/Charts/Treemap.cs`), `Sankey` (`A/Charts/Sankey.cs`),
`SunburstChart` (`A/Charts/SunburstChart.cs`), `XAxis`, `YAxis` (`A/Elements/Axes.cs`), `ZAxis`, `ReferenceArea`,
`ReferenceDot`, `ErrorBar` (`A/Elements/CartesianExtras.cs`), `CartesianGrid`, `ReferenceLine`, `LabelList`, `Cell`
(`A/Elements/Parts.cs`), `Legend`, `LegendPayload` (`A/Elements/Legend.cs`), `Tooltip` (`A/Elements/Tooltip.cs`),
`Line`, `Area`, `Bar`, `Scatter` (`A/Elements/Series.cs`), `Brush` (`A/Elements/Brush.cs`), `PolarGrid`,
`PolarAngleAxis`, `PolarRadiusAxis`, `Pie`, `Radar`, `RadialBar`, `ActiveShape` (`A/Elements/PolarElements.cs`),
`Label` (`A/Elements/Label.cs`), `Customized` (`A/Content/Customized.cs`). No `Text`, `CartesianAxis`, shapes, `Surface`, `Layer`, `DefaultLegendContent`,
`DefaultTooltipContent` or `ResponsiveContainer` element (grepped `src/`).

| Export | Status | Where / what is missing | Criteria |
|---|---|---|---|
| LineChart, AreaChart, BarChart, ComposedChart, ScatterChart | done | `A/Charts/CartesianChart.cs` (props in §2.1) | CHARTS-01, CHARTS-07, CHARTS-09, CHARTS-15, SCATTER-01 |
| PieChart, RadarChart, RadialBarChart | done | `A/Charts/PolarCharts.cs` | POLAR-01, POLAR-06, POLAR-08 |
| FunnelChart, Funnel | done | `A/Charts/FunnelChart.cs` | FUNNEL-01 |
| Treemap | done | `A/Charts/Treemap.cs` | TREEMAP-01 |
| Sankey | done | `A/Charts/Sankey.cs` | SANKEY-01 |
| SunburstChart | done | `A/Charts/SunburstChart.cs` (props in §2.8) | SUNBURST-01, SUNBURST-08 |
| XAxis, YAxis, ZAxis, CartesianGrid, Line, Area, Bar, Scatter, ErrorBar, ReferenceLine, ReferenceArea, ReferenceDot, Brush | done | see their tables | CHARTS-01, CHARTS-05, CHARTS-07, CHARTS-09, CHARTS-19, CHARTS-20, SCATTER-02, BRUSH-01 |
| CartesianAxis | n/a | a standalone axis drawn by hand at given coordinates; in XAML the chart's `XAxis`/`YAxis` cover it; Pictor's axis painter is internal (`P/Painting/ChartPainter.cs`) | |
| Pie, Radar, RadialBar, PolarGrid, PolarAngleAxis, PolarRadiusAxis | done | `A/Elements/PolarElements.cs` | POLAR-01, POLAR-06, POLAR-08 |
| Legend, Tooltip, Cell, LabelList | done | `A/Elements/Legend.cs`, `A/Elements/Tooltip.cs`, `A/Elements/Parts.cs` | CHARTS-01, CHARTS-06, CHARTS-13, TOOLTIP-01 |
| Label | done | `Label` element (`A/Elements/Label.cs`) as children of axes, references and the polar radius axis, several per element after the element's own `Label` text | AXES-01, AXES-02, POLARAXIS-01..03, GAPS-28, GAPS-17 |
| Text | n/a | used standalone it is an SVG text with word wrapping: Avalonia's `TextBlock` (`TextWrapping`, `MaxLines`, `TextTrimming`) serves XAML; inside a chart Recharts renders it only through custom content (`content`, `Customized`, element ticks), counted there; Pictor ports its wrapping internally for ticks and labels (`P/Text/TextLines.cs`) | |
| Customized | done | `Customized` element: a template whose context is the chart's layout (`LayoutContext`: its size and the plot's box), drawn in its place among the chart's elements | CUSTOM-12 |
| ResponsiveContainer | done | every chart fills its parent and takes `Aspect`; props in §2.6, the exact re-layout at the client's width n/a | RESPONSIVE-01, RESPONSIVE-02, WEB-05 |
| DefaultLegendContent, DefaultTooltipContent | n/a | building blocks for custom `content`; Pictor writes their markup internally (`P/Layout/LegendLayout.cs`, `P/Painting/TipPainter.cs`, `P/Svg/HoverMarkup.cs`) | |
| Sector, Curve, Rectangle, Polygon, Dot, Cross, Symbols, Trapezoid | n/a as standalone elements | used standalone they are plain SVG shapes: Avalonia's `Path`/`Ellipse`/`Polygon` serve XAML. Pictor's geometry exists internally (`P/Painting/Shapes.cs`, `P/Painting/PolarShapes.cs`). Inside custom content Pictor's template shapes (`Sector`, `Rectangle`, `Dot`, `Symbols`, `Layer`, `SvgText`, `A/Content/TemplateShapes.cs`) play their role, counted on the slots | |
| Surface, Layer | n/a | `<svg>` and `<g>` wrappers; the chart writes its own surface | |
| Global (`isSsr`) | n/a | React SSR switch; Pictor renders on the server by design | |

## 2. Components and props

### 2.1 Cartesian charts (LineChart, AreaChart, BarChart, ComposedChart, ScatterChart; `CategoricalChartProps`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| width, height | done | Avalonia `Width`/`Height`, taken into the spec (`A/Charts/Chart.cs` `WrittenSlot`) | WEB-02, DESKTOP-01 |
| data | done | `Chart.Data` (`A/Charts/Chart.cs`) | WEB-02, DESKTOP-03 |
| margin | done | `Chart.Padding` (`A/Charts/Chart.cs`;) | CHARTS-01, CHARTS-03 |
| layout | done | `CartesianChart.Layout`, `ChartLayoutKind` | CHARTS-16, CHARTS-17, CHARTS-18 |
| stackOffset | done | `CartesianChart.StackOffset`; all six (`P/Layout/Stacking.cs`) | CHARTS-08, STACK-01..05 |
| barCategoryGap, barGap | done | `A/Charts/CartesianChart.cs` (number or percent text) | CHARTS-09, STACK-06, STACK-07 |
| barSize | done | a number or Recharts' percent text | STACK-06 |
| maxBarSize | done | `A/Charts/CartesianChart.cs` | STACK-07 |
| baseValue (AreaChart) | done | `CartesianChart.BaseValue`: `dataMin`, `dataMax` or a number, inherited by areas | RANGE-04, RANGE-09 |
| children | done | `Chart.Elements` content collection | CHARTS-01 |
| title | done | `Chart.Title` (`A/Charts/Chart.cs`) written as the SVG `<title>` (and the web wrapper's accessible name) | GAPS-01, GAPS-17 |
| desc | done | `Chart.Desc`, written as the SVG `<desc>` | GAPS-01, GAPS-17 |
| syncId, syncMethod | done | `SyncId`, `SyncMethod` (`index`, `value`): page-wide `:has()` rules on the web, a sync group on the desktop that also moves synced brushes | SYNC-01..04 |
| compact | done | `CartesianChart.Compact`: the surface alone, without the web wrapper, the legend or the tooltip (also used for a brush's panorama) | GAPS-33, GAPS-17 |
| reverseStackOrder | done | `CartesianChart.ReverseStackOrder` | STACK-06 |
| defaultShowTooltip | n/a | inert in Recharts 2.15.4: it only sets the initial `isTooltipActive`, and with no active payload `TooltipBoundingBox` stays hidden; the tooltip shows at mount through `Tooltip.defaultIndex` (done, TOOLTIP-09) with or without it | |
| accessibilityLayer, role, tabIndex | done | `Chart.AccessibilityLayer`: Recharts' `AccessibilityManager` on the desktop (focus, ArrowLeft/ArrowRight), Aioros's roving focus among a horizontal chart's categories on the web (one tab stop, the arrows, Escape, Home and End left alone) and Recharts' focusable surface in the other charts, a scatter chart's keys drawing Recharts' cross; `Chart.Role` and Avalonia's `TabIndex` override the surface's | KEYBOARD-01..12, DETAIL-04, DETAIL-05, DETAIL-10 |
| throttleDelay | n/a | throttles React's mousemove handler; the web uses CSS `:hover`, the desktop draws per pointer event | |
| dataKey (chart level) | n/a | unused by Recharts' cartesian charts | |
| className, style | done | the chart's Avalonia `Classes` after `recharts-wrapper`, `Chart.WrapperStyle` last in the wrapper's style; elements' `Classes` where Recharts writes their `className` | DETAIL-07 |
| `onClick` (the chart's, with the active category) | done | `ClickCommand` on the chart, its parameter the category's index in the data: a button per category zone on the web, the pointer on the desktop | INTERACT-03, INTERACT-06 |
| other event handlers (`onMouseEnter/Move/Leave/Down/Up`, `onContextMenu`, `onDoubleClick`) | n/a | Recharts' hover handlers as commands would be a request per pointer movement on the web; the active shape, tooltip and cursor they serve are drawn without one (decisions "Clicks as commands") | |
| (Pictor extra) ResponsiveWidths | — | `A/Charts/Chart.cs`; layouts outside their range hidden without `display:none` | WEB-05, AUDIT-46 |
| (Pictor extra) ReduceMotion | — | attached, `A/Charts/Chart.cs` | ANIMATION-07 |

### 2.2 Polar charts (PieChart, RadarChart, RadialBarChart)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| cx, cy, innerRadius, outerRadius | done | `PolarChart` (`A/Charts/PolarCharts.cs`) | POLAR-06, POLAR-08 |
| startAngle, endAngle (on the chart) | done | `PolarChart.StartAngle`/`EndAngle` | GAPS-07, GAPS-17 |
| barSize (RadialBarChart) | done | `RadialBarChart.BarSize` | POLAR-08, POLAR-14 |
| barGap, barCategoryGap (RadialBarChart) | done | `RadialBarChart.BarGap`/`BarCategoryGap` (`A/Charts/PolarCharts.cs`) | GAPS-07, GAPS-17 |
| maxBarSize (RadialBarChart) | done | `RadialBarChart.MaxBarSize`, `RadialBar.MaxBarSize` | POLAR-13, POLAR-19 |
| layout (`centric`/`radial`), stackOffset | done | `RadialBarChart.Layout` (centric: categories around the angle axis, values along the radius), `RadialBarChart.StackOffset` | GAPS-26, GAPS-27, GAPS-17 |
| data, margin, width, height, children (title: see §2.1) | done | `Chart` base | POLAR-03, POLAR-06, WEB-02 |
| desc | done | `Chart.Desc`, the SVG's `<desc>` (title: see §2.1) | GAPS-09 |
| syncId (RadarChart, RadialBarChart) | done | as in §2.1 | SYNC-01, SYNC-03 |
| events: `onClick` | done | the chart's `ClickCommand` (a hover state's index) and each item's (`ItemClickCommand`, a series' own) | INTERACT-04 |
| accessibilityLayer, other events, className | done | accessibilityLayer as in §2.1 (Recharts' manager does nothing in polar charts); `Classes` as `className` on the wrapper and the pie; the hover handlers n/a as every chart's | KEYBOARD-06, KEYBOARD-10, DETAIL-07 |

### 2.3 Axes

#### XAxis / YAxis (`A/Elements/Axes.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| xAxisId / yAxisId | done | `XAxis.XAxisId`, `YAxis.YAxisId` | CHARTS-04, TICKS-04 |
| type | done | `AxisType` Category/Number (`P/Specs/ElementSpecs.cs`) | CHARTS-16, SCATTER-01 |
| dataKey | done | | CHARTS-01 |
| hide | done | `Axis.Hide`: the axis draws nothing and takes no room | GAPS-05, GAPS-17 |
| orientation | done | `AxisOrientation` | CHARTS-04, TICKS-06 |
| width (YAxis), height (XAxis) | done | `YAxis.Width`, `XAxis.Height` | TICKS-04, TICKS-09 |
| width (XAxis), height (YAxis) | n/a | ignored by Recharts' layout | |
| mirror | done | | TICKS-06 |
| reversed | done | | TICKS-06 |
| tickCount | done | | TICKS-06 |
| allowDecimals | done | | TICKS-06 |
| domain | done | two bounds as text: number, `auto`, `dataMin`, `dataMax`, `dataMin - n`, `dataMax + n`, or an explicit category list (`P/Layout/AxisDomains.cs`); function bounds n/a | AXES-05, TICKS-07 |
| allowDataOverflow | done | the domain cuts the data and the series are clipped on both targets | CLIP-01, CLIP-02, CLIP-04 |
| allowDuplicatedCategory | done | `Axis.AllowDuplicatedCategory` | AXES-04 |
| includeHidden | done | `Axis.IncludeHidden` | AXES-05 |
| padding `{left,right}` / `{top,bottom}` | done | `Axis.Padding` Thickness | TICKS-06 |
| padding `'gap'` / `'no-gap'` | done | `Axis.PaddingKind` | AXES-06 |
| scale | done | `auto`, `linear`, `band`, `point`, `log`, `pow`, `sqrt`, `time`, `utc` (`P/Layout/AxisDomains.cs`), `identity`, `quantile`, `quantize`, `sequential`, `threshold` (`P/Layout/ValueScale.cs`); `ordinal` n/a (§3); function n/a | CHARTS-15, AXES-08, AXES-09, GAPS-23, GAPS-24 |
| ticks (explicit list) | done | `Axis.Ticks` | TICKS-07 |
| interval | done | number, `preserveEnd`, `preserveStart`, `preserveStartEnd`, `equidistantPreserveStart` | TICKS-06, TICKS-08 |
| minTickGap | done | | TICKS-06 |
| tickSize, tickMargin | done | | TICKS-05 |
| tickLine, axisLine | done | booleans and their props as `TickLineStroke`/`StrokeWidth`/`StrokeDashArray`, `AxisLineStroke`/`StrokeWidth`/`StrokeDashArray` | TICKS-05 |
| tick | done | boolean and SVG text props as `TickFill`, `TickFontSize`; the element form as `TickTemplate` (a `TickContext`) | TICKS-05, CUSTOM-05 |
| tickFormatter | done | `TickFormat` .NET composite format, or `TickFormatter`, a converter of the tick's value with its index as the parameter (a function through `FuncValueConverter`) | AXES-09, AXES-12 |
| angle (XAxis tick rotation) | done | `Angle`, `TextAnchor` | TICKS-04 |
| unit | done | | SCATTER-01, TICKS-06 |
| name | done | used in scatter tooltips | SCATTER-06 |
| label | done | `Label` text with `LabelPosition`, `LabelAngle`, `LabelOffset`, `LabelFill`, `LabelFontSize` | AXES-01, AXES-03 |
| stroke | done | `Axis.Stroke`: the axis line, the tick lines and the tick texts' default fill | GAPS-05, GAPS-17 |
| SVG presentation attributes | done | `Presentation` attached properties (`opacity`, `strokeOpacity`, `strokeLinecap`, `strokeLinejoin`, `strokeMiterlimit`, `fontWeight`, `fontStyle`) on the axis line, tick lines and tick texts | DETAIL-06 |
| event handlers | done | `ClickCommand` on `XAxis`/`YAxis`: Recharts' tick `onClick` as a command with the tick's value (its payload's), a button per visible tick clipped to its tick line and label on the web, the pointer on the desktop; the hover handlers n/a, as every chart's | INTERACT-09 |

#### ZAxis (`A/Elements/CartesianExtras.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| zAxisId, dataKey, range, name, unit | done | `A/Elements/CartesianExtras.cs` | SCATTER-02, SCATTER-03, SCATTER-06, AXES-10 |
| type | done | category z: the band of its text, the text in the tooltip (Recharts' `value \|\| '-'`) | AXES-10, GAPS-25 |
| scale | done | | AXES-10 |
| domain | done | | AXES-10 |

#### PolarAngleAxis (`A/Elements/PolarElements.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| dataKey, axisLine, tickLine, tick, tickSize, orientation, stroke | done | `PolarAngleAxis` (booleans with `TickLineStroke`/`StrokeWidth`, `AxisLineStroke`/`StrokeWidth`, `TickFill`, `TickFontSize`) | POLAR-06, POLARAXIS-01, POLARAXIS-03 |
| axisLineType, hide | done | `PolarAngleAxis.AxisLineType`, `Hide` (inert: Recharts 2.15.4 draws a hidden polar axis) | GAPS-11, GAPS-13, GAPS-17 |
| tickFormatter | done | `TickFormat` composite format, or a `TickFormatter` converter | GAPS-11, GAPS-14, AXES-12 |
| type | done | `PolarAngleAxis.Type` | POLARAXIS-03 |
| angleAxisId | done | `PolarAngleAxis.AngleAxisId`: several angle axes per chart, each numeric one over its series' values (polar `formatAxisMap`) | POLARAXIS-04, GAPS-17 |
| domain, tickCount, allowDecimals, reversed | done | | POLARAXIS-02, POLARAXIS-03 |
| allowDataOverflow, scale | done | `PolarAngleAxis.AllowDataOverflow`, `Scale`; without `TickCount` a numeric angle axis shows d3's ten ticks, as Recharts' (no default tick count) | GAPS-14, GAPS-17 |
| ticks, allowDuplicatedCategory | done | | POLARAXIS-02 |
| tick / axisLine / tickLine objects | done | flat properties; the element form as `TickTemplate` (a `TickContext` with the chart's center), on both polar axes | POLARAXIS-01, POLARAXIS-03, CUSTOM-21 |
| cx, cy, radius | n/a | filled by the chart | |

#### PolarRadiusAxis (`A/Elements/PolarElements.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| angle, dataKey, type, orientation, tick, tickCount, domain | done | `PolarRadiusAxis` | POLAR-07, POLARAXIS-01..03 |
| stroke, axisLine, allowDecimals, allowDataOverflow, hide | done | `PolarRadiusAxis` properties (`Hide` inert as Recharts') | GAPS-11, GAPS-13, GAPS-14, GAPS-17 |
| tickFormatter | done | `TickFormat` composite format, or a `TickFormatter` converter | GAPS-11, GAPS-14, AXES-12 |
| label | done | every position, with the label props | POLARAXIS-01..03 |
| radiusAxisId | done | `PolarRadiusAxis.RadiusAxisId`: several radius axes per chart, each numeric one over its series' values | POLARAXIS-04, GAPS-17 |
| reversed, scale, ticks, allowDuplicatedCategory | done | | POLARAXIS-01..03 |
| tickLine | n/a | Recharts 2.15.4's `PolarRadiusAxis` draws no tick lines | |
| cx, cy | n/a | filled by the chart | |

### 2.4 Cartesian series

#### Line and Area (`Series` + `PointSeries`, `A/Elements/Series.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| dataKey, name, xAxisId, yAxisId, hide, legendType | done | `Series` | CHARTS-01, CHARTS-04, TICKS-04, LEGEND-04, LEGEND-09, AXES-05 |
| unit | done | `Series.Unit` (tooltip values) | GAPS-02, GAPS-17 |
| type (curve) | done | all 15 named curves, `bump` in a vertical chart is bumpY (`P/Painting/Shapes.cs`); `CurveFactory` function n/a | AXES-07 |
| stroke, strokeWidth, strokeDasharray, fill, fillOpacity | done | `StrokeDashArray` a text or a list of numbers (as Avalonia's `Shape.StrokeDashArray`) | CHARTS-02, CHARTS-03, CHARTS-07, BINDING-12 |
| connectNulls | done | `PointSeries.ConnectNulls` (radar: §2.7) | GAPS-01, GAPS-17 |
| stackId (Area) | done | `PointSeries.StackId`; also accepted on `Line` (Recharts ignores it there) | CHARTS-08 |
| dot | done | `Dot`, `DotRadius`, `DotFill`, `DotStroke`, `DotStrokeWidth`, `DotFillOpacity`, `DotStrokeDashArray`, `ClipDot`; the element form as `DotTemplate` (a `DotContext`) | SHAPES-01, SHAPES-02, SHAPES-08, CLIP-01, CUSTOM-03 |
| activeDot | done | radius (0 hides), `ActiveDotFill`, `ActiveDotStroke`, `ActiveDotStrokeWidth` | CHARTS-22, SHAPES-01 |
| label | done | `Label` position, or a label object as a first `LabelList` | CHARTS-06, LABELS-01 |
| LabelList child | done | several per series | CHARTS-06, LABELS-01 |
| ErrorBar child | done | | CHARTS-19 |
| data (series' own) | done | `Series.Data`, spec table | AXES-04, RANGE-12 |
| baseValue (Area) | done | `Area.BaseValue`, or the chart's | RANGE-04, RANGE-09 |
| isRange / range values `[low, high]` (Area) | done | a member holding a pair, both edges stroked, `low ~ high` tooltips | RANGE-01, RANGE-02, RANGE-07..10 |
| tooltipType (`'none'`) | done | `Series.TooltipType` | TOOLTIP-15 |
| isAnimationActive, animationBegin, animationDuration, animationEasing | done | `A/Elements/Animated.cs`, `P/Specs/AnimationSpec.cs` | ANIMATION-02, ANIMATION-04, ANIMATION-06 |
| animateNewValues | done | `Line.AnimateNewValues` (on by default): a line's points with no previous one come from twice the plot's width and half its height; `Area` declares it and never reads it (n/a there) | UPDATE-01, DETAIL-09 |
| onAnimationStart, onAnimationEnd | done | `AnimationStarted` and `AnimationEnded` events on the desktop; a scriptless web chart raises nothing | DETAIL-08 |
| SVG presentation attributes | done | `Presentation` attached properties on the curves, areas and dots | DETAIL-06 |
| `onClick` (a dot's, Recharts' `activeDot`/`dot` `onClick`) | done | `ClickCommand`: a round button of the active dot's radius plus 2 over each point, its index as the parameter (the `activeTooltipIndex` Recharts' chart handler receives in the same click, its dot handler receiving the dot's own props) | INTERACT-03, MATRIX-01, MATRIX-02 |
| other event handlers | n/a | Recharts' hover handlers as commands would be a request per pointer movement on the web; the active shape, tooltip and cursor they serve are drawn without one (decisions "Clicks as commands") | |

#### Bar (`A/Elements/Series.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| dataKey, legendType, fill, stroke | done | `Series` base | CHARTS-09, LEGEND-04 |
| name, unit, xAxisId, yAxisId, hide, strokeWidth, fillOpacity | done | `Series` base properties; bars of several value axes share their category axis' band (Recharts' `getBarSizeList`) | GAPS-03, GAPS-04, GAPS-17 |
| stackId | done | `Bar.StackId` | CHARTS-10, CHARTS-11 |
| barSize | done | a number or a percent text | CHARTS-15, STACK-07 |
| maxBarSize | done | `Bar.MaxBarSize`, over the chart's; each bar caps the slots at its own, as Recharts lays the band out per bar | GAPS-03, GAPS-17 |
| radius | done | `Radius`: a text (one value or four), a number, a list of numbers or an Avalonia `CornerRadius` (Recharts' `[topLeft, topRight, bottomRight, bottomLeft]`) | GAPS-03, GAPS-17, BINDING-11 |
| background | done | `Background` fill with `BackgroundStroke`, `BackgroundStrokeWidth`, `BackgroundFillOpacity`, `BackgroundRadius` (none of the bar's own props, as Recharts) | CHARTS-14, GAPS-31, GAPS-17 |
| minPointSize | done | `Bar.MinPointSize` number; the function form n/a (function props) | GAPS-03, GAPS-17 |
| activeBar | done | `ActiveFill`, `ActiveStroke`, `ActiveStrokeWidth`; the element form as `ActiveShapeTemplate` | CHARTS-21, CUSTOM-15 |
| shape | done | `Bar.ShapeTemplate` (a `BarShapeContext`); a template the web cannot write is refused with its reason and drawn as Avalonia lays it out | CUSTOM-01, CUSTOM-02, CUSTOM-04 |
| activeIndex | done | `Bar.ActiveIndex`: the bar in Recharts' active layer with its background, its `ActiveShapeTemplate`, its active props (its size cut to whole pixels, as Recharts' `parseInt`) or its own shape; the pointer leaves it (in a `shared={false}` chart too, where Recharts 2.15.4 takes the hovered element's place among the chart's children as the index and repaints every bar series with it, a bug not ported:) | SHAPES-09 |
| label, LabelList, Cell, ErrorBar | done | several label lists; `Cell` with every SVG prop | CHARTS-13, CHARTS-19, LABELS-01, SHAPES-03 |
| range values `[low, high]` (floating bars) | done | | RANGE-03, RANGE-07 |
| tooltipType | done | `Series.TooltipType` | TOOLTIP-15 |
| animation props | done | bar grows from its base (rounded corners scale with it, decisions "Animations") | ANIMATION-02, ANIMATION-05 |
| onAnimationStart/End | done | desktop events, as §2.4's lines | DETAIL-08 |
| `onClick` | done | `ClickCommand`: a button over each bar shown with its index in the whole data (under a brush only the window's bars have one), over its whole band when it draws a background (Recharts' background rectangles carry the bar's handlers) | INTERACT-01, INTERACT-02, INTERACT-06, MATRIX-02, MATRIX-05 |
| other event handlers | n/a | Recharts' hover handlers as commands would be a request per pointer movement on the web; the active shape, tooltip and cursor they serve are drawn without one (decisions "Clicks as commands") | |

#### Scatter (`A/Elements/Series.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| data, zAxisId, name, fill | done | `Scatter` + base | SCATTER-01, AXES-10 |
| xAxisId, yAxisId, legendType, hide | done | `Series` base properties | GAPS-06, GAPS-17 |
| shape (symbol name) | done | `ScatterShape`: circle, cross, diamond, square, star, triangle, wye (`P/Specs/CartesianSpecs.cs`) | SCATTER-02, SCATTER-03, AXES-10 |
| shape (element / function) | done | `Scatter.ShapeTemplate` (a `ScatterShapeContext`); the function form n/a | CUSTOM-07 |
| line | done | `Line` with `LineStroke`, `LineStrokeWidth`, `LineStrokeDashArray` | SCATTER-03, SCATTER-10, SCATTER-12 |
| lineType `joint` | done | | SCATTER-03 |
| lineType `fitting` | done | `Scatter.LineType`, Recharts' `getLinearRegression` | SCATTER-10, SCATTER-11, SCATTER-12 |
| lineJointType | done | | SCATTER-10 |
| activeShape, activeIndex | done | `ActiveShapeTemplate` on the hovered point; `Scatter.ActiveIndex` drawn active in place (the template or the default symbol in the active layer), kept under the pointer | CUSTOM-07, SHAPES-10 |
| label, LabelList | done | | SCATTER-04 |
| ErrorBar child | done | | SCATTER-05 |
| tooltipType | done | `Series.TooltipType` | GAPS-21, GAPS-17 |
| animation props | done | each symbol grows by area as Recharts interpolates `size`: on the web through a registered number property, on the desktop frame by frame | ANIMATION-02, ANIMATION-21 |
| `onClick` | done | `ClickCommand`: a button clipped to each symbol's path, with Recharts' `onClick` index | INTERACT-04, MATRIX-03 |
| other event handlers | n/a | Recharts' hover handlers as commands would be a request per pointer movement on the web; the active shape, tooltip and cursor they serve are drawn without one (decisions "Clicks as commands") | |

#### ErrorBar (`A/Elements/CartesianExtras.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| dataKey | done | a value or a `[low, high]` pair | CHARTS-19, RANGE-05 |
| width, direction, stroke, strokeWidth | done | `ErrorBar` | CHARTS-19, SCATTER-05 |
| on stacked bars | done | the whisker stands on the stack's top | RANGE-11 |
| SVG presentation attributes | done | `Presentation` attached properties on each error bar's group (its opacity) and lines | DETAIL-06 |

### 2.5 Cartesian annotations

#### CartesianGrid (`A/Elements/Parts.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| horizontal, vertical | done | booleans (an object is `true` in Recharts) | SHAPES-04, SHAPES-05 |
| stroke, strokeDasharray, fill | done | | CHARTS-01, CHARTS-15, SHAPES-04 |
| horizontalPoints, verticalPoints, horizontalCoordinatesGenerator, verticalCoordinatesGenerator | done | explicit points; the generators are functions (n/a) | SHAPES-05 |
| horizontalFill, verticalFill (stripes) | done | | SHAPES-04 |
| syncWithTicks, horizontalValues, verticalValues | done | `SyncWithTicks`, `HorizontalValues`, `VerticalValues` (values kept by the axis' tick rules, Recharts' `getTicks`) | SHAPES-05, GAPS-19, GAPS-17 |
| x, y, width, height | done | `X`, `Y`, `GridWidth`, `GridHeight` | SHAPES-08 |
| fillOpacity, strokeWidth, strokeOpacity | done | | SHAPES-04, SHAPES-05 |
| other SVG presentation attributes | done | `Presentation` attached properties on the grid's lines | DETAIL-06 |

#### References: ReferenceLine (`A/Elements/Parts.cs`), ReferenceArea, ReferenceDot (`A/Elements/CartesianExtras.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| xAxisId, yAxisId (ReferenceLine, ReferenceArea, ReferenceDot) | done | properties on the three references | GAPS-03, GAPS-17 |
| ReferenceLine: x, y, stroke, strokeWidth, strokeDasharray | done | `ReferenceLine` | CHARTS-05, CHARTS-12, REFERENCE-01 |
| ReferenceLine: label | done | text with the label props | CHARTS-05, AXES-01 |
| ReferenceLine: ifOverflow | done | `discard`, `visible`, `hidden`, `extendDomain` | REFERENCE-01, REFERENCE-02, REFERENCE-03 |
| ReferenceLine: segment | done | `X1`, `Y1`, `X2`, `Y2` | REFERENCE-02, REFERENCE-04 |
| ReferenceLine: position (`start`/`middle`/`end` on a band) | done | `Position` | REFERENCE-02, REFERENCE-04 |
| ReferenceLine, ReferenceArea, ReferenceDot: isFront | n/a | Recharts 2.15.4 declares it in `defaultProps` and never reads it | |
| ReferenceLine: alwaysShow | n/a | deprecated alias of `ifOverflow="extendDomain"` | |
| ReferenceLine: shape | done | `ReferenceLine.ShapeTemplate` (a `ReferenceLineContext`) | CUSTOM-11 |
| ReferenceArea: x1, x2, y1, y2, fill, fillOpacity, stroke, strokeOpacity, strokeWidth | done | `ReferenceArea` | CHARTS-20, SCATTER-05, REFERENCE-01 |
| ReferenceArea: label | done | text with the label props | CHARTS-20, AXES-02 |
| ReferenceArea: ifOverflow | done | | REFERENCE-01, REFERENCE-03 |
| ReferenceArea: radius (`r`) | done | `ReferenceArea.Radius` | REFERENCE-01, REFERENCE-03 |
| ReferenceArea: shape | done | `ReferenceArea.ShapeTemplate` (a `ReferenceAreaContext`) | CUSTOM-11 |
| ReferenceDot: x, y, r, fill, fillOpacity, stroke, strokeWidth | done | `ReferenceDot` | CHARTS-20 |
| ReferenceDot: label | done | text with the label props | CHARTS-20, AXES-02 |
| ReferenceDot: ifOverflow | done | | REFERENCE-01, REFERENCE-03 |
| ReferenceDot: shape | done | `ReferenceDot.ShapeTemplate` (a `ReferenceDotContext`) | CUSTOM-11 |

#### Brush (`A/Elements/Brush.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| dataKey, height, stroke, fill, travellerWidth, gap | done | `Brush` | BRUSH-01, BRUSH-07 |
| startIndex, endIndex | done | two-way bindable, dragged on the desktop (`A/Charts/BrushDrag.cs`); on the web the travellers are two range fields and the slide one composite field moving both indexes (Aioros's `SlotPart.Composite`, decoded by `ChartRenderer.Decode`) | BRUSH-02, BRUSH-03, BRUSH-04, BRUSH-13, BRUSH-15 |
| onChange, onDragEnd | done (by decision) | replaced by the two-way `StartIndex`/`EndIndex` bindings, followed on both targets (the web's range inputs post them): XAML's way of hearing a change | BRUSH-04, BRUSH-12, BRUSH-13 |
| tickFormatter | done | `Brush.TickFormat` | BRUSH-06, BRUSH-09, BRUSH-10 |
| children (panorama) | done | a `CartesianChart` as the brush's content, laid out compact | BRUSH-06..09 |
| traveller (custom element) | done | `Brush.TravellerTemplate` (a `TravellerContext`) | CUSTOM-11 |
| alwaysShowText | done | | BRUSH-06, BRUSH-07, BRUSH-09, BRUSH-10 |
| padding, x, y, width | done | | BRUSH-07, BRUSH-09 |
| ariaLabel | done | `Brush.AriaLabel`: Recharts' `role="slider"`, `aria-label` and `aria-valuenow` on the drawn travellers; on a web brush that moves, the range inputs are the sliders and carry the label; the desktop's automation peer belongs to the Automation row | GAPS-29, GAPS-17 |
| leaveTimeOut | done | the end texts hide after it on the desktop, through the chart's `TimeProvider`; the web brush shows no texts on hover | BRUSH-11 |

### 2.6 Components

#### Legend (`A/Elements/Legend.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| layout, align, verticalAlign, iconSize | done | `Legend` | LEGEND-08, LEGEND-09, POLAR-08 |
| iconType | done | all eleven `LegendType`s | LEGEND-04 |
| inactiveColor | done | `Legend.InactiveColor`; the dark theme's default `#5c5c64` | LEGEND-05, AUDIT-40 |
| wrapperStyle | done | CSS declarations, `LineHeight` | LEGEND-08, BRUSH-01 |
| payload (custom entries) | done | `LegendPayload` children | LEGEND-06 |
| formatter | done (by decision) | `Format` composite format | LEGEND-07 |
| content | done | `Legend.ContentTemplate`: a template per entry (a `LegendEntryContext`), written as the items of the list Recharts' wrapper holds and measured for the plot's size; `Legend.Template`: a template for the whole legend (a `LegendContext`) holding a `LegendItems` where the entries go (Recharts' default list, or the entries' templates) | CUSTOM-14, CUSTOM-26 |
| width | done | `Legend.Width` | LEGEND-09 |
| height | done | `Legend.Height`: the wrapper's height, by which the plot shrinks | GAPS-01, GAPS-17 |
| margin | n/a | Recharts overwrites it with the chart's margin in `renderLegend` | |
| payloadUniqBy | done | | LEGEND-10 |
| onClick | done | `ClickCommand` on the `Legend`, with the item's index (it takes the click instead of `Toggles`); `Toggles` (Pictor feature) covers click-to-hide | LEGEND-01..03, LEGEND-11, INTERACT-05 |
| onMouseEnter, onMouseLeave | n/a | Recharts' hover handlers as commands would be a request per pointer movement on the web; the active shape, tooltip and cursor they serve are drawn without one (decisions "Clicks as commands") | |
| onBBoxUpdate | n/a | DOM measurement callback | |

#### Tooltip (`A/Elements/Tooltip.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| separator | done | | TOOLTIP-05 |
| cursor | done | `Cursor` and `CursorStroke`, `CursorFill`, `CursorStrokeWidth`, `CursorFillOpacity`, `CursorStrokeOpacity`, `CursorStrokeDashArray` | SCATTER-06, TOOLTIP-07, TOOLTIP-08 |
| formatter | done | `Format` composite format on values, or `Formatter`, a converter of the value with the series' name as the parameter; name rewriting n/a | TOOLTIP-05, TOOLTIP-26 |
| labelFormatter | done | `LabelFormat`, or a `LabelFormatter` converter | TOOLTIP-05, TOOLTIP-26 |
| content | done | `Tooltip.ContentTemplate` (a `TooltipContentContext`: the label, the entries and the active item's payload; its `TooltipItems` writes `Tooltip.ItemTemplate`, a `TooltipItemContext`, once per line, TOOLTIP-25) written as HTML in the tooltip's box on every tooltip: cartesian axis, bar, scatter, pie, radar, radial bar, funnel, treemap, Sankey and sunburst | CUSTOM-13, CUSTOM-25 |
| contentStyle, itemStyle, labelStyle, wrapperStyle, wrapperClassName, labelClassName | done | CSS declarations written inline on the web, a subset drawn on the desktop | TOOLTIP-06, TOOLTIP-17 |
| trigger `hover` | done | CSS `:hover` on the web, pointer on the desktop | CHARTS-23, TOOLTIP-02, DESKTOP-02 |
| trigger `click` | done | focusable hit groups on the web | TOOLTIP-11, TOOLTIP-16 |
| shared (`true`) | done | cartesian category tooltips: every series at the category, web and desktop | CHARTS-23, CHARTS-24, DESKTOP-02 |
| shared `false` | done | item tooltips in bar and radial bar charts, the axis tooltip kept elsewhere | TOOLTIP-01, TOOLTIP-02, POLAR-15 |
| active, defaultIndex | done | | TOOLTIP-09, TOOLTIP-10, TOOLTIP-16 |
| position, offset, coordinate | done | `PositionX`/`PositionY`, `Offset`; `coordinate` is internal (n/a) | TOOLTIP-12, TOOLTIP-13 |
| allowEscapeViewBox, reverseDirection | done | per axis | TOOLTIP-13, TOOLTIP-14 |
| filterNull | done | | TOOLTIP-03, TOOLTIP-04 |
| includeHidden | done | | TOOLTIP-15 |
| itemSorter | done | Recharts' key forms (`dataKey`, `value`, `name`); the function form n/a | TOOLTIP-05 |
| payloadUniqBy | done | | TOOLTIP-15 |
| isAnimationActive, animationDuration, animationEasing (box movement) | n/a | each web hover state is its own box; there is no movement between them to animate | |
| useTranslate3d | n/a | CSS transform choice of React's positioning | |
| accessibilityLayer | done | `role="status" aria-live="assertive"` on the box | TOOLTIP-17 |

#### ResponsiveContainer

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| width, height (percent) | done | a chart without `Width`/`Height` fills its parent; web: view box scaling or `ResponsiveWidths` | WEB-05, RESPONSIVE-01 |
| exact re-layout at the client's width (web) | n/a | `ResponsiveWidths` is the scriptless approach, layouts at the widths given shown by container queries | |
| aspect | done | `Chart.Aspect`; CSS `aspect-ratio` on the web | RESPONSIVE-01 |
| minWidth, minHeight, maxHeight | done | Avalonia's `MinWidth`/`MinHeight`/`MaxHeight` on the chart control, written on the web wrapper | RESPONSIVE-02 |
| initialDimension | n/a | client-side first paint before measuring | |
| debounce | n/a | the desktop re-lays out synchronously per layout pass; the web is written once | |
| onResize | n/a | Avalonia's `SizeChanged` on the chart control | |
| id, className, style | done | `Chart.ContainerId`, `ContainerClass`, `ContainerStyle` on a responsive chart's web container (the style before the computed sizes) | GAPS-30 |

#### Cell (`A/Elements/Parts.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| fill, stroke | done | | CHARTS-13, POLAR-04, SHAPES-03 |
| other SVG props (strokeWidth, fillOpacity, opacity, strokeDasharray) | done | `Cell.StrokeWidth`, `FillOpacity`, `Opacity`, `StrokeDashArray` | SHAPES-03, SHAPES-08 |

#### Label (as props of axes, references, polar radius axis, series `label`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| value | done | `Label` string properties | CHARTS-05, AXES-01 |
| position | done | every Recharts value, `centerTop` and `centerBottom` included; `{x, y}` as `PositionX`/`PositionY` on label lists and `Label` elements | LABELS-02, POLARAXIS-03, GAPS-28 |
| offset | done | `LabelOffset` | AXES-01, AXES-02 |
| angle, fontSize, fill | done | `LabelAngle`, `LabelFontSize`, `LabelFill` on the axes, references and the polar radius axis | AXES-01..03 |
| formatter | done (by decision) | `Format` on `LabelList` and `Label` elements | GAPS-03, GAPS-28 |
| children, textBreakAll | done | a `Label` element's content is its value; `textBreakAll` on label lists | LABELS-05, GAPS-28 |
| content | done | `Label.ContentTemplate` (a `LabelContext`: the element's box as Recharts' `viewBox`, the value, the text) on the labels of cartesian axes, references, series and the polar radius axis (its polar `viewBox`) | CUSTOM-18, CUSTOM-19, CUSTOM-22 |

#### LabelList (`A/Elements/Parts.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| dataKey, position, offset, fill | done | | CHARTS-06, SCATTER-04, FUNNEL-01, FUNNEL-02 |
| formatter | done (by decision) | `Format` composite format | GAPS-03, GAPS-17 |
| valueAccessor | n/a | a function; `DataKey` covers it | |
| angle, clockWise, textBreakAll | done | | LABELS-01, LABELS-04, LABELS-05 |
| content | done | `LabelList.ContentTemplate` on the label lists of lines, areas, bars, pies, radial bars, radars and funnels (a polar label's point at its sector's middle, a funnel's label box) | CUSTOM-05, CUSTOM-18, CUSTOM-20 |
| SVG text props (fontSize, fontWeight, stroke) | done | `FontSize`, `FontWeight`, `Stroke`, `StrokeWidth` | LABELS-01 |
| several LabelLists per series | done | | LABELS-01, LABELS-06 |
| on stacked bars and areas | done | a label list without a data key shows the stack's top, as Recharts' `last(value)` | GAPS-18 |
| on Pie, RadialBar | done | | LABELS-03, LABELS-04, LABELS-07 |

#### Text, Customized

See §1: `Text` n/a, `Customized` done (CUSTOM-12).

### 2.7 Polar series

#### Pie (`A/Elements/PolarElements.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| dataKey, data, cx, cy, innerRadius, outerRadius, startAngle, endAngle, paddingAngle, minAngle, fill | done | `Pie` | POLAR-01..04, POLAR-18 |
| nameKey, cornerRadius, stroke, legendType, hide | done | `Pie` properties; a hidden pie's sectors stay in the legend as Recharts lists them | GAPS-09, GAPS-10, GAPS-17 |
| label | done | `Label` with `LabelOffsetRadius`, `LabelFill`, `LabelFontSize`, `LabelFontWeight`, `LabelStroke` | POLAR-01, POLAR-17, POLAR-19 |
| labelLine | done | `LabelLine` with `LabelLineStroke`, `LabelLineStrokeWidth`, `LabelLineStrokeDashArray` | POLAR-17 |
| activeIndex | done | `ActiveIndex` or `ActiveIndexes`, kept under the pointer | POLAR-05, POLAR-16, SHAPES-13 |
| activeShape | done | object form via `ActiveShape` (fill, stroke, radii, corner radius, opacities); the element form as `ActiveShapeTemplate` (a `SectorContext`) | POLAR-05, POLAR-16, CUSTOM-06 |
| inactiveShape | done | `InactiveShape` | POLAR-16 |
| blendStroke | done | | POLAR-16 |
| valueKey | n/a | deprecated alias of `dataKey` | |
| maxRadius | n/a | internal | |
| tooltipType | done | `Pie.TooltipType` | GAPS-20, GAPS-17 |
| rootTabIndex | done | `Pie.RootTabIndex` (0 by default): the pie layer's tab index, its sector groups `-1`, as Recharts' | DETAIL-07 |
| Cell children | done | | POLAR-04 |
| animation props | done | every sector grows from 0 as Recharts grows it (arcs on the web, one per run of one color, the edges above them; Recharts' update from nothing on the desktop); the update re-packs the sectors | ANIMATION-03, ANIMATION-09, ANIMATION-12, ANIMATION-17, ANIMATION-18, UPDATE-05 |
| `onClick` | done | `ClickCommand`: a button clipped to each sector | INTERACT-04 |
| other event handlers | n/a | Recharts' hover handlers as commands would be a request per pointer movement on the web; the active shape, tooltip and cursor they serve are drawn without one (decisions "Clicks as commands") | |
| (Pictor extra) FillKey | — | items' own colors | |

#### Radar (`A/Elements/PolarElements.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| dataKey, name, stroke, fill, fillOpacity | done | `Radar` | POLAR-06, POLAR-07 |
| strokeWidth, unit, legendType, hide | done | `Radar` properties; a hidden radar is inactive in the legend | GAPS-11, GAPS-12, GAPS-17 |
| dot | done | `Dot` and the `Dot*` props | SHAPES-07, SHAPES-08 |
| activeDot | done | radius, `ActiveDotFill`, `ActiveDotStroke`, `ActiveDotStrokeWidth` | SHAPES-07 |
| isRange / baseLinePoints | done | range values | RANGE-06, RANGE-07, RANGE-10 |
| connectNulls | done | | SHAPES-07 |
| label | done | `Label` and `LabelList` children | SHAPES-07, SHAPES-08 |
| angleAxisId, radiusAxisId | done | `Radar.AngleAxisId`, `RadiusAxisId`; an id no axis declares gets an implicit axis | POLARAXIS-04, GAPS-17 |
| shape | done | `Radar.ShapeTemplate` (a `RadarShapeContext` with the radar's points) | CUSTOM-08 |
| tooltipType | done | `Radar.TooltipType` | GAPS-20, GAPS-17 |
| animation props | done | points and dots come from the center; the update moves them from the previous points | ANIMATION-03, ANIMATION-10, ANIMATION-13, UPDATE-06 |
| `onClick` | done | `ClickCommand`: a round button over each point (Recharts' `activeDot` `onClick`, the chart's `activeTooltipIndex` as the index) | INTERACT-04, MATRIX-04 |
| other event handlers | n/a | Recharts' hover handlers as commands would be a request per pointer movement on the web; the active shape, tooltip and cursor they serve are drawn without one (decisions "Clicks as commands") | |

#### RadialBar (`A/Elements/PolarElements.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| dataKey, fill, stroke, cornerRadius | done | `RadialBar` | POLAR-08, POLAR-13 |
| name, barSize, minPointSize, unit, legendType, hide | done | `RadialBar` properties; the legend lists every radial bar's items (a hidden one's too) by their data's `name` and `fill`, sorted by name, and the tooltip leaves the unit out, as Recharts 3's | GAPS-07, GAPS-08, GAPS-17, POLAR-20 |
| nameKey | n/a | Recharts 2.15.4's `RadialBar` declares no `nameKey` (its legend reads `entry.name`); Pictor's `NameKey` is an extra for the legend names | |
| background | done | `Background` with `BackgroundStroke`, `BackgroundStrokeWidth`, `BackgroundFillOpacity` | POLAR-08, POLAR-13 |
| label | done | `Label` position with `LabelFill`, and `LabelList` children | POLAR-08, LABELS-04 |
| maxBarSize | done | | POLAR-13, POLAR-19 |
| stackId | done | | POLAR-14, POLAR-19 |
| forceCornerRadius, cornerIsExternal | done | | POLAR-13 |
| shape, activeShape, activeIndex | done | `ShapeTemplate` and `ActiveShapeTemplate` (a `SectorContext`) on the hovered ring; `RadialBar.ActiveIndex` ignored, as Recharts 3 ignores it | CUSTOM-08, CUSTOM-17, SHAPES-12 |
| angleAxisId, radiusAxisId (per bar) | done | `RadialBar.AngleAxisId`, `RadiusAxisId` | POLARAXIS-04, GAPS-17 |
| startAngle, endAngle (per bar) | n/a | Recharts 2.15.4's `RadialBar` never calls the `getDeltaAngle` that reads them, and each sector's own angles override them when spread | |
| tooltipType | done | `RadialBar.TooltipType` | GAPS-20, GAPS-17 |
| `onClick` | done | `ClickCommand`: a button clipped to each sector, or to its background sector when the bar draws one (Recharts' background sectors carry the bar's handlers), with Recharts' `onClick` index | MATRIX-05 |
| animation props | done | sectors sweep from their start angle (arcs on the web); the update moves their angles | ANIMATION-03, ANIMATION-11, ANIMATION-12, UPDATE-07 |
| `minAngle`, `clockWise` | n/a | do nothing in 2.15.4 (decisions "Polar charts") | |

#### PolarGrid (`A/Elements/PolarElements.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| gridType, radialLines, stroke | done | | POLAR-06, SHAPES-06 |
| polarAngles, polarRadius | done | | SHAPES-06 |
| strokeDasharray, fill, other SVG props | done | `StrokeDashArray`, `StrokeWidth`, `StrokeOpacity`, `Fill` (Recharts' fill reaches only the radial lines) | SHAPES-06, SHAPES-08 |
| cx, cy, innerRadius, outerRadius | n/a | filled by the chart | |

### 2.8 Funnel, Treemap, Sankey, Sunburst

#### FunnelChart + Funnel (`A/Charts/FunnelChart.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| FunnelChart: data, margin, width, height, children | done | `Chart` base | FUNNEL-01, FUNNEL-04 |
| FunnelChart: events (`onClick` on the funnel) | done | `ClickCommand` on the `Funnel`: a button clipped to each trapezoid | INTERACT-04 |
| FunnelChart: syncId, accessibility | done | `AccessibilityLayer` (a focusable surface; Recharts' manager moves nothing in a funnel chart); `syncId` n/a: Recharts' FunnelChart never emits a sync event (its item tooltip registers no wrapper handler and it has no brush) and one received reads tooltip ticks it does not have | KEYBOARD-06, KEYBOARD-10 |
| Funnel: dataKey, data, reversed | done | `Funnel` | FUNNEL-01, FUNNEL-02, FUNNEL-04 |
| Funnel: nameKey, hide, legendType, lastShapeType, fill, stroke | done | `Funnel` properties | GAPS-15, GAPS-16, GAPS-17 |
| Funnel: animation props | done | | ANIMATION-03 |
| Funnel: LabelList children | done | | FUNNEL-01, FUNNEL-02, LABELS-06 |
| Funnel: Cell children | done | read by the funnel over the items' own colors | GAPS-15, GAPS-17 |
| Funnel: shape, activeShape, activeIndex | done | `ShapeTemplate` and `ActiveShapeTemplate` (a `TrapezoidContext`); `Funnel.ActiveIndex` or `ActiveIndexes` drawn active in place, kept under the pointer | CUSTOM-08, CUSTOM-17, SHAPES-11 |
| Funnel: tooltipType | done | `Funnel.TooltipType` | GAPS-20, GAPS-17 |
| Funnel: animateNewValues, events | done | `animateNewValues` n/a (Recharts' `Funnel` declares it and never reads it); `onClick` as `ClickCommand`, `onAnimationStart/End` as desktop events, the hover handlers n/a as every chart's | INTERACT-04, DETAIL-08 |
| (Pictor extras) FillKey, StrokeKey, Width | — | | |

#### Treemap (`A/Charts/Treemap.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| width, height, data, dataKey, nameKey, aspectRatio, type, fill, stroke, colorPanel | done | `Treemap` | TREEMAP-01..05 |
| isAnimationActive, animationBegin, animationDuration, animationEasing | done | golden-ratio offsets instead of random (decisions) | ANIMATION-03 |
| type `nest`: zoom on click | done | `Zoom` (a node's path, bindable both ways) and `ZoomCommand`: the node's children laid out as the root and Recharts' nest index, natively on the desktop and on the web through a choice field of the nodes when `Zoom` is bound (planned, no command), through buttons and a hidden zoom field otherwise; a responsive nest treemap writes its index too | INTERACT-07, INTERACT-08, AUDIT-36, TREEMAP-08 |
| content (custom node) | done | `Treemap.ContentTemplate` (a `TreemapNodeContext`: the node's box, depth, index, name, value and panel color) | CUSTOM-09 |
| nestIndexContent | done | `Treemap.NestIndexTemplate` (a `NestIndexContext`), written as HTML in each trail box | CUSTOM-16 |
| isUpdateAnimationActive | done | `Treemap.IsUpdateAnimationActive` (true by default), negated onto each node's rectangle as Recharts passes it: off, each default rectangle draws its stroke in (1500 ms `ease`, 64 ms after it mounts, as recorded) and follows its node one animation frame late on an update; Recharts' sub-pixel tail after the update (react-smooth's stopped manager replaying its last targets) not ported | ANIMATION-19, UPDATE-26 |
| onClick | done | `ClickCommand` (or `ItemClickCommand`) with the leaf's path | INTERACT-04 |
| onMouseEnter, onMouseLeave | n/a | Recharts' hover handlers as commands would be a request per pointer movement on the web; the active shape, tooltip and cursor they serve are drawn without one (decisions "Clicks as commands") | |
| onAnimationStart/End | done | desktop events on the `Treemap` | DETAIL-08 |
| Tooltip child | done | | TREEMAP-06 |

#### Sankey (`A/Charts/Sankey.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| data, nameKey, nodePadding, nodeWidth, linkCurvature, iterations, sort, margin, width, height | done | `Sankey` | SANKEY-01, SANKEY-02, SANKEY-03, SANKEY-07 |
| dataKey | n/a | Recharts 2.15.4 declares it and never reads it; links' `value` is read | |
| node (props object) | done | `NodeFill`, `NodeFillOpacity`, `NodeStroke`, `NodeStrokeWidth`, `NodeStrokeOpacity`, `NodeStrokeDashArray` | SANKEY-02, SANKEY-07, SANKEY-09 |
| link (props object) | done | `LinkStroke`, `LinkStrokeOpacity`, `LinkStrokeWidth`, `LinkFill`, `LinkStrokeDashArray` | SANKEY-01, SANKEY-07, SANKEY-08, SANKEY-09 |
| node, link (element / function) | done | `Sankey.NodeTemplate` and `LinkTemplate` (a `SankeyNodeContext`, a `SankeyLinkContext`); the function forms n/a | CUSTOM-10 |
| Tooltip child | done | | SANKEY-06 |
| onClick | done | `ItemClickCommand` (or `ClickCommand`) with `link:N` or `node:N` | INTERACT-04 |
| onMouseEnter, onMouseLeave | n/a | Recharts' hover handlers as commands would be a request per pointer movement on the web; the active shape, tooltip and cursor they serve are drawn without one (decisions "Clicks as commands") | |

#### SunburstChart (`A/Charts/SunburstChart.cs`)

| Prop | Status | Where / what is missing | Criteria |
|---|---|---|---|
| data, dataKey, width, height | done | `SunburstChart` | SUNBURST-01, SUNBURST-02, SUNBURST-03 |
| padding (as `SectorPadding`), ringPadding, innerRadius, outerRadius, cx, cy, startAngle, endAngle, fill, stroke | done | `Chart.Padding` stays Recharts' margin | SUNBURST-01, SUNBURST-02, SUNBURST-08 |
| textOptions (as `Text*` properties) | done | | SUNBURST-01, SUNBURST-02, SUNBURST-08 |
| Tooltip child | done | `P/Painting/SunburstPainter.cs` | SUNBURST-06, SUNBURST-07 |
| animation | n/a | Recharts' `SunburstChart` has none | |
| onClick | done | `ItemClickCommand` (or `ClickCommand`) with the node's index | INTERACT-04 |
| onMouseEnter, onMouseLeave | n/a | Recharts' hover handlers as commands would be a request per pointer movement on the web; the active shape, tooltip and cursor they serve are drawn without one (decisions "Clicks as commands") | |

## 3. Behaviors

| Behavior | Status | Where / what is missing | Criteria |
|---|---|---|---|
| Enter animations (every series kind, Recharts' timings and easings) | done | `P/Motion/*`, `P/Svg/SvgMotion.cs`, `A/Drawing/MotionFrame.cs`; no finished animation retained at rest | ANIMATION-01..06, ANIMATION-16 |
| Enter animation approximations | done | pies and radial bars grow as Recharts' (no mask), radar dots move with their points, Recharts' 80 ms start and 32 ms end lags ported; dashed lines drawn along their length with their own dashes (a mask of their path on the web, a segment on the desktop), scatter symbols grown by area through a registered property's square root on the web, rounded bars drawn again at each height on the desktop; each treemap node slides on its own layer (its rectangle and name, the root's too), never with its ancestors, by the treemap's linear timing on both targets | ANIMATION-03, ANIMATION-09..14, ANIMATION-20..24 |
| Enter animation: rounded bars on the web, treemap nodes' offsets | n/a | a rounded bar's corner radius is clamped to half its animated height, a value CSS cannot compute from a running animation (a `d` keyframe interpolates linearly between its ends; the web scales the bar, its corners squashed while it grows; the desktop is exact); Recharts slides each treemap node from `Math.random()` offsets, which no render can reproduce, so Pictor takes the golden-ratio sequence over the nodes in document order from 1 (a page renders the same bytes every time, as the output cache and the plan's verification need) |  |
| Update animation (previous geometry to new) | done | desktop: every family's `prevPoints`/`prevData`/`prevSectors` ported, frame for frame with Recharts; web: the same logic declared for Aioros's morph behaviors (points mapped by proportional index, `animateNewValues`, bars and symbols by index, pies re-packed, dots and labels waiting), matching Recharts' frames in Chromium and Firefox (`samples/Demo/updates.mjs`, outside the suite; the suite checks the declared morphs, UPDATE-01..24); treemap nodes, curves other than linear and monotone, and rounded bars and sectors keep CSS transitions, Safari unverified | UPDATE-01..24 |
| `animateNewValues`, `onAnimationStart/End` | done | `Line.AnimateNewValues`; the animated elements' desktop events | DETAIL-08, DETAIL-09 |
| Reduced motion | done | CSS `prefers-reduced-motion` on the web; `Chart.ReduceMotion` on the desktop (Avalonia 12 reports no OS preference) | ANIMATION-02, ANIMATION-07 |
| Tooltip: hover trigger | done | CSS `:has()` hover states (decisions "The tooltip without script"); desktop pointer | CHARTS-23, TOOLTIP-02, DESKTOP-02 |
| Tooltip: click trigger | done | | TOOLTIP-11, TOOLTIP-16 |
| Tooltip: shared (cartesian categories) | done | every series at the category in the box, its cursor and active dots, web and desktop | CHARTS-23, CHARTS-24, DESKTOP-02 |
| Tooltip: item (scatter, pie, funnel, treemap, Sankey, sunburst) | done | | SCATTER-06, POLAR-12, FUNNEL-06, TREEMAP-06, SANKEY-06, SUNBURST-06, SUNBURST-07 |
| Tooltip: item mode (`shared={false}`) in bar and radial bar charts, the axis tooltip kept in line, area and composed charts | done | as Recharts' `validateTooltipEventTypes` | TOOLTIP-01, TOOLTIP-02, POLAR-15 |
| Tooltip: cursor (band, line, cross, radar line, radial arc) and its props | done | | SCATTER-06, POLAR-10, CHARTS-23, TOOLTIP-07, TOOLTIP-08 |
| Tooltip: custom content / formatters | done | formats; custom content on every family's tooltip | TOOLTIP-05, CUSTOM-13, CUSTOM-25 |
| Tooltip: follows the pointer | done | Recharts' `getTooltipTranslateXY` at the pointer: on the web through Aioros's pointer variables and one CSS rule per chart (cartesian across the category axis, the radar along its category's angle, radial bars around their band), on the desktop from the pointer; `position`, `reverseDirection` and `allowEscapeViewBox` honored | TOOLTIP-12, TOOLTIP-13, TOOLTIP-18..22 |
| Tooltip: `filterNull`, `itemSorter`, `defaultIndex`, `active` | done | | TOOLTIP-03, TOOLTIP-04, TOOLTIP-05, TOOLTIP-09, TOOLTIP-10 |
| Legend: horizontal and vertical layouts, align, verticalAlign | done | `P/Layout/LegendLayout.cs` | CHARTS-01, POLAR-08, LEGEND-08 |
| Legend: icons | done | all eleven `LegendType`s | LEGEND-04 |
| Legend: custom payload, formatter, content | done | payload and formatter; content as a template per entry or for the whole legend | LEGEND-06, LEGEND-07, CUSTOM-14, CUSTOM-26 |
| Legend: a hidden series' item drawn inactive | done | Recharts' inactive item, `inactiveColor` | AXES-05, LEGEND-05 |
| Legend: click toggles | done (Pictor feature) | every family with a legend; a responsive chart's legend inside the SVG holds its checkboxes in foreign objects; a hidden series' item has no toggle | LEGEND-01..03, LEGEND-11, AUDIT-43, AUDIT-48 |
| syncId / syncMethod (synchronised hover and brush across charts) | done | hover on both targets; the brush window through the sync group on the desktop, through brushes bound to the same model on the web | SYNC-01..04 |
| stackOffset none | done | | CHARTS-08, CHARTS-10 |
| stackOffset expand, silhouette, wiggle, sign, positive | done | `P/Layout/Stacking.cs` | STACK-01..05 |
| reverseStackOrder | done | | STACK-06 |
| barCategoryGap, barGap, barSize, maxBarSize | done | `P/Layout/BarPositions.cs` | CHARTS-09, CHARTS-11, CHARTS-17, STACK-06, STACK-07 |
| Vertical layout | done | | CHARTS-16..18, CHARTS-23, CHARTS-24 |
| Reversed axes | done | `Axis.Reversed` | TICKS-06 |
| allowDataOverflow | done | domain honoured, series clipped | CLIP-01, CLIP-02, CLIP-04 |
| Clip paths only where something uses them, named by their rectangle | done | | CLIP-03 |
| Scale `auto`, `linear`, `band`, `point` | done | | CHARTS-15, CHARTS-17, AXES-08 |
| Scale `log`, `sqrt`, `pow`, `time`, `utc` | done | Aioros.Graphs' ports of d3's scales | AXES-08, AXES-09 |
| Scale `identity`, `quantile`, `quantize`, `sequential`, `threshold` | done | d3's scales as Recharts' `parseScale` sets them over the axis' range (`P/Layout/ValueScale.cs`) | GAPS-23, GAPS-24 |
| Scale `ordinal` | n/a | d3's `scaleOrdinal` on a numeric axis sends each value to one of the two range ends in turn, growing its domain as the series read it, so Recharts' ticks depend on the order its components map values (over values from 2000 to 2500 it drew the ticks 2181 and 2100); no chart relies on that | |
| Nice ticks (recharts-scale) | done | `P/Ticks/NiceTicks.cs` | TICKS-01..03 |
| Tick formatters | done (by decision) | .NET composite formats on axes, polar axes, labels, tooltip, legend, brush | AXES-09, TOOLTIP-05, LEGEND-07, BRUSH-06 |
| Rotated ticks (`angle`) | done | | TICKS-04 |
| Explicit `ticks` | done | | TICKS-07 |
| interval number, preserveStart, preserveEnd, preserveStartEnd | done | `P/Layout/TickPlacement.cs` | TICKS-06, TICKS-08 |
| interval equidistantPreserveStart | done | | TICKS-08 |
| minTickGap, tick collision with Chromium-equal text widths | done | `P/Text/InterMetrics.cs` | CHARTS-01, TICKS-06 |
| Tick labels wider than the axis wrap at its width (Recharts' `Text`) | done | `P/Text/TextLines.cs` | TICKS-09 |
| An axis with `tick={false}` still draws its line | done | kept as Recharts draws it | GAPS-05 |
| mirror | done | | TICKS-06 |
| Multiple x/y axes (ids, biaxial) | done | | CHARTS-04 |
| Curve types | done | 15 of 15 named; `bump` vertical gives bumpY | AXES-07 |
| connectNulls | done | radars, lines and areas | SHAPES-07, GAPS-01 |
| Label positions | done | all, `{x, y}` on label lists and `Label` elements | LABELS-02, POLARAXIS-03, GAPS-28 |
| Label angle/offset/fontSize on axes and references | done | | AXES-01, AXES-02, AXES-03 |
| Label text wrapping within a bar or a funnel's width (`textBreakAll`) | done | | LABELS-05 |
| Active shapes: pie object `activeShape` + `activeIndex` | done | | POLAR-05, POLAR-16 |
| Active shapes: `activeBar`, `activeDot` (props) | done | | CHARTS-21, CHARTS-22, SHAPES-01, SHAPES-07 |
| Active shapes: functions/elements, `inactiveShape`, scatter/funnel/radial `activeShape` | done | the pie's `inactiveShape`; element forms as `ActiveShapeTemplate` on pies, bars, scatters, funnels (drawn in the item's layer under its labels, as Recharts) and radial bars, `ActiveDotTemplate` on lines and areas; `activeIndex` on every family | POLAR-16, CUSTOM-06, CUSTOM-07, CUSTOM-15, CUSTOM-17, CUSTOM-23, CUSTOM-24, SHAPES-09..13 |
| Custom shapes (`shape`, `dot`, `tick`, `content` elements) | done | XAML templates compiled to SVG with holes: shapes of bars, scatters, funnels, radars, radial bars and references, dots, cartesian and polar ticks, labels and label lists of every family, treemap and Sankey content, `Customized` | CUSTOM-01..24 |
| Brush: drawing, window slicing, gap snapping, clamping | done | Recharts' `getIndex` on every move, the web's too | BRUSH-01..04, AUDIT-45 |
| Brush: dragging on the desktop | done | `A/Charts/BrushDrag.cs` | BRUSH-04 |
| Brush: dragging on the web | done | two range inputs over the travellers posted as the framework's fields, and the slide as one range input posting a composite field whose start moves the end by the same amount; with a gap, composite fields snapped by `getIndex`; the slide's fill follows a drag | BRUSH-12, BRUSH-13, BRUSH-14, BRUSH-15, AUDIT-45, AUDIT-47 |
| Brush: tickFormatter, panorama, custom traveller, alwaysShowText | done | tick format, panorama, texts and `TravellerTemplate` | BRUSH-06..10, CUSTOM-11 |
| ResponsiveContainer: fill the parent | done | the exact re-layout at the client's width and `debounce` are n/a, counted once in §2.6 | WEB-05, RESPONSIVE-01 |
| ResponsiveContainer: aspect | done | | RESPONSIVE-01 |
| ResponsiveContainer: min/max sizes | done | Avalonia control props | RESPONSIVE-02 |
| Treemap: flat squarify, color panel | done | | TREEMAP-01, TREEMAP-02 |
| Treemap: nest (static) | done | | TREEMAP-03 |
| Treemap: nest zoom (click a node, breadcrumb navigation) | done | | INTERACT-07, INTERACT-08 |
| Treemap: custom content | done | `ContentTemplate`, `NestIndexTemplate` | CUSTOM-09, CUSTOM-16 |
| Sankey: layout options (padding, width, curvature, iterations, sort) | done | | SANKEY-01, SANKEY-02, SANKEY-07 |
| Sankey: custom node/link | done | `NodeTemplate`, `LinkTemplate` | CUSTOM-10 |
| Sunburst | done | Recharts' `drawArcs` ported | SUNBURST-01..10 |
| Pie: padding angle, min angle, corner radius, labels and label lines, two-level | done | | POLAR-01..05, POLAR-18 |
| Radar range, stacked radial bars | done | | RANGE-06, POLAR-14 |
| Error bars `[low, high]`, on stacked bars | done | | RANGE-05, RANGE-11 |
| Range (floating) bars and areas | done | | RANGE-01..03 |
| ifOverflow `hidden` | done | the reference's shape clipped to the plot, not its label | REFERENCE-01, REFERENCE-03 |
| ifOverflow `visible` on ReferenceLine | done | | REFERENCE-01 |
| Chart and item events (click, hover callbacks) | done | clicks as commands on both targets; the hover callbacks n/a | INTERACT-01..06 |
| Keyboard accessibility (`accessibilityLayer`) | done | desktop exact; web: Aioros's roving focus among a horizontal chart's categories (one tab stop, ArrowLeft/ArrowRight stopping at the ends, Escape), checked in Chromium by the suite (KEYBOARD-13) and compared with Recharts in Chromium and Firefox by `samples/Demo/keyboard.mjs` (outside the suite); Escape keeps the cursor and active dots without the box as Recharts; Home and End left alone as Recharts (`data-aioros-roving-ends="off"`); a scatter chart's keys draw Recharts' cross | KEYBOARD-01..13, DETAIL-04, DETAIL-05 |
| Bound child properties | done | followed on the desktop and by the web plan (a part per bound property, its holes in the spec found by markers: texts, numbers, whole numbers, booleans, enumerations, own items, Avalonia colors and brushes as paints, thicknesses by side, corner radii and number lists, dash patterns); a value no hole holds (a gradient stop's color) is refused by the plan by name, so Aioros's engine renders the page from its current values | DESKTOP-04, WEB-06, WEB-07, WEB-08, BRUSH-12, BINDING-09..12 |
| A series' own data in a stacked chart | done | | RANGE-12 |
| Error bars on a category axis' bands when bars have own data | done | as Recharts in a chart with data of its own; without the chart's data Recharts draws no bar at all (`getStackGroupsByAxisId` returns null), where Pictor draws them | GAPS-32 |

## 5. Summary

### Counts

Counted over every status table of this file (an export row, a prop row or a behavior row each counts once; "Pictor
extra" rows are not counted; "done (by decision)" and "done (Pictor feature)" count as done). The counts at the top
are these totals; COVERAGE-05 checks them.

| Section | done | partial | missing | n/a | total |
|---|---|---|---|---|---|
| §1 Exports | 12 | 0 | 0 | 6 | 18 |
| §2 Props (all components) | 289 | 0 | 0 | 36 | 325 |
| §3 Behaviors | 76 | 0 | 0 | 2 | 78 |
| Added after the inventory, interaction parity, native control, Gallery | 39 | 0 | 0 | 1 | 40 |
| **All** | **416** | **0** | **0** | **45** | **461** |

At the inventory (`0c2a733`, §1 to §3): 367 rows, 127 done, 60 partial, 159 missing, 21 n/a. Rows were split where a
grouped row held props verified by criteria beside props no criterion verifies; those are `partial` with "no criterion
yet".

### Prioritized list at the inventory (historical, all closed)

The list written at the inventory, each item with what became of it. Nothing in it is open: the counts above have no
`partial` or `missing` row.

#### (a) Pictor-only work

Priority 1, common in real apps (shadcn/ui-style charts, Recharts' docs):

1. Criteria for the rows marked "no criterion yet": done, no row is left without its criteria.
2. Theme variants and resources: done, the Gallery following the system theme through Aioros's `/theme/system` and
   charts following Fluent or Semi.
3. Interactions compared with Recharts: touch, keyboard (`accessibilityLayer`), item clicks, `syncId`, update
   animations and live data: done, the fidelity matrix filled (MATRIX-01..17).
4. Scale types `identity`, `quantile`, `quantize`, `sequential`, `threshold`: done;
   `ordinal` is n/a (its implicit domain depends on the order of Recharts' renders).
5. A `<Label>` child element and `{x, y}` positions on axis and reference labels; `PolarAngleAxis`/`PolarRadiusAxis`
   ids, `angleAxisId`/`radiusAxisId` on radars and radial bars: done.

Priority 2, less common configuration:

6. Update animations (previous geometry to the new): done on both targets (the web through Aioros's morph behaviors);
   treemap nodes, curves other than linear and monotone, and rounded bars and sectors keep CSS transitions of `d`.
7. RadialBarChart `layout="centric"` and its stack offsets; `tooltipType` on pie, radar, radial bar and funnel;
   CartesianGrid `horizontalValues`/`verticalValues`; a Bar's `background` props: done.
8. `desc`, `className` on charts and elements, `compact`, `Brush` `ariaLabel`: done; `Text` as an element and
   `defaultShowTooltip` are n/a.
9. Desktop-side interactivity: `onAnimationStart/End` as desktop events (DETAIL-08) and keyboard navigation
   (KEYBOARD-01..13): done; treemap nest zoom and chart, item, legend and axis tick clicks are done on both targets.

#### (b) Needs an Aioros change (or Pictor's use of Aioros's slot renderers)

1. Web Brush dragging (form fields), chart and item `onClick` and the treemap's nest zoom (commands), bound child
   properties (bound parts): done through Aioros's slot renderers. Taken since: a field written by several elements
   (the zoom's choice field, AUDIT-36), one input posting into two parts (the brush's slide, BRUSH-15), a slot's
   post answered with its own region, buttons numbered apart from fields (only
   the buttons shown are written, INTERACT-02) and page-level CSS (a sync id's rules once per page, SYNC-05).
2. Exact responsive layout at the client's width: n/a; `ResponsiveWidths` is the
   scriptless approach.
3. Custom content as XAML templates: done without an Aioros change: a
   template is compiled once to SVG with holes and the slot writer fills them per item; a template that cannot be
   compiled declares a part Aioros's plan refuses by name, so its page is rendered by Avalonia. A public way for a slot
   writer to refuse the plan with a reason would be cleaner; it is not needed.
4. Performance only: typed numeric getters from Aioros's generator (a proposal).
5. Web update animations: done through Aioros's morph behaviors.

#### (c) n/a

- Function props: `tickFormatter`, `formatter`, `labelFormatter`, `valueAccessor`, `CurveFactory`, domain functions,
  `scale` functions, grid coordinate generators, `minPointSize` functions (Pictor's .NET composite formats replace the
  formatters by decision).
- React and DOM plumbing: `ref`, `key`, `id`, `animationId`, `Global.isSsr`, `throttleDelay`, `useTranslate3d`,
  `initialDimension`, `debounce`, `onBBoxUpdate`, `onResize`, `Surface`, `Layer`, `DefaultLegendContent`,
  `DefaultTooltipContent`, `CartesianAxis` as a standalone element, the shapes used standalone (Avalonia shapes serve
  XAML), the tooltip box's animation (each web state is its own box).
- Props filled by the chart (`cx`/`cy`/radius on polar axes and grid, `maxRadius`, XAxis `width`, YAxis `height`),
  deprecated or inert props (`alwaysShow`, `valueKey`, RadialBar `minAngle`/`clockWise`, chart-level `dataKey`), props
  Recharts declares and never reads (`isFront` on references, Sankey `dataKey`), Legend `margin` (overwritten by the
  chart's), `PolarRadiusAxis.tickLine` (never drawn) and the sunburst's animation (Recharts has none).

## Added after the inventory

| Behavior | Status | Where / what is missing | Criteria |
|---|---|---|---|
| A chart follows a property change of an item already in its data (`INotifyPropertyChanged` on the item, nested lists, series' own items), on the desktop | done | `A/Charts/ItemFollower.cs` | BINDING-01, BINDING-03, BINDING-04 |
| The same on the web (live pages) | done | the page's loader or live invalidation renders the chart from the current items; the writer holds no handler | BINDING-07 |
| Many item changes before a frame lay the chart out once | done | | BINDING-02 |
| A chart outside the visual tree holds no subscription | done | taken on attach, dropped on detach | BINDING-05 |
| An item change on another thread is posted once to the chart's dispatcher | done | | BINDING-06 |
| A compiled page's kept markup compares the values below the bound value (nested children, links, range lower ends) | done | | SUNBURST-10, RANGE-13 |
| Gradient paints: Recharts' `linearGradient` in a chart's `defs` named as `url(#id)`, and Avalonia brushes (solid, `LinearGradientBrush`) as a series' `Fill` and `Stroke`, written as SVG gradients on the web and drawn natively on the desktop | done | `P/Specs/GradientSpecs.cs`, `P/Svg/GradientMarkup.cs`, `A/Theming/Paints.cs`, `A/Drawing/ChartPaints.cs`; radial gradients, image brushes and brushes on elements other than series are not taken yet | PAINT-01..04 |

## Interaction parity with Recharts

Recharts' charts respond to the mouse, touch and (with `accessibilityLayer`) the keyboard. Each row is checked by
driving Recharts' client render and Pictor's web chart with the same events in Chromium (and Pictor's desktop chart
with the same input in Avalonia's headless platform) and comparing the resulting markup and pixels.

| Behavior | Status | Where / what is missing | Criteria |
|---|---|---|---|
| Hover: tooltip, cursor and active dots or bars for the category under the pointer | done | the web shows the category's precomputed state, compared with Recharts' render after the same pointer events (`HoverComparison`); the desktop finds the same state | TOOLTIP-01, TOOLTIP-02, POLAR-15, CHARTS-24, DESKTOP-02 |
| Tooltip following the pointer (`position` unset) | done | the box's position measured in Chromium by the suite at the reference examples' mouse positions, within 1 px of Recharts' recorded box (TOOLTIP-23); Firefox by `samples/Demo/follow.mjs` over the LAN, outside the suite (CI has no Firefox) | TOOLTIP-18..23 |
| Touch: `touchstart`/`touchmove` show the tooltip of the category under the finger, `touchend` keeps it (Recharts' `handleTouchMove` and `handleTouchStart`) | done | desktop: tap, drag (axis tooltips follow, item tooltips stay), lift and tap outside as Recharts; web: a tap shows the state, a drag moves an axis tooltip through Aioros's touch follow (`data-aioros-follow`, `="capture"` in vertical charts) and synced charts with it, lifting keeps it, a tap outside hides it, item tooltips stay; checked in Chromium by the suite (CDP touches on the Gallery: drag, lift, tap outside, synced charts, a vertical chart's capture, TOUCH-19); compared with Recharts under the same touches in Chromium and Firefox by `samples/Demo/touch.mjs` and `gallery-focus.mjs`, outside the suite (CI has no Firefox); over command buttons too | TOUCH-01..12, TOUCH-14..19 |
| Touch: dragging the Brush's travellers and slide | done | the web's native range inputs take touch on the travellers and the slide; the desktop drags with a finger | BRUSH-13, BRUSH-15, TOUCH-13 |
| Click on an item or the chart (`onClick` and the item handlers) | done | the framework's buttons over each item's hover area on the web, the pointer on the desktop, the same parameter text, compared with what Recharts' handlers received | INTERACT-01..06, MATRIX-01..05 |
| Legend click toggling (Pictor's feature) and Recharts' legend events | done | toggles; `onClick` as a command; hover events n/a (as the items') | LEGEND-01..03, LEGEND-11, INTERACT-05 |
| Keyboard (`accessibilityLayer`): focus, arrow keys move the active category, the tooltip follows | done | desktop as Recharts' `AccessibilityManager` and the tooltip's Escape; web: Aioros's roving focus (one tab stop, the arrows, Escape, Home and End left alone), checked step by step in Chromium by the suite (KEYBOARD-13) and against Recharts' tooltip in Chromium and Firefox by `samples/Demo/keyboard.mjs`, outside the suite (CI has no Firefox); a scatter chart's cross | KEYBOARD-01..13, DETAIL-04, DETAIL-05 |
| `syncId`: charts sharing the active index (and the Brush window) | done | | SYNC-01..04 |
| Live data: the chart updates when the server pushes new data, without a reload | done | an Aioros live page with a view whose loader reads an invalidation key (Gallery `/interactive`, checked in Chromium) | LIVE-01, LIVE-02 |
| Update animation from the previous geometry to the new | done | as in §3 (desktop exact; web exact through Aioros's morph behaviors, no enter replay after a post or a theme switch) | UPDATE-01..25 |

## A native Avalonia control

Recharts has no theme system (colors are props); a XAML chart must behave as an Avalonia control. Recharts' defaults
stay the defaults of the light theme, so the reference examples keep matching.

| Behavior | Status | Where / what is missing | Criteria |
|---|---|---|---|
| Theme variants: a chart's default colors (grid, axes, ticks, tooltip box, legend text, brush, cursor) follow `ThemeVariant` Light and Dark, with Recharts' values in Light | done | `ChartTheme.Light` (Recharts') and `.Dark` (Pictor's) in `PictorTheme`'s dictionaries, resolved into the spec; a legend's text keeps its series' color, as in Recharts | THEME-01, THEME-03, THEME-20..43 |
| The theme's colors as resources (`DynamicResource` keys for the defaults, overridable per app, per page and per chart) | done | `PictorKeys`; strings, colors and solid brushes; an element's value wins over every key | THEME-02, THEME-04..09, THEME-11, THEME-14..16, STYLE-07 |
| A theme switch at run time redraws the chart (desktop) and the web page served in each theme draws its own colors (Aioros writes a page per theme) | done | the desktop follows `ActualThemeVariantChanged` and `ResourcesChanged`; each per-theme plan records its theme | THEME-10..12, THEME-17 |
| Third-party and Fluent themes: the chart takes its text font and colors from the theme it sits in | done | `PictorTheme.Host`: `Fluent` maps Pictor's keys to Fluent's base, alt, list and accent colors and its font, `Semi` to Semi.Avalonia's (and Ursa's Semi theme's) text, border, background, fill and primary colors and its font, through `ResourceAlias` entries looked up from the chart for its variant; any other theme maps its resources the same way with `ResourceAlias` (best effort, by the Aioros ecosystem's decision: Fluent and Aioros's own library guaranteed); the default stays Recharts' light theme, so the references keep matching; texts measured with Inter's metrics | THEME-13, THEME-19 |
| Avalonia styles on chart elements: `Style Selector="p\|Line.accent"`, classes and pseudo-classes on series, axes and cells, setters for every styled property | done | elements are styled elements in the chart's logical tree: styles, classes, control themes (implicit and named) and dynamic resources reach them; no pseudo-class is defined on them | STYLE-01..06 |
| Data binding of every property, including a change of a property of an item already in the data (`INotifyPropertyChanged` on the item) | done | items followed on both targets; bound child properties of every followed kind (texts, numbers, whole numbers, booleans, enumerations, a series' own items, Avalonia `Color`s and brushes (solid and linear gradient), `Thickness`es, `CornerRadius`es and number lists, dash patterns) followed by the web plan, a bound value the spec does not copy (a gradient stop's color) left to Aioros's engine, which renders the chart from its current values | BINDING-01..12, DESKTOP-04, WEB-06..08 |
| `FlowDirection` right to left | n/a | Recharts has no RTL | |
| Automation (screen readers): `AutomationProperties` and a peer exposing the chart's data | done | `ChartAutomationPeer`: a list named by the title or automation name, one item per hover state named by its tooltip text | A11Y-01, A11Y-02 |

## The Gallery as the demo

Pictor's demo is its Gallery (an Aioros app), and it shows what the port does, on the web and
the desktop, each part with its criteria.

| Behavior | Status | Where / what is missing | Criteria |
|---|---|---|---|
| A theme switch for the whole app (Light, Dark, following the system), changing the Gallery's chrome and every chart at once, through `PictorTheme`'s dictionaries; the choice kept across pages | done | Light, Dark and System in the sidebar through Aioros's `/theme/*` routes, a cookie kept across pages; System follows `Sec-CH-Prefers-Color-Scheme` or the `theme-system` cookie the framework's script keeps from `matchMedia` (checked by hand in Chromium and Firefox over the LAN, the device's scheme emulated, outside the suite; the suite checks the cookie, the hint and the pages written), `ThemeVariant.Default` on the desktop | THEME-10, THEME-12, THEME-18 |
| The enter animations of every chart family, replayable on demand (a button that renders the chart again), honoring reduced motion | done | `ReplayButton` on every family page: on the web a checkbox whose change restarts the page's CSS animations (no script, no request), on the desktop a replay of every chart in the window | ANIMATION-15 |
| Update animations: data changing in place (a button and a live feed) animating from the previous geometry to the new | done | "Change data" and a posted data version on every family page, `/updates` with Recharts' recorded cases (more, fewer and no items before), the live chart on `/interactive` | UPDATE-18..24 |
| Every interaction of the matrix shown on a page: hover, touch, keyboard, item clicks running a command, legend toggles, the Brush moving, treemap zoom, `syncId`, live data | done | `/families`: every family's chart with its tooltip (hover, touch), Recharts' keyboard focus on the cartesian charts, item clicks running a command of the family, legend toggles and brushes; `/live-charts`: every family updated live, a quarter of the families at each tick so each morph settles before the family's next push; the theme switch in the sidebar; `/interactive`: treemap zoom, `syncId` by index and value | GALLERY-02, GALLERY-06, MATRIX-06..17, THEME-18 |
| Customization: a page restyling charts through resource keys, styles and control themes | done | `/themes`: an application key, a page's keys, a chart's keys, styles with classes, a control theme | THEME-04..06, STYLE-01..07 |
| A page per chart family with each Recharts reference example beside Recharts' render and the verdict | done | `/compare/{family}` (linked from each family page and the sidebar's `/compare`): every reference example without custom content drawn by Pictor's writer beside Recharts' screenshot, with the element verdict of the tests' comparison and the recorded share of strongly differing pixels, shown only while Pictor writes the markup it was measured on (the headline counts measured examples only) | GALLERY-01, GALLERY-05 |
| The home page is a live analytics dashboard for three dummy products (a SaaS web app, a mobile app, an API), the product and range chosen in the URL, every chart a Pictor control: KPI sparklines, an area and line chart with a brush, a clickable bar chart, a donut, a funnel, stacked bars, a punch card of scatter rows, two synced charts | done | `samples/Gallery/Gallery/Pages/Page.axaml`, `Views/`, `Analytics/` | DASHBOARD-01, DASHBOARD-05 |
| KPI tiles: compact values, deltas against the previous period with an arrow and a sign, good or bad by the metric's direction | done | | DASHBOARD-02, DASHBOARD-03 |
| A click on a top page focuses the main chart on it (a command navigating to the dashboard's address with the focus) | done | | DASHBOARD-06 |
| Real time from a seeded generator, one owner publishing immutable moments, a ticker invalidating the live key each second so that only the live views' regions are pushed | done | | DASHBOARD-04, DASHBOARD-07, DASHBOARD-08 |
| Every page usable on a phone and a tablet: no route scrolls sideways at 390 or 1024 px, charts fill their card at their aspect (scaled within it, those with legend toggles too) or, when they keep their pixel size (the comparison panels), scroll in their own box; links and titles wrap; the dashboard's KPI tiles stack | done | `samples/Gallery` (decisions "Gallery fixes after the audit", "Fixes after the second functional audit") | GALLERY-03, DASHBOARD-12, AUDIT-44 |
| The samples read in both themes: the neutral colors the reference examples set (black labels, light backgrounds, grid stripes, cursor bands) come from the Gallery's sample keys, Recharts' literals in light and dark values in dark | done | `App.axaml` (`Sample*` keys) | GALLERY-04 |
| A grouped sidebar (overview, chart families, features) marking the current page, collapsing to a drawer on narrow web widths, on both targets | done | `Pages/Layout.axaml`; the drawer is web only (a popover below 1024 px, checked in Chromium), the desktop keeps the pane (`SplitView` inline and open) | DASHBOARD-09 |
| Switching the product, the range or the focus on the web keeps the live views on the new choice | done | Aioros's script opens the live stream again with the new query (SSR-179); checked in Chromium by the suite (pushes awaited after ticks of the Gallery's test clock); Firefox only by hand over the LAN, outside the suite (CI has no Firefox); the desktop is right | DASHBOARD-10 |
| The main chart's brush kept across live ticks | done | on every range: the web brush is bound to a window that goes to the dashboard's address with `from` and `to` when a post moves it, so the post is answered with that address and the live stream opened again at it keeps the window; the desktop model keeps its brush as dragged | DASHBOARD-11 |

## Fidelity matrix

One row per chart family, one column per aspect. A cell names the criteria that compare that aspect of the family
with Recharts driven the same way (on the web, the desktop or both), or `n/a` with the reason; an empty cell is
missing. The `Finished` marker at the top gates the empty cells (COVERAGE-03).

- **Click** is a click on an item or the chart (`onClick`, the item handlers, the treemap's nest zoom); the tooltip's
  click trigger (TOOLTIP-11, TOOLTIP-16) is a hover state.
- **Live update** is the chart following new data without a reload on both targets: on the web a live view of the
  Gallery's `/live-charts` pushed with the new data and, where Recharts animates, the update morph (MATRIX-06..17); on
  the desktop the view model receiving the data (LIVE-02, BINDING-01) and each family's update animation (UPDATE-01..09).
- **Brush** applies where Recharts' family takes a `Brush` (the categorical cartesian charts).

| Family | Render | Hover | Touch | Keyboard | Click | Legend | Brush | Theme light | Theme dark | Live update |
|---|---|---|---|---|---|---|---|---|---|---|
| LineChart | CHARTS-01..04, CHARTS-16, REFERENCE-03 | CHARTS-23, TOOLTIP-02, TOOLTIP-07, DESKTOP-02 | TOUCH-01, TOUCH-14, TOUCH-16, TOUCH-17 | KEYBOARD-01, KEYBOARD-05, KEYBOARD-07, KEYBOARD-08, KEYBOARD-09, KEYBOARD-11 | INTERACT-03 | CHARTS-01, LEGEND-01, LEGEND-02, LEGEND-08 | BRUSH-06, BRUSH-08 | THEME-20 | THEME-21 | LIVE-01, LIVE-02, MATRIX-06 |
| AreaChart | CHARTS-07, CHARTS-08, STACK-01, RANGE-01, RANGE-10 | RANGE-07 | TOUCH-02, TOUCH-14 | KEYBOARD-02, KEYBOARD-08 | MATRIX-01 | GAPS-34 | GAPS-34 | THEME-22 | THEME-23 | MATRIX-07, UPDATE-02, LIVE-02 |
| BarChart | CHARTS-09, CHARTS-10, CHARTS-12, CHARTS-18, DESKTOP-01 | CHARTS-21, CHARTS-24, TOOLTIP-01, TOOLTIP-08 | TOUCH-03, TOUCH-13, TOUCH-14, TOUCH-15, TOUCH-18 | KEYBOARD-03, KEYBOARD-08, KEYBOARD-10 | INTERACT-01, INTERACT-02, INTERACT-06 | BRUSH-01, LEGEND-04 | BRUSH-01, BRUSH-02, BRUSH-04, BRUSH-07, BRUSH-12, BRUSH-13 | THEME-24 | THEME-25 | MATRIX-08, UPDATE-03, LIVE-02 |
| ComposedChart | CHARTS-15, CHARTS-17, CHARTS-19, CLIP-01, CLIP-04 | CHARTS-22 | TOUCH-04, TOUCH-14 | KEYBOARD-04, KEYBOARD-08 | MATRIX-02 | CHARTS-17 | GAPS-34 | THEME-26 | THEME-27 | MATRIX-09, LIVE-02 |
| ScatterChart | SCATTER-01..03, SCATTER-05, SCATTER-08 | SCATTER-06, SCATTER-07 | TOUCH-05 | DETAIL-04, DETAIL-05 | MATRIX-03 | SCATTER-02 | GAPS-22 | THEME-28 | THEME-29 | MATRIX-10, UPDATE-04, LIVE-02 |
| PieChart | POLAR-01..05, POLAR-11 | POLAR-12 | TOUCH-06 | n/a: Recharts' AccessibilityManager moves the tooltip only in a horizontal axis chart (KEYBOARD-06 and KEYBOARD-10 check it does nothing) | INTERACT-04 | LEGEND-03 | n/a: Recharts' Brush slices a cartesian axis' categories | THEME-30 | THEME-31 | MATRIX-11, UPDATE-05, BINDING-01 |
| RadarChart | POLAR-06, POLAR-07, POLAR-11 | POLAR-10, RANGE-07 | TOUCH-07, TOUCH-14 | n/a: Recharts' AccessibilityManager moves the tooltip only in a horizontal axis chart (KEYBOARD-06 and KEYBOARD-10 check it does nothing) | MATRIX-04 | POLAR-07, LEGEND-03 | n/a: Recharts' Brush slices a cartesian axis' categories | THEME-32 | THEME-33 | MATRIX-12, UPDATE-06, LIVE-02 |
| RadialBarChart | POLAR-08, POLAR-13, POLAR-14 | POLAR-10, POLAR-15 | TOUCH-08, TOUCH-14 | n/a: Recharts' AccessibilityManager moves the tooltip only in a horizontal axis chart (KEYBOARD-06 and KEYBOARD-10 check it does nothing) | MATRIX-05 | POLAR-08, LEGEND-11 | n/a: Recharts' Brush slices a cartesian axis' categories | THEME-34 | THEME-35 | MATRIX-13, UPDATE-07, LIVE-02 |
| FunnelChart | FUNNEL-01, FUNNEL-02, FUNNEL-05 | FUNNEL-06 | TOUCH-09 | n/a: Recharts' AccessibilityManager moves the tooltip only in a horizontal axis chart (KEYBOARD-06 and KEYBOARD-10 check it does nothing) | INTERACT-04 | FUNNEL-02, LEGEND-11 | n/a: Recharts' Brush slices a cartesian axis' categories | THEME-36 | THEME-37 | MATRIX-14, UPDATE-08, BINDING-01 |
| Treemap | TREEMAP-01..03, TREEMAP-05 | TREEMAP-06 | TOUCH-10 | n/a: Recharts' Treemap handles no key | INTERACT-04, INTERACT-07, INTERACT-08 | n/a: Recharts' Treemap renders no Legend | n/a: no Brush in Recharts' Treemap | THEME-38 | THEME-39 | MATRIX-15, UPDATE-09, BINDING-01 |
| Sankey | SANKEY-01, SANKEY-02, SANKEY-05 | SANKEY-06 | TOUCH-11 | n/a: Recharts' Sankey handles no key | INTERACT-04 | n/a: Recharts' Sankey renders no Legend | n/a: no Brush in Recharts' Sankey | THEME-40 | THEME-41 | MATRIX-16, BINDING-01 |
| SunburstChart | SUNBURST-01, SUNBURST-02, SUNBURST-05 | SUNBURST-06, SUNBURST-07 | TOUCH-12 | n/a: Recharts' SunburstChart handles no key (its groups are focusable, nothing reads a key) | INTERACT-04 | n/a: Recharts' SunburstChart renders no Legend | n/a: no Brush in Recharts' SunburstChart | THEME-42 | THEME-43 | MATRIX-17, BINDING-01 |
