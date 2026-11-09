using System.Buffers;
using System.Collections;
using System.Globalization;
using Aioros.Pictor.Data;
using Aioros.Pictor.Specs;
using Aioros.Runtime.Rendering;

namespace Aioros.Pictor.Svg;

/// <summary>
/// One chart of a compiled page, written per request: the chart's markup depends only on its spec (fixed for the
/// writer) and on the values it reads from the data, so when a request's data holds the same values in every column
/// the last render read, the markup that render wrote is written again without laying the chart out. The markup is
/// kept only once a data version is seen twice, so data that changes on every request costs no copy. The state is one
/// immutable entry swapped atomically: concurrent requests read a consistent snapshot and never wait.
/// </summary>
internal sealed class ChartOutput(ChartSpec spec, DataMembers members)
{
    private Entry? _last;

    /// <summary>How many times the chart was laid out and painted (what the cache saves).</summary>
    public int Renders => Volatile.Read(ref _renders);

    private int _renders;

    private readonly bool _marked = ChartSvg.Marked(spec);

    private readonly bool _synced = spec.SyncId is { Length: > 0 };

    /// <summary>The capacity first rented for a render kept apart from the output: a larger chart grows through pooled arrays.</summary>
    internal const int RentedCapacity = 32 * 1024;

    public void Write(object? value, IBufferWriter<byte> output)
    {
        if (_marked)
        {
            using var rendered = Marked(value, page: false);
            ChartMarks.Write(output, rendered.Markup.Span, rendered.Marks);
            return;
        }

        Plain(value, output, page: false);
    }

    /// <summary>
    /// Writes the chart through a slot renderer's output: its markup, with the framework's field and command attributes at
    /// its marks, and the rule sets it contributes to the page (a sync id's rules), which the page writes once.
    /// </summary>
    public void Write(ref SlotOutput output)
    {
        if (!_marked)
        {
            Contribute(ref output, Plain(output.Value, output.Buffer, _synced));
            return;
        }

        using var rendered = Marked(output.Value, _synced);
        ChartMarks.Write(ref output, rendered.Markup.Span, rendered.Marks);
        Contribute(ref output, rendered.Styles);
    }

    private static void Contribute(ref SlotOutput output, SyncRules[] styles)
    {
        foreach (var rules in styles)
        {
            if (output.NeedsStyle(rules.Key))
            {
                output.WriteStyle(rules.Key, rules.Css);
            }
        }
    }

    /// <summary>Writes an unmarked chart: the kept markup when the data holds the same values, otherwise a new render; returns its page styles.</summary>
    private SyncRules[] Plain(object? value, IBufferWriter<byte> output, bool page)
    {
        var last = Volatile.Read(ref _last);
        if (last is not null && last.Page == page && last.Culture == CultureInfo.CurrentCulture.Name && last.Matches(value, single: false, members))
        {
            if (last.Markup is { } markup)
            {
                output.Write(markup);
                return last.Styles;
            }

            using var buffer = new PooledBuffer(RentedCapacity);
            var styles = Render(ChartData.From(value, members), buffer, null, page, last);
            output.Write(buffer.WrittenSpan);
            Interlocked.CompareExchange(ref _last, last with { Markup = buffer.WrittenSpan.ToArray(), Styles = styles }, last);
            return styles;
        }

        var data = ChartData.From(value, members);
        var written = Render(data, output, null, page, last);
        Interlocked.CompareExchange(ref _last, Entry.Of(data, page) with { Styles = written }, last);
        return written;
    }

    /// <summary>
    /// The marked markup for the bound value: the kept one when the data holds the same values, otherwise a new render in a
    /// buffer rented from the shared array pool, returned when the result is disposed.
    /// </summary>
    private Rendered Marked(object? value, bool page)
    {
        var last = Volatile.Read(ref _last);
        if (last is not null && last.Page == page && last.Culture == CultureInfo.CurrentCulture.Name && last.Matches(value, single: false, members))
        {
            if (last.Markup is { } markup)
            {
                return new Rendered(markup, last.Marks, last.Styles, null);
            }

            var again = RenderMarked(ChartData.From(value, members), page, last);
            Interlocked.CompareExchange(ref _last, last with { Markup = again.Markup.ToArray(), Marks = again.Marks, Styles = again.Styles }, last);
            return again;
        }

        var data = ChartData.From(value, members);
        var rendered = RenderMarked(data, page, last);
        Interlocked.CompareExchange(ref _last, Entry.Of(data, page) with { Styles = rendered.Styles }, last);
        return rendered;
    }

