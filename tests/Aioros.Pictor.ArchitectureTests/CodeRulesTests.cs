using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aioros.Pictor.ArchitectureTests;

public sealed class CodeRulesTests
{
    public static TheoryData<string> Folders { get; } = ["src", "tests", "bench"];

    [Theory]
    [MemberData(nameof(Folders))]
    public async Task EveryClassIsSealedOrStaticAsync(string folder)
    {
        var files = await Repository.ParseAsync(folder);

        var kept = await KeptAbstractAsync();
        var open = from file in files
                   from type in file.Root.DescendantNodes().OfType<TypeDeclarationSyntax>()
                   where type is ClassDeclarationSyntax || (type is RecordDeclarationSyntax record && !record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword))
                   where !type.Modifiers.Any(SyntaxKind.SealedKeyword) && !type.Modifiers.Any(SyntaxKind.StaticKeyword)
                   where !((type.Modifiers.Any(SyntaxKind.AbstractKeyword) || type.Modifiers.Any(SyntaxKind.PartialKeyword)) && kept.Contains(type.Identifier.Text))
                   where !IsBenchmark(type)
                   select $"{file.Path}: {type.Identifier.Text}";

        Assert.Empty(open);
    }

    /// <summary>The abstract classes docs/conventions.md keeps, each named in backticks on its line "Abstract base classes kept".</summary>
    private static async Task<HashSet<string>> KeptAbstractAsync()
    {
        var line = (await File.ReadAllLinesAsync(Path.Combine(Repository.Root, "docs", "conventions.md"))).FirstOrDefault(text => text.Contains("Abstract base classes kept", StringComparison.Ordinal)) ?? string.Empty;
        return [.. System.Text.RegularExpressions.Regex.Matches(line, "`([A-Za-z]+)`").Select(match => match.Groups[1].Value)];
    }

    private static bool IsBenchmark(TypeDeclarationSyntax type) =>
        type.Members.OfType<MethodDeclarationSyntax>().Any(m => m.AttributeLists.SelectMany(l => l.Attributes).Any(a => a.Name.ToString() == "Benchmark"));

    [Fact]
    public async Task TheSourcesHaveNoLocksNoConcurrentCollectionsAndNoReflectionAsync()
    {
        var files = await Repository.ParseAsync("src");

        var found = from file in files
                    from node in file.Root.DescendantNodes()
                    where node is LockStatementSyntax
                        || (node is UsingDirectiveSyntax directive && directive.Name?.ToString() is "System.Reflection" or "System.Collections.Concurrent")
                        || (node is QualifiedNameSyntax name && name.ToString().StartsWith("System.Reflection", StringComparison.Ordinal))
                    select $"{file.Path}: {node}";

        Assert.Empty(found);
    }

    [Fact]
    public async Task TheSourcesHaveNoCommentsAsync()
    {
        var files = await Repository.ParseAsync("src");

        var comments = from file in files
                       from trivia in file.Root.DescendantTrivia()
                       where trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                       select $"{file.Path}: {trivia}";

        Assert.Empty(comments);
    }

    [Theory]
    [MemberData(nameof(Folders))]
    public async Task NoTypeIsLongerThan600LinesAsync(string folder)
    {
        var files = await Repository.ParseAsync(folder);

        var lines = from file in files
                    from type in file.Root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
                    let span = type.GetLocation().GetLineSpan()
                    group (file.Path, Lines: span.EndLinePosition.Line - span.StartLinePosition.Line + 1) by FullName(type) into declarations
                    where declarations.Sum(declaration => declaration.Lines) > 600
                    select $"{string.Join(", ", declarations.Select(declaration => declaration.Path))}: {declarations.Key} ({declarations.Sum(declaration => declaration.Lines)} lines)";

        Assert.Empty(lines);
    }

    /// <summary>A type's namespace, containing types and name: the partial declarations of one type share it, so the rule measures the type across them.</summary>
    private static string FullName(BaseTypeDeclarationSyntax type) =>
        string.Join('.', type.AncestorsAndSelf().Select(node => node switch
        {
            BaseNamespaceDeclarationSyntax space => space.Name.ToString(),
            BaseTypeDeclarationSyntax declared => declared.Identifier.Text,
            _ => null,
        }).Where(part => part is not null).Reverse());

    [Fact]
    public void TheWebPathReferencesNeitherAvaloniaNorAspNetCore()
    {
        var project = XDocument.Load(Path.Combine(Repository.Root, "src", "Aioros.Pictor", "Aioros.Pictor.csproj"));

        var packages = project.Descendants().Where(e => e.Name.LocalName == "PackageReference").Select(e => e.Attribute("Include")?.Value ?? "").ToList();

        Assert.All(packages, package => Assert.Contains(package, (string[])["Aioros.Graphs", "Aioros.Graphs.Svg", "Aioros.Runtime"]));
        Assert.DoesNotContain(project.Descendants(), e => e.Name.LocalName is "ProjectReference" or "FrameworkReference");
    }

    [Fact]
    public void OnlyTheControlsTheSamplesAndTheTestsReferenceAvalonia()
    {
        var referencing = Directory.EnumerateFiles(Repository.Root, "*.csproj", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint })
            .Where(path => !Path.GetRelativePath(Repository.Root, path).StartsWith("samples", StringComparison.Ordinal))
            .Where(path => !Path.GetRelativePath(Repository.Root, path).StartsWith("scratch", StringComparison.Ordinal))
            .Where(path => XDocument.Load(path).Descendants().Any(e => e.Name.LocalName == "PackageReference" && (e.Attribute("Include")?.Value ?? "").StartsWith("Avalonia", StringComparison.Ordinal)))
            .Select(Path.GetFileName)
            .ToList();

        Assert.All(referencing, name => Assert.Contains(name, (string[])["Aioros.Pictor.Avalonia.csproj", "Aioros.Pictor.Tests.csproj", "Aioros.Pictor.Benchmarks.csproj"]));
    }
}
