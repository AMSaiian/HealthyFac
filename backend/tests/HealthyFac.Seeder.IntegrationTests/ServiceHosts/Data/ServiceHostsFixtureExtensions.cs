using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.ServiceHosts.Data;

public static class ServiceHostsFixtureExtensions
{
    extension(SeederFixture fixture)
    {
        public async Task<int> CountRolesAsync()
        {
            using var scope = fixture.Hosts.Core.CreateScope(ServiceHostsTestData.Actor);

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>().Roles.CountAsync();
        }

        public async Task<int> CountMeasurementUnitsAsync()
        {
            using var scope = fixture.Hosts.Telemetry.CreateScope(ServiceHostsTestData.Actor);

            return await scope.ServiceProvider.GetRequiredService<TelemetryDbContext>().MeasurementUnits.CountAsync();
        }
    }
}
