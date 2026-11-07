namespace HealthyFac.Telemetry.Application.MachineRegistrations;

/// <summary>
/// Defines error messages for the machine registration messages.
/// </summary>
public static class MachineRegistrationErrors
{
    public const string MachineIdRequired = "MachineId is required.";
    public const string GatewayIdInvalid = "GatewayId must have the gateway ID format.";
    public const string CompanyIdRequired = "CompanyId is required.";
    public const string DepartmentIdRequired = "DepartmentId is required.";
    public const string RegistrationNotFound = "The machine registration does not exist.";
    public const string MachineNotFound = "The machine does not exist.";
}
