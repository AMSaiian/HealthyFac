using HealthyFac.Seeder.ServiceHosts;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Shared.Infrastructure.Messaging.Sending;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.Relay;

/// <summary>
/// The relay port of one service host. It reads and writes the outbox and the inbox of the service database. It never
/// calls Rebus or RabbitMQ.
/// </summary>
public sealed class DatabaseRelayEndpoint : IRelayEndpoint
{
    private readonly ServiceHost _host;

    public DatabaseRelayEndpoint(ServiceHost host)
    {
        _host = host;
    }

    public string Service => _host.Name.Value;

    private ActorContext RelayActor => ServiceActor.OutboxProcessor.For(_host.Name);

    public async Task<OutboxMessage?> FindOldestPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _host.CreateScope(RelayActor);

        return await scope.ServiceProvider.GetRequiredService<IOutboxDbContext>()
            .OutboxMessages
            .AsNoTracking()
            .Where(message => message.ProcessedOn == null && message.Error == null)
            .OrderBy(message => message.OccurredOn)
            .ThenBy(message => message.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public bool Handles(string type) => _host.MessageRegistry.TryGetDispatch(type, out _);

    /// <remarks>
    /// <see cref="IInbox.TryAddAsync"/> inserts the row at once, so no save follows.
    /// </remarks>
    public async Task AddToInboxAsync(OutboxMessage row, CancellationToken cancellationToken)
    {
        using var scope = _host.CreateScope(RelayActor);

        await scope.ServiceProvider.GetRequiredService<IInbox>()
            .TryAddAsync(
                row.Id,
                row.Type,
                row.Payload,
                row.TraceParent,
                row.ActorId,
                row.CompanyId,
                cancellationToken);
    }

    public async Task MarkPublishedAsync(Guid rowId, CancellationToken cancellationToken)
    {
        using var scope = _host.CreateScope(RelayActor);
        var dbContext = scope.ServiceProvider.GetRequiredService<IOutboxDbContext>();

        var row = await dbContext.OutboxMessages.SingleAsync(message => message.Id == rowId, cancellationToken);
        row.MarkPublished(scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task DrainAsync(CancellationToken cancellationToken) => _host.DrainInboxAsync(cancellationToken);

    public async Task<string?> FindFailureAsync(Guid rowId, CancellationToken cancellationToken)
    {
        using var scope = _host.CreateScope(RelayActor);

        var row = await scope.ServiceProvider.GetRequiredService<IInboxDbContext>()
            .InboxMessages
            .AsNoTracking()
            .Where(message => message.Id == rowId)
            .Select(message => new { message.Error, message.AttemptCount, message.ProcessedOn })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        var hasFailed = row.Error is not null || (row.AttemptCount > 0 && row.ProcessedOn is null);

        return hasFailed ? row.Error ?? RelayErrors.NotProcessed(row.AttemptCount) : null;
    }
}
