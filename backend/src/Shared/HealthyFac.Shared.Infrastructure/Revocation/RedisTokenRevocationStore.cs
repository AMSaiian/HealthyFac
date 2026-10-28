using StackExchange.Redis;

namespace HealthyFac.Shared.Infrastructure.Revocation;

/// <summary>
/// Keeps the revocation list in Redis. The value is the revocation time in Unix seconds, and the entry expires with
/// its time to live, so the list needs no cleanup. The methods let a Redis failure propagate. The caller decides
/// whether Redis is a hard dependency (see <c>RedisCircuitBreaker</c>).
/// </summary>
/// <param name="connection">The shared Redis connection.</param>
public sealed class RedisTokenRevocationStore(IConnectionMultiplexer connection) : ITokenRevocationStore
{
    private readonly IConnectionMultiplexer _connection = connection;

    public async Task RevokeAsync(
        Guid userId,
        DateTimeOffset revokedAt,
        TimeSpan timeToLive,
        CancellationToken cancellationToken
    )
    {
        await _connection.GetDatabase()
            .StringSetAsync(RevocationKeys.For(userId), revokedAt.ToUnixTimeSeconds(), timeToLive)
            .WaitAsync(cancellationToken);
    }

    public async Task<DateTimeOffset?> GetRevokedAtAsync(Guid userId, CancellationToken cancellationToken)
    {
        var value = await _connection.GetDatabase()
            .StringGetAsync(RevocationKeys.For(userId))
            .WaitAsync(cancellationToken);

        if (value.IsNull || !value.TryParse(out long unixSeconds))
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
    }
}
