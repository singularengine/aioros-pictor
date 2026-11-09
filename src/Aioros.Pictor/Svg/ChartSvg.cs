using System.Buffers;
using System.Globalization;
using Aioros.Pictor.BoundValues;
using Aioros.Pictor.Data;
using Aioros.Pictor.Interaction;
using Aioros.Pictor.Painting;
using Aioros.Pictor.Specs;
using Aioros.Runtime.Rendering;

namespace Aioros.Pictor.Svg;

/// <summary>Writes a chart as Recharts' markup, straight into a UTF-8 buffer.</summary>
public static class ChartSvg
{
    /// <summary>Writes the chart a spec describes for the given data.</summary>
    /// <param name="output">Where the markup goes.</param>
    /// <param name="spec">The chart.</param>
    /// <param name="data">The bound data: a list of items.</param>
    /// <param name="members">How the items' members are read.</param>
    public static void Write(IBufferWriter<byte> output, ChartSpec spec, object? data, DataMembers members)
    {
        if (!Marked(spec))
        {
            Paint(MarkupBuffer.Over(output), spec, ChartData.From(data, members));
            return;
        }

        using var buffer = new PooledBuffer(ChartOutput.RentedCapacity);
        var markup = MarkupBuffer.Over(buffer);
        var marks = new ChartMarks(markup);
        Paint(markup, spec, ChartData.From(data, members), marks);
        ChartMarks.Write(output, buffer.WrittenSpan, marks.ToArray());
    }

    /// <summary>The click targets a chart writes as command buttons: none unless it is written with marks and one of its click commands is set.</summary>
    private static List<ClickTarget>? Clicks(IChartPlot plot, ChartData data, ChartMarks? marks) =>
        marks is not null && Interactions.Clicks(plot.Spec) ? ClickTargets.Of(plot, data) : null;

    /// <summary>Whether the chart writes fields or commands, whose attributes the framework writes at marks of its markup.</summary>
    internal static bool Marked(ChartSpec spec) => Interactions.Marked(spec);

    /// <summary>
    /// Writes the chart for data already read: once, or, for a responsive chart with <see cref="ChartSpec.ResponsiveWidths"/>,
    /// once per width (at its aspect ratio's height, for a chart that keeps one) inside a container whose queries show the layout nearest the width the chart gets (as a picture's
    /// sources let the browser pick the image nearest its size). Its sync rules go to <paramref name="page"/> when given.
    /// The markup is committed to the output when it returns.
    /// </summary>
    internal static void Paint(MarkupBuffer output, ChartSpec spec, ChartData data, ChartMarks? marks = null, List<SyncRules>? page = null)
    {
        if (!spec.Responsive || spec.ResponsiveWidths.IsDefaultOrEmpty)
        {
            var plot = Layout.ChartFit.Compute(spec, data);
            var canvas = new SvgCanvas(output) { Marks = marks, Clicks = Clicks(plot, data, marks), PageStyles = page };
            Plots.Paint(ref canvas, plot, ChartPainter.AllTips);
            output.Flush();
            return;
        }

        var widths = spec.ResponsiveWidths.Where(width => width > 0).Distinct().Order().ToArray();
        var ranges = new string[widths.Length];
        Container(output, spec);
        Utf8Markup.Raw(output, "<style>.pictor-responsive{container-type:inline-size;position:relative;width:100%;height:100%}"u8);
        for (var i = 0; i < widths.Length; i++)
        {
            var low = i == 0 ? 0 : Math.Round((widths[i - 1] + widths[i]) / 2);
            var high = i == widths.Length - 1 ? double.PositiveInfinity : Math.Round((widths[i] + widths[i + 1]) / 2);
            ranges[i] = string.Create(CultureInfo.InvariantCulture, $"pictor-w{low}-{(double.IsInfinity(high) ? "up" : high)}");
            Utf8Markup.Css(output, Outside(ranges[i], low, high));
        }

        Utf8Markup.Raw(output, "</style>"u8);
        var written = -1;
        for (var i = 0; i < widths.Length; i++)
        {
            var plot = Layout.ChartFit.Compute(spec with { Width = widths[i], Height = Height(spec, widths[i]), Aspect = spec.FixedHeight ? widths[i] / spec.Height : spec.Aspect }, data);
            var canvas = new SvgCanvas(output) { Variant = ranges[i], Marks = marks, Clicks = Clicks(plot, data, marks), Repeated = plot.HoverCount <= written, PageStyles = page };
            written = Math.Max(written, plot.HoverCount);
            Plots.Paint(ref canvas, plot, ChartPainter.AllTips);
        }

        Utf8Markup.Raw(output, "</div>"u8);
        output.Flush();
    }

