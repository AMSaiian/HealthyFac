using System.Diagnostics;
using FluentValidation;
using HealthyFac.Insights.Domain.Documents;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Application.Ingest.Processing;

public record ProcessIngestAttemptRequest : IRequest
{
    public required Guid AttemptId { get; init; }

    public required Guid CompanyId { get; init; }
}

public class ProcessIngestAttemptRequestValidator : AbstractValidator<ProcessIngestAttemptRequest>
{
    public ProcessIngestAttemptRequestValidator()
    {
        RuleFor(x => x.AttemptId).NotEmpty().WithMessage(IngestErrors.AttemptIdRequired);
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(IngestErrors.CompanyIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor. The worker restores the actor of the attempt, so the check holds.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class ProcessIngestAttemptPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<ProcessIngestAttemptRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(ProcessIngestAttemptRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Runs one claimed attempt: the pipeline, then one final save that creates the document with its chunks, completes
/// the attempt, and queues the draft and state messages. An attempt ends <c>completed</c>, <c>failed</c> (with an
/// error code), or <c>interrupted</c>.
/// </summary>
/// <remarks>
/// The run limit is a source on the clock of <see cref="TimeProvider"/>, linked to the stop token of the service. The
/// final save locks the attempt row (<c>FOR UPDATE</c>) and reads it again: an attempt that a cancel, a file delete,
/// or a sweep already interrupted is discarded without a document, and a cancel that comes after the lock conflicts on
/// the row version.
/// <para>
/// <see cref="CancellationToken.None"/> is passed to the save of a failure or an interruption (<b>Finish</b>): the
/// attempt must reach a final state, the save is short, and a cancelled token would leave the attempt in process until
/// a sweep. The stop of the service is not a failure: the attempt is interrupted with <c>serviceStopped</c>, and the
/// <see cref="OperationCanceledException"/> goes on.
/// </para>
/// <para>
/// The last <c>catch</c> ends any other error of the run as <c>providerUnavailable</c> and logs it once at the
/// <c>Error</c> level, so no unexpected error leaves the attempt in process until the lease sweep. The catches for the
/// cancel, the run limit, and the stop of the service come first and stay unchanged.
/// </para>
/// </remarks>
/// <param name="dbContext">Reads the attempt and saves the result.</param>
/// <param name="attemptLock">Locks the active attempt for the final save.</param>
/// <param name="pipeline">Runs the stages.</param>
/// <param name="outbox">Stores the messages with the change.</param>
/// <param name="timeProvider">Provides the clock of the run limit and the times of the change.</param>
/// <param name="options">Holds the run limit.</param>
/// <param name="logger">Logs the final state of each attempt.</param>
/// <param name="metrics">Records the result, the stage durations, and the counts of each run.</param>
public sealed class ProcessIngestAttemptRequestHandler(
    IInsightsDbContext dbContext,
    IIngestAttemptLock attemptLock,
    IngestPipeline pipeline,
    IOutbox outbox,
    TimeProvider timeProvider,
    IOptions<IngestProcessingOptions> options,
    ILogger<ProcessIngestAttemptRequestHandler> logger,
    IngestMetrics metrics
) : RequestHandler<ProcessIngestAttemptRequest>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly IIngestAttemptLock _attemptLock = attemptLock;
    private readonly IngestPipeline _pipeline = pipeline;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IngestProcessingOptions _options = options.Value;
    private readonly ILogger<ProcessIngestAttemptRequestHandler> _logger = logger;
    private readonly IngestMetrics _metrics = metrics;

    private Activity? _processSpan;

    protected override async Task HandleInternalAsync(
        ProcessIngestAttemptRequest request,
        CancellationToken cancellationToken
    )
    {
        var snapshot = await LoadSnapshotAsync(request, cancellationToken);
        if (snapshot is null)
        {
            return;
        }

        var startedAt = _timeProvider.GetTimestamp();
        using var processSpan = StartProcessSpan(snapshot);
        _processSpan = processSpan;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds), _timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            var result = await _pipeline.RunAsync(
                snapshot,
                token => IsNoLongerActiveAsync(snapshot.AttemptId, token),
                linked.Token);
            var state = await CompleteAsync(snapshot, result, cancellationToken);
            if (state == IngestState.Completed)
            {
                await FinishAsync(snapshot, new IngestOutcome { State = state }, startedAt, result);
            }
            else
            {
                await FinishInterruptedAsync(snapshot, startedAt);
            }
        }
        catch (AttemptNoLongerActiveException)
        {
            await FinishInterruptedAsync(snapshot, startedAt);
        }
        catch (IngestFailureException exception)
        {
            await FailAsync(snapshot, exception.Code, startedAt);
        }
        catch (ProviderUnavailableException)
        {
            await FailAsync(snapshot, IngestErrorCode.ProviderUnavailable, startedAt);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await InterruptAsync(snapshot, IngestInterruptReason.ServiceStopped);
            await FinishAsync(
                snapshot,
                new IngestOutcome
                {
                    State = IngestState.Interrupted,
                    InterruptReason = IngestInterruptReason.ServiceStopped,
                },
                startedAt);

            throw;
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            await FailAsync(snapshot, IngestErrorCode.ConversionTimeout, startedAt);
        }
        catch (OperationCanceledException)
        {
            var isInactive = await IsNoLongerActiveAsync(snapshot.AttemptId, CancellationToken.None);
            if (!isInactive)
            {
                throw;
            }

            await FinishInterruptedAsync(snapshot, startedAt);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                message: "Ingest attempt {AttemptId} failed with an unexpected error.",
                snapshot.AttemptId);
            await FailAsync(snapshot, IngestErrorCode.ProviderUnavailable, startedAt);
        }
    }

