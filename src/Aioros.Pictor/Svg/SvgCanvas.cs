using System.Buffers;
using Aioros.Graphs.Svg;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Motion;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>
/// Writes a chart as Recharts' markup in UTF-8: the wrapper, the surface, the layers with Recharts' class names, the
/// legend as its HTML list, and the hover states as hidden groups that CSS shows (no script).
/// </summary>
internal struct SvgCanvas(IBufferWriter<byte> writer) : IChartCanvas
{
    private readonly MarkupBuffer output = MarkupBuffer.Over(writer);

    private bool _inline;
    private HoverMarkup.Layer _layer;
    private int _depth;
    private int _layerDepth;
    private ulong _skipped;
    private bool _animated;
    private int _rules;
    private int _motion;
    private Step _step;
    private FocusMarkup.Mode _focus;
    private MorphWriter _morph = new();
    private bool _compact;
    private int _tabStop;
    private bool _followButtons;
    private bool _clipped;
    private ClipRect _clip;
    private string? _zoom;
    private string?[]? _values;
    private int _group;
    private ulong _buffered;
    private int? _tab;
    private HoverLayers? _hover;
    private PointerFollow? _follow;
    private (BrushLayout Brush, string Start, string End)? _brushFields;
    private (NestBox[] Trail, Specs.ContentSpec? Content)? _nest;

    /// <summary>Where the fields and commands of the chart are noted, when it writes any.</summary>
    public ChartMarks? Marks { get; init; }

    /// <summary>The chart's click targets, written as command buttons over its surface.</summary>
    public List<Interaction.ClickTarget>? Clicks { get; init; }

    private IChartPlot? _plot;
    private Theming.ChartTheme _theme = Theming.ChartTheme.Light;

    /// <summary>
    /// Whether an earlier layout of the same responsive chart wrote the hover and sync rules for as many hover states: they
    /// name classes only, so those rules serve this layout too; a layout with more states (a bar's tips, a radial bar's
    /// drawn sectors can differ by width) writes its own.
    /// </summary>
    public bool Repeated { get; init; }

    /// <summary>Where the chart's sync rules go when it is written into a page, which writes them once; inline when none.</summary>
    public List<SyncRules>? PageStyles { get; init; }

    /// <summary>A class added to the wrapper: the width range of one of a responsive chart's layouts.</summary>
    public string? Variant { get; init; }

    public void Begin(IChartPlot plot)
    {
        var spec = plot.Spec;
        _plot = plot;
        if (spec.Compact)
        {
            _compact = true;
            WrapperMarkup.Compact(output, spec);
            GradientMarkup.Defs(output, spec.Gradients, null);
            return;
        }
        _inline = spec.Responsive;
        _zoom = spec.Treemap?.ZoomCommand;
        _theme = spec.Colors;
        (_focus, _tabStop) = (FocusMarkup.Of(plot), Math.Max(plot.DefaultTip, 0));
        var keys = ScatterKeys.Applies(plot);
        _follow = PointerFollow.Of(plot);
        _followButtons = FocusMarkup.OnSurface(_focus, Clicks);
        var synced = WrapperMarkup.Classes(output, plot, Variant);

        WrapperMarkup.Style(output, spec, Variant is null, _theme);
        WrapperMarkup.Surface(output, spec, new(spec.AccessibilityLayer && _focus != FocusMarkup.Mode.Keys && !keys, _followButtons ? _focus : FocusMarkup.Mode.None, _follow is not null));
        GradientMarkup.Defs(output, spec.Gradients, Variant);
        _values = HoverMarkup.Rules(output, plot, synced, new(_focus, keys, Plots.DrawsCursors(plot), _follow, Plots.InPlace(plot)), Repeated, PageStyles);
    }

    public void Open(ReadOnlySpan<byte> classes)
    {
        _depth++;
        if (OpenedHover(classes))
        {
            return;
        }

        var step = Take();
        var late = (_motion > 0 && SvgMotion.IsLate(classes)) || step.Kind == StepKind.Hold;
        step = step.Kind == StepKind.Hold ? default : step;
        _motion += _motion > 0 ? 1 : 0;
        if (step.Kind == StepKind.None && !late)
        {
            Plain(classes);
            return;
        }

        Raw(output, "<g"u8);
        SvgMotion.Classes(output, classes, step, series: false, late);
        SvgMotion.Attributes(output, step, null);
        ClipAttribute();
        Raw(output, ">"u8);
    }

