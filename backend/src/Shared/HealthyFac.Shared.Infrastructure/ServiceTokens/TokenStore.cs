using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Keeps access tokens in an <see cref="ITokenCacheStorage"/> (Redis), one for each key, until they near their
/// expiry. The instances of a service share the tokens. The Client Credentials cache and the token exchange client
/// use it.
/// </summary>
/// <remarks>
/// Never replace it with a cache in process memory, a database, or a message: the token store is the only place of
/// a token. The storage keeps each token as JSON (the token and its refresh moment) with the token lifetime
/// as the time to live, so Redis removes a token at its expiry. <see cref="TimeProvider"/> checks the refresh moment.
/// <para>
/// Steps of <c>GetOrFetchAsync</c>:
/// </para>
/// <list type="number">
/// <item><description>Read the storage. A usable token ends the call.</description></item>
/// <item><description>Try to take the storage lock of the key. When this call holds it, read once more, because
/// another instance can have stored the token just before the lock was free. Fetch only when the token is still not
/// usable, store it, and release the lock in a <c>finally</c> block with <see cref="CancellationToken.None"/>: the
/// release is short, and a lost release only makes the others wait for the lock time to live.</description></item>
/// <item><description>When another instance holds the lock, read the storage every
/// <see cref="ServiceTokenOptions.LockPollMilliseconds"/> and return the token as soon as it is usable. After
/// <see cref="ServiceTokenOptions.LockTimeoutSeconds"/> the call fetches the token itself and stores it, so a holder
/// that crashed never blocks a call.</description></item>
/// <item><description>When the lock call fails, fetch the token locally and log a warning.</description></item>
/// </list>
/// The storage lock protects only the load on the token endpoint, never the correctness of a token: a token from a
/// second fetch is as valid as the first one. The fetch gets its own limit of
/// <see cref="ServiceTokenOptions.LockTimeoutSeconds"/> on the clock of the <see cref="TimeProvider"/>, so a slow token
/// endpoint never outlives the lock. Every call of every instance waits on the same storage lock. The store keeps no
/// state in process memory. A failed fetch stores nothing, and the next call tries again. A token whose refresh
/// moment has passed is returned and not stored.
/// A storage failure never fails a call (see <see cref="TokenStorageException"/>): a failed read counts as a miss,
/// and a failed write still returns the token. While the storage is down, every call fetches its own token, and the
/// Redis circuit breaker makes each storage call fail fast. The log never holds a token or a stored value.
/// </remarks>
/// <param name="storage">Keeps the tokens and the locks.</param>
/// <param name="timeProvider">Provides the current time and the clock of the waits.</param>
/// <param name="tokenOptions">Holds the lock timeout and the poll interval.</param>
/// <param name="serviceOptions">Holds the name of the running service for the lock keys.</param>
/// <param name="logger">Logs the storage failures.</param>
public sealed class TokenStore(
    ITokenCacheStorage storage,
    TimeProvider timeProvider,
    IOptions<ServiceTokenOptions> tokenOptions,
    IOptions<ServiceOptions> serviceOptions,
    ILogger<TokenStore> logger
)
{
    private readonly ITokenCacheStorage _storage = storage;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ServiceTokenOptions _options = tokenOptions.Value;
    private readonly string _serviceName = serviceOptions.Value.Name;
    private readonly ILogger<TokenStore> _logger = logger;

    private enum LockOutcome
    {
        Acquired,
        Held,
        Unavailable,
    }

    private TimeSpan LockTimeout => TimeSpan.FromSeconds(_options.LockTimeoutSeconds);

    private TimeSpan PollInterval => TimeSpan.FromMilliseconds(_options.LockPollMilliseconds);

    /// <summary>
    /// Returns the stored token of the key, or fetches a new one when the storage has none or the token has passed
    /// its refresh moment. Two calls can fetch one token at the same time when the lock call fails.
    /// </summary>
    /// <param name="key">The key of the token (see <see cref="TokenCacheKeys"/>).</param>
    /// <param name="fetch">Requests a new token. It gets a token that the fetch limit also cancels.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The access token.</returns>
    internal async Task<string> GetOrFetchAsync(
        string key,
        Func<CancellationToken, Task<CachedToken>> fetch,
        CancellationToken cancellationToken
    )
    {
        var token = await TryReadUsableAsync(key, cancellationToken);
        if (token is not null)
        {
            return token;
        }

        return await FetchAcrossInstancesAsync(key, fetch, cancellationToken);
    }

    private async Task<string> FetchAcrossInstancesAsync(
        string key,
        Func<CancellationToken, Task<CachedToken>> fetch,
        CancellationToken cancellationToken
    )
    {
        var lockKey = TokenCacheKeys.ForLock(_serviceName, key);
        var owner = Guid.NewGuid().ToString("N");
        var outcome = await TryAcquireLockAsync(lockKey, owner, cancellationToken);

        if (outcome == LockOutcome.Acquired)
        {
            return await FetchHoldingLockAsync(key, lockKey, owner, fetch, cancellationToken);
        }

        if (outcome == LockOutcome.Held)
        {
            var token = await WaitForOtherInstanceAsync(key, cancellationToken);
            if (token is not null)
            {
                return token;
            }
        }

        return await FetchAndStoreAsync(key, fetch, cancellationToken);
    }

    private async Task<string> FetchHoldingLockAsync(
        string key,
        string lockKey,
        string owner,
        Func<CancellationToken, Task<CachedToken>> fetch,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var token = await TryReadUsableAsync(key, cancellationToken);
            if (token is not null)
            {
                return token;
            }

            return await FetchAndStoreAsync(key, fetch, cancellationToken);
        }
        finally
        {
            await TryReleaseLockAsync(lockKey, owner);
        }
    }

    private async Task<LockOutcome> TryAcquireLockAsync(
        string lockKey,
        string owner,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var isAcquired = await _storage.TryAcquireLockAsync(lockKey, owner, LockTimeout, cancellationToken);

            return isAcquired ? LockOutcome.Acquired : LockOutcome.Held;
        }
        catch (TokenStorageException exception)
        {
            _logger.LogWarning(
                "Token lock {LockKey} could not be taken. The call fetches the token itself. Failure: {FailureMessage}",
                lockKey,
                exception.Message);

            return LockOutcome.Unavailable;
        }
    }

    private async Task<string?> WaitForOtherInstanceAsync(string key, CancellationToken cancellationToken)
    {
        var deadline = _timeProvider.GetUtcNow() + LockTimeout;

        while (_timeProvider.GetUtcNow() < deadline)
        {
            await Task.Delay(PollInterval, _timeProvider, cancellationToken);

            try
            {
                var token = await ReadUsableAsync(key, cancellationToken);
                if (token is not null)
                {
                    return token;
                }
            }
            catch (TokenStorageException exception)
            {
                LogReadFailure(key, exception);

                return null;
            }
        }

        return null;
    }

    private async Task<string> FetchAndStoreAsync(
        string key,
        Func<CancellationToken, Task<CachedToken>> fetch,
        CancellationToken cancellationToken
    )
    {
        using var timeout = new CancellationTokenSource(LockTimeout, _timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var fetched = await fetch(linked.Token);
        await TryStoreAsync(key, fetched, cancellationToken);

        return fetched.Value;
    }

    private async Task TryStoreAsync(string key, CachedToken token, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        if (token.RefreshAt <= now)
        {
            return;
        }

        var json = new StoredToken { Token = token.Value, RefreshAt = token.RefreshAt }.ToJson();
        try
        {
            await _storage.SetAsync(key, json, token.ExpiresAt - now, cancellationToken);
        }
        catch (TokenStorageException exception)
        {
            _logger.LogWarning(
                "Token entry {Key} could not be stored. The call keeps the token. Failure: {FailureMessage}",
                key,
                exception.Message);
        }
    }

    private async Task TryReleaseLockAsync(string lockKey, string owner)
    {
        try
        {
            await _storage.ReleaseLockAsync(lockKey, owner, CancellationToken.None);
        }
        catch (TokenStorageException exception)
        {
            _logger.LogWarning(
                "Token lock {LockKey} could not be released. It ends with its time to live. Failure: {FailureMessage}",
                lockKey,
                exception.Message);
        }
    }

    private async Task<string?> TryReadUsableAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            return await ReadUsableAsync(key, cancellationToken);
        }
        catch (TokenStorageException exception)
        {
            LogReadFailure(key, exception);

            return null;
        }
    }

    private async Task<string?> ReadUsableAsync(string key, CancellationToken cancellationToken)
    {
        var json = await _storage.GetAsync(key, cancellationToken);
        if (json is null)
        {
            return null;
        }

        var stored = StoredToken.FromJson(json);
        if (stored is null)
        {
            _logger.LogWarning("Token entry {Key} is not a valid entry. The call treats it as missing.", key);

            return null;
        }

        return _timeProvider.GetUtcNow() < stored.RefreshAt ? stored.Token : null;
    }

    private void LogReadFailure(string key, TokenStorageException exception) =>
        _logger.LogWarning(
            "Token entry {Key} could not be read. The call treats it as missing. Failure: {FailureMessage}",
            key,
            exception.Message);
}
