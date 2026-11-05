using HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Defines the exceptions of the service token request. The texts never hold the client secret or a token.
/// </summary>
public static class ServiceTokenErrors
{
    public static InvalidOperationException TokenResponseInvalid() =>
        new(message: "The token endpoint answered success, but the response has no access token or no valid lifetime.");

    public static InvalidOperationException NoIncomingUserToken() =>
        new(
            "The call needs the token of a user, but the current flow has none. "
            + "Use a client with a service token for work that no user started.");

    public static InvalidOperationException ActorHeadersNeedTokenExchange() =>
        new(
            "The current request has the token of a user, so the call must use the token exchange. "
            + "A service token with actor headers would replace the user token.");

    public static InvalidOperationException ActorHeadersWithoutActor() =>
        new(message: "The current flow has no actor, so the call has no actor to send in the actor headers.");

    public static TokenStorageException StorageSkipped() =>
        new(message: "The token storage was skipped, because the Redis circuit breaker is open.");

    public static TokenStorageException StorageFailed(Exception innerException) =>
        new($"The token storage call failed with {innerException.GetType().Name}.", innerException);
}
