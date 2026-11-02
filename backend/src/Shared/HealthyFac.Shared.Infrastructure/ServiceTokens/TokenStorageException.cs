namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Signals that the <see cref="ITokenCacheStorage"/> failed or was skipped. The <see cref="TokenStore"/> handles it
/// and fetches the token itself. The message never holds a token or a stored value.
/// </summary>
public sealed class TokenStorageException : Exception
{
    public TokenStorageException(string message)
        : base(message)
    {
    }

    public TokenStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
