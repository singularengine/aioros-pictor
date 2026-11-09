namespace Gallery.Models;

/// <summary>A pie's item: its name, value and, where Recharts' data gives one, its own fill.</summary>
public sealed record Slice(string Name, double Value, string? Fill = null);

/// <summary>A row of Recharts' radar examples.</summary>
public sealed record Subject(string Name, double A, double B, double FullMark);

/// <summary>A row of Recharts' radial bar example.</summary>
public sealed record AgeGroup(string Name, double Uv, double Pv, string Fill);

/// <summary>The data of Recharts' polar examples, as the Recharts 2 site gives it.</summary>
public sealed class PolarData
{
    /// <summary>The data of the page's motion demo, at the version its "Change data" button moves on.</summary>
    public Motion Motion { get; } = new();

    public static IReadOnlyList<Slice> Groups { get; } =
    [
        new("Group A", 400),
        new("Group B", 300),
        new("Group C", 300),
        new("Group D", 200),
    ];

    public static IReadOnlyList<Slice> SixGroups { get; } =
    [
        new("Group A", 400),
        new("Group B", 300),
        new("Group C", 300),
        new("Group D", 200),
        new("Group E", 278),
        new("Group F", 189),
    ];

    public static IReadOnlyList<Slice> Ring { get; } =
    [
        new("A1", 100),
        new("A2", 300),
        new("B1", 100),
        new("B2", 80),
        new("B3", 40),
        new("B4", 30),
        new("B5", 50),
        new("C1", 100),
        new("C2", 200),
        new("D1", 150),
        new("D2", 50),
    ];

    public static IReadOnlyList<Slice> BigGroups { get; } =
    [
        new("Group A", 2400),
        new("Group B", 4567),
        new("Group C", 1398),
        new("Group D", 9800),
        new("Group E", 3908),
        new("Group F", 4800),
    ];

    public static IReadOnlyList<Slice> ActiveGroups { get; } =
    [
        new("Group A", 400),
        new("Group B", 300),
        new("Group C", 100),
        new("Group D", 200),
    ];

    public IReadOnlyList<Slice> Pie { get; } = SixGroups;

    public IReadOnlyList<Subject> Subjects { get; } =
    [
        new("Math", 120, 110, 150),
        new("Chinese", 98, 130, 150),
        new("English", 86, 130, 150),
        new("Geography", 99, 100, 150),
        new("Physics", 85, 90, 150),
        new("History", 65, 85, 150),
    ];

    public IReadOnlyList<AgeGroup> Ages { get; } =
    [
        new("18-24", 31.47, 2400, "#8884d8"),
        new("25-29", 26.69, 4567, "#83a6ed"),
        new("30-34", 15.69, 1398, "#8dd1e1"),
        new("35-39", 8.22, 9800, "#82ca9d"),
        new("40-49", 8.63, 3908, "#a4de6c"),
        new("50+", 2.63, 4800, "#d0ed57"),
        new("unknow", 6.67, 4800, "#ffc658"),
    ];
}
