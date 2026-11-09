using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;

namespace Gallery.Models;

/// <summary>
/// The interactive page's model: what a click on a bar picked, the brush's window, the treemap's zoom and the data of
/// the synced charts. On the web it is created per request: a post binds the brush's and the treemap's fields into it,
/// then runs the clicked command, and the page is answered from it.
/// </summary>
public sealed class InteractiveData : INotifyPropertyChanged
{
    public InteractiveData()
    {
        Pick = new Command(parameter => Picked = Describe(parameter));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<Point> Pages { get; } = new Datasets().Pages;

    public IReadOnlyList<Sample> Samples { get; } = new InteractionData().Samples;

    public IReadOnlyList<Package> Flare { get; } = new Hierarchies().FlareTop;

    /// <summary>The same pages in reverse order, synced by category value.</summary>
    public IReadOnlyList<Point> Reversed { get; } = [.. new Datasets().Pages.Reverse()];

    /// <summary>What the last click picked.</summary>
    public string Picked
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Picked)));
        }
    } = "Click a bar.";

    /// <summary>The brush's first item.</summary>
    public int? BrushStart { get; set; } = 5;

    /// <summary>The brush's last item.</summary>
    public int? BrushEnd { get; set; } = 20;

    /// <summary>The treemap's zoomed node.</summary>
    public string? Zoom { get; set; }

    /// <summary>Runs with the clicked bar's index.</summary>
    public ICommand Pick { get; }

    private string Describe(string? parameter) =>
        int.TryParse(parameter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) && index >= 0 && index < Pages.Count
            ? $"You clicked {Pages[index].Name}: pv {Pages[index].Pv}, uv {Pages[index].Uv}."
            : "Click a bar.";

    private sealed class Command(Action<string?> run) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => run(parameter as string);
    }
}
