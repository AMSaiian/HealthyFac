using HealthyFac.Document.Messaging.Files;
using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Documents;

/// <summary>
/// The message carries the company, and the handler filters every query by it, so no scope check is needed.
/// </summary>
public sealed class RemoveIngestedFilePreHandlerCheck : PassThroughPreHandlerCheck<FileDeleted>;

/// <summary>
/// Removes what Insights holds of a deleted file: it interrupts the queued, running, or completed attempt with the
/// reason <c>fileDeleted</c> and deletes the ingested document with its chunks (the cascade). Both steps run under the
/// lock of that attempt, so the worker cannot store a document for the file between them. A repeat delivery changes
/// nothing, because no such attempt and no document are left.
/// </summary>
/// <remarks>
/// The lock also matches a completed attempt. When the worker holds the lock and commits <c>completed</c> while this
/// handler waits, PostgreSQL checks the <c>WHERE</c> condition again after the wait. A lock on active rows only would
/// then return nothing, and the delete would leave the stored document in place.
/// </remarks>
/// <param name="dbContext">Loads and changes the attempt, deletes the document.</param>
/// <param name="attemptLock">Locks the attempt that the delete stops.</param>
/// <param name="outbox">Stores the state message with the change.</param>
/// <param name="timeProvider">Provides the time of the change.</param>
public sealed class RemoveIngestedFileRequestHandler(
    IInsightsDbContext dbContext,
    IIngestAttemptLock attemptLock,
    IOutbox outbox,
    TimeProvider timeProvider
) : RequestHandler<FileDeleted>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly IIngestAttemptLock _attemptLock = attemptLock;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override Task HandleInternalAsync(FileDeleted request, CancellationToken cancellationToken) =>
        _dbContext.RunInTransactionAsync(
            token => RemoveAsync(request, token),
            cancellationToken);

    private async Task RemoveAsync(FileDeleted request, CancellationToken cancellationToken)
    {
        var attempt = await _attemptLock.LockAttemptForFileDeleteAsync(
            request.FileId,
            request.CompanyId,
            cancellationToken);
        if (attempt is not null)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            attempt.InterruptForFileDelete(now);

            var failedCount = await _dbContext.IngestAttempts.CountAsync(
                candidate => candidate.FileId == request.FileId
                    && candidate.CompanyId == request.CompanyId
                    && candidate.State == IngestState.Failed,
                cancellationToken);
            _outbox.Add(IngestStateMessages.Create(attempt, failedCount, now));
        }

        await _dbContext.IngestedDocuments
            .Where(document => document.Id == request.FileId && document.CompanyId == request.CompanyId)
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
