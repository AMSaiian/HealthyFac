namespace HealthyFac.Core.Messaging.Employees;

/// <summary>
/// Defines the validation messages of the employee messages.
/// </summary>
public static class EmployeeMessageErrors
{
    public static class Validation
    {
        public const string InviteIdRequired = "InviteId is required.";
        public const string EmployeeIdRequired = "EmployeeId is required.";
        public const string CompanyIdRequired = "CompanyId is required.";
        public const string EmailRequired = "Email is required.";
        public const string EmailInvalid = "Email must be a valid email address.";
        public const string FirstNameRequired = "First name is required.";
        public const string LastNameRequired = "Last name is required.";
        public const string RoleCodeRequired = "RoleCode is required.";
        public const string DepartmentIdsRequired = "At least one department ID is required.";
        public const string DepartmentIdEmpty = "A department ID must not be empty.";
        public const string IdentityUserIdInvalid = "IdentityUserId must not be empty when it is set.";
        public const string DeactivatedAtRequired = "DeactivatedAt is required.";
    }
}
