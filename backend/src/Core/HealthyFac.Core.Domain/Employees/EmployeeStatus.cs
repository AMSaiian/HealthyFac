using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Domain.Employees;

/// <summary>
/// Defines the lifecycle states of an employee. The camelCase values reach the API and the database,
/// so never rename one in place.
/// </summary>
public sealed class EmployeeStatus : Enumeration<EmployeeStatus>
{
    public static readonly EmployeeStatus Provisioning = new(value: "provisioning");
    public static readonly EmployeeStatus Active = new(value: "active");
    public static readonly EmployeeStatus ProvisioningFailed = new(value: "provisioningFailed");
    public static readonly EmployeeStatus Deactivated = new(value: "deactivated");

    private EmployeeStatus(string value)
        : base(value)
    {
    }
}
