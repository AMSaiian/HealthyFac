using Azure.Storage.Blobs;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Seeder.Accounts;
using HealthyFac.Seeder.IntegrationTests.Accounts.Data;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.Ingest.Data;
using HealthyFac.Seeder.IntegrationTests.Timeline.Data;
using HealthyFac.Seeder.Reset;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.Reset.Data;

public static class SeedResetFixtureExtensions
{
    extension(SeederFixture fixture)
    {
        public async Task<JobResult> RunResetAsync(DateTimeOffset anchor, CancellationToken cancellationToken) =>
            await fixture.RunJobAsync(
                [SeedResetTestData.ResetArgument], cancellationToken, anchor: anchor);

        public async Task RunResetStoppedAfterTheMarkerAsync(CancellationTokenSource stop)
        {
            var folder = new SeedFolder(Path.Combine(AppContext.BaseDirectory, SeedProgram.SeedFolderName));
            fixture.EnterNextMoment(item: "reset");
            var reset = new SeedReset(
                fixture.Hosts,
                new KeycloakAccounts(fixture.Hosts.Auth),
                SeedCompletionTestData.CompanyId,
                SeedCompletion.FindLastDraftId(folder),
                TextWriter.Null)
            {
                AfterMarkerDeleted = () =>
                {
                    stop.Cancel();

                    return Task.CompletedTask;
                },
            };

            await reset.RunAsync(stop.Token);
        }

        public async Task AddExtraMemberAsync(CancellationToken cancellationToken)
        {
            var organizationId = await fixture.FindOrganizationAsync(SeedCompletionTestData.CompanyId);
            var userId = await new KeycloakAccounts(fixture.Hosts.Auth).CreateUserAsync(
                Guid.CreateVersion7(),
                SeedResetTestData.ExtraEmail,
                SeedResetTestData.ExtraFirstName,
                SeedResetTestData.ExtraLastName,
                SeedResetTestData.ExtraPassword,
                cancellationToken);
            await fixture.AddMemberAsync(organizationId!.Value, userId);
        }

        public async Task AddExtraBlobAsync(CancellationToken cancellationToken)
        {
            using var scope = fixture.Hosts.Document.CreateScope(SeederTestData.InitializationActor);
            var container = scope.ServiceProvider.GetRequiredService<BlobServiceClient>()
                .GetBlobContainerClient(IngestTestData.BlobContainer);
            await container.GetBlobClient(SeedResetTestData.ExtraBlobName)
                .UploadAsync(BinaryData.FromString(SeedResetTestData.ExtraBlobName), cancellationToken);
        }

        public async Task<List<Guid>> LoadRoleIdsAsync()
        {
            using var scope = fixture.Hosts.Core.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .Roles
                .AsNoTracking()
                .OrderBy(role => role.Code)
                .Select(role => role.Id)
                .ToListAsync();
        }

        public async Task<List<Guid>> LoadUnitIdsAsync()
        {
            using var scope = fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<TelemetryDbContext>()
                .MeasurementUnits
                .AsNoTracking()
                .Select(unit => unit.Id)
                .OrderBy(id => id)
                .ToListAsync();
        }
    }
}
