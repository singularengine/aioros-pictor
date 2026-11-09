using Avalonia;

namespace Aioros.Pictor;

/// <summary>A bar's props for its <c>ShapeTemplate</c> and <c>ActiveShapeTemplate</c> (Recharts' <c>shape</c> and <c>activeBar</c>).</summary>
public sealed class BarShapeContext : IContentContext
{
    internal BarShapeContext(ContentState state) => State = state;

    /// <summary>The bar's left.</summary>
    public double X => State.Number(nameof(X));

    /// <summary>The bar's top.</summary>
    public double Y => State.Number(nameof(Y));

    /// <summary>The bar's width.</summary>
    public double Width => State.Number(nameof(Width));

    /// <summary>The bar's height.</summary>
    public double Height => State.Number(nameof(Height));

    /// <summary>The bar's value.</summary>
    public double Value => State.Number(nameof(Value));

    /// <summary>The item's index.</summary>
    public int Index => State.Whole(nameof(Index));

    /// <summary>The background's left.</summary>
    public double BackgroundX => State.Number(nameof(BackgroundX));

    /// <summary>The background's top.</summary>
    public double BackgroundY => State.Number(nameof(BackgroundY));

    /// <summary>The background's width.</summary>
    public double BackgroundWidth => State.Number(nameof(BackgroundWidth));

    /// <summary>The background's height.</summary>
    public double BackgroundHeight => State.Number(nameof(BackgroundHeight));

    /// <summary>The bar's fill (its cell's, or the series').</summary>
    public string? Fill => State.Text(nameof(Fill));

    /// <summary>The bar's stroke.</summary>
    public string? Stroke => State.Text(nameof(Stroke));

    /// <summary>The series' name.</summary>
    public string? Name => State.Text(nameof(Name));

    /// <summary>The series' data key.</summary>
    public string? DataKey => State.Text(nameof(DataKey));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A point's props for a line's or an area's <c>DotTemplate</c> and <c>ActiveDotTemplate</c> (Recharts' <c>dot</c> and <c>activeDot</c>).</summary>
public sealed class DotContext : IContentContext
{
    internal DotContext(ContentState state) => State = state;

    /// <summary>The point's x.</summary>
    public double Cx => State.Number(nameof(Cx));

    /// <summary>The point's y.</summary>
    public double Cy => State.Number(nameof(Cy));

    /// <summary>The default dot's radius.</summary>
    public double R => State.Number(nameof(R));

    /// <summary>The default dot's stroke width.</summary>
    public double StrokeWidth => State.Number(nameof(StrokeWidth));

    /// <summary>The point's value.</summary>
    public double Value => State.Number(nameof(Value));

    /// <summary>The item's index.</summary>
    public int Index => State.Whole(nameof(Index));

    /// <summary>The default dot's fill.</summary>
    public string? Fill => State.Text(nameof(Fill));

    /// <summary>The default dot's stroke (the series' color).</summary>
    public string? Stroke => State.Text(nameof(Stroke));

    /// <summary>The series' name.</summary>
    public string? Name => State.Text(nameof(Name));

    /// <summary>The series' data key.</summary>
    public string? DataKey => State.Text(nameof(DataKey));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A label's props for a <c>LabelList</c>'s or a <c>Label</c>'s <c>ContentTemplate</c> (Recharts' <c>content</c>).</summary>
public sealed class LabelContext : IContentContext
{
    internal LabelContext(ContentState state) => State = state;

    /// <summary>The x of the box the label is placed around (a point's x; on a polar shape, where the default label sits).</summary>
    public double X => State.Number(nameof(X));

    /// <summary>The y of that box.</summary>
    public double Y => State.Number(nameof(Y));

    /// <summary>The box's width (0 for a point).</summary>
    public double Width => State.Number(nameof(Width));

    /// <summary>The box's height.</summary>
    public double Height => State.Number(nameof(Height));

    /// <summary>The value, as a number.</summary>
    public double Value => State.Number(nameof(Value));

    /// <summary>The item's index.</summary>
    public int Index => State.Whole(nameof(Index));

    /// <summary>The label's offset.</summary>
    public double Offset => State.Number(nameof(Offset));

    /// <summary>A polar label's center x (Recharts' polar view box).</summary>
    public double Cx => State.Number(nameof(Cx));

