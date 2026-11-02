using HealthyFac.Shared.Infrastructure.ServiceTokens;

namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

/// <summary>
/// Plays the token storage in memory, so several token caches can share it like instances that share Redis. The lock
/// check and the lock write are one atomic step. It ignores the time to live, because the tests check the refresh
/// moment of the entry. Each failure switch makes the matching calls throw <see cref="TokenStorageException"/>.
/// </summary>
public sealed class InMemoryTokenCacheStorage : ITokenCacheStorage
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, string> _values = [];
    private readonly Dictionary<string, string> _locks = [];
    private readonly TaskCompletionSource _tokenStored = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _lockAcquireCount;

    public bool IsReadFailing { get; set; }

    public bool IsWriteFailing { get; set; }

    public bool IsLockFailing { get; set; }

    public int LockAcquireCount => Volatile.Read(ref _lockAcquireCount);

    /// <summary>
    /// Completes when the first entry is stored, so a test moves the clock of the waiting calls only after the
    /// holder of the lock has stored its token.
    /// </summary>
    public Task TokenStored => _tokenStored.Task;

    public IReadOnlyCollection<string> HeldLockKeys
    {
        get
        {
            lock (_gate)
            {
                return _locks.Keys.ToList();
            }
        }
    }

    public IReadOnlyCollection<string> StoredKeys
    {
        get
        {
            lock (_gate)
            {
                return _values.Keys.ToList();
            }
        }
    }

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        if (IsReadFailing)
        {
            throw ServiceTokenErrors.StorageSkipped();
        }

        lock (_gate)
        {
            return Task.FromResult(_values.GetValueOrDefault(key));
        }
    }

    public Task SetAsync(string key, string value, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        if (IsWriteFailing)
        {
            throw ServiceTokenErrors.StorageSkipped();
        }

        lock (_gate)
        {
            _values[key] = value;
        }

        _tokenStored.TrySetResult();

        return Task.CompletedTask;
    }

    public Task<bool> TryAcquireLockAsync(
        string key,
        string owner,
        TimeSpan timeToLive,
        CancellationToken cancellationToken
    )
    {
        if (IsLockFailing)
        {
            throw ServiceTokenErrors.StorageSkipped();
        }

        lock (_gate)
        {
            var isAcquired = _locks.TryAdd(key, owner);
            if (isAcquired)
            {
                Interlocked.Increment(ref _lockAcquireCount);
            }

            return Task.FromResult(isAcquired);
        }
    }

    public Task ReleaseLockAsync(string key, string owner, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_locks.TryGetValue(key, out var holder) && holder == owner)
            {
                _locks.Remove(key);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stores a token entry the way another instance writes it.
    /// </summary>
    /// <param name="key">The key of the entry.</param>
    /// <param name="token">The access token.</param>
    /// <param name="refreshAt">The moment at which the entry stops being usable.</param>
    public void StoreToken(string key, string token, DateTimeOffset refreshAt)
    {
        var json = new StoredToken { Token = token, RefreshAt = refreshAt }.ToJson();
        lock (_gate)
        {
            _values[key] = json;
        }
    }

    /// <summary>
    /// Makes another instance hold a lock.
    /// </summary>
    /// <param name="key">The key of the lock.</param>
    /// <param name="owner">The value that names the other holder.</param>
    public void HoldLock(string key, string owner)
    {
        lock (_gate)
        {
            _locks[key] = owner;
        }
    }
}
