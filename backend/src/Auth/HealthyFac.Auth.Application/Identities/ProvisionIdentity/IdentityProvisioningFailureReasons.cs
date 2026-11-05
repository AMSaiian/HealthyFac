namespace HealthyFac.Auth.Application.Identities.ProvisionIdentity;

/// <summary>
/// Defines the reasons that <c>IdentityProvisioningFailed</c> carries. Each reason is a failure that no retry can fix.
/// </summary>
public static class IdentityProvisioningFailureReasons
{
    public const string OrganizationNotFound = "OrganizationNotFound";

    public const string EmailAlreadyRegistered = "EmailAlreadyRegistered";

    public const string RoleNotFound = "RoleNotFound";
}
