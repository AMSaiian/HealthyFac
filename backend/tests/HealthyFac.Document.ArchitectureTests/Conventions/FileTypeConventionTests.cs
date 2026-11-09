using FileSignatures;
using HealthyFac.Document.ArchitectureTests.Conventions.Data;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.Infrastructure.Files.Inspection;

namespace HealthyFac.Document.ArchitectureTests.Conventions;

public sealed class FileTypeConventionTests
{
    [Fact]
    public void FileFormatMap_ForEachBinaryFileType_MapsAtLeastOneFormat()
    {
        var mappedExtensions = FileFormatMap.FormatTypes
            .Select(type => (FileFormat)Activator.CreateInstance(type)!)
            .Select(FileFormatMap.Find)
            .Select(type => type?.Extension)
            .ToHashSet();

        var unmapped = AllowedFileTypes.All
            .Select(type => type.Extension)
            .Where(extension => !FileTypeConventionTestData.TextExtensions.Contains(extension))
            .Where(extension => !mappedExtensions.Contains(extension));

        unmapped.Should().BeEmpty();
    }

    [Fact]
    public void FileFormatMap_Always_HoldsNoMacroFormat()
    {
        var macroFormats = FileFormatMap.FormatTypes
            .Where(type => type.Name.Contains(FileTypeConventionTestData.MacroMarker, StringComparison.Ordinal))
            .Select(type => type.FullName);

        macroFormats.Should().BeEmpty();
    }
}
