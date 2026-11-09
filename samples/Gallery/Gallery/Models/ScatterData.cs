namespace Gallery.Models;

/// <summary>A point of Recharts' scatter examples: its x, y and z.</summary>
public sealed record ScatterPoint(double X, double Y, double? Z = null, double? ErrorX = null, double? ErrorY = null);

/// <summary>The scatter page's model: the examples' items, which the page gives each scatter as constants (<see cref="ScatterSets"/>).</summary>
public sealed class ScatterData
{
    /// <summary>The data of the page's motion demo, at the version its "Change data" button moves on.</summary>
    public Motion Motion { get; } = new();

    public IReadOnlyList<ScatterPoint> SchoolA { get; } = ScatterSets.SchoolA;

    public IReadOnlyList<ScatterPoint> SchoolB { get; } = ScatterSets.SchoolB;
}

/// <summary>The data of Recharts' scatter examples, as recharts.org gives it; each scatter holds its own items.</summary>
public static class ScatterSets
{
    public static IReadOnlyList<ScatterPoint> SchoolA { get; } =
    [
        new(100, 200, 200),
        new(120, 100, 260),
        new(170, 300, 400),
        new(140, 250, 280),
        new(150, 400, 500),
        new(110, 280, 200),
    ];

    public static IReadOnlyList<ScatterPoint> SchoolB { get; } =
    [
        new(200, 260, 240),
        new(240, 290, 220),
        new(190, 290, 250),
        new(198, 250, 210),
        new(180, 280, 260),
        new(210, 220, 230),
    ];

    public static IReadOnlyList<ScatterPoint> JointA { get; } = [new(10, 30), new(30, 200), new(45, 100), new(50, 400), new(70, 150), new(100, 250)];

    public static IReadOnlyList<ScatterPoint> JointB { get; } = [new(30, 20), new(50, 180), new(75, 240), new(100, 100), new(120, 190)];

    public static IReadOnlyList<ScatterPoint> Measured { get; } =
    [
        new(100, 200, ErrorX: 10, ErrorY: 30),
        new(120, 100, ErrorX: 5, ErrorY: 20),
        new(170, 300, ErrorX: 15, ErrorY: 50),
        new(140, 250, ErrorX: 8, ErrorY: 40),
        new(150, 400, ErrorX: 12, ErrorY: 60),
        new(110, 280, ErrorX: 6, ErrorY: 25),
    ];
}
