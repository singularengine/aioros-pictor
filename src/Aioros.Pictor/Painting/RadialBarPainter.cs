using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>
/// Paints a <c>RadialBar</c> as Recharts 3 renders it statically, in its z-index layers: its backgrounds and sectors,
/// then (over the polar axes) its label lists, shown once the bars have animated in.
/// </summary>
internal static class RadialBarPainter
{
    public static void RadialBar<TCanvas>(ref TCanvas canvas, PolarLayout layout, RadialBarLayout bar)
        where TCanvas : struct, IChartCanvas
    {
        var spec = bar.Spec;
        var timing = Motion.Animations.Of(bar);
        if (timing is { } value)
        {
            canvas.MorphLayer(new Motion.MorphLayer(Motion.MorphKind.Sector, 0, false, value, layout.Data.Key()));
            canvas.OpenMotion("recharts-layer recharts-area"u8, value);
        }
        else
        {
            canvas.Open("recharts-layer recharts-area"u8);
        }

        if (spec.Background is { } background)
        {
            canvas.Open("recharts-layer recharts-radial-bar-background"u8);
            foreach (var sector in bar.Sectors.Where(sector => sector.Background.Drawn))
            {
                canvas.Shape(sector.Background, SeriesPainter.Celled(new Paint(spec.BackgroundProps?.Stroke, double.NaN, layout.Spec.Colors.BackgroundOf(background)), spec.BackgroundProps), "recharts-sector recharts-radial-bar-background-sector undefined"u8);
            }

            canvas.Close();
        }

        Sectors(ref canvas, layout, bar, timing is not null);
        canvas.Close();
    }

    /// <summary>A radial bar's label lists, in Recharts 3's label layer over the axes, waiting for the bars' animation to end.</summary>
    public static void Labels<TCanvas>(ref TCanvas canvas, PolarLayout layout, RadialBarLayout bar)
        where TCanvas : struct, IChartCanvas
    {
        var spec = bar.Spec;
        var lists = new LabelLists(spec.Labels, spec.LabelLists);
        if (spec.Labels is null && spec.LabelLists.IsDefaultOrEmpty)
        {
            return;
        }

        if (Motion.Animations.Of(bar) is { } timing)
        {
            canvas.OpenMotion(default, timing);
            canvas.Animate(new Motion.Step(Motion.StepKind.Hold));
        }
        else
        {
            canvas.Open(default);
        }

        var list = 0;
        foreach (var label in lists)
        {
            RadialLabels(ref canvas, layout, bar, label, ContentProps.Template(spec.Order, 4 + list++));
        }

        canvas.Close();
    }

    /// <summary>The arcs that grow a radial bar's sectors from their start angles as Recharts' enter animation sweeps them, shown while the bars animate in; the sectors wait for the end.</summary>
    private static void Arcs<TCanvas>(ref TCanvas canvas, PolarLayout layout, RadialBarLayout bar)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Animate(new Motion.Step(Motion.StepKind.Shown));
        canvas.Open(default);
        for (var i = 0; i < bar.Sectors.Length; i++)
        {
            var shape = bar.Sectors[i].Shape;
            if (shape.Drawn && !Toggles.Concealed(layout.Spec, i))
            {
                var marked = Toggles.Open(ref canvas, layout.Spec, i);
                var span = shape.EndAngle - shape.StartAngle;
                canvas.Grow(new Motion.GrowingArc(shape.Cx, shape.Cy, (shape.InnerRadius + shape.OuterRadius) / 2, shape.OuterRadius - shape.InnerRadius, shape.StartAngle, span >= 0, 0, 0, Math.Abs(span), bar.Sectors[i].Fill ?? "#000"));
                Close(ref canvas, marked);
            }
        }

