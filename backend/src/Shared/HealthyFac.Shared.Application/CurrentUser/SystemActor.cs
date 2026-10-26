namespace HealthyFac.Shared.Application.CurrentUser;

/// <summary>
/// Defines the actor of anonymous requests, jobs, and system messages. The identifier is not a Keycloak user, and
/// nothing can log in with it.
/// </summary>
public static class SystemActor
{
    public static readonly Guid Id = Guid.Parse("5f0c4a51-7e1d-4c1b-9d0e-0a5c1d7e2b10");

    /// <summary>
    /// Creates the system actor.
    /// </summary>
    /// <param name="companyId">The company of a job that runs for one tenant, or <see langword="null"/>.</param>
    /// <returns>An <see cref="ActorContext"/> with <see cref="ActorContext.IsSystem"/> set.</returns>
    public static ActorContext Create(Guid? companyId = null) =>
        new()
        {
            UserId = Id,
            CompanyId = companyId,
            IsSystem = true,
        };
}
