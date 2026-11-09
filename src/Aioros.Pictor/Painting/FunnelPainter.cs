using Aioros.Graphs.Paths;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Painting;

/// <summary>Recharts' <c>Trapezoid</c> path: the upper edge, the lower edge centered under it, closed.</summary>
internal readonly struct TrapezoidShape(double x, double y, double upperWidth, double lowerWidth, double height) : IShape
{
    public void Write<TSink>(ref TSink sink)
        where TSink : struct, IPathSink
    {
        var gap = upperWidth - lowerWidth;
        sink.MoveTo(x, y);
        sink.LineTo(x + upperWidth, y);
        sink.LineTo(x + upperWidth - (gap / 2), y + height);
        sink.LineTo(x + upperWidth - (gap / 2) - lowerWidth, y + height);
        sink.LineTo(x, y);
        sink.ClosePath();
    }
}

/// <summary>Paints a funnel chart as Recharts' <c>Funnel</c> renders it statically, then its hover states.</summary>
internal static class FunnelPainter
{
    public static void Paint<TCanvas>(ref TCanvas canvas, FunnelPlot plot, int active)
        where TCanvas : struct, IChartCanvas => Paint(ref canvas, plot, active, layers: true);

    /// <summary>
    /// The chart with a hover state's active shapes in place and no hover layers: the desktop's frame while an item of a
    /// funnel that draws its active shape in place is hovered (<see cref="InPlace(FunnelPlot)"/>).
    /// </summary>
    public static void PaintInPlace<TCanvas>(ref TCanvas canvas, FunnelPlot plot, int active)
        where TCanvas : struct, IChartCanvas => Paint(ref canvas, plot, active, layers: false);

