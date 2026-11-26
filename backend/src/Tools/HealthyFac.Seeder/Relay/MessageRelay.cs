using HealthyFac.Shared.Infrastructure.Messaging.Sending;

namespace HealthyFac.Seeder.Relay;

/// <summary>
/// Moves the messages between the services without a broker. After each seed event, it takes the oldest pending
/// outbox row of all services, stores it in the inbox of every service that handles its type, marks it published, and
/// runs the inbox processors. A row that a receiver adds while it handles a message is newer than every row that was
/// pending before, so it comes after them. The order of the messages is therefore the order of their times.
/// </summary>
/// <param name="endpoints">The relay ports of the services.</param>
public sealed class MessageRelay(IReadOnlyList<IRelayEndpoint> endpoints)
{
    private readonly IReadOnlyList<IRelayEndpoint> _endpoints = endpoints;

    /// <summary>Relays messages until no service holds a pending outbox row.</summary>
    /// <param name="eventItem">The seed event that caused the messages, named in the error of a failed message.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests. It is checked before each message and passed to each step of a message.</param>
    /// <returns>A task that completes when no row is pending.</returns>
    /// <exception cref="InvalidOperationException">A receiver rejected a message.</exception>
    public async Task RunAsync(string eventItem, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var next = await FindOldestPendingAsync(cancellationToken);

            if (next is null)
            {
                return;
            }

            await RelayAsync(next.Value.Sender, next.Value.Row, eventItem, cancellationToken);
        }
    }

    private async Task<(IRelayEndpoint Sender, OutboxMessage Row)?> FindOldestPendingAsync(
        CancellationToken cancellationToken
    )
    {
        (IRelayEndpoint Sender, OutboxMessage Row)? oldest = null;

        foreach (var endpoint in _endpoints)
        {
            var row = await endpoint.FindOldestPendingAsync(cancellationToken);

            if (row is not null && (oldest is null || IsOlder(row, oldest.Value.Row)))
            {
                oldest = (endpoint, row);
            }
        }

        return oldest;
    }

    /// <summary>Relays one row: the receiver inboxes, the published mark of the sender, then the drains.</summary>
    /// <remarks>
    /// Resume: the inbox insert commits at once, so a stop after it leaves the outbox row pending. The next run relays
    /// the same message ID again, and the inbox drops the repeat. The next run then reports an incomplete seed.
    /// </remarks>
    /// <param name="sender">The endpoint that holds the row.</param>
    /// <param name="row">The pending outbox row.</param>
    /// <param name="eventItem">The seed event, for the error of a failed message.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when every receiver handled the row.</returns>
    private async Task RelayAsync(
        IRelayEndpoint sender,
        OutboxMessage row,
        string eventItem,
        CancellationToken cancellationToken
    )
    {
        var receivers = _endpoints.Where(endpoint => endpoint.Handles(row.Type)).ToList();

        foreach (var receiver in receivers)
        {
            await receiver.AddToInboxAsync(row, cancellationToken);
        }

        await sender.MarkPublishedAsync(row.Id, cancellationToken);

        foreach (var receiver in receivers)
        {
            await receiver.DrainAsync(cancellationToken);

            var failure = await receiver.FindFailureAsync(row.Id, cancellationToken);

            if (failure is not null)
            {
                throw new InvalidOperationException(
                    RelayErrors.MessageFailed(row.Type, receiver.Service, eventItem, failure));
            }
        }
    }

    private static bool IsOlder(OutboxMessage candidate, OutboxMessage current) =>
        candidate.OccurredOn < current.OccurredOn
        || (candidate.OccurredOn == current.OccurredOn && candidate.Id.CompareTo(current.Id) < 0);
}
