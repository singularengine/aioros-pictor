using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Aioros.Pictor.Data;
using Avalonia.Threading;

namespace Aioros.Pictor;

/// <summary>
/// What a chart in the visual tree follows below its bound value, as an <c>ItemsControl</c>'s bindings follow its
/// items: the property changes of the items it read (<see cref="INotifyPropertyChanged"/>), the changes of the nested
/// lists it read (a node's children, a Sankey's links) and of its series' own lists. The set is taken again at every
/// layout, from what the layout read, so an item removed or replaced is let go; a change only invalidates the chart,
/// which Avalonia's render loop lays out once at its next frame however many changes came before it. A change raised
/// on another thread is posted to the chart's dispatcher once until that post runs.
/// </summary>
internal sealed class ItemFollower
{
    private readonly Action _changed;
    private readonly Dispatcher _dispatcher;
    private readonly PropertyChangedEventHandler _onItem;
    private readonly NotifyCollectionChangedEventHandler _onList;
    private readonly Action _flush;
    private HashSet<object> _followed = new(ReferenceEqualityComparer.Instance);
    private HashSet<object> _next = new(ReferenceEqualityComparer.Instance);
    private object?[] _lastItems = [];
    private object? _lastBound;
    private int _pending;
    private int _posts;

    public ItemFollower(Action changed, Dispatcher dispatcher)
    {
        _changed = changed;
        _dispatcher = dispatcher;
        _onItem = OnItemChanged;
        _onList = OnListChanged;
        _flush = Flush;
    }

    /// <summary>How many times a change raised on another thread was posted to the UI thread.</summary>
    public int Posts => Volatile.Read(ref _posts);

    /// <summary>Follows what a layout read: the data's items and the lists and items below them, and the elements' own lists and items; lets go of the rest.</summary>
    public void Follow(ChartData data, IEnumerable<IChartElement> elements)
    {
        if (Unchanged(data, elements))
        {
            return;
        }

        if (data.Bound is not INotifyCollectionChanged)
        {
            Add(data.Bound);
        }

        AddItems(data);
        foreach (var element in elements)
        {
            if (element is IOwnItems { OwnItems: { } own })
            {
                Add(own);
                foreach (var item in own)
                {
                    Add(item);
                }
            }
        }

        foreach (var followed in _followed)
        {
            Leave(followed);
        }

        foreach (var added in _next)
        {
            Join(added);
        }

        (_followed, _next) = (_next, _followed);
        _next.Clear();
    }

    /// <summary>
    /// Whether a flat layout read the same items as the last one, in the same order, with nothing below them (a property
    /// change of an item lays the chart out again over the same items): the set stays, and the walk costs a comparison
    /// of references instead of a hash per item. The items are kept for the next layout otherwise.
    /// </summary>
    private bool Unchanged(ChartData data, IEnumerable<IChartElement> elements)
    {
        var items = data.Items;
        var flat = data.DerivedCount == 0 && !OwnItems(elements);
        var same = flat && ReferenceEquals(data.Bound, _lastBound) && items.Length == _lastItems.Length && Same(items, _lastItems);
        if (same)
        {
            return true;
        }

        _lastBound = flat ? data.Bound : null;
        if (!flat)
        {
            _lastItems = [];
            return false;
        }

        if (_lastItems.Length != items.Length)
        {
            _lastItems = new object?[items.Length];
        }

        items.CopyTo(_lastItems);
        return false;
    }

    private static bool OwnItems(IEnumerable<IChartElement> elements)
    {
        var own = false;
        foreach (var element in elements)
        {
            own |= element is IOwnItems { OwnItems: not null };
        }

        return own;
    }

    private static bool Same(ReadOnlySpan<object?> items, object?[] last)
    {
        for (var i = 0; i < items.Length; i++)
        {
            if (!ReferenceEquals(items[i], last[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Lets go of items a list no longer holds, at once, so a change of theirs before the next layout is not followed.</summary>
    public void Forget(IList? items)
    {
        if (items is null || !_dispatcher.CheckAccess())
        {
            return;
        }

        foreach (var item in items)
        {
            if (item is not null && _followed.Remove(item))
            {
                Unsubscribe(item);
                _lastItems = [];
                _lastBound = null;
            }
        }
    }

    /// <summary>Lets go of everything (the chart left the visual tree).</summary>
    public void Clear()
    {
        _lastItems = [];
        _lastBound = null;
        foreach (var followed in _followed)
        {
            Unsubscribe(followed);
        }

        _followed.Clear();
    }

    private void AddItems(ChartData data)
    {
        foreach (var item in data.Items)
        {
            Add(item);
        }

        for (var i = 0; i < data.DerivedCount; i++)
        {
            var nested = data.DerivedAt(i).Data;
            Add(nested.Bound);
            AddItems(nested);
        }
    }

    private void Leave(object followed)
    {
        if (!_next.Contains(followed))
        {
            Unsubscribe(followed);
        }
    }

    private void Join(object added)
    {
        if (!_followed.Contains(added))
        {
            Subscribe(added);
        }
    }

    private void Add(object? value)
    {
        if (value is INotifyCollectionChanged or INotifyPropertyChanged)
        {
            _next.Add(value);
        }
    }

    private void Subscribe(object value)
    {
        if (value is INotifyCollectionChanged list)
        {
            list.CollectionChanged += _onList;
        }
        else if (value is INotifyPropertyChanged item)
        {
            item.PropertyChanged += _onItem;
        }
    }

    private void Unsubscribe(object value)
    {
        if (value is INotifyCollectionChanged list)
        {
            list.CollectionChanged -= _onList;
        }
        else if (value is INotifyPropertyChanged item)
        {
            item.PropertyChanged -= _onItem;
        }
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e) => Changed();

    private void OnListChanged(object? sender, NotifyCollectionChangedEventArgs e) => ListChanged(e);

    /// <summary>A change of a list the chart reads (its own collection or one below it): its removed items let go at once on the UI thread, and the chart laid out again there, posted when the change was raised on another thread.</summary>
    public void ListChanged(NotifyCollectionChangedEventArgs e)
    {
        Forget(e.OldItems);
        Changed();
    }

    private void Changed()
    {
        if (_dispatcher.CheckAccess())
        {
            _changed();
            return;
        }

        if (Interlocked.Exchange(ref _pending, 1) == 0)
        {
            Interlocked.Increment(ref _posts);
            _dispatcher.Post(_flush);
        }
    }

    private void Flush()
    {
        Volatile.Write(ref _pending, 0);
        _changed();
    }
}
