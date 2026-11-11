using System.Security.Cryptography;
using HealthyFac.Document.Application;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.Infrastructure.Database;
using HealthyFac.Document.Infrastructure.Database.Initialization;
using HealthyFac.Document.Infrastructure.Database.Seeding;
using HealthyFac.Document.Infrastructure.Files.Storage;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Document.IntegrationTests.Database.Data;

public static class DemoFileSeedFixtureExtensions
{
    extension(DocumentDatabaseFixture fixture)
    {
        public async Task RunDemoFileSeedAsync(bool hasTenant = true)
        {
            var options = DemoFileSeedTestData.CreateOptions(hasTenant);
            using var scope = fixture.ScopeFactory.CreateScope();
            await new DocumentDevelopmentDataSeeder(
                scope.ServiceProvider.GetRequiredService<DocumentDbContext>(),
                options).SeedAsync(CancellationToken.None);

            await new DemoFileSeeder(
                fixture.GetSingleton<IServiceScopeFactory>(),
                options,
                new FakeTimeProvider(DemoFileSeedTestData.SeedTime),
                new DemoIngestSeedLocation(DemoFileSeedTestData.Folder)).SeedAsync(CancellationToken.None);
        }

        public async Task RunInitializerSeedAsync(string environmentName)
        {
            var options = DemoFileSeedTestData.CreateOptions();
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
            var initializer = new DocumentDbInitializer(
                NullLogger<DocumentDbInitializer>.Instance,
                dbContext,
                new TestHostEnvironment { EnvironmentName = environmentName },
                new DocumentDevelopmentDataSeeder(dbContext, options),
                new DemoFileSeeder(
                    fixture.GetSingleton<IServiceScopeFactory>(),
                    options,
                    new FakeTimeProvider(DemoFileSeedTestData.SeedTime),
                    new DemoIngestSeedLocation(DemoFileSeedTestData.Folder)),
                scope.ServiceProvider.GetRequiredService<BlobFileStorage>());

            await initializer.SeedOptionalDataAsync(CancellationToken.None);
        }

        public async Task<IReadOnlyList<FileRef>> LoadDemoFilesAsync(bool isDeletedIncluded = false)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var files = scope.ServiceProvider.GetRequiredService<IDocumentDbContext>().FileRefs.AsNoTracking();

            return await (isDeletedIncluded ? files.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName]) : files)
                .OrderBy(file => file.Id)
                .ToListAsync();
        }

        public async Task DeleteFileRefAsync(Guid id)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
            var fileRef = await dbContext.FileRefs.SingleAsync(file => file.Id == id);
            dbContext.FileRefs.Remove(fileRef);

            await dbContext.SaveChangesAsync();
        }

        public async Task<string> HashBlobAsync(Guid id)
        {
            var blob = fixture.BlobService.GetBlobContainerClient(DatabaseTestData.ContainerName)
                .GetBlobClient(id.ToString());
            await using var stream = await blob.OpenReadAsync();

            return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream));
        }

        public async Task<DateTimeOffset?> LoadBlobWrittenAtAsync(Guid id)
        {
            var blob = fixture.BlobService.GetBlobContainerClient(DatabaseTestData.ContainerName)
                .GetBlobClient(id.ToString());
            var exists = await blob.ExistsAsync();

            return exists.Value ? (await blob.GetPropertiesAsync()).Value.LastModified : null;
        }

        public async Task DeleteBlobAsync(Guid id)
        {
            await fixture.BlobService.GetBlobContainerClient(DatabaseTestData.ContainerName)
                .DeleteBlobIfExistsAsync(id.ToString());
        }
    }
}
