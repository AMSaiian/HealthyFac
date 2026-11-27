using System.Text.RegularExpressions;
using HealthyFac.Shared.UnitTests.Conventions.Data;
using HealthyFac.Testing.Features;
using HealthyFac.Testing.Repository;

namespace HealthyFac.Shared.UnitTests.Conventions;

[Feature(TestFeatures.Conventions)]
public sealed class OptionsValidatorPlacementTests
{
    private static readonly char[] PathSeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    [Fact]
    public void OptionsValidators_InBackendSources_LiveInTheirOptionsFile()
    {
        var sourceFiles = FindSourceFolder()
            .GetFiles(
                OptionsValidatorPlacementConstants.SourcePattern,
                SearchOption.AllDirectories)
            .Where(file => !IsSkipped(file))
            .ToList();

        var misplacedPaths = sourceFiles
            .Where(IsMisplaced)
            .Select(file => file.FullName)
            .Order()
            .ToList();

        misplacedPaths.Should().BeEmpty();
    }

    private static DirectoryInfo FindSourceFolder()
        => new(
            Path.Combine(
                RepositoryRoot.Find().FullName,
                OptionsValidatorPlacementConstants.BackendFolderName,
                OptionsValidatorPlacementConstants.SourceFolderName));

    private static bool IsSkipped(FileInfo file)
        => file.FullName
            .Split(PathSeparators)
            .Any(OptionsValidatorPlacementConstants.SkippedFolders.Contains);

    private static bool IsMisplaced(FileInfo file)
    {
        if (file.Name.EndsWith(OptionsValidatorPlacementConstants.ValidatorFileSuffix, StringComparison.Ordinal))
        {
            return true;
        }

        return Regex
            .Matches(File.ReadAllText(file.FullName), OptionsValidatorPlacementConstants.ValidatorPattern)
            .Any(match => file.Name != $"{match.Groups["options"].Value}.cs");
    }
}
