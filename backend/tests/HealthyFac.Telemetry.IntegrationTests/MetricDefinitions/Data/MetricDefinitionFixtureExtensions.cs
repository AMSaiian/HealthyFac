using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.MetricDefinitions.Data;

/// <summary>
/// Runs the metric definition use cases as a user with a company and reads what is stored.
/// Every method opens its own DI scope, so it reads what is stored, not tracked objects.
/// </summary>
public static class MetricDefinitionFixtureExtensions
{
    extension(TelemetryDatabaseFixture fixture)
    {
        public async Task<ApplyMetricDefinitionChangesResponse> ApplyMetricChangesAsync(
            MachineTenant tenant,
            Guid machineId,
            int expectedRevision,
            IReadOnlyList<CreatedMetricDefinition>? created = null,
            IReadOnlyList<UpdatedMetricDefinition>? updated = null,
            IReadOnlyList<Guid>? deleted = null
        ) =>
            await fixture.ApplyMetricChangesAsync(
                tenant.CompanyId,
                new ApplyMetricDefinitionChangesRequest
                {
                    CompanyId = tenant.CompanyId,
                    MachineId = machineId,
                    ExpectedRevision = expectedRevision,
                    Created = created ?? [],
                    Updated = updated ?? [],
                    Deleted = deleted ?? [],
                });

        public async Task<ApplyMetricDefinitionChangesResponse> ApplyMetricChangesAsync(
            Guid companyId,
            ApplyMetricDefinitionChangesRequest request
        )
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<
                    ApplyMetricDefinitionChangesRequest,
                    ApplyMetricDefinitionChangesResponse>>();

            return await handler.HandleAsync(request);
        }

        public async Task<IReadOnlyList<MetricDefinition>> LoadMetricRowsAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .MetricDefinitions
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .Where(metric => metric.MachineRegistrationId == machineId)
                .OrderBy(metric => metric.Key)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<KeyValuePair<string, string>>> LoadMetricUnitPairsAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var rows = await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .MetricDefinitions
                .AsNoTracking()
                .Where(metric => metric.MachineRegistrationId == machineId)
                .OrderBy(metric => metric.Key)
                .Select(metric => new
                {
                    metric.Key,
                    Kind = metric.MeasurementUnit.QuantityKind.Name,
                    Unit = metric.MeasurementUnit.Name,
                })
                .ToListAsync();

            return rows.Select(row => new KeyValuePair<string, string>(row.Key, $"{row.Kind}.{row.Unit}")).ToList();
        }

        public async Task<int> LoadMetricSchemaRevisionAsync(Guid machineId)
        {
            var registration = await fixture.LoadRegistrationAsync(machineId);

            return registration!.MetricSchemaRevision;
        }
    }
}
