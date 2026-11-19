using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Infrastructure.Persistence.RowLocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Ingest;

/// <summary>
/// The queries of the worker loop: the claim of the oldest queued attempt and the sweep of an attempt whose lease
/// ended. Both lock one row with <c>FOR UPDATE SKIP LOCKED</c>, change it, and queue the state message in one
/// transaction, so two workers never take the same row.
/// </summary>
/// <param name="dbContext">Reads and changes the attempts.</param>
/// <param name="outbox">Stores the state message with the change.</param>
/// <param name="timeProvider">Provides the times of the claim and the sweep.</param>
/// <param name="options">Holds the run limit and the lease margin.</param>
public sealed class IngestAttemptClaims(
    IInsightsDbContext dbContext,
    IOutbox outbox,
    TimeProvider timeProvider,
    IOptions<IngestProcessingOptions> options
)
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IngestProcessingOptions _options = options.Value;

    /// <summary>
    /// Claims the oldest queued attempt: <c>queued</c> to <c>inProcess</c> with a lease that outlasts the run limit.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the claim before it commits.</param>
    /// <returns>The claimed attempt, or <see langword="null"/> when no queued attempt is free.</returns>
    public async Task<ClaimedAttempt?> ClaimNextAsync(CancellationToken cancellationToken)
    {
        ClaimedAttempt? claimed = null;

        await _dbContext.RunInTransactionAsync(
            async token =>
            {
                claimed = null;
                var attempt = await _dbContext.IngestAttempts.ClaimFirstAsync(
                    candidate => candidate.State == IngestState.Queued,
                    candidate => candidate.QueuedAt,
                    token);
                if (attempt is null)
                {
                    return;
                }

                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var lease = TimeSpan.FromSeconds(_options.TimeoutSeconds + _options.LeaseMarginSeconds);
                attempt.Claim(now, now + lease);
                await AddStateMessageAsync(attempt, now, token);
                await _dbContext.SaveChangesAsync(token);
                claimed = new ClaimedAttempt
                {
                    AttemptId = attempt.Id,
                    CompanyId = attempt.CompanyId,
                    QueuedBy = attempt.CreatedBy,
                };
            },
            cancellationToken);

        return claimed;
    }

    /// <summary>
    /// Interrupts one in-process attempt whose lease ended, with the reason <c>leaseExpired</c>. The run of such an
    /// attempt is dead (the service stopped or crashed), so nobody else changes the row.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the sweep before it commits.</param>
    /// <returns>
    /// <see langword="true"/> when an attempt was interrupted; <see langword="false"/> when none was due.
    /// </returns>
    public async Task<bool> InterruptNextExpiredAsync(CancellationToken cancellationToken)
    {
        var isInterrupted = false;

        await _dbContext.RunInTransactionAsync(
            async token =>
            {
                isInterrupted = false;
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var attempt = await _dbContext.IngestAttempts.ClaimFirstAsync(
                    candidate => candidate.State == IngestState.InProcess && candidate.LeaseUntil < now,
                    candidate => candidate.LeaseUntil,
                    token);
                if (attempt is null)
                {
                    return;
                }

                attempt.Interrupt(now, IngestInterruptReason.LeaseExpired);
                await AddStateMessageAsync(attempt, now, token);
                await _dbContext.SaveChangesAsync(token);
                isInterrupted = true;
            },
            cancellationToken);

        return isInterrupted;
    }

    private async Task AddStateMessageAsync(IngestAttempt attempt, DateTime now, CancellationToken cancellationToken)
    {
        var failedCount = await _dbContext.IngestAttempts.CountAsync(
            candidate => candidate.FileId == attempt.FileId
                && candidate.CompanyId == attempt.CompanyId
                && candidate.State == IngestState.Failed,
            cancellationToken);
        _outbox.Add(IngestStateMessages.Create(attempt, failedCount, now));
    }
}
