using System.Text.RegularExpressions;
using HealthyFac.Shared.UnitTests.Conventions.Data;

namespace HealthyFac.Shared.UnitTests.Conventions;

public sealed class OptionsValidatorPlacementTests
{
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
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            var backendFolder = new[]
                {
                    folder.FullName,
                    Path.Combine(folder.FullName, OptionsValidatorPlacementConstants.BackendFolderName),
                }
                .FirstOrDefault(candidate =>
                    File.Exists(Path.Combine(candidate, OptionsValidatorPlacementConstants.SolutionFileName)));

            if (backendFolder is not null)
            {
                return new DirectoryInfo(
                    Path.Combine(backendFolder, OptionsValidatorPlacementConstants.SourceFolderName));
            }
        }

        throw new DirectoryNotFoundException(
            $"No parent folder holds {OptionsValidatorPlacementConstants.SolutionFileName}, directly or in "
            + $"{OptionsValidatorPlacementConstants.BackendFolderName}/.");
    }

    private static bool IsSkipped(FileInfo file)
        => file.FullName
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(OptionsValidatorPlacementConstants.SkippedFolders.Contains);

    private static bool IsMisplaced(FileInfo file)
    {
        if (file.Name.EndsWith(OptionsValidatorPlacementConstants.ValidatorFileSuffix, StringComparison.Ordinal))
        {
            return true;
        }

        return Regex
            .Matches(File.ReadAllText(file.FullName), OptionsValidatorPlacementConstants.ValidatorPattern)
            .Any(match => file.Name != $"{match.Groups[groupname: "options"].Value}.cs");
    }
}
