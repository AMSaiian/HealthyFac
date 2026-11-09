using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.UnitTests.Files.Data;

namespace HealthyFac.Document.UnitTests.Files;

public sealed class FileRefTests
{
    [Fact]
    public void Constructor_WithEmbeddedPurposeAndMachine_Throws()
    {
        var act = () => FileRefTestData.Create(FilePurpose.Embedded, FileRefTestData.MachineId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WithEmbeddedPurposeAndNoMachine_Succeeds()
    {
        var act = () => FileRefTestData.Create(FilePurpose.Embedded);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithZeroSize_Throws()
    {
        var act = () => FileRefTestData.Create(FilePurpose.Attachment, sizeBytes: 0);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WithShortHash_Throws()
    {
        var act = () => FileRefTestData.Create(FilePurpose.Attachment, sha256: new byte[FileRef.Sha256Length - 1]);

        act.Should().Throw<InvalidOperationException>();
    }
}
