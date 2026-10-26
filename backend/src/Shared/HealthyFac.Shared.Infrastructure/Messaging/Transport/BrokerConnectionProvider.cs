using RabbitMQ.Client;

namespace HealthyFac.Shared.Infrastructure.Messaging.Transport;

/// <summary>
/// Keeps one RabbitMQ connection for the readiness probes. A probe that opened its own connection and disposed
/// only its channel would leak one connection for each probe. Register the provider as a singleton.
/// </summary>
public sealed class BrokerConnectionProvider : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;

    private readonly SemaphoreSlim _gate = new(1, 1);

    private IConnection? _connection;

    public BrokerConnectionProvider(Uri brokerUri)
    {
        _factory = new ConnectionFactory
        {
            Uri = brokerUri,
            RequestedConnectionTimeout = MessagingConstants.HealthCheck.ConnectionTimeout,
        };
    }

    /// <summary>
    /// Returns the cached connection while it is open. Otherwise it disposes the old connection and opens a new
    /// one. The gate lets parallel probes open one connection.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>An open connection to the broker.</returns>
    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            var staleConnection = _connection;
            _connection = null;
            if (staleConnection is not null)
            {
                await staleConnection.DisposeAsync();
            }

            _connection = await _factory.CreateConnectionAsync(cancellationToken);

            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _gate.Dispose();
    }
}
