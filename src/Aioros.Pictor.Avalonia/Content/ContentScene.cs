using System.Globalization;
using Aioros.Pictor.Content;
using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using AvaloniaLine = Avalonia.Controls.Shapes.Line;
using AvaloniaRectangle = Avalonia.Controls.Shapes.Rectangle;

namespace Aioros.Pictor;

/// <summary>A prop of a laid-out template's element, as SVG writes it: its numbers, or its text (a color, a path's letters, a transform's operations, a text).</summary>
internal sealed record SceneProp(string Name, double[] Numbers, string? Text)
{
    /// <summary>The format of a text's number (<see cref="SvgText.Format"/>).</summary>
    public string? Format { get; init; }
}

/// <summary>An element of a laid-out template as the SVG it stands for: its element, class, whether it is visible, its props and children.</summary>
internal sealed class SceneNode(ContentElement element, string origin, bool visible)
{
    public ContentElement Element { get; } = element;

    /// <summary>The Avalonia type it comes from, for the reasons the compiler gives.</summary>
    public string Origin { get; } = origin;

    /// <summary>Whether the element's own <c>IsVisible</c> is true (not its ancestors').</summary>
    public bool Visible { get; } = visible;

    public string? Class { get; init; }

    public List<SceneProp> Props { get; } = [];

    public List<SceneNode> Children { get; } = [];

    public SceneProp? Prop(string name) => Props.Find(prop => prop.Name == name);
}

/// <summary>A chart's XAML template that Pictor cannot compile to SVG with holes, with the reason; the web then renders the page with Avalonia.</summary>
public sealed class ContentRefusedException : Exception
{
    /// <summary>A refusal without a reason.</summary>
    public ContentRefusedException()
    {
    }

    /// <summary>A refusal with its reason.</summary>
    /// <param name="message">The reason.</param>
    public ContentRefusedException(string message)
        : base(message)
    {
    }

    /// <summary>A refusal with its reason and its cause.</summary>
    /// <param name="message">The reason.</param>
    /// <param name="innerException">The cause.</param>
    public ContentRefusedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Reads a laid-out template as SVG: panels as groups translated by their offsets, a <c>Viewbox</c> as a nested
/// <c>svg</c>, Avalonia's shapes and text blocks and Pictor's template shapes as their SVG elements, with every number
/// in the coordinates of the group above it.
/// </summary>
internal static class ContentScene
{
    public static SceneNode Read(Control root) => Node(root, default);

    private static SceneNode Node(Control control, Point offset)
    {
        BoundBrushes(control);
        var position = offset + control.Bounds.Position;
        var visible = control.IsVisible;
        return control switch
        {
            Viewbox viewbox => Viewbox(viewbox, position, visible),
            Border border => Border(border, position, visible),
            Panel panel => Group(panel, panel.Children, position, visible),
            AvaloniaPath path => Path(path, position, visible),
            AvaloniaRectangle rectangle => Rect(rectangle, position, visible),
            Ellipse ellipse => Ellipse(ellipse, position, visible),
            AvaloniaLine line => Line(line, position, visible),
            Polygon polygon => Points(ContentElement.Polygon, polygon, polygon.Points, position, visible),
            Polyline polyline => Points(ContentElement.Polyline, polyline, polyline.Points, position, visible),
            TextBlock text => TextBlock(text, position, visible),
            SvgText text => SvgText(text, position, visible),
            Sector sector => Sector(sector, position, visible),
            Dot dot => Dot(dot, position, visible),
            Rectangle rectangle => RechartsRectangle(rectangle, position, visible),
            Symbols symbols => Symbols(symbols, position, visible),
            _ => throw new ContentRefusedException($"{control.GetType().Name} is not an element Pictor writes as SVG (panels, Viewbox, Border, Avalonia's shapes, TextBlock and Pictor's SvgText, Sector, Dot, Rectangle and Symbols are)"),
        };
    }

    private static SceneNode Group(Control control, Controls children, Point position, bool visible)
    {
        var node = new SceneNode(ContentElement.Group, control.GetType().Name, visible) { Class = SvgElement.GetClass(control) ?? (control is Layer ? "recharts-layer" : null) };
        node.Props.Add(Transform(control, position, local: true));
        Opacity(node, control);
        foreach (var child in children)
        {
            node.Children.Add(Node(child, default));
        }

        return node;
    }