    /// <summary>Opens what the hover layers write in place of a plain group (<see cref="HoverLayers"/>), or nothing; whether it did.</summary>
    private bool OpenedHover(ReadOnlySpan<byte> classes)
    {
        if (_layer == HoverMarkup.Layer.None && HoverMarkup.LayerOf(classes) is var layer and not HoverMarkup.Layer.None)
        {
            (_layer, _layerDepth, _group) = (layer, _depth, 0);
            if (layer != HoverMarkup.Layer.Keys)
            {
                OpenHover(layer);
            }

            return layer != HoverMarkup.Layer.Keys;
        }

        if (StateGroup(classes))
        {
            State(_group++);
            return true;
        }

        if (_layer == HoverMarkup.Layer.Hits && _hover is not null && _depth < 64)
        {
            InHits(classes);
            return true;
        }

        if (_layer == HoverMarkup.Layer.Tips && HoverMarkup.Unwrapped(classes) && _depth < 64)
        {
            _skipped |= 1UL << _depth;
            return true;
        }

        return false;
    }

    /// <summary>A group inside the hit layer other than a plain state's: a state's own classed group (a legend toggle's), or a group inside a state, both put aside with the hit groups.</summary>
    private void InHits(ReadOnlySpan<byte> classes)
    {
        if (_depth == _layerDepth + 1)
        {
            _hover!.OpenWrapped(classes, _group++);
            return;
        }

        _buffered |= 1UL << _depth;
        Raw(_hover!.Hits, classes.IsEmpty ? "<g"u8 : "<g class=\""u8);
        Raw(_hover.Hits, classes);
        Raw(_hover.Hits, classes.IsEmpty ? ">"u8 : "\">"u8);
    }

    private void Plain(ReadOnlySpan<byte> classes)
    {
        Raw(output, "<g"u8);
        Classes(classes);
        if (_depth == _layerDepth && _layer == HoverMarkup.Layer.Keys)
        {
            Raw(output, " data-aioros-roving=\"horizontal\" data-aioros-roving-escape data-aioros-roving-ends=\"off\""u8);
        }

        ClipAttribute();
        Raw(output, ">"u8);
    }

    public void OpenFaded(ReadOnlySpan<byte> classes, double opacity)
    {
        if (double.IsNaN(opacity))
        {
            Open(classes);
            return;
        }

        _depth++;
        Raw(output, "<g"u8);
        Classes(classes);
        Attribute(output, " opacity"u8, opacity);
        Raw(output, ">"u8);
    }

    public void OpenSlider(ReadOnlySpan<byte> classes, string label, double value)
    {
        _depth++;
        Raw(output, "<g role=\"slider\""u8);
        Attribute(output, " aria-label"u8, label);
        Attribute(output, " aria-valuenow"u8, value);
        Classes(classes);
        ClipAttribute();
        Raw(output, ">"u8);
    }

    private void OpenHover(HoverMarkup.Layer layer)
    {
        _hover ??= new HoverLayers(_focus, _tabStop, _values, _followButtons, _follow, _plot?.HoverCount ?? 0);
        if (layer == HoverMarkup.Layer.Hits)
        {
            HoverLayers.OpenCursors(output);
        }
        else
        {
            _hover.OpenTips(output);
        }
    }

    /// <summary>A hover state's group: a hit layer's (its cursor group and its hit group, <see cref="HoverLayers"/>), a tip, or a key step (the first in the tab order).</summary>
    private readonly void State(int index)
    {
        switch (_layer)
        {
            case HoverMarkup.Layer.Hits:
                _hover!.OpenState(output, index);
                break;
            case HoverMarkup.Layer.Tips:
                _hover!.Tip(output, index);
                break;
            default:
                Raw(output, FocusMarkup.Group(FocusMarkup.Mode.Keys, index == 0));
                Raw(output, ">"u8);
                break;
        }
    }

    /// <summary>A hit shape's class: <c>pictor-hit</c> in a state's hit group, or the state's own classes when the shape is the state (an item chart's hit shapes, written straight in the hit layer); both put aside until the tips.</summary>
    private void HitClass()
    {
        if (Bare)
        {
            _hover!.BareHit(_group++);
            return;
        }

        Raw(HitOutput, " class=\"pictor-hit\""u8);
    }

