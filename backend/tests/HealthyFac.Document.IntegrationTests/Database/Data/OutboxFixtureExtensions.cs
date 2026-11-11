using System.Text.Json;
using HealthyFac.Document.Infrastructure.Database;
using HealthyFac.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Document.IntegrationTests.Database.Data;

/// <summary>
/// Reads the messages that a use case stored in the outbox.
/// </summary>
public static class OutboxFixtureExtensions
{
    extension(DocumentDatabaseFixture fixture)
    {
        public async Task<IReadOnlyList<TMessage>> ReadOutboxMessagesAsync<TMessage>()
            where TMessage : IIntegrationMessage
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var messageType = TMessage.MessageType;
            var payloads = await scope.ServiceProvider.GetRequiredService<DocumentDbContext>()
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
