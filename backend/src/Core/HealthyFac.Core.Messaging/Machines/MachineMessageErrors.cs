namespace HealthyFac.Core.Messaging.Machines;

/// <summary>
/// Defines the validation messages of the machine messages.
/// </summary>
public static class MachineMessageErrors
{
    public static class Validation
    {
        public const string MachineIdRequired = "MachineId is required.";
        public const string GatewayIdInvalid = "GatewayId must have the gateway ID format.";
        public const string CompanyIdRequired = "CompanyId is required.";
    }
}