    private static SceneNode Border(Border border, Point position, bool visible)
    {
        var node = new SceneNode(ContentElement.Group, nameof(Avalonia.Controls.Border), visible) { Class = SvgElement.GetClass(border) };
        node.Props.Add(Transform(border, position, local: true));
        Opacity(node, border);
        if (border.Background is not null || border.BorderBrush is not null)
        {
            var box = new SceneNode(ContentElement.Rect, nameof(Avalonia.Controls.Border), true);
            var thickness = border.BorderBrush is null ? 0 : border.BorderThickness.Left;
            box.Props.Add(new SceneProp("x", [thickness / 2], null));
            box.Props.Add(new SceneProp("y", [thickness / 2], null));
            box.Props.Add(new SceneProp("width", [border.Bounds.Width - thickness], null));
            box.Props.Add(new SceneProp("height", [border.Bounds.Height - thickness], null));
            box.Props.Add(new SceneProp("rx", [border.CornerRadius.TopLeft], null));
            box.Props.Add(new SceneProp("fill", [], Translucent(border.Background)));
            box.Props.Add(new SceneProp("stroke", [], Translucent(border.BorderBrush)));
            box.Props.Add(new SceneProp("stroke-width", [thickness], null));
            node.Children.Add(box);
        }

        if (border.Child is { } child)
        {
            node.Children.Add(Node(child, default));
        }

        return node;
    }

    private static SceneNode Viewbox(Viewbox viewbox, Point position, bool visible)
    {
        var node = new SceneNode(ContentElement.Svg, nameof(Avalonia.Controls.Viewbox), visible) { Class = SvgElement.GetClass(viewbox) };
        node.Props.Add(new SceneProp("x", [position.X], null));
        node.Props.Add(new SceneProp("y", [position.Y], null));
        node.Props.Add(new SceneProp("width", [viewbox.Bounds.Width], null));
        node.Props.Add(new SceneProp("height", [viewbox.Bounds.Height], null));
        if (viewbox.Child is not { } child)
        {
            return node;
        }

        var size = child.DesiredSize;
        node.Props.Add(new SceneProp("viewBox", [0, 0, double.IsNaN(child.Width) ? size.Width : child.Width, double.IsNaN(child.Height) ? size.Height : child.Height], null));
        node.Props.Add(new SceneProp("preserveAspectRatio", [], viewbox.Stretch switch
        {
            Stretch.Fill => "none",
            Stretch.UniformToFill => "xMidYMid slice",
            _ => "xMidYMid meet",
        }));
        Opacity(node, viewbox);
        if (child is Panel panel && panel.RenderTransform is null && SvgElement.GetClass(panel) is null && panel is not Layer)
        {
            foreach (var inner in panel.Children)
            {
                node.Children.Add(Node(inner, default));
            }
        }
        else
        {
            node.Children.Add(Node(child, -child.Bounds.Position));
        }

        return node;
    }

    private static SceneNode Path(AvaloniaPath path, Point position, bool visible)
    {
        var node = new SceneNode(ContentElement.Path, nameof(AvaloniaPath), visible) { Class = SvgElement.GetClass(path) };
        var stretch = path.RenderedGeometry?.Transform?.Value ?? Matrix.Identity;
        var matrix = stretch * Matrix.CreateTranslation(position.X, position.Y);
        var (letters, numbers) = path.Data is null ? (string.Empty, new List<double>()) : PathData(path.Data, matrix);
        node.Props.Add(new SceneProp(ContentNames.Path, [.. numbers], letters));
        ShapeProps(node, path, position);
        return node;
    }

