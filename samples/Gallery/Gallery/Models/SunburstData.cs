namespace Gallery.Models;

/// <summary>A node of a sunburst: its value, its own fill (inherited by its children) and its children.</summary>
public sealed record Segment(string Name, double Value, string? Fill = null, IReadOnlyList<Segment>? Children = null);

/// <summary>A node of a budget sunburst sized by its share.</summary>
public sealed record Share(string Name, double Size, string? Fill = null, IReadOnlyList<Share>? Children = null);

/// <summary>The data of the sunburst examples.</summary>
public sealed class SunburstData
{
    /// <summary>The data of the page's motion demo, at the version its "Change data" button moves on.</summary>
    public Motion Motion { get; } = new();

    public Segment Tree { get; } = new("Root", 100, Children:
    [
        new("Child1", 30, "#264653",
        [
            new("third child", 10),
            new("another child", 5),
            new("next child", 15, Children: [new("third level child", 5), new("third level child", 5), new("third level child", 5, Children: [new("level 4", 2)])]),
        ]),
        new("Child2", 20, "#2a9d8f",
        [
            new("another child", 10),
            new("next child", 10, Children: [new("level 3 of child 2", 5), new("level 3 of child 2", 3), new("level 3 of child 2", 2)]),
        ]),
        new("Child3", 20, "#e9c46a"),
        new("Child4", 10, "#F4A261", [new("child4 child", 5), new("child4 child", 5)]),
        new("Child5", 20, "#e76f51",
        [
            new("child5 child", 10, Children: [new("level 3 child of child 5", 5), new("level 3 child of child 5", 5)]),
            new("child5 child", 5),
            new("child5 child", 5),
        ]),
    ]);

    public Share Budget { get; } = new("Budget", 1000, Children:
    [
        new("Housing", 450, Children: [new("Rent", 350), new("Utilities", 100)]),
        new("Food", 250, "#82ca9d", [new("Groceries", 180), new("Restaurants", 70)]),
        new("Savings", 300, "#ffc658"),
    ]);
}
