using System.Globalization;
using System.Text.RegularExpressions;

namespace Aioros.Pictor.ArchitectureTests;

internal sealed record CoverageRow(string Section, string Name, string Status, string Criteria)
{
    public string Kind => Status.Split(' ')[0];

    public string Where => $"{Section}: {Name}";
}

internal sealed record MatrixCell(string Family, string Aspect, string Text)
{
    public string Where => $"{Family} / {Aspect}";

    public bool IsNotApplicable => Text.StartsWith("n/a", StringComparison.Ordinal);
}

internal sealed partial record CoverageDocument(IReadOnlyList<CoverageRow> Rows, IReadOnlyList<MatrixCell> Matrix, bool? Finished, string? CountsLine)
{
    private const string MatrixSection = "Fidelity matrix";

    public static async Task<CoverageDocument> ReadAsync() =>
        Parse(await File.ReadAllLinesAsync(Path.Combine(Repository.Root, "docs", "coverage.md")));

    public static CoverageDocument Parse(IReadOnlyList<string> lines)
    {
        var rows = new List<CoverageRow>();
        var matrix = new List<MatrixCell>();
        bool? finished = null;
        string? counts = null;
        var section = "";
        var i = 0;
        while (i < lines.Count)
        {
            var line = lines[i++];
            if (line.StartsWith("##", StringComparison.Ordinal))
            {
                section = line.TrimStart('#').Trim();
            }
            else if (FinishedLine().Match(line) is { Success: true } marker)
            {
                finished = marker.Groups[1].Value == "true";
            }
            else if (line.StartsWith("Counts:", StringComparison.Ordinal))
            {
                counts = line;
            }
            else if (line.StartsWith('|') && i < lines.Count && Separator().IsMatch(lines[i]))
            {
                i = ReadTable(lines, i - 1, section, rows, matrix);
            }
        }

        return new CoverageDocument(rows, matrix, finished, counts);
    }

    public static IEnumerable<string> Identifiers(string cell)
    {
        foreach (Match match in Identifier().Matches(cell))
        {
            var area = match.Groups[1].Value;
            var first = match.Groups[2].Value;
            var last = match.Groups[3].Success ? match.Groups[3].Value : first;
            for (var number = int.Parse(first, CultureInfo.InvariantCulture); number <= int.Parse(last, CultureInfo.InvariantCulture); number++)
            {
                yield return $"{area}-{number.ToString(CultureInfo.InvariantCulture).PadLeft(first.Length, '0')}";
            }
        }
    }

    public static bool HoldsOnlyCriteria(string cell) =>
        Identifier().Replace(cell, "#").Split(',').All(part => part.Trim() == "#");

    public static bool IsReasonedNotApplicable(string cell) => NotApplicable().IsMatch(cell);

    private static int ReadTable(IReadOnlyList<string> lines, int start, string section, List<CoverageRow> rows, List<MatrixCell> matrix)
    {
        var header = Cells(lines[start]);
        var end = start + 2;
        var body = new List<string[]>();
        for (; end < lines.Count && lines[end].StartsWith('|'); end++)
        {
            body.Add(Cells(lines[end]));
        }

        var status = Array.IndexOf(header, "Status");
        if (section == MatrixSection)
        {
            matrix.AddRange(from row in body
                            from aspect in Enumerable.Range(1, header.Length - 1)
                            select new MatrixCell(row[0], header[aspect], aspect < row.Length ? row[aspect] : ""));
        }
        else if (status >= 0 && header[^1] == "Criteria")
        {
            rows.AddRange(body.Select(row => new CoverageRow(section, row[0], row[status], row.Length == header.Length ? row[^1] : "")));
        }

        return end;
    }

    private static string[] Cells(string line) =>
        [.. CellBorder().Split(line.Trim()[1..^1]).Select(cell => cell.Trim())];

    [GeneratedRegex(@"^Finished: (true|false)$")]
    private static partial Regex FinishedLine();

    [GeneratedRegex(@"^\|(\s*:?-+:?\s*\|)+$")]
    private static partial Regex Separator();

    [GeneratedRegex(@"(?<!\\)\|")]
    private static partial Regex CellBorder();

    [GeneratedRegex(@"\b([A-Z][A-Z0-9]*)-(\d{2,})(?:\.\.(?:\1-)?(\d{2,}))?\b")]
    private static partial Regex Identifier();

    [GeneratedRegex(@"^n/a\W+\w")]
    private static partial Regex NotApplicable();
}