        canvas.Close();
    }

    private static void Sectors<TCanvas>(ref TCanvas canvas, PolarLayout layout, RadialBarLayout bar, bool animated)
        where TCanvas : struct, IChartCanvas
    {
        if (animated)
        {
            Arcs(ref canvas, layout, bar);
            canvas.Animate(new Motion.Step(Motion.StepKind.Hold));
        }

        canvas.Open("recharts-layer recharts-radial-bar-sectors"u8);
        var content = ContentProps.Of(bar.Spec.ShapeContent, Content.ContentSlots.Sector, layout.Data);
        for (var i = 0; i < bar.Sectors.Length; i++)
        {
            if (!bar.Sectors[i].Shape.Drawn || Toggles.Concealed(layout.Spec, i))
            {
                continue;
            }

            var marked = Toggles.Open(ref canvas, layout.Spec, i);
            BarSector(ref canvas, bar, i, content, ContentProps.Template(bar.Spec.Order, 0), animated);
            Close(ref canvas, marked);
        }

        canvas.Close();
    }

    /// <summary>A radial bar's sector: its custom content, or its sector morphing from its start angle when the bars animate.</summary>
    private static void BarSector<TCanvas>(ref TCanvas canvas, RadialBarLayout bar, int i, Content.ContentValues? content, int template, bool animated)
        where TCanvas : struct, IChartCanvas
    {
        if (content is not null)
        {
            canvas.Content(content.Spec, ContentProps.RadialBar(content, bar, i, template));
            return;
        }

        var sector = bar.Sectors[i];
        SeriesPainter.Morph(ref canvas, animated ? sector.Shape.Morph(sector.Shape.StartAngle) : null);
        canvas.Shape(sector.Shape, SeriesPainter.Celled(new Paint(bar.Spec.Stroke, double.NaN, sector.Fill), i < bar.Spec.Cells.Length ? bar.Spec.Cells[i] : null), "recharts-sector recharts-radial-bar-sector undefined"u8);
    }

    /// <summary>A radial bar's label list, its labels running counterclockwise whatever its <c>clockWise</c>, which Recharts 3's <c>LabelList</c> takes and never passes on.</summary>
    private static void RadialLabels<TCanvas>(ref TCanvas canvas, PolarLayout layout, RadialBarLayout bar, LabelSpec label, int template)
        where TCanvas : struct, IChartCanvas
    {
        label = label with { ClockWise = false };
        var content = ContentProps.Of(label.Content, Content.ContentSlots.Label, layout.Data);
        canvas.Open("recharts-layer recharts-label-list"u8);
        for (var i = 0; i < bar.Sectors.Length; i++)
        {
            if (!Toggles.Concealed(layout.Spec, i))
            {
                var marked = Toggles.Open(ref canvas, layout.Spec, i);
                RadialLabel(ref canvas, layout, bar, label, i, content is null ? null : ContentProps.Label(content, template, i, bar.Sectors[i].Value, bar.Spec.Name ?? bar.Spec.DataKey));
                Close(ref canvas, marked);
            }
        }

        canvas.Close();
    }

    private static void Close<TCanvas>(ref TCanvas canvas, bool marked)
        where TCanvas : struct, IChartCanvas
    {
        if (marked)
        {
            canvas.Close();
        }
    }

    private static void RadialLabel<TCanvas>(ref TCanvas canvas, PolarLayout layout, RadialBarLayout bar, LabelSpec label, int index, LabelContent? content)
        where TCanvas : struct, IChartCanvas
    {
        var sector = bar.Sectors[index];
        var text = label.DataKey is { } key ? LabelPainter.Text(layout.Data, key, index, label.Format) : SeriesPainter.Value(sector.Value, label.Format);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var shape = sector.Shape;
        var box = new PolarBox(shape.Cx, shape.Cy, shape.InnerRadius, shape.OuterRadius, shape.StartAngle, shape.EndAngle);
        LabelPainter.Polar(ref canvas, label, text, box, layout.Spec.FontSize, sector.Fill ?? layout.Spec.Colors.LabelText, bar.Spec.Stroke, content);
    }
}
