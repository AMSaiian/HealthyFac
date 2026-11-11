using System.Text.Json;
using HealthyFac.Document.Messaging.Files;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Domain.Documents;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Documents.Data;

public static class FileMessageFixtureExtensions
{
    extension(InsightsFixture fixture)
    {
        public async Task HandleFileDeletedAsync(Guid companyId, Guid fileId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<FileDeleted>>();

            await handler.HandleAsync(new FileDeleted { FileId = fileId, CompanyId = companyId });
        }

        public async Task HandleMachineChangedAsync(Guid companyId, Guid fileId, Guid? machineId, DateTime changedAt)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<FileMachineChanged>>();

            await handler.HandleAsync(
                new FileMachineChanged
                {
                    FileId = fileId,
                    CompanyId = companyId,
                    MachineId = machineId,
                    ChangedAt = changedAt,
                });
        }

        public async Task<IReadOnlyList<IngestAttempt>> LoadAttemptsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .IngestAttempts
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IngestedDocument?> LoadDocumentAsync(Guid fileId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .IngestedDocuments
                .AsNoTracking()
                .SingleOrDefaultAsync(document => document.Id == fileId);
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
}
