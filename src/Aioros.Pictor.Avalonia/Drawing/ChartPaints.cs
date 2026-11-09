using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Aioros.Pictor.Drawing;

/// <summary>The nodes a refused template's instance gives for an item, laid out by Avalonia with the item's context.</summary>
internal delegate System.Collections.Immutable.ImmutableArray<Specs.ContentNode> ContentResolver(Specs.ContentSpec content, in Content.ContentItem item);

/// <summary>
/// The brushes, pens and texts of one chart, created once per color, stroke and text and reused on every frame: a
/// chart's colors are few and its texts change only with its layout.
/// </summary>
internal sealed class ChartPaints(Typeface typeface, double fontSize)
{
    private readonly Dictionary<(string Color, double Opacity), IImmutableBrush?> _brushes = [];
    private System.Collections.Immutable.ImmutableArray<Specs.GradientSpec> _gradients = [];
    private readonly Dictionary<(string Color, double Width, string? Dash, double Opacity), IPen?> _pens = [];
    private readonly Dictionary<(string Text, double Size, string Color), FormattedText> _texts = [];
    private readonly Dictionary<(string Text, double Size, string Color, string? Family, string? Weight), FormattedText> _styledTexts = [];

    public Typeface Typeface { get; } = typeface;

    public double FontSize { get; } = fontSize;

    /// <summary>The theme of the chart's defaults that the canvas draws itself (the tooltip's box, a nest treemap's index).</summary>
    public Theming.ChartTheme Theme { get; init; } = Theming.ChartTheme.Light;

    /// <summary>The clips pushed while the chart is drawn.</summary>
    public ClipStack Clips { get; } = new();

    /// <summary>What a refused template draws for an item: the nodes Avalonia lays out from it with the item's context.</summary>
    public ContentResolver? Resolve { get; init; }

    public IBrush? Brush(string? color, double opacity = double.NaN)
    {
        if (color is null || color == "none")
        {
            return null;
        }

        var key = (color, double.IsNaN(opacity) ? 1 : opacity);
        if (!_brushes.TryGetValue(key, out var brush))
        {
            brush = color.StartsWith("url(#", StringComparison.Ordinal) ? Gradient(color[5..^1], key.Item2)
                : Color.TryParse(color, out var parsed) ? new ImmutableSolidColorBrush(parsed, key.Item2) : null;
            _brushes[key] = brush;
        }

        return brush;
    }

    /// <summary>The gradients the chart's paints may name; the brushes made from earlier ones are dropped when they change.</summary>
    public void Define(System.Collections.Immutable.ImmutableArray<Specs.GradientSpec> gradients)
    {
        if (gradients.IsDefault || gradients.SequenceEqual(_gradients))
        {
            return;
        }

        _gradients = gradients;
        foreach (var key in _brushes.Keys.Where(key => key.Color.StartsWith("url(#", StringComparison.Ordinal)).ToList())
        {
            _brushes.Remove(key);
        }

        foreach (var key in _pens.Keys.Where(key => key.Color.StartsWith("url(#", StringComparison.Ordinal)).ToList())
        {
            _pens.Remove(key);
        }
    }

    /// <summary>A gradient the paints name, as SVG paints it: relative to the shape's box or in the chart's pixels, the stops' opacities times the paint's.</summary>
    private ImmutableLinearGradientBrush? Gradient(string id, double opacity)
    {
        var gradient = _gradients.FirstOrDefault(known => known.Id == id);
        if (gradient is null)
        {
            return null;
        }

        var unit = gradient.UserSpace ? Avalonia.RelativeUnit.Absolute : Avalonia.RelativeUnit.Relative;
        var stops = gradient.Stops.Select(stop => Color.TryParse(stop.Color, out var parsed)
            ? new ImmutableGradientStop(stop.Offset, Color.FromArgb((byte)Math.Round(parsed.A * Math.Clamp(stop.Opacity, 0, 1)), parsed.R, parsed.G, parsed.B))
            : new ImmutableGradientStop(stop.Offset, Colors.Transparent)).ToList();
        return new ImmutableLinearGradientBrush(
            stops,
            opacity,
            startPoint: new Avalonia.RelativePoint(gradient.X1, gradient.Y1, unit),
            endPoint: new Avalonia.RelativePoint(gradient.X2, gradient.Y2, unit));
    }