    /// <summary>A geometry's figures as SVG path commands, every point through a matrix.</summary>
    internal static (string Letters, List<double> Numbers) PathData(Geometry geometry, Matrix matrix)
    {
        var letters = new System.Text.StringBuilder();
        var numbers = new List<double>();
        switch (geometry)
        {
            case PathGeometry path:
                foreach (var figure in path.Figures ?? [])
                {
                    Figure(figure, matrix, letters, numbers);
                }

                break;
            case RectangleGeometry rectangle:
                var rect = rectangle.Rect;
                Add(letters, numbers, 'M', matrix, rect.TopLeft);
                Add(letters, numbers, 'L', matrix, rect.TopRight);
                Add(letters, numbers, 'L', matrix, rect.BottomRight);
                Add(letters, numbers, 'L', matrix, rect.BottomLeft);
                letters.Append('Z');
                break;
            case LineGeometry line:
                Add(letters, numbers, 'M', matrix, line.StartPoint);
                Add(letters, numbers, 'L', matrix, line.EndPoint);
                break;
            case PolylineGeometry polyline:
                for (var i = 0; i < polyline.Points.Count; i++)
                {
                    Add(letters, numbers, i == 0 ? 'M' : 'L', matrix, polyline.Points[i]);
                }

                letters.Append(polyline.IsFilled ? "Z" : string.Empty);
                break;
            case EllipseGeometry ellipse:
                EllipseFigure(ellipse, matrix, letters, numbers);
                break;
            default:
                throw new ContentRefusedException($"Path.Data is a {geometry.GetType().Name}, whose figures Avalonia does not expose: write the geometry as <PathGeometry Figures=\"...\"/> (or use Pictor's shapes)");
        }

        return (letters.ToString(), numbers);
    }

    private static void Figure(PathFigure figure, Matrix matrix, System.Text.StringBuilder letters, List<double> numbers)
    {
        Add(letters, numbers, 'M', matrix, figure.StartPoint);
        foreach (var segment in figure.Segments ?? [])
        {
            switch (segment)
            {
                case LineSegment line:
                    Add(letters, numbers, 'L', matrix, line.Point);
                    break;
                case BezierSegment bezier:
                    letters.Append('C');
                    Point(numbers, matrix, bezier.Point1);
                    Point(numbers, matrix, bezier.Point2);
                    Point(numbers, matrix, bezier.Point3);
                    break;
                case QuadraticBezierSegment quadratic:
                    letters.Append('Q');
                    Point(numbers, matrix, quadratic.Point1);
                    Point(numbers, matrix, quadratic.Point2);
                    break;
                case ArcSegment arc:
                    letters.Append('A');
                    numbers.Add(arc.Size.Width * Math.Abs(matrix.M11));
                    numbers.Add(arc.Size.Height * Math.Abs(matrix.M22));
                    numbers.Add(arc.RotationAngle);
                    numbers.Add(arc.IsLargeArc ? 1 : 0);
                    numbers.Add((arc.SweepDirection == SweepDirection.Clockwise) == (matrix.M11 * matrix.M22 >= 0) ? 1 : 0);
                    Point(numbers, matrix, arc.Point);
                    break;
                case PolyLineSegment poly:
                    foreach (var point in poly.Points)
                    {
                        Add(letters, numbers, 'L', matrix, point);
                    }

                    break;
                default:
                    throw new ContentRefusedException($"a path's {segment.GetType().Name} is not written as SVG");
            }
        }

        if (figure.IsClosed)
        {
            letters.Append('Z');
        }
    }

    private static void EllipseFigure(EllipseGeometry ellipse, Matrix matrix, System.Text.StringBuilder letters, List<double> numbers)
    {
        var (cx, cy, rx, ry) = ellipse.Rect.Width > 0 ? (ellipse.Rect.Center.X, ellipse.Rect.Center.Y, ellipse.Rect.Width / 2, ellipse.Rect.Height / 2) : (ellipse.Center.X, ellipse.Center.Y, ellipse.RadiusX, ellipse.RadiusY);
        Add(letters, numbers, 'M', matrix, new Point(cx - rx, cy));
        foreach (var x in (double[])[cx + rx, cx - rx])
        {
            letters.Append('A');
            numbers.Add(rx * Math.Abs(matrix.M11));
            numbers.Add(ry * Math.Abs(matrix.M22));
            numbers.Add(0);
            numbers.Add(0);
            numbers.Add(1);
            Point(numbers, matrix, new Point(x, cy));
        }

        letters.Append('Z');
    }

    private static void Add(System.Text.StringBuilder letters, List<double> numbers, char letter, Matrix matrix, Point point)
    {
        letters.Append(letter);
        Point(numbers, matrix, point);
    }

