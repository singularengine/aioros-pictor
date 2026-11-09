using System.ComponentModel;
using System.Windows.Input;

namespace Gallery.Models;

/// <summary>
/// The update cases Recharts' frames are recorded for (`tools/recharts/catalog/motion.mjs`): each chart's data before
/// and after the change, the version moving between them, so on the web a post morphs every chart into its new data
/// and the update animations can be compared with Recharts' frame by frame.
/// </summary>
public sealed class UpdateCases : INotifyPropertyChanged
{
    private static readonly string[] Changed =
        [nameof(Version), nameof(LineValues), nameof(LineMore), nameof(LineFewer), nameof(LineFromEmpty), nameof(AreaValues), nameof(StackedAreaMore), nameof(BarMore), nameof(Points), nameof(FirstPie), nameof(SecondPie), nameof(Subjects), nameof(Ages), nameof(Stages)];

    private static readonly Point[] Extra = [new("Page H", 2600, 5200, 2300), new("Page I", 3100, 2900, 2400)];

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The data's version: even before the change, odd after it.</summary>
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

    /// <summary>Moves every chart to its other data.</summary>
    public ICommand Change => field ??= new ChangeCommand(() => Version++);

    private bool After => (int)Version % 2 == 1;

    private static IReadOnlyList<Point> Pages => new Datasets().Pages;

    public IReadOnlyList<Point> LineValues => After ? Rotate(Pages, static (row, next) => row with { Pv = next.Pv, Uv = next.Uv }) : Pages;

    public IReadOnlyList<Point> LineMore => After ? [.. Pages, .. Extra] : Pages;

    public IReadOnlyList<Point> LineFewer => After ? [.. Pages.Take(5)] : Pages;

    public IReadOnlyList<Point> LineFromEmpty => After ? Pages : [];

    public IReadOnlyList<Point> AreaValues => After ? Rotate(Pages, static (row, next) => row with { Uv = next.Uv }) : Pages;

    public IReadOnlyList<Point> StackedAreaMore => After ? [.. Rotate(Pages, static (row, next) => row with { Uv = next.Uv, Pv = next.Pv, Amt = next.Amt }), .. Extra] : Pages;

    public IReadOnlyList<Point> BarMore => After ? [.. Rotate(Pages, static (row, next) => row with { Pv = next.Pv, Uv = next.Uv }), Extra[0]] : Pages;

    public IReadOnlyList<ScatterPoint> Points => After ? [.. Rotate(ScatterSets.SchoolA, static (row, next) => row with { Y = next.Y }), new(130, 350, 300), new(160, 220, 200)] : ScatterSets.SchoolA;

    public IReadOnlyList<Slice> FirstPie => After ? [.. Rotate(PolarData.SixGroups, static (row, next) => row with { Value = next.Value }), new("Group G", 250)] : PolarData.SixGroups;

    public IReadOnlyList<Slice> SecondPie => After ? [.. PolarData.BigGroups.Take(4)] : PolarData.BigGroups;

    public IReadOnlyList<Subject> Subjects => After ? Rotate(new PolarData().Subjects, static (row, next) => row with { A = next.A, B = next.B }) : new PolarData().Subjects;

    public IReadOnlyList<AgeGroup> Ages => After ? Rotate(new PolarData().Ages, static (row, next) => row with { Uv = next.Uv, Pv = next.Pv }) : new PolarData().Ages;

    public IReadOnlyList<Stage> Stages => After ? [.. new Hierarchies().Funnel.Select(stage => stage with { Value = Math.Round(stage.Value * 0.8, MidpointRounding.AwayFromZero) }), new("Retained", 10, "#a4de6c")] : new Hierarchies().Funnel;

    private static IReadOnlyList<T> Rotate<T>(IReadOnlyList<T> rows, Func<T, T, T> take) =>
        [.. rows.Select((row, i) => take(row, rows[(i + 1) % rows.Count]))];

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
