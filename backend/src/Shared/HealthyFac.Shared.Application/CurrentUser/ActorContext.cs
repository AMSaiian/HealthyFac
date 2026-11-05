namespace HealthyFac.Shared.Application.CurrentUser;

/// <summary>
/// Describes who performs the current operation: a user of a request, the original actor of a received message,
/// or a flow of a service. Services never copy a token into it.
/// </summary>
public sealed record ActorContext
{
    public required ActorKind Kind { get; init; }

    /// <summary>The ID of the actor: the Keycloak user ID of a user, or the name-based ID of any other actor.</summary>
    public required Guid UserId { get; init; }

    /// <summary>The name of a service actor in the service that runs the flow (e.g., <c>core:outboxProcessor</c>).
    /// <see langword="null"/> for a user, for the anonymous actor, and for every restored actor.</summary>
    public string? Name { get; init; }

    public Guid? CompanyId { get; init; }

    public Guid? EmployeeId { get; init; }

    /// <summary>
    /// The IDs of the departments of the employee. A user token fills them. Every other actor (anonymous, service
    /// flow, restored message actor) has none, so a rule that needs departments fails closed for them.
    /// </summary>
    public required IReadOnlySet<Guid> DepartmentIds { get; init; }
}
