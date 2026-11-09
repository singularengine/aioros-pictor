using Aioros.Pictor.Data;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Svg;

namespace Aioros.Pictor.Tests.Audit;

public sealed partial class BrushAuditTests
{
    private static string Indexes(string html) => Composite().Replace(html, match =>
    {
        var min = long.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        string Index(int group) => (long.Parse(match.Groups[group].Value, System.Globalization.CultureInfo.InvariantCulture) - min).ToString(System.Globalization.CultureInfo.InvariantCulture);
        return $"class=\"{match.Groups[1].Value}\" min=\"0\" max=\"{Index(3)}\"{match.Groups[4].Value} value=\"{Index(5)}\"";
    });

    [System.Text.RegularExpressions.GeneratedRegex("class=\"(pictor-b[01])\" style=\"[^\"]*\" min=\"(\\d+)\" max=\"(\\d+)\"((?: step=\"\\d+\")?) value=\"(\\d+)\"")]
    private static partial System.Text.RegularExpressions.Regex Composite();

    [Fact]
    [Trait("AC", "AUDIT-06")]
    public void TheSlideIsDecodedFromTheClampedWindow()
    {
        string?[] Decoded(long posted, object? start, object? end)
        {
            var decoded = new string?[2];
            return ChartRenderer.Decode(BrushFields.Slide, posted.ToString(System.Globalization.CultureInfo.InvariantCulture), [start, end], decoded) ? decoded : [];
        }

        static long Slide(long count, long written, long moved) => (((count * BrushFields.SlideBase) + written) * BrushFields.SlideBase) + moved;
        Assert.Equal(["1", "8"], Decoded(Slide(10, 2, 1), 2, null));
        Assert.Equal(["6", "9"], Decoded(Slide(10, 2, 9), 2, 5));
        Assert.Equal(["6", "9"], Decoded(Slide(10, 2, BrushFields.SlideBase - 1), 2, 5));
        Assert.Equal(["3", "4"], Decoded(Slide(10, 8, 3), 8, 100));
        Assert.Equal(["0", "3"], Decoded(Slide(10, 2, 0), 5, 2));
        Assert.Equal(["0", "9"], Decoded(Slide(10, 0, 4), null, null));
        Assert.Empty(Decoded(Slide(10, 1, 1), 3, 5));

        var refused = new string?[2];
        Assert.False(ChartRenderer.Decode(BrushFields.Slide, "5", [2, 5], refused));
        Assert.False(ChartRenderer.Decode(BrushFields.Slide, "-400000000005", [2, 5], refused));
        Assert.False(ChartRenderer.Decode(BrushFields.Slide, "1.5", [2, 5], refused));
        Assert.False(ChartRenderer.Decode(BrushFields.Slide, "99999999999999999999", [2, 5], refused));
        Assert.False(ChartRenderer.Decode(BrushFields.Start, Slide(10, 2, 3).ToString(System.Globalization.CultureInfo.InvariantCulture), [2, 5], refused));
    }

    [Fact]
    [Trait("AC", "AUDIT-07")]
    public async Task TheEndInputCanReachTheLastItemWithAGapAsync()
    {
        var example = await Reference.ExampleAsync("BrushBarChart");
        var rows = Reference.Rows(example);

        string Fields(int count)
        {
            var spec = example.Spec with { Brush = example.Spec.Brush! with { Fields = true, Gap = 3, StartIndex = null, EndIndex = null } };
            var render = ChartRenderer.Create(Specs.SpecJson.Write(spec), static (_, _) => null!);
            return Indexes(Slots.Render(render, rows.Take(count).ToList(), new Dictionary<string, object?>()).Html);
        }

        var eleven = Fields(11);
        var ten = Fields(10);

        Assert.Contains("class=\"pictor-b0\" min=\"0\" max=\"10\" step=\"3\" value=\"0\"", eleven, StringComparison.Ordinal);
        Assert.Contains("class=\"pictor-b1\" min=\"0\" max=\"10\" value=\"10\"", eleven, StringComparison.Ordinal);
        Assert.Contains("class=\"pictor-b0\" min=\"0\" max=\"9\" step=\"3\" value=\"0\"", ten, StringComparison.Ordinal);
        Assert.Contains("class=\"pictor-b1\" min=\"0\" max=\"9\" step=\"3\" value=\"9\"", ten, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AUDIT-37")]
    public async Task AnInputOffItsStepsIsWrittenWithoutThemAsync()
    {
        var example = await Reference.ExampleAsync("BrushBarChart");
        var rows = Reference.Rows(example);

        string Fields(int? start, int? end)
        {
            var spec = example.Spec with { Brush = example.Spec.Brush! with { Fields = true, Gap = 5, StartIndex = start, EndIndex = end } };
            var render = ChartRenderer.Create(Specs.SpecJson.Write(spec), static (_, _) => null!);
            return Indexes(Slots.Render(render, rows.Take(31).ToList(), new Dictionary<string, object?>()).Html);
        }

        var off = Fields(3, 17);
        var on = Fields(5, 30);

        Assert.Contains("class=\"pictor-b0\" min=\"0\" max=\"30\" value=\"3\"", off, StringComparison.Ordinal);
        Assert.Contains("class=\"pictor-b1\" min=\"0\" max=\"30\" value=\"17\"", off, StringComparison.Ordinal);
        Assert.DoesNotContain("step=", off, StringComparison.Ordinal);
        Assert.Contains("class=\"pictor-b0\" min=\"0\" max=\"30\" step=\"5\" value=\"5\"", on, StringComparison.Ordinal);
        Assert.Contains("class=\"pictor-b1\" min=\"0\" max=\"30\" step=\"5\" value=\"30\"", on, StringComparison.Ordinal);
        Assert.Matches("class=\"pictor-bs\" min=\"\\d+\" max=\"\\d+\" step=\"5\"", on);
    }

    [Fact]
    [Trait("AC", "AUDIT-08")]
    public async Task ATravellerOfANarrowBrushIsDraggedWithoutThrowingAsync()
    {
        var example = await Reference.ExampleAsync("BrushBarChart");
        var spec = example.Spec with { Brush = example.Spec.Brush! with { TravellerWidth = 1000 } };
        var brush = ChartLayout.Compute(spec, ChartData.From(example.Data, static (_, _) => null)).Brush!;
        var drag = BrushDrag.Start(brush, brush.StartX + 1, brush.Y + 1)!;

        var window = drag.Move(brush, brush.StartX + 50);

        Assert.Equal(brush.X, drag.StartX);
        Assert.Equal(0, window.Start);
    }
}
