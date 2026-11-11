using System.Net.Http.Headers;
using System.Text.Json;
using HealthyFac.Insights.ApiTests.Hosting;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Files;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Testing.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.ApiTests.Ingest.Data;

public static class IngestApiFixtureExtensions
{
    private const string BearerScheme = "Bearer";

    extension(InsightsApiFixture fixture)
    {
        public TestTokenClaims CreateIngestClaims(params string[] permissions) =>
            new()
            {
                Subject = Guid.NewGuid(),
                Organizations = [fixture.CompanyId.ToString()],
                Permissions = permissions,
            };

        public async Task<HttpResponseMessage> SendRequestAsync(
            HttpMethod method,
            string path,
            TestTokenClaims claims
        )
        {
            using var client = fixture.CreateClient();
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                BearerScheme,
                fixture.Tokens.CreateToken(claims));

            return await client.SendAsync(request);
        }

        public IngestFile ArrangeFile(string extension, string purpose, Guid? companyId = null, Guid? machineId = null)
        {
            var file = new IngestFile
            {
                FileId = StartIngestAttemptTestData.FileId,
                CompanyId = companyId ?? fixture.CompanyId,
                MachineId = machineId,
                MachineChangedAt = StartIngestAttemptTestData.ChangedAt,
                Purpose = purpose,
                FileName = "manual",
                Extension = extension,
                SizeBytes = 2048,
            };
            fixture.Files.Add(file);

            return file;
        }

        public async Task ArrangeAttemptsAsync(params IngestState[] states)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            var now = StartIngestAttemptTestData.ChangedAt;

            foreach (var state in states)
            {
                var attempt = new IngestAttempt(
                    fixture.CompanyId,
                    StartIngestAttemptTestData.FileId,
                    machineId: null,
                    now,
                    fileName: "manual",
                    StartIngestAttemptTestData.Pdf,
                    sizeBytes: 2048,
                    now,
                    traceParent: null);
                MoveTo(attempt, state, now);
                dbContext.IngestAttempts.Add(attempt);
            }

            await dbContext.SaveChangesAsync();
        }

        public async Task<Guid> ArrangeAttemptAsync(
            IngestState state,
            Guid? companyId = null,
            DateTime? queuedAt = null
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            var now = StartIngestAttemptTestData.ChangedAt;
            var attempt = new IngestAttempt(
                companyId ?? fixture.CompanyId,
                StartIngestAttemptTestData.FileId,
                machineId: null,
                now,
                fileName: "manual",
                StartIngestAttemptTestData.Pdf,
                sizeBytes: 2048,
                queuedAt ?? now,
                traceParent: null);
            MoveTo(attempt, state, now);
            dbContext.IngestAttempts.Add(attempt);

            await dbContext.SaveChangesAsync();

            return attempt.Id;
        }

        public async Task<IReadOnlyList<IngestAttempt>> LoadAttemptsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .IngestAttempts
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<TMessage>> ReadOutboxMessagesAsync<TMessage>()
            where TMessage : IIntegrationMessage
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var messageType = TMessage.MessageType;
            var payloads = await scope.ServiceProvider.GetRequiredService<InsightsDbContext>()
                .OutboxMessages
                .Where(message => message.Type == messageType)
                .Select(message => message.Payload)
                .ToListAsync();

            return payloads
                .Select(payload => JsonSerializer.Deserialize<TMessage>(payload, JsonSerializerOptions.Web)!)
                .ToList();
        }
    }

    private static void MoveTo(IngestAttempt attempt, IngestState state, DateTime now)
    {
        if (state == IngestState.Queued)
        {
            return;
        }

        attempt.Claim(now, now.AddMinutes(value: 5));

        if (state == IngestState.Completed)
        {
            attempt.Complete(now, pageCount: 1, chunkCount: 1, draftCount: 0, warning: null);
        }
        else if (state == IngestState.Failed)
        {
            attempt.Fail(now, IngestErrorCode.ProviderUnavailable);
        }
        else if (state == IngestState.Interrupted)
        {
            attempt.Interrupt(now, IngestInterruptReason.Cancelled);
        }
    }
}
