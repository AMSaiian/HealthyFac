using HealthyFac.MachineSimulator.Machines;
using HealthyFac.MachineSimulator.Scenarios;
using Npgsql;

namespace HealthyFac.MachineSimulator.Simulation;

/// <summary>
/// Allows one machine simulator for each Core database. The lock is a PostgreSQL session advisory lock, so it works
/// across containers and processes, which a named OS mutex does not.
/// </summary>
/// <remarks>
/// The connection stays open for the life of the process, because a session lock ends with its connection. The lock
/// works over the read-only Core connection, because it writes no row. A broken connection ends the lock, and the
/// simulator does not take it again. This service runs before every other hosted service, so a second simulator
/// starts no MQTT client, no loop, and no control API.
/// </remarks>
/// <param name="configuration">Holds the Core connection string.</param>
/// <param name="logger">Logs the refusal of a second simulator.</param>
public sealed class SimulatorInstanceLock(IConfiguration configuration, ILogger<SimulatorInstanceLock> logger)
    : IHostedService, IAsyncDisposable
{
    /// <summary>
    /// The fixed key of the simulator lock. No other code of the repository uses an advisory lock, so a new lock
    /// needs another key.
    /// </summary>
    public const long LockKey = 4_846_534_901;

    private const string TryLockSql = "SELECT pg_try_advisory_lock(@key)";
    private const string UnlockSql = "SELECT pg_advisory_unlock(@key)";
    private const string KeyParameter = "key";

    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<SimulatorInstanceLock> _logger = logger;

    private NpgsqlConnection? _connection;

    /// <summary>Takes the lock or stops the host.</summary>
    /// <param name="cancellationToken">Cancels the start.</param>
    /// <returns>A task that completes when the lock is taken.</returns>
    /// <exception cref="InvalidOperationException">
    /// The Core connection string is missing, or another simulator holds the lock.
    /// </exception>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var name = SimulatorDatabaseConstants.ConnectionStrings.Core;
        var connectionString = _configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(SimulatorErrors.MissingConnectionString(name));
        }

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(TryLockSql, connection);
        command.Parameters.AddWithValue(KeyParameter, LockKey);
        var isLockTaken = (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
        if (isLockTaken)
        {
            _connection = connection;

            return;
        }

        await connection.DisposeAsync();
        _logger.LogError(message: "Another machine simulator runs on the Core database; this instance stops");

        throw new InvalidOperationException(SimulatorErrors.AlreadyRunning);
    }

    /// <summary>Releases the lock.</summary>
    /// <param name="cancellationToken">Not used: the release is short.</param>
    /// <returns>A task that completes when the lock is released.</returns>
    public async Task StopAsync(CancellationToken cancellationToken) => await DisposeAsync();

    /// <summary>Releases the lock and closes the connection.</summary>
    /// <remarks>
    /// A pooled connection keeps its session lock, so the method unlocks the key before it closes the connection. A
    /// broken connection fails the unlock, and the server has already ended the lock then.
    /// </remarks>
    /// <returns>A task that completes when the connection is closed.</returns>
    public async ValueTask DisposeAsync()
    {
        var connection = Interlocked.Exchange(ref _connection, value: null);
        if (connection is null)
        {
            return;
        }

        try
        {
            await using var command = new NpgsqlCommand(UnlockSql, connection);
            command.Parameters.AddWithValue(KeyParameter, LockKey);
            await command.ExecuteScalarAsync(CancellationToken.None);
        }
        catch (NpgsqlException)
        {
        }

        await connection.DisposeAsync();
    }
}
