using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Agents.Runs;

/// <summary>
/// Renews the lease of a run with one <c>ExecuteUpdate</c> that applies only while the run is <c>running</c>. The
/// update changes the row version, so the renewal copies the new version and the new lease into the tracked run.
/// </summary>
/// <remarks>
/// A sweep that interrupts the run in the same moment finds no running row at the renewal, and the renewal throws. The
/// sweep only takes a run whose lease ended, and the renewal sets a lease of <see cref="AgentOptions.RunLeaseSeconds"/>
/// seconds, so no sweep can take the run between the update and the read of the new version.
/// </remarks>
/// <param name="dbContext">Runs the update.</param>
/// <param name="timeProvider">Provides the time of the renewal.</param>
/// <param name="options">Holds the lease time.</param>
public sealed class AgentRunLease(
    InsightsDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<AgentOptions> options
) : IAgentRunLease
{
    private readonly InsightsDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly AgentOptions _options = options.Value;

    public async Task RenewAsync(AgentRun run, CancellationToken cancellationToken)
    {
        var leaseUntil = _timeProvider.GetUtcNow().UtcDateTime.AddSeconds(_options.RunLeaseSeconds);
        var rowCount = await _dbContext.AgentRuns
            .Where(candidate => candidate.Id == run.Id && candidate.State == AgentRunState.Running)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(candidate => candidate.LeaseUntil, leaseUntil),
                cancellationToken);

        if (rowCount == 0)
        {
            throw new AgentRunInterruptedException(run.Id);
        }

        var rowVersion = await _dbContext.AgentRuns
            .AsNoTracking()
            .Where(candidate => candidate.Id == run.Id)
            .Select(candidate => candidate.RowVersion)
            .SingleAsync(cancellationToken);
        var entry = _dbContext.Entry(run);
        entry.Property(candidate => candidate.RowVersion).OriginalValue = rowVersion;
        entry.Property(candidate => candidate.LeaseUntil).OriginalValue = leaseUntil;
        entry.Property(candidate => candidate.LeaseUntil).CurrentValue = leaseUntil;
    }
}
