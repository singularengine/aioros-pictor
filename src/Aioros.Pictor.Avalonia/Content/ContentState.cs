using System.Globalization;
using Aioros.Pictor.Content;

namespace Aioros.Pictor;

/// <summary>A template's context, as <see cref="Calc"/> reads it: a number member by its name (<c>Cx</c>, <c>Payload.uv</c>).</summary>
internal interface IContentContext
{
    double Number(string name);

    ContentState State { get; }
}

/// <summary>
/// The values a template's context reads: its slot's numbers and texts, its points, and its item's data. The chart fills
/// them from the item it draws; the compiler fills them with marked values it can recognize in what Avalonia lays out.
/// </summary>
internal sealed class ContentState
{
    public ContentState(ContentSlot slot, double[] numbers, string?[] texts, ContentPayload payload)
    {
        Slot = slot;
        Numbers = numbers;
        Texts = texts;
        Payload = payload;
    }

    public ContentSlot Slot { get; }

    public double[] Numbers { get; }

    public string?[] Texts { get; }

    public double[] Points { get; init; } = [];

    public ContentPayload Payload { get; }

    public double Number(string name) => Slot.IndexOfNumber(name) is var index and >= 0
        ? Numbers[index]
        : name.StartsWith("Payload.", StringComparison.Ordinal) ? Data.ChartData.Number(Payload[name[8..]])
        : name.StartsWith("Payload[", StringComparison.Ordinal) && name.EndsWith(']') ? Data.ChartData.Number(Payload[name[8..^1]])
        : double.NaN;

    public string? Text(string name) => Slot.IndexOfText(name) is var index and >= 0 ? Texts[index] : null;

    public int Whole(string name) => Number(name) is var value && !double.IsNaN(value) ? (int)value : 0;

    public bool Flag(string name) => Number(name) > 0.5;
}

/// <summary>
/// The item a template draws, as Recharts' <c>payload</c>: its members by name, <c>{Binding Payload[name]}</c> (a
/// dictionary's entry, or the member of an object, read through the app's generated accessors).
/// </summary>
public sealed class ContentPayload
{
    private readonly Func<string, object?> _read;

    internal ContentPayload(Func<string, object?> read, object? item)
    {
        _read = read;
        Source = item;
    }

    /// <summary>The item itself, for a template the web does not compile (Avalonia renders the page).</summary>
    public object? Source { get; }

    /// <summary>A member of the item by its name.</summary>
    /// <param name="key">The member's name, a data key.</param>
    /// <returns>Its value, or null.</returns>
    public object? this[string key] => _read(key);

    /// <summary>No item.</summary>
    internal static ContentPayload None { get; } = new(static _ => null, null);

    /// <summary>The members of an item, read as the chart reads its data.</summary>
    internal static ContentPayload Of(object? item, Data.DataMembers members)
    {
        Type? type = null;
        Func<object, object?>? getter = null;
        string? last = null;
        return new ContentPayload(key =>
        {
            if (!string.Equals(key, last, StringComparison.Ordinal))
            {
                (type, getter, last) = (null, null, key);
            }

            return Data.ChartData.Read(item, key, members, ref type, ref getter);
        }, item);
    }

    /// <inheritdoc />
    public override string ToString() => Source is null ? string.Empty : Convert.ToString(Source, CultureInfo.InvariantCulture) ?? string.Empty;
}