    /// <summary>A shape's pen: its stroke, width, dashes and opacity, and its ends, corners and miter limit.</summary>
    public IPen? Pen(in Painting.Paint paint)
    {
        if (paint.LineCap is null && paint.LineJoin is null && double.IsNaN(paint.MiterLimit))
        {
            return Pen(paint.Stroke, paint.StrokeWidth, paint.Dash, paint.StrokeOpacity);
        }

        if (Pen(paint.Stroke, paint.StrokeWidth, paint.Dash, paint.StrokeOpacity) is not ImmutablePen plain)
        {
            return null;
        }

        return new ImmutablePen(plain.Brush as IImmutableBrush, plain.Thickness, plain.DashStyle as ImmutableDashStyle, Cap(paint.LineCap), Join(paint.LineJoin), double.IsNaN(paint.MiterLimit) ? 10 : paint.MiterLimit);
    }

    public IPen? Pen(string? color, double width, string? dash, double opacity = double.NaN)
    {
        if (color is null || color == "none")
        {
            return null;
        }

        var thickness = double.IsNaN(width) ? 1 : width;
        var key = (color, thickness, dash, double.IsNaN(opacity) ? 1 : opacity);
        if (!_pens.TryGetValue(key, out var pen))
        {
            pen = Brush(color, key.Item4) is IImmutableBrush brush ? new ImmutablePen(brush, thickness, Dashes(dash, thickness)) : null;
            _pens[key] = pen;
        }

        return pen;
    }

    private static PenLineCap Cap(string? cap) => cap switch
    {
        "round" => PenLineCap.Round,
        "square" => PenLineCap.Square,
        _ => PenLineCap.Flat,
    };

    private static PenLineJoin Join(string? join) => join switch
    {
        "round" => PenLineJoin.Round,
        "bevel" => PenLineJoin.Bevel,
        _ => PenLineJoin.Miter,
    };

    public FormattedText Text(string text, double size, string color)
    {
        var key = (text, size, color);
        if (!_texts.TryGetValue(key, out var formatted))
        {
            formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface, size, Brush(color));
            _texts[key] = formatted;
        }

        return formatted;
    }

    /// <summary>A text in a font family and weight of its own (CSS's names: <c>bold</c>, <c>normal</c>, <c>700</c>); the chart's typeface where they are null.</summary>
    public FormattedText Text(string text, double size, string color, string? family, string? weight, string? style = null)
    {
        if (family is null && weight is null && style is null)
        {
            return Text(text, size, color);
        }

        var key = (text, size, color, family, weight + "/" + style);
        if (!_styledTexts.TryGetValue(key, out var formatted))
        {
            var styled = new Typeface(family is null ? Typeface.FontFamily : new FontFamily(family), style is "italic" ? FontStyle.Italic : style is "oblique" ? FontStyle.Oblique : FontStyle.Normal, Weight(weight));
            formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, styled, size, Brush(color));
            _styledTexts[key] = formatted;
        }

        return formatted;
    }

    /// <summary>A CSS font size in pixels: <c>px</c> or a bare number as it is, <c>rem</c> of the browser's 16 px, <c>em</c> and <c>%</c> of the chart's size; the chart's size when it cannot be read.</summary>
    public static double FontPixels(string? size, double chartSize)
    {
        if (string.IsNullOrWhiteSpace(size))
        {
            return chartSize;
        }

        var text = size.Trim();
        var (number, scale) = text switch
        {
            _ when text.EndsWith("rem", StringComparison.Ordinal) => (text[..^3], 16.0),
            _ when text.EndsWith("em", StringComparison.Ordinal) => (text[..^2], chartSize),
            _ when text.EndsWith("px", StringComparison.Ordinal) => (text[..^2], 1.0),
            _ when text.EndsWith('%') => (text[..^1], chartSize / 100),
            _ => (text, 1.0),
        };
        return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value * scale : chartSize;
    }

    private static FontWeight Weight(string? weight) => weight switch
    {
        null or "normal" => FontWeight.Normal,
        "bold" or "bolder" => FontWeight.Bold,
        "lighter" => FontWeight.Light,
        _ => int.TryParse(weight, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? (FontWeight)value : FontWeight.Normal,
    };

    private static ImmutableDashStyle? Dashes(string? dash, double thickness)
    {
        if (string.IsNullOrWhiteSpace(dash))
        {
            return null;
        }

        var values = dash.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => double.Parse(part, NumberStyles.Float, CultureInfo.InvariantCulture) / thickness)
            .ToArray();
        return new ImmutableDashStyle(values, 0);
    }
}
