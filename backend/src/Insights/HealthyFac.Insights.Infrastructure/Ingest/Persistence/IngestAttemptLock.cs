using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Shared.Infrastructure.Persistence.RowLocks;

namespace HealthyFac.Insights.Infrastructure.Ingest;

/// <summary>
/// Locks the active attempt of a file with <c>FOR UPDATE</c> through the shared row lock.
/// </summary>
/// <param name="dbContext">Provides the attempts on the connection of the unit of work.</param>
public sealed class IngestAttemptLock(IInsightsDbContext dbContext) : IIngestAttemptLock
{
    private readonly IInsightsDbContext _dbContext = dbContext;

    public Task<IngestAttempt?> LockActiveAttemptAsync(
        Guid fileId,
        Guid companyId,
        CancellationToken cancellationToken
    ) =>
        _dbContext.IngestAttempts.LockFirstAsync(
            attempt => attempt.FileId == fileId
                && attempt.CompanyId == companyId
                && (attempt.State == IngestState.Queued || attempt.State == IngestState.InProcess),
            cancellationToken);

    public Task<IngestAttempt?> LockAttemptForFileDeleteAsync(
        Guid fileId,
        Guid companyId,
        CancellationToken cancellationToken
    ) =>
        _dbContext.IngestAttempts.LockFirstAsync(
            attempt => attempt.FileId == fileId
                && attempt.CompanyId == companyId
                && (attempt.State == IngestState.Queued
                    || attempt.State == IngestState.InProcess
                    || attempt.State == IngestState.Completed),
            cancellationToken);
}
