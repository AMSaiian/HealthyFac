using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.ApiTests.Rpc.Data;

public static class TelemetryRpcFixtureExtensions
{
    extension(TelemetryApiFixture fixture)
    {
        public async Task ArrangeStatusDefinitionAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            dbContext.StatusDefinitions.Add(
                new StatusDefinition(
                    machineId,
                    TelemetryRpcTestData.StatusCode,
                    StatusSeverity.Error,
                    description: null));
            await dbContext.SaveChangesAsync();
        }
    }
}
