using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.UnitTests.Files.Data;

namespace HealthyFac.Document.UnitTests.Files;

public sealed class FileNamesTests
{
    [Theory]
    [MemberData(nameof(FileNamesTestData.BaseNames), MemberType = typeof(FileNamesTestData))]
    public void ToBaseName_WithClientFileName_RemovesPathAndExtension(string clientFileName, string? expected)
    {
        var baseName = FileNames.ToBaseName(clientFileName);

        baseName.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(FileNamesTestData.Extensions), MemberType = typeof(FileNamesTestData))]
    public void ToExtension_WithClientFileName_ReturnsTheLowerCaseExtension(string clientFileName, string? expected)
    {
        var extension = FileNames.ToExtension(clientFileName);

        extension.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(FileNamesTestData.StrippedNames), MemberType = typeof(FileNamesTestData))]
    public void StripStoredExtension_WithName_RemovesOnlyTheStoredExtension(
        string name,
        string extension,
        string expected
    )
    {
        var stripped = FileNames.StripStoredExtension(name, extension);

        stripped.Should().Be(expected);
    }
}
