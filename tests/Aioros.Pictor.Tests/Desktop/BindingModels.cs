using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Aioros.Pictor.Tests.Desktop;

/// <summary>
/// An item that notifies its value's changes and counts the handlers attached to it; read by key, as the tests' charts
/// have no generated accessors.
/// </summary>
public sealed class NotifyingDictionary : INotifyPropertyChanged, IReadOnlyDictionary<string, object?>
{
    private PropertyChangedEventHandler? _changed;

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add => _changed += value;
        remove => _changed -= value;
    }

    public int Handlers => _changed?.GetInvocationList().Length ?? 0;

    public string Name { get; init; } = string.Empty;

    public double Source { get; init; }

    public double Target { get; init; }

    public IList<NotifyingDictionary>? Children { get; init; }

    public double Value
    {
        get;
        set
        {
            field = value;
            _changed?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    public IEnumerable<string> Keys => ["Name", "Value", "Source", "Target", "Children"];

    public IEnumerable<object?> Values => Keys.Select(key => this[key]);

    public int Count => Keys.Count();

    public object? this[string key] => key switch
    {
        "Name" => Name,
        "Value" => Value,
        "Source" => Source,
        "Target" => Target,
        "Children" => Children,
        _ => null,
    };

    public bool ContainsKey(string key) => Keys.Contains(key);

    public bool TryGetValue(string key, out object? value)
    {
        value = this[key];
        return ContainsKey(key);
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Keys.Select(key => KeyValuePair.Create(key, this[key])).GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>An observable list that counts the handlers attached to its changes.</summary>
public sealed class WatchedCollection<T> : ObservableCollection<T>
{
    public WatchedCollection(IEnumerable<T> items)
        : base(items)
    {
    }

    public int Handlers { get; private set; }

    public override event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add
        {
            base.CollectionChanged += value;
            Handlers++;
        }

        remove
        {
            base.CollectionChanged -= value;
            Handlers--;
        }
    }
}
