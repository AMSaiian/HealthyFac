using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.Evaluation;

/// <summary>
/// The changes that reset what the evaluation learned: the statistics of a phase whose meaning changed, and the
/// statistics, the streaks, and the open episodes of a machine that left maintenance. The methods only track the
/// changes. The caller saves them in its own save, so a reset never commits alone.
/// </summary>
public static class EvaluationResets
{
    extension(ITelemetryDbContext dbContext)
    {
        /// <summary>
        /// Removes the statistics rows of an operating phase.
        /// </summary>
        /// <param name="phaseId">The phase.</param>
        /// <param name="cancellationToken">Cancels the load.</param>
        public async Task RemovePhaseStatisticsAsync(Guid phaseId, CancellationToken cancellationToken)
        {
            var rows = await dbContext.MetricStatistics
                .Where(row => row.OperatingPhaseId == phaseId)
                .ToListAsync(cancellationToken);

            dbContext.MetricStatistics.RemoveRange(rows);
        }

        /// <summary>
        /// Removes the statistics rows and the rule streaks of a machine, and resolves its open episodes. Each resolve
        /// queues its message.
        /// </summary>
        /// <param name="outbox">Receives the messages of the resolves.</param>
        /// <param name="registration">The machine.</param>
        /// <param name="resolvedAt">The time of the resolves.</param>
        /// <param name="cancellationToken">Cancels the loads.</param>
        public async Task ResetMachineAsync(
            IOutbox outbox,
            MachineRegistration registration,
            DateTime resolvedAt,
            CancellationToken cancellationToken
        )
        {
            var metrics = await dbContext.MetricDefinitions
                .AsNoTracking()
                .Where(metric => metric.MachineRegistrationId == registration.Id)
                .Select(metric => new { metric.Id, metric.Key })
                .ToListAsync(cancellationToken);
            var metricIds = metrics.Select(metric => metric.Id).ToList();
            var statistics = await dbContext.MetricStatistics
                .Where(row => metricIds.Contains(row.MetricDefinitionId))
                .ToListAsync(cancellationToken);
            var streaks = await dbContext.RuleStreaks
                .Where(streak => streak.MachineRegistrationId == registration.Id)
                .ToListAsync(cancellationToken);
            var openEpisodes = await dbContext.SignalEpisodes
                .Where(episode => episode.MachineRegistrationId == registration.Id
                    && episode.State == SignalEpisodeState.Open)
                .ToListAsync(cancellationToken);
            var statuses = await dbContext.StatusDefinitions
                .AsNoTracking()
                .Where(status => status.MachineRegistrationId == registration.Id)
                .Select(status => new { status.Id, status.Code })
                .ToListAsync(cancellationToken);
            var phases = await dbContext.OperatingPhases
                .AsNoTracking()
                .Where(phase => phase.MachineRegistrationId == registration.Id)
                .Select(phase => new { phase.Id, phase.Code })
                .ToListAsync(cancellationToken);

            dbContext.MetricStatistics.RemoveRange(statistics);
            dbContext.RuleStreaks.RemoveRange(streaks);
            new EpisodeChanges(
                    dbContext,
                    outbox,
                    registration.Id,
                    registration.CompanyId,
                    openEpisodes,
                    metrics.ToDictionary(metric => metric.Id, metric => metric.Key),
                    statuses.ToDictionary(status => status.Id, status => status.Code),
                    phases.ToDictionary(phase => phase.Id, phase => phase.Code))
                .ResolveAll(resolvedAt);
        }
    }
}
