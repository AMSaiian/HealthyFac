using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Shared.Infrastructure.Persistence.RowLocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Agents.Runs;

/// <summary>
/// The queries of the worker loop: the claim of the oldest due run and the sweep of a run whose lease ended. Both lock
/// one row with <c>FOR UPDATE SKIP LOCKED</c>, change it, and save in one transaction, so two workers never take the
/// same row.
/// </summary>
/// <param name="dbContext">Reads and changes the runs.</param>
/// <param name="timeProvider">Provides the times of the claim and the sweep.</param>
/// <param name="options">Holds the lease of a run.</param>
public sealed class AgentRunClaims(
    IInsightsDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<AgentOptions> options
)
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly AgentOptions _options = options.Value;

    /// <summary>
    /// Claims the oldest due run: <c>queued</c> with a due time that is not after now becomes <c>running</c> with a
    /// lease.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the claim before it commits.</param>
    /// <returns>The claimed run, or <see langword="null"/> when no queued run is due.</returns>
    public async Task<ClaimedRun?> ClaimNextAsync(CancellationToken cancellationToken)
    {
        ClaimedRun? claimed = null;

        await _dbContext.RunInTransactionAsync(
            async token =>
            {
                claimed = null;
                var now = _timeProvider.GetUtcNow().UtcDateTime;

                var run = await _dbContext.AgentRuns.ClaimFirstAsync(
                    candidate => candidate.State == AgentRunState.Queued && candidate.DueAt <= now,
                    candidate => candidate.DueAt,
                    token);

                if (run is null)
                {
                    return;
                }

                var companyId = await FindCompanyIdAsync(run, token);
                run.Claim(now, now.AddSeconds(_options.RunLeaseSeconds));
                await _dbContext.SaveChangesAsync(token);
                claimed = new ClaimedRun { RunId = run.Id, CompanyId = companyId, Role = run.Role };
            },
            cancellationToken);

        return claimed;
    }

    /// <summary>
    /// Interrupts one running run whose lease ended, and discards its buffered actions. The run is dead (the
    /// service stopped or crashed), because a live run renews its lease after each model call and each tool call.
    /// No new run replaces it: the case shows that the AI is unavailable. Both changes commit together.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the sweep before it commits.</param>
    /// <returns><see langword="true"/> when a run was interrupted; <see langword="false"/> when none was due.</returns>
    public async Task<AgentRole?> InterruptNextExpiredAsync(CancellationToken cancellationToken)
    {
        AgentRole? interruptedRole = null;

        await _dbContext.RunInTransactionAsync(
            async token =>
            {
                interruptedRole = null;
                var now = _timeProvider.GetUtcNow().UtcDateTime;

                var run = await _dbContext.AgentRuns.ClaimFirstAsync(
                    candidate => candidate.State == AgentRunState.Running && candidate.LeaseUntil < now,
                    candidate => candidate.LeaseUntil,
                    token);

                if (run is null)
                {
                    return;
                }

                run.Interrupt(now);

                var actions = await _dbContext.AgentActions
                    .Where(candidate => candidate.RunId == run.Id && candidate.State == AgentActionState.Buffered)
                    .ToListAsync(token);

                actions.ForEach(action => action.Discard());
                await _dbContext.SaveChangesAsync(token);
                interruptedRole = run.Role;
            },
            cancellationToken);

        return interruptedRole;
    }

    public async Task<long> CountDueAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return await _dbContext.AgentRuns.LongCountAsync(
            candidate => candidate.State == AgentRunState.Queued && candidate.DueAt <= now,
            cancellationToken);
    }

    public async Task<AgentRun?> LoadAsync(Guid runId, CancellationToken cancellationToken) =>
        await _dbContext.AgentRuns
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == runId, cancellationToken);

    private async Task<Guid> FindCompanyIdAsync(AgentRun run, CancellationToken cancellationToken)
    {
        if (run.CaseId is { } caseId)
        {
            return await _dbContext.MaintenanceCases
                .Where(candidate => candidate.Id == caseId)
                .Select(candidate => candidate.CompanyId)
                .SingleAsync(cancellationToken);
        }

        return await _dbContext.IngestAttempts
            .Where(candidate => candidate.Id == run.IngestAttemptId)
            .Select(candidate => candidate.CompanyId)
            .SingleAsync(cancellationToken);
    }
}
