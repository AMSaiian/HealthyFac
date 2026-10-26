using System.Diagnostics;
using System.Text.Json;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Messaging;

namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Adds outbox rows to the scoped service context. <see cref="MessagingExtensions"/> registers the context
/// interfaces against one instance, so this class and the use case share the same change tracker.
/// </summary>
/// <param name="dbContext">The scoped service context.</param>
/// <param name="timeProvider">The clock for the time of the business change.</param>
/// <param name="currentUserProvider">The source of the actor that the row carries to the receiver.</param>
internal sealed class Outbox(
    IOutboxDbContext dbContext,
    TimeProvider timeProvider,
    ICurrentUserProvider currentUserProvider
) : IOutbox
{
    public void Add<TMessage>(TMessage message) where TMessage : IIntegrationMessage
    {
        var actor = currentUserProvider.TryGetActor() ?? throw MessagingErrors.ActorMissing();

        dbContext.OutboxMessages.Add(
            new OutboxMessage(
                TMessage.MessageType,
                JsonSerializer.Serialize(message, JsonSerializerOptions.Web),
                timeProvider.GetUtcNow().UtcDateTime,
                Activity.Current?.Id,
                actor.UserId,
                actor.CompanyId));
    }
}
