using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.Infrastructure.Database.Initialization;
using HealthyFac.Core.Infrastructure.Database.Seeding;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Seeding;
using HealthyFac.Testing.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HealthyFac.Core.IntegrationTests.Database.Data;

public static class DemoDraftSeedFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public async Task RunDemoDraftSeedAsync(SeedingOptions options)
        {
            await new DemoDraftSeeder(
                fixture.GetSingleton<IServiceScopeFactory>(),
                Options.Create(options),
                TimeProvider.System,
                new DemoIngestSeedLocation(
                    Path.Combine(Path.GetDirectoryName(SeedDataFile.Find())!, path2: "demo-ingest"))).SeedAsync(
                CancellationToken.None);
        }

        public async Task RunInitializerSeedAsync(SeedingOptions options, string environmentName)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var initializer = new CoreDbInitializer(
                NullLogger<CoreDbInitializer>.Instance,
                dbContext,
                new TestHostEnvironment { EnvironmentName = environmentName },
                new CoreDevelopmentDataSeeder(
                    dbContext,
                    scope.ServiceProvider.GetRequiredService<TimeProvider>(),
                    Options.Create(options),
                    NullLogger<CoreDevelopmentDataSeeder>.Instance,
                    fixture.InviteTokenService,
                    scope.ServiceProvider.GetRequiredService<IOutbox>()),
                new DemoDraftSeeder(
                    fixture.GetSingleton<IServiceScopeFactory>(),
                    Options.Create(options),
                    TimeProvider.System,
                    new DemoIngestSeedLocation(
                        Path.Combine(Path.GetDirectoryName(SeedDataFile.Find())!, path2: "demo-ingest"))));

            await initializer.SeedOptionalDataAsync(CancellationToken.None);
        }

        public async Task<DemoDrafts> LoadDemoDraftsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            return new DemoDrafts
            {
                Machines = await dbContext.MachineDrafts.AsNoTracking()
                    .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ToListAsync(),
                Metrics = await dbContext.MetricDefinitionDrafts.AsNoTracking()
                    .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ToListAsync(),
                Statuses = await dbContext.StatusDefinitionDrafts.AsNoTracking()
                    .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ToListAsync(),
            };
        }

        public async Task DismissAllMachineDraftsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var drafts = await dbContext.MachineDrafts.ToListAsync();
            drafts.ForEach(draft => draft.Dismiss());

            await dbContext.SaveChangesAsync();
        }

        public async Task<int> CountAllOutboxMessagesAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>().OutboxMessages.CountAsync();
        }
    }
}

public sealed record DemoDrafts
{
    public required IReadOnlyList<MachineDraft> Machines { get; init; }
    public required IReadOnlyList<MetricDefinitionDraft> Metrics { get; init; }
    public required IReadOnlyList<StatusDefinitionDraft> Statuses { get; init; }
}
