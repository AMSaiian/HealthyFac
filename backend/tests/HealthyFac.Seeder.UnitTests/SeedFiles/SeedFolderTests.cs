using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.UnitTests.SeedFiles.Data;
using HealthyFac.Shared.Application.CurrentUser.Actors;

namespace HealthyFac.Seeder.UnitTests.SeedFiles;

public sealed class SeedFolderTests
{
    [Fact(Explicit = true)]
    public void Read_WithAValidFile_ReadsCamelCaseNamesEnumerationsAndMoments()
    {
        var folder = CreateFolder(SeedFolderTestData.ValidJson, out _);

        var file = folder.Read<SeedFolderTestFile>(SeedFolderTestData.FileName);

        file.Name.Should().Be(expected: "anna");
        file.Kind.Should().Be(ActorKind.User);
        file.At.Should().BeEquivalentTo(RelativeMoment.Parse(text: "wd-1 09:00"));
    }

    [Theory(Explicit = true)]
    [MemberData(nameof(SeedFolderTestData.InvalidFiles), MemberType = typeof(SeedFolderTestData))]
    public void Read_WithAnInvalidFile_ThrowsWithTheFilePathAndTheJsonPath(string name, string json, string jsonPath)
    {
        var folder = CreateFolder(json, out var path);

        var act = () => folder.Read<SeedFolderTestFile>(SeedFolderTestData.FileName);

        var exception = act.Should().Throw<SeedFileException>(name).Which;
        exception.Message.Should().Contain(path).And.Contain(jsonPath);
    }

    [Fact(Explicit = true)]
    public void Read_WithAMissingFile_ThrowsWithTheFilePath()
    {
        var folder = new SeedFolder(Directory.CreateTempSubdirectory().FullName);

        var act = () => folder.Read<SeedFolderTestFile>(SeedFolderTestData.FileName);

        act.Should().Throw<SeedFileException>().WithMessage($"*{SeedFolderTestData.FileName}*");
    }

    private static SeedFolder CreateFolder(string json, out string path)
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        path = Path.Combine(directory, SeedFolderTestData.FileName);
        File.WriteAllText(path, json);

        return new SeedFolder(directory);
    }
}