    private Rendered RenderMarked(ChartData data, bool page, Entry? last)
    {
        var buffer = new PooledBuffer(RentedCapacity);
        try
        {
            var markup = MarkupBuffer.Over(buffer);
            var marks = new ChartMarks(markup);
            var styles = Render(data, markup, marks, page, last);
            return new Rendered(buffer.WrittenMemory, marks.ToArray(), styles, buffer);
        }
        catch
        {
            buffer.Dispose();
            throw;
        }
    }

    /// <summary>Lays the chart out and paints it; returns its page styles, keeping the last render's for a key it contributes again (their rules are built already).</summary>
    private SyncRules[] Render(ChartData data, IBufferWriter<byte> output, ChartMarks? marks, bool page, Entry? last)
    {
        Interlocked.Increment(ref _renders);
        var styles = page ? new List<SyncRules>() : null;
        ChartSvg.Paint(MarkupBuffer.Over(output), spec, data, marks, styles);
        if (styles is null or { Count: 0 })
        {
            return [];
        }

        var kept = last?.Styles ?? [];
        return [.. styles.Select(rules => Array.Find(kept, old => old.Key == rules.Key) ?? rules)];
    }

    /// <summary>A marked markup, its marks and its page styles, with the pooled buffer holding a new render until it is copied out.</summary>
    private readonly record struct Rendered(ReadOnlyMemory<byte> Markup, Mark[] Marks, SyncRules[] Styles, PooledBuffer? Rented) : IDisposable
    {
        public void Dispose() => Rented?.Dispose();
    }

    /// <summary>The values a render read, whether it wrote its sync rules to the page, and its markup and page styles once the same values came again.</summary>
    private sealed record Entry(bool Page, int Count, KeyValuePair<string, double[]>[] Numbers, KeyValuePair<string, string?[]>[] Texts, KeyValuePair<string, double[]>[] Lows, (int Index, string Key, Entry Entry)[] Derived, (string Key, bool Root, string Resolved, string Capitalized)[] Resolutions)
    {
        public string Culture { get; init; } = CultureInfo.CurrentCulture.Name;

        public byte[]? Markup { get; init; }

        public Mark[] Marks { get; init; } = [];

        public SyncRules[] Styles { get; init; } = [];

        public static Entry Of(ChartData data, bool page)
        {
            var derived = data.DerivedCount == 0 ? [] : new (int Index, string Key, Entry Entry)[data.DerivedCount];
            for (var i = 0; i < derived.Length; i++)
            {
                var (index, key, nested) = data.DerivedAt(i);
                derived[i] = (index, key, Of(nested, page));
            }

            return new(page, data.Count, [.. data.NumbersRead], [.. data.TextsRead], [.. data.LowsRead], derived, [.. data.Resolutions.Select(static resolution => (resolution.Key, resolution.Root, resolution.Resolved, ChartData.Capitalized(resolution.Key)))]);
        }

