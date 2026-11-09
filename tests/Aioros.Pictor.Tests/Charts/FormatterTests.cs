using System.Globalization;
using Aioros.Pictor.Tests.Custom;
using Aioros.Pictor.Tests.Desktop;
using Avalonia.Data.Converters;

namespace Aioros.Pictor.Tests.Charts;

public sealed class FormatterTests(DesktopSession desktop)
{
    private static readonly IValueConverter Short = new FuncValueConverter<object?, string?>(static value => value is string text ? text[..4] : null);

    private static readonly IValueConverter Thousands = new Thousand();

    private static readonly IValueConverter Capitals = new FuncValueConverter<object?, string?>(static value => (value as string)?.ToUpperInvariant());

    private static readonly IValueConverter Kilograms = new Kilogram();

    private static readonly IValueConverter Page = new FuncValueConverter<object?, string?>(static value => "Page: " + value);

    [Fact]
    [Trait("AC", "AXES-12")]
    public async Task TickFormatterConvertersWriteTheTicksTextsAsync()
    {
        static void Axes(Chart chart, CustomTemplates templates)
        {
            var x = chart.Elements.OfType<XAxis>().Single();
            (x.TickFormat, x.TickFormatter) = ("{0} unused", Short);
            var y = chart.Elements.OfType<YAxis>().Single();
            (y.TickFormat, y.TickFormatter) = ("{0} unused", Thousands);
        }

        static void Angle(Chart chart, CustomTemplates templates) => chart.Elements.OfType<PolarAngleAxis>().Single().TickFormatter = Capitals;

        var bars = await CustomReference.MarkupAsync(desktop, "SimpleBarChart", Axes);
        var radar = await CustomReference.MarkupAsync(desktop, "RadarAxesChart", Angle);
        static void Silent(Chart chart, CustomTemplates templates) => chart.Elements.OfType<XAxis>().Single().TickFormatter = new FuncValueConverter<object?, string?>(static _ => null);
        var kept = await CustomReference.MarkupAsync(desktop, "SimpleBarChart", Silent);

        Assert.Equal(7, Count(bars, ">Page</tspan>"));
        Assert.DoesNotContain("unused", bars, StringComparison.Ordinal);
        foreach (var text in (string[])["0k", "2.5k", "5k", "7.5k", "10k"])
        {
            Assert.Contains(">" + text + "</tspan>", bars, StringComparison.Ordinal);
        }

        Assert.Contains(">MATH</tspan>", radar, StringComparison.Ordinal);
        Assert.DoesNotContain(">Math</tspan>", radar, StringComparison.Ordinal);
        Assert.Contains(">Page A</tspan>", kept, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "TOOLTIP-26")]
    public async Task TooltipFormatterConvertersWriteTheLinesAndTheLabelAsync()
    {
        var reference = "type:" + typeof(Kilogram).AssemblyQualifiedName;
        Aioros.Runtime.Slots.PlanConverters.Register(new Aioros.Runtime.Slots.ConverterCode(
            [new(Aioros.Runtime.Slots.PlanConverters.Key(reference), static () => new Kilogram())],
            static (converter, value, target, parameter, culture) => ((IValueConverter)converter).Convert(value, target, parameter, culture),
            static (_, _, _, _, _) => null,
            static (_, inner) => inner!));
        static void Tooltip(Chart chart, CustomTemplates templates)
        {
            var tooltip = chart.Elements.OfType<Tooltip>().Single();
            (tooltip.Format, tooltip.Formatter, tooltip.LabelFormat, tooltip.LabelFormatter) = ("{0} unused", Kilograms, "{0} unused", Page);
        }

        var markup = await CustomReference.MarkupAsync(desktop, "SimpleBarChart", Tooltip);
        var pageB = markup[markup.IndexOf(">Page: Page B<", StringComparison.Ordinal)..];
        pageB = pageB[..pageB.IndexOf("</foreignObject>", StringComparison.Ordinal)];

        Assert.Contains("pv : 1398 kg [pv]<", pageB, StringComparison.Ordinal);
        Assert.Contains("uv : 3000 kg [uv]<", pageB, StringComparison.Ordinal);
        Assert.DoesNotContain("unused", markup, StringComparison.Ordinal);

        static void Rebuilt(Chart chart, CustomTemplates templates) => chart.Elements.OfType<Tooltip>().Single().Formatter = Kilograms;
        var (rebuiltParts, unrebuiltParts) = await desktop.Session.Dispatch(
            async () =>
            {
                var example = await Reference.ExampleAsync("SimpleBarChart");
                return (CustomReference.Chart(example, Rebuilt).WrittenSlot!.Parts, CustomReference.Chart(example, Tooltip).WrittenSlot!.Parts);
            },
            TestContext.Current.CancellationToken);
        var spec = await CustomReference.SpecAsync(desktop, "SimpleBarChart", Rebuilt);
        var rows = Reference.Rows(await Reference.ExampleAsync("SimpleBarChart")).Select(static row => row.ToDictionary(static pair => pair.Key, static pair => pair.Value)).ToList();
        rows[1]["pv"] = 5555d;
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        Svg.ChartSvg.Write(buffer, spec, rows, static (_, _) => null);
        var changed = System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);

        Assert.Equal(reference, spec.Tooltip!.Converter);
        Assert.DoesNotContain(rebuiltParts, static part => part.Key == ChartSlot.FormatterReason);
        Assert.Contains(unrebuiltParts, static part => part.Key == ChartSlot.FormatterReason);
        Assert.Contains("pv : 5555 kg [pv]<", changed, StringComparison.Ordinal);
    }

    private static int Count(string text, string part)
    {
        var count = 0;
        for (var at = text.IndexOf(part, StringComparison.Ordinal); at >= 0; at = text.IndexOf(part, at + 1, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private sealed class Thousand : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is double number && parameter is int ? (number / 1000).ToString(CultureInfo.InvariantCulture) + "k" : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    private sealed class Kilogram : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            string.Create(CultureInfo.InvariantCulture, $"{value} kg [{parameter}]");

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
