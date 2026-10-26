using System.Diagnostics;
using System.Text.Json;
using HealthyFac.Shared.Application.Messaging;

namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Adds outbox rows to the scoped service context. <see cref="MessagingExtensions"/> registers the context
/// interfaces against one instance, so this class and the use case share the same change tracker.
/// </summary>
/// <param name="dbContext">The scoped service context.</param>
/// <param name="dateProvider">The clock for the time of the business change.</param>
internal sealed class Outbox(IOutboxDbContext dbContext, TimeProvider timeProvider) : IOutbox
{
    public void Add<TMessage>(TMessage message) where TMessage : IIntegrationMessage
        => dbContext.OutboxMessages.Add(
            new OutboxMessage(
                TMessage.MessageType,
                JsonSerializer.Serialize(message, JsonSerializerOptions.Web),
                timeProvider.GetUtcNow().UtcDateTime,
                Activity.Current?.Id));
}
