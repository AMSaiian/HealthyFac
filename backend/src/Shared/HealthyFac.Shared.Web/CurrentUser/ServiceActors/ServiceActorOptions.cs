namespace HealthyFac.Shared.Web.CurrentUser.ServiceActors;

/// <summary>
/// Configures which services may call this service with a service token (Client Credentials).
/// </summary>
public sealed class ServiceActorOptions
{
    public const string SectionName = "ServiceActors";

    /// <summary>
    /// The client IDs whose service accounts may call this service (e.g., <c>healthyfac-auth</c>). A service token
    /// of any other client gets <c>401</c>. The empty list is a real value: the service then accepts no service
    /// token, so a missing section fails closed. The property is not <see langword="required"/>, because the
    /// configuration binder writes no key for an empty JSON array.
    /// </summary>
    public IReadOnlyCollection<string> AllowedClients { get; init; } = [];
}
