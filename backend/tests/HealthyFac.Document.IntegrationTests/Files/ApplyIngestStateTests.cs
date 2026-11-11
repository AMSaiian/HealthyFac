using System.Text.Json;
using HealthyFac.Document.Application.Files;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.Files.Data;
using HealthyFac.Insights.Messaging.Ingest;
using HealthyFac.Shared.Infrastructure.Messaging;

namespace HealthyFac.Document.IntegrationTests.Files;

[Collection(DocumentDatabaseCollection.Name)]
public sealed class ApplyIngestStateTests(DocumentDatabaseFixture fixture)
{
    private static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000b01");

    private static readonly DateTime ChangedAt =
        new(year: 2026, month: 11, day: 9, hour: 9, minute: 0, second: 0, DateTimeKind.Utc);

    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ApplyIngestState_WithNewerState_StoresIt()
    {
        var fileId = await SeedFileAsync();
        await _fixture.ApplyIngestStateAsync(
            Create(fileId, IngestMessageStates.Queued, failedCount: 0, ChangedAt));

        await _fixture.ApplyIngestStateAsync(
            Create(fileId, IngestMessageStates.Failed, failedCount: 1, ChangedAt.AddMinutes(value: 1)));

        var file = await _fixture.LoadFileRowAsync(fileId);

        file.IngestState.Should().Be(FileIngestState.Failed);
        file.IngestFailedCount.Should().Be(expected: 1);
        file.IngestStateChangedAt.Should().Be(ChangedAt.AddMinutes(value: 1));
    }

    [Fact]
    public async Task ApplyIngestState_WithOlderTime_ChangesNothing()
    {
        var fileId = await SeedFileAsync();
        await _fixture.ApplyIngestStateAsync(
            Create(fileId, IngestMessageStates.Completed, failedCount: 0, ChangedAt));

        await _fixture.ApplyIngestStateAsync(
            Create(fileId, IngestMessageStates.Queued, failedCount: 0, ChangedAt.AddMinutes(value: -1)));

        var file = await _fixture.LoadFileRowAsync(fileId);

        file.IngestState.Should().Be(FileIngestState.Completed);
    }

    [Fact]
    public async Task ApplyIngestState_WithDeletedFile_CompletesWithoutChange()
    {
        var fileId = await SeedFileAsync();
        await _fixture.DeleteCompanyFileAsync(CompanyId, new DeleteFileRequest { CompanyId = CompanyId, Id = fileId });

        await _fixture.ApplyIngestStateAsync(
            Create(fileId, IngestMessageStates.Queued, failedCount: 0, ChangedAt));

        var file = await _fixture.LoadFileRowAsync(fileId);

        file.IngestState.Should().BeNull();
    }

    [Fact]
    public async Task ApplyIngestState_WithMessageFromTheMessagingJson_StoresTheState()
    {
        var fileId = await SeedFileAsync();
        var payload = JsonSerializer.Serialize(
            Create(fileId, IngestMessageStates.InProcess, failedCount: 2, ChangedAt),
            MessagingJsonOptions.Instance);
        var received = JsonSerializer.Deserialize<IngestStateChanged>(payload, MessagingJsonOptions.Instance)!;

        await _fixture.ApplyIngestStateAsync(received);

        var file = await _fixture.LoadFileRowAsync(fileId);

        file.IngestState.Should().Be(FileIngestState.InProcess);
        file.IngestFailedCount.Should().Be(expected: 2);
    }

    private static IngestStateChanged Create(Guid fileId, string state, int failedCount, DateTime changedAt) =>
        new()
        {
            FileId = fileId,
            CompanyId = CompanyId,
            State = state,
            FailedCount = failedCount,
            ChangedAt = changedAt,
        };

    private async Task<Guid> SeedFileAsync()
    {
        var ids = await _fixture.SeedFilesAsync(CompanyId, [UpdateFileTestData.CompanyFile()]);

        return ids[index: 0];
    }
}
