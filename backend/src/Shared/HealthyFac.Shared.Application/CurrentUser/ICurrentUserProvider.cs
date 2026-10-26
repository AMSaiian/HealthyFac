namespace HealthyFac.Shared.Application.CurrentUser;

/// <summary>
/// Provides the actor of the current DI scope: the user of a request, the original actor of a received message,
/// or the system actor.
/// </summary>
public interface ICurrentUserProvider
{
    /// <summary>
    /// Attempts to retrieve the current user identifier.
    /// </summary>
    /// <returns>The user identifier if an actor is set; otherwise, null.</returns>
    Guid? TryGetUserId();

    /// <summary>
    /// Retrieves the current user identifier or throws if unavailable.
    /// </summary>
    /// <returns>The user identifier of the actor.</returns>
    /// <exception cref="ErrorHandling.UnauthorizedAccessException">Thrown when the scope has no actor.</exception>
    Guid GetUserId();

    /// <summary>
    /// Attempts to retrieve the company of the current actor.
    /// </summary>
    /// <returns>The company identifier if the actor has one; otherwise, null.</returns>
    Guid? TryGetCompanyId();

    /// <summary>
    /// Retrieves the company of the current actor or throws if unavailable.
    /// </summary>
    /// <returns>The company identifier of the actor.</returns>
    /// <exception cref="ErrorHandling.UnauthorizedAccessException">
    /// Thrown when the scope has no actor or the actor has no company.
    /// </exception>
    Guid GetCompanyId();

    /// <summary>
    /// Attempts to retrieve the whole actor of the scope.
    /// </summary>
    /// <returns>The actor if one is set; otherwise, null.</returns>
    ActorContext? TryGetActor();
}
