namespace HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;

/// <summary>
/// Defines the key format of the token store. Every key starts with the service name, so two services never share a
/// key, and the instances of one service do. A key holds no secret, so it may appear in a log.
/// </summary>
internal static class TokenCacheKeys
{
    private const char Separator = ':';
    private const string ServiceTokenSegment = "service-token";
    private const string ExchangedTokenSegment = "exchange-token";
    private const string LockSegment = "token-lock";

    /// <summary>
    /// Builds the key of a Client Credentials token.
    /// </summary>
    /// <param name="serviceName">The name of the running service.</param>
    /// <param name="scopes">The sorted scopes, separated by spaces. The text is empty for a token without
    /// scopes.</param>
    /// <returns>The key, for example <c>auth:service-token:auth-provisioning</c>.</returns>
    public static string ForServiceToken(string serviceName, string scopes) =>
        $"{serviceName}{Separator}{ServiceTokenSegment}{Separator}{scopes}";

    /// <summary>
    /// Builds the key of an exchanged user token.
    /// </summary>
    /// <param name="serviceName">The name of the running service.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="audience">The client ID of the target service.</param>
    /// <param name="scope">The sorted scopes, separated by spaces.</param>
    /// <returns>The key, for example <c>auth:exchange-token:{user ID}:healthyfac-core:organization</c>.</returns>
    public static string ForExchangedToken(string serviceName, Guid userId, string audience, string scope) =>
        $"{serviceName}{Separator}{ExchangedTokenSegment}{Separator}{userId:D}{Separator}{audience}{Separator}{scope}";

    /// <summary>
    /// Builds the key of the fetch lock of a token.
    /// </summary>
    /// <param name="serviceName">The name of the running service.</param>
    /// <param name="tokenKey">The key of the token that the lock protects.</param>
    /// <returns>The key, for example <c>auth:token-lock:auth:service-token:auth-provisioning</c>.</returns>
    public static string ForLock(string serviceName, string tokenKey) =>
        $"{serviceName}{Separator}{LockSegment}{Separator}{tokenKey}";
}
