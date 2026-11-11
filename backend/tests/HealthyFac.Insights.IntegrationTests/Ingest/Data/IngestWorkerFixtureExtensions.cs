using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Files;
using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.Infrastructure.Ingest;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Ingest.Data;

public static class IngestWorkerFixtureExtensions
{
    extension(InsightsFixture fixture)
    {
        public IngestFile ArrangeFile(string extension = "pdf", Guid? machineId = null)
        {
            var file = new IngestFile
            {
                FileId = IngestAttemptTestData.FileId,
                CompanyId = IngestAttemptTestData.CompanyId,
                MachineId = machineId,
                MachineChangedAt = IngestAttemptTestData.Now,
                Purpose = IngestRules.AttachmentPurpose,
                FileName = "manual",
                Extension = extension,
                SizeBytes = 1024,
            };
            fixture.Files.Add(file);

            return file;
        }

        public async Task<Guid> StartAttemptAsync()
        {
            using var scope = fixture.CreateActorScope(UserActor());
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<StartIngestAttemptRequest, StartIngestAttemptResponse>>();

            var response = await handler.HandleAsync(
                new StartIngestAttemptRequest
                {
                    CompanyId = IngestAttemptTestData.CompanyId,
                    FileId = IngestAttemptTestData.FileId,
                });

            return response.Id;
        }

        public async Task CancelAttemptAsync(Guid attemptId)
        {
            using var scope = fixture.CreateActorScope(UserActor());
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<CancelIngestAttemptRequest>>();

            await handler.HandleAsync(
                new CancelIngestAttemptRequest { CompanyId = IngestAttemptTestData.CompanyId, Id = attemptId });
        }

        public Task<bool> RunWorkerAsync(CancellationToken stoppingToken) =>
            fixture.GetSingleton<IngestWorker>().RunOnceAsync(stoppingToken);

        public async Task<ClaimedAttempt?> ClaimAttemptAsync()
        {
            using var scope = fixture.CreateActorScope(IngestServiceActors.Worker);

            var claims = scope.ServiceProvider.GetRequiredService<IngestAttemptClaims>();

            return await claims.ClaimNextAsync(CancellationToken.None);
        }

        public async Task RunClaimedAttemptAsync(ClaimedAttempt claimed, CancellationToken stoppingToken)
        {
            using var scope = fixture.CreateActorScope(KnownActors.Restore(claimed.QueuedBy, claimed.CompanyId));
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<ProcessIngestAttemptRequest>>();

            await handler.HandleAsync(
                new ProcessIngestAttemptRequest { AttemptId = claimed.AttemptId, CompanyId = claimed.CompanyId },
                stoppingToken);
        }

        public async Task<IngestAttempt> LoadAttemptAsync(Guid attemptId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .IngestAttempts
                .AsNoTracking()
                .SingleAsync(attempt => attempt.Id == attemptId);
        }

        public async Task<int> CountOutboxMessagesAsync(string messageType)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider
                .GetRequiredService<HealthyFac.Insights.Infrastructure.Database.InsightsDbContext>()
                .OutboxMessages
                .CountAsync(message => message.Type == messageType);
        }
    }

    private static ActorContext UserActor() =>
        new()
        {
            Kind = ActorKind.User,
            UserId = IngestWorkerTestData.UserId,
            CompanyId = IngestAttemptTestData.CompanyId,
        };
}
