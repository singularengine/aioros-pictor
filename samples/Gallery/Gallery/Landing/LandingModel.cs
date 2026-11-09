using Gallery.Models;

namespace Gallery.Landing;

/// <summary>The language a landing page is shown in.</summary>
public sealed record LandingData(string Slug);

/// <summary>Pictor's landing page: its text in the page's language and the data its twelve live charts draw.</summary>
public sealed class LandingModel(LandingData data)
{
    private static readonly Datasets Sets = new();

    private static readonly PolarData Polar = new();

    private static readonly Hierarchies Trees = new();

    public LandingCopy Copy { get; } = LandingCopy.For(data.Slug);

    public string AiorosSite { get; } = "https://github.com/singularengine/aioros";

    public IReadOnlyList<Point> Pages { get; } = Sets.Pages;

    public IReadOnlyList<Point> Composed { get; } = Sets.Composed;

    public IReadOnlyList<Slice> Slices { get; } = PolarData.Groups;

    public IReadOnlyList<Subject> Subjects { get; } = Polar.Subjects;

    public IReadOnlyList<AgeGroup> Ages { get; } = Polar.Ages;

    public IReadOnlyList<ScatterPoint> Scatter { get; } = ScatterSets.SchoolA;

    public IReadOnlyList<Stage> Funnel { get; } = Trees.Funnel;

    public IReadOnlyList<Package> Flare { get; } = Trees.FlareTop;

    public Flow Visits { get; } = Trees.Visits;

    public Segment Sunburst { get; } = new SunburstData().Tree;

    public string Sample { get; } = """
        <p:AreaChart Data="{Binding Visits}">
          <p:CartesianGrid StrokeDashArray="3 3" />
          <p:XAxis DataKey="Month" />
          <p:YAxis />
          <p:Tooltip />
          <p:Area Type="Monotone" DataKey="Web"
              Stroke="#8884d8" Fill="#8884d8" />
        </p:AreaChart>
        """;
}
