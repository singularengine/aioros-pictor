using System.Buffers;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>
/// An axis tooltip following the pointer on the web, as Recharts places it when <c>position</c> leaves the coordinate
/// free (<c>getTooltipTranslateXY</c>): the pointer plus the offset, flipped to the pointer's other side when the box
/// would leave the view box, never before the view box's start; <c>reverseDirection</c> starts on the other side and
/// <c>allowEscapeViewBox</c> never flips. The surface carries the framework's pointer variables in its own units
/// (<c>data-aioros-pointer="viewbox"</c>), the hover layer a class naming the rule and the view box and offset as custom
/// properties, and one rule of the chart's style block moves every box of the chart (each keeps its category's place
/// along the other coordinate): without a pointer (the default index, the keyboard, a finger, a synced chart) the box
/// stands where Recharts' <c>combineCoordinateForDefaultIndex</c> places it.
/// </summary>
internal sealed class PointerFollow(TipFollow follow, TooltipSpec spec, Layout.PolarLayout? polar = null)
{

    private bool AlongX => follow.Axis == TipAxis.X;

    private bool Reverse => AlongX ? spec.ReverseX : spec.ReverseY;

    private bool Escape => AlongX ? spec.AllowEscapeX : spec.AllowEscapeY;

    /// <summary>How a chart's boxes follow the pointer on the web, or null when they do not (<see cref="Plots.FollowsPointer"/>).</summary>
    public static PointerFollow? Of(IChartPlot plot) => !Plots.FollowsPointer(plot) ? null : plot switch
    {
        Layout.ChartLayout cartesian => new PointerFollow(TipPainter.Follow(cartesian), plot.Spec.Tooltip!),
        Layout.PolarLayout polar => new PointerFollow(PolarTips.Followed(polar, 0, null).Follow, plot.Spec.Tooltip!, polar),
        _ => null,
    };

    /// <summary>The surface's attribute asking the framework for the pointer's position in the surface's units.</summary>
    public static void Surface(MarkupBuffer output) => Raw(output, " data-aioros-pointer=\"viewbox\""u8);

    /// <summary>The hover layer's class (the end of its class attribute) and custom properties: the view box along the coordinate and the offset.</summary>
    public void Layer(MarkupBuffer output)
    {
        if (polar is not null)
        {
            PolarLayer(output, polar);
            return;
        }

        Raw(output, " "u8);
        Class(output);
        Raw(output, "\" style=\"--pictor-fs:"u8);
        Number(output, follow.Start);
        Raw(output, "px;--pictor-fe:"u8);
        Number(output, follow.Start + follow.Length);
        Raw(output, "px;--pictor-fo:"u8);
        Number(output, spec.Offset);
        Raw(output, "px;--pictor-fr:"u8);
        Number(output, follow.Rest);
        Raw(output, "px\""u8);
    }

    /// <summary>The rule moving the boxes of a hover layer with this class: the pointer's coordinate through Recharts' placement.</summary>
    public void Rule(MarkupBuffer output)
    {
        if (polar is not null)
        {
            PolarRule(output);
            return;
        }

        Raw(output, "."u8);
        Class(output);
        Raw(output, spec.Content is null ? " .recharts-default-tooltip"u8 : " .pictor-content"u8);
        Raw(output, AlongX ? "{--pictor-p:var(--aioros-pointer-x,var(--pictor-fr));translate:"u8 : "{--pictor-p:var(--aioros-pointer-y,var(--pictor-fr));translate:0 "u8);
        Raw(output, (Escape, Reverse) switch
        {
            (true, false) => "calc(var(--pictor-p) + var(--pictor-fo))"u8,
            (true, true) => "calc(var(--pictor-p) - var(--pictor-fo) - 100%)"u8,
            (false, true) => "max(var(--pictor-fs),calc(var(--pictor-p) - var(--pictor-fo) - 100% + clamp(0px,(var(--pictor-fs) - var(--pictor-p) + var(--pictor-fo) + 100%)*1e5,2*var(--pictor-fo) + 100%)))"u8,
            _ => "max(var(--pictor-fs),calc(var(--pictor-p) + var(--pictor-fo) - clamp(0px,(var(--pictor-p) + var(--pictor-fo) + 100% - var(--pictor-fe))*1e5,2*var(--pictor-fo) + 100%)))"u8,
        });
        Raw(output, AlongX ? " 0}"u8 : "}"u8);
    }

