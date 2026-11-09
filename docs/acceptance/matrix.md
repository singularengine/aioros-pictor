# Fidelity matrix

The cells of [the fidelity matrix](../coverage.md#fidelity-matrix) that no other area's criteria filled: the click on
the items of the families whose clicks had no criterion, and the live update of every family through a live view.

Clicks are compared with Recharts' own handlers: the examples `MatrixClick*` (`tools/recharts/catalog/matrix.mjs`) give
Recharts recording handlers (`{$click}`), and `screenshot.mjs` writes what each handler received after each click
(`out/<name>.<i>.clicks.json`). Recharts runs an item's handler and then the chart's (the event bubbles to the
wrapper); Pictor runs the topmost target's command alone, as INTERACT-06 states. An active dot's handler receives the
dot's own props from Recharts (`adaptEventHandlers(activeDot)`), no index: the index Pictor's dot command takes is the
active index the chart's handler receives in the same click.

Live updates go through the Gallery's `/live-charts` page: one view per family whose loader reads the `live` key, which the
Gallery's ticker invalidates each second, so each connection's stream pushes the view's region with the chart over the
next readings. Each live criterion opens its own stream, ticks the Gallery's test clock (dashboard.md) and compares the
push after its first tick with the first later push whose tick differs, every read bounded so a silent server fails the
test instead of hanging it. Where Recharts animates an update, the pushed chart declares Aioros's morph behaviors (decision "Update
animations"), so the framework's script moves each shape from the previous numbers; Recharts' Sankey and SunburstChart
do not animate, and their charts declare nothing.

| Id | Criterion |
|---|---|
| MATRIX-01 | Given MatrixClickAreaChart on the desktop with the chart's `ClickCommand` and the area's, when it is clicked where Recharts was clicked, then a click on the third category's active dot runs the area's command with `"2"` (Recharts' chart handler's `activeTooltipIndex` in that click, its dot handler receiving no index) and not the chart's, a click in the fifth category's zone away from the dot runs the chart's with `"4"` as Recharts' `onClick` received; the web chart reserves a button with each of those parameters. |
| MATRIX-02 | Given MatrixClickComposedChart on the desktop with the chart's `ClickCommand`, the bar's, the scatter's, the line's and the area's, when it is clicked where Recharts was clicked, then a click on the third bar runs the bar's command with `"2"` (Recharts' bar `onClick` index), on the line's second active dot the line's with `"1"`, on the first scatter symbol the scatter's with `"0"`, and in the sixth category's zone above every item the chart's with `"5"`, each click running one command; the web chart reserves a button with each of those parameters. |
| MATRIX-03 | Given MatrixClickScatterChart on the desktop with the scatter's `ClickCommand`, when it is clicked where Recharts was clicked, then a click on the first symbol runs it with `"0"` and on the third with `"2"`, Recharts' `onClick` index, and a click beside every symbol runs nothing, as Recharts called no handler; the web chart reserves a button per symbol with its index. |
| MATRIX-04 | Given MatrixClickRadarChart on the desktop with the chart's `ClickCommand` and both radars', when it is clicked where Recharts was clicked, then a click on Mike's first active dot runs Mike's command with `"0"` and on Lily's second dot Lily's with `"1"` (the `activeTooltipIndex` Recharts' chart handler received in those clicks), and a click inside the third angle's sector away from the dots runs the chart's with `"2"`; the web chart reserves a button with each of those parameters. |
| MATRIX-05 | Given MatrixClickRadialBarChart on the desktop with the chart's `ClickCommand` and the radial bar's, when it is clicked where Recharts was clicked, then a click on the fourth bar (where Recharts 3 lays its ring) runs the bar's command with `"3"` and a click on the sixth band's background with `"5"`, Recharts 3's radial bar `onClick` index (the chart's handler receiving the same `activeTooltipIndex`), and not the chart's; the web chart reserves a button with each of those parameters. Given BarChartHasBackground with a `ClickCommand` on its `pv` bar, a click in the third bar's background band above the bar runs it with `"2"`, as Recharts' background rectangles and sectors carry their bar's handlers. |
| MATRIX-06 | Given the running Gallery and a client following the live stream of `/live-charts`, when the ticker invalidates the line view's key (`live-line`, every fourth tick: GALLERY-06), then each push of the line view carries its region with the tick it shows (`Tick N at mm:ss`, another in each of its pushes) and its chart drawn over that tick's data (the reference data with each value scaled at that tick: its curves other paths than in the push before), declaring the line's morph (`data-aioros-morph-path="line monotone-x"`), as Recharts animates a line whose data changed. |
| MATRIX-07 | Given the same stream, when ticks come, then each push of the area view carries its region with the area over the new readings (another band than the push before) declaring `band monotone-x` mapped by ratio. |
| MATRIX-08 | Given the same stream, when ticks come, then each push of the bar view carries its region with bars of the new values (other rectangles than the push before) declaring `rect` and where a new bar grows from. |
| MATRIX-09 | Given the same stream, when ticks come, then each push of the composed view carries its region, whose bar, line and area are drawn over the new readings and each declare their morph (`rect`, `line`, `band`). |
| MATRIX-10 | Given the same stream, when ticks come, then each push of the scatter view carries its region with the symbols at the new points (other centers than the push before) declaring `glyph`. |
| MATRIX-11 | Given the same stream, when ticks come, then each push of the pie view carries its region with sectors of the new values (other paths than the push before) declaring `sector`. |
| MATRIX-12 | Given the same stream, when ticks come, then each push of the radar view carries its region with the polygon through the new values declaring `polygon` mapped by ratio from the chart's center. |
| MATRIX-13 | Given the same stream, when ticks come, then each push of the radial bar view carries its region with sectors of the new values declaring `sector`. |
| MATRIX-14 | Given the same stream, when ticks come, then each push of the funnel view carries its region with trapezoids of the new values declaring `polygon` (a trapezoid's corners) and where a new one starts. |
| MATRIX-15 | Given the same stream, when ticks come, then each push of the treemap view carries its region with the leaves laid out over the new sizes (other rectangles than the push before), which Pictor keeps moving by CSS transitions of `d` (the treemap's update approximation, decision "Update animations"). |
| MATRIX-16 | Given the same stream, when ticks come, then each push of the Sankey view carries its region with links of the new values (other paths than the push before) and no morph declaration, as Recharts' Sankey does not animate. |
| MATRIX-17 | Given the same stream, when ticks come, then each push of the sunburst view carries its region with sectors of the new values (other paths than the push before) and no morph declaration, as Recharts' SunburstChart does not animate. |
