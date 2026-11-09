using System.Buffers;
using Aioros.Pictor.Motion;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>
/// Recharts' enter animations as CSS: one style block whose rules mean the same thing in every chart of a page (a
/// series' timing travels in its own custom properties, a shape's center or angle in its own), applied only when the
/// reader has not asked for reduced motion; each animated series' layer, shape and late part carries its class.
/// </summary>
internal static class SvgMotion
{
    /// <summary>
    /// The rules every animated chart needs: the default timing, the parts shown once their series is in, and the update
    /// animation of a chart whose markup the framework morphs in place (its elements kept): a curve's, a bar's, a
    /// trapezoid's, a polygon's and a symbol's path and a dot's center move from their previous values by the series'
    /// timing (CSS transitions of <c>d</c>, <c>cx</c> and <c>cy</c>).
    /// </summary>
    public static ReadOnlySpan<byte> Base =>
        "<style>@media (prefers-reduced-motion:no-preference){.pictor-a{--pb:0ms;--pd:1500ms;--pe:ease}.pictor-late{animation:pictor-late 0s calc(var(--pb) + var(--pd) + 112ms) backwards}@keyframes pictor-late{from{visibility:hidden}}.pictor-a :is(.recharts-curve,.recharts-rectangle,.recharts-trapezoid,.recharts-polygon,.recharts-symbols):not([data-aioros-morph]){transition:d var(--pd) var(--pe) calc(var(--pb) + 80ms)}.pictor-a circle:not([data-aioros-morph]){transition:cx var(--pd) var(--pe) calc(var(--pb) + 80ms),cy var(--pd) var(--pe) calc(var(--pb) + 80ms)}.pictor-late{transition:visibility 0s 32ms}.pictor-a:has([data-aioros-morphing]) .pictor-late:not(:has([data-aioros-morph])){visibility:hidden;transition:none}[data-aioros-morphing]{transform-box:view-box;transform-origin:0 0}}</style>"u8;

