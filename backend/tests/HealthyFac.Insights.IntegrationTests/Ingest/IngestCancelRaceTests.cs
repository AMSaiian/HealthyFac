using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Insights.IntegrationTests.Documents.Data;
using HealthyFac.Insights.IntegrationTests.Ingest.Data;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Insights.IntegrationTests.Ingest;

[Collection(InsightsCollection.Name)]
public sealed class IngestCancelRaceTests(InsightsFixture fixture)
{
    private const int RaceParticipantCount = 2;

    private const int CancelDelayMilliseconds = 1000;

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task Cancel_WhenItOverlapsTheFinalSave_ConflictsAndTheAttemptCompletes()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        var claimed = await _fixture.ClaimAttemptAsync();
        using var race = _fixture.SaveGate.StartRace(RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.RunClaimedAttemptAsync(claimed!, TestContext.Current.CancellationToken)),
            race.RunAsync(() => CancelAfterTheRunHoldsItsLockAsync(attemptId)));

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        var document = await _fixture.LoadDocumentAsync(IngestAttemptTestData.FileId);
        var chunkCount = await _fixture.CountChunksAsync();

        race.SaveCounts.Should().Equal(1, 1);
        failures[0].Should().BeNull();
        failures[1].Should().BeOfType<ConflictException>().Which.Message.Should().Be(IngestErrors.IngestChanged);
        attempt.State.Should().Be(IngestState.Completed);
        document.Should().NotBeNull();
        chunkCount.Should().Be(expected: 2);
    }

    [Fact]
    public async Task Cancel_WhenItCommitsBeforeTheRunReachesTheFinalSave_LeavesNoDocumentChunkOrDraft()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        var claimed = await _fixture.ClaimAttemptAsync();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _fixture.Ingest.Embeddings.Before = async _ =>
        {
            reached.TrySetResult();
            await release.Task;
        };
        var run = _fixture.RunClaimedAttemptAsync(claimed!, TestContext.Current.CancellationToken);
        await reached.Task;

        await _fixture.CancelAttemptAsync(attemptId);
        release.SetResult();
        await run;

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        var document = await _fixture.LoadDocumentAsync(IngestAttemptTestData.FileId);
        var chunkCount = await _fixture.CountChunksAsync();
        var drafts = await _fixture.ReadOutboxMessagesAsync<DraftsProposed>();

        attempt.State.Should().Be(IngestState.Interrupted);
        attempt.InterruptReason.Should().Be(IngestInterruptReason.Cancelled);
        document.Should().BeNull();
        chunkCount.Should().Be(expected: 0);
        drafts.Should().BeEmpty();
    }

    /// <summary>
    /// Starts the cancel late, so it is the last call to reach the gate and the first to send its update. Only the row
    /// lock of the final save then decides the outcome: without it, the cancel would win and the run would conflict.
    /// </summary>
    private async Task CancelAfterTheRunHoldsItsLockAsync(Guid attemptId)
    {
        await Task.Delay(CancelDelayMilliseconds, TestContext.Current.CancellationToken);
        await _fixture.CancelAttemptAsync(attemptId);
    }
}
