namespace HealthyFac.Shared.Web.Providers;

/// <summary>
/// Defines constants for current user resolution.
/// </summary>
public static class CurrentUserConstants
{
    /// <summary>
    /// Log message when user identifier resolution fails.
    /// </summary>
    public const string FailedToResolveUserIdMessage = "Failed to resolve current user identifier.";

    /// <summary>
    /// Error message when the user is not authenticated.
    /// </summary>
    public const string UserNotAuthenticatedMessage =
        "Current user is not authenticated or identifier claim is missing.";
}
