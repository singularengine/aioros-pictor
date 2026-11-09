using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using Aioros.Navigation;

namespace Gallery.Analytics;

/// <summary>
/// The dashboard page's model: the loader's data, and the command a click on a top page runs, which focuses the main
/// chart on that page (or clears the focus when the page is already focused) by going to the dashboard's address with
/// the focus in its query, so the web and the desktop keep it the same way.
/// </summary>
public sealed class DashboardModel
{
    public DashboardModel(DashboardData data, INavigator navigator)
    {
        Data = data;
        FocusPage = new Command(parameter =>
        {
            if (Target(parameter) is { } path)
            {
                _ = navigator.NavigateAsync(new NavigationRequest(path), CancellationToken.None).AsTask();
            }
        });
    }

    public DashboardData Data { get; }

    public bool Focused => Data.FocusIndex >= 0;

    public Choice WebApp => Data.Apps[0];

    public Choice MobileApp => Data.Apps[1];

    public Choice ApiApp => Data.Apps[2];

    public Slice Source1 => Data.Sources[0];

    public Slice Source2 => Data.Sources[1];

    public Slice Source3 => Data.Sources[2];

    public Slice Source4 => Data.Sources[3];

    public Slice Source5 => Data.Sources[4];

    public IReadOnlyList<Slot> Monday => Data.Week[0].Slots;

    public IReadOnlyList<Slot> Tuesday => Data.Week[1].Slots;

    public IReadOnlyList<Slot> Wednesday => Data.Week[2].Slots;

    public IReadOnlyList<Slot> Thursday => Data.Week[3].Slots;

    public IReadOnlyList<Slot> Friday => Data.Week[4].Slots;

    public IReadOnlyList<Slot> Saturday => Data.Week[5].Slots;

    public IReadOnlyList<Slot> Sunday => Data.Week[6].Slots;

    public string Device1 => Data.DeviceNames[0];

    public string Device2 => Data.DeviceNames[1];

    public string Device3 => Data.DeviceNames[2];

    /// <summary>Runs with the clicked bar's index.</summary>
    public ICommand FocusPage { get; }

    /// <summary>Where a click on the bar at <paramref name="parameter"/> goes, or nothing for an index off the list.</summary>
    public string? Target(string? parameter) =>
        int.TryParse(parameter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) && index >= 0 && index < Data.Pages.Count
            ? Links.Dashboard(Data.App, Data.Range, index == Data.FocusIndex ? -1 : index)
            : null;
}

/// <summary>The live tile's model, which receives each tick's tile.</summary>
public sealed class KpiNowModel(Kpi data) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public Kpi Data { get; private set; } = data;

    public void Receive(Kpi data)
    {
        Data = data;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Data)));
    }
}

/// <summary>The period tiles' model, which receives each minute's tiles.</summary>
public sealed class KpiModel(KpiData data) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public KpiData Data { get; private set; } = data;

    public void Receive(KpiData data)
    {
        Data = data;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Data)));
    }
}

/// <summary>
/// The main chart's model, which receives each tick's buckets. Its brush window is kept across ticks two ways: on the
/// desktop the model lives as long as the page, so <see cref="BrushStart"/> and <see cref="BrushEnd"/> stay as dragged;
/// on the web each live connection has its own model, created from the address, so the web brush is bound to
/// <see cref="WindowStart"/> and <see cref="WindowEnd"/>, which go to the dashboard's address with the window in its
/// query when a post moves them: the post is answered with that address, and the framework's script opens the live
/// stream again at it (Aioros's SSR-179), so every later push keeps the window.
/// </summary>
public sealed class TrafficView : INotifyPropertyChanged
{
    private readonly INavigator _navigator;
    private TrafficState _state;

    public TrafficView(TrafficState state, INavigator navigator)
    {
        _state = state;
        _navigator = navigator;
        BrushStart = WindowStart;
        BrushEnd = WindowEnd;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public TrafficData Data => _state.Data;

    /// <summary>The desktop brush's first bucket, kept across ticks by the model itself.</summary>
    public int? BrushStart { get; set; }

    /// <summary>The desktop brush's last bucket.</summary>
    public int? BrushEnd { get; set; }

    /// <summary>The web brush's first bucket: the address' <c>from</c> (by default the whole Live range, the later half of a longer one), within the data.</summary>
    public int? WindowStart
    {
        get => Math.Clamp(_state.From ?? (Data.Live ? 0 : Data.Points.Count / 2), 0, Last);
        set
        {
            if (value is { } start && start != WindowStart)
            {
                Go(start, Math.Max(start, WindowEnd ?? Last));
            }
        }
    }

    /// <summary>The web brush's last bucket: the address' <c>to</c> (by default the last bucket), within the data and not before the first.</summary>
    public int? WindowEnd
    {
        get => Math.Clamp(_state.To ?? Last, Math.Clamp(_state.From ?? 0, 0, Last), Last);
        set
        {
            if (value is { } end && end != WindowEnd)
            {
                Go(Math.Min(WindowStart ?? 0, end), end);
            }
        }
    }

    private int Last => Math.Max(Data.Points.Count - 1, 0);

    public void Receive(TrafficState state)
    {
        _state = state;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Data)));
    }

    private void Go(int start, int end) =>
        _ = _navigator.NavigateAsync(new NavigationRequest(Links.Dashboard(_state.App, _state.Range, _state.Focus, start, end)), CancellationToken.None).AsTask();
}

/// <summary>The live feed's model, which receives each tick's events.</summary>
public sealed class FeedModel(FeedData data) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public FeedData Data { get; private set; } = data;

    public void Receive(FeedData data)
    {
        Data = data;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Data)));
    }
}

/// <summary>A command running an action with its parameter as text.</summary>
internal sealed class Command(Action<string?> run) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => run(parameter as string ?? (parameter is null ? null : Convert.ToString(parameter, CultureInfo.InvariantCulture)));
}
