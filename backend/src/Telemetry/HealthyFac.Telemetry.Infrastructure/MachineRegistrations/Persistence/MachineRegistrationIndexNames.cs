namespace HealthyFac.Telemetry.Infrastructure.MachineRegistrations.Persistence;

/// <summary>
/// Defines the database index names of the machine registration table that code reads, so a name never differs
/// between the mapping and a check.
/// </summary>
public static class MachineRegistrationIndexNames
{
    public const string GatewayId = "ix_machine_registrations_gateway_id";
}