    private static void Point(List<double> numbers, Matrix matrix, Point point)
    {
        var transformed = point.Transform(matrix);
        numbers.Add(transformed.X);
        numbers.Add(transformed.Y);
    }

    private static SceneNode Rect(AvaloniaRectangle rectangle, Point position, bool visible)
    {
        var node = new SceneNode(ContentElement.Rect, nameof(AvaloniaRectangle), visible) { Class = SvgElement.GetClass(rectangle) };
        node.Props.Add(new SceneProp("x", [position.X], null));
        node.Props.Add(new SceneProp("y", [position.Y], null));
        node.Props.Add(new SceneProp("width", [rectangle.Bounds.Width], null));
        node.Props.Add(new SceneProp("height", [rectangle.Bounds.Height], null));
        node.Props.Add(new SceneProp("rx", [rectangle.RadiusX], null));
        node.Props.Add(new SceneProp("ry", [rectangle.RadiusY], null));
        ShapeProps(node, rectangle, position);
        return node;
    }

    private static SceneNode Ellipse(Ellipse ellipse, Point position, bool visible)
    {
        var size = ellipse.Bounds.Size;
        var round = Math.Abs(size.Width - size.Height) < 1e-9;
        var node = new SceneNode(round ? ContentElement.Circle : ContentElement.Ellipse, nameof(Avalonia.Controls.Shapes.Ellipse), visible) { Class = SvgElement.GetClass(ellipse) };
        node.Props.Add(new SceneProp("cx", [position.X + (size.Width / 2)], null));
        node.Props.Add(new SceneProp("cy", [position.Y + (size.Height / 2)], null));
        if (round)
        {
            node.Props.Add(new SceneProp("r", [size.Width / 2], null));
        }
        else
        {
            node.Props.Add(new SceneProp("rx", [size.Width / 2], null));
            node.Props.Add(new SceneProp("ry", [size.Height / 2], null));
        }

        ShapeProps(node, ellipse, position);
        return node;
    }

    private static SceneNode Line(AvaloniaLine line, Point position, bool visible)
    {
        var node = new SceneNode(ContentElement.Line, nameof(AvaloniaLine), visible) { Class = SvgElement.GetClass(line) };
        node.Props.Add(new SceneProp("x1", [position.X + line.StartPoint.X], null));
        node.Props.Add(new SceneProp("y1", [position.Y + line.StartPoint.Y], null));
        node.Props.Add(new SceneProp("x2", [position.X + line.EndPoint.X], null));
        node.Props.Add(new SceneProp("y2", [position.Y + line.EndPoint.Y], null));
        ShapeProps(node, line, position);
        return node;
    }

    private static SceneNode Points(ContentElement element, Shape shape, IList<Point> points, Point position, bool visible)
    {
        var node = new SceneNode(element, shape.GetType().Name, visible) { Class = SvgElement.GetClass(shape) };
        var numbers = new double[points.Count * 2];
        for (var i = 0; i < points.Count; i++)
        {
            (numbers[2 * i], numbers[(2 * i) + 1]) = (position.X + points[i].X, position.Y + points[i].Y);
        }

        node.Props.Add(new SceneProp(ContentNames.Points, numbers, null));
        ShapeProps(node, shape, position);
        return node;
    }

    /// <summary>An Avalonia shape's paint (a missing brush, or a stroke of no thickness, Avalonia's default, is SVG's <c>none</c>, as Avalonia draws nothing), opacity and render transform.</summary>
    private static void ShapeProps(SceneNode node, Shape shape, Point position)
    {
        var stroked = shape.Stroke is not null && shape.StrokeThickness > 0;
        node.Props.Add(new SceneProp("fill", [], Paint(shape.Fill)));
        node.Props.Add(new SceneProp("fill-opacity", [Alpha(shape.Fill) * Own(ShapePaint.GetFillOpacity(shape))], null));
        node.Props.Add(new SceneProp("stroke", [], stroked ? Paint(shape.Stroke) : "none"));
        node.Props.Add(new SceneProp("stroke-opacity", [stroked ? Alpha(shape.Stroke) * Own(ShapePaint.GetStrokeOpacity(shape)) : 1], null));
        node.Props.Add(new SceneProp("stroke-width", [stroked ? shape.StrokeThickness : 1], null));
        if (shape.StrokeDashArray is { Count: > 0 } dashes)
        {
            node.Props.Add(new SceneProp("stroke-dasharray", [], string.Join(' ', dashes.Select(dash => (dash * shape.StrokeThickness).ToString(CultureInfo.InvariantCulture)))));
        }

        Opacity(node, shape);
        if (shape.RenderTransform is not null)
        {
            node.Props.Add(Transform(shape, position, local: false));
        }
    }

