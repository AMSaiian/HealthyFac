namespace HealthyFac.Shared.Application.DownstreamScopes;

/// <summary>
/// Holds the downstream scopes of the current flow. The service token handler reads them to ask Keycloak for a token
/// that carries only the permissions of the flow. The value flows with the asynchronous call chain, because a handler of
/// <c>IHttpClientFactory</c> lives in its own DI scope.
/// </summary>
public interface IDownstreamScopeHolder
{
    /// <summary>The downstream scopes of the current flow, or an empty collection when it declares none.</summary>
    IReadOnlyCollection<string> Current { get; }

    /// <summary>
    /// Sets the downstream scopes of the current flow until the returned lease is disposed. A call replaces the current
    /// set and never adds to it. The use case decorator calls it for every request, with the downstream scopes of
    /// <see cref="DownstreamScopesAttribute"/> or with none. A worker that is not a use case calls it around its
    /// work.
    /// </summary>
    /// <param name="scopes">The short names of the downstream scopes.</param>
    /// <returns>A lease that restores the downstream scopes from before when it is disposed.</returns>
    IDisposable Enter(params string[] scopes);
}
