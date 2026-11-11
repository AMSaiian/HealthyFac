using System.Net;
using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Insights.IntegrationTests.Documents.Data;
using HealthyFac.Insights.IntegrationTests.Ingest.Data;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Insights.Messaging.Ingest;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.IntegrationTests.Ingest;

[Collection(InsightsCollection.Name)]
public sealed class IngestWorkerTests(InsightsFixture fixture)
{
    private const int ShortLimitSeconds = 1;
    private const int LeaseWaitMilliseconds = 2500;

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task RunOnce_WithQueuedAttempt_CompletesItWithDocumentChunksDraftsAndMessages()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();

        var didRun = await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        var document = await _fixture.LoadDocumentAsync(IngestAttemptTestData.FileId);
        var chunkCount = await _fixture.CountChunksAsync();
        var drafts = await _fixture.ReadOutboxMessagesAsync<DraftsProposed>();
        var states = await _fixture.ReadOutboxMessagesAsync<IngestStateChanged>();

        didRun.Should().BeTrue();
        attempt.State.Should().Be(IngestState.Completed);
        attempt.StartedAt.Should().NotBeNull();
        attempt.FinishedAt.Should().NotBeNull();
        attempt.PageCount.Should().Be(expected: 2);
        attempt.ChunkCount.Should().Be(expected: 2);
        attempt.ProposedDraftCount.Should().Be(expected: 3);
        document.Should().NotBeNull();
        chunkCount.Should().Be(expected: 2);
        drafts.Should().ContainSingle().Which.Machines.Should().ContainSingle();
        states.Select(message => message.State).Should().Equal(
            IngestMessageStates.Queued, IngestMessageStates.InProcess, IngestMessageStates.Completed);
    }

    [Fact]
    public async Task RunOnce_WithNoQueuedAttempt_RunsNothing()
    {
        var didRun = await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        didRun.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(IngestWorkerTestData.FailureCases), MemberType = typeof(IngestWorkerTestData))]
    public async Task RunOnce_WhenAStageFails_FailsTheAttemptWithItsErrorCode(IngestFailureCase failure)
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        failure.Arrange(_fixture);

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        var document = await _fixture.LoadDocumentAsync(IngestAttemptTestData.FileId);
        var drafts = await _fixture.ReadOutboxMessagesAsync<DraftsProposed>();
        var states = await _fixture.ReadOutboxMessagesAsync<IngestStateChanged>();

        attempt.State.Should().Be(IngestState.Failed);
        attempt.ErrorCode.Should().Be(failure.ExpectedCode);
        attempt.FinishedAt.Should().NotBeNull();
        document.Should().BeNull();
        drafts.Should().BeEmpty();
        states.Last().State.Should().Be(IngestMessageStates.Failed);
        states.Last().FailedCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task RunOnce_WhenTheProviderStaysUnavailable_TriesEachCallFourTimes()
    {
        _fixture.ArrangeFile();
        await _fixture.StartAttemptAsync();
        _fixture.Ingest.Embeddings.Failure =
            new HttpRequestException("Embedding is down", inner: null, HttpStatusCode.ServiceUnavailable);

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        _fixture.Ingest.Embeddings.CallCount.Should()
            .Be(IngestWorkerTestData.ProviderTryCount * FakeDocumentConverter.DefaultDocument.Chunks.Count);
    }

    [Fact]
    public async Task RunOnce_WhenOneTextAnswers429Once_RepeatsOnlyThatTextAndCompletes()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        var texts = FakeDocumentConverter.DefaultDocument.Chunks;
        _fixture.Ingest.Embeddings.TransientFailureText = texts[index: 0].Text;

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        attempt.State.Should().Be(IngestState.Completed);
        _fixture.Ingest.Embeddings.CallCount.Should().Be(texts.Count + 1);
    }

    [Theory]
    [MemberData(nameof(IngestWorkerTestData.RejectionCases), MemberType = typeof(IngestWorkerTestData))]
    public async Task RunOnce_WhenAProviderRejectsTheCall_FailsAtOnceWithoutRetry(ProviderRejectionCase rejection)
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        rejection.Arrange(_fixture);

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        rejection.CallCount(_fixture).Should().Be(rejection.ExpectedCalls);
        attempt.State.Should().Be(IngestState.Failed);
        attempt.ErrorCode.Should().Be(IngestErrorCode.ProviderUnavailable);
        attempt.FinishedAt.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(IngestWorkerTestData.UnexpectedErrors), MemberType = typeof(IngestWorkerTestData))]
    public async Task RunOnce_WhenTheRunThrowsAnUnexpectedError_FailsWithProviderUnavailableAndLogsOneError(
        ProviderRejectionCase unexpected
    )
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        unexpected.Arrange(_fixture);

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        var errors = _fixture.Logs.Entries
            .Where(entry => entry.Level == LogLevel.Error
                && entry.Category == typeof(ProcessIngestAttemptRequestHandler).FullName)
            .ToList();
        attempt.State.Should().Be(IngestState.Failed);
        attempt.ErrorCode.Should().Be(IngestErrorCode.ProviderUnavailable);
        attempt.FinishedAt.Should().NotBeNull();
        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task RunOnce_WhenTheHttpClientTimesOut_TriesEachCallFourTimesThenFails()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        _fixture.Ingest.Embeddings.Failure = new TaskCanceledException("Timeout", new TimeoutException());

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);
        _fixture.Ingest.Embeddings.CallCount.Should()
            .Be(IngestWorkerTestData.ProviderTryCount * FakeDocumentConverter.DefaultDocument.Chunks.Count);
        attempt.State.Should().Be(IngestState.Failed);
        attempt.ErrorCode.Should().Be(IngestErrorCode.ProviderUnavailable);
    }

    [Fact]
    public async Task RunOnce_WhenTheRunLimitEnds_FailsWithConversionTimeout()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        _fixture.Ingest.Options.Value = new IngestProcessingOptions
        {
            RetryDelayMilliseconds = 1,
            TimeoutSeconds = ShortLimitSeconds,
        };
        _fixture.Ingest.Converter.Behavior = async cancellationToken =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);

            return FakeDocumentConverter.DefaultDocument;
        };

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);

        attempt.State.Should().Be(IngestState.Failed);
        attempt.ErrorCode.Should().Be(IngestErrorCode.ConversionTimeout);
    }

    [Fact]
    public async Task RunOnce_WhenTheUserCancelsBeforeTheFinalSave_InterruptsWithoutDocumentChunksOrDrafts()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        _fixture.Ingest.Converter.Behavior = async _ =>
        {
            await _fixture.CancelAttemptAsync(attemptId);

            return FakeDocumentConverter.DefaultDocument;
        };

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task RunOnce_WhenTheLeaseEnded_InterruptsTheAttemptWithLeaseExpired()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        _fixture.Ingest.Options.Value = new IngestProcessingOptions
        {
            RetryDelayMilliseconds = 1,
            TimeoutSeconds = ShortLimitSeconds,
            LeaseMarginSeconds = ShortLimitSeconds,
        };
        await _fixture.ClaimAttemptAsync();
        await Task.Delay(LeaseWaitMilliseconds, TestContext.Current.CancellationToken);

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = await _fixture.LoadAttemptAsync(attemptId);

        attempt.State.Should().Be(IngestState.Interrupted);
        attempt.InterruptReason.Should().Be(IngestInterruptReason.LeaseExpired);
    }

    [Fact]
    public async Task RunOnce_WhenTheServiceStopsDuringTheRun_InterruptsWithServiceStoppedAndRethrows()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        _fixture.Ingest.Converter.Behavior = async cancellationToken =>
        {
            await stop.CancelAsync();
            await Task.Delay(Timeout.Infinite, cancellationToken);

            return FakeDocumentConverter.DefaultDocument;
        };

        var act = () => _fixture.RunWorkerAsync(stop.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();

        var attempt = await _fixture.LoadAttemptAsync(attemptId);

        attempt.State.Should().Be(IngestState.Interrupted);
        attempt.InterruptReason.Should().Be(IngestInterruptReason.ServiceStopped);
    }
}
