namespace HealthyFac.Core.Application.Employees;

/// <summary>
/// Defines error messages and exceptions for identity provisioning and deactivation of employees, grouped by the
/// kind of failure.
/// </summary>
public static class EmployeeProvisioningErrors
{
    public static class Validation
    {
        public const string EmployeeIdRequired = "EmployeeId is required.";
        public const string IdentityUserIdRequired = "IdentityUserId is required.";
        public const string ReasonRequired = "Reason is required.";
    }

    public static class Rejections
    {
        public const string EmployeeNotFound = "The employee does not exist in the company.";
        public const string EmployeeNotRetryable = "Only an employee who waits for an identity can retry.";
        public const string EmployeeAlreadyDeactivated = "The employee is already deactivated.";
        public const string EmployeeIsSelf = "An employee cannot deactivate themselves.";
        public const string EmployeeChanged = "The employee changed at the same time. Reload it and try again.";
    }

    public static InvalidOperationException EmployeeMissing(Guid employeeId) =>
        new($"Employee '{employeeId}' does not exist, so its provisioning result cannot apply.");
}