    /// <summary>Whether a funnel of the chart draws its hovered item's active shape in place (an <c>activeShape</c> template, a tooltip, no set <c>activeIndex</c>), as Recharts renders it in the item's own layer under the label lists.</summary>
    public static bool InPlace(FunnelPlot plot)
    {
        for (var i = 0; i < plot.Funnels.Count; i++)
        {
            if (InPlace(plot, plot.Funnels[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool InPlace(FunnelPlot plot, FunnelSeries funnel) => plot.HoverCount > 0 && funnel.Spec.ActiveShapeContent is not null && !HasActive(funnel.Spec);

    private static void Paint<TCanvas>(ref TCanvas canvas, FunnelPlot plot, int active, bool layers)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Begin(plot);
        foreach (var funnel in plot.Funnels.Where(funnel => funnel.Items.Length > 0))
        {
            var target = Target(plot, funnel);
            if (!Toggles.Concealed(plot.Spec, target))
            {
                var marked = Toggles.Open(ref canvas, plot.Spec, target);
                Funnel(ref canvas, plot, funnel, active);
                Close(ref canvas, marked);
            }
        }

        if (layers && active != ChartPainter.NoTips && plot.HoverCount > 0)
        {
            Hits(ref canvas, plot, active);
            Tips(ref canvas, plot, active);
        }

        canvas.EndSurface();
        if (plot.Legend is { } legend)
        {
            canvas.Legend(legend);
        }

        canvas.End();
    }

    public static void Hits<TCanvas>(ref TCanvas canvas, FunnelPlot plot, int active)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Open(TipPainter.HitsLayer(plot.Spec.Tooltip!));
        var index = 0;
        foreach (var funnel in plot.Funnels)
        {
            var target = Target(plot, funnel);
            foreach (var item in funnel.Items)
            {
                if (active < 0 || active == index)
                {
                    var marked = Toggles.Open(ref canvas, plot.Spec, target);
                    canvas.HitShape(Drawable(item) ? Shape(item) : default);
                    Close(ref canvas, marked);
                }

                index++;
            }
        }

        canvas.Close();
    }

    public static void Tips<TCanvas>(ref TCanvas canvas, FunnelPlot plot, int active)
        where TCanvas : struct, IChartCanvas
    {
        canvas.Open("pictor-tips"u8);
        var index = 0;
        foreach (var funnel in plot.Funnels)
        {
            var target = Target(plot, funnel);
            var first = First(plot, funnel);
            foreach (var item in funnel.Items)
            {
                if (active < 0 || active == index)
                {
                    canvas.Open(default);
                    var marked = Toggles.Open(ref canvas, plot.Spec, target);
                    Tip(ref canvas, plot, funnel, item, index - first);
                    Close(ref canvas, marked);
                    canvas.Close();
                }

                index++;
            }
        }

        canvas.Close();
    }

    /// <summary>An item's hover state: its tooltip (an active shape is drawn in the item's own layer, <see cref="InPlace(FunnelPlot)"/>).</summary>
    private static void Tip<TCanvas>(ref TCanvas canvas, FunnelPlot plot, FunnelSeries funnel, Trapezoid item, int index)
        where TCanvas : struct, IChartCanvas
    {
        ItemTips.Tooltip(ref canvas, plot.Spec, new ItemTip(item.TipX, item.TipY, item.Name, item.Value, funnel.Spec.TooltipNone), plot.Left, plot.Top, plot.PlotWidth, plot.PlotHeight, funnel.Data, index);
    }

    /// <summary>
    /// The active shape a hover state shows in place of an item's shape, in the item's layer: drawn instead of it for the
    /// desktop's hovered state, written after it in a hidden group <c>pictor-akK</c> for every state on the web (the style
    /// block shows the group and hides the shape before it while state K is entered).
    /// </summary>
    private static void HoveredInPlace<TCanvas>(ref TCanvas canvas, FunnelSeries funnel, int i, Content.ContentValues active, int template, int state)
        where TCanvas : struct, IChartCanvas
    {
        Span<byte> classes = stackalloc byte[24];
        canvas.Open(classes[..Toggles.Class(classes, "pictor-ak"u8, state)]);
        ActiveShape(ref canvas, funnel, i, active, template);
        canvas.Close();
    }

    /// <summary>An item's active shape from the funnel's template, in Recharts' active layer.</summary>
    private static void ActiveShape<TCanvas>(ref TCanvas canvas, FunnelSeries funnel, int index, Content.ContentValues content, int template)
        where TCanvas : struct, IChartCanvas
    {
        var item = funnel.Items[index];
        var paint = SeriesPainter.Celled(new Paint(item.Stroke, double.NaN, item.Fill), index < funnel.Spec.Cells.Length ? funnel.Spec.Cells[index] : null);
        canvas.Open("recharts-layer recharts-active-shape"u8);
        canvas.Content(content.Spec, ContentProps.Trapezoid(content, funnel, index, paint, template, state: 1));
        canvas.Close();
    }

    /// <summary>The place of a funnel among the chart's.</summary>
    private static int Position(FunnelPlot plot, FunnelSeries funnel)
    {
        for (var i = 0; i < plot.Funnels.Count; i++)
        {
            if (ReferenceEquals(plot.Funnels[i], funnel))
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>The hover state of a funnel's first item: the items of the funnels before it.</summary>
    private static int First(FunnelPlot plot, FunnelSeries funnel)
    {
        var index = 0;
        foreach (var other in plot.Funnels)
        {
            if (ReferenceEquals(other, funnel))
            {
                return index;
            }

            index += other.Items.Length;
        }

        return index;
    }

    public static int Hovered(FunnelPlot plot, double x, double y)
    {
        if (plot.HoverCount == 0)
        {
            return ChartPainter.NoTips;
        }

        var index = 0;
        foreach (var (item, target) in plot.Funnels.SelectMany(funnel => funnel.Items.Select(item => (item, Target(plot, funnel)))))
        {
            if (Drawable(item) && !Toggles.Concealed(plot.Spec, target) && y >= item.Y && y <= item.Y + item.Height)
            {
                var t = (y - item.Y) / item.Height;
                var half = (item.UpperWidth + ((item.LowerWidth - item.UpperWidth) * t)) / 2;
                var center = item.X + (item.UpperWidth / 2);
                if (Math.Abs(x - center) <= half)
                {
                    return index;
                }
            }

            index++;
        }

        return ChartPainter.NoTips;
    }

    private static void Funnel<TCanvas>(ref TCanvas canvas, FunnelPlot plot, FunnelSeries funnel, int hovered)
        where TCanvas : struct, IChartCanvas
    {
        var timing = Motion.Animations.Of(funnel);
        if (timing is { } value)
        {
            canvas.MorphLayer(new Motion.MorphLayer(Motion.MorphKind.Polygon, 0, false, value, funnel.Data.Key()));
            canvas.OpenMotion("recharts-layer recharts-trapezoids"u8, value);
        }
        else
        {
            canvas.Open("recharts-layer recharts-trapezoids"u8);
        }

        var content = ContentProps.Of(funnel.Spec.ShapeContent, Content.ContentSlots.Trapezoid, funnel.Data);
        var active = ContentProps.Of(funnel.Spec.ActiveShapeContent, Content.ContentSlots.Trapezoid, funnel.Data);
        var position = Position(plot, funnel);
        var first = First(plot, funnel);
        var inPlace = active is not null && InPlace(plot, funnel);
        for (var i = 0; i < funnel.Items.Length; i++)
        {
            canvas.Open("recharts-layer recharts-funnel-trapezoid"u8);
            if (IsActive(funnel.Spec, i))
            {
                canvas.Open("recharts-layer recharts-active-shape"u8);
                Item(ref canvas, funnel, i, active, ContentProps.Template(position, 1), timing is not null);
                canvas.Close();
            }
            else if (inPlace && hovered == first + i && Drawable(funnel.Items[i]))
            {
                ActiveShape(ref canvas, funnel, i, active!, ContentProps.Template(position, 1));
            }
            else
            {
                Item(ref canvas, funnel, i, content, ContentProps.Template(position, 0), timing is not null);
                if (inPlace && hovered == ChartPainter.AllTips && Drawable(funnel.Items[i]))
                {
                    HoveredInPlace(ref canvas, funnel, i, active!, ContentProps.Template(position, 1), first + i);
                }
            }

            canvas.Close();
        }

        var list = 0;
        foreach (var label in new LabelLists(funnel.Spec.Labels, funnel.Spec.LabelLists))
        {
            Labels(ref canvas, plot, funnel, label, ContentProps.Template(position, 4 + list++));
        }

        canvas.Close();
    }

    /// <summary>Whether a trapezoid is drawn active by the funnel's <c>activeIndex</c> (a number or an array).</summary>
    public static bool IsActive(FunnelSpec spec, int index) => spec.ActiveIndex == index || (!spec.ActiveIndexes.IsDefaultOrEmpty && spec.ActiveIndexes.Contains(index));

    /// <summary>Whether the funnel has an <c>activeIndex</c>, which the pointer does not change.</summary>
    public static bool HasActive(FunnelSpec spec) => spec.ActiveIndex is not null || !spec.ActiveIndexes.IsDefaultOrEmpty;

    /// <summary>An item's shape in its layer: its growth when the funnel animates in, its custom content, or its default trapezoid in a group, as Recharts' <c>Trapezoid</c> writes it.</summary>
    private static void Item<TCanvas>(ref TCanvas canvas, FunnelSeries funnel, int i, Content.ContentValues? content, int template, bool animated)
        where TCanvas : struct, IChartCanvas
    {
        var item = funnel.Items[i];
        if (!Drawable(item))
        {
            return;
        }

        if (content is not null)
        {
            if (animated)
            {
                canvas.Animate(new Motion.Step(Motion.StepKind.Scale));
            }

            Trapezoid(ref canvas, funnel, i, content, template);
            return;
        }

        canvas.Open(default);
        if (animated)
        {
            canvas.Animate(new Motion.Step(Motion.StepKind.Scale));
            canvas.Morph(Mark(item));
        }

        Trapezoid(ref canvas, funnel, i, null, 0);
        canvas.Close();
    }

    /// <summary>A trapezoid drawn: its default shape, or its custom content.</summary>
    private static void Trapezoid<TCanvas>(ref TCanvas canvas, FunnelSeries funnel, int i, Content.ContentValues? content, int template)
        where TCanvas : struct, IChartCanvas
    {
        var item = funnel.Items[i];
        var paint = SeriesPainter.Celled(new Paint(item.Stroke, double.NaN, item.Fill), i < funnel.Spec.Cells.Length ? funnel.Spec.Cells[i] : null);
        if (content is not null)
        {
            canvas.Content(content.Spec, ContentProps.Trapezoid(content, funnel, i, paint, template));
            return;
        }

        canvas.Shape(Shape(item), paint, "recharts-trapezoid"u8);
    }

    private static void Labels<TCanvas>(ref TCanvas canvas, FunnelPlot plot, FunnelSeries funnel, LabelSpec label, int template)
        where TCanvas : struct, IChartCanvas
    {
        var content = ContentProps.Of(label.Content, Content.ContentSlots.Label, funnel.Data);
        var key = funnel.Data.Resolve(label.DataKey ?? funnel.Spec.DataKey);
        var numbers = funnel.Data.Numbers(key);
        var texts = funnel.Data.Texts(key);
        canvas.Open("recharts-layer recharts-label-list"u8);
        for (var i = 0; i < funnel.Items.Length; i++)
        {
            var text = double.IsNaN(numbers[i]) ? texts[i] : SeriesPainter.Value(numbers[i], label.Format);
            if (text is not null)
            {
                var item = funnel.Items[i];
                var box = (item.LabelX, item.LabelY, item.LabelWidth, item.Height);
                LabelPainter.Cartesian(ref canvas, label, text, box, plot.Spec.FontSize, item.Fill, wrap: Wrap(plot, label, box), content: ContentProps.Label(content, template, i, item.Value, item.Name));
            }
        }

        canvas.Close();
    }

    /// <summary>
    /// The width a funnel label is wrapped to, as Recharts gives it with the plot as the parent box: the space beside the
    /// label at the left and the right, the trapezoid's label box elsewhere.
    /// </summary>
    private static double Wrap(FunnelPlot plot, LabelSpec label, (double X, double Y, double Width, double Height) box)
    {
        if (label.PositionX is not null && label.PositionY is not null)
        {
            return box.Width;
        }

        var (x, _, _, _) = LabelPlacement.Of(label.Position ?? LabelPosition.Center, box.X, box.Y, box.Width, box.Height, label.Offset);
        return label.Position switch
        {
            LabelPosition.Left => Math.Max(x - plot.Left, 0),
            LabelPosition.Right => Math.Max(plot.Left + plot.PlotWidth - x, 0),
            _ => box.Width,
        };
    }

    /// <summary>The legend toggle of a funnel: its index among the chart's funnels.</summary>
    private static int Target(FunnelPlot plot, FunnelSeries funnel) => Specs.SpecIndex.Of(plot.Spec.Funnels, funnel.Spec);

    private static void Close<TCanvas>(ref TCanvas canvas, bool marked)
        where TCanvas : struct, IChartCanvas
    {
        if (marked)
        {
            canvas.Close();
        }
    }

    private static bool Drawable(in Trapezoid item) =>
        double.IsFinite(item.X) && double.IsFinite(item.Y) && double.IsFinite(item.UpperWidth) && double.IsFinite(item.LowerWidth) && double.IsFinite(item.Height)
        && (item.UpperWidth != 0 || item.LowerWidth != 0) && item.Height != 0;

    /// <summary>A trapezoid's corners for the web's update, a new one growing from its center as Recharts' update grows it.</summary>
    private static Motion.MorphMark Mark(in Trapezoid item)
    {
        var inset = (item.UpperWidth - item.LowerWidth) / 2;
        var bottom = item.Y + item.Height;
        double[] x = [item.X, item.X + item.UpperWidth, item.X + item.UpperWidth - inset, item.X + inset];
        double[] y = [item.Y, item.Y, bottom, bottom];
        return Motion.MorphMark.Points(Motion.MorphKind.Polygon, 0, x, y, (item.X + (item.UpperWidth / 2), item.Y + (item.Height / 2)));
    }

    internal static TrapezoidShape Shape(in Trapezoid item) => new(item.X, item.Y, item.UpperWidth, item.LowerWidth, item.Height);
}