    /// <summary>The rules of one movement, written the first time a chart uses it (a chart's few movements, not all of them).</summary>
    public static ReadOnlySpan<byte> Rules(StepKind kind) => kind switch
    {
        StepKind.Draw => "<style>@media (prefers-reduced-motion:no-preference){.pictor-draw{stroke-dasharray:1 1;animation:pictor-draw var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-draw{from{stroke-dashoffset:1}to{stroke-dashoffset:0}}}</style>"u8,
        StepKind.Reveal => "<style>@media (prefers-reduced-motion:no-preference){.pictor-reveal{animation:pictor-reveal var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-reveal{from{clip-path:inset(0 100% 0 0)}to{clip-path:inset(0)}}}</style>"u8,
        StepKind.RevealDown => "<style>@media (prefers-reduced-motion:no-preference){.pictor-reveal-down{animation:pictor-reveal-down var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-reveal-down{from{clip-path:inset(0 0 100% 0)}to{clip-path:inset(0)}}}</style>"u8,
        StepKind.GrowUp => "<style>@media (prefers-reduced-motion:no-preference){.pictor-grow-b{transform-box:fill-box;transform-origin:50% 100%;animation:pictor-grow-y var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-grow-y{from{transform:scaleY(0)}}}</style>"u8,
        StepKind.GrowDown => "<style>@media (prefers-reduced-motion:no-preference){.pictor-grow-t{transform-box:fill-box;transform-origin:50% 0;animation:pictor-grow-y var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-grow-y{from{transform:scaleY(0)}}}</style>"u8,
        StepKind.GrowRight => "<style>@media (prefers-reduced-motion:no-preference){.pictor-grow-l{transform-box:fill-box;transform-origin:0 50%;animation:pictor-grow-x var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-grow-x{from{transform:scaleX(0)}}}</style>"u8,
        StepKind.GrowLeft => "<style>@media (prefers-reduced-motion:no-preference){.pictor-grow-r{transform-box:fill-box;transform-origin:100% 50%;animation:pictor-grow-x var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-grow-x{from{transform:scaleX(0)}}}</style>"u8,
        StepKind.Scale => "<style>@media (prefers-reduced-motion:no-preference){.pictor-scale{transform-box:fill-box;transform-origin:50% 50%;animation:pictor-scale var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-scale{from{transform:scale(0)}}}</style>"u8,
        StepKind.ScaleArea => "<style>@property --pictor-s{syntax:'&lt;number&gt;';inherits:false;initial-value:1}@media (prefers-reduced-motion:no-preference){.pictor-scale-area{transform-box:fill-box;transform-origin:50% 50%;transform:scale(sqrt(var(--pictor-s)));animation:pictor-scale-area var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-scale-area{from{--pictor-s:0}to{--pictor-s:1}}}</style>"u8,
        StepKind.DrawDashed => "<style>@media (prefers-reduced-motion:no-preference){.pictor-dash-mask{stroke-dasharray:1 1;animation:pictor-dash var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-dash{from{stroke-dashoffset:1}to{stroke-dashoffset:0}}}[data-aioros-morphing][mask]{mask:none}</style>"u8,
        StepKind.Center => "<style>@media (prefers-reduced-motion:no-preference){.pictor-center{transform-box:view-box;animation:pictor-scale var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-scale{from{transform:scale(0)}}}</style>"u8,
        StepKind.Shown => "<style>.pictor-shown{visibility:hidden}@media (prefers-reduced-motion:no-preference){.pictor-shown{animation:pictor-shown calc(var(--pb) + var(--pd) + 112ms)}@keyframes pictor-shown{from,to{visibility:visible}}.pictor-arc{fill:none;stroke-dasharray:calc(var(--l) * 1px) 400px;stroke-dashoffset:calc(var(--b) * -1px);animation:pictor-arc var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-arc{from{stroke-dasharray:0px 400px;stroke-dashoffset:calc(var(--a) * -1px)}}.pictor-edge{transform-box:view-box;animation:pictor-edge var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-edge{from{transform:rotate(var(--r))}}}</style>"u8,
        StepKind.Converge => "<style>@media (prefers-reduced-motion:no-preference){.pictor-converge{animation:pictor-converge var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-converge{from{transform:translate(var(--dx),var(--dy))}}}</style>"u8,
        StepKind.Slide => "<style>@media (prefers-reduced-motion:no-preference){.pictor-slide{animation:pictor-slide var(--pd) var(--pe) calc(var(--pb) + 80ms) backwards}@keyframes pictor-slide{from{transform:translate(var(--tx),var(--tx))}}}</style>"u8,
        StepKind.Stroke => "<style>@media (prefers-reduced-motion:no-preference){.pictor-stroke{animation:pictor-stroke 1500ms ease 64ms backwards}.pictor-stroke-late{animation:pictor-stroke 1500ms ease calc(var(--pb) + var(--pd) + 176ms) backwards}@keyframes pictor-stroke{from{stroke-dasharray:0 1}to{stroke-dasharray:1 0}}}</style>"u8,
        _ => default,
    };

    /// <summary>
    /// The mask a dashed line is drawn in through (<see cref="StepKind.DrawDashed"/>): its own path, wider than its stroke,
    /// drawn along its length (<c>pictor-dash-mask</c>), so the line shows its own dashes up to the drawn length as
    /// Recharts' growing dash pattern does; named by a hash of the path and the width, so equal lines share one. Returns
    /// the name's number and the path's data, which the line writes too.
    /// </summary>
    public static (uint Mask, byte[] Data) DashMask<TShape>(MarkupBuffer output, in TShape shape, double width)
        where TShape : struct, Painting.IShape
    {
        var data = new ArrayBufferWriter<byte>(256);
        var sink = new Aioros.Graphs.Svg.SvgPathWriter(data, TShape.Digits);
        shape.Write(ref sink);
        width = double.IsNaN(width) || width <= 0 ? 1 : width;
        var hash = 2166136261u;
        foreach (var b in data.WrittenSpan)
        {
            hash = (hash ^ b) * 16777619u;
        }

        hash = (hash ^ (uint)Math.Round(width * 100)) * 16777619u;
        Raw(output, "<mask id=\"pictor-dm"u8);
        Number(output, hash);
        Raw(output, "\" maskUnits=\"userSpaceOnUse\" x=\"0\" y=\"0\" width=\"100%\" height=\"100%\"><path class=\"pictor-dash-mask\" pathLength=\"1\" fill=\"none\" stroke=\"#fff\""u8);
        Attribute(output, " stroke-width"u8, (2 * width) + 4);
        Raw(output, " d=\""u8);
        output.Write(data.WrittenSpan);
        Raw(output, "\"/></mask>"u8);
        return (hash, data.WrittenSpan.ToArray());
    }

    /// <summary>A dashed line's reference to its mask, when it is drawn in through one.</summary>
    public static void MaskAttribute(MarkupBuffer output, uint mask)
    {
        if (mask != 0)
        {
            Raw(output, " mask=\"url(#pictor-dm"u8);
            Number(output, mask);
            Raw(output, ")\""u8);
        }
    }

