using System.Collections;
using System.Globalization;
using Aioros.Graphs.Formatting;

namespace Aioros.Pictor.Data;

/// <summary>Reads a member of a data item by the item's type and the member's name; null when the type has no such member.</summary>
/// <param name="owner">The item's runtime type.</param>
/// <param name="member">The member's name (a data key).</param>
/// <returns>A getter of that member, or null.</returns>
public delegate Func<object, object?>? DataMembers(Type owner, string member);

/// <summary>
/// A chart's data as columns: for every data key the chart reads, the items' values as numbers (NaN where missing) and,
/// for category keys, as texts. Items are read once, in order, through <see cref="DataMembers"/> (generated accessors
/// in production), dictionaries by key.
/// </summary>
public sealed class ChartData
{
    private static readonly Dictionary<string, double[]> NoNumbers = [];
    private static readonly Dictionary<string, string?[]> NoTexts = [];
    private Dictionary<string, double[]>? _numbers;
    private Dictionary<string, string?[]>? _texts;
    private readonly object?[] _items;
    private readonly DataMembers _members;
    /// <summary>The index of <see cref="Member"/>'s data among the data read below.</summary>
    internal const int MemberIndex = -1;

    /// <summary>The index of <see cref="Root"/>'s data among the data read below.</summary>
    internal const int RootIndex = -2;
    private object? _root;
    private List<(int Index, string Key, ChartData Data)>? _derived;
    private ChartData?[]? _nested;
    private string? _nestedKey;
    private Extras? _extras;
    private List<(string Key, bool Root, string Resolved)>? _resolutions;

    private ChartData(object?[] items, DataMembers members)
    {
        _items = items;
        _members = members;
    }

    /// <summary>The number of items.</summary>
    public int Count => _items.Length;

    /// <summary>The items, in order.</summary>
    internal ReadOnlySpan<object?> Items => _items;

    /// <summary>The value the items were read from: the bound value, a nested list, a member's list or the root.</summary>
    internal object? Bound => _root;

    /// <summary>The columns read so far as numbers, by key.</summary>
    internal IReadOnlyDictionary<string, double[]> NumbersRead => _numbers ?? NoNumbers;

    /// <summary>The lower ends of the pairs read so far, by key (only keys that hold a pair).</summary>
    internal IReadOnlyDictionary<string, double[]> LowsRead => _extras?.Lows ?? ImmutableDictionaryOfNone.Value;

    /// <summary>The columns read so far as texts, by key.</summary>
    internal IReadOnlyDictionary<string, string?[]> TextsRead => _texts ?? NoTexts;

    /// <summary>
    /// A key of the values the layout read (each column hashed by its name and values, the columns combined in any
    /// order): two renders of the same items give the same key, as Recharts' <c>isEqual</c> finds their points equal.
    /// </summary>
    internal uint Key()
    {
        var key = (uint)Count;
        foreach (var (name, values) in NumbersRead)
        {
            var hash = Hash(name);
            foreach (var value in values)
            {
                hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value));
            }

