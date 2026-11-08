namespace HealthyFac.Auth.Messaging.Identities;

/// <summary>
/// Defines the validation messages of the identity messages.
/// </summary>
public static class IdentityMessageErrors
{
    public static class Validation
    {
        public const string EmployeeIdRequired = "EmployeeId is required.";
        public const string IdentityUserIdRequired = "IdentityUserId is required.";
        public const string ReasonRequired = "Reason is required.";
    }
}
