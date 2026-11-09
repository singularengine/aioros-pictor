namespace Gallery.Models;

/// <summary>A radar subject with a score and hours, on two scales.</summary>
public sealed record Effort(string Subject, double A, double Hours);

/// <summary>The data of the page of props closed after the coverage audit.</summary>
public sealed class MoreData
{
    public IReadOnlyList<Point> Pages { get; } = new Datasets().Pages;

    public IReadOnlyList<Point> Gaps { get; } =
    [
        new("Page A", 4000, 2400, 2400),
        new("Page B", 3000, 1398, 2210),
        new("Page C", null, 9800, 2290),
        new("Page D", 2780, 3908, null),
        new("Page E", 1890, null, 2181),
        new("Page F", 2390, 3800, 2500),
        new("Page G", 3490, 4300, 2100),
    ];

    public IReadOnlyList<Point> Small { get; } =
    [
        new("Page A", 4000, 2400, 2400, 0),
        new("Page B", 3000, 1398, 2210, 40),
        new("Page C", 2000, 9800, 2290, 2500),
        new("Page D", 2780, 3908, 2000, 15),
        new("Page E", 1890, 4800, 2181, 1800),
        new("Page F", 2390, 3800, 2500, 900),
        new("Page G", 3490, 4300, 2100, 3000),
    ];

    public IReadOnlyList<Effort> Scores { get; } =
    [
        new("Math", 120, 12),
        new("Chinese", 98, 30),
        new("English", 86, 22),
        new("Geography", 99, 8),
        new("Physics", 85, 17),
        new("History", 65, 26),
    ];

    public IReadOnlyList<Band> Bands { get; } =
    [
        new("18-24", 31.47, 12, "#8884d8"),
        new("25-29", 26.69, 20, "#83a6ed"),
        new("30-34", 15.69, 9, "#8dd1e1"),
        new("35-39", 8.22, 14, "#82ca9d"),
        new("40-49", 0.4, 6, "#a4de6c"),
        new("50+", 2.63, 3, "#d0ed57"),
    ];
}
