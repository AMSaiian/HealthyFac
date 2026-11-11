using HealthyFac.Document.Application;
using HealthyFac.Document.Infrastructure.Database;
using HealthyFac.Document.Infrastructure.Database.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Document.IntegrationTests.Database.Data;

public static class DevelopmentSeedFixtureExtensions
{
    extension(DocumentDatabaseFixture fixture)
    {
        public async Task RunDevelopmentSeedAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var seeder = new DocumentDevelopmentDataSeeder(
                scope.ServiceProvider.GetRequiredService<DocumentDbContext>(),
                DevelopmentSeedTestData.CreateOptions());

            await seeder.SeedAsync(CancellationToken.None);
        }

        public async Task<IReadOnlyList<SeededRegistration>> LoadSeededRegistrationsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IDocumentDbContext>()
                .MachineRegistrations
                .AsNoTracking()
                .Where(registration => registration.CompanyId == DevelopmentSeedTestData.CompanyId)
                .OrderBy(registration => registration.Id)
                .Select(registration => new SeededRegistration
                {
                    Id = registration.Id,
                    CompanyId = registration.CompanyId,
                    Name = registration.Name,
                })
                .ToListAsync();
        }
    }
}

public sealed record SeededRegistration
{
    public required Guid Id { get; init; }
    public required Guid CompanyId { get; init; }
    public required string Name { get; init; }
}
