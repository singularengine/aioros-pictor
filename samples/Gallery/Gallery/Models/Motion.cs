using System.ComponentModel;
using System.Windows.Input;

namespace Gallery.Models;

/// <summary>
/// The data a family page's motion demo draws, at a version the "Change data" command moves on: each version gives
/// every item the values of the next one (the last the first's), so the charts animate from their previous geometry
/// to the new. The version is a field of the page, so on the web each post carries it and the server renders the next.
/// </summary>
public sealed class Motion : INotifyPropertyChanged
{
    private static readonly string[] Changed =
        [nameof(Version), nameof(Pages), nameof(Composed), nameof(Groups), nameof(Subjects), nameof(Ages), nameof(Stages), nameof(Packages), nameof(Visits), nameof(Points), nameof(Tree)];

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The data's version, shown and posted with the page.</summary>
    public decimal Version
    {
        get;
        set
        {
            field = value;
            foreach (var name in Changed)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    /// <summary>Moves the data on to its next version.</summary>
    public ICommand Change => field ??= new ChangeCommand(() => Version++);

    private int Step => (int)Version;

    public IReadOnlyList<Point> Pages => Rotate(new Datasets().Pages, Step, static (row, next) => row with { Uv = next.Uv, Pv = next.Pv, Amt = next.Amt });

    public IReadOnlyList<Point> Composed => Rotate(new Datasets().Composed, Step, static (row, next) => row with { Uv = next.Uv, Pv = next.Pv, Amt = next.Amt, Cnt = next.Cnt });

    public IReadOnlyList<Slice> Groups => Rotate(PolarData.SixGroups, Step, static (row, next) => row with { Value = next.Value });

    public IReadOnlyList<Subject> Subjects => Rotate(new PolarData().Subjects, Step, static (row, next) => row with { A = next.A, B = next.B });

    public IReadOnlyList<AgeGroup> Ages => Rotate(new PolarData().Ages, Step, static (row, next) => row with { Uv = next.Uv, Pv = next.Pv });

    public IReadOnlyList<Stage> Stages => [.. new Hierarchies().Funnel.Select((stage, i) => stage with { Value = Math.Round(stage.Value * (1 - (0.12 * ((i + Step) % 3)))) })];

    public IReadOnlyList<Package> Packages => [.. new Hierarchies().FlareTop.Select((package, i) => Scaled(package, 1 + (0.6 * ((i + Step) % 3))))];

    public Flow Visits
    {
        get
        {
            var flow = new Hierarchies().Visits;
            return flow with { Links = [.. flow.Links.Select((link, i) => link with { Value = Math.Round(link.Value * (1 + (0.5 * ((i + Step) % 2)))) })] };
        }
    }

    public IReadOnlyList<ScatterPoint> Points => Rotate(ScatterSets.SchoolA, Step, static (row, next) => row with { Y = next.Y, Z = next.Z });

    public Segment Tree
    {
        get
        {
            var tree = new SunburstData().Tree;
            return tree with { Children = [.. (tree.Children ?? []).Select((child, i) => child with { Value = child.Value * (1 + (0.5 * ((i + Step) % 2))) })] };
        }
    }

    private static IReadOnlyList<T> Rotate<T>(IReadOnlyList<T> rows, int step, Func<T, T, T> take) =>
        [.. rows.Select((row, i) => take(row, rows[(i + step) % rows.Count]))];

    private static Package Scaled(Package package, double factor) =>
        package with { Size = Math.Round(package.Size * factor), Children = package.Children is { } children ? [.. children.Select(child => Scaled(child, factor))] : null };

    private sealed class ChangeCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute();
    }
}
