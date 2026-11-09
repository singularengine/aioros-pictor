namespace Gallery.Models;

/// <summary>A value at an instant, in milliseconds since the epoch.</summary>
public sealed record Moment(double Time, double Value);

/// <summary>A value of a named group: a month, a year, a region.</summary>
public sealed record Measure(string Name, double Value, double Other = 0);

/// <summary>A point of a line that carries its own items, by category.</summary>
public sealed record CategoryValue(string Category, double Value);

/// <summary>The own items of the three lines of Recharts' LineChartHasMultiSeries.</summary>
public static class MultiSeries
{
    public static IReadOnlyList<CategoryValue> One { get; } = [new("A", 10), new("B", 22), new("C", 15)];

    public static IReadOnlyList<CategoryValue> Two { get; } = [new("B", 31), new("C", 18), new("D", 25)];

    public static IReadOnlyList<CategoryValue> Three { get; } = [new("C", 12), new("D", 27), new("E", 20)];
}

/// <summary>The data of the axes, scales, curves and stacking examples.</summary>
public sealed class AxesData
{
    public IReadOnlyList<CategoryValue> SeriesOne { get; } = MultiSeries.One;

    public IReadOnlyList<Point> Pages { get; } = new Datasets().Pages;

    public IReadOnlyList<Point> PositiveAndNegative { get; } = new Datasets().PositiveAndNegative;

    public IReadOnlyList<Subject> Subjects { get; } = new PolarData().Subjects;

    public IReadOnlyList<AgeGroup> Ages { get; } = new PolarData().Ages;

    public IReadOnlyList<Measure> Months { get; } =
    [
        new("January", 4000, 2400),
        new("February", 3000, 1398),
        new("March", 2000, 9800),
        new("April", 2780, 3908),
        new("May", 1890, 4800),
        new("June", 2390, 3800),
        new("July", 3490, 4300),
        new("August", 3200, 2900),
        new("September", 2600, 5100),
        new("October", 3900, 4100),
        new("November", 2100, 3600),
        new("December", 4400, 5300),
    ];

    public IReadOnlyList<Measure> Growth { get; } =
    [
        new("2016", 12),
        new("2017", 95),
        new("2018", 410),
        new("2019", 1800),
        new("2020", 5200),
        new("2021", 21000),
        new("2022", 64000),
    ];

    public IReadOnlyList<Measure> Regions { get; } =
    [
        new("North America", 4200),
        new("South America", 1800),
        new("Western Europe", 3900),
        new("Asia Pacific", 5100),
        new("Africa", 900),
    ];

    public IReadOnlyList<Moment> Timeline { get; } =
    [
        new(1704067200000, 30),
        new(1704758400000, 42),
        new(1705708800000, 28),
        new(1706918400000, 51),
        new(1708128000000, 47),
        new(1709337600000, 63),
        new(1711756800000, 58),
    ];
}
