using HealthyFac.Document.ArchitectureTests.Conventions.Data;
using HealthyFac.Document.Domain.Files;

namespace HealthyFac.Document.ArchitectureTests.Conventions;

public sealed class FileTypeConventionTests
{
    [Fact]
    public void AllowedFileTypes_ForEachBinaryFileType_HoldsAtLeastOneFormat()
    {
        var typesWithoutFormat = AllowedFileTypes.All
            .Where(type => !FileTypeConventionTestData.TextExtensions.Contains(type.Extension))
            .Where(type => type.Formats.Count == 0)
            .Select(type => type.Extension);

        typesWithoutFormat.Should().BeEmpty();
    }

    [Fact]
    public void AllowedFileTypes_Always_HoldsNoMacroFormat()
    {
        var macroFormats = AllowedFileTypes.All
            .SelectMany(type => type.Formats)
            .Where(type => type.Name.Contains(FileTypeConventionTestData.MacroMarker, StringComparison.Ordinal))
            .Select(type => type.FullName);

        macroFormats.Should().BeEmpty();
    }
}