    /// <summary>A following box's own custom properties on its <c>foreignObject</c>: its category's direction (a centric chart's) or radius (a radial one's).</summary>
    public static void Tip(MarkupBuffer output, in TipFollow follow)
    {
        if (follow.Axis == TipAxis.Radius)
        {
            var (u, v) = Layout.Polar.Point(0, 0, 1, follow.Param);
            Raw(output, "--pictor-u:"u8);
            Number(output, Math.Round(u, 6));
            Raw(output, ";--pictor-v:"u8);
            Number(output, Math.Round(v, 6));
            return;
        }

        Raw(output, "--pictor-r:"u8);
        Number(output, follow.Param);
        Raw(output, "px"u8);
    }

    /// <summary>A polar chart's hover layer: its rule's class, the center, the view box along both coordinates, the offset, and where the boxes stand without a pointer (half the outer radius, or the start angle's direction).</summary>
    private void PolarLayer(MarkupBuffer output, Layout.PolarLayout layout)
    {
        Raw(output, follow.Axis == TipAxis.Radius ? " pictor-fp\" style=\"--pictor-cx:"u8 : " pictor-fa\" style=\"--pictor-cx:"u8);
        Number(output, layout.Cx);
        Raw(output, "px;--pictor-cy:"u8);
        Number(output, layout.Cy);
        Raw(output, "px;--pictor-xs:"u8);
        Number(output, layout.Left);
        Raw(output, "px;--pictor-xe:"u8);
        Number(output, layout.Left + layout.PlotWidth);
        Raw(output, "px;--pictor-ys:"u8);
        Number(output, layout.Top);
        Raw(output, "px;--pictor-ye:"u8);
        Number(output, layout.Top + layout.PlotHeight);
        Raw(output, "px;--pictor-fo:"u8);
        Number(output, spec.Offset);
        if (follow.Axis == TipAxis.Radius)
        {
            Raw(output, "px;--pictor-rest:"u8);
            Number(output, follow.Rest);
            Raw(output, "px\""u8);
            return;
        }

        var (u, v) = Layout.Polar.Point(0, 0, 1, follow.Rest);
        Raw(output, "px;--pictor-u0:"u8);
        Number(output, Math.Round(u, 6));
        Raw(output, ";--pictor-v0:"u8);
        Number(output, Math.Round(v, 6));
        Raw(output, "\""u8);
    }

    /// <summary>
    /// The rule placing a polar chart's boxes: the pointer's distance from the center along the box's direction, or the
    /// pointer's angle (<c>atan2</c>) on the box's radius, then Recharts' placement along each coordinate in the view box.
    /// </summary>
    private void PolarRule(MarkupBuffer output)
    {
        Raw(output, follow.Axis == TipAxis.Radius
            ? ".pictor-fp .recharts-default-tooltip{--pictor-d:hypot(calc(var(--aioros-pointer-x,calc(var(--pictor-cx) + var(--pictor-rest))) - var(--pictor-cx)),calc(var(--aioros-pointer-y,var(--pictor-cy)) - var(--pictor-cy)));--pictor-x:calc(var(--pictor-cx) + var(--pictor-d) * var(--pictor-u));--pictor-y:calc(var(--pictor-cy) + var(--pictor-d) * var(--pictor-v));translate:"u8
            : ".pictor-fa .recharts-default-tooltip{--pictor-t:atan2(calc(var(--aioros-pointer-y,calc(var(--pictor-cy) + var(--pictor-v0) * 1px)) - var(--pictor-cy)),calc(var(--aioros-pointer-x,calc(var(--pictor-cx) + var(--pictor-u0) * 1px)) - var(--pictor-cx)));--pictor-x:calc(var(--pictor-cx) + var(--pictor-r) * cos(var(--pictor-t)));--pictor-y:calc(var(--pictor-cy) + var(--pictor-r) * sin(var(--pictor-t)));translate:"u8);
        Raw(output, "max(var(--pictor-xs),calc(var(--pictor-x) + var(--pictor-fo) - clamp(0px,(var(--pictor-x) + var(--pictor-fo) + 100% - var(--pictor-xe))*1e5,2*var(--pictor-fo) + 100%))) max(var(--pictor-ys),calc(var(--pictor-y) + var(--pictor-fo) - clamp(0px,(var(--pictor-y) + var(--pictor-fo) + 100% - var(--pictor-ye))*1e5,2*var(--pictor-fo) + 100%)))}"u8);
    }

    private void Class(MarkupBuffer output)
    {
        Raw(output, AlongX ? "pictor-fx"u8 : "pictor-fy"u8);
        Raw(output, Escape ? "e"u8 : default);
        Raw(output, Reverse ? "r"u8 : default);
    }
}
