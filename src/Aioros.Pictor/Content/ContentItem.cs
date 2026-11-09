using Aioros.Pictor.Data;
using Aioros.Pictor.Specs;

namespace Aioros.Pictor.Content;

/// <summary>
/// One item's props as a template's holes read them: its numbers and texts in the slot's order, then the data members
/// the template reads, its points (x and y in turn) when the slot has some, the key of its call (its index and state),
/// and the box Recharts' animations move.
/// </summary>
internal readonly ref struct ContentItem(ReadOnlySpan<double> numbers, ReadOnlySpan<string?> texts, long key)
{
    public ReadOnlySpan<double> Numbers { get; } = numbers;

    public ReadOnlySpan<string?> Texts { get; } = texts;

    public long Key { get; } = key;

    public ReadOnlySpan<double> Points { get; init; }

    /// <summary>The item itself, for a template the compiler refused (Avalonia lays it out with the item's context).</summary>
    public object? Datum { get; init; }

    /// <summary>The rectangle the desktop's enter animation scales the content in (a bar's box), or empty.</summary>
    public (double X, double Y, double Width, double Height) Box { get; init; }

    /// <summary>The value of a number of the content for this item.</summary>
    public double Evaluate(ContentNumber number)
    {
        var value = number.Constant;
        var members = number.Members;
        if (members.IsDefaultOrEmpty)
        {
            return value;
        }

        var coefficients = number.Coefficients;
        var sum = 0.0;
        for (var i = 0; i < members.Length; i++)
        {
            var member = members[i];
            sum += coefficients[i] * (member < Numbers.Length ? Numbers[member] : double.NaN);
        }

        return value + (sum / number.Divisor);
    }

    /// <summary>Whether a node is written for this item.</summary>
    public bool Shows(ContentNode node)
    {
        if (node.When.IsDefaultOrEmpty)
        {
            return true;
        }

        foreach (var when in node.When)
        {
            var value = Evaluate(when.Value);
            if (!(when.Inclusive ? value >= 0 : value > 0))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>A text member, or null.</summary>
    public string? Text(int member) => (uint)member < (uint)Texts.Length ? Texts[member] : null;

    /// <summary>The key of a call: the item's index and its state (0 at rest, 1 active), with the template's place among the chart's.</summary>
    public static long KeyOf(int template, int index, int state = 0) => ((long)template << 40) | ((long)state << 32) | (uint)index;
}

/// <summary>
/// The buffers a painter fills per item for one template: the slot's members, then the data members the template reads
/// (<c>Payload[key]</c>), read from the item's data; allocated once per series, never per item.
/// </summary>
internal sealed class ContentValues
{
    private readonly ChartData? _data;
    private readonly ContentSlot _slot;
    private readonly ContentSpec _spec;
    private object? _datum;

    public ContentValues(ContentSlot slot, ContentSpec spec, ChartData? data)
    {
        _slot = slot;
        _spec = spec;
        _data = data;
        Numbers = new double[slot.Numbers.Length + spec.Keys.Length];
        Texts = new string?[slot.Texts.Length + spec.Keys.Length];
    }

    public double[] Numbers { get; }

    public string?[] Texts { get; }

    public ContentSpec Spec => _spec;

    /// <summary>The chart's data the template reads its data members from.</summary>
    public ChartData? Data => _data;

    /// <summary>Reads the data members the template reads for an item, after the slot's own.</summary>
    public void Read(int index)
    {
        _datum = _data is not null && (uint)index < (uint)_data.Count ? _data.Items[index] : null;
        var keys = _spec.Keys;
        if (keys.IsDefaultOrEmpty)
        {
            return;
        }

        for (var k = 0; k < keys.Length; k++)
        {
            var numbers = _data?.Numbers(keys[k]);
            var texts = _data?.Texts(keys[k]);
            Numbers[_slot.Numbers.Length + k] = numbers is not null && (uint)index < (uint)numbers.Length ? numbers[index] : double.NaN;
            Texts[_slot.Texts.Length + k] = texts is not null && (uint)index < (uint)texts.Length ? texts[index] : null;
        }
    }

    /// <summary>The item the buffers hold now, under a call's key.</summary>
    public ContentItem Item(long key) => new(Numbers, Texts, key) { Datum = _datum };
}