    /// <summary>A layout's height at a width: the width over the chart's aspect ratio (within its maximum height), as its wrapper's <c>aspect-ratio</c> sizes it, or the chart's height.</summary>
    private static double Height(ChartSpec spec, double width) =>
        spec.Aspect is { } aspect and > 0 ? Math.Max(spec.MinHeight ?? 0, Math.Min(width / aspect, spec.MaxHeight ?? double.PositiveInfinity)) : spec.Height;

    /// <summary>
    /// The rules taking a layout away while its container is outside the layout's range: out of the flow, clipped to nothing
    /// and hidden with all its descendants, but rendered, so its animations run at load with the others' and crossing a
    /// breakpoint shows it finished (a browser restarts every CSS animation of an element shown from <c>display:none</c>).
    /// </summary>
    private static string Outside(string range, double low, double high)
    {
        var condition = (low > 0, double.IsFinite(high)) switch
        {
            (false, false) => null,
            (false, true) => string.Create(CultureInfo.InvariantCulture, $"(width >= {high}px)"),
            (true, false) => string.Create(CultureInfo.InvariantCulture, $"(width < {low}px)"),
            (true, true) => string.Create(CultureInfo.InvariantCulture, $"(width < {low}px) or (width >= {high}px)"),
        };
        if (condition is null)
        {
            return string.Empty;
        }

        var wrapper = ".pictor-responsive>.recharts-wrapper." + range;
        return "@container " + condition + "{" + wrapper + "{position:absolute!important;top:0;left:0;width:0!important;height:0!important;overflow:hidden;visibility:hidden}" + wrapper + " *{visibility:hidden!important}}";
    }

    /// <summary>The container of a responsive chart's layouts, with Recharts' <c>ResponsiveContainer</c> <c>id</c>, <c>className</c> and <c>style</c>.</summary>
    private static void Container(MarkupBuffer output, ChartSpec spec)
    {
        Utf8Markup.Raw(output, "<div class=\"pictor-responsive"u8);
        if (ClassNames.Tokens(spec.ContainerClass) is { } className)
        {
            Utf8Markup.Raw(output, " "u8);
            Utf8Markup.Text(output, className);
        }

        Utf8Markup.Raw(output, "\""u8);
        if (spec.ContainerId is { } id)
        {
            Utf8Markup.Attribute(output, " id"u8, id);
        }

        if (spec.ContainerStyle is { } style)
        {
            Utf8Markup.Attribute(output, " style"u8, style);
        }

        Utf8Markup.Raw(output, ">"u8);
    }
}

/// <summary>
/// The renderer Aioros calls at request time for every Pictor chart of a compiled page: the chart's spec, recorded as
/// JSON when the page was traced, is read once per plan; each request writes the chart for the bound data, or writes the
/// same markup again when the data holds the same values as the last time (<see cref="ChartOutput"/>), with the bound
/// values of the chart's children (<see cref="SpecHoles"/>) and the fields and commands its markup carries.
/// </summary>
public sealed class ChartRenderer : ISlotRenderer
{
    /// <inheritdoc />
    public static SlotRender Create(string argument, MemberLookup members) => Create(argument, members, null);

    /// <summary>The renderer of an argument whose bound values are read through <paramref name="plain"/> (Avalonia's colors, brushes and thicknesses, by Pictor.Avalonia's renderer).</summary>
    internal static SlotRender Create(string argument, MemberLookup members, PlainValue? plain)
    {
        DataMembers read = (owner, member) => members(owner, member);
        var holes = SpecHoles.Read(argument, read, plain);
        if (holes.IsEmpty)
        {
            var output = new ChartOutput(holes.Spec, read);
            return output.Write;
        }

        return holes.Write;
    }

    /// <summary>
    /// Decodes a brush's slide (<see cref="BrushFields.Slide"/>, a composite of its two indexes): the posted value carries the
    /// item count, the start it was written at and the new start (<see cref="BrushFields.SlideBase"/>), and the end moves by the same amount, as
    /// Recharts' slide drag moves both travellers, the window kept inside the data (an unset end is the last item); a slide posted at the
    /// start it was written at (another field changed) and anything else are refused. A brush with a gap posts its travellers and
    /// its slide under keys naming the gap, decoded to the window Recharts' <c>getIndex</c> snaps them to (<see cref="BrushFields.Moved"/>).
    /// </summary>
    /// <param name="key">The composite's key.</param>
    /// <param name="posted">The posted value.</param>
    /// <param name="current">The members' current values: the start and the end index.</param>
    /// <param name="decoded">The members' new values.</param>
    /// <returns>Whether the post is decoded.</returns>
    public static bool Decode(string key, string posted, ReadOnlySpan<object?> current, Span<string?> decoded)
    {
        if (current.Length != 2 || decoded.Length != 2
            || !long.TryParse(posted, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value)
            || BrushFields.Moved(key, value, current[0], current[1]) is not { } window)
        {
            return false;
        }

        decoded[0] = window.Start.ToString(System.Globalization.CultureInfo.InvariantCulture);
        decoded[1] = window.End.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return true;
    }
}
