using Aioros.Pictor.Data;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Text;

namespace Aioros.Pictor.Painting;

/// <summary>The box a polar label is placed in (Recharts' polar <c>viewBox</c>): the center, the radii and the angles of a sector.</summary>
internal readonly record struct PolarBox(double Cx, double Cy, double InnerRadius, double OuterRadius, double StartAngle, double EndAngle);

/// <summary>A label list's custom content for one label: its template's buffers, the place of its template among the chart's, the item's index, value and series' name.</summary>
internal readonly record struct LabelContent(Content.ContentValues Values, int Template, int Index, double Value, string? Name);

/// <summary>
/// Recharts' <c>LabelList</c> labels: each list of a series in order (the <c>label</c> prop's, then every child), each
/// label placed by <c>getAttrsOfCartesianLabel</c> or <c>getAttrsOfPolarLabel</c> (or along an arc), written by
/// <c>Text</c> with the label's size, weight, stroke and angle, and wrapped to its shape's width where Recharts gives one.
/// </summary>
internal static class LabelPainter
{
    /// <summary>The label lists of an element: the first one, then the others.</summary>
    public static LabelLists Lists(LabelSpec? first, System.Collections.Immutable.ImmutableArray<LabelSpec> others) => new(first, others);

    /// <summary>A label's text: the number at the key (formatted), or the text there; null when the item has neither.</summary>
    public static string? Text(ChartData data, string key, int index, string? format)
    {
        var number = data.Numbers(key)[index];
        return double.IsNaN(number) ? data.Texts(key)[index] : SeriesPainter.Value(number, format);
    }

    /// <summary>
    /// A label of a cartesian box; <paramref name="wrap"/> is the width its text is wrapped to, when Recharts gives one,
    /// and <paramref name="angle"/> the rotation its shape spreads onto it when the label has none (a radar point's angle).
    /// </summary>
    public static void Cartesian<TCanvas>(ref TCanvas canvas, LabelSpec label, string text, (double X, double Y, double Width, double Height) box, double fontSize, string fill, string? stroke = null, double? wrap = null, double angle = 0, LabelContent? content = null)
        where TCanvas : struct, IChartCanvas
    {
        if (content is { } custom)
        {
            canvas.Content(custom.Values.Spec, ContentProps.Label(custom, label, text, box, default));
            return;
        }

        var (x, y, width, height) = box;
        double lx, ly, dy;
        TextAnchor anchor;
        if (label.PositionX is { } px && label.PositionY is { } py)
        {
            (lx, ly, anchor, dy) = (x + Layout.Polar.Percent(px, width, 0), y + Layout.Polar.Percent(py, height, 0), TextAnchor.End, LabelPlacement.End);
        }
        else
        {
            var position = label.Position is LabelPosition.CenterTop or LabelPosition.CenterBottom or null ? LabelPosition.Center : label.Position.Value;
            (lx, ly, anchor, dy) = LabelPlacement.Of(position, x, y, width, height, label.Offset);
        }

        if (wrap is { } lineWidth)
        {
            text = TextLines.Wrap(text, lineWidth, fontSize, label.BreakAll);
        }

        var paint = Paint(label, anchor, Lines(dy, text), fontSize, fill, stroke);
        canvas.Text(lx, ly, text, label.Angle == 0 && angle != 0 ? paint with { Angle = angle } : paint, "recharts-text recharts-label"u8);
    }

