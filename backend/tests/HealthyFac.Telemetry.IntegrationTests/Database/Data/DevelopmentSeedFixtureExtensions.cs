using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Seeding;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.MetricSchemas;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Telemetry.Infrastructure.Database.Seeding;
using HealthyFac.Testing.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.Database.Data;

public static class DevelopmentSeedFixtureExtensions
{
    extension(TelemetryDatabaseFixture fixture)
    {
        public async Task RunDevelopmentSeedAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var seeder = new TelemetryDevelopmentDataSeeder(
                scope.ServiceProvider.GetRequiredService<TelemetryDbContext>(),
                DevelopmentSeedTestData.CreateOptions(),
                scope.ServiceProvider.GetRequiredService<TimeProvider>(),
                scope.ServiceProvider.GetRequiredService<IMetricSchemaGenerator>());

            await seeder.SeedAsync(CancellationToken.None);
        }

        public async Task RunDemoDefinitionDraftSeedAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var seeder = new DemoDefinitionDraftSeeder(
                scope.ServiceProvider.GetRequiredService<TelemetryDbContext>(),
                DevelopmentSeedTestData.CreateOptions(),
                new DemoIngestSeedLocation(
                    Path.Combine(Path.GetDirectoryName(SeedDataFile.Find())!, path2: "demo-ingest")));

            await seeder.SeedAsync(CancellationToken.None);
        }

        public async Task<DemoDefinitionDrafts> LoadDemoDefinitionDraftsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();

            return new DemoDefinitionDrafts
            {
                Metrics = await dbContext.MetricDefinitionDrafts
                    .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .AsNoTracking()
                    .ToListAsync(),
                Statuses = await dbContext.StatusDefinitionDrafts
                    .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .AsNoTracking()
                    .ToListAsync(),
                Entries = await dbContext.ProposalEntries
                    .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .AsNoTracking()
                    .ToListAsync(),
            };
        }

        public async Task<IReadOnlyList<SeededRegistration>> LoadSeededRegistrationsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();

            return await dbContext.MachineRegistrations
                .AsNoTracking()
                .Where(registration => registration.CompanyId == DevelopmentSeedTestData.CompanyId)
                .OrderBy(registration => registration.GatewayId)
                .Select(registration => new SeededRegistration
                {
                    Id = registration.Id,
                    GatewayId = registration.GatewayId,
                    StatusDefinitionCount =
                        dbContext.StatusDefinitions.Count(status => status.MachineRegistrationId == registration.Id),
                })
                .ToListAsync();
        }
    }
}

public sealed record DemoDefinitionDrafts
{
    public required IReadOnlyList<MetricDefinitionDraft> Metrics { get; init; }
    public required IReadOnlyList<StatusDefinitionDraft> Statuses { get; init; }
    public required IReadOnlyList<ProposalEntry> Entries { get; init; }
}

public sealed record SeededRegistration
{
    public required Guid Id { get; init; }
    public required string GatewayId { get; init; }
    public required int StatusDefinitionCount { get; init; }
}