            key += hash;
        }

        foreach (var (name, texts) in TextsRead)
        {
            var hash = Hash(name);
            foreach (var text in texts)
            {
                hash = Mix(hash, text is null ? 1UL : Hash(text));
            }

            key += hash;
        }

        return key;
    }

    private static uint Hash(string text)
    {
        var hash = 2166136261u;
        foreach (var c in text)
        {
            hash = (hash ^ c) * 16777619u;
        }

        return hash;
    }

    private static uint Mix(uint hash, ulong value) => (((hash ^ (uint)value) * 16777619u) ^ (uint)(value >> 32)) * 16777619u;

    /// <summary>The data a bound value holds: a list of items, or nothing.</summary>
    /// <param name="value">The bound value.</param>
    /// <param name="members">How members of the items are read.</param>
    /// <returns>The data.</returns>
    public static ChartData From(object? value, DataMembers members)
    {
        object?[] items;
        switch (value)
        {
            case ICollection collection:
                items = new object?[collection.Count];
                collection.CopyTo(items, 0);
                break;
            case IEnumerable enumerable and not string:
                var list = new List<object?>();
                foreach (var item in enumerable)
                {
                    list.Add(item);
                }

                items = [.. list];
                break;
            default:
                items = [];
                break;
        }

        return new ChartData(items, members) { _root = value };
    }

    /// <summary>The data a spec carries as a table (a series' own data).</summary>
    /// <param name="table">The table.</param>
    /// <returns>The data.</returns>
    public static ChartData From(Specs.DataTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        var data = new ChartData(new object?[table.Count], static (_, _) => null);
        foreach (var (key, column) in table.Columns)
        {
            var texts = new string?[table.Count];
            var numbers = new double[table.Count];
            double[]? lows = null;
            for (var i = 0; i < table.Count; i++)
            {
                texts[i] = i < column.Length ? column[i] : null;
                numbers[i] = Number(texts[i]);
                if (double.IsNaN(numbers[i]) && TextPair(texts[i]) is { } pair)
                {
                    lows ??= NaNs(table.Count);
                    (lows[i], numbers[i]) = pair;
                }
            }

            (data._texts ??= new(StringComparer.Ordinal))[key] = texts;
            (data._numbers ??= new(StringComparer.Ordinal))[key] = numbers;
            if (lows is not null)
            {
                data.LowTable()[key] = lows;
            }
        }

        return data;
    }

    /// <summary>
    /// The items from <paramref name="start"/> to <paramref name="end"/> (a brush's window): their columns are taken from
    /// this data's, so what the window reads is read, and kept, by the whole data.
    /// </summary>
    /// <param name="start">The first item.</param>
    /// <param name="end">The last item.</param>
    /// <returns>The window.</returns>
    public ChartData Window(int start, int end)
    {
        return new ChartData(_items[start..(end + 1)], _members) { _extras = new Extras { Whole = this, Start = start }, _root = _root };
    }

    /// <summary>The list a member of the bound value holds (a Sankey's nodes or links), read with this data's member lookup.</summary>
    /// <param name="key">The member's name.</param>
    /// <returns>The member's items.</returns>
    public ChartData Member(string key) => Derive(MemberIndex, key);

    /// <summary>The list a member of one item holds (a treemap node's children).</summary>
    /// <param name="index">The item's index.</param>
    /// <param name="key">The member's name.</param>
    /// <returns>The member's items.</returns>
    public ChartData Nested(int index, string key) => Derive(index, key);

    /// <summary>The bound value itself as the only item (a sunburst's root node).</summary>
    /// <returns>The bound value as data.</returns>
    public ChartData Root() => Derive(RootIndex, string.Empty);

    /// <summary>How many lists or values were read below this data's items (nested lists, members, the root).</summary>
    internal int DerivedCount => _derived?.Count ?? 0;

    /// <summary>One of the lists or values read below this data, by the item's index and the member's key, in the order they were read.</summary>
    internal (int Index, string Key, ChartData Data) DerivedAt(int position) => _derived![position];

    /// <summary>
    /// The data below an item (<paramref name="index"/> from 0), below the bound value (<see cref="MemberIndex"/>) or the
    /// bound value itself (<see cref="RootIndex"/>), read once and kept: the items' lists under one key by index, the
    /// rest in the order read.
    /// </summary>
    internal ChartData Derive(int index, string key)
    {
        if (Kept(index, key) is { } kept)
        {
            return kept;
        }

        Type? type = null;
        Func<object, object?>? getter = null;
        var data = index switch
        {
            RootIndex => new ChartData([_root], _members) { _root = _root },
            MemberIndex => From(Value(_root, key, ref type, ref getter), _members),
            _ => From(Value(_items[index], key, ref type, ref getter), _members),
        };
        (_derived ??= []).Add((index, key, data));
        if (index >= 0 && (_nestedKey is null || string.Equals(_nestedKey, key, StringComparison.Ordinal)))
        {
            _nestedKey = key;
            (_nested ??= new ChartData?[_items.Length])[index] = data;
        }

        return data;
    }

    private ChartData? Kept(int index, string key)
    {
        if (index >= 0 && _nested is not null && string.Equals(_nestedKey, key, StringComparison.Ordinal))
        {
            return _nested[index];
        }

        for (var i = 0; i < DerivedCount; i++)
        {
            if (_derived![i].Index == index && string.Equals(_derived[i].Key, key, StringComparison.Ordinal))
            {
                return _derived[i].Data;
            }
        }

        return null;
    }

    /// <summary>
    /// The key as the data names it: for dictionaries the key itself unless only its capitalized form is there; for
    /// objects its capitalized form when that member exists (Recharts' fixed keys <c>nodes</c> or <c>children</c> on C#
    /// members <c>Nodes</c> or <c>Children</c>), otherwise the key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="root">Whether the key names a member of the bound value rather than of its items.</param>
    /// <returns>The key to read.</returns>
    public string Resolve(string key, bool root = false)
    {
        var owner = root ? _root : Array.Find(_items, static item => item is not null);
        var resolved = ResolveOn(owner, key, _members);
        if (owner is not null && key.Length > 0 && !char.IsUpper(key[0]))
        {
            _resolutions ??= [];
            if (!_resolutions.Contains((key, root, resolved)))
            {
                _resolutions.Add((key, root, resolved));
            }
        }

        return resolved;
    }

    /// <summary>The keys resolved so far, with what they resolved to: a kept render holds only while the same keys resolve alike.</summary>
    internal IReadOnlyList<(string Key, bool Root, string Resolved)> Resolutions => _resolutions is { } resolutions ? resolutions : [];

    /// <summary>The key as an owner (an item, or the bound value) names it (<see cref="Resolve(string, bool)"/>).</summary>
    internal static string ResolveOn(object? owner, string key, DataMembers members, string? capitalized = null)
    {
        if (owner is null || key.Length == 0 || char.IsUpper(key[0]))
        {
            return key;
        }

        var dictionary = owner is IReadOnlyDictionary<string, object?> or IDictionary;
        if (dictionary && Has(owner, key, members))
        {
            return key;
        }

        capitalized ??= Capitalized(key);
        return dictionary
            ? !Has(owner, capitalized, members) ? key : capitalized
            : Has(owner, capitalized, members) ? capitalized : key;
    }

    /// <summary>A key with its first letter upper case (<c>name</c>, <c>Name</c>).</summary>
    internal static string Capitalized(string key) => string.Create(key.Length, key, static (span, key) =>
    {
        key.AsSpan().CopyTo(span);
        span[0] = char.ToUpperInvariant(key[0]);
    });

    private static bool Has(object owner, string key, DataMembers members) => owner switch
    {
        IReadOnlyDictionary<string, object?> map => map.ContainsKey(key),
        IDictionary dictionary => dictionary.Contains(key),
        _ => members(owner.GetType(), key) is not null,
    };

    /// <summary>
    /// The values of a key as numbers, NaN where an item has none or one that is not a number; a value that is a pair
    /// (Recharts' range value <c>[low, high]</c>) counts as its upper end, its lower end is in <see cref="Lows"/>.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <returns>One number per item.</returns>
    public double[] Numbers(string key)
    {
        if (_numbers is not null && _numbers.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (_extras?.Whole is { } whole)
        {
            var start = _extras.Start;
            var window = whole.Numbers(key)[start..(start + Count)];
            var lows = whole.Lows(key);
            (_numbers ??= new(StringComparer.Ordinal))[key] = window;
            if (lows.Length > 0)
            {
                LowTable()[key] = lows[start..(start + Count)];
            }

            return window;
        }

        var numbers = new double[_items.Length];
        double[]? low = null;
        Type? type = null;
        Func<object, object?>? getter = null;
        for (var i = 0; i < numbers.Length; i++)
        {
            var value = Value(_items[i], key, ref type, ref getter);
            if (value is double number)
            {
                numbers[i] = number;
            }
            else if (Pair(value) is { } pair)
            {
                low ??= NaNs(numbers.Length);
                (low[i], numbers[i]) = pair;
            }
            else
            {
                numbers[i] = Number(value);
            }
        }

        (_numbers ??= new(StringComparer.Ordinal))[key] = numbers;
        if (low is not null)
        {
            LowTable()[key] = low;
        }

        return numbers;
    }

    /// <summary>
    /// The lower ends of a key's values that are pairs (Recharts' range values <c>[low, high]</c>: a list or an array of
    /// two numbers, a tuple, or their text <c>low,high</c>), NaN where a value is a single number; empty when no value is a pair.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <returns>One number per item, or none.</returns>
    public double[] Lows(string key)
    {
        if (_numbers is null || !_numbers.ContainsKey(key))
        {
            Numbers(key);
        }

        return _extras?.Lows is { } table && table.TryGetValue(key, out var lows) ? lows : [];
    }

    /// <summary>A value's two ends when it is a pair, or null.</summary>
    internal static (double Low, double High)? Pair(object? value) => value switch
    {
        string => null,
        IList { Count: >= 2 } list => (Number(list[0]), Number(list[1])),
        System.Runtime.CompilerServices.ITuple { Length: >= 2 } tuple => (Number(tuple[0]), Number(tuple[1])),
        _ => null,
    };

    /// <summary>A pair written as JavaScript writes an array, <c>low,high</c>, or null.</summary>
    private static (double Low, double High)? TextPair(string? text)
    {
        var comma = text is null ? -1 : text.IndexOf(',', StringComparison.Ordinal);
        if (comma <= 0)
        {
            return null;
        }

        var (low, high) = (Number(text![..comma]), Number(text[(comma + 1)..]));
        return double.IsNaN(low) && double.IsNaN(high) ? null : (low, high);
    }

    private static double[] NaNs(int count)
    {
        var values = new double[count];
        Array.Fill(values, double.NaN);
        return values;
    }

    /// <summary>The values of a key as texts, as JavaScript would write them; null where an item has none.</summary>
    /// <param name="key">The data key.</param>
    /// <returns>One text per item.</returns>
    public string?[] Texts(string key)
    {
        if (_texts is not null && _texts.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (_extras?.Whole is { } whole)
        {
            var window = whole.Texts(key)[_extras.Start..(_extras.Start + Count)];
            (_texts ??= new(StringComparer.Ordinal))[key] = window;
            return window;
        }

        var texts = new string?[_items.Length];
        Type? type = null;
        Func<object, object?>? getter = null;
        for (var i = 0; i < texts.Length; i++)
        {
            texts[i] = Text(Value(_items[i], key, ref type, ref getter));
        }

        (_texts ??= new(StringComparer.Ordinal))[key] = texts;
        return texts;
    }

    /// <summary>A value as a number: numbers as they are, numeric texts parsed, anything else NaN.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The number.</returns>
    public static double Number(object? value) => value switch
    {
        null => double.NaN,
        double number => number,
        int number => number,
        long number => number,
        float number => number,
        decimal number => (double)number,
        short number => number,
        byte number => number,
        uint number => number,
        ulong number => number,
        string text => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : double.NaN,
        IConvertible convertible and not bool and not DateTime => convertible.ToDouble(CultureInfo.InvariantCulture),
        _ => double.NaN,
    };

    /// <summary>A value as text, numbers and booleans as JavaScript's <c>String(n)</c> writes them.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The text, or null for no value.</returns>
    public static string? Text(object? value) => value switch
    {
        null => null,
        string text => text,
        bool flag => flag ? "true" : "false",
        double number => JsNumber.ToShortestString(number),
        float number => JsNumber.ToShortestString(number),
        int or long or short or byte or decimal or uint or ulong => JsNumber.ToShortestString(Number(value)),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        IList list => Joined(list),
        System.Runtime.CompilerServices.ITuple tuple => Joined(tuple),
        _ => value.ToString(),
    };

    /// <summary>A list as JavaScript's <c>String(array)</c> writes it: its items' texts joined by commas.</summary>
    private static string Joined(IList list)
    {
        var parts = new string?[list.Count];
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = Text(list[i]);
        }

        return string.Join(',', parts);
    }

    /// <summary>A tuple as a list of its items, joined by commas.</summary>
    private static string Joined(System.Runtime.CompilerServices.ITuple tuple)
    {
        var parts = new string?[tuple.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = Text(tuple[i]);
        }

        return string.Join(',', parts);
    }

    private object? Value(object? item, string key, ref Type? type, ref Func<object, object?>? getter) => Read(item, key, _members, ref type, ref getter);

    /// <summary>A member of an item as the data reads it: a dictionary's entry, else the member lookup's getter, looked up again only when the item's type changes.</summary>
    internal static object? Read(object? item, string key, DataMembers members, ref Type? type, ref Func<object, object?>? getter)
    {
        if (item is null)
        {
            return null;
        }

        var itemType = item.GetType();
        if (itemType != type)
        {
            type = itemType;
            getter = item is IReadOnlyDictionary<string, object?> or IDictionary ? null : members(itemType, key) ?? Missing;
        }

        if (getter is not null)
        {
            return getter(item);
        }

        return item switch
        {
            IReadOnlyDictionary<string, object?> map => map.TryGetValue(key, out var mapped) ? mapped : null,
            IDictionary dictionary => dictionary.Contains(key) ? dictionary[key] : null,
            _ => null,
        };
    }

    private static readonly Func<object, object?> Missing = static _ => null;

    /// <summary>The lower ends' table, made when a first pair is read.</summary>
    private Dictionary<string, double[]> LowTable() => (_extras ??= new Extras()).Lows ??= new(StringComparer.Ordinal);

    /// <summary>What few data carry, kept apart so every item list (a treemap makes one per node) stays small: a window's whole data and start, and pairs' lower ends.</summary>
    private sealed class Extras
    {
        public ChartData? Whole { get; init; }

        public int Start { get; init; }

        public Dictionary<string, double[]>? Lows { get; set; }
    }

    private static class ImmutableDictionaryOfNone
    {
        public static readonly IReadOnlyDictionary<string, double[]> Value = new Dictionary<string, double[]>(0);
    }
}