    private readonly bool Bare => _layer == HoverMarkup.Layer.Hits && _depth == _layerDepth && _hover is not null;

    private readonly MarkupBuffer HitOutput => _layer == HoverMarkup.Layer.Hits && _hover is not null ? _hover.Hits : output;

    private readonly bool StateGroup(ReadOnlySpan<byte> classes) => classes.IsEmpty && _layer != HoverMarkup.Layer.None && _depth == _layerDepth + 1;

    public void OpenMotion(ReadOnlySpan<byte> classes, in Timing timing)
    {
        _depth++;
        if (!_animated)
        {
            Raw(output, SvgMotion.Base);
            _animated = true;
        }

        var step = Take();
        Raw(output, "<g"u8);
        SvgMotion.Classes(output, classes, step, series: true, late: false);
        SvgMotion.Attributes(output, step, timing);
        _morph.Open(output);
        ClipAttribute();
        Raw(output, ">"u8);
        _motion = 1;
    }

    public void Animate(in Step step) => _step = step;

    public void MorphLayer(in MorphLayer layer) => _morph.Layer(layer);

    public void Morph(in MorphMark mark) => _morph.Mark(mark);

    public void Grow(in GrowingArc arc) => SvgMotion.Arc(output, arc);

    /// <summary>The pending movement, with its rules written the first time the chart uses it.</summary>
    private Step Take()
    {
        var step = _step;
        _step = default;
        var bit = 1 << (int)step.Kind;
        if (step.Kind != StepKind.None && (_rules & bit) == 0)
        {
            Raw(output, SvgMotion.Rules(step.Kind));
            _rules |= bit;
        }

        return step;
    }

    public void Close()
    {
        if (_depth < 64 && (_skipped & (1UL << _depth)) != 0)
        {
            _skipped &= ~(1UL << _depth);
        }
        else if (_depth < 64 && (_buffered & (1UL << _depth)) != 0)
        {
            _buffered &= ~(1UL << _depth);
            Raw(_hover!.Hits, "</g>"u8);
        }
        else if (_layer == HoverMarkup.Layer.Hits && _depth == _layerDepth + 1 && _hover is not null)
        {
            _hover.CloseState(output);
        }
        else if (_layer == HoverMarkup.Layer.Tips && _depth == _layerDepth && _hover is not null)
        {
            _hover.CloseTips(output);
            Raw(output, "</g>"u8);
        }
        else
        {
            _motion -= _motion > 0 ? 1 : 0;
            Raw(output, "</g>"u8);
        }

        if (_depth == _layerDepth)
        {
            _layer = HoverMarkup.Layer.None;
        }

        _depth--;
    }

    public void Line(double x1, double y1, double x2, double y2, in Paint paint, ReadOnlySpan<byte> classes)
    {
        if (_layer == HoverMarkup.Layer.Hits && HoverMarkup.IsCursor(classes))
        {
            HoverMarkup.Segment(output, x1, y1, x2, y2, paint, classes, _theme);
            return;
        }

        Raw(output, "<line"u8);
        Paint(paint);
        Attribute(output, " x1"u8, x1);
        Attribute(output, " y1"u8, y1);
        Attribute(output, " x2"u8, x2);
        Attribute(output, " y2"u8, y2);
        Classes(classes);
        ClipAttribute();
        Raw(output, "/>"u8);
    }

    public void Shape<TShape>(in TShape shape, in Paint paint, ReadOnlySpan<byte> classes)
        where TShape : struct, IShape
    {
        var step = Take();
        var (mask, data) = step.Kind == StepKind.DrawDashed ? SvgMotion.DashMask(output, shape, step.Offset) : (0u, null);
        step = step.Kind == StepKind.DrawDashed ? default : step;
        Raw(output, "<path"u8);
        if ((_layer is HoverMarkup.Layer.Hits or HoverMarkup.Layer.Keys) && HoverMarkup.IsCursor(classes))
        {
            HoverMarkup.CursorPaint(output, paint, classes, _theme);
        }
        else
        {
            Paint(paint);
        }

        if (step.Kind == StepKind.None)
        {
            Classes(classes);
        }
        else
        {
            SvgMotion.Classes(output, classes, step, series: false, late: false);
            SvgMotion.Attributes(output, step, null);
        }

        SvgMotion.MaskAttribute(output, mask);
        ClipAttribute();
        _morph.Take(output);
        Raw(output, " d=\""u8);
        if (data is not null)
        {
            output.Write(data);
        }
        else
        {
            var sink = new SvgPathWriter(output, TShape.Digits);
            shape.Write(ref sink);
        }

        Raw(output, "\"/>"u8);
    }

