using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Insights.IntegrationTests.Documents.Data;
using HealthyFac.Insights.IntegrationTests.Ingest.Data;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Insights.Messaging.Ingest;

namespace HealthyFac.Insights.IntegrationTests.Ingest;

[Collection(InsightsCollection.Name)]
public sealed class ScreeningTests(InsightsFixture fixture)
{
    private const int ChunkCount = 5;
    private const int UnsafeIndex = 3;

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task RunOnce_WithSafeChunks_ScreensThemThenEmbedsAndExtracts()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        var runs = await _fixture.LoadScreenerRunsAsync();
        attempt.State.Should().Be(IngestState.Completed);
        _fixture.Ingest.ScreenerChat.Batches.Should().ContainSingle().Which.Should().Equal(0, 1);
        _fixture.Ingest.Embeddings.CallCount.Should().BeGreaterThan(expected: 0);
        _fixture.Ingest.Chat.CallCount.Should().BeGreaterThan(expected: 0);
        var run = runs.Should().ContainSingle().Subject;
        run.State.Should().Be(AgentRunState.Completed);
        run.Verdict.Should().Be(AgentRunVerdict.Safe);
        run.IngestAttemptId.Should().Be(attemptId);
        run.CaseId.Should().BeNull();
        run.Model.Should().Be(expected: "test-triage-model");
        run.PromptVersion.Should().Be(expected: "screener-v1");
        run.InputTokens.Should().Be(ScreeningChatClient.InputTokensPerCall);
        run.OutputTokens.Should().Be(ScreeningChatClient.OutputTokensPerCall);
    }

    [Fact]
    public async Task RunOnce_WithAnUnsafeChunkInTheSecondBatch_FailsTheAttemptAndSendsNoLaterBatch()
    {
        _fixture.ArrangeFile();
        _fixture.ArrangeChunks(ChunkCount);
        _fixture.Ingest.ScreenerChat.UnsafeIndexes.Add(UnsafeIndex);
        var attemptId = await _fixture.StartAttemptAsync();

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        var runs = await _fixture.LoadScreenerRunsAsync();
        var drafts = await _fixture.ReadOutboxMessagesAsync<DraftsProposed>();
        var states = await _fixture.ReadOutboxMessagesAsync<IngestStateChanged>();
        attempt.State.Should().Be(IngestState.Failed);
        attempt.ErrorCode.Should().Be(IngestErrorCode.UnsafeContent);
        attempt.ErrorDetail.Should().NotBeNull();
        attempt.ErrorDetail.Value.Location.Should().Be(expected: "page 4");
        attempt.ErrorDetail.Value.Reason.Should().Be(ScreeningChatClient.UnsafeReason);
        _fixture.Ingest.ScreenerChat.Batches.Should().HaveCount(expected: 2);
        _fixture.Ingest.Embeddings.CallCount.Should().Be(expected: 0);
        _fixture.Ingest.Chat.CallCount.Should().Be(expected: 0);
        drafts.Should().BeEmpty();
        states[^1].State.Should().Be(IngestMessageStates.Failed);
        var run = runs.Should().ContainSingle().Subject;
        run.Verdict.Should().Be(AgentRunVerdict.Unsafe);
        run.InputTokens.Should().Be(2 * ScreeningChatClient.InputTokensPerCall);
    }

    [Fact]
    public async Task RunOnce_WhenTheScreenerStaysUnavailable_FailsTheAttemptAndStoresAFailedScreenerRun()
    {
        _fixture.ArrangeFile();

        _fixture.Ingest.ScreenerChat.Failure = new HttpRequestException(
            message: "Screener is down", inner: null, System.Net.HttpStatusCode.ServiceUnavailable);

        var attemptId = await _fixture.StartAttemptAsync();

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        var runs = await _fixture.LoadScreenerRunsAsync();
        attempt.ErrorCode.Should().Be(IngestErrorCode.ProviderUnavailable);
        attempt.ErrorDetail.Should().BeNull();
        var run = runs.Should().ContainSingle().Subject;
        run.State.Should().Be(AgentRunState.Failed);
        run.ErrorCode.Should().Be(AgentRunErrorCode.ProviderUnavailable);
        _fixture.Ingest.Embeddings.CallCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task RunOnce_WhenTheEmbeddingStaysUnavailableAfterSafeChunks_KeepsTheCompletedScreenerRun()
    {
        _fixture.ArrangeFile();

        _fixture.Ingest.Embeddings.Failure = new HttpRequestException(
            message: "Embedding is down", inner: null, System.Net.HttpStatusCode.ServiceUnavailable);

        var attemptId = await _fixture.StartAttemptAsync();

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        var runs = await _fixture.LoadScreenerRunsAsync();
        attempt.ErrorCode.Should().Be(IngestErrorCode.ProviderUnavailable);
        var run = runs.Should().ContainSingle().Subject;
        run.State.Should().Be(AgentRunState.Completed);
        run.Verdict.Should().Be(AgentRunVerdict.Safe);
        run.InputTokens.Should().Be(ScreeningChatClient.InputTokensPerCall);
        run.OutputTokens.Should().Be(ScreeningChatClient.OutputTokensPerCall);
    }

    [Fact]
    public async Task RunOnce_WhenTheScreenerFailsAtTheSecondBatch_StoresTheTokensOfTheFirstBatch()
    {
        _fixture.ArrangeFile();
        _fixture.ArrangeChunks(ChunkCount);
        _fixture.Ingest.ScreenerChat.FailFromCall = 2;

        _fixture.Ingest.ScreenerChat.Failure = new HttpRequestException(
            message: "Screener is down", inner: null, System.Net.HttpStatusCode.ServiceUnavailable);

        await _fixture.StartAttemptAsync();

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var runs = await _fixture.LoadScreenerRunsAsync();
        var run = runs.Should().ContainSingle().Subject;
        run.State.Should().Be(AgentRunState.Failed);
        run.ErrorCode.Should().Be(AgentRunErrorCode.ProviderUnavailable);
        run.InputTokens.Should().Be(ScreeningChatClient.InputTokensPerCall);
        run.OutputTokens.Should().Be(ScreeningChatClient.OutputTokensPerCall);
    }

    [Fact]
    public async Task RunOnce_WithAnUnsafeChunk_RemovesTheEarlierDocumentAndItsChunksOfTheFile()
    {
        var earlier = IngestAttemptTestData.CreateFailed();
        await _fixture.SaveAsync(earlier, IngestAttemptTestData.CreateDocument(earlier.Id));
        _fixture.ArrangeFile();
        _fixture.Ingest.ScreenerChat.UnsafeIndexes.Add(item: 0);
        await _fixture.StartAttemptAsync();

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var document = await _fixture.LoadDocumentAsync(IngestAttemptTestData.FileId);
        var chunkCount = await _fixture.CountChunksAsync();
        document.Should().BeNull();
        chunkCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task RunOnce_WhenACancelCommitsDuringTheScreening_TheCancelWinsAndTheEarlierDocumentStays()
    {
        var earlier = IngestAttemptTestData.CreateFailed();
        await _fixture.SaveAsync(earlier, IngestAttemptTestData.CreateDocument(earlier.Id));
        _fixture.ArrangeFile();
        _fixture.Ingest.ScreenerChat.UnsafeIndexes.Add(item: 0);
        var attemptId = await _fixture.StartAttemptAsync();
        _fixture.Ingest.ScreenerChat.Before = () => _fixture.CancelAttemptAsync(attemptId);

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        var document = await _fixture.LoadDocumentAsync(IngestAttemptTestData.FileId);
        attempt.State.Should().Be(IngestState.Interrupted);
        attempt.InterruptReason.Should().Be(IngestInterruptReason.Cancelled);
        attempt.ErrorCode.Should().BeNull();
        document.Should().NotBeNull();
    }
}
