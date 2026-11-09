using Aioros.Pictor.Specs;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Aioros.Pictor;

/// <summary>
/// What Pictor's shapes for custom content share: SVG's paint props. These elements are the building blocks of a chart's
/// XAML templates (Recharts' <c>Sector</c>, <c>Dot</c>, <c>Rectangle</c>, <c>Symbols</c> and SVG's <c>text</c>): the
/// chart draws them on the desktop and writes them as SVG on the web, they draw nothing by themselves.
/// </summary>
public sealed class ShapePaint
{
    /// <summary>Defines the fill of Pictor's template shapes.</summary>
    public static readonly AttachedProperty<IBrush?> FillProperty = AvaloniaProperty.RegisterAttached<Control, IBrush?>("Fill", typeof(ShapePaint));

    private ShapePaint()
    {
    }

    /// <summary>Defines the stroke of Pictor's template shapes.</summary>
    public static readonly AttachedProperty<IBrush?> StrokeProperty = AvaloniaProperty.RegisterAttached<Control, IBrush?>("Stroke", typeof(ShapePaint));

    /// <summary>Defines the stroke width of Pictor's template shapes (unset when NaN).</summary>
    public static readonly AttachedProperty<double> StrokeWidthProperty = AvaloniaProperty.RegisterAttached<Control, double>("StrokeWidth", typeof(ShapePaint), double.NaN);

    /// <summary>Defines the fill opacity of Pictor's template shapes (unset when NaN).</summary>
    public static readonly AttachedProperty<double> FillOpacityProperty = AvaloniaProperty.RegisterAttached<Control, double>("FillOpacity", typeof(ShapePaint), double.NaN);

    /// <summary>Defines the stroke opacity of Pictor's template shapes (unset when NaN).</summary>
    public static readonly AttachedProperty<double> StrokeOpacityProperty = AvaloniaProperty.RegisterAttached<Control, double>("StrokeOpacity", typeof(ShapePaint), double.NaN);

    /// <summary>Defines the dash pattern of Pictor's template shapes.</summary>
    public static readonly AttachedProperty<string?> StrokeDashArrayProperty = AvaloniaProperty.RegisterAttached<Control, string?>("StrokeDashArray", typeof(ShapePaint));

    /// <summary>The fill opacity of a shape in a template (SVG's <c>fill-opacity</c>, which an Avalonia shape also takes: <c>p:ShapePaint.FillOpacity="0.3"</c>).</summary>
    /// <param name="control">The shape.</param>
    /// <returns>The fill opacity, NaN when unset.</returns>
    public static double GetFillOpacity(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return control.GetValue(FillOpacityProperty);
    }

    /// <summary>Sets the fill opacity of a shape in a template.</summary>
    /// <param name="control">The shape.</param>
    /// <param name="value">The fill opacity.</param>
    public static void SetFillOpacity(Control control, double value)
    {
        ArgumentNullException.ThrowIfNull(control);
        control.SetValue(FillOpacityProperty, value);
    }

    /// <summary>The stroke opacity of a shape in a template (SVG's <c>stroke-opacity</c>).</summary>
    /// <param name="control">The shape.</param>
    /// <returns>The stroke opacity, NaN when unset.</returns>
    public static double GetStrokeOpacity(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return control.GetValue(StrokeOpacityProperty);
    }

    /// <summary>Sets the stroke opacity of a shape in a template.</summary>
    /// <param name="control">The shape.</param>
    /// <param name="value">The stroke opacity.</param>
    public static void SetStrokeOpacity(Control control, double value)
    {
        ArgumentNullException.ThrowIfNull(control);
        control.SetValue(StrokeOpacityProperty, value);
    }
}

/// <summary>SVG's <c>text</c> in a chart's template: a text anchored at a point of its baseline, as Recharts' examples write it.</summary>
public sealed class SvgText : Control
{
    /// <summary>Defines <see cref="X"/>.</summary>
    public static readonly StyledProperty<double> XProperty = AvaloniaProperty.Register<SvgText, double>(nameof(X));