    public void Circle(double cx, double cy, double r, in Paint paint, ReadOnlySpan<byte> classes)
    {
        if (_layer == HoverMarkup.Layer.Tips && HoverMarkup.Dot(output, cx, cy, r, paint, classes, _theme))
        {
            return;
        }

        var step = Take();
        Raw(output, "<circle"u8);
        Attribute(output, " r"u8, r);
        Paint(paint);
        Attribute(output, " cx"u8, cx);
        Attribute(output, " cy"u8, cy);
        if (step.Kind == StepKind.None)
        {
            Classes(classes);
        }
        else
        {
            SvgMotion.Classes(output, classes, step, series: false, late: false);
            SvgMotion.Attributes(output, step, null);
        }

        ClipAttribute();
        _morph.Take(output);
        Raw(output, "/>"u8);
    }

    public readonly void Text(double x, double y, string text, in TextPaint paint, ReadOnlySpan<byte> classes) => ElementMarkup.Text(output, x, y, text, paint, classes);

    public readonly void ArcText(in ArcPath arc, string text, in TextPaint paint, ReadOnlySpan<byte> classes) => ElementMarkup.Arc(output, arc, text, paint, classes);

    public readonly void EndSurface()
    {
        if (!_inline)
        {
            Controls();
            Raw(output, "</svg>"u8);
        }
    }

    /// <summary>The surface's last children: the command buttons, then a brush's range inputs (after the categories in the tab order).</summary>
    private readonly void Controls()
    {
        if (Clicks is { } clicks && _plot is { } plot)
        {
            ClickMarkup.Write(output, Marks, clicks, plot, _followButtons);
        }

        if (_brushFields is var (brush, start, end))
        {
            Svg.BrushFields.Write(output, Marks, brush, brush.Spec.Stroke ?? _theme.BrushStroke, start, end);
        }
    }

    public readonly void Legend(LegendLayout legend) => LegendMarkup.Write(output, legend, _inline, Marks, _plot is { } plot ? (plot.Spec.Width, plot.Spec.Height) : default);

    public void Rect(double x, double y, double width, double height, in Paint paint, ReadOnlySpan<byte> classes)
    {
        Raw(output, "<rect"u8);
        Classes(classes);
        ClipAttribute();
        Paint(paint);
        Attribute(output, " x"u8, x);
        Attribute(output, " y"u8, y);
        Attribute(output, " width"u8, width);
        Attribute(output, " height"u8, height);
        Raw(output, "/>"u8);
    }

    public void Hit(double x, double y, double width, double height)
    {
        var target = HitOutput;
        Raw(target, "<rect"u8);
        HitClass();
        Attribute(target, " x"u8, x);
        Attribute(target, " y"u8, y);
        Attribute(target, " width"u8, width);
        Attribute(target, " height"u8, height);
        Raw(target, "/>"u8);
        if (Bare)
        {
            _hover!.CloseBareHit();
        }
    }

    public void HitShape<TShape>(in TShape shape)
        where TShape : struct, IShape
    {
        var target = HitOutput;
        Raw(target, "<path"u8);
        HitClass();
        Raw(target, " d=\""u8);
        var sink = new SvgPathWriter(target, TShape.Digits);
        shape.Write(ref sink);
        Raw(target, "\"/>"u8);
        if (Bare)
        {
            _hover!.CloseBareHit();
        }
    }

    public readonly void Tooltip(double x, double y, string label, ReadOnlySpan<TooltipEntry> entries, TooltipSpec spec, in TipFollow follow) =>
        HoverMarkup.Tooltip(output, x, y, label, entries, spec, follow);

