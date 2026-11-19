using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MetricUnits;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.OperatingPhases.Data;

/// <summary>
/// Arranges machines, definitions, and phases for the phase tests, and reads them back. Every method opens its own
/// DI scope, so it reads what is stored, not tracked objects.
/// </summary>
public static class OperatingPhaseFixtureExtensions
{
    extension(TelemetryDatabaseFixture fixture)
    {
        public async Task<PhaseArrangement> ArrangePhaseMachineAsync()
        {
            var machineId = await fixture.RegisterTenantMachineAsync(MachineRegistrationTestData.CompanyId);
            var metricId = await fixture.AddMetricDefinitionAsync(machineId);
            var statusId = await fixture.AddStatusDefinitionAsync(machineId);
            var stateMetricId = await fixture.AddStateSetMetricAsync(machineId);

            return new PhaseArrangement(machineId, metricId, statusId, stateMetricId);
        }

        public async Task<Guid> AddStateSetMetricAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var unit = await dbContext.MeasurementUnits
                .Include(candidate => candidate.QuantityKind)
                .SingleAsync(candidate => candidate.QuantityKind.Name == DiscreteUnits.KindName
                    && candidate.Name == DiscreteUnits.StateName);
            var metric = new MetricDefinition(
                machineId,
                OperatingPhaseTestData.StateMetricKey,
                name: "Spindle mode",
                description: null,
                MetricValueKind.StateSet,
                unit,
                min: null,
                max: null,
                OperatingPhaseTestData.StateMetricStates);

            dbContext.MetricDefinitions.Add(metric);
            await dbContext.SaveChangesAsync();

            return metric.Id;
        }

        public async Task<Guid> AddStatusDefinitionAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var status = new StatusDefinition(
                machineId, OperatingPhaseTestData.StatusCode, StatusSeverity.Info, description: null);

            dbContext.StatusDefinitions.Add(status);
            await dbContext.SaveChangesAsync();

            return status.Id;
        }

        public async Task<Guid> AddPhaseAsync(
            PhaseArrangement arrangement,
            string code,
            int priority,
            bool isTracked = true
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var phase = OperatingPhaseTestData.CreatePhase(arrangement, code, priority, isTracked);

            dbContext.OperatingPhases.Add(phase);
            await dbContext.SaveChangesAsync();

            return phase.Id;
        }

        public async Task DeletePhaseAsync(Guid phaseId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var phase = await dbContext.OperatingPhases.SingleAsync(candidate => candidate.Id == phaseId);

            dbContext.OperatingPhases.Remove(phase);
            await dbContext.SaveChangesAsync();
        }

        public async Task<OperatingPhase> LoadPhaseAsync(Guid phaseId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .OperatingPhases
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .Include(phase => phase.Conditions)
                .Include(phase => phase.Limits)
                .SingleAsync(phase => phase.Id == phaseId);
        }

        public async Task<(IReadOnlyList<OperatingPhaseCondition> Conditions, IReadOnlyList<OperatingPhaseLimit> Limits
                )>
            LoadPhaseRowsAsync(Guid phaseId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
            var conditions = await dbContext.Set<OperatingPhaseCondition>()
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .Where(condition => EF.Property<Guid>(condition, "PhaseId") == phaseId)
                .ToListAsync();
            var limits = await dbContext.Set<OperatingPhaseLimit>()
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .Where(limit => EF.Property<Guid>(limit, "PhaseId") == phaseId)
                .ToListAsync();

            return (conditions, limits);
        }

        public async Task ExecuteSqlAsync(FormattableString sql)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();

            await dbContext.Database.ExecuteSqlAsync(sql);
        }
    }
}

public sealed record PhaseArrangement(Guid MachineId, Guid MetricId, Guid StatusId, Guid StateMetricId);
