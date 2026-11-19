using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.MetricDefinitions.Data;

public static class MetricLimitsFixtureExtensions
{
    extension(TelemetryDatabaseFixture fixture)
    {
        public async Task ChangeMetricLimitsAsync(Guid metricId, MetricLimits limits, MetricBehavior behavior)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var metric = await dbContext.MetricDefinitions.SingleAsync(candidate => candidate.Id == metricId);

            metric.ChangeLimits(limits, behavior);
            await dbContext.SaveChangesAsync();
        }

        public async Task<Guid> InsertMetricWithoutLimitColumnsAsync(Guid metricId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
            var copyId = Guid.CreateVersion7();

            await dbContext.Database.ExecuteSqlAsync(
                $"""
                 INSERT INTO metric_definitions
                     (id, machine_registration_id, key, name, value_kind, measurement_unit_id, created_at, created_by)
                 SELECT {copyId}, machine_registration_id, 'old.style', name, value_kind, measurement_unit_id,
                        created_at, created_by
                 FROM metric_definitions WHERE id = {metricId}
                 """);

            return copyId;
        }
    }
}
