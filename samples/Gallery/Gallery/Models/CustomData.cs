namespace Gallery.Models;

/// <summary>A page of Recharts' example data with the sentence CustomContentOfTooltip shows for it.</summary>
public sealed record IntroPage(string Name, double Uv, double Pv, double Amt, string Intro);

/// <summary>The data of the custom content examples (Recharts' shape, dot, label, tick and content elements as XAML templates).</summary>
public sealed class CustomData
{
    public Motion Motion { get; } = new();

    public IReadOnlyList<Point> Pages { get; } = new Datasets().Pages;

    public IReadOnlyList<IntroPage> IntroPages { get; } =
    [
        new("Page A", 4000, 2400, 2400, "Page A is about men's clothing"),
        new("Page B", 3000, 1398, 2210, "Page B is about women's dress"),
        new("Page C", 2000, 9800, 2290, "Page C is about women's bag"),
        new("Page D", 2780, 3908, 2000, "Page D is about household goods"),
        new("Page E", 1890, 4800, 2181, "Page E is about food"),
        new("Page F", 2390, 3800, 2500, "Page F is about baby food"),
    ];

    public IReadOnlyList<Slice> Groups { get; } = PolarData.Groups;

    public IReadOnlyList<ScatterPoint> SchoolA { get; } = ScatterSets.SchoolA;

    public IReadOnlyList<Stage> Stages { get; } = new Hierarchies().Funnel;

    public IReadOnlyList<Subject> Subjects { get; } = new PolarData().Subjects;

    public IReadOnlyList<AgeGroup> Ages { get; } = new PolarData().Ages;

    public IReadOnlyList<Package> Flare { get; } = new Hierarchies().Flare;

    public IReadOnlyList<Package> FlareTop { get; } = new Hierarchies().FlareTop;

    public Flow Visits { get; } = new Hierarchies().Visits;
}