    /// <summary>Defines <see cref="Y"/>.</summary>
    public static readonly StyledProperty<double> YProperty = AvaloniaProperty.Register<SvgText, double>(nameof(Y));

    /// <summary>Defines <see cref="Dx"/>.</summary>
    public static readonly StyledProperty<double> DxProperty = AvaloniaProperty.Register<SvgText, double>(nameof(Dx));

    /// <summary>Defines <see cref="Dy"/>.</summary>
    public static readonly StyledProperty<double> DyProperty = AvaloniaProperty.Register<SvgText, double>(nameof(Dy));

    /// <summary>Defines <see cref="TextAnchor"/>.</summary>
    public static readonly StyledProperty<TextAnchorKind> TextAnchorProperty = AvaloniaProperty.Register<SvgText, TextAnchorKind>(nameof(TextAnchor));

    /// <summary>Defines <see cref="Content"/>.</summary>
    public static readonly StyledProperty<string?> ContentProperty = AvaloniaProperty.Register<SvgText, string?>(nameof(Content));

    /// <summary>Defines <see cref="Value"/>.</summary>
    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<SvgText, double>(nameof(Value), double.NaN);

    /// <summary>Defines <see cref="Format"/>.</summary>
    public static readonly StyledProperty<string?> FormatProperty = AvaloniaProperty.Register<SvgText, string?>(nameof(Format));

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<IBrush?> FillProperty = ShapePaint.FillProperty.AddOwner<SvgText>();

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty = ShapePaint.StrokeProperty.AddOwner<SvgText>();

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double> StrokeWidthProperty = ShapePaint.StrokeWidthProperty.AddOwner<SvgText>();

    /// <summary>Defines <see cref="FillOpacity"/>.</summary>
    public static readonly StyledProperty<double> FillOpacityProperty = ShapePaint.FillOpacityProperty.AddOwner<SvgText>();

    /// <summary>Defines <see cref="StrokeOpacity"/>.</summary>
    public static readonly StyledProperty<double> StrokeOpacityProperty = ShapePaint.StrokeOpacityProperty.AddOwner<SvgText>();

    /// <summary>Defines <see cref="FontSize"/>.</summary>
    public static readonly StyledProperty<double> FontSizeProperty = AvaloniaProperty.Register<SvgText, double>(nameof(FontSize), double.NaN);

    /// <summary>Defines <see cref="FontWeight"/>.</summary>
    public static readonly StyledProperty<string?> FontWeightProperty = AvaloniaProperty.Register<SvgText, string?>(nameof(FontWeight));

    /// <summary>Defines <see cref="Angle"/>.</summary>
    public static readonly StyledProperty<double> AngleProperty = AvaloniaProperty.Register<SvgText, double>(nameof(Angle));

    /// <summary>The x of the text's anchor.</summary>
    public double X { get => GetValue(XProperty); set => SetValue(XProperty, value); }

    /// <summary>The y of the text's baseline.</summary>
    public double Y { get => GetValue(YProperty); set => SetValue(YProperty, value); }

    /// <summary>A shift along x, SVG's <c>dx</c>.</summary>
    public double Dx { get => GetValue(DxProperty); set => SetValue(DxProperty, value); }

    /// <summary>A shift along y, SVG's <c>dy</c>.</summary>
    public double Dy { get => GetValue(DyProperty); set => SetValue(DyProperty, value); }

    /// <summary>Where the text sits on its anchor, SVG's <c>text-anchor</c>.</summary>
    public TextAnchorKind TextAnchor { get => GetValue(TextAnchorProperty); set => SetValue(TextAnchorProperty, value); }

