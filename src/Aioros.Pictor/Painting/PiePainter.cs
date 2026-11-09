using Aioros.Pictor.Layout;
using Aioros.Pictor.Motion;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>Paints a <c>Pie</c> as Recharts renders it statically: its sectors, the active one, its labels and label lines.</summary>
internal static class PiePainter
{
    public static void Pie<TCanvas>(ref TCanvas canvas, PieLayout pie, ChartSpec chart, int offset)
        where TCanvas : struct, IChartCanvas
    {
        if (pie.Sectors.Length == 0)
        {
            return;
        }

        var timing = Animations.Of(pie);
        var animated = timing is not null;
        if (timing is { } value)
        {
            canvas.MorphLayer(new MorphLayer(MorphKind.Sector, 0, false, value, pie.Data.Key()));
            Focusable(ref canvas, pie.Spec.RootTabIndex);
            canvas.OpenMotion(ClassNames.With("recharts-layer recharts-pie"u8, pie.Spec.ClassName), value);
            PieArcs.Write(ref canvas, pie, chart, offset);
        }
        else
        {
            Focusable(ref canvas, pie.Spec.RootTabIndex);
            canvas.Open(ClassNames.With("recharts-layer recharts-pie"u8, pie.Spec.ClassName));
        }

        for (var i = 0; i < pie.Sectors.Length; i++)
        {
            var shape = pie.Sectors[i].Shape;
            if ((shape.StartAngle != 0 || shape.EndAngle != 0 || pie.Sectors.Length == 1) && !Toggles.Concealed(chart, offset + i))
            {
                SectorLayer(ref canvas, pie, chart, offset, i, animated);
            }
        }

        if (pie.Spec.Label)
        {
            Labels(ref canvas, pie, chart, offset);
        }

        for (var list = 0; list < pie.Spec.LabelLists.Length; list++)
        {
            LabelList(ref canvas, pie, chart, offset, pie.Spec.LabelLists[list], ContentProps.Template(pie.Spec.Order, 4 + list));
        }

        canvas.Close();
    }

    private static void SectorLayer<TCanvas>(ref TCanvas canvas, PieLayout pie, ChartSpec chart, int offset, int i, bool animated)
        where TCanvas : struct, IChartCanvas
    {
        var marked = Toggles.Open(ref canvas, chart, offset + i);
        if (animated)
        {
            canvas.Animate(new Step(StepKind.Hold));
        }

        canvas.TabIndex(-1);
        canvas.Open("recharts-layer recharts-pie-sector"u8);
        if (IsActive(pie.Spec, i))
        {
            Active(ref canvas, pie, i);
        }
        else if (ContentProps.Of(pie.Spec.ShapeContent, Content.ContentSlots.Sector, pie.Data) is { } content)
        {
            canvas.Content(content.Spec, ContentProps.Sector(content, pie, i, ContentProps.Template(pie.Spec.Order, 0)));
        }
        else
        {
            var inactive = HasActive(pie.Spec) ? pie.Spec.InactiveShape : null;
            Sector(ref canvas, Shaped(pie.Sectors[i].Shape, inactive), pie.Items[i], pie.Spec, inactive, animated ? Entry(pie.Spec) : null);
        }

        canvas.Close();
        Close(ref canvas, marked);
    }

    /// <summary>A <c>LabelList</c> of a pie: a polar label per sector, in the sector's fill and the pie's stroke unless the list sets its own.</summary>
    private static void LabelList<TCanvas>(ref TCanvas canvas, PieLayout pie, ChartSpec chart, int offset, LabelSpec label, int template)
        where TCanvas : struct, IChartCanvas
    {
        var content = ContentProps.Of(label.Content, Content.ContentSlots.Label, pie.Data);
        canvas.Open("recharts-layer recharts-label-list"u8);
        for (var i = 0; i < pie.Sectors.Length; i++)
        {
            var text = label.DataKey is { } key ? LabelPainter.Text(pie.Data, key, i, label.Format) : SeriesPainter.Value(pie.Items[i].Value, label.Format);
            if (Toggles.Concealed(chart, offset + i) || string.IsNullOrEmpty(text))
            {
                continue;
            }

            var marked = Toggles.Open(ref canvas, chart, offset + i);
            var shape = pie.Sectors[i].Shape;
            var box = new PolarBox(shape.Cx, shape.Cy, shape.InnerRadius, shape.OuterRadius, shape.StartAngle, shape.EndAngle);
            LabelPainter.Polar(ref canvas, label, text, box, chart.FontSize, pie.Items[i].Fill, pie.Items[i].Stroke, ContentProps.Label(content, template, i, pie.Items[i].Value, pie.Items[i].Name));
            Close(ref canvas, marked);
        }

        canvas.Close();
    }

    /// <summary>Whether a sector is drawn active by the pie's <c>activeIndex</c> (a number or an array).</summary>
    public static bool IsActive(PieSpec spec, int index) => spec.ActiveIndex == index || (!spec.ActiveIndexes.IsDefaultOrEmpty && spec.ActiveIndexes.Contains(index));

    /// <summary>Whether the pie has an <c>activeIndex</c>, which hovering does not change.</summary>
    public static bool HasActive(PieSpec spec) => spec.ActiveIndex is not null || !spec.ActiveIndexes.IsDefaultOrEmpty;