    private async Task<IngestAttemptSnapshot?> LoadSnapshotAsync(
        ProcessIngestAttemptRequest request,
        CancellationToken cancellationToken
    )
    {
        var attempt = await _dbContext.IngestAttempts
            .AsNoTracking()
            .Where(candidate => candidate.Id == request.AttemptId && candidate.CompanyId == request.CompanyId)
            .Where(candidate => candidate.State == IngestState.InProcess)
            .SingleOrDefaultAsync(cancellationToken);

        return attempt is null
            ? null
            : new IngestAttemptSnapshot
            {
                AttemptId = attempt.Id,
                FileId = attempt.FileId,
                CompanyId = attempt.CompanyId,
                FileName = attempt.FileName,
                Extension = attempt.Extension,
                QueuedAt = attempt.QueuedAt,
                TraceParent = attempt.TraceParent,
            };
    }

    private async Task<bool> IsNoLongerActiveAsync(Guid attemptId, CancellationToken cancellationToken)
    {
        var isActive = await _dbContext.IngestAttempts
            .AsNoTracking()
            .AnyAsync(
                attempt => attempt.Id == attemptId && attempt.State == IngestState.InProcess,
                cancellationToken);

        return !isActive;
    }

    private async Task<IngestState> CompleteAsync(
        IngestAttemptSnapshot snapshot,
        IngestPipelineResult result,
        CancellationToken cancellationToken
    )
    {
        var state = IngestState.Interrupted;
        using var storeSpan = IngestActivitySource.Instance.StartActivity(ObservabilityConstants.Ingest.Spans.Store);
        using var storeTimer = _metrics.MeasureStage(ObservabilityConstants.Ingest.Stages.Store);

        await _dbContext.RunInTransactionAsync(
            async token =>
            {
                var locked = await LockOwnAttemptAsync(snapshot, token);
                if (locked is null)
                {
                    return;
                }

                var now = _timeProvider.GetUtcNow().UtcDateTime;
                locked.ChangeMachine(result.MachineId, result.MachineChangedAt);
                var document = new IngestedDocument(
                    snapshot.FileId,
                    snapshot.CompanyId,
                    locked.MachineId,
                    locked.MachineChangedAt,
                    locked.Id,
                    locked.Extension,
                    result.PageCount);
                for (var index = 0; index < result.Chunks.Count; index++)
                {
                    var chunk = result.Chunks[index];
                    document.AddChunk(chunk.Location, chunk.Text, result.Embeddings[index]);
                }

                _dbContext.IngestedDocuments.Add(document);
                locked.Complete(
                    now,
                    result.PageCount,
                    result.Chunks.Count,
                    result.Mapped.DraftCount,
                    result.Mapped.Warning);
                if (result.Mapped.Message is not null)
                {
                    _outbox.Add(result.Mapped.Message);
                }

                var failedCount = await CountFailedAsync(snapshot, token);
                _outbox.Add(IngestStateMessages.Create(locked, failedCount, now));
                await _dbContext.SaveChangesAsync(token);
                state = IngestState.Completed;
            },
            cancellationToken);

        return state;
    }

    private async Task FailAsync(IngestAttemptSnapshot snapshot, IngestErrorCode code, long startedAt)
    {
        var isFailed = false;

        await _dbContext.RunInTransactionAsync(
            async token =>
            {
                var locked = await LockOwnAttemptAsync(snapshot, token);
                if (locked is null)
                {
                    return;
                }

                var now = _timeProvider.GetUtcNow().UtcDateTime;
                locked.Fail(now, code);
                var failedCount = await CountFailedAsync(snapshot, token) + 1;
                _outbox.Add(IngestStateMessages.Create(locked, failedCount, now));
                await _dbContext.SaveChangesAsync(token);
                isFailed = true;
            },
            CancellationToken.None);

        if (!isFailed)
        {
            await FinishInterruptedAsync(snapshot, startedAt);

            return;
        }

        _logger.LogWarning(
            message: "Ingest attempt {AttemptId} failed with the error code {ErrorCode}.",
            snapshot.AttemptId,
            code.Value);
        await FinishAsync(
            snapshot,
            new IngestOutcome { State = IngestState.Failed, ErrorCode = code },
            startedAt);
    }

