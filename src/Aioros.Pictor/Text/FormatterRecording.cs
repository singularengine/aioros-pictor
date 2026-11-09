using System.Buffers;
using System.Collections.Immutable;
using Aioros.Pictor.Data;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Text;

/// <summary>
/// The texts a spec's formatter functions write, recorded into the spec for the web writer: the plan records a spec at
/// build time and its writer reads it back without the functions, so a chart with one is written by Avalonia's engine
/// path, whose spec then carries what each function wrote for this data (as a refused template's spec carries what
/// Avalonia drew, <see cref="ChartSpec.Resolved"/>). The chart is written once with recording functions; every tick and
/// every hover state's tooltip is in its markup, so every text the writer needs is recorded.
/// </summary>
public static class FormatterRecording
{
    /// <summary>Whether the spec has a formatter function: an axis' <c>tickFormatter</c>, a tooltip's <c>formatter</c> or <c>labelFormatter</c>.</summary>
    /// <param name="spec">The spec.</param>
    /// <returns>Whether it has one.</returns>
    public static bool Has(ChartSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return spec.XAxes.Any(static axis => axis.TickFormatter is not null)
            || spec.YAxes.Any(static axis => axis.TickFormatter is not null)
            || spec.Tooltip is { Formatter: not null } or { LabelFormatter: not null }
            || spec.Polar is { } polar && (polar.AngleAxis?.TickFormatter is not null || polar.RadiusAxis?.TickFormatter is not null
                || polar.MoreAngleAxes.Any(static axis => axis.TickFormatter is not null) || polar.MoreRadiusAxes.Any(static axis => axis.TickFormatter is not null));
    }

    /// <summary>Whether a formatter function of the spec names no converter the web's writer rebuilds, so only Avalonia's engine path can run it.</summary>
    /// <param name="spec">The spec.</param>
    /// <returns>Whether one cannot be rebuilt.</returns>
    public static bool Unrebuilt(ChartSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return spec.XAxes.Any(static axis => axis is { TickFormatter: not null, TickConverter: null })
            || spec.YAxes.Any(static axis => axis is { TickFormatter: not null, TickConverter: null })
            || spec.Tooltip is { Formatter: not null, Converter: null } or { LabelFormatter: not null, LabelConverter: null }
            || spec.Polar is { } polar && (polar.AngleAxis is { TickFormatter: not null, TickConverter: null } || polar.RadiusAxis is { TickFormatter: not null, TickConverter: null }
                || polar.MoreAngleAxes.Any(static axis => axis is { TickFormatter: not null, TickConverter: null }) || polar.MoreRadiusAxes.Any(static axis => axis is { TickFormatter: not null, TickConverter: null }));
    }

    /// <summary>The spec with what its functions write for the data recorded beside them.</summary>
    /// <param name="spec">The spec.</param>
    /// <param name="data">The chart's data.</param>
    /// <param name="members">How the data's members are read.</param>
    /// <returns>The spec with its texts recorded.</returns>
    public static ChartSpec Record(ChartSpec spec, object? data, DataMembers members)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var tables = new List<Dictionary<string, string>>();
        Svg.ChartSvg.Write(new ArrayBufferWriter<byte>(), new Mapping(tables, true).Spec(spec), data, members);
        return new Mapping(tables, false).Spec(spec);
    }

    /// <summary>One pass over a spec's functions, in a fixed order: each wrapped to record into its table, or given what its table recorded.</summary>
    private sealed class Mapping(List<Dictionary<string, string>> tables, bool recording)
    {
        private int _slot;

        public ChartSpec Spec(ChartSpec spec)
        {
            spec = spec with { XAxes = [.. spec.XAxes.Select(Axis)], YAxes = [.. spec.YAxes.Select(Axis)] };
            if (spec.Polar is { } polar)
            {
                spec = spec with { Polar = Polar(polar) };
            }

            return spec.Tooltip is { } tooltip ? spec with { Tooltip = Tooltip(tooltip) } : spec;
        }

        private PolarSpec Polar(PolarSpec polar) => polar with
        {
            AngleAxis = polar.AngleAxis is { } angle ? Angle(angle) : null,
            RadiusAxis = polar.RadiusAxis is { } radius ? Radius(radius) : null,
            MoreAngleAxes = [.. polar.MoreAngleAxes.Select(Angle)],
            MoreRadiusAxes = [.. polar.MoreRadiusAxes.Select(Radius)],
        };

        private AxisSpec Axis(AxisSpec axis) => axis.TickFormatter is not { } function ? axis
            : Table() is var table && recording ? axis with { TickFormatter = Tick(function, table) } : axis with { TickTexts = Texts(table) };

        private PolarAngleAxisSpec Angle(PolarAngleAxisSpec axis) => axis.TickFormatter is not { } function ? axis
            : Table() is var table && recording ? axis with { TickFormatter = Tick(function, table) } : axis with { TickTexts = Texts(table) };

        private PolarRadiusAxisSpec Radius(PolarRadiusAxisSpec axis) => axis.TickFormatter is not { } function ? axis
            : Table() is var table && recording ? axis with { TickFormatter = Tick(function, table) } : axis with { TickTexts = Texts(table) };

        private TooltipSpec Tooltip(TooltipSpec tooltip)
        {
            if (tooltip.Formatter is { } formatter)
            {
                var table = Table();
                tooltip = recording ? tooltip with { Formatter = Value(formatter, table) } : tooltip with { Texts = Texts(table) };
            }

            if (tooltip.LabelFormatter is { } labelFormatter)
            {
                var table = Table();
                tooltip = recording ? tooltip with { LabelFormatter = Label(labelFormatter, table) } : tooltip with { LabelTexts = Texts(table) };
            }

            return tooltip;
        }

        private Dictionary<string, string> Table()
        {
            if (_slot == tables.Count)
            {
                tables.Add(new Dictionary<string, string>(StringComparer.Ordinal));
            }

            return tables[_slot++];
        }

        private static ImmutableDictionary<string, string> Texts(Dictionary<string, string> table) => table.ToImmutableDictionary(StringComparer.Ordinal);

        private static Func<object?, int, string?> Tick(Func<object?, int, string?> function, Dictionary<string, string> table) => (value, index) =>
            Recorded(table, FormatKeys.Tick(value, index), function(value, index));

        private static Func<object?, string, string?> Value(Func<object?, string, string?> function, Dictionary<string, string> table) => (value, name) =>
            Recorded(table, FormatKeys.Value(value, name), function(value, name));

        private static Func<object?, string?> Label(Func<object?, string?> function, Dictionary<string, string> table) => label =>
            Recorded(table, FormatKeys.Label(label), function(label));

        private static string? Recorded(Dictionary<string, string> table, string key, string? text)
        {
            if (text is not null)
            {
                table[key] = text;
            }

            return text;
        }
    }
}
