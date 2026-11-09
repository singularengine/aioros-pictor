namespace Gallery.Models;

/// <summary>A row of SimpleLineChart's data with an error on pv, for the data overflow example.</summary>
public sealed record OverflowPoint(string Name, double Uv, double Pv, double Amt, double PvError);

/// <summary>A day's lowest and highest temperature, a range value Recharts reads as <c>[low, high]</c>.</summary>
public sealed record TemperatureDay(string Day, double[] Temperature);

/// <summary>A row of SimpleBarChart's data with asymmetric errors, each a <c>(below, above)</c> pair.</summary>
public sealed record ErrorPairPoint(string Name, double Uv, double Pv, (double Below, double Above) PvError, (double Below, double Above) UvError);

/// <summary>A point of a scatter: an x and a y.</summary>
public sealed record PlotPoint(double X, double Y);

/// <summary>A subject's range of scores, for the range radar.</summary>
public sealed record ScoreRange(string Subject, List<double> Range);

/// <summary>The data of the clipping, reference and range examples.</summary>
public sealed class RangeData
{
    /// <summary>The brush with always shown texts' window, which its range inputs post.</summary>
    public int? TextStart { get; set; } = 5;

    /// <summary>Its last item.</summary>
    public int? TextEnd { get; set; } = 20;

    /// <summary>The brush with options' window.</summary>
    public int? OptionStart { get; set; } = 3;

    /// <summary>Its last item.</summary>
    public int? OptionEnd { get; set; } = 17;

    public IReadOnlyList<Point> Pages { get; } = new Datasets().Pages;

    public IReadOnlyList<Point> PositiveAndNegative { get; } = new Datasets().PositiveAndNegative;

    public Flow Visits { get; } = new Hierarchies().Visits;

    public IReadOnlyList<TemperatureDay> Temperatures { get; } =
    [
        new("05-01", [-1, 10]),
        new("05-02", [2, 15]),
        new("05-03", [3, 12]),
        new("05-04", [4, 12]),
        new("05-05", [12, 16]),
        new("05-06", [5, 16]),
        new("05-07", [3, 12]),
        new("05-08", [0, 8]),
        new("05-09", [-3, 5]),
    ];

    public IReadOnlyList<ErrorPairPoint> ErrorPairs { get; } =
    [
        new("Page A", 4000, 2400, (200, 600), (400, 100)),
        new("Page B", 3000, 1398, (100, 300), (250, 250)),
        new("Page C", 2000, 9800, (900, 400), (300, 600)),
        new("Page D", 2780, 3908, (400, 400), (280, 0)),
        new("Page E", 1890, 4800, (0, 500), (190, 400)),
        new("Page F", 2390, 3800, (350, 350), (240, 240)),
        new("Page G", 3490, 4300, (450, 150), (350, 100)),
    ];

    public IReadOnlyList<Sample> Days { get; } =
    [
        new("Day 1", 300, 456),
        new("Day 2", 145, 230),
        new("Day 3", 100, 345),
        new("Day 4", 8, 450),
        new("Day 5", 100, 321),
        new("Day 6", 9, 235),
        new("Day 7", 53, 267),
        new("Day 8", 252, 378),
        new("Day 9", 79, 210),
        new("Day 10", 294, 23),
        new("Day 11", 43, 45),
        new("Day 12", 74, 90),
        new("Day 13", 71, 130),
        new("Day 14", 117, 11),
        new("Day 15", 186, 107),
        new("Day 16", 16, 926),
        new("Day 17", 125, 653),
        new("Day 18", 222, 366),
        new("Day 19", 372, 486),
        new("Day 20", 182, 512),
        new("Day 21", 164, 302),
        new("Day 22", 316, 425),
        new("Day 23", 131, 467),
        new("Day 24", 291, 190),
        new("Day 25", 47, 194),
        new("Day 26", 415, 371),
        new("Day 27", 182, 376),
        new("Day 28", 93, 295),
        new("Day 29", 99, 322),
        new("Day 30", 52, 246),
    ];

    public IReadOnlyList<PlotPoint> Spread { get; } = SpreadPoints;

    public static IReadOnlyList<PlotPoint> SpreadPoints { get; } = [new(10, 30), new(30, 200), new(45, 100), new(50, 400), new(70, 150), new(100, 250)];

    public static IReadOnlyList<PlotPoint> TrendPoints { get; } = [new(30, 20), new(50, 180), new(75, 240), new(100, 100), new(120, 190)];

    public IReadOnlyList<ScoreRange> Scores { get; } =
    [
        new("Math", [60, 120]),
        new("Chinese", [50, 98]),
        new("English", [70, 86]),
        new("Geography", [40, 99]),
        new("Physics", [55, 85]),
        new("History", [30, 65]),
    ];

    public IReadOnlyList<OverflowPoint> Overflow { get; } =
    [
        new("Page A", 4000, 2400, 2400, 300),
        new("Page B", 3000, 1398, 2210, 200),
        new("Page C", 2000, 9800, 2290, 900),
        new("Page D", 2780, 3908, 2000, 400),
        new("Page E", 1890, 4800, 2181, 500),
        new("Page F", 2390, 3800, 2500, 350),
        new("Page G", 3490, 4300, 2100, 450),
    ];
}
