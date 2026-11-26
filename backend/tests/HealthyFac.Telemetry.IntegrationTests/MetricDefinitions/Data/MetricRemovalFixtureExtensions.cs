using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.OperatingPhases.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.MetricDefinitions.Data;

/// <summary>
/// Arranges the phases and the removal calls of the metric removal tests.
/// </summary>
public static class MetricRemovalFixtureExtensions
{
    extension(TelemetryDatabaseFixture fixture)
    {
        /// <summary>
        /// Stores a phase whose only condition reads the state set metric, with a limit row for each given metric.
        /// </summary>
        /// <param name="arrangement">The machine and its metrics.</param>
        /// <param name="code">The code of the phase.</param>
        /// <param name="limitMetricIds">The numeric metrics that get a limit row.</param>
        /// <returns>The ID of the phase.</returns>
        public async Task<Guid> AddPhaseUsingStateMetricAsync(
            PhaseArrangement arrangement,
            string code,
            params Guid[] limitMetricIds
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();

            var condition = OperatingPhaseCondition.Create(
                PhaseConditionSource.Metric(
                    arrangement.StateMetricId,
                    MetricValueKind.StateSet,
                    OperatingPhaseTestData.StateMetricStates),
                PhaseConditionOperator.In,
                value: null,
                upperValue: null,
                MetricRemovalTestData.RunningStates);

            var phase = new OperatingPhase(
                arrangement.MachineId,
                code,
                code,
                description: null,
                priority: 0,
                isTracked: true,
                [condition],
                [
                    .. limitMetricIds.Select(metricId => OperatingPhaseLimit.Create(
                        metricId,
                        MetricValueKind.Numeric,
                        OperatingPhaseTestData.PhaseLimits)),
                ]);

            dbContext.OperatingPhases.Add(phase);
            await dbContext.SaveChangesAsync();

            return phase.Id;
        }

        /// <summary>
        /// Stores a phase with a status condition and a limit row for each given metric.
        /// </summary>
        /// <param name="arrangement">The machine and its metrics.</param>
        /// <param name="code">The code of the phase.</param>
        /// <param name="limitMetricIds">The numeric metrics that get a limit row.</param>
        /// <returns>The ID of the phase.</returns>
        public async Task<Guid> AddPhaseWithLimitsAsync(
            PhaseArrangement arrangement,
            string code,
            params Guid[] limitMetricIds
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();

            var condition = OperatingPhaseCondition.Create(
                PhaseConditionSource.Status(arrangement.StatusId),
                PhaseConditionOperator.Active,
                value: null,
                upperValue: null,
                states: null);

            var phase = new OperatingPhase(
                arrangement.MachineId,
                code,
                code,
                description: null,
                priority: 0,
                isTracked: true,
                [condition],
                [
                    .. limitMetricIds.Select(metricId => OperatingPhaseLimit.Create(
                        metricId,
                        MetricValueKind.Numeric,
                        OperatingPhaseTestData.PhaseLimits)),
                ]);

            dbContext.OperatingPhases.Add(phase);
            await dbContext.SaveChangesAsync();

            return phase.Id;
        }

        /// <summary>
        /// Removes metric definitions of the machine at the stored schema revision.
        /// </summary>
        /// <param name="machineId">The machine.</param>
        /// <param name="metricIds">The definitions to remove.</param>
        public async Task RemoveMetricsAsync(Guid machineId, params Guid[] metricIds)
        {
            var revision = await fixture.LoadMetricSchemaRevisionAsync(machineId);

            await fixture.ApplyMetricChangesAsync(
                new MachineTenant(MachineRegistrationTestData.CompanyId),
                machineId,
                revision,
                deleted: metricIds);
        }

        /// <summary>
        /// Adds a numeric metric with the given key and returns its ID.
        /// </summary>
        /// <param name="machineId">The machine.</param>
        /// <param name="key">The key of the new metric.</param>
        /// <returns>The ID of the new definition.</returns>
        public async Task<Guid> AddNumericMetricAsync(Guid machineId, string key)
        {
            var revision = await fixture.LoadMetricSchemaRevisionAsync(machineId);

            await fixture.ApplyMetricChangesAsync(
                new MachineTenant(MachineRegistrationTestData.CompanyId),
                machineId,
                revision,
                [MetricDefinitionTestData.NewNumeric(key)]);

            var rows = await fixture.LoadMetricRowsAsync(machineId);

            return rows.Single(metric => metric.Key == key).Id;
        }
    }
}
