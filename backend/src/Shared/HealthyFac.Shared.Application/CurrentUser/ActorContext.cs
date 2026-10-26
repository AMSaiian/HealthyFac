namespace HealthyFac.Shared.Application.CurrentUser;

/// <summary>
/// Describes who performs the current operation: a user of a request, the original actor of a received message,
/// or the system itself. Services never copy a token into it.
/// </summary>
public sealed record ActorContext
{
    public required Guid UserId { get; init; }

    public Guid? CompanyId { get; init; }

    public Guid? EmployeeId { get; init; }

    /// <summary>Tells that the system acts and no user does, for example in an anonymous request or a job.</summary>
    public bool IsSystem { get; init; }
}
