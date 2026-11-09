namespace Gallery.Models;

/// <summary>One item of Recharts' BrushBarChart example.</summary>
public sealed record Sample(string Name, double Uv, double Pv);

/// <summary>The data of the legend toggle and brush examples.</summary>
public sealed class InteractionData
{
    /// <summary>The first brush's window, which its range inputs post (its end unset: the last item).</summary>
    public int? WholeStart { get; set; }

    /// <summary>The first brush's last item, unset for the last.</summary>
    public int? WholeEnd { get; set; }

    /// <summary>The second brush's window.</summary>
    public int? WindowStart { get; set; } = 5;

    /// <summary>The second brush's last item.</summary>
    public int? WindowEnd { get; set; } = 15;

    public IReadOnlyList<Point> Pages { get; } = new Datasets().Pages;

    public IReadOnlyList<Sample> Samples { get; } =
    [
        new("1", 300, 456),
        new("2", -145, 230),
        new("3", -100, 345),
        new("4", -8, 450),
        new("5", 100, 321),
        new("6", 9, 235),
        new("7", 53, 267),
        new("8", 252, -378),
        new("9", 79, -210),
        new("10", 294, -23),
        new("12", 43, 45),
        new("13", -74, 90),
        new("14", -71, 130),
        new("15", -117, 11),
        new("16", -186, 107),
        new("17", -16, 926),
        new("18", -125, 653),
        new("19", 222, 366),
        new("20", 372, 486),
        new("21", 182, 512),
        new("22", 164, 302),
        new("23", 316, 425),
        new("24", 131, 467),
        new("25", 291, -190),
        new("26", -47, 194),
        new("27", -415, 371),
        new("28", -182, 376),
        new("29", -93, 295),
        new("30", -99, 322),
        new("31", -52, 246),
        new("32", 154, 33),
        new("33", 205, 354),
        new("34", 70, 258),
        new("35", -25, 359),
        new("36", -59, 192),
        new("37", -63, 464),
        new("38", -91, -2),
        new("39", -66, 154),
        new("40", -50, 186),
    ];
}