    /// <summary>Whether a layer inside an animated series is one Recharts renders once the animation ends: dots (a radar's move with its points), labels, error bars, a treemap's deeper nodes.</summary>
    public static bool IsLate(ReadOnlySpan<byte> classes) =>
        (classes.IndexOf("dots"u8) >= 0 && classes.IndexOf("radar-dots"u8) < 0) || classes.IndexOf("label"u8) >= 0 || classes.IndexOf("errorBar"u8) >= 0 || IsDeep(classes);

    /// <summary>Writes an element's class attribute: its own classes, the series' marker, its movement's and whether it shows late.</summary>
    public static void Classes(MarkupBuffer output, ReadOnlySpan<byte> classes, in Step step, bool series, bool late)
    {
        var name = Name(step);
        if (classes.IsEmpty && name.IsEmpty && !series && !late)
        {
            return;
        }

        Raw(output, " class=\""u8);
        var first = true;
        Append(output, classes, ref first);
        Append(output, series ? "pictor-a"u8 : default, ref first);
        Append(output, name, ref first);
        Append(output, late ? "pictor-late"u8 : default, ref first);
        Raw(output, "\""u8);
    }

    /// <summary>Writes what a movement needs on its element: a drawn line's path length, the center, angles or offset a shape moves by, the series' timing when it is not the default.</summary>
    public static void Attributes(MarkupBuffer output, in Step step, Timing? timing)
    {
        if (step.Kind is StepKind.Draw or StepKind.Stroke)
        {
            Raw(output, " pathLength=\"1\""u8);
        }

        var style = timing is { } value && (value.Begin != 0 || value.Duration != 1500 || value.Easing != AnimationEasing.Ease);
        if (!style && step.Kind is not (StepKind.Center or StepKind.Converge or StepKind.Slide))
        {
            return;
        }

        Raw(output, " style=\""u8);
        if (timing is { } t && style)
        {
            Raw(output, "--pb:"u8);
            Number(output, t.Begin);
            Raw(output, "ms;--pd:"u8);
            Number(output, t.Duration);
            Raw(output, "ms;--pe:"u8);
            Raw(output, Easing(t.Easing));
            Raw(output, ";"u8);
        }

        Geometry(output, step);
        Raw(output, "\""u8);
    }

    private static void Geometry(MarkupBuffer output, in Step step)
    {
        switch (step.Kind)
        {
            case StepKind.Center:
                Raw(output, "transform-origin:"u8);
                Number(output, step.Cx);
                Raw(output, "px "u8);
                Number(output, step.Cy);
                Raw(output, "px"u8);
                break;
            case StepKind.Converge:
                Raw(output, "--dx:"u8);
                Number(output, step.Cx);
                Raw(output, "px;--dy:"u8);
                Number(output, step.Cy);
                Raw(output, "px"u8);
                break;
            case StepKind.Slide:
                Raw(output, "--tx:"u8);
                Number(output, step.Offset);
                Raw(output, "px"u8);
                break;
        }
    }

    private static ReadOnlySpan<byte> Name(in Step step) => step.Kind switch
    {
        StepKind.Draw => "pictor-draw"u8,
        StepKind.Reveal => "pictor-reveal"u8,
        StepKind.RevealDown => "pictor-reveal-down"u8,
        StepKind.GrowUp => "pictor-grow-b"u8,
        StepKind.GrowDown => "pictor-grow-t"u8,
        StepKind.GrowRight => "pictor-grow-l"u8,
        StepKind.GrowLeft => "pictor-grow-r"u8,
        StepKind.Scale => "pictor-scale"u8,
        StepKind.ScaleArea => "pictor-scale-area"u8,
        StepKind.Center => "pictor-center"u8,
        StepKind.Shown => "pictor-shown"u8,
        StepKind.Converge => "pictor-converge"u8,
        StepKind.Slide => "pictor-slide"u8,
        StepKind.Stroke => step.Offset > 0 ? "pictor-stroke-late"u8 : "pictor-stroke"u8,
        _ => default,
    };

