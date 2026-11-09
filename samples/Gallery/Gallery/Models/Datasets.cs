namespace Gallery.Models;

/// <summary>One row of Recharts' example data.</summary>
public sealed record Point(string Name, double? Uv, double? Pv, double? Amt, double? Cnt = null);

/// <summary>The data of Recharts' examples, as recharts.org gives it.</summary>
public sealed class Datasets
{
    /// <summary>The data of the page's motion demo, at the version its "Change data" button moves on.</summary>
    public Motion Motion { get; } = new();

    public IReadOnlyList<Point> Pages { get; } =
    [
        new("Page A", 4000, 2400, 2400),
        new("Page B", 3000, 1398, 2210),
        new("Page C", 2000, 9800, 2290),
        new("Page D", 2780, 3908, 2000),
        new("Page E", 1890, 4800, 2181),
        new("Page F", 2390, 3800, 2500),
        new("Page G", 3490, 4300, 2100),
    ];

    public IReadOnlyList<Point> PositiveAndNegative { get; } =
    [
        new("Page A", 4000, 2400, 2400),
        new("Page B", -3000, 1398, 2210),
        new("Page C", -2000, -9800, 2290),
        new("Page D", 2780, 3908, 2000),
        new("Page E", -1890, 4800, 2181),
        new("Page F", 2390, -3800, 2500),
        new("Page G", 3490, 4300, 2100),
    ];

    public IReadOnlyList<Point> Composed { get; } =
    [
        new("Page A", 590, 800, 1400, 490),
        new("Page B", 868, 967, 1506, 590),
        new("Page C", 1397, 1098, 989, 350),
        new("Page D", 1480, 1200, 1228, 480),
        new("Page E", 1520, 1108, 1100, 460),
        new("Page F", 1400, 680, 1700, 380),
    ];
}
