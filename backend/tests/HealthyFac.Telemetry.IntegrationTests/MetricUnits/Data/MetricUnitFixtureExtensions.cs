using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.MetricUnits;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Telemetry.Infrastructure.Database.Initialization;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.Messaging.MetricUnits;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.MetricUnits.Data;

public static class MetricUnitFixtureExtensions
{
    extension(TelemetryDatabaseFixture fixture)
    {
        public async Task SeedCatalogAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            await scope.ServiceProvider.GetRequiredService<ITelemetryDbInitializer>().SeedRequiredDataAsync();
        }

        public async Task<IReadOnlyList<string>> LoadCatalogPairsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .MeasurementUnits
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .Select(unit => unit.QuantityKind.Name + "." + unit.Name)
                .ToListAsync();
        }

        public async Task<int> CountMetricUnitsChangedAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var messageType = MetricUnitsChanged.MessageType;

            return await scope.ServiceProvider.GetRequiredService<TelemetryDbContext>()
                .OutboxMessages
                .CountAsync(message => message.Type == messageType);
        }

        public async Task ChangeAbbreviationAsync(string unitName, string abbreviation)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();

            await dbContext.Database.ExecuteSqlRawAsync(
                sql: "UPDATE measurement_units SET abbreviation = {0} WHERE name = {1}",
                abbreviation,
                unitName);
        }

        public async Task<string> LoadAbbreviationAsync(string unitName)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .MeasurementUnits
                .AsNoTracking()
                .Where(unit => unit.Name == unitName)
                .Select(unit => unit.Abbreviation)
                .SingleAsync();
        }

        public async Task<int> CountQuantityKindsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .QuantityKinds
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .CountAsync();
        }

        public async Task<ListMetricUnitsResponse> ListMetricUnitsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<HealthyFac.Shared.Application.UseCases.IRequestHandler<ListMetricUnitsRequest,
                    ListMetricUnitsResponse>>();

            return await handler.HandleAsync(new ListMetricUnitsRequest());
        }
    }
}
