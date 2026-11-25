using HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;

namespace HealthyFac.Insights.UnitTests.WorkOrders.Data;

/// <summary>
/// Plays the Redis storage of the service token in memory, for one process.
/// </summary>
public sealed class InMemoryTokenStorage : ITokenCacheStorage
{
    private readonly Dictionary<string, string> _values = [];
    private readonly HashSet<string> _locks = [];

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(_values.GetValueOrDefault(key));

    public Task SetAsync(string key, string value, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        _values[key] = value;

        return Task.CompletedTask;
    }

    public Task<bool> TryAcquireLockAsync(
        string key,
        string owner,
        TimeSpan timeToLive,
        CancellationToken cancellationToken
    ) => Task.FromResult(_locks.Add(key));

    public Task ReleaseLockAsync(string key, string owner, CancellationToken cancellationToken)
    {
        _locks.Remove(key);

        return Task.CompletedTask;
    }
}
