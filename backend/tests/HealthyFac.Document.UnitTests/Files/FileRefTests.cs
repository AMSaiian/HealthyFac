using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.UnitTests.Files.Data;

namespace HealthyFac.Document.UnitTests.Files;

public sealed class FileRefTests
{
    [Theory]
    [MemberData(
        nameof(FileRefTestData.InvalidConstructorArguments),
        MemberType = typeof(FileRefTestData)
    )]
    public void Constructor_WithInvalidArgument_Throws(
        string caseName,
        FilePurpose purpose,
        Guid? machineId,
        long sizeBytes,
        byte[] sha256
    )
    {
        var act = () => FileRefTestData.Create(purpose, machineId, sizeBytes, sha256);

        act.Should().Throw<InvalidOperationException>(because: caseName);
    }

    [Fact]
    public void Constructor_WithEmbeddedPurposeAndNoMachine_Succeeds()
    {
        var act = () => FileRefTestData.Create(FilePurpose.Embedded);

        act.Should().NotThrow();
    }

    [Fact]
    public void TieTo_OnEmbeddedImage_Throws()
    {
        var file = FileRefTestData.Create(FilePurpose.Embedded);

        var act = () => file.TieTo(FileRefTestData.MachineId, FileRefTestData.QueuedAt);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Untie_OnEmbeddedImage_Throws()
    {
        var file = FileRefTestData.Create(FilePurpose.Embedded);

        var act = () => file.Untie(FileRefTestData.QueuedAt);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ApplyIngestState_WithOlderTime_ChangesNothing()
    {
        var file = FileRefTestData.Create(FilePurpose.Attachment);
        file.ApplyIngestState(FileIngestState.Completed, failedCount: 1, FileRefTestData.QueuedAt);

        file.ApplyIngestState(FileIngestState.Queued, failedCount: 0, FileRefTestData.QueuedAt.AddMinutes(value: -1));

        file.IngestState.Should().Be(FileIngestState.Completed);
        file.IngestFailedCount.Should().Be(expected: 1);
    }

    [Fact]
    public void ApplyIngestState_WithSameTime_ChangesNothing()
    {
        var file = FileRefTestData.Create(FilePurpose.Attachment);
        file.ApplyIngestState(FileIngestState.Completed, failedCount: 1, FileRefTestData.QueuedAt);

        file.ApplyIngestState(FileIngestState.Failed, failedCount: 2, FileRefTestData.QueuedAt);

        file.IngestState.Should().Be(FileIngestState.Completed);
    }

    [Fact]
    public void ApplyIngestState_WithNewerTime_StoresState()
    {
        var file = FileRefTestData.Create(FilePurpose.Attachment);
        file.ApplyIngestState(FileIngestState.Queued, failedCount: 0, FileRefTestData.QueuedAt);

        file.ApplyIngestState(FileIngestState.Failed, failedCount: 1, FileRefTestData.QueuedAt.AddMinutes(value: 1));

        file.IngestState.Should().Be(FileIngestState.Failed);
        file.IngestFailedCount.Should().Be(expected: 1);
    }

    [Theory]
    [MemberData(nameof(FileRefTestData.AutomaticTieCases), MemberType = typeof(FileRefTestData))]
    public void CanTieAutomatically_WithFileState_ReturnsExpected(FileRef file, bool expected)
    {
        var canTie = file.CanTieAutomatically(FileRefTestData.QueuedAt);

        canTie.Should().Be(expected);
    }
}
