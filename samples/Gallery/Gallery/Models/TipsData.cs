namespace Gallery.Models;

/// <summary>A radial bar band: its name, two values and its fill.</summary>
public sealed record Band(string Name, double Uv, double Amt, string Fill);

/// <summary>A radar subject whose second value may be missing.</summary>
public sealed record Score(string Subject, double A, double? B);

/// <summary>The data of the tooltip, legend, label, shape and responsive examples.</summary>
public sealed class TipsData
{
    public IReadOnlyList<Point> Pages { get; } = new Datasets().Pages;

    public IReadOnlyList<Point> Gaps { get; } =
    [
        new("Page A", 4000, 2400, 2400),
        new("Page B", 3000, 1398, 2210),
        new("Page C", 2000, null, 2290),
        new("Page D", 2780, 3908, 2000),
        new("Page E", 1890, null, 2181),
        new("Page F", 2390, 3800, 2500),
        new("Page G", 3490, 4300, 2100),
    ];

    public IReadOnlyList<Band> Bands { get; } =
    [
        new("18-24", 31.47, 12, "#8884d8"),
        new("25-29", 26.69, 20, "#83a6ed"),
        new("30-34", 15.69, 8, "#8dd1e1"),
        new("35-39", 8.22, 30, "#82ca9d"),
        new("40-49", 0.6, 5, "#a4de6c"),
    ];

    public IReadOnlyList<Score> Scores { get; } =
    [
        new("Math", 120, 110),
        new("Chinese", 98, null),
        new("English", 86, 130),
        new("Geography", 99, 100),
        new("Physics", 85, 90),
        new("History", 65, 85),
    ];
}
