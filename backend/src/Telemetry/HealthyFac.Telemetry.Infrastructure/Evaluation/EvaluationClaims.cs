using HealthyFac.Shared.Infrastructure.Persistence.RowLocks;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthyFac.Telemetry.Infrastructure.Evaluation;

/// <summary>
/// A machine that a worker claimed for one run.
/// </summary>
public sealed record ClaimedEvaluation
{
    public required Guid MachineId { get; init; }

    public required Guid CompanyId { get; init; }
}

/// <summary>
/// The claim query of the evaluator worker. It locks the first due machine with <c>FOR UPDATE SKIP LOCKED</c> and sets
/// a lease in one transaction, so two workers never take the same machine, and neither waits for the other. A machine
/// whose lease ended is due again: its worker stopped or failed.
/// </summary>
/// <param name="dbContext">Reads and changes the machine evaluations.</param>
/// <param name="timeProvider">Provides the time of the claim.</param>
/// <param name="options">Holds the lease time.</param>
public sealed class EvaluationClaims(
    ITelemetryDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<DetectionOptions> options
)
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly DetectionOptions _options = options.Value;

    /// <summary>
    /// Counts the machines with a due time that is not after now, whether a worker owns them or not.
    /// </summary>
    /// <param name="cancellationToken">Cancels the count.</param>
    /// <returns>The number of due machines.</returns>
    public Task<int> CountDueAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return _dbContext.MachineEvaluations
            .AsNoTracking()
            .CountAsync(
                candidate => candidate.DueAt <= now
                    && _dbContext.MachineRegistrations.Any(registration => registration.Id == candidate.Id),
                cancellationToken);
    }

    /// <summary>
    /// Claims the machine with the earliest due time that no worker owns.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the claim before it commits.</param>
    /// <returns>The claimed machine, or <see langword="null"/> when no machine is due and free.</returns>
    public async Task<ClaimedEvaluation?> ClaimNextAsync(CancellationToken cancellationToken)
    {
        ClaimedEvaluation? claimed = null;

        await _dbContext.RunInTransactionAsync(
            async token =>
            {
                claimed = null;
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var evaluation = await _dbContext.MachineEvaluations.ClaimFirstAsync(
                    candidate => candidate.DueAt <= now
                        && (candidate.LeaseUntil == null || candidate.LeaseUntil < now)
                        && _dbContext.MachineRegistrations.Any(registration => registration.Id == candidate.Id),
                    candidate => candidate.DueAt,
                    token);

                if (evaluation is null)
                {
                    return;
                }

                var companyId = await _dbContext.MachineRegistrations
                    .AsNoTracking()
                    .Where(registration => registration.Id == evaluation.Id)
                    .Select(registration => registration.CompanyId)
                    .SingleAsync(token);

                evaluation.Claim(now.AddSeconds(_options.LeaseSeconds));
                await _dbContext.SaveChangesAsync(token);
                claimed = new ClaimedEvaluation { MachineId = evaluation.Id, CompanyId = companyId };
            },
            cancellationToken);

        return claimed;
    }
}
