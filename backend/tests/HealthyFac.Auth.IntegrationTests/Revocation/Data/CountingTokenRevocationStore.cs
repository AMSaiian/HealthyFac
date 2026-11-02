using HealthyFac.Shared.Infrastructure.Revocation;

namespace HealthyFac.Auth.IntegrationTests.Revocation.Data;

/// <summary>
/// Plays the revocation list in memory and counts the writes, so a test can tell whether a write reached the store.
/// </summary>
public sealed class CountingTokenRevocationStore : ITokenRevocationStore
{
    private int _revokeCount;

    public int RevokeCount => Volatile.Read(ref _revokeCount);

    public Task RevokeAsync(
        Guid userId,
        DateTimeOffset revokedAt,
        TimeSpan timeToLive,
        CancellationToken cancellationToken
    )
    {
        Interlocked.Increment(ref _revokeCount);

        return Task.CompletedTask;
    }

    public Task<DateTimeOffset?> GetRevokedAtAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<DateTimeOffset?>(null);
}