    /// <summary>Writes an arc that grows a sector (a circle at the sector's middle radius from its start angle, its dash moving and growing along a path length of 360) or its radial edges.</summary>
    public static void Arc(MarkupBuffer output, in GrowingArc arc)
    {
        if (arc.Part != ArcPart.Arc)
        {
            if (arc.Stroke is { } stroke && stroke != "none")
            {
                if (arc.Part == ArcPart.Edges)
                {
                    Edge(output, arc, stroke, arc.To, arc.To - arc.From);
                }

                Edge(output, arc, stroke, arc.To + arc.Length, arc.To + arc.Length - arc.From);
            }

            return;
        }

        Raw(output, "<path class=\"pictor-arc\" stroke=\""u8);
        Utf8Markup.Text(output, arc.Color);
        Raw(output, "\" stroke-width=\""u8);
        Number(output, arc.Width);
        Raw(output, "\" pathLength=\"360\" style=\"--a:"u8);
        Number(output, arc.From);
        Raw(output, ";--b:"u8);
        Number(output, arc.To);
        Raw(output, ";--l:"u8);
        Number(output, arc.Length);
        Raw(output, "\" d=\"M"u8);
        var (x, y) = Layout.Polar.Point(arc.Cx, arc.Cy, arc.Radius, arc.Angle);
        var (ox, oy) = Layout.Polar.Point(arc.Cx, arc.Cy, arc.Radius, arc.Angle + 180);
        var sweep = arc.Counterclockwise ? "0"u8 : "1"u8;
        Point(output, x, y);
        for (var i = 0; i < 2; i++)
        {
            Raw(output, "A"u8);
            Number(output, arc.Radius);
            Raw(output, ","u8);
            Number(output, arc.Radius);
            Raw(output, ",0,0,"u8);
            Raw(output, sweep);
            Raw(output, ","u8);
            Point(output, i == 0 ? ox : x, i == 0 ? oy : y);
        }

        Raw(output, "\"/>"u8);
    }

    /// <summary>A radial edge of a growing sector at its final offset along the arc, turned back by how far it travels (a screen rotation, clockwise for a counterclockwise pie).</summary>
    private static void Edge(MarkupBuffer output, in GrowingArc arc, string stroke, double offset, double travel)
    {
        var sign = arc.Counterclockwise ? 1 : -1;
        var angle = arc.Angle + (sign * offset);
        var (x1, y1) = Layout.Polar.Point(arc.Cx, arc.Cy, arc.Radius - (arc.Width / 2), angle);
        var (x2, y2) = Layout.Polar.Point(arc.Cx, arc.Cy, arc.Radius + (arc.Width / 2), angle);
        Raw(output, "<line class=\"pictor-edge\" stroke=\""u8);
        Utf8Markup.Text(output, stroke);
        Raw(output, "\" x1=\""u8);
        Number(output, x1);
        Raw(output, "\" y1=\""u8);
        Number(output, y1);
        Raw(output, "\" x2=\""u8);
        Number(output, x2);
        Raw(output, "\" y2=\""u8);
        Number(output, y2);
        Raw(output, "\" style=\"transform-origin:"u8);
        Number(output, arc.Cx);
        Raw(output, "px "u8);
        Number(output, arc.Cy);
        Raw(output, "px;--r:"u8);
        Number(output, sign * travel);
        Raw(output, "deg\"/>"u8);
    }

    private static void Point(MarkupBuffer output, double x, double y)
    {
        Number(output, x);
        Raw(output, ","u8);
        Number(output, y);
    }

    /// <summary>
    /// Writes a series layer's update animation for Aioros's morph: the kind its elements are rebuilt as, the proportional
    /// mapping of a line's, an area's and a radar's points, and the timing (Recharts' begin plus its 80 ms start).
    /// </summary>
    public static void MorphLayer(MarkupBuffer output, in MorphLayer layer)
    {
        if (layer.Kind != MorphKind.None)
        {
            Raw(output, " data-aioros-morph-path=\""u8);
            Raw(output, Path(layer.Kind, layer.Curve));
            Raw(output, "\""u8);
        }

        if (layer.Ratio)
        {
            Raw(output, " data-aioros-morph-map=\"ratio\""u8);
        }

        Raw(output, " data-aioros-morph-duration=\""u8);
        Number(output, layer.Timing.Duration);
        Raw(output, "\" data-aioros-morph-delay=\""u8);
        Number(output, layer.Timing.Begin + layer.Timing.Lag);
        if (layer.Timing.Easing != AnimationEasing.Ease)
        {
            Raw(output, "\" data-aioros-morph-easing=\""u8);
            Raw(output, Easing(layer.Timing.Easing));
        }

        Raw(output, "\""u8);
    }