    private static SceneNode TextBlock(TextBlock text, Point position, bool visible)
    {
        var node = new SceneNode(ContentElement.Text, nameof(Avalonia.Controls.TextBlock), visible) { Class = SvgElement.GetClass(text) };
        var width = text.Bounds.Width;
        var (anchor, x) = (text.HorizontalAlignment, text.TextAlignment) switch
        {
            (HorizontalAlignment.Center, _) or (_, TextAlignment.Center) => ("middle", position.X + (width / 2)),
            (HorizontalAlignment.Right, _) or (_, TextAlignment.Right) => ("end", position.X + width),
            _ => ("start", position.X),
        };
        var lines = text.TextLayout.TextLines;
        var baseline = lines.Count > 0 ? lines[0].Baseline : text.FontSize;
        node.Props.Add(new SceneProp("x", [x], null));
        node.Props.Add(new SceneProp("y", [position.Y + text.Padding.Top + baseline], null));
        node.Props.Add(new SceneProp("text-anchor", [], anchor));
        node.Props.Add(new SceneProp("fill", [], text.Foreground is null ? "#000" : Translucent(text.Foreground)));
        if (text.IsSet(Avalonia.Controls.TextBlock.FontSizeProperty))
        {
            node.Props.Add(new SceneProp("font-size", [text.FontSize], null));
        }

        if (text.FontWeight != Avalonia.Media.FontWeight.Normal)
        {
            node.Props.Add(new SceneProp("font-weight", [], ((int)text.FontWeight).ToString(CultureInfo.InvariantCulture)));
        }

        Opacity(node, text);
        node.Props.Add(new SceneProp(ContentNames.Content, [], text.Text ?? string.Empty));
        return node;
    }

    private static SceneNode SvgText(SvgText text, Point position, bool visible)
    {
        var node = new SceneNode(ContentElement.Text, nameof(Pictor.SvgText), visible) { Class = SvgElement.GetClass(text) };
        var (x, y) = (position.X + text.X, position.Y + text.Y);
        node.Props.Add(new SceneProp("x", [x], null));
        node.Props.Add(new SceneProp("y", [y], null));
        node.Props.Add(new SceneProp("dx", [text.Dx], null));
        node.Props.Add(new SceneProp("dy", [text.Dy], null));
        node.Props.Add(new SceneProp("text-anchor", [], text.TextAnchor switch
        {
            TextAnchorKind.Middle => "middle",
            TextAnchorKind.End => "end",
            _ => "start",
        }));
        PaintProps(node, text.Fill, text.Stroke, text.StrokeWidth, text.FillOpacity, text.StrokeOpacity, "#000");
        if (!double.IsNaN(text.FontSize))
        {
            node.Props.Add(new SceneProp("font-size", [text.FontSize], null));
        }

        if (text.FontWeight is { } weight)
        {
            node.Props.Add(new SceneProp("font-weight", [], weight));
        }

        Opacity(node, text);
        node.Props.Add(new SceneProp(ContentNames.Transform, [text.Angle, x, y], "r"));
        node.Props.Add(text.Content is { } content
            ? new SceneProp(ContentNames.Content, [], content)
            : new SceneProp(ContentNames.Content, [text.Value], null) { Format = text.Format });
        return node;
    }

    private static SceneNode Sector(Sector sector, Point position, bool visible)
    {
        var node = new SceneNode(ContentElement.Sector, nameof(Pictor.Sector), visible) { Class = SvgElement.GetClass(sector) ?? "recharts-sector" };
        PaintProps(node, sector.Fill, sector.Stroke, sector.StrokeWidth, sector.FillOpacity, double.NaN, null);
        node.Props.Add(new SceneProp(ContentNames.Shape, [position.X + sector.Cx, position.Y + sector.Cy, sector.InnerRadius, sector.OuterRadius, sector.StartAngle, sector.EndAngle, sector.CornerRadius], null));
        Opacity(node, sector);
        return node;
    }

