using System.ComponentModel;
using System.Windows.Input;

namespace Gallery.Models;

/// <summary>
/// The extras page's model: what the last tick click named, the brush's window (the slide posts one field that moves
/// both), and the data of the active items, label lists, polar ticks, tooltips and legend templates it shows.
/// </summary>
public sealed class ExtrasData : INotifyPropertyChanged
{
    public ExtrasData() => PickTick = new Command(parameter => Tick = parameter is null ? "Click a tick." : $"You clicked the tick {parameter}.");

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<Point> Pages { get; } = new Datasets().Pages;

    public IReadOnlyList<Sample> Samples { get; } = new InteractionData().Samples;

    public IReadOnlyList<Slice> Groups { get; } = PolarData.Groups;

    public IReadOnlyList<Stage> Stages { get; } = new Hierarchies().Funnel;

    public IReadOnlyList<Subject> Subjects { get; } = new PolarData().Subjects;

    public IReadOnlyList<AgeGroup> Ages { get; } = new PolarData().Ages;

    /// <summary>What the last tick click named.</summary>
    public string Tick
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tick)));
        }
    } = "Click a tick.";

    /// <summary>The brush's first item.</summary>
    public int? BrushStart { get; set; } = 10;

    /// <summary>The brush's last item.</summary>
    public int? BrushEnd { get; set; } = 18;

    /// <summary>Runs with the clicked tick's value.</summary>
    public ICommand PickTick { get; }

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
