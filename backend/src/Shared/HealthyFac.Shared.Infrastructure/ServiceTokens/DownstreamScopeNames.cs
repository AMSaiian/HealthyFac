namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Builds the Keycloak scope names of the downstream scopes. Both token handlers use it, so the format
/// <c>{service}-{name}</c> exists in one place.
/// </summary>
internal static class DownstreamScopeNames
{
    /// <summary>
    /// Adds the name of the running service to each downstream scope.
    /// </summary>
    /// <param name="serviceName">The name of the running service.</param>
    /// <param name="downstreamScopes">The short names of the downstream scopes.</param>
    /// <returns>The Keycloak scope names, in the order of <paramref name="downstreamScopes"/>.</returns>
    public static List<string> Prefix(string serviceName, IEnumerable<string> downstreamScopes) =>
        downstreamScopes
            .Select(name => $"{serviceName}{ServiceTokenConstants.DownstreamScopeSeparator}{name}")
            .ToList();
}