    private static SceneNode Dot(Dot dot, Point position, bool visible)
    {
        var node = new SceneNode(ContentElement.Dot, nameof(Pictor.Dot), visible) { Class = SvgElement.GetClass(dot) ?? "recharts-dot" };
        node.Props.Add(new SceneProp("r", [dot.R], null));
        PaintProps(node, dot.Fill, dot.Stroke, dot.StrokeWidth, dot.FillOpacity, double.NaN, null);
        node.Props.Add(new SceneProp("cx", [position.X + dot.Cx], null));
        node.Props.Add(new SceneProp("cy", [position.Y + dot.Cy], null));
        Opacity(node, dot);
        return node;
    }

    private static SceneNode RechartsRectangle(Rectangle rectangle, Point position, bool visible)
    {
        var node = new SceneNode(ContentElement.Rectangle, nameof(Pictor.Rectangle), visible) { Class = SvgElement.GetClass(rectangle) ?? "recharts-rectangle" };
        PaintProps(node, rectangle.Fill, rectangle.Stroke, rectangle.StrokeWidth, rectangle.FillOpacity, double.NaN, null);
        var radius = SpecText.Numbers(rectangle.Radius);
        double Corner(int i) => radius.Length == 0 ? 0 : radius.Length == 1 ? radius[0] : i < radius.Length ? radius[i] : 0;
        node.Props.Add(new SceneProp(ContentNames.Shape, [position.X, position.Y, rectangle.Bounds.Width, rectangle.Bounds.Height, Corner(0), Corner(1), Corner(2), Corner(3)], null));
        Opacity(node, rectangle);
        return node;
    }

    private static SceneNode Symbols(Symbols symbols, Point position, bool visible)
    {
        var node = new SceneNode(ContentElement.Symbols, nameof(Pictor.Symbols), visible) { Class = SvgElement.GetClass(symbols) ?? "recharts-symbols" };
        PaintProps(node, symbols.Fill, symbols.Stroke, symbols.StrokeWidth, double.NaN, double.NaN, null);
        node.Props.Add(new SceneProp(ContentNames.Shape, [position.X + symbols.Cx, position.Y + symbols.Cy, symbols.Size], symbols.Type.ToString().ToLowerInvariant()));
        Opacity(node, symbols);
        return node;
    }

    /// <summary>The paint props of Pictor's shapes, written only when set (as Recharts writes the props it is given); a text's fill is black when unset.</summary>
    private static void PaintProps(SceneNode node, IBrush? fill, IBrush? stroke, double strokeWidth, double fillOpacity, double strokeOpacity, string? defaultFill)
    {
        if ((fill is null ? defaultFill : Translucent(fill)) is { } color)
        {
            node.Props.Add(new SceneProp("fill", [], color));
        }

        if (stroke is not null)
        {
            node.Props.Add(new SceneProp("stroke", [], Translucent(stroke)));
        }

        if (!double.IsNaN(strokeWidth))
        {
            node.Props.Add(new SceneProp("stroke-width", [strokeWidth], null));
        }

        fillOpacity = double.IsNaN(fillOpacity) && Alpha(fill) < 1 ? Alpha(fill) : fillOpacity;
        if (!double.IsNaN(fillOpacity))
        {
            node.Props.Add(new SceneProp("fill-opacity", [fillOpacity], null));
        }

        strokeOpacity = double.IsNaN(strokeOpacity) && stroke is not null && Alpha(stroke) < 1 ? Alpha(stroke) : strokeOpacity;
        if (!double.IsNaN(strokeOpacity))
        {
            node.Props.Add(new SceneProp("stroke-opacity", [strokeOpacity], null));
        }
    }

    internal static void Opacity(SceneNode node, Visual visual) => node.Props.Add(new SceneProp("opacity", [visual.Opacity], null));