        /// <summary>
        /// Whether a bound value holds the values this entry read, compared on the live items in one pass, without
        /// reading them into new data: numbers bit for bit, texts by reference then by value (a number's text by its
        /// number, since JavaScript's shortest text of a number parses back to it), and the lists below each item read
        /// again where the render read them.
        /// </summary>
        public bool Matches(object? bound, bool single, DataMembers members)
        {
            var items = new LiveItems(bound, single);
            if (items.Count != Count || !SameResolutions(ref items, members))
            {
                return false;
            }

            foreach (var (key, numbers) in Numbers)
            {
                if (!SameNumbers(ref items, key, numbers, Low(key), members))
                {
                    return false;
                }
            }

            foreach (var (key, texts) in Texts)
            {
                Type? type = null;
                Func<object, object?>? getter = null;
                for (var i = 0; i < texts.Length; i++)
                {
                    if (!SameText(ChartData.Read(items[i], key, members, ref type, ref getter), texts[i]))
                    {
                        return false;
                    }
                }
            }

            foreach (var (index, key, nested) in Derived)
            {
                if (index >= Count || !nested.Matches(Below(ref items, index, key, members), index == ChartData.RootIndex, members))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// A key's numbers as the data reads them, bit for bit: a number as it is, a pair (Recharts' <c>[low, high]</c>)
        /// as its upper end with its lower end against the lower ends the render read (none when it read no pair).
        /// </summary>
        private static bool SameNumbers(ref LiveItems items, string key, double[] numbers, double[] lows, DataMembers members)
        {
            Type? type = null;
            Func<object, object?>? getter = null;
            for (var i = 0; i < numbers.Length; i++)
            {
                var value = ChartData.Read(items[i], key, members, ref type, ref getter);
                var pair = value is double ? null : ChartData.Pair(value);
                var (low, high) = pair ?? (double.NaN, value is double number ? number : ChartData.Number(value));
                if (!Same(high, numbers[i]) || (lows.Length == 0 ? pair is not null : !Same(low, lows[i])))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Whether each key the render resolved (<c>name</c> or <c>Name</c>, from the first item or the bound value) resolves alike on the live items.</summary>
        private bool SameResolutions(ref LiveItems items, DataMembers members)
        {
            if (Resolutions.Length == 0)
            {
                return true;
            }

            object? first = null;
            var found = false;
            foreach (var (key, root, resolved, capitalized) in Resolutions)
            {
                if (!root && !found)
                {
                    for (var i = 0; i < items.Count && first is null; i++)
                    {
                        first = items[i];
                    }

                    found = true;
                }

                if (!string.Equals(ChartData.ResolveOn(root ? items.Bound : first, key, members, capitalized), resolved, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Same(double left, double right) => BitConverter.DoubleToInt64Bits(left) == BitConverter.DoubleToInt64Bits(right);

        /// <summary>The lower ends a render read for a key, none when its values were single numbers.</summary>
        private double[] Low(string key)
        {
            foreach (var (name, lows) in Lows)
            {
                if (name == key)
                {
                    return lows;
                }
            }

            return [];
        }

        private static object? Below(ref LiveItems items, int index, string key, DataMembers members)
        {
            Type? type = null;
            Func<object, object?>? getter = null;
            return index switch
            {
                ChartData.RootIndex => items.Bound,
                ChartData.MemberIndex => ChartData.Read(items.Bound, key, members, ref type, ref getter),
                _ => ChartData.Read(items[index], key, members, ref type, ref getter),
            };
        }

        private static bool SameText(object? value, string? text) => value switch
        {
            null => text is null,
            string live => ReferenceEquals(live, text) || string.Equals(live, text, StringComparison.Ordinal),
            double or float or int or long or short or byte or decimal or uint or ulong => text is not null
                && double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                && BitConverter.DoubleToInt64Bits(parsed) == BitConverter.DoubleToInt64Bits(ChartData.Number(value)),
            _ => string.Equals(ChartData.Text(value), text, StringComparison.Ordinal),
        };
    }

    /// <summary>
    /// The items of a live bound value as the data reads them (a list by index, the bound value alone for a root, any
    /// other collection copied only when an item is asked for), without copying a list.
    /// </summary>
    private struct LiveItems
    {
        private readonly bool _single;
        private readonly IList? _list;
        private object?[]? _copy;

        public LiveItems(object? bound, bool single)
        {
            Bound = bound;
            _single = single;
            _list = single ? null : bound as IList;
            Count = single ? 1 : bound switch
            {
                IList list => list.Count,
                ICollection collection => collection.Count,
                string => 0,
                IEnumerable => Copy().Length,
                _ => 0,
            };
        }

        public readonly object? Bound { get; }

        public readonly int Count { get; }

        public object? this[int index] => _single ? Bound : _list is { } list ? list[index] : Copy()[index];

        private object?[] Copy() => _copy ??= ChartData.From(Bound, static (_, _) => null).Items.ToArray();
    }
}