    /// <summary>A sector drawn active: Recharts' active shape props merged over the sector's own, in its active layer.</summary>
    public static void Active<TCanvas>(ref TCanvas canvas, PieLayout pie, int index)
        where TCanvas : struct, IChartCanvas
    {
        var active = pie.Spec.ActiveShape;
        canvas.Open("recharts-layer recharts-active-shape"u8);
        if (ContentProps.Of(pie.Spec.ActiveShapeContent, Content.ContentSlots.Sector, pie.Data) is { } content)
        {
            canvas.Content(content.Spec, ContentProps.Sector(content, pie, index, ContentProps.Template(pie.Spec.Order, 1), state: 1));
        }
        else
        {
            Sector(ref canvas, Shaped(pie.Sectors[index].Shape, active), pie.Items[index], pie.Spec, active, Animations.Of(pie) is null ? null : Entry(pie.Spec));
        }

        canvas.Close();
    }

    /// <summary>A sector's geometry with a shape object's radii and corner radius merged over it.</summary>
    private static SectorShape Shaped(SectorShape shape, SectorShapeSpec? option)
    {
        if (option is null)
        {
            return shape;
        }

        shape = shape with { InnerRadius = option.InnerRadius ?? shape.InnerRadius, OuterRadius = option.OuterRadius ?? shape.OuterRadius };
        return option.CornerRadius is { } corner ? shape with { CornerRadius = Layout.Polar.Percent(corner, shape.OuterRadius - shape.InnerRadius, 0, validate: true) } : shape;
    }

    /// <summary>
    /// Where Recharts' update sweeps a sector the pie did not have from: after the previous sectors, packed from the
    /// first start angle, so at the pie's end angle (past it by the padding when the pie is not a full turn).
    /// </summary>
    private static double Entry(PieSpec pie)
    {
        var delta = pie.EndAngle - pie.StartAngle;
        return Math.Abs(delta) >= 360 ? pie.EndAngle : pie.EndAngle + (Math.Sign(delta) * pie.PaddingAngle);
    }

    private static void Sector<TCanvas>(ref TCanvas canvas, SectorShape shape, PieItem item, PieSpec pie, SectorShapeSpec? option, double? entry)
        where TCanvas : struct, IChartCanvas
    {
        if (!shape.Drawn)
        {
            return;
        }

        SeriesPainter.Morph(ref canvas, entry is { } from ? shape.Morph(from) : null);

        var fill = option?.Fill ?? item.Fill;
        var paint = SeriesPainter.Celled(new Paint(option?.Stroke ?? (pie.BlendStroke ? item.Fill : item.Stroke), double.NaN, fill), item.Cell);
        paint = option is null ? paint : paint with { FillOpacity = option.FillOpacity ?? paint.FillOpacity, Opacity = option.Opacity ?? paint.Opacity, StrokeWidth = option.StrokeWidth ?? paint.StrokeWidth };
        canvas.Shape(shape, paint, ClassNames.With("recharts-sector"u8, pie.ClassName));
    }

    private static void Close<TCanvas>(ref TCanvas canvas, bool marked)
        where TCanvas : struct, IChartCanvas
    {
        if (marked)
        {
            canvas.Close();
        }
    }

    private static void Labels<TCanvas>(ref TCanvas canvas, PieLayout pie, ChartSpec chart, int offset)
        where TCanvas : struct, IChartCanvas
    {
        var fontSize = chart.FontSize;
        canvas.Open("recharts-layer recharts-pie-labels"u8);
        for (var i = 0; i < pie.Sectors.Length; i++)
        {
            if (Toggles.Concealed(chart, offset + i))
            {
                continue;
            }

            var marked = Toggles.Open(ref canvas, chart, offset + i);
            var sector = pie.Sectors[i];
            var item = pie.Items[i];
            var spec = pie.Spec;
            var (x, y) = Layout.Polar.Point(pie.Cx, pie.Cy, pie.OuterRadius + spec.LabelOffsetRadius, sector.MidAngle);
            canvas.Open("recharts-layer"u8);
            if (spec.LabelLine)
            {
                var (sx, sy) = Layout.Polar.Point(pie.Cx, pie.Cy, pie.OuterRadius, sector.MidAngle);
                canvas.Shape(new SegmentShape(sx, sy, x, y), new Paint(spec.LabelLineStroke ?? item.Fill, spec.LabelLineStrokeWidth ?? double.NaN, "none", double.NaN, spec.LabelLineStrokeDashArray), "recharts-curve recharts-pie-label-line"u8);
            }

            var anchor = x > pie.Cx ? TextAnchor.Start : x < pie.Cx ? TextAnchor.End : TextAnchor.Middle;
            var props = spec.LabelProps;
            var text = new TextPaint(anchor, LabelPlacement.End, props?.Fill ?? item.Fill, props?.FontSize ?? fontSize, 0, MiddleBaseline: true)
            {
                FontWeight = props?.FontWeight,
                Stroke = props?.Stroke,
                StrokeWidth = props?.StrokeWidth ?? double.NaN,
                OwnSize = props?.FontSize is not null,
            };
            canvas.Text(x, y, SeriesPainter.Value(item.Value, props?.Format), text, "recharts-text recharts-pie-label-text"u8);
            canvas.Close();
            Close(ref canvas, marked);
        }

        canvas.Close();
    }

    /// <summary>The pie layer's tab index (Recharts' <c>rootTabIndex</c>), when it has one.</summary>
    private static void Focusable<TCanvas>(ref TCanvas canvas, int? index)
        where TCanvas : struct, IChartCanvas
    {
        if (index is { } tab)
        {
            canvas.TabIndex(tab);
        }
    }
}