    /// <summary>
    /// An element's transform: a group's translation to its offset (<paramref name="local"/>), then its render transform
    /// around its origin (Avalonia's <c>RenderTransformOrigin</c>), as SVG's operations.
    /// </summary>
    private static SceneProp Transform(Control control, Point position, bool local)
    {
        var operations = new System.Text.StringBuilder();
        var numbers = new List<double>();
        if (local)
        {
            operations.Append('t');
            numbers.Add(position.X);
            numbers.Add(position.Y);
        }

        if (control.RenderTransform is { } transform)
        {
            var origin = control.RenderTransformOrigin.ToPixels(control.Bounds.Size);
            var center = local ? origin : position + origin;
            Operations(transform, center, operations, numbers);
        }

        return new SceneProp(ContentNames.Transform, [.. numbers], operations.ToString());
    }

    private static void Operations(ITransform transform, Point center, System.Text.StringBuilder operations, List<double> numbers)
    {
        switch (transform)
        {
            case RotateTransform rotate:
                operations.Append('r');
                numbers.AddRange([rotate.Angle, center.X + rotate.CenterX, center.Y + rotate.CenterY]);
                break;
            case TranslateTransform translate:
                operations.Append('t');
                numbers.AddRange([translate.X, translate.Y]);
                break;
            case TransformGroup group:
                foreach (var child in group.Children.Reverse())
                {
                    Operations(child, center, operations, numbers);
                }

                break;
            default:
                var m = Matrix.CreateTranslation(-center.X, -center.Y) * transform.Value * Matrix.CreateTranslation(center.X, center.Y);
                operations.Append('m');
                numbers.AddRange([m.M11, m.M12, m.M21, m.M22, m.M31, m.M32]);
                break;
        }
    }

    /// <summary>
    /// Refuses an element whose brush is bound and gives no brush: a brush bound to a data member of <c>Payload</c> (which
    /// the probes fill with numbers) or through a converter that gives none would otherwise compile to a constant
    /// <c>none</c> for every item. A color comes from a color member of the context (<c>Fill</c>, <c>Stroke</c>).
    /// </summary>
    internal static void BoundBrushes(Control control)
    {
        foreach (var property in AvaloniaPropertyRegistry.Instance.GetRegistered(control))
        {
            if (property.PropertyType == typeof(IBrush) && control.GetValue(property) is null && Avalonia.Data.BindingOperations.GetBindingExpressionBase(control, property) is not null)
            {
                throw new ContentRefusedException($"the {property.Name} of the template's {control.GetType().Name} is bound and gives no brush (a member of Payload is read as a number when the template is compiled; bind a color member of the context, such as Fill or Stroke)");
            }
        }
    }

    /// <summary>A brush as a color with its translucency, where no separate opacity carries it: <c>#rrggbb</c> when opaque, <c>rgba()</c> otherwise.</summary>
    public static string? Translucent(IBrush? brush) => brush is ISolidColorBrush solid ? Color(solid.Color, Alpha(solid)) : Paint(brush);

    /// <summary>A brush as SVG paints it: a solid color as <c>#rrggbb</c> (its translucency is the paint's opacity, <see cref="Alpha"/>), none for no brush; other brushes are refused.</summary>
    public static string? Paint(IBrush? brush) => brush switch
    {
        null => "none",
        ISolidColorBrush solid => Color(solid.Color, 1),
        _ => throw new ContentRefusedException($"a {brush.GetType().Name} is not written as SVG in a template (solid colors are)"),
    };

    /// <summary>An opacity set on a shape (<see cref="ShapePaint.FillOpacityProperty"/>), or 1.</summary>
    private static double Own(double opacity) => double.IsNaN(opacity) ? 1 : opacity;

    /// <summary>A brush's translucency, SVG's <c>fill-opacity</c> or <c>stroke-opacity</c>: its color's alpha times its opacity.</summary>
    public static double Alpha(IBrush? brush) => brush is ISolidColorBrush solid ? Math.Round(solid.Color.A / 255.0 * solid.Opacity, 4) : 1;

    public static string Color(Color color, double opacity) => opacity >= 1
        ? string.Create(CultureInfo.InvariantCulture, $"#{color.R:x2}{color.G:x2}{color.B:x2}")
        : string.Create(CultureInfo.InvariantCulture, $"rgba({color.R},{color.G},{color.B},{opacity})");

}
