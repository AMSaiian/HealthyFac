namespace HealthyFac.Core.Application.Machines;

/// <summary>
/// Defines error messages for machine operations.
/// </summary>
public static class MachineErrors
{
    public const string NameRequired = "Machine name is required.";
    public const string SerialNumberRequired = "Serial number is required.";
    public const string CompanyIdRequired = "CompanyId is required.";
    public const string DepartmentIdRequired = "DepartmentId is required.";
    public const string DepartmentNotFound = "The department does not exist in the company.";
}
