using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;

namespace Gallery.Models;

/// <summary>
/// The model of the page that shows every interaction of the fidelity matrix on every family: each family's clicks run
/// a command of its own that writes what was clicked, and the cartesian charts' brushes are bound to a window each.
/// </summary>
public sealed class FamilyInteractions : INotifyPropertyChanged
{
    public FamilyInteractions()
    {
        PickLine = Picker("Line chart");
        PickArea = Picker("Area chart");
        PickBar = Picker("Bar chart");
        PickComposed = Picker("Composed chart");
        PickScatter = Picker("Scatter chart");
        PickPie = Picker("Pie chart");
        PickRadar = Picker("Radar chart");
        PickRadialBar = Picker("Radial bar chart");
        PickFunnel = Picker("Funnel chart");
        PickTreemap = Picker("Treemap");
        PickSankey = Picker("Sankey");
        PickSunburst = Picker("Sunburst chart");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<Point> Pages { get; } = new Datasets().Pages;

    public IReadOnlyList<Point> Composed { get; } = new Datasets().Composed;

    public IReadOnlyList<ScatterPoint> Points { get; } = ScatterSets.SchoolA;

    public IReadOnlyList<Slice> Groups { get; } = PolarData.SixGroups;

    public IReadOnlyList<Subject> Subjects { get; } = new PolarData().Subjects;

    public IReadOnlyList<AgeGroup> Ages { get; } = new PolarData().Ages;

    public IReadOnlyList<Stage> Stages { get; } = new Hierarchies().Funnel;

    public IReadOnlyList<Package> Packages { get; } = new Hierarchies().FlareTop;

    public Flow Visits { get; } = new Hierarchies().Visits;

    public Segment Tree { get; } = new SunburstData().Tree;

    /// <summary>What the last click ran: the family and the parameter its command received.</summary>
    public string Picked
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Picked)));
        }
    } = "Click an item of any chart.";

    public int? LineStart { get; set; } = 1;

    public int? LineEnd { get; set; } = 5;

    public int? AreaStart { get; set; } = 1;

    public int? AreaEnd { get; set; } = 5;

    public int? BarStart { get; set; } = 1;

    public int? BarEnd { get; set; } = 5;

    public int? ComposedStart { get; set; } = 0;

    public int? ComposedEnd { get; set; } = 4;

    public ICommand PickLine { get; }

    public ICommand PickArea { get; }

    public ICommand PickBar { get; }

    public ICommand PickComposed { get; }

    public ICommand PickScatter { get; }

    public ICommand PickPie { get; }

    public ICommand PickRadar { get; }

    public ICommand PickRadialBar { get; }

    public ICommand PickFunnel { get; }

    public ICommand PickTreemap { get; }

    public ICommand PickSankey { get; }

    public ICommand PickSunburst { get; }

    private Command Picker(string family) => new(parameter => Picked = string.Create(CultureInfo.InvariantCulture, $"{family}: the command ran with \"{parameter}\"."));

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