    private async Task InterruptAsync(IngestAttemptSnapshot snapshot, IngestInterruptReason reason)
    {
        await _dbContext.RunInTransactionAsync(
            async token =>
            {
                var locked = await LockOwnAttemptAsync(snapshot, token);
                if (locked is null)
                {
                    return;
                }

                var now = _timeProvider.GetUtcNow().UtcDateTime;
                locked.Interrupt(now, reason);
                var failedCount = await CountFailedAsync(snapshot, token);
                _outbox.Add(IngestStateMessages.Create(locked, failedCount, now));
                await _dbContext.SaveChangesAsync(token);
            },
            CancellationToken.None);
    }

    private async Task<IngestAttempt?> LockOwnAttemptAsync(
        IngestAttemptSnapshot snapshot,
        CancellationToken cancellationToken
    )
    {
        var locked = await _attemptLock.LockActiveAttemptAsync(snapshot.FileId, snapshot.CompanyId, cancellationToken);

        return locked is { State: var state } && locked.Id == snapshot.AttemptId && state == IngestState.InProcess
            ? locked
            : null;
    }

    private Task<int> CountFailedAsync(IngestAttemptSnapshot snapshot, CancellationToken cancellationToken) =>
        _dbContext.IngestAttempts.CountAsync(
            candidate => candidate.FileId == snapshot.FileId
                && candidate.CompanyId == snapshot.CompanyId
                && candidate.State == IngestState.Failed,
            cancellationToken);

    private Activity? StartProcessSpan(IngestAttemptSnapshot snapshot)
    {
        var links = ActivityContext.TryParse(snapshot.TraceParent, traceState: null, out var parent)
            ? new[] { new ActivityLink(parent) }
            : null;

        return IngestActivitySource.Instance.StartActivity(
            ObservabilityConstants.Ingest.Spans.Process,
            ActivityKind.Internal,
            parentContext: default,
            tags: [new KeyValuePair<string, object?>(ObservabilityConstants.Ingest.Tags.FileType, snapshot.Extension)],
            links: links);
    }

    private async Task FinishInterruptedAsync(IngestAttemptSnapshot snapshot, long startedAt)
    {
        var reason = await _dbContext.IngestAttempts
            .AsNoTracking()
            .Where(attempt => attempt.Id == snapshot.AttemptId)
            .Select(attempt => attempt.InterruptReason)
            .SingleOrDefaultAsync(CancellationToken.None);

        await FinishAsync(
            snapshot,
            new IngestOutcome { State = IngestState.Interrupted, InterruptReason = reason },
            startedAt);
    }

    private Task FinishAsync(
        IngestAttemptSnapshot snapshot,
        IngestOutcome outcome,
        long startedAt,
        IngestPipelineResult? result = null
    )
    {
        var duration = _timeProvider.GetElapsedTime(startedAt);
        var resultName = outcome.State == IngestState.Completed
            ? ObservabilityConstants.Ingest.Results.Completed
            : outcome.State == IngestState.Failed
                ? ObservabilityConstants.Ingest.Results.Failed
                : ObservabilityConstants.Ingest.Results.Interrupted;

        _metrics.RecordAttempt(resultName, outcome.ErrorCode?.Value, outcome.InterruptReason?.Value);
        if (result is not null)
        {
            var message = result.Mapped.Message;
            _metrics.RecordCompleted(
                result.PageCount,
                result.Chunks.Count,
                message?.Machines.Count ?? 0,
                message?.Machines.Sum(machine => machine.Metrics.Count) ?? 0,
                message?.Machines.Sum(machine => machine.Statuses.Count) ?? 0);
        }

        _processSpan?.SetTag(ObservabilityConstants.Ingest.Tags.Result, resultName);
        _processSpan?.SetTag(ObservabilityConstants.Ingest.Tags.ErrorCode, outcome.ErrorCode?.Value);
        _processSpan?.SetTag(ObservabilityConstants.Ingest.Tags.InterruptReason, outcome.InterruptReason?.Value);
        if (outcome.State == IngestState.Failed)
        {
            _processSpan?.SetStatus(ActivityStatusCode.Error);
        }

        _logger.LogInformation(
            message: "Ingest attempt {AttemptId} ended as {State} after {DurationSeconds} seconds.",
            snapshot.AttemptId,
            outcome.State.Value,
            duration.TotalSeconds);

        return Task.CompletedTask;
    }
}
