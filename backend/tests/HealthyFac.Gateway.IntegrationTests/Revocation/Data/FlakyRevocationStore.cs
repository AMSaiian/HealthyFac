using HealthyFac.Shared.Infrastructure.Revocation;
using StackExchange.Redis;

namespace HealthyFac.Gateway.IntegrationTests.Revocation.Data;

/// <summary>
/// A revocation store that fails the first read as a Redis outage would, and answers "not revoked" after that. It
/// counts every read, so a test can tell whether the middleware asked the store at all.
/// </summary>
public sealed class FlakyRevocationStore : ITokenRevocationStore
{
    private int _readCount;
    private int _failuresLeft = 1;

    public int ReadCount => _readCount;

    public Task RevokeAsync(
        Guid userId,
        DateTimeOffset revokedAt,
        TimeSpan timeToLive,
        CancellationToken cancellationToken
    ) => Task.CompletedTask;

    public Task<DateTimeOffset?> GetRevokedAtAsync(Guid userId, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _readCount);

        if (Interlocked.Decrement(ref _failuresLeft) >= 0)
        {
            throw new RedisConnectionException(
                ConnectionFailureType.UnableToConnect,
                CommandFlags.None,
                "Redis is down.",
                innerException: null,
                commandStatus: CommandStatus.Unknown);
        }

        return Task.FromResult<DateTimeOffset?>(null);
    }
}
