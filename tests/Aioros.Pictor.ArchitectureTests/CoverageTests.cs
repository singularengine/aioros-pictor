namespace Aioros.Pictor.ArchitectureTests;

public sealed class CoverageTests
{
    private static readonly string[] Statuses = ["done", "partial", "missing", "n/a", "—"];

    [Fact]
    [Trait("AC", "COVERAGE-01")]
    public async Task EveryDoneRowNamesItsCriteriaAsync()
    {
        var document = await CoverageDocument.ReadAsync();

        var wrong = document.Rows.Where(row => !Statuses.Contains(row.Kind)).Select(row => $"{row.Where}: unknown status '{row.Status}'")
            .Concat(document.Rows.Where(row => row.Kind == "done" && !CoverageDocument.Identifiers(row.Criteria).Any()).Select(row => $"{row.Where}: done without criteria"));

        Assert.NotEmpty(document.Rows);
        Assert.Empty(wrong);
    }

    [Fact]
    [Trait("AC", "COVERAGE-02")]
    public async Task EveryCriterionNamedExistsAsync()
    {
        var document = await CoverageDocument.ReadAsync();
        var criteria = (await Repository.CriteriaAsync()).ToHashSet(StringComparer.Ordinal);

        var named = document.Rows.SelectMany(row => CoverageDocument.Identifiers(row.Criteria).Select(id => (row.Where, Id: id)))
            .Concat(document.Matrix.Where(cell => !cell.IsNotApplicable).SelectMany(cell => CoverageDocument.Identifiers(cell.Text).Select(id => (cell.Where, Id: id))));

        Assert.Empty(named.Where(name => !criteria.Contains(name.Id)).Select(name => $"{name.Where}: {name.Id}"));
    }

    [Fact]
    [Trait("AC", "COVERAGE-03")]
    public async Task EveryMatrixCellNamesCriteriaOrAReasonAsync()
    {
        var document = await CoverageDocument.ReadAsync();

        var malformed = document.Matrix
            .Where(cell => cell.Text.Length > 0 && !CoverageDocument.HoldsOnlyCriteria(cell.Text) && !CoverageDocument.IsReasonedNotApplicable(cell.Text))
            .Select(cell => $"{cell.Where}: '{cell.Text}'");
        var empty = document.Matrix.Where(cell => cell.Text.Length == 0).Select(cell => cell.Where).ToList();
        Report("empty matrix cells", empty);

        Assert.NotEmpty(document.Matrix);
        Assert.Empty(malformed);
        if (document.Finished is true)
        {
            Assert.Empty(empty);
        }
    }

    [Fact]
    [Trait("AC", "COVERAGE-04")]
    public async Task EveryRowIsDoneOrNotApplicableOnceFinishedAsync()
    {
        var document = await CoverageDocument.ReadAsync();

        var open = document.Rows.Where(row => row.Kind is "partial" or "missing").Select(row => $"{row.Where} ({row.Kind})").ToList();
        Report("rows neither done nor n/a", open);

        Assert.NotNull(document.Finished);
        if (document.Finished is true)
        {
            Assert.Empty(open);
        }
    }

    [Fact]
    [Trait("AC", "COVERAGE-05")]
    public async Task TheCountsMatchTheTablesAsync()
    {
        var document = await CoverageDocument.ReadAsync();

        var kinds = document.Rows.GroupBy(row => row.Kind).ToDictionary(group => group.Key, group => group.Count());
        var filled = document.Matrix.Count(cell => cell.Text.Length > 0);
        var expected = $"Counts: done {Count(kinds, "done")} · partial {Count(kinds, "partial")} · missing {Count(kinds, "missing")} · n/a {Count(kinds, "n/a")} · fidelity matrix {filled} of {document.Matrix.Count} cells filled";

        Assert.Equal(expected, document.CountsLine);
    }

    private static int Count(Dictionary<string, int> kinds, string kind) => kinds.GetValueOrDefault(kind);

    private static void Report(string what, List<string> items) =>
        TestContext.Current.TestOutputHelper?.WriteLine($"{items.Count} {what}:{Environment.NewLine}{string.Join(Environment.NewLine, items)}");
}
