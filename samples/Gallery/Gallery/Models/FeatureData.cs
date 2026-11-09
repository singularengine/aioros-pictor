namespace Gallery.Models;

/// <summary>A row of SimpleBarChart's data with an error per value, for the error bar example.</summary>
public sealed record ErrorPoint(string Name, double Uv, double Pv, double PvError, double UvError);

/// <summary>The data of the vertical, error bar and reference examples.</summary>
public sealed class FeatureData
{
    public IReadOnlyList<Point> Pages { get; } = new Datasets().Pages;

    public IReadOnlyList<Point> Composed { get; } = new Datasets().Composed;

    public IReadOnlyList<ErrorPoint> Errors { get; } =
    [
        new("Page A", 4000, 2400, 300, 400),
        new("Page B", 3000, 1398, 200, 250),
        new("Page C", 2000, 9800, 900, 300),
        new("Page D", 2780, 3908, 400, 280),
        new("Page E", 1890, 4800, 500, 190),
        new("Page F", 2390, 3800, 350, 240),
        new("Page G", 3490, 4300, 450, 350),
    ];
}