    /// <summary>A label of a polar sector: along its arc at <c>insideStart</c>, <c>insideEnd</c> and <c>end</c>, otherwise placed as <c>getAttrsOfPolarLabel</c> places it.</summary>
    public static void Polar<TCanvas>(ref TCanvas canvas, LabelSpec label, string text, in PolarBox box, double fontSize, string fill, string? stroke = null, LabelContent? content = null)
        where TCanvas : struct, IChartCanvas
    {
        var position = label.PositionX is not null && label.PositionY is not null ? null : label.Position;
        if (content is { } custom)
        {
            PolarContent(ref canvas, label, text, box, position, custom);
            return;
        }

        if (position is LabelPosition.InsideStart or LabelPosition.InsideEnd or LabelPosition.End)
        {
            var delta = Math.Sign(box.EndAngle - box.StartAngle) * Math.Min(Math.Abs(box.EndAngle - box.StartAngle), 360);
            var sign = delta >= 0 ? 1 : -1;
            var angle = position switch
            {
                LabelPosition.InsideStart => box.StartAngle + (sign * label.Offset),
                LabelPosition.InsideEnd => box.EndAngle - (sign * label.Offset),
                _ => box.EndAngle + (sign * label.Offset),
            };
            var direction = position == LabelPosition.InsideEnd ? !label.ClockWise : label.ClockWise;
            direction = delta <= 0 ? direction : !direction;
            var paint = Paint(label, TextAnchor.Middle, LabelPlacement.Middle, fontSize, fill, stroke);
            canvas.ArcText(new ArcPath(box.Cx, box.Cy, (box.InnerRadius + box.OuterRadius) / 2, angle, direction), text, paint, "recharts-radial-bar-label"u8);
            return;
        }

        var middle = (box.StartAngle + box.EndAngle) / 2;
        var (x, y, anchor, dy) = position switch
        {
            LabelPosition.Outside => Outside(box, label.Offset, middle),
            LabelPosition.Center => (box.Cx, box.Cy, TextAnchor.Middle, LabelPlacement.Middle),
            LabelPosition.CenterTop => (box.Cx, box.Cy, TextAnchor.Middle, LabelPlacement.Start),
            LabelPosition.CenterBottom => (box.Cx, box.Cy, TextAnchor.Middle, LabelPlacement.End),
            _ => Within(box, middle),
        };
        canvas.Text(x, y, text, Paint(label, anchor, dy, fontSize, fill, stroke), "recharts-text recharts-label"u8);
    }

    /// <summary>A polar label's custom content, at the point where its default label sits, with its sector.</summary>
    private static void PolarContent<TCanvas>(ref TCanvas canvas, LabelSpec label, string text, in PolarBox box, LabelPosition? position, in LabelContent content)
        where TCanvas : struct, IChartCanvas
    {
        var middle = (box.StartAngle + box.EndAngle) / 2;
        var at = position == LabelPosition.Outside ? Outside(box, label.Offset, middle) : Within(box, middle);
        canvas.Content(content.Values.Spec, ContentProps.Label(content, label, text, (at.X, at.Y, 0, 0), box));
    }

    private static (double X, double Y, TextAnchor Anchor, double Dy) Outside(in PolarBox box, double offset, double angle)
    {
        var (x, y) = Layout.Polar.Point(box.Cx, box.Cy, box.OuterRadius + offset, angle);
        return (x, y, x >= box.Cx ? TextAnchor.Start : TextAnchor.End, LabelPlacement.Middle);
    }

    private static (double X, double Y, TextAnchor Anchor, double Dy) Within(in PolarBox box, double angle)
    {
        var (x, y) = Layout.Polar.Point(box.Cx, box.Cy, (box.InnerRadius + box.OuterRadius) / 2, angle);
        return (x, y, TextAnchor.Middle, LabelPlacement.Middle);
    }

    /// <summary>The first line's shift of a text of several lines, as <c>Text</c> computes it from its vertical anchor.</summary>
    private static double Lines(double dy, string text)
    {
        var lines = text.AsSpan().Count('\n');
        if (lines == 0)
        {
            return dy;
        }

        return dy switch
        {
            LabelPlacement.Start => dy,
            LabelPlacement.Middle => (lines / 2.0 * -1) + dy,
            _ => -lines,
        };
    }

    private static TextPaint Paint(LabelSpec label, TextAnchor anchor, double dy, double fontSize, string fill, string? stroke) =>
        new(anchor, dy, label.Fill ?? fill, label.FontSize ?? fontSize, label.Angle)
        {
            Stroke = label.Stroke ?? stroke,
            StrokeWidth = label.StrokeWidth ?? double.NaN,
            FontWeight = label.FontWeight,
            OwnSize = label.FontSize is not null,
        };
}

/// <summary>An element's label lists: the first, then the others, enumerated without allocation.</summary>
internal readonly struct LabelLists(LabelSpec? first, System.Collections.Immutable.ImmutableArray<LabelSpec> others)
{
    public Enumerator GetEnumerator() => new(first, others);

    public struct Enumerator(LabelSpec? first, System.Collections.Immutable.ImmutableArray<LabelSpec> others)
    {
        private int _index = first is null ? -1 : -2;

        public readonly LabelSpec Current => _index < 0 ? first! : others[_index];

        public bool MoveNext()
        {
            _index++;
            return _index == -1 || (!others.IsDefault && _index < others.Length);
        }
    }
}
