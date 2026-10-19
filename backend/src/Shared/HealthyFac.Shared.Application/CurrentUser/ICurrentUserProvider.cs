namespace HealthyFac.Shared.Application.CurrentUser;

/// <summary>
/// Provides access to the current authenticated user identifier.
/// </summary>
public interface ICurrentUserProvider
{
    /// <summary>
    /// Attempts to retrieve the current user identifier.
    /// </summary>
    /// <returns>The user identifier if resolved; otherwise, null.</returns>
    Guid? TryGetUserId();

    /// <summary>
    /// Retrieves the current user identifier or throws if unavailable.
    /// </summary>
    /// <returns>The authenticated user identifier.</returns>
    Guid GetUserId();
}
