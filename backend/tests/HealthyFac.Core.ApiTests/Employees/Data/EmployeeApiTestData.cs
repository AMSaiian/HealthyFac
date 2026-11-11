namespace HealthyFac.Core.ApiTests.Employees.Data;

public static class EmployeeApiTestData
{
    public const string RetryProvisioningPath = "v1/employees/retry-provisioning";
    public const string DeactivatePath = "v1/employees/deactivate";
    public const string MePath = "v1/employees/me";
    public const string SearchPath = "v1/employees/search";
    public const string LookupPath = "v1/employees/lookup";
    public const string InviteSearchPath = "v1/employee-invites/search";
    public const string ReadPermission = "employees:read";
    public const string MachinesReadPermission = "machines:read";
    public const string InvitePermission = "employees:invite";
    public const string ManagePermission = "employees:manage";
}
