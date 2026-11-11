using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Insights.IntegrationTests.Documents.Data;
using HealthyFac.Insights.IntegrationTests.Ingest.Data;
using HealthyFac.Insights.Messaging.Ingest;

namespace HealthyFac.Insights.IntegrationTests.Ingest;

[Collection(InsightsCollection.Name)]
public sealed class IngestFileDeletedRaceTests(InsightsFixture fixture)
{
    private static readonly TimeSpan LockWaitLimit = TimeSpan.FromSeconds(seconds: 30);

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task FileDeleted_WhileFinalSaveHoldsTheLock_EndsInterruptedWithNoDocument()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        var claimed = await _fixture.ClaimAttemptAsync();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _fixture.SaveHook.RunBeforeNextSave(async () =>
        {
            reached.TrySetResult();
            await release.Task;
        });
        var run = _fixture.RunClaimedAttemptAsync(claimed!, TestContext.Current.CancellationToken);
        await reached.Task;
        var delete = _fixture.HandleFileDeletedAsync(IngestAttemptTestData.CompanyId, IngestAttemptTestData.FileId);
        await _fixture.WaitForRowLockWaitAsync(LockWaitLimit, TestContext.Current.CancellationToken);

        release.SetResult();
        await Task.WhenAll(run, delete);

        await AssertDeletedAsync(attemptId, completedSaveCount: 1);
    }

    [Fact]
    public async Task FileDeleted_BeforeFinalSaveTakesTheLock_EndsInterruptedWithNoDocument()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        var claimed = await _fixture.ClaimAttemptAsync();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _fixture.SaveHook.RunBeforeNextSave(async () =>
        {
            reached.TrySetResult();
            await release.Task;
        });
        var delete = _fixture.HandleFileDeletedAsync(IngestAttemptTestData.CompanyId, IngestAttemptTestData.FileId);
        await reached.Task;
        var run = _fixture.RunClaimedAttemptAsync(claimed!, TestContext.Current.CancellationToken);
        await _fixture.WaitForRowLockWaitAsync(LockWaitLimit, TestContext.Current.CancellationToken);

        release.SetResult();
        await Task.WhenAll(run, delete);

        await AssertDeletedAsync(attemptId, completedSaveCount: 0);
    }

    /// <summary>
    /// Checks the one outcome. Each side saves its state in one save with its outbox row, so the rows count the saves:
    /// the delete saves <c>interrupted</c> once in both orders, and the worker saves <c>completed</c> only in order A.
    /// </summary>
    private async Task AssertDeletedAsync(Guid attemptId, int completedSaveCount)
    {
        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        var document = await _fixture.LoadDocumentAsync(IngestAttemptTestData.FileId);
        var chunkCount = await _fixture.CountChunksAsync();
        var states = await _fixture.ReadOutboxMessagesAsync<IngestStateChanged>();

        attempt.State.Should().Be(IngestState.Interrupted);
        attempt.InterruptReason.Should().Be(IngestInterruptReason.FileDeleted);
        document.Should().BeNull();
        chunkCount.Should().Be(expected: 0);
        states.OrderBy(message => message.ChangedAt).Last().State.Should().Be(IngestMessageStates.Interrupted);
        states.Count(message => message.State == IngestMessageStates.Completed).Should().Be(completedSaveCount);
        states.Count(message => message.State == IngestMessageStates.Interrupted).Should().Be(expected: 1);
    }
}
