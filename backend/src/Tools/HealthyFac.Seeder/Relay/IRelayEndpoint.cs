using HealthyFac.Shared.Infrastructure.Messaging.Sending;

namespace HealthyFac.Seeder.Relay;

/// <summary>
/// The relay port of one service. A service is the sender of its own outbox rows and the receiver of the rows whose
/// types it handles.
/// </summary>
public interface IRelayEndpoint
{
    /// <summary>The name of the service, for the error of a failed message.</summary>
    string Service { get; }

    /// <summary>Finds the oldest pending outbox row by its time, then by its ID.</summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The row, or <see langword="null"/> when the service holds no pending row.</returns>
    Task<OutboxMessage?> FindOldestPendingAsync(CancellationToken cancellationToken);

    /// <summary>Tells whether the service handles the message type.</summary>
    /// <param name="type">The stable contract name of the message.</param>
    /// <returns><see langword="true"/> when the service has a use case for the type.</returns>
    bool Handles(string type);

    /// <summary>Stores the row in the inbox of the service, with the same message ID.</summary>
    /// <param name="row">The outbox row of the sender.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the inbox holds the row.</returns>
    Task AddToInboxAsync(OutboxMessage row, CancellationToken cancellationToken);

    /// <summary>Marks an outbox row of the service as published.</summary>
    /// <param name="rowId">The ID of the outbox row.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the row is published.</returns>
    Task MarkPublishedAsync(Guid rowId, CancellationToken cancellationToken);

    /// <summary>Runs the inbox processor of the service until no inbox row is due.</summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the run ends.</returns>
    Task DrainAsync(CancellationToken cancellationToken);

    /// <summary>Reads the failure of the inbox row of a message.</summary>
    /// <param name="rowId">The message ID, which is the ID of the outbox row.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The error text, or <see langword="null"/> when the row has not failed.</returns>
    Task<string?> FindFailureAsync(Guid rowId, CancellationToken cancellationToken);
}