    public readonly void PlainText(double x, double y, string? text, double fontSize, string? fill, string? stroke) =>
        ItemMarkup.PlainText(output, x, y, text, fontSize, fill, stroke);

    /// <summary>Recharts' nest index, written after the surface once it ends (a responsive chart's surface ends last).</summary>
    public void NestIndex(double top, double width, double height, NestBox[] trail, Specs.ContentSpec? content) => _nest = (trail, content);

    public void OpenItem(string name)
    {
        _depth++;
        _motion += _motion > 0 ? 1 : 0;
        ItemMarkup.Item(output, name);
    }

    public readonly void OptionsText(double x, double y, string text, TextOptionsSpec options) => ItemMarkup.OptionsText(output, x, y, text, options, _theme.LabelText);

    public readonly void End()
    {
        if (!_compact)
        {
            if (_inline)
            {
                Controls();
                Raw(output, "</svg>"u8);
            }

            if (_nest is var (trail, content))
            {
                ItemMarkup.NestIndex(output, trail, _zoom, Marks, _theme, content, _plot?.Spec.Treemap?.ZoomChoice == true);
            }

            Raw(output, "</div>"u8);
        }

        output.Flush();
    }

    public readonly void DefineClip(in ClipRect clip) => SurfaceMarkup.DefineClip(output, clip);

    public readonly void OpenSurface(double x, double y, double width, double height) => SurfaceMarkup.OpenSurface(output, x, y, width, height);

    public readonly void CloseSurface() => Raw(output, "</svg>"u8);

    public void ClipNext(in ClipRect clip) => (_clipped, _clip) = (true, clip);

    /// <summary>
    /// Custom content for one item: its compiled nodes with the item's holes filled, or, for a template the compiler
    /// refused, the nodes Avalonia drew for that call; a pending movement or clip wraps it in a group.
    /// </summary>
    public void Content(ContentSpec content, in Content.ContentItem item)
    {
        var step = Take();
        var wrapped = step.Kind != StepKind.None || _clipped;
        if (wrapped)
        {
            Raw(output, "<g"u8);
            if (step.Kind != StepKind.None)
            {
                SvgMotion.Classes(output, default, step, series: false, late: false);
                SvgMotion.Attributes(output, step, null);
            }

            ClipAttribute();
            Raw(output, ">"u8);
        }

        Pictor.Content.ContentMarkup.Write(output, content.Refused is null ? content.Nodes : Pictor.Content.ContentResolution.Nodes(_plot?.Spec, item.Key), item, Variant);
        if (wrapped)
        {
            Raw(output, "</g>"u8);
        }
    }

    /// <summary>A tooltip's or a legend's custom content as HTML in a foreign object at a point, its wrapper shrunk to its content as Recharts' wrapper is.</summary>
    public readonly void Html(double x, double y, ContentSpec content, in Content.ContentItem item, TooltipSpec tooltip, in TipFollow follow, TooltipLines? lines) =>
        HoverMarkup.Content(output, x, y, content.Refused is null ? content.Nodes : Pictor.Content.ContentResolution.Nodes(_plot?.Spec, item.Key), item, tooltip, follow, lines);

    /// <summary>A brush's range inputs, written at the end of the surface (<see cref="EndSurface"/>) so that the categories' focus comes first in the tab order, as Recharts' surface does before its brush's travellers.</summary>
    public void BrushFields(BrushLayout brush, string startText, string endText) => _brushFields = (brush, startText, endText);

    /// <summary>Writes the pending clip's reference on the element being written, once.</summary>
    public void TabIndex(int index) => _tab = index;

    private void ClipAttribute()
    {
        if (_tab is { } tab)
        {
            _tab = null;
            Attribute(output, " tabindex"u8, tab);
        }

        if (!_clipped)
        {
            return;
        }

        _clipped = false;
        Raw(output, " clip-path=\"url(#"u8);
        SurfaceMarkup.ClipId(output, _clip);
        Raw(output, ")\""u8);
    }

    private readonly void Paint(in Paint paint) => ElementMarkup.Paint(output, paint, Variant);

    private readonly void Classes(ReadOnlySpan<byte> classes)
    {
        if (!classes.IsEmpty)
        {
            Raw(output, " class=\""u8);
            Raw(output, classes);
            Raw(output, "\""u8);
        }
    }
}
