namespace HealthyFac.Shared.Application.CurrentUser;

/// <summary>
/// Defines the exceptions that the actor context throws.
/// </summary>
public static class CurrentUserErrors
{
    public static InvalidOperationException ActorAlreadySet() =>
        new("The actor of this scope is already set. Each entry point sets the actor once.");

    public static ErrorHandling.UnauthorizedAccessException ActorMissing() =>
        new("The current operation has no actor.");

    public static ErrorHandling.UnauthorizedAccessException CompanyMissing() =>
        new("The current actor has no company.");
}
