using System.Buffers;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using static Aioros.Pictor.Svg.Utf8Markup;

namespace Aioros.Pictor.Svg;

/// <summary>The markup of a chart's surfaces: a responsive wrapper's size, nested surfaces and the clips elements are drawn in.</summary>
internal static class SurfaceMarkup
{
    /// <summary>A responsive wrapper's size: the whole box, or its width at the chart's aspect ratio, within its minimum and maximum sizes.</summary>
    public static void Responsive(MarkupBuffer output, ChartSpec spec)
    {
        if (spec.Aspect is { } aspect and > 0)
        {
            Raw(output, "width:100%;aspect-ratio:"u8);
            Number(output, aspect);
        }
        else
        {
            Raw(output, "width:100%;height:100%"u8);
        }

        Size(output, ";min-width:"u8, spec.MinWidth);
        Size(output, ";min-height:"u8, spec.MinHeight);
        Size(output, ";max-height:"u8, spec.MaxHeight);
    }

    private static void Size(MarkupBuffer output, ReadOnlySpan<byte> property, double? pixels)
    {
        if (pixels is { } value)
        {
            Raw(output, property);
            Number(output, value);
            Raw(output, "px"u8);
        }
    }

    public static void DefineClip(MarkupBuffer output, in ClipRect clip)
    {
        Raw(output, "<defs><clipPath id=\""u8);
        ClipId(output, clip);
        Raw(output, "\"><rect"u8);
        Attribute(output, " x"u8, clip.X);
        Attribute(output, " y"u8, clip.Y);
        Attribute(output, " width"u8, clip.Width);
        Attribute(output, " height"u8, clip.Height);
        Raw(output, "/></clipPath></defs>"u8);
    }

    public static void OpenSurface(MarkupBuffer output, double x, double y, double width, double height)
    {
        Raw(output, "<svg"u8);
        Attribute(output, " x"u8, x);
        Attribute(output, " y"u8, y);
        Raw(output, " class=\"recharts-surface\""u8);
        Attribute(output, " width"u8, width);
        Attribute(output, " height"u8, height);
        Raw(output, " viewBox=\"0 0 "u8);
        Number(output, width);
        Raw(output, " "u8);
        Number(output, height);
        Raw(output, "\"><title></title><desc></desc>"u8);
    }

    /// <summary>A clip's id named by its rectangle, so two clips with the same id on a page are the same clip.</summary>
    public static void ClipId(MarkupBuffer output, in ClipRect clip)
    {
        Raw(output, "pictor-clip-"u8);
        Number(output, clip.X);
        Raw(output, "_"u8);
        Number(output, clip.Y);
        Raw(output, "_"u8);
        Number(output, clip.Width);
        Raw(output, "_"u8);
        Number(output, clip.Height);
    }
}
