using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Insights.IntegrationTests.Documents.Data;
using HealthyFac.Insights.Messaging.Ingest;

namespace HealthyFac.Insights.IntegrationTests.Documents;

[Collection(InsightsCollection.Name)]
public sealed class FileMessageTests(InsightsFixture fixture)
{
    private static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task FileDeleted_WithQueuedAttempt_InterruptsIt()
    {
        var queued = IngestAttemptTestData.CreateQueued();
        await _fixture.SaveAsync(queued);

        await _fixture.HandleFileDeletedAsync(IngestAttemptTestData.CompanyId, IngestAttemptTestData.FileId);

        var attempts = await _fixture.LoadAttemptsAsync();
        var messages = await _fixture.ReadOutboxMessagesAsync<IngestStateChanged>();

        var interrupted = attempts.Should().ContainSingle().Which;
        interrupted.State.Should().Be(IngestState.Interrupted);
        interrupted.InterruptReason.Should().Be(IngestInterruptReason.FileDeleted);
        messages.Should().ContainSingle().Which.State.Should().Be(IngestMessageStates.Interrupted);
    }

    [Fact]
    public async Task FileDeleted_DeliveredTwice_ChangesNothingTheSecondTime()
    {
        await _fixture.SaveAsync(IngestAttemptTestData.CreateQueued());
        await _fixture.HandleFileDeletedAsync(IngestAttemptTestData.CompanyId, IngestAttemptTestData.FileId);

        await _fixture.HandleFileDeletedAsync(IngestAttemptTestData.CompanyId, IngestAttemptTestData.FileId);

        var messages = await _fixture.ReadOutboxMessagesAsync<IngestStateChanged>();

        messages.Should().ContainSingle();
    }

    [Fact]
    public async Task FileDeleted_WithCompletedAttempt_InterruptsItAndDeletesTheDocument()
    {
        var completed = IngestAttemptTestData.CreateCompleted();
        await _fixture.SaveAsync(completed, IngestAttemptTestData.CreateDocument(completed.Id));

        await _fixture.HandleFileDeletedAsync(IngestAttemptTestData.CompanyId, IngestAttemptTestData.FileId);

        var attempts = await _fixture.LoadAttemptsAsync();
        var document = await _fixture.LoadDocumentAsync(IngestAttemptTestData.FileId);
        var chunkCount = await _fixture.CountChunksAsync();
        var messages = await _fixture.ReadOutboxMessagesAsync<IngestStateChanged>();

        var attempt = attempts.Should().ContainSingle().Which;
        attempt.State.Should().Be(IngestState.Interrupted);
        attempt.InterruptReason.Should().Be(IngestInterruptReason.FileDeleted);
        attempt.ChunkCount.Should().Be(expected: 1);
        document.Should().BeNull();
        chunkCount.Should().Be(expected: 0);
        messages.Should().ContainSingle().Which.State.Should().Be(IngestMessageStates.Interrupted);
    }

    [Fact]
    public async Task FileDeleted_WithFailedAttempt_LeavesItFailed()
    {
        await _fixture.SaveAsync(IngestAttemptTestData.CreateFailed());

        await _fixture.HandleFileDeletedAsync(IngestAttemptTestData.CompanyId, IngestAttemptTestData.FileId);

        var attempts = await _fixture.LoadAttemptsAsync();
        var messages = await _fixture.ReadOutboxMessagesAsync<IngestStateChanged>();

        attempts.Should().ContainSingle().Which.State.Should().Be(IngestState.Failed);
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task FileDeleted_ForAnotherCompany_ChangesNothing()
    {
        var completed = IngestAttemptTestData.CreateCompleted();
        await _fixture.SaveAsync(completed, IngestAttemptTestData.CreateDocument(completed.Id));

        await _fixture.HandleFileDeletedAsync(Guid.CreateVersion7(), IngestAttemptTestData.FileId);

        var attempts = await _fixture.LoadAttemptsAsync();
        var document = await _fixture.LoadDocumentAsync(IngestAttemptTestData.FileId);

        attempts.Should().ContainSingle().Which.State.Should().Be(IngestState.Completed);
        document.Should().NotBeNull();
    }

    [Fact]
    public async Task FileMachineChanged_WithDocument_UpdatesTheDocument()
    {
        var completed = IngestAttemptTestData.CreateCompleted();
        await _fixture.SaveAsync(completed, IngestAttemptTestData.CreateDocument(completed.Id));
        var changedAt = IngestAttemptTestData.Now.AddMinutes(value: 5);

        await _fixture.HandleMachineChangedAsync(
            IngestAttemptTestData.CompanyId, IngestAttemptTestData.FileId, MachineId, changedAt);

        var document = await _fixture.LoadDocumentAsync(IngestAttemptTestData.FileId);

        document!.MachineId.Should().Be(MachineId);
        document.MachineChangedAt.Should().Be(changedAt);
    }

    [Fact]
    public async Task FileMachineChanged_OlderThanTheStoredChange_IsIgnored()
    {
        var completed = IngestAttemptTestData.CreateCompleted();
        await _fixture.SaveAsync(completed, IngestAttemptTestData.CreateDocument(completed.Id));
        var newerAt = IngestAttemptTestData.Now.AddMinutes(value: 10);
        await _fixture.HandleMachineChangedAsync(
            IngestAttemptTestData.CompanyId, IngestAttemptTestData.FileId, MachineId, newerAt);

        await _fixture.HandleMachineChangedAsync(
            IngestAttemptTestData.CompanyId,
            IngestAttemptTestData.FileId,
            machineId: null,
            IngestAttemptTestData.Now.AddMinutes(value: 5));

        var document = await _fixture.LoadDocumentAsync(IngestAttemptTestData.FileId);

        document!.MachineId.Should().Be(MachineId);
        document.MachineChangedAt.Should().Be(newerAt);
    }

    [Fact]
    public async Task FileMachineChanged_WithOnlyAnActiveAttempt_UpdatesTheAttempt()
    {
        await _fixture.SaveAsync(IngestAttemptTestData.CreateQueued());
        var changedAt = IngestAttemptTestData.Now.AddMinutes(value: 5);

        await _fixture.HandleMachineChangedAsync(
            IngestAttemptTestData.CompanyId, IngestAttemptTestData.FileId, MachineId, changedAt);

        var attempts = await _fixture.LoadAttemptsAsync();

        var attempt = attempts.Should().ContainSingle().Which;
        attempt.MachineId.Should().Be(MachineId);
        attempt.MachineChangedAt.Should().Be(changedAt);
    }

    [Fact]
    public async Task FileMachineChanged_WithNeitherDocumentNorActiveAttempt_ChangesNothing()
    {
        await _fixture.SaveAsync(IngestAttemptTestData.CreateCompleted());

        await _fixture.HandleMachineChangedAsync(
            IngestAttemptTestData.CompanyId,
            IngestAttemptTestData.FileId,
            MachineId,
            IngestAttemptTestData.Now.AddMinutes(value: 5));

        var attempts = await _fixture.LoadAttemptsAsync();

        attempts.Should().ContainSingle().Which.MachineId.Should().BeNull();
    }
}