    /// <summary>Writes one moving element's numbers, its kind when its layer's differs, its origin and the series' data key.</summary>
    public static void Morph(MarkupBuffer output, in MorphMark mark, MorphKind layerKind, byte layerCurve, uint? key)
    {
        if (mark.Kind != layerKind || mark.Curve != layerCurve)
        {
            Raw(output, " data-aioros-morph-path=\""u8);
            Raw(output, Path(mark.Kind, mark.Curve));
            Raw(output, "\""u8);
        }

        Raw(output, " data-aioros-morph=\""u8);
        if (mark.X is { } x && mark.Y is { } y)
        {
            Points(output, x, y, mark.BaseX, mark.BaseY);
        }
        else
        {
            Numbers(output, mark.Values, mark.Count, zero: true);
        }

        Raw(output, "\""u8);
        if (mark.FromCount > 0)
        {
            Raw(output, " data-aioros-morph-from=\""u8);
            Numbers(output, mark.From, mark.FromCount, zero: false);
            Raw(output, "\""u8);
        }

        if (key is { } value)
        {
            Raw(output, " data-aioros-morph-key=\""u8);
            Key(output, value);
            Raw(output, "\""u8);
        }
    }

    private static void Points(MarkupBuffer output, double[] x, double[] y, double[]? baseX, double[]? baseY)
    {
        var space = false;
        for (var i = 0; i < x.Length; i++)
        {
            Value(output, x[i], ref space);
            Value(output, y[i], ref space);
            if (baseX is not null && baseY is not null)
            {
                Value(output, i < baseX.Length ? baseX[i] : 0, ref space);
                Value(output, i < baseY.Length ? baseY[i] : 0, ref space);
            }
        }
    }

    private static void Key(MarkupBuffer output, uint value)
    {
        Span<byte> hex = stackalloc byte[8];
        for (var i = 0; i < 8; i++)
        {
            hex[i] = (byte)"0123456789abcdef"[(int)((value >> (28 - (4 * i))) & 0xF)];
        }

        Raw(output, hex);
    }

    private static void Numbers(MarkupBuffer output, in Six values, int count, bool zero)
    {
        var space = false;
        for (var i = 0; i < count; i++)
        {
            if (!zero && double.IsNaN(values[i]))
            {
                Raw(output, space ? " _"u8 : "_"u8);
                space = true;
                continue;
            }

            Value(output, values[i], ref space);
        }
    }

    private static void Value(MarkupBuffer output, double value, ref bool space)
    {
        if (space)
        {
            Raw(output, " "u8);
        }

        Number(output, double.IsNaN(value) ? 0 : value);
        space = true;
    }

    private static ReadOnlySpan<byte> Path(MorphKind kind, byte curve) => (kind, curve) switch
    {
        (MorphKind.Line, 1) => "line monotone-x"u8,
        (MorphKind.Line, 2) => "line monotone-y"u8,
        (MorphKind.Line, _) => "line"u8,
        (MorphKind.Band, 1) => "band monotone-x"u8,
        (MorphKind.Band, 2) => "band monotone-y"u8,
        (MorphKind.Band, _) => "band"u8,
        (MorphKind.Polygon, _) => "polygon"u8,
        (MorphKind.Rect, _) => "rect"u8,
        (MorphKind.Sector, _) => "sector"u8,
        (MorphKind.Glyph, _) => "glyph"u8,
        _ => "cx cy"u8,
    };

    private static ReadOnlySpan<byte> Easing(AnimationEasing easing) => easing switch
    {
        AnimationEasing.EaseIn => "ease-in"u8,
        AnimationEasing.EaseOut => "ease-out"u8,
        AnimationEasing.EaseInOut => "ease-in-out"u8,
        AnimationEasing.Linear => "linear"u8,
        _ => "ease"u8,
    };

    private static void Append(MarkupBuffer output, ReadOnlySpan<byte> name, ref bool first)
    {
        if (name.IsEmpty)
        {
            return;
        }

        if (!first)
        {
            Raw(output, " "u8);
        }

        Raw(output, name);
        first = false;
    }

    private static bool IsDeep(ReadOnlySpan<byte> classes)
    {
        var at = classes.IndexOf("treemap-depth-"u8);
        if (at < 0)
        {
            return false;
        }

        var depth = classes[(at + 14)..];
        return depth.Length > 1 ? depth[1] is >= (byte)'0' and <= (byte)'9' || depth[0] >= (byte)'3' : depth.Length == 1 && depth[0] >= (byte)'3';
    }
}