    /// <summary>The text; when unset, <see cref="Value"/> through <see cref="Format"/>.</summary>
    [Content]
    public string? Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>A number written as the text when <see cref="Content"/> is unset: as JavaScript writes it, or through <see cref="Format"/>.</summary>
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>A .NET composite format for <see cref="Value"/> (<c>PV {0}</c>, <c>(Rate {0:0.00%})</c>).</summary>
    public string? Format { get => GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>The fill, black when unset (SVG's default).</summary>
    public IBrush? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The stroke.</summary>
    public IBrush? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The stroke width.</summary>
    public double StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    /// <summary>The fill opacity.</summary>
    public double FillOpacity { get => GetValue(FillOpacityProperty); set => SetValue(FillOpacityProperty, value); }

    /// <summary>The stroke opacity.</summary>
    public double StrokeOpacity { get => GetValue(StrokeOpacityProperty); set => SetValue(StrokeOpacityProperty, value); }

    /// <summary>The size of the text, the chart's when NaN.</summary>
    public double FontSize { get => GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

    /// <summary>The weight, as CSS writes it (<c>bold</c>, <c>600</c>).</summary>
    public string? FontWeight { get => GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }

    /// <summary>A rotation in degrees around the anchor.</summary>
    public double Angle { get => GetValue(AngleProperty); set => SetValue(AngleProperty, value); }
}

/// <summary>Recharts' <c>Sector</c> in a chart's template: a ring's sector from its center, radii and angles (degrees, counterclockwise from three o'clock).</summary>
public sealed class Sector : Control
{
    /// <summary>Defines <see cref="Cx"/>.</summary>
    public static readonly StyledProperty<double> CxProperty = AvaloniaProperty.Register<Sector, double>(nameof(Cx));

    /// <summary>Defines <see cref="Cy"/>.</summary>
    public static readonly StyledProperty<double> CyProperty = AvaloniaProperty.Register<Sector, double>(nameof(Cy));

    /// <summary>Defines <see cref="InnerRadius"/>.</summary>
    public static readonly StyledProperty<double> InnerRadiusProperty = AvaloniaProperty.Register<Sector, double>(nameof(InnerRadius));

    /// <summary>Defines <see cref="OuterRadius"/>.</summary>
    public static readonly StyledProperty<double> OuterRadiusProperty = AvaloniaProperty.Register<Sector, double>(nameof(OuterRadius));

    /// <summary>Defines <see cref="StartAngle"/>.</summary>
    public static readonly StyledProperty<double> StartAngleProperty = AvaloniaProperty.Register<Sector, double>(nameof(StartAngle));

    /// <summary>Defines <see cref="EndAngle"/>.</summary>
    public static readonly StyledProperty<double> EndAngleProperty = AvaloniaProperty.Register<Sector, double>(nameof(EndAngle));

    /// <summary>Defines <see cref="CornerRadius"/>.</summary>
    public static readonly StyledProperty<double> CornerRadiusProperty = AvaloniaProperty.Register<Sector, double>(nameof(CornerRadius));

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<IBrush?> FillProperty = ShapePaint.FillProperty.AddOwner<Sector>();

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty = ShapePaint.StrokeProperty.AddOwner<Sector>();

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double> StrokeWidthProperty = ShapePaint.StrokeWidthProperty.AddOwner<Sector>();

    /// <summary>Defines <see cref="FillOpacity"/>.</summary>
    public static readonly StyledProperty<double> FillOpacityProperty = ShapePaint.FillOpacityProperty.AddOwner<Sector>();

    /// <summary>The center's x.</summary>
    public double Cx { get => GetValue(CxProperty); set => SetValue(CxProperty, value); }

    /// <summary>The center's y.</summary>
    public double Cy { get => GetValue(CyProperty); set => SetValue(CyProperty, value); }

    /// <summary>The inner radius (0 for a pie's slice).</summary>
    public double InnerRadius { get => GetValue(InnerRadiusProperty); set => SetValue(InnerRadiusProperty, value); }

    /// <summary>The outer radius.</summary>
    public double OuterRadius { get => GetValue(OuterRadiusProperty); set => SetValue(OuterRadiusProperty, value); }

    /// <summary>The start angle, in degrees.</summary>
    public double StartAngle { get => GetValue(StartAngleProperty); set => SetValue(StartAngleProperty, value); }

    /// <summary>The end angle, in degrees.</summary>
    public double EndAngle { get => GetValue(EndAngleProperty); set => SetValue(EndAngleProperty, value); }

    /// <summary>The radius of its rounded corners.</summary>
    public double CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>The fill.</summary>
    public IBrush? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The stroke.</summary>
    public IBrush? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The stroke width.</summary>
    public double StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    /// <summary>The fill opacity.</summary>
    public double FillOpacity { get => GetValue(FillOpacityProperty); set => SetValue(FillOpacityProperty, value); }
}

/// <summary>Recharts' <c>Dot</c> in a chart's template: a circle from its center and radius, with Recharts' class.</summary>
public sealed class Dot : Control
{
    /// <summary>Defines <see cref="Cx"/>.</summary>
    public static readonly StyledProperty<double> CxProperty = AvaloniaProperty.Register<Dot, double>(nameof(Cx));

    /// <summary>Defines <see cref="Cy"/>.</summary>
    public static readonly StyledProperty<double> CyProperty = AvaloniaProperty.Register<Dot, double>(nameof(Cy));

    /// <summary>Defines <see cref="R"/>.</summary>
    public static readonly StyledProperty<double> RProperty = AvaloniaProperty.Register<Dot, double>(nameof(R));

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<IBrush?> FillProperty = ShapePaint.FillProperty.AddOwner<Dot>();

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty = ShapePaint.StrokeProperty.AddOwner<Dot>();

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double> StrokeWidthProperty = ShapePaint.StrokeWidthProperty.AddOwner<Dot>();

    /// <summary>Defines <see cref="FillOpacity"/>.</summary>
    public static readonly StyledProperty<double> FillOpacityProperty = ShapePaint.FillOpacityProperty.AddOwner<Dot>();

    /// <summary>The center's x.</summary>
    public double Cx { get => GetValue(CxProperty); set => SetValue(CxProperty, value); }

    /// <summary>The center's y.</summary>
    public double Cy { get => GetValue(CyProperty); set => SetValue(CyProperty, value); }

    /// <summary>The radius.</summary>
    public double R { get => GetValue(RProperty); set => SetValue(RProperty, value); }

    /// <summary>The fill.</summary>
    public IBrush? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The stroke.</summary>
    public IBrush? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The stroke width.</summary>
    public double StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    /// <summary>The fill opacity.</summary>
    public double FillOpacity { get => GetValue(FillOpacityProperty); set => SetValue(FillOpacityProperty, value); }
}

/// <summary>
/// Recharts' <c>Rectangle</c> in a chart's template: its layout box (<c>Canvas.Left</c>, <c>Canvas.Top</c>,
/// <c>Width</c>, <c>Height</c>) with a radius per corner, written as Recharts' rectangle path.
/// </summary>
public sealed class Rectangle : Control
{
    /// <summary>Defines <see cref="Radius"/>.</summary>
    public static readonly StyledProperty<string?> RadiusProperty = AvaloniaProperty.Register<Rectangle, string?>(nameof(Radius));

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<IBrush?> FillProperty = ShapePaint.FillProperty.AddOwner<Rectangle>();

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty = ShapePaint.StrokeProperty.AddOwner<Rectangle>();

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double> StrokeWidthProperty = ShapePaint.StrokeWidthProperty.AddOwner<Rectangle>();

    /// <summary>Defines <see cref="FillOpacity"/>.</summary>
    public static readonly StyledProperty<double> FillOpacityProperty = ShapePaint.FillOpacityProperty.AddOwner<Rectangle>();

    /// <summary>The corner radius: one value, or four (top left, top right, bottom right, bottom left).</summary>
    public string? Radius { get => GetValue(RadiusProperty); set => SetValue(RadiusProperty, value); }

    /// <summary>The fill.</summary>
    public IBrush? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The stroke.</summary>
    public IBrush? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The stroke width.</summary>
    public double StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    /// <summary>The fill opacity.</summary>
    public double FillOpacity { get => GetValue(FillOpacityProperty); set => SetValue(FillOpacityProperty, value); }
}

/// <summary>Recharts' <c>Symbols</c> in a chart's template: d3's symbol of a type and an area at a center.</summary>
public sealed class Symbols : Control
{
    /// <summary>Defines <see cref="Cx"/>.</summary>
    public static readonly StyledProperty<double> CxProperty = AvaloniaProperty.Register<Symbols, double>(nameof(Cx));

    /// <summary>Defines <see cref="Cy"/>.</summary>
    public static readonly StyledProperty<double> CyProperty = AvaloniaProperty.Register<Symbols, double>(nameof(Cy));

    /// <summary>Defines <see cref="Size"/>.</summary>
    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<Symbols, double>(nameof(Size), 64);

    /// <summary>Defines <see cref="Type"/>.</summary>
    public static readonly StyledProperty<ScatterShape> TypeProperty = AvaloniaProperty.Register<Symbols, ScatterShape>(nameof(Type));

    /// <summary>Defines <see cref="Fill"/>.</summary>
    public static readonly StyledProperty<IBrush?> FillProperty = ShapePaint.FillProperty.AddOwner<Symbols>();

    /// <summary>Defines <see cref="Stroke"/>.</summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty = ShapePaint.StrokeProperty.AddOwner<Symbols>();

    /// <summary>Defines <see cref="StrokeWidth"/>.</summary>
    public static readonly StyledProperty<double> StrokeWidthProperty = ShapePaint.StrokeWidthProperty.AddOwner<Symbols>();

    /// <summary>The center's x.</summary>
    public double Cx { get => GetValue(CxProperty); set => SetValue(CxProperty, value); }

    /// <summary>The center's y.</summary>
    public double Cy { get => GetValue(CyProperty); set => SetValue(CyProperty, value); }

    /// <summary>The symbol's area, 64 by default.</summary>
    public double Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>The symbol.</summary>
    public ScatterShape Type { get => GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>The fill.</summary>
    public IBrush? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The stroke.</summary>
    public IBrush? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The stroke width.</summary>
    public double StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }
}

/// <summary>
/// Where a legend's whole template (<c>Legend.Template</c>) shows its entries, as an items presenter in an items
/// control's template: Recharts' default list (<c>DefaultLegendContent</c>), or each entry's <c>ContentTemplate</c> in a
/// list, or, with a horizontal <see cref="Orientation"/>, in a wrapping row aligned as the legend (shadcn/ui's
/// <c>ChartLegendContent</c>: <c>flex flex-wrap justify-center gap-4</c>).
/// </summary>
public sealed class LegendItems : Control
{
    /// <summary>Defines <see cref="Orientation"/>.</summary>
    public static readonly StyledProperty<Avalonia.Layout.Orientation> OrientationProperty = AvaloniaProperty.Register<LegendItems, Avalonia.Layout.Orientation>(nameof(Orientation), Avalonia.Layout.Orientation.Vertical);

    /// <summary>Defines <see cref="Spacing"/>.</summary>
    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<LegendItems, double>(nameof(Spacing));

    /// <summary>Defines <see cref="ItemBasis"/>.</summary>
    public static readonly StyledProperty<double> ItemBasisProperty = AvaloniaProperty.Register<LegendItems, double>(nameof(ItemBasis), double.NaN);

    /// <summary>How the entries' templates follow each other: a list (vertical, the default) or a wrapping row (horizontal).</summary>
    public Avalonia.Layout.Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>The gap between a row's entries and between its lines (CSS's <c>gap</c>).</summary>
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    /// <summary>The share of the row each entry takes, its content centered in it (Tailwind's <c>basis-1/4</c> is 0.25); unset, an entry is as wide as its content.</summary>
    public double ItemBasis { get => GetValue(ItemBasisProperty); set => SetValue(ItemBasisProperty, value); }

    /// <summary>Takes no room where Avalonia lays the template out: the browser and the legend's layout size the entries.</summary>
    /// <param name="availableSize">The size available.</param>
    /// <returns>No size.</returns>
    protected override Size MeasureOverride(Size availableSize) => default;
}

/// <summary>
/// Where a tooltip's <c>ContentTemplate</c> shows its lines: the tooltip's <c>ItemTemplate</c> once per line of the
/// payload, down a column <see cref="Spacing"/> apart, each line stretched across it (shadcn/ui's
/// <c>ChartTooltipContent</c>: <c>payload.map</c> in a <c>grid gap-1.5</c>).
/// </summary>
public sealed class TooltipItems : Control
{
    /// <summary>Defines <see cref="Spacing"/>.</summary>
    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<TooltipItems, double>(nameof(Spacing));

    /// <summary>The gap between the lines (CSS's <c>gap</c>).</summary>
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    /// <summary>Takes no room where Avalonia lays the template out: the browser and the tooltip's layout size the lines.</summary>
    /// <param name="availableSize">The size available.</param>
    /// <returns>No size.</returns>
    protected override Size MeasureOverride(Size availableSize) => default;
}

/// <summary>Recharts' <c>Layer</c> in a chart's template: a canvas written as <c>&lt;g class="recharts-layer"&gt;</c>.</summary>
public sealed class Layer : Canvas
{
    /// <summary>The class it writes on its group, Recharts' layer's.</summary>
    public static string ClassName => "recharts-layer";
}

/// <summary>SVG names for the elements of a chart's template: a class written on an element's SVG element.</summary>
public sealed class SvgElement
{
    /// <summary>Defines the <c>Class</c> attached property.</summary>
    public static readonly AttachedProperty<string?> ClassProperty = AvaloniaProperty.RegisterAttached<SvgElement, Control, string?>("Class");

    /// <summary>Defines the <c>BorderStyle</c> attached property.</summary>
    public static readonly AttachedProperty<string?> BorderStyleProperty = AvaloniaProperty.RegisterAttached<SvgElement, Control, string?>("BorderStyle");

    /// <summary>A border's line in a tooltip's or a legend's HTML (CSS's <c>border-style</c>: <c>dashed</c> or <c>dotted</c>), which Avalonia's <c>Border</c> has no property for; solid when unset.</summary>
    /// <param name="control">The border.</param>
    /// <returns>The style, or null.</returns>
    public static string? GetBorderStyle(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return control.GetValue(BorderStyleProperty);
    }

    /// <summary>Sets a border's line in a tooltip's or a legend's HTML.</summary>
    /// <param name="control">The border.</param>
    /// <param name="value">The style: <c>dashed</c>, <c>dotted</c> or <c>solid</c>.</param>
    public static void SetBorderStyle(Control control, string? value)
    {
        ArgumentNullException.ThrowIfNull(control);
        control.SetValue(BorderStyleProperty, value);
    }

    private SvgElement()
    {
    }

    /// <summary>The class an element of a template writes on its SVG element.</summary>
    /// <param name="control">The element.</param>
    /// <returns>The class, or null.</returns>
    public static string? GetClass(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return control.GetValue(ClassProperty);
    }

    /// <summary>Sets the class an element of a template writes on its SVG element.</summary>
    /// <param name="control">The element.</param>
    /// <param name="value">The class.</param>
    public static void SetClass(Control control, string? value)
    {
        ArgumentNullException.ThrowIfNull(control);
        control.SetValue(ClassProperty, value);
    }
}