    /// <summary>A polar label's center y.</summary>
    public double Cy => State.Number(nameof(Cy));

    /// <summary>A polar label's inner radius.</summary>
    public double InnerRadius => State.Number(nameof(InnerRadius));

    /// <summary>A polar label's outer radius.</summary>
    public double OuterRadius => State.Number(nameof(OuterRadius));

    /// <summary>A polar label's start angle, in degrees.</summary>
    public double StartAngle => State.Number(nameof(StartAngle));

    /// <summary>A polar label's end angle, in degrees.</summary>
    public double EndAngle => State.Number(nameof(EndAngle));

    /// <summary>The value's text, through the label's format.</summary>
    public string? Text => State.Text(nameof(Text));

    /// <summary>The label's own fill, when it sets one (Recharts passes no other).</summary>
    public string? Fill => State.Text(nameof(Fill));

    /// <summary>The series' name.</summary>
    public string? Name => State.Text(nameof(Name));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A tick's props for an axis' <c>TickTemplate</c> (Recharts' <c>tick</c> element).</summary>
public sealed class TickContext : IContentContext
{
    internal TickContext(ContentState state) => State = state;

    /// <summary>The x of the tick's label.</summary>
    public double X => State.Number(nameof(X));

    /// <summary>The y of the tick's label.</summary>
    public double Y => State.Number(nameof(Y));

    /// <summary>The axis' width.</summary>
    public double Width => State.Number(nameof(Width));

    /// <summary>The axis' height.</summary>
    public double Height => State.Number(nameof(Height));

    /// <summary>The tick's index.</summary>
    public int Index => State.Whole(nameof(Index));

    /// <summary>The tick's coordinate along the axis.</summary>
    public double Coordinate => State.Number(nameof(Coordinate));

    /// <summary>How many ticks are shown.</summary>
    public int VisibleTicksCount => State.Whole(nameof(VisibleTicksCount));

    /// <summary>A polar axis' center x (Recharts' <c>cx</c>); 0 on a cartesian axis.</summary>
    public double Cx => State.Number(nameof(Cx));

    /// <summary>A polar axis' center y (Recharts' <c>cy</c>); 0 on a cartesian axis.</summary>
    public double Cy => State.Number(nameof(Cy));

    /// <summary>A polar angle axis' radius (Recharts' <c>radius</c>); 0 elsewhere.</summary>
    public double Radius => State.Number(nameof(Radius));

    /// <summary>A polar radius axis' angle in degrees, its ticks turned by 90 less it; 0 elsewhere.</summary>
    public double Angle => State.Number(nameof(Angle));

    /// <summary>The tick's text (Recharts' <c>payload.value</c>).</summary>
    public string? Value => State.Text(nameof(Value));

    /// <summary>The tick's fill.</summary>
    public string? Fill => State.Text(nameof(Fill));

    /// <summary>The axis' stroke.</summary>
    public string? Stroke => State.Text(nameof(Stroke));

    /// <summary>The anchor the default label takes.</summary>
    public Specs.TextAnchorKind TextAnchor => State.Text(nameof(TextAnchor)) switch { "end" => Specs.TextAnchorKind.End, "middle" => Specs.TextAnchorKind.Middle, _ => Specs.TextAnchorKind.Start };

    /// <summary>The axis' orientation.</summary>
    public string? Orientation => State.Text(nameof(Orientation));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A sector's props for a pie's or a radial bar's <c>ShapeTemplate</c> and <c>ActiveShapeTemplate</c> (Recharts' <c>shape</c> and <c>activeShape</c>).</summary>
public sealed class SectorContext : IContentContext
{
    internal SectorContext(ContentState state) => State = state;

    /// <summary>The center's x.</summary>
    public double Cx => State.Number(nameof(Cx));

    /// <summary>The center's y.</summary>
    public double Cy => State.Number(nameof(Cy));

    /// <summary>The inner radius.</summary>
    public double InnerRadius => State.Number(nameof(InnerRadius));

    /// <summary>The outer radius.</summary>
    public double OuterRadius => State.Number(nameof(OuterRadius));

    /// <summary>The start angle, in degrees.</summary>
    public double StartAngle => State.Number(nameof(StartAngle));

    /// <summary>The end angle, in degrees.</summary>
    public double EndAngle => State.Number(nameof(EndAngle));

    /// <summary>The middle angle, in degrees.</summary>
    public double MidAngle => State.Number(nameof(MidAngle));

    /// <summary>The item's share of the total, from 0 to 1.</summary>
    public double Percent => State.Number(nameof(Percent));

    /// <summary>The item's value.</summary>
    public double Value => State.Number(nameof(Value));

    /// <summary>The item's index.</summary>
    public int Index => State.Whole(nameof(Index));

    /// <summary>The corner radius.</summary>
    public double CornerRadius => State.Number(nameof(CornerRadius));

    /// <summary>The cosine of the middle angle as Recharts' examples compute it (<c>Math.cos(-RADIAN * midAngle)</c>).</summary>
    public double Cos => State.Number(nameof(Cos));

    /// <summary>The sine of the middle angle as Recharts' examples compute it (<c>Math.sin(-RADIAN * midAngle)</c>).</summary>
    public double Sin => State.Number(nameof(Sin));

    /// <summary>1 when the middle angle points right (its cosine is at least 0), -1 when it points left.</summary>
    public double Side => State.Number(nameof(Side));

    /// <summary>The x of the point on the outer radius at the middle angle.</summary>
    public double OuterX => State.Number(nameof(OuterX));

    /// <summary>The y of the point on the outer radius at the middle angle.</summary>
    public double OuterY => State.Number(nameof(OuterY));

    /// <summary>The sector's fill.</summary>
    public string? Fill => State.Text(nameof(Fill));

    /// <summary>The sector's stroke.</summary>
    public string? Stroke => State.Text(nameof(Stroke));

    /// <summary>The item's name.</summary>
    public string? Name => State.Text(nameof(Name));

    /// <summary><c>Start</c> on the right side, <c>End</c> on the left.</summary>
    public Specs.TextAnchorKind TextAnchor => State.Text(nameof(TextAnchor)) switch { "end" => Specs.TextAnchorKind.End, "middle" => Specs.TextAnchorKind.Middle, _ => Specs.TextAnchorKind.Start };

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A trapezoid's props for a funnel's <c>ShapeTemplate</c> and <c>ActiveShapeTemplate</c>.</summary>
public sealed class TrapezoidContext : IContentContext
{
    internal TrapezoidContext(ContentState state) => State = state;

    /// <summary>The left of its upper edge.</summary>
    public double X => State.Number(nameof(X));

    /// <summary>Its top.</summary>
    public double Y => State.Number(nameof(Y));

    /// <summary>The upper edge's width.</summary>
    public double UpperWidth => State.Number(nameof(UpperWidth));

    /// <summary>The lower edge's width.</summary>
    public double LowerWidth => State.Number(nameof(LowerWidth));

    /// <summary>Its height.</summary>
    public double Height => State.Number(nameof(Height));

    /// <summary>The item's value.</summary>
    public double Value => State.Number(nameof(Value));

    /// <summary>The item's index.</summary>
    public int Index => State.Whole(nameof(Index));

    /// <summary>The item's share of the first value.</summary>
    public double Percent => State.Number(nameof(Percent));

    /// <summary>The fill.</summary>
    public string? Fill => State.Text(nameof(Fill));

    /// <summary>The stroke.</summary>
    public string? Stroke => State.Text(nameof(Stroke));

    /// <summary>The item's name.</summary>
    public string? Name => State.Text(nameof(Name));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A point's props for a scatter's <c>ShapeTemplate</c> and <c>ActiveShapeTemplate</c>.</summary>
public sealed class ScatterShapeContext : IContentContext
{
    internal ScatterShapeContext(ContentState state) => State = state;

    /// <summary>The point's x.</summary>
    public double Cx => State.Number(nameof(Cx));

    /// <summary>The point's y.</summary>
    public double Cy => State.Number(nameof(Cy));

    /// <summary>The left of the symbol's box.</summary>
    public double X => State.Number(nameof(X));

    /// <summary>The top of the symbol's box.</summary>
    public double Y => State.Number(nameof(Y));

    /// <summary>The symbol's box width.</summary>
    public double Width => State.Number(nameof(Width));

    /// <summary>The symbol's box height.</summary>
    public double Height => State.Number(nameof(Height));

    /// <summary>The symbol's area.</summary>
    public double Size => State.Number(nameof(Size));

    /// <summary>The item's index.</summary>
    public int Index => State.Whole(nameof(Index));

    /// <summary>The point's x value.</summary>
    public double XValue => State.Number(nameof(XValue));

    /// <summary>The point's y value.</summary>
    public double YValue => State.Number(nameof(YValue));

    /// <summary>The point's z value.</summary>
    public double ZValue => State.Number(nameof(ZValue));

    /// <summary>The fill.</summary>
    public string? Fill => State.Text(nameof(Fill));

    /// <summary>The stroke.</summary>
    public string? Stroke => State.Text(nameof(Stroke));

    /// <summary>The series' name.</summary>
    public string? Name => State.Text(nameof(Name));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A radar's props for its <c>ShapeTemplate</c>.</summary>
public sealed class RadarShapeContext : IContentContext
{
    internal RadarShapeContext(ContentState state) => State = state;

    /// <summary>The chart's center x.</summary>
    public double Cx => State.Number(nameof(Cx));

    /// <summary>The chart's center y.</summary>
    public double Cy => State.Number(nameof(Cy));

    /// <summary>The radar's fill opacity.</summary>
    public double FillOpacity => State.Number(nameof(FillOpacity));

    /// <summary>The fill.</summary>
    public string? Fill => State.Text(nameof(Fill));

    /// <summary>The stroke.</summary>
    public string? Stroke => State.Text(nameof(Stroke));

    /// <summary>The radar's name.</summary>
    public string? Name => State.Text(nameof(Name));

    /// <summary>The polygon's points.</summary>
    public IReadOnlyList<Point> Points => ContentContexts.Points(State.Points);

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A node's props for a treemap's <c>ContentTemplate</c> (Recharts' <c>content</c>).</summary>
public sealed class TreemapNodeContext : IContentContext
{
    internal TreemapNodeContext(ContentState state) => State = state;

    /// <summary>The node's left.</summary>
    public double X => State.Number(nameof(X));

    /// <summary>The node's top.</summary>
    public double Y => State.Number(nameof(Y));

    /// <summary>The node's width.</summary>
    public double Width => State.Number(nameof(Width));

    /// <summary>The node's height.</summary>
    public double Height => State.Number(nameof(Height));

    /// <summary>The node's depth (the root is 0).</summary>
    public int Depth => State.Whole(nameof(Depth));

    /// <summary>The node's index among its siblings.</summary>
    public int Index => State.Whole(nameof(Index));

    /// <summary>The node's value.</summary>
    public double Value => State.Number(nameof(Value));

    /// <summary>Whether the node has children.</summary>
    public bool HasChildren => State.Flag(nameof(HasChildren));

    /// <summary>Whether the node has no children.</summary>
    public bool IsLeaf => State.Flag(nameof(IsLeaf));

    /// <summary>The node's name.</summary>
    public string? Name => State.Text(nameof(Name));

    /// <summary>The fill Recharts' default content gives the node.</summary>
    public string? Fill => State.Text(nameof(Fill));

    /// <summary>The treemap's stroke.</summary>
    public string? Stroke => State.Text(nameof(Stroke));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>An entry of a nest treemap's trail for its <c>NestIndexTemplate</c> (Recharts' <c>nestIndexContent</c>).</summary>
public sealed class NestIndexContext : IContentContext
{
    internal NestIndexContext(ContentState state) => State = state;

    /// <summary>The entry's index in the trail.</summary>
    public int Index => State.Whole(nameof(Index));

    /// <summary>The entry's depth.</summary>
    public int Depth => State.Whole(nameof(Depth));

    /// <summary>The entry's name.</summary>
    public string? Name => State.Text(nameof(Name));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A node's props for a Sankey's <c>NodeTemplate</c> (Recharts' <c>node</c>).</summary>
public sealed class SankeyNodeContext : IContentContext
{
    internal SankeyNodeContext(ContentState state) => State = state;

    /// <summary>The node's left.</summary>
    public double X => State.Number(nameof(X));

    /// <summary>The node's top.</summary>
    public double Y => State.Number(nameof(Y));

    /// <summary>The node's width.</summary>
    public double Width => State.Number(nameof(Width));

    /// <summary>The node's height.</summary>
    public double Height => State.Number(nameof(Height));

    /// <summary>The node's index.</summary>
    public int Index => State.Whole(nameof(Index));

    /// <summary>The node's value.</summary>
    public double Value => State.Number(nameof(Value));

    /// <summary>The node's depth.</summary>
    public int Depth => State.Whole(nameof(Depth));

    /// <summary>The width of the chart's plot.</summary>
    public double ContainerWidth => State.Number(nameof(ContainerWidth));

    /// <summary>The node's name.</summary>
    public string? Name => State.Text(nameof(Name));

    /// <summary>The node's fill.</summary>
    public string? Fill => State.Text(nameof(Fill));

    /// <summary>The node's stroke.</summary>
    public string? Stroke => State.Text(nameof(Stroke));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A link's props for a Sankey's <c>LinkTemplate</c> (Recharts' <c>link</c>).</summary>
public sealed class SankeyLinkContext : IContentContext
{
    internal SankeyLinkContext(ContentState state) => State = state;

    /// <summary>The x where the link leaves its source.</summary>
    public double SourceX => State.Number(nameof(SourceX));

    /// <summary>The y of the link's middle at its source.</summary>
    public double SourceY => State.Number(nameof(SourceY));

    /// <summary>The x of the curve's first control point.</summary>
    public double SourceControlX => State.Number(nameof(SourceControlX));

    /// <summary>The x where the link reaches its target.</summary>
    public double TargetX => State.Number(nameof(TargetX));

    /// <summary>The y of the link's middle at its target.</summary>
    public double TargetY => State.Number(nameof(TargetY));

    /// <summary>The x of the curve's second control point.</summary>
    public double TargetControlX => State.Number(nameof(TargetControlX));

    /// <summary>The link's width.</summary>
    public double LinkWidth => State.Number(nameof(LinkWidth));

    /// <summary>The link's index.</summary>
    public int Index => State.Whole(nameof(Index));

    /// <summary>The link's value.</summary>
    public double Value => State.Number(nameof(Value));

    /// <summary>The source node's name.</summary>
    public string? SourceName => State.Text(nameof(SourceName));

    /// <summary>The target node's name.</summary>
    public string? TargetName => State.Text(nameof(TargetName));

    /// <summary>The link's stroke.</summary>
    public string? Stroke => State.Text(nameof(Stroke));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A reference line's props for its <c>ShapeTemplate</c>.</summary>
public sealed class ReferenceLineContext : IContentContext
{
    internal ReferenceLineContext(ContentState state) => State = state;

    /// <summary>The segment's first x.</summary>
    public double X1 => State.Number(nameof(X1));

    /// <summary>The segment's first y.</summary>
    public double Y1 => State.Number(nameof(Y1));

    /// <summary>The segment's second x.</summary>
    public double X2 => State.Number(nameof(X2));

    /// <summary>The segment's second y.</summary>
    public double Y2 => State.Number(nameof(Y2));

    /// <summary>The stroke width.</summary>
    public double StrokeWidth => State.Number(nameof(StrokeWidth));

    /// <summary>The stroke.</summary>
    public string? Stroke => State.Text(nameof(Stroke));

    /// <summary>The fill.</summary>
    public string? Fill => State.Text(nameof(Fill));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A reference area's props for its <c>ShapeTemplate</c>.</summary>
public sealed class ReferenceAreaContext : IContentContext
{
    internal ReferenceAreaContext(ContentState state) => State = state;

    /// <summary>The area's left.</summary>
    public double X => State.Number(nameof(X));

    /// <summary>The area's top.</summary>
    public double Y => State.Number(nameof(Y));

    /// <summary>The area's width.</summary>
    public double Width => State.Number(nameof(Width));

    /// <summary>The area's height.</summary>
    public double Height => State.Number(nameof(Height));

    /// <summary>The fill.</summary>
    public string? Fill => State.Text(nameof(Fill));

    /// <summary>The stroke.</summary>
    public string? Stroke => State.Text(nameof(Stroke));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A reference dot's props for its <c>ShapeTemplate</c>.</summary>
public sealed class ReferenceDotContext : IContentContext
{
    internal ReferenceDotContext(ContentState state) => State = state;

    /// <summary>The dot's x.</summary>
    public double Cx => State.Number(nameof(Cx));

    /// <summary>The dot's y.</summary>
    public double Cy => State.Number(nameof(Cy));

    /// <summary>The dot's radius.</summary>
    public double R => State.Number(nameof(R));

    /// <summary>The fill.</summary>
    public string? Fill => State.Text(nameof(Fill));

    /// <summary>The stroke.</summary>
    public string? Stroke => State.Text(nameof(Stroke));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A brush traveller's props for the brush's <c>TravellerTemplate</c> (Recharts' <c>traveller</c>).</summary>
public sealed class TravellerContext : IContentContext
{
    internal TravellerContext(ContentState state) => State = state;

    /// <summary>The traveller's left.</summary>
    public double X => State.Number(nameof(X));

    /// <summary>The traveller's top.</summary>
    public double Y => State.Number(nameof(Y));

    /// <summary>The traveller's width.</summary>
    public double Width => State.Number(nameof(Width));

    /// <summary>The traveller's height.</summary>
    public double Height => State.Number(nameof(Height));

    /// <summary>The brush's fill.</summary>
    public string? Fill => State.Text(nameof(Fill));

    /// <summary>The brush's stroke.</summary>
    public string? Stroke => State.Text(nameof(Stroke));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A tooltip's props for its <c>ContentTemplate</c> (Recharts' <c>content</c>), written as HTML: the active item's label and first line.</summary>
public sealed class TooltipContentContext : IContentContext
{
    internal TooltipContentContext(ContentState state) => State = state;

    /// <summary>The x of the point the tooltip is anchored at.</summary>
    public double X => State.Number(nameof(X));

    /// <summary>The y of that point.</summary>
    public double Y => State.Number(nameof(Y));

    /// <summary>The active item's index in the data.</summary>
    public int Index => State.Whole(nameof(Index));

    /// <summary>How many lines Recharts' default content would show.</summary>
    public int Count => State.Whole(nameof(Count));

    /// <summary>The active category's label (Recharts' <c>label</c>).</summary>
    public string? Label => State.Text(nameof(Label));

    /// <summary>The first line's name (Recharts' <c>payload[0].name</c>).</summary>
    public string? Name => State.Text(nameof(Name));

    /// <summary>The first line's value, through the tooltip's format (<c>payload[0].value</c>).</summary>
    public string? Value => State.Text(nameof(Value));

    /// <summary>The first line's color.</summary>
    public string? Color => State.Text(nameof(Color));

    /// <summary>The second line's name (<c>payload[1].name</c>), shown while <see cref="Count"/> is above one.</summary>
    public string? Name2 => State.Text(nameof(Name2));

    /// <summary>The second line's value, through the tooltip's format.</summary>
    public string? Value2 => State.Text(nameof(Value2));

    /// <summary>The second line's color.</summary>
    public string? Color2 => State.Text(nameof(Color2));

    /// <summary>The third line's name (<c>payload[2].name</c>), shown while <see cref="Count"/> is above two.</summary>
    public string? Name3 => State.Text(nameof(Name3));

    /// <summary>The third line's value, through the tooltip's format.</summary>
    public string? Value3 => State.Text(nameof(Value3));

    /// <summary>The third line's color.</summary>
    public string? Color3 => State.Text(nameof(Color3));

    /// <summary>The fourth line's name (<c>payload[3].name</c>), shown while <see cref="Count"/> is above three.</summary>
    public string? Name4 => State.Text(nameof(Name4));

    /// <summary>The fourth line's value, through the tooltip's format.</summary>
    public string? Value4 => State.Text(nameof(Value4));

    /// <summary>The fourth line's color.</summary>
    public string? Color4 => State.Text(nameof(Color4));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A tooltip line's props for the tooltip's <c>ItemTemplate</c>, one per line where its content's <see cref="TooltipItems"/> stands, written as HTML.</summary>
public sealed class TooltipItemContext : IContentContext
{
    internal TooltipItemContext(ContentState state) => State = state;

    /// <summary>The line's index in the payload.</summary>
    public int Index => State.Whole(nameof(Index));

    /// <summary>The line's name (Recharts' <c>item.name</c>).</summary>
    public string? Name => State.Text(nameof(Name));

    /// <summary>The line's value, through the tooltip's format or formatter (<c>item.value</c>).</summary>
    public string? Value => State.Text(nameof(Value));

    /// <summary>The line's color (<c>item.color</c>).</summary>
    public string? Color => State.Text(nameof(Color));

    /// <summary>The active item's data (Recharts' <c>item.payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A legend entry's props for the legend's <c>ContentTemplate</c> (Recharts' <c>content</c>, one per entry), written as HTML.</summary>
public sealed class LegendEntryContext : IContentContext
{
    internal LegendEntryContext(ContentState state) => State = state;

    /// <summary>The entry's index.</summary>
    public int Index => State.Whole(nameof(Index));

    /// <summary>Whether the entry's series is toggled off.</summary>
    public bool Inactive => State.Flag(nameof(Inactive));

    /// <summary>The entry's text (Recharts' <c>entry.value</c>).</summary>
    public string? Value => State.Text(nameof(Value));

    /// <summary>The entry's color.</summary>
    public string? Color => State.Text(nameof(Color));

    /// <summary>The entry's icon (<c>line</c>, <c>rect</c>...).</summary>
    public string? Type => State.Text(nameof(Type));

    /// <summary>The entry's data key.</summary>
    public string? DataKey => State.Text(nameof(DataKey));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>A legend's props for its whole template (<c>Legend.Template</c>, Recharts' <c>content</c> receiving the whole payload).</summary>
public sealed class LegendContext : IContentContext
{
    internal LegendContext(ContentState state) => State = state;

    /// <summary>How many entries the legend lists (Recharts' <c>payload.length</c>).</summary>
    public int Count => State.Whole(nameof(Count));

    /// <summary>The legend's width.</summary>
    public double Width => State.Number(nameof(Width));

    /// <summary>The legend's alignment (Recharts' <c>align</c>).</summary>
    public string? Align => State.Text(nameof(Align));

    /// <summary>The legend's layout: <c>horizontal</c> or <c>vertical</c>.</summary>
    public string? Layout => State.Text(nameof(Layout));

    /// <summary>The item's data (none for a legend): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>The chart's layout for a <c>Customized</c> child's template: its size and its plot's box.</summary>
public sealed class LayoutContext : IContentContext
{
    internal LayoutContext(ContentState state) => State = state;

    /// <summary>The chart's width.</summary>
    public double Width => State.Number(nameof(Width));

    /// <summary>The chart's height.</summary>
    public double Height => State.Number(nameof(Height));

    /// <summary>The plot's left.</summary>
    public double Left => State.Number(nameof(Left));

    /// <summary>The plot's top.</summary>
    public double Top => State.Number(nameof(Top));

    /// <summary>The plot's width.</summary>
    public double PlotWidth => State.Number(nameof(PlotWidth));

    /// <summary>The plot's height.</summary>
    public double PlotHeight => State.Number(nameof(PlotHeight));

    /// <summary>The item's data (Recharts' <c>payload</c>): <c>{Binding Payload[name]}</c>.</summary>
    public ContentPayload Payload => State.Payload;

    ContentState IContentContext.State => State;

    internal ContentState State { get; }

    double IContentContext.Number(string name) => State.Number(name);
}

/// <summary>The context of each slot, made from the values it reads.</summary>
internal static class ContentContexts
{
    public static IContentContext Create(ContentState state) => state.Slot.Name switch
    {
        "Bar" => new BarShapeContext(state),
        "Dot" => new DotContext(state),
        "Label" => new LabelContext(state),
        "Tick" => new TickContext(state),
        "Sector" => new SectorContext(state),
        "Trapezoid" => new TrapezoidContext(state),
        "Scatter" => new ScatterShapeContext(state),
        "Radar" => new RadarShapeContext(state),
        "TreemapNode" => new TreemapNodeContext(state),
        "NestIndex" => new NestIndexContext(state),
        "SankeyNode" => new SankeyNodeContext(state),
        "SankeyLink" => new SankeyLinkContext(state),
        "ReferenceLine" => new ReferenceLineContext(state),
        "ReferenceArea" => new ReferenceAreaContext(state),
        "ReferenceDot" => new ReferenceDotContext(state),
        "Traveller" => new TravellerContext(state),
        "Tooltip" => new TooltipContentContext(state),
        "TooltipItem" => new TooltipItemContext(state),
        "LegendEntry" => new LegendEntryContext(state),
        "Legend" => new LegendContext(state),
        "Layout" => new LayoutContext(state),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state.Slot.Name, "No context for this slot."),
    };

    public static IReadOnlyList<Point> Points(double[] values)
    {
        var points = new Point[values.Length / 2];
        for (var i = 0; i < points.Length; i++)
        {
            points[i] = new Point(values[2 * i], values[(2 * i) + 1]);
        }

        return points;
    }
}
