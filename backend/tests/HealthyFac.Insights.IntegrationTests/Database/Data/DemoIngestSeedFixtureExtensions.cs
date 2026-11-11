using HealthyFac.Insights.Domain.Documents;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Insights.Infrastructure.Database.Initialization;
using HealthyFac.Insights.Infrastructure.Database.Seeding;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Seeding;
using HealthyFac.Testing.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Insights.IntegrationTests.Database.Data;

public static class DemoIngestSeedFixtureExtensions
{
    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e01");
    public static readonly Guid SupervisorId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e21");

    public static readonly DateTimeOffset SeedTime = new(
        year: 2030, month: 5, day: 1, hour: 12, minute: 0, second: 0, TimeSpan.Zero);

    extension(InsightsFixture fixture)
    {
        public async Task RunDemoIngestSeedAsync(bool hasTenant = true)
        {
            await CreateSeeder(fixture, hasTenant).SeedAsync(CancellationToken.None);
        }

        private static DemoIngestSeeder CreateSeeder(InsightsFixture insightsFixture, bool hasTenant)
        {
            var options = Options.Create(
                new SeedingOptions
                {
                    IsOptionalDataSeeded = true,
                    DevelopmentData = new DevelopmentDataOptions
                    {
                        TestTenant = hasTenant
                            ? new TestTenantOptions { CompanyId = CompanyId, SupervisorIdentityId = SupervisorId }
                            : null,
                    },
                });

            return new DemoIngestSeeder(
                insightsFixture.GetSingleton<IServiceScopeFactory>(),
                options,
                new FakeTimeProvider(SeedTime),
                new DemoIngestSeedLocation(
                    Path.Combine(Path.GetDirectoryName(SeedDataFile.Find())!, path2: "demo-ingest")));
        }

        public async Task RunInitializerSeedAsync(string environmentName)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var initializer = new InsightsDbInitializer(
                NullLogger<InsightsDbInitializer>.Instance,
                scope.ServiceProvider.GetRequiredService<InsightsDbContext>(),
                new TestHostEnvironment { EnvironmentName = environmentName },
                CreateSeeder(fixture, hasTenant: true));

            await initializer.SeedOptionalDataAsync(CancellationToken.None);
        }

        public async Task RemoveSeededDocumentAsync(Guid documentId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            await scope.ServiceProvider.GetRequiredService<InsightsDbContext>()
                .IngestedDocuments
                .Where(document => document.Id == documentId)
                .ExecuteDeleteAsync();
        }

        public async Task<IReadOnlyList<IngestAttempt>> LoadSeededAttemptsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<InsightsDbContext>()
                .IngestAttempts
                .AsNoTracking()
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .OrderBy(attempt => attempt.Id)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<IngestedDocument>> LoadSeededDocumentsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<InsightsDbContext>()
                .IngestedDocuments
                .AsNoTracking()
                .OrderBy(document => document.Id)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<DocumentChunk>> LoadSeededChunksAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<InsightsDbContext>()
                .DocumentChunks
                .AsNoTracking()
                .OrderBy(chunk => chunk.Id)
                .ToListAsync();
        }
    }
}
