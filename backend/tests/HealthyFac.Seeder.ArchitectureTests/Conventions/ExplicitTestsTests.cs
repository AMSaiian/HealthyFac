using System.Text.RegularExpressions;

namespace HealthyFac.Seeder.ArchitectureTests.Conventions;

public sealed partial class ExplicitTestsTests
{
    private static readonly string[] SeederTestFolders =
    [
        "backend/tests/HealthyFac.Seeder.UnitTests",
        "backend/tests/HealthyFac.Seeder.IntegrationTests",
    ];

    [Fact]
    public void SeederTests_AreExplicit()
    {
        var repositoryRoot = FindRepositoryRoot();
        var attributes = SeederTestFolders
            .SelectMany(folder => Directory.EnumerateFiles(
                Path.Combine(repositoryRoot, folder),
                searchPattern: "*.cs",
                SearchOption.AllDirectories))
            .Where(file => !IsBuildOutput(file))
            .SelectMany(ReadTestAttributes)
            .ToList();

        attributes.Should().NotBeEmpty(because: "a moved folder must not pass with no test found");
        var violations = attributes
            .Where(attribute => !attribute.IsExplicit)
            .Select(attribute => $"{attribute.File}:{attribute.Line}");
        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, path2: "backend", path3: "HealthyFac.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException(
                message: "The repository root with backend/HealthyFac.slnx was not found.");
    }

    private static bool IsBuildOutput(string file)
    {
        var segments = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Contains(value: "bin") || segments.Contains(value: "obj");
    }

    private static IEnumerable<TestAttribute> ReadTestAttributes(string file)
    {
        var lines = File.ReadAllLines(file);
        for (var index = 0; index < lines.Length; index++)
        {
            if (!TestAttributeRegex().IsMatch(lines[index]))
            {
                continue;
            }

            yield return new TestAttribute
            {
                File = file,
                Line = index + 1,
                IsExplicit = ExplicitRegex().IsMatch(lines[index]),
            };
        }
    }

    [GeneratedRegex(pattern: @"^\s*\[(Fact|Theory)\b")]
    private static partial Regex TestAttributeRegex();

    [GeneratedRegex(pattern: @"\bExplicit\s*=\s*true\b")]
    private static partial Regex ExplicitRegex();

    private sealed record TestAttribute
    {
        public required string File { get; init; }

        public required int Line { get; init; }

        public required bool IsExplicit { get; init; }
    }
}
