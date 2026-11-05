namespace HealthyFac.Shared.Application.CurrentUser;

/// <summary>
/// Defines the exceptions that the actor context throws.
/// </summary>
public static class CurrentUserErrors
{
    public static InvalidOperationException ActorAlreadySet() =>
        new(message: "The actor of this scope is already set. Each entry point sets the actor once.");

    public static ErrorHandling.UnauthorizedAccessException ActorMissing() =>
        new(message: "The current operation has no actor.");

    public static ErrorHandling.UnauthorizedAccessException CompanyMissing() =>
        new(message: "The current actor has no company.");

    public static ErrorHandling.UnauthorizedAccessException SubjectInvalid() =>
        new(message: "The token subject must be a GUID.");

    public static ErrorHandling.UnauthorizedAccessException OrganizationInvalid() =>
        new(message: "The token must carry exactly one organization, and it must be a GUID.");

    public static ErrorHandling.UnauthorizedAccessException EmployeeIdInvalid() =>
        new(message: "The token employee ID must be a GUID.");

    public static ErrorHandling.UnauthorizedAccessException DepartmentIdsInvalid() =>
        new(message: "The token department IDs must be GUIDs.");

    public static ErrorHandling.ForbiddenAccessException CompanyOutOfScope() =>
        new(message: "The request names a company outside the company of the actor.");

    public static ErrorHandling.ForbiddenAccessException DepartmentsOutOfScope() =>
        new(message: "The request names a department outside the departments of the actor.");

    public static ErrorHandling.UnauthorizedAccessException ServiceActorMissing() =>
        new(
            "A service token needs the actor ID header with a GUID, "
            + "and the company header must be a GUID when it is present.");

    public static ErrorHandling.UnauthorizedAccessException ServiceClientNotAllowed(string clientId) =>
        new($"The service client {clientId} is not allowed to call this service.");
}
