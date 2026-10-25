namespace HealthyFac.Core.Application.Employees;

/// <summary>
/// Defines error messages for employee invite operations, grouped by the kind of failure.
/// </summary>
public static class EmployeeInviteErrors
{
    public static class Validation
    {
        public const string EmailRequired = "Email is required.";
        public const string EmailInvalid = "Email must be a valid email address.";
        public const string CompanyIdRequired = "CompanyId is required.";
        public const string RoleCodeRequired = "RoleCode is required.";
        public const string DepartmentIdsRequired = "At least one department is required.";
        public const string DepartmentIdRequired = "DepartmentIds must not contain an empty ID.";
        public const string InviteIdRequired = "InviteId is required.";
        public const string TokenRequired = "Token is required.";
        public const string FirstNameRequired = "First name is required.";
        public const string LastNameRequired = "Last name is required.";
    }

    public static class Rejections
    {
        public const string RoleNotFound = "The role does not exist.";
        public const string DepartmentsNotFound = "One or more departments do not exist in the company.";
        public const string EmailAlreadyEmployed = "A person with this email is already an employee.";
        public const string OpenInviteExists = "An open invite for this email already exists in the company.";
        public const string InviteNotFound = "The invite does not exist in the company.";
        public const string InviteAlreadyAccepted = "The invite is already accepted.";
        public const string InviteChanged = "The invite changed at the same time. Reload it and try again.";

        /// <summary>
        /// The one message for every token failure, so a caller cannot learn which check failed.
        /// </summary>
        public const string InviteLinkInvalid = "The invite link is invalid or expired.";
    }
}
