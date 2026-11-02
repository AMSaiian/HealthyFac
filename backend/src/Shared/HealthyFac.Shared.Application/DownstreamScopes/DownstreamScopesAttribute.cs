namespace HealthyFac.Shared.Application.DownstreamScopes;

/// <summary>
/// Declares the downstream scopes of a use case: what it asks for when it calls other services. They narrow the
/// service token of the call. Put it on an Application request or on a consumed message. The name of a downstream
/// scope is a constant in one class for each service (e.g., <c>AuthDownstreamScopes</c>). Prefer one downstream scope
/// for each use case.
/// </summary>
/// <param name="scopes">The short names of the downstream scopes.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DownstreamScopesAttribute(params string[] scopes) : Attribute
{
    public IReadOnlyCollection<string> Scopes { get; } = scopes;
}
